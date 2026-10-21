using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using PayAhead.Application.Payments;
using PayAhead.Application.Premium;
using PayAhead.Domain;

namespace PayAhead.Infrastructure;

public sealed class SqlEntitlementService(PayAheadDbContext db, MembershipOptions options, TimeProvider clock) : IEntitlementService
{
    public static string AccountId(Guid user) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes("PayAhead:"+user.ToString("D")))).ToLowerInvariant();
    public async Task<EntitlementResponse> GetAsync(Guid user,CancellationToken ct)
    {
        var now=clock.GetUtcNow().UtcDateTime;
        var rows=await db.UserSubscriptions.AsNoTracking().Where(x=>x.UserId==user&&x.IsStoreVerified).ToArrayAsync(ct);
        var granting=rows.Where(x=>MembershipRules.GrantsPremium(x,now,TimeSpan.FromHours(options.VerificationFreshnessHours)))
            .OrderByDescending(x=>x.CurrentPeriodEndUtc).FirstOrDefault();
        var latest=granting??rows.OrderByDescending(x=>x.LastVerifiedAtUtc).FirstOrDefault();
        var premium=granting is not null;
        var state=latest is null?MembershipState.Free:Enum.TryParse<MembershipState>(latest.Status,out var parsed)?parsed:MembershipState.Expired;
        if(!premium && state is MembershipState.Active or MembershipState.GracePeriod)state=MembershipState.Expired;
        return new(premium?"Premium":"Free",state,premium,premium?options.Premium:options.Free,
            latest?.CurrentPeriodEndUtc is {} end?DateTime.SpecifyKind(end,DateTimeKind.Utc):null,latest?.AutoRenew??false,user,AccountId(user),AdvancedSubscriptionTracking:premium);
    }
    public async Task LockUserAsync(Guid user,CancellationToken ct)
    {
        if(db.Database.CurrentTransaction is null)throw new InvalidOperationException("Entitlement enforcement requires a transaction.");
        if(await db.Users.FromSqlInterpolated($"SELECT * FROM Users WITH (UPDLOCK,HOLDLOCK) WHERE Id={user} AND IsActive=1").SingleOrDefaultAsync(ct) is null)throw new AuthenticationRequiredException();
    }
    public async Task EnsureIncreaseAsync(Guid user,LimitResource resource,Guid? plan,int increase,CancellationToken ct)
    {
        if(increase<=0)return;await LockUserAsync(user,ct);var limits=(await GetAsync(user,ct)).Limits;
        var zoneId=await db.UserSettings.Where(x=>x.UserId==user).Select(x=>x.TimeZoneId).SingleOrDefaultAsync(ct);
        var zone=PayAhead.Application.Notifications.NotificationPolicy.Zone(zoneId,out _);
        var today=DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(clock.GetUtcNow(),zone).DateTime);
        var count=resource switch
        {
            LimitResource.PaymentPlans=>await db.PaymentPlans.CountAsync(x=>x.UserId==user&&x.IsActive,ct),
            LimitResource.Reminders=>await db.ReminderRules.CountAsync(x=>x.UserId==user&&x.PaymentPlanId==plan&&x.IsEnabled&&!x.IsDeleted,ct),
            LimitResource.TrackedSubscriptions=>await db.TrackedSubscriptions.CountAsync(x=>x.UserId==user&&!x.IsDeleted
                &&(x.Status==SubscriptionStatus.Active||x.Status==SubscriptionStatus.Trial)&&(x.AccessEndsOn==null||x.AccessEndsOn>=today)
                &&(x.Status!=SubscriptionStatus.Trial||x.AutoRenew||x.TrialEndDate>=today),ct),
            _=>throw new ArgumentOutOfRangeException(nameof(resource))
        };
        var maximum=resource switch { LimitResource.PaymentPlans=>limits.ActivePaymentPlans,LimitResource.Reminders=>limits.ActiveRemindersPerPlan,_=>limits.ActiveTrackedSubscriptions };
        if(count+increase>maximum)throw new EntitlementLimitException();
    }
}
