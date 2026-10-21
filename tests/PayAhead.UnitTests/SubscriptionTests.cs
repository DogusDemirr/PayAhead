using System.Text.Json;
using System.Text.Json.Nodes;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using PayAhead.Application;
using PayAhead.Application.Payments;
using PayAhead.Application.Subscriptions;
using PayAhead.Domain;
using PayAhead.Domain.Recurrence;
using PayAhead.Infrastructure;
using Xunit;

namespace PayAhead.UnitTests;

public sealed class SubscriptionTests
{
    private static readonly DateOnly Today=new(2026,11,21);
    private static readonly Guid User=Guid.NewGuid();
    private static SubscriptionRequest Request(Action<JsonObject>? edit=null)
    {
        var json=JsonSerializer.SerializeToNode(new SubscriptionRequest { Name="Spotify",ProviderName="Spotify",Amount=99.99m,CurrencyCode="TRY",BillingFrequency=PaymentFrequency.Monthly,BillingInterval=1,
            StartDate=new(2026,11,1),FirstBillingDate=new(2026,12,1),TrialStartDate=new(2026,11,1),TrialEndDate=new(2026,11,30),Status=SubscriptionStatus.Trial })!.AsObject();
        edit?.Invoke(json);return json.Deserialize<SubscriptionRequest>()!;
    }
    [Theory]
    [InlineData("Name"," ")] [InlineData("ProviderName","")] [InlineData("CurrencyCode","INVALID")]
    [InlineData("StartDate","0001-01-01")] [InlineData("TrialEndDate","2026-10-01")] [InlineData("FirstBillingDate","2026-11-30")]
    [InlineData("TrialStartDate","2026-10-01")] [InlineData("AccessEndsOn","2026-11-01")]
    public void Invalid_text_and_dates_are_rejected(string property,string value)=>Assert.False(new SubscriptionValidator().Validate(Request(j=>j[property]=value)).IsValid);
    [Theory] [InlineData("Amount",0)] [InlineData("Amount",-1)] [InlineData("BillingInterval",0)] [InlineData("BillingInterval",-1)] [InlineData("BillingFrequency",0)] [InlineData("BillingFrequency",99)] [InlineData("Status",2)] [InlineData("Status",3)] [InlineData("AnchorDay",32)]
    public void Invalid_numbers_and_direct_terminal_status_are_rejected(string property,int value)=>Assert.False(new SubscriptionValidator().Validate(Request(j=>j[property]=value)).IsValid);
    [Fact] public void Trial_requires_both_dates_and_paid_amount_precision()
    {
        Assert.True(new SubscriptionValidator().Validate(Request()).IsValid);
        Assert.False(new SubscriptionValidator().Validate(Request(j=>j["TrialStartDate"]=null)).IsValid);
        Assert.False(new SubscriptionValidator().Validate(Request(j=>j["Amount"]=1.00001m)).IsValid);
        Assert.False(new SubscriptionValidator().Validate(Request(j=>j["Name"]=new string('x',151))).IsValid);
    }
    [Theory] [InlineData(1,20,true)] [InlineData(0,20,false)] [InlineData(1,101,false)] [InlineData(1,0,false)]
    public void Pagination_is_bounded(int page,int size,bool valid)=>Assert.Equal(valid,new SubscriptionQueryValidator().Validate(new SubscriptionQuery { PageNumber=page,PageSize=size }).IsValid);
    private static TrackedSubscription Trial(bool auto=true)=>new() { Id=Guid.NewGuid(),UserId=User,StartDate=new(2026,11,1),TrialStartDate=new(2026,11,1),TrialEndDate=new(2026,11,30),Status=SubscriptionStatus.Trial,AutoRenew=auto };
    [Fact] public void Trial_auto_activates_after_end_without_implying_payment()
    {
        var trial=Trial();Assert.Equal(SubscriptionStatus.Trial,trial.EffectiveStatus(new(2026,11,30)));Assert.Equal(SubscriptionStatus.Active,trial.EffectiveStatus(new(2026,12,1)));
        Assert.Equal(SubscriptionStatus.Trial,trial.Status);Assert.Null(trial.CancelledAtUtc);
    }
    [Fact] public void Nonrenewing_trial_expires_and_remaining_days_never_go_negative()
    {
        var trial=Trial(false);Assert.Equal(9,trial.TrialDaysRemaining(Today));Assert.Equal(0,trial.TrialDaysRemaining(new(2026,12,5)));Assert.Equal(SubscriptionStatus.Expired,trial.EffectiveStatus(new(2026,12,1)));
    }
    [Fact] public void Cancellation_preserves_access_until_inclusive_end_and_is_user_declaration()
    {
        var trial=Trial();var now=new DateTime(2026,11,21,9,0,0,DateTimeKind.Utc);trial.Cancel(new(2026,11,30),Today,now);
        Assert.False(trial.AutoRenew);Assert.Equal(now,trial.CancellationRequestedAtUtc);Assert.Equal(now,trial.CancelledAtUtc);
        Assert.Equal(SubscriptionStatus.Cancelled,trial.EffectiveStatus(new(2026,11,30)));Assert.Equal(SubscriptionStatus.Expired,trial.EffectiveStatus(new(2026,12,1)));
        Assert.Throws<DomainRuleException>(()=>trial.Transition(SubscriptionStatus.Active,Today));
    }
    [Fact] public void Trial_can_activate_but_active_cannot_return_to_trial()
    {
        var trial=Trial();trial.Transition(SubscriptionStatus.Active,Today);Assert.Equal(SubscriptionStatus.Active,trial.Status);
        Assert.Throws<DomainRuleException>(()=>trial.Transition(SubscriptionStatus.Trial,Today));
        Assert.Throws<DomainRuleException>(()=>Trial().Transition(SubscriptionStatus.Expired,Today));
    }
    [Fact] public void Expired_active_cannot_be_edited()
    { var trial=Trial();trial.Status=SubscriptionStatus.Active;trial.AccessEndsOn=Today.AddDays(-1);Assert.Equal(SubscriptionStatus.Expired,trial.EffectiveStatus(Today));Assert.Throws<DomainRuleException>(()=>trial.EnsureMutable(Today)); }
    [Theory] [InlineData(PaymentFrequency.Monthly,1,120,120)] [InlineData(PaymentFrequency.Monthly,3,120,40)] [InlineData(PaymentFrequency.Yearly,1,120,10)] [InlineData(PaymentFrequency.Weekly,1,12,52)] [InlineData(PaymentFrequency.Daily,1,12,365)]
    public void Estimated_monthly_equivalents_use_documented_annualization(PaymentFrequency frequency,int interval,decimal amount,decimal expected)=>Assert.Equal(expected,SubscriptionService.MonthlyEquivalent(amount,frequency,interval));
    [Theory] [InlineData(PaymentFrequency.Monthly,"2026-01-31","2026-02-01","2026-02-28")] [InlineData(PaymentFrequency.Monthly,"2026-01-31","2026-03-01","2026-03-31")]
    [InlineData(PaymentFrequency.Yearly,"2024-02-29","2025-01-01","2025-02-28")] [InlineData(PaymentFrequency.Yearly,"2024-02-29","2028-01-01","2028-02-29")]
    public void Billing_uses_existing_month_end_and_leap_recurrence(PaymentFrequency frequency,string first,string reference,string expected)
    { var plan=new PaymentPlan { StartDate=DateOnly.Parse(first),Frequency=frequency };Assert.Equal(DateOnly.Parse(expected),new PaymentRecurrenceCalculator().GetNextOccurrence(plan,DateOnly.Parse(reference))); }
    [Fact] public void Trial_reminder_eligibility_uses_trial_end_and_cancellation_not_billing_due()
    {
        var trial=Trial(false);Assert.True(trial.TrialReminderEligible(Today,new(2026,11,30)));Assert.False(trial.TrialReminderEligible(Today,new(2026,12,1)));
        trial.EnableTrialReminders=false;Assert.False(trial.TrialReminderEligible(Today,new(2026,11,30)));trial.EnableTrialReminders=true;
        trial.Cancel(new(2026,11,30),Today,new(2026,11,21,9,0,0,DateTimeKind.Utc));Assert.False(trial.TrialReminderEligible(Today,new(2026,11,30)));
        Assert.False(Trial().TrialReminderEligible(new(2026,12,1),new(2026,11,30)));
    }
    [Fact] public void SQL_model_has_unique_owned_plan_link_and_subscription_concurrency()
    {
        using var db=new PayAheadDbContext(new DbContextOptionsBuilder<PayAheadDbContext>().UseSqlServer("Server=localhost;Database=ModelOnly;Integrated Security=True").Options);
        var type=db.Model.FindEntityType(typeof(TrackedSubscription))!;Assert.True(type.FindProperty("RowVersion")!.IsConcurrencyToken);
        Assert.Contains(type.GetIndexes(),x=>x.IsUnique&&x.Properties.Select(p=>p.Name).SequenceEqual(["PaymentPlanId"]));
        Assert.Contains(type.GetForeignKeys(),x=>x.Properties.Select(p=>p.Name).SequenceEqual(["PaymentPlanId","UserId"]));
    }
    private sealed class Clock : TimeProvider { public override DateTimeOffset GetUtcNow()=>new(2026,11,21,23,30,0,TimeSpan.Zero); }
    private sealed class Current(Guid id) : ICurrentUser { public Guid? UserId=>id; }
    private sealed class Store : ISubscriptionStore
    {
        public List<SubscriptionRecord> Rows=[];public string? Zone="UTC";public Guid LastUser;
        public Task<string?> TimeZoneAsync(Guid user,CancellationToken ct)=>Task.FromResult(Zone);
        public Task<IReadOnlyList<SubscriptionRecord>> LoadAsync(Guid user,CancellationToken ct) { LastUser=user;return Task.FromResult<IReadOnlyList<SubscriptionRecord>>(Rows.Where(x=>x.Subscription.UserId==user).ToArray()); }
        public Task<SubscriptionRecord?> FindAsync(Guid user,Guid id,CancellationToken ct)=>Task.FromResult(Rows.SingleOrDefault(x=>x.Subscription.UserId==user&&x.Subscription.Id==id));
        public Task<SubscriptionRecord> CreateAsync(Guid user,SubscriptionRequest r,DateTime now,DateOnly today,CancellationToken ct)=>throw new NotSupportedException();
        public Task<SubscriptionRecord> UpdateAsync(Guid user,Guid id,SubscriptionRequest r,DateTime now,DateOnly today,CancellationToken ct)=>throw new NotSupportedException();
        public Task<SubscriptionRecord> CancelAsync(Guid user,Guid id,CancelSubscriptionRequest r,DateTime now,DateOnly today,CancellationToken ct)=>throw new NotSupportedException();
        public Task DeleteAsync(Guid user,Guid id,byte[] version,DateTime now,CancellationToken ct)=>throw new NotSupportedException();
    }
    private static SubscriptionService Service(Store store,Guid? user=null)=>new(store,new Current(user??User),new Clock(),new PaymentRecurrenceCalculator(),new SubscriptionValidator(),new SubscriptionQueryValidator());
    private static SubscriptionRecord Record(string currency,decimal amount,PaymentFrequency frequency)
    {
        var s=Trial();s.Status=SubscriptionStatus.Active;s.TrialStartDate=null;s.TrialEndDate=null;
        var p=new PaymentPlan { Id=Guid.NewGuid(),UserId=User,Name="Test",Amount=amount,CurrencyCode=currency,Frequency=frequency,StartDate=Today,RowVersion=new byte[8] };s.PaymentPlanId=p.Id;s.RowVersion=new byte[8];return new(s,p);
    }
    [Fact] public async Task Summary_groups_currencies_and_rounds_group_total_to_even_four_places()
    {
        var store=new Store { Rows=[Record("TRY",120,PaymentFrequency.Yearly),Record("USD",12,PaymentFrequency.Weekly),Record("TRY",1,PaymentFrequency.Yearly)] };
        var summary=await Service(store).SummaryAsync(CancellationToken.None);Assert.Equal(3,summary.TotalSubscriptionCount);Assert.Equal(10.0833m,summary.MonthlyEquivalentTotalsByCurrency.Single(x=>x.CurrencyCode=="TRY").EstimatedAmount);Assert.Equal(52m,summary.MonthlyEquivalentTotalsByCurrency.Single(x=>x.CurrencyCode=="USD").EstimatedAmount);
        Assert.Equal(User,store.LastUser);Assert.Equal(0,(await Service(store,Guid.NewGuid()).SummaryAsync(CancellationToken.None)).TotalSubscriptionCount);
    }
    [Fact] public async Task User_timezone_and_invalid_zone_fallback_change_local_today()
    {
        var store=new Store { Zone="Europe/Istanbul" };Assert.Equal(new DateOnly(2026,11,22),(await Service(store).SummaryAsync(CancellationToken.None)).Context.Today);
        store.Zone="invalid";var summary=await Service(store).SummaryAsync(CancellationToken.None);Assert.Equal(Today,summary.Context.Today);Assert.True(summary.Context.UsedTimeZoneFallback);
    }
    [Fact] public async Task Query_and_rowversion_validation_do_not_reach_mutating_store()
    {
        var service=Service(new());await Assert.ThrowsAsync<ValidationException>(()=>service.TrialsAsync(new() { WithinDays=-1 },CancellationToken.None));
        await Assert.ThrowsAsync<ValidationException>(()=>service.DeleteAsync(Guid.NewGuid(),null,CancellationToken.None));
        await Assert.ThrowsAsync<ValidationException>(()=>service.UpdateAsync(Guid.NewGuid(),Request(),CancellationToken.None));
    }
}
