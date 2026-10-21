using Microsoft.EntityFrameworkCore;
using PayAhead.Application.Payments;
using PayAhead.Application.Subscriptions;
using PayAhead.Domain;

namespace PayAhead.Infrastructure;

public sealed class SubscriptionStore(PayAheadDbContext db) : ISubscriptionStore
{
    private IQueryable<TrackedSubscription> Owned(Guid user) => db.TrackedSubscriptions.AsNoTracking().Where(x => x.UserId == user && !x.IsDeleted);
    private IQueryable<SubscriptionRecord> Joined(IQueryable<TrackedSubscription> subscriptions) => from s in subscriptions
        join p in db.PaymentPlans.AsNoTracking() on new { Id=s.PaymentPlanId,s.UserId } equals new { p.Id,p.UserId }
        select new SubscriptionRecord(s,p);
    public Task<string?> TimeZoneAsync(Guid user,CancellationToken ct)=>db.UserSettings.Where(x=>x.UserId==user).Select(x=>x.TimeZoneId).SingleOrDefaultAsync(ct);
    public async Task<IReadOnlyList<SubscriptionRecord>> LoadAsync(Guid user,CancellationToken ct)
    {
        var rows=await Joined(Owned(user).OrderBy(x=>x.Id).Take(1001)).ToArrayAsync(ct);
        if(rows.Length>1000)throw new DomainRuleException("En fazla 1000 silinmemiş abonelik desteklenir.");return rows;
    }
    public Task<SubscriptionRecord?> FindAsync(Guid user,Guid id,CancellationToken ct)=>Joined(Owned(user).Where(x=>x.Id==id)).SingleOrDefaultAsync(ct);
    public Task<SubscriptionRecord> CreateAsync(Guid user,SubscriptionRequest request,DateTime now,DateOnly today,CancellationToken ct)
        => Transaction(async()=>
        {
            if(await db.Users.FromSqlInterpolated($"SELECT * FROM Users WITH (UPDLOCK,HOLDLOCK) WHERE Id={user} AND IsActive=1").SingleOrDefaultAsync(ct) is null)throw new AuthenticationRequiredException();
            if(await db.TrackedSubscriptions.CountAsync(x=>x.UserId==user&&!x.IsDeleted,ct)>=1000)throw new ResourceConflictException("En fazla 1000 abonelik oluşturulabilir.");
            var plan=PaymentPlan.Create(user,Details(request),now);
            var s=new TrackedSubscription { Id=Guid.NewGuid(),UserId=user,PaymentPlanId=plan.Id,CreatedAtUtc=now };
            Apply(s,request,now);db.PaymentPlans.Add(plan);db.TrackedSubscriptions.Add(s);
            await SyncTrialRules(s,ct);await db.SaveChangesAsync(ct);return new SubscriptionRecord(s,plan);
        },ct);
    public Task<SubscriptionRecord> UpdateAsync(Guid user,Guid id,SubscriptionRequest request,DateTime now,DateOnly today,CancellationToken ct)
        => Transaction(async()=>
        {
            var row=await Locked(user,id,Convert.FromBase64String(request.RowVersion!),ct);
            Mutable(row.Subscription,today);row.Subscription.Transition(request.Status!.Value,today);
            Apply(row.Subscription,request,now);row.Plan.UpdateDetails(Details(request),now);
            await CancelPending(row.Plan.Id,now,ct);await SyncTrialRules(row.Subscription,ct);
            await db.SaveChangesAsync(ct);return row;
        },ct);
    public Task<SubscriptionRecord> CancelAsync(Guid user,Guid id,CancelSubscriptionRequest request,DateTime now,DateOnly today,CancellationToken ct)
        => Transaction(async()=>
        {
            var row=await Locked(user,id,Convert.FromBase64String(request.RowVersion!),ct);Mutable(row.Subscription,today);
            row.Subscription.Cancel(request.AccessEndsOn!.Value,today,now);row.Plan.IsActive=false;row.Plan.UpdatedAtUtc=now;
            await CancelPending(row.Plan.Id,now,ct);await SyncTrialRules(row.Subscription,ct);
            await db.SaveChangesAsync(ct);return row;
        },ct);
    public async Task DeleteAsync(Guid user,Guid id,byte[] version,DateTime now,CancellationToken ct)
        => _=await Transaction(async()=>
        {
            var row=await Locked(user,id,version,ct);row.Subscription.IsDeleted=true;row.Subscription.AutoRenew=false;row.Subscription.UpdatedAtUtc=now;
            row.Plan.IsActive=false;row.Plan.UpdatedAtUtc=now;
            await CancelPending(row.Plan.Id,now,ct);await SyncTrialRules(row.Subscription,ct);await db.SaveChangesAsync(ct);return true;
        },ct);
    private async Task<SubscriptionRecord> Locked(Guid user,Guid id,byte[] version,CancellationToken ct)
    {
        var info=await db.TrackedSubscriptions.AsNoTracking().SingleOrDefaultAsync(x=>x.Id==id&&x.UserId==user&&!x.IsDeleted,ct)??throw new ResourceNotFoundException();
        // Same lock order as the notification scheduler: plan first, then subscription.
        var plan=await db.PaymentPlans.FromSqlInterpolated($"SELECT * FROM PaymentPlans WITH (UPDLOCK,HOLDLOCK) WHERE Id={info.PaymentPlanId} AND UserId={user}").SingleAsync(ct);
        var s=await db.TrackedSubscriptions.SingleOrDefaultAsync(x=>x.Id==id&&x.UserId==user&&!x.IsDeleted,ct)??throw new ResourceNotFoundException();
        if(!s.RowVersion.SequenceEqual(version))throw new ResourceConflictException("Abonelik başka bir işlem tarafından değiştirildi.");
        db.Entry(s).Property(x=>x.RowVersion).OriginalValue=version;db.Entry(s).Property(x=>x.UpdatedAtUtc).IsModified=true;
        return new(s,plan);
    }
    private static void Mutable(TrackedSubscription s,DateOnly today)
    { if(s.EffectiveStatus(today) is SubscriptionStatus.Cancelled or SubscriptionStatus.Expired)throw new ResourceConflictException("İptal edilmiş veya süresi bitmiş abonelik güncellenemez."); }
    private static PaymentPlanDetails Details(SubscriptionRequest r)=>new(r.Name!.Trim(),r.Description?.Trim(),r.Amount!.Value,r.CurrencyCode!.Trim().ToUpperInvariant(),"Subscriptions",r.BillingFrequency!.Value,r.BillingInterval!.Value,r.FirstBillingDate!.Value,
        r.AutoRenew?r.AccessEndsOn:null,r.AnchorDay,r.AutoRenew);
    private static void Apply(TrackedSubscription s,SubscriptionRequest r,DateTime now)
    {
        s.ProviderName=r.ProviderName!.Trim();s.StartDate=r.StartDate!.Value;s.TrialStartDate=r.TrialStartDate;s.TrialEndDate=r.TrialEndDate;
        s.Status=r.Status!.Value;s.AutoRenew=r.AutoRenew;s.AccessEndsOn=r.AccessEndsOn;s.EnableTrialReminders=r.EnableTrialReminders;s.UpdatedAtUtc=now;
    }
    private async Task SyncTrialRules(TrackedSubscription s,CancellationToken ct)
    {
        var rules=await db.ReminderRules.Where(x=>x.PaymentPlanId==s.PaymentPlanId&&x.UserId==s.UserId&&x.Target==ReminderTarget.SubscriptionTrial).ToListAsync(ct);
        if(rules.Count==0 && s.TrialEndDate.HasValue)
            foreach(var lead in new[]{0,1,3,7}) { var rule=new ReminderRule { Id=Guid.NewGuid(),UserId=s.UserId,PaymentPlanId=s.PaymentPlanId,Target=ReminderTarget.SubscriptionTrial,DaysBefore=lead,TimeOfDay=new(9,0) };db.ReminderRules.Add(rule);rules.Add(rule); }
        foreach(var rule in rules)rule.IsEnabled=!s.IsDeleted&&s.EnableTrialReminders&&s.Status==SubscriptionStatus.Trial;
    }
    private Task<int> CancelPending(Guid plan,DateTime now,CancellationToken ct)=>db.NotificationDeliveries
        .Where(x=>x.PaymentPlanId==plan&&x.ProviderMessageId==null&&(x.Status==DeliveryStatus.Pending||x.Status==DeliveryStatus.Processing))
        .ExecuteUpdateAsync(s=>s.SetProperty(x=>x.Status,DeliveryStatus.Cancelled).SetProperty(x=>x.LeaseOwner,(Guid?)null).SetProperty(x=>x.LeaseExpiresAtUtc,(DateTime?)null)
            .SetProperty(x=>x.LastErrorCode,"SubscriptionChanged").SetProperty(x=>x.UpdatedAtUtc,now),ct);
    private Task<T> Transaction<T>(Func<Task<T>> action,CancellationToken ct)=>db.Database.CreateExecutionStrategy().ExecuteAsync(async()=>
    { db.ChangeTracker.Clear();await using var transaction=await db.Database.BeginTransactionAsync(ct);var result=await action();await transaction.CommitAsync(ct);return result; });
}
