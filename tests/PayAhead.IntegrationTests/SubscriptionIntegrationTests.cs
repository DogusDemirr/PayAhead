using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using PayAhead.Api;
using PayAhead.Application.Notifications;
using PayAhead.Application.Overview;
using PayAhead.Application.Payments;
using PayAhead.Application.Subscriptions;
using PayAhead.Domain;
using PayAhead.Domain.Recurrence;
using PayAhead.Infrastructure;
using Xunit;

namespace PayAhead.IntegrationTests;

public sealed class SubscriptionIntegrationTests(SqlServerFixture sql) : IClassFixture<SqlServerFixture>,IAsyncLifetime
{
    private string connection="";
    private readonly Guid user=Guid.NewGuid(),other=Guid.NewGuid();
    private readonly Clock clock=new();
    private static readonly DateTime Now=new(2026,11,21,9,0,0,DateTimeKind.Utc);
    private static readonly JsonSerializerOptions Json=new(JsonSerializerDefaults.Web) { Converters={new JsonStringEnumConverter()} };
    private WebApplicationFactory<Program> factory=null!;private HttpClient client=null!;
    private PayAheadDbContext Db()=>new(new DbContextOptionsBuilder<PayAheadDbContext>().UseSqlServer(connection).Options);
    private static SqlNotificationQueue Queue(PayAheadDbContext db)=>new(db,new PaymentRecurrenceCalculator());
    private sealed class Clock : TimeProvider { public DateTime Current=Now;public override DateTimeOffset GetUtcNow()=>new(DateTime.SpecifyKind(Current,DateTimeKind.Utc)); }
    public async Task InitializeAsync()
    {
        connection=new SqlConnectionStringBuilder(sql.ConnectionString) { InitialCatalog="PayAheadTest_"+Guid.NewGuid().ToString("N") }.ConnectionString;
        await using var db=Db();await db.Database.MigrateAsync();
        foreach(var id in new[]{user,other}) { db.Users.Add(new() { Id=id,Email=id+"@test.invalid",NormalizedEmail=(id+"@TEST.INVALID").ToUpperInvariant(),DisplayName="Test",PasswordHash="seed-only",CreatedAtUtc=Now,UpdatedAtUtc=Now });db.UserSettings.Add(new() { UserId=id,TimeZoneId="UTC",CreatedAtUtc=Now,UpdatedAtUtc=Now }); }
        await db.SaveChangesAsync();
        factory=new WebApplicationFactory<Program>().WithWebHostBuilder(b=>b.UseEnvironment("Development").UseRegressionPremium().UseSetting("ConnectionStrings:PayAhead",connection).UseSetting("DevelopmentIdentity:Enabled","false")
            .UseSetting("Notifications:Enabled","false").ConfigureTestServices(services=>services.AddSingleton<TimeProvider>(clock)));
        client=factory.CreateClient();Authenticate(user);
    }
    private void Authenticate(Guid id)=>client.DefaultRequestHeaders.Authorization=new AuthenticationHeaderValue("Bearer",new TokenService(factory.Services.GetRequiredService<JwtSettings>(),TimeProvider.System).CreateAccessToken(id).Token);
    public async Task DisposeAsync()
    { client.Dispose();await factory.DisposeAsync();await using var db=Db();Assert.StartsWith("PayAheadTest_",db.Database.GetDbConnection().Database);await db.Database.EnsureDeletedAsync(); }
    private static SubscriptionRequest Request(Action<JsonObject>? edit=null)
    {
        var json=JsonSerializer.SerializeToNode(new SubscriptionRequest { Name="Spotify",Description="Private",ProviderName="Spotify",Amount=99.1234m,CurrencyCode="TRY",BillingFrequency=PaymentFrequency.Monthly,BillingInterval=1,
            StartDate=new(2026,11,1),TrialStartDate=new(2026,11,1),TrialEndDate=new(2026,11,28),FirstBillingDate=new(2026,11,29),Status=SubscriptionStatus.Trial },Json)!.AsObject();
        edit?.Invoke(json);return json.Deserialize<SubscriptionRequest>(Json)!;
    }
    private Task<HttpResponseMessage> Post(string path,object request)=>client.PostAsync(path,JsonContent.Create(request,options:Json));
    private Task<HttpResponseMessage> Put(string path,object request)=>client.PutAsync(path,JsonContent.Create(request,options:Json));
    private static async Task<T> Read<T>(HttpResponseMessage response)
    { response.EnsureSuccessStatusCode();return (await response.Content.ReadFromJsonAsync<T>(Json))!; }
    private async Task<SubscriptionResponse> Create(Action<JsonObject>? edit=null)
    { var response=await Post("/api/subscriptions",Request(edit));Assert.Equal(HttpStatusCode.Created,response.StatusCode);Assert.NotNull(response.Headers.ETag);return await Read<SubscriptionResponse>(response); }
    [Fact] public async Task CRUD_creates_one_plan_and_preserves_history_on_soft_delete()
    {
        var value=await Create();Assert.Equal(new DateOnly(2026,11,29),value.NextBillingDate);Assert.Equal(7,value.TrialDaysRemaining);
        var fetched=await Read<SubscriptionResponse>(await client.GetAsync($"/api/subscriptions/{value.Id}"));Assert.Equal(value.RowVersion,fetched.RowVersion);
        var updatedResponse=await Put($"/api/subscriptions/{value.Id}",Request(j=>{j["Amount"]=123.4567m;j["RowVersion"]=value.RowVersion;}));Assert.Equal(HttpStatusCode.OK,updatedResponse.StatusCode);
        var updated=await Read<SubscriptionResponse>(updatedResponse);Assert.NotEqual(value.RowVersion,updated.RowVersion);
        await using var db=Db();Assert.Equal(1,await db.PaymentPlans.CountAsync());Assert.Equal(1,await db.TrackedSubscriptions.CountAsync());Assert.Equal(123.4567m,(await db.PaymentPlans.SingleAsync()).Amount);Assert.Empty(await db.UserSubscriptions.ToArrayAsync());
        db.PaymentOccurrences.Add(new() { Id=Guid.NewGuid(),PaymentPlanId=value.PaymentPlanId,DueDate=value.FirstBillingDate,Status=OccurrenceStatus.Paid,ActualAmount=120m,PaidAtUtc=Now,CreatedAtUtc=Now });await db.SaveChangesAsync();
        using var delete=new HttpRequestMessage(HttpMethod.Delete,$"/api/subscriptions/{value.Id}");delete.Headers.TryAddWithoutValidation("If-Match",'"'+updated.RowVersion+'"');Assert.Equal(HttpStatusCode.NoContent,(await client.SendAsync(delete)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,(await client.GetAsync($"/api/subscriptions/{value.Id}")).StatusCode);Assert.Single(await db.PaymentOccurrences.ToArrayAsync());Assert.False((await db.PaymentPlans.AsNoTracking().SingleAsync()).IsActive);
    }
    [Fact] public async Task Filters_pagination_and_summary_keep_currencies_separate()
    {
        await Create();await Create(j=>{j["Name"]="Music";j["ProviderName"]="Other";j["CurrencyCode"]="USD";j["Amount"]=120m;j["BillingFrequency"]="Yearly";});
        var list=await Read<PagedResult<SubscriptionResponse>>(await client.GetAsync("/api/subscriptions?status=Trial&providerName=spotify&currencyCode=try&search=spot&pageSize=1"));Assert.Equal(1,list.TotalCount);
        var summary=await Read<SubscriptionSummary>(await client.GetAsync("/api/subscriptions/summary"));Assert.Equal(2,summary.TotalSubscriptionCount);Assert.Equal(2,summary.TrialSubscriptionCount);Assert.Equal(2,summary.ExpiringTrialsCount);
        Assert.Equal(99.1234m,summary.MonthlyEquivalentTotalsByCurrency.Single(x=>x.CurrencyCode=="TRY").EstimatedAmount);Assert.Equal(10m,summary.MonthlyEquivalentTotalsByCurrency.Single(x=>x.CurrencyCode=="USD").EstimatedAmount);Assert.NotNull(summary.NextRenewal);
    }
    [Fact] public async Task Cancellation_stops_billing_and_trials_but_keeps_user_declared_access_and_history()
    {
        var value=await Create();await using var db=Db();await Queue(db).ScheduleBatchAsync(null,Now,CancellationToken.None);
        var cancelledResponse=await Post($"/api/subscriptions/{value.Id}/cancel",new CancelSubscriptionRequest(new(2026,11,30),value.RowVersion));Assert.Equal(HttpStatusCode.OK,cancelledResponse.StatusCode);
        var cancelled=await Read<SubscriptionResponse>(cancelledResponse);Assert.Equal(SubscriptionStatus.Cancelled,cancelled.Status);Assert.False(cancelled.AutoRenew);Assert.Null(cancelled.NextBillingDate);Assert.NotNull(cancelled.CancellationRequestedAtUtc);
        Assert.Equal(HttpStatusCode.Conflict,(await Put($"/api/subscriptions/{value.Id}",Request(j=>j["RowVersion"]=cancelled.RowVersion))).StatusCode);
        Assert.Empty(await Read<ExpiringTrialResponse[]>(await client.GetAsync("/api/subscriptions/trials/expiring")));
        Assert.All(await db.NotificationDeliveries.AsNoTracking().ToArrayAsync(),x=>Assert.Equal(DeliveryStatus.Cancelled,x.Status));Assert.False((await db.PaymentPlans.AsNoTracking().SingleAsync()).IsActive);
        Assert.Empty(await db.PaymentOccurrences.ToArrayAsync());clock.Current=Now.AddDays(10);Assert.Equal(SubscriptionStatus.Expired,(await Read<SubscriptionResponse>(await client.GetAsync($"/api/subscriptions/{value.Id}"))).Status);
    }
    [Fact] public async Task Trial_expiry_automatic_activation_and_nonrenewal_are_timezone_based()
    {
        var renewing=await Create();var noRenew=await Create(j=>j["AutoRenew"]=false);
        clock.Current=new(2026,11,28,23,30,0,DateTimeKind.Utc);
        await using var db=Db();await db.UserSettings.Where(x=>x.UserId==user).ExecuteUpdateAsync(s=>s.SetProperty(x=>x.TimeZoneId,"Europe/Istanbul"));
        var active=await Read<SubscriptionResponse>(await client.GetAsync($"/api/subscriptions/{renewing.Id}"));var expired=await Read<SubscriptionResponse>(await client.GetAsync($"/api/subscriptions/{noRenew.Id}"));
        Assert.Equal(SubscriptionStatus.Active,active.Status);Assert.Equal(SubscriptionStatus.Expired,expired.Status);Assert.Equal(0,expired.TrialDaysRemaining);Assert.Null(expired.NextBillingDate);
        Assert.Equal(HttpStatusCode.Conflict,(await Put($"/api/subscriptions/{noRenew.Id}",Request(j=>j["RowVersion"]=expired.RowVersion))).StatusCode);
        await db.UserSettings.Where(x=>x.UserId==user).ExecuteUpdateAsync(s=>s.SetProperty(x=>x.TimeZoneId,"bad-zone"));
        var fallback=await Read<SubscriptionResponse>(await client.GetAsync($"/api/subscriptions/{renewing.Id}"));Assert.True(fallback.Context.UsedTimeZoneFallback);Assert.Equal(SubscriptionStatus.Trial,fallback.Status);
    }
    [Fact] public async Task Dashboard_and_calendar_use_only_linked_plan_once()
    {
        var value=await Create();var calendar=await Read<CalendarResponse>(await client.GetAsync("/api/calendar?year=2026&month=11"));
        Assert.Single(calendar.Days);Assert.Single(calendar.Days[0].Payments);Assert.Equal(value.PaymentPlanId,calendar.Days[0].Payments[0].PaymentPlanId);
        var summary=await Read<DashboardSummaryResponse>(await client.GetAsync("/api/dashboard/summary"));Assert.Equal(1,summary.ActivePaymentPlanCount);Assert.Equal(1,summary.PendingPaymentCount);Assert.Equal(99.1234m,summary.UpcomingTotalsByCurrency.Single().Amount);
    }
    [Fact] public async Task Managed_plan_cannot_be_modified_or_deleted_through_generic_payment_API()
    {
        var value=await Create();var plan=await Read<PaymentResponse>(await client.GetAsync($"/api/payments/{value.PaymentPlanId}"));
        Assert.Equal(HttpStatusCode.Conflict,(await Put($"/api/payments/{plan.Id}",new { name="Changed",amount=1,currencyCode="TRY",category="Other",frequency="Monthly",interval=1,startDate=plan.StartDate,rowVersion=plan.RowVersion })).StatusCode);
        using var delete=new HttpRequestMessage(HttpMethod.Delete,$"/api/payments/{plan.Id}");delete.Headers.TryAddWithoutValidation("If-Match",'"'+plan.RowVersion+'"');Assert.Equal(HttpStatusCode.Conflict,(await client.SendAsync(delete)).StatusCode);
    }
    [Fact] public async Task Cross_user_and_anonymous_access_to_all_subscription_routes_is_blocked()
    {
        var value=await Create();Authenticate(other);
        Assert.Equal(HttpStatusCode.NotFound,(await client.GetAsync($"/api/subscriptions/{value.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,(await Put($"/api/subscriptions/{value.Id}",Request(j=>j["RowVersion"]=value.RowVersion))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,(await Post($"/api/subscriptions/{value.Id}/cancel",new CancelSubscriptionRequest(new(2026,11,30),value.RowVersion))).StatusCode);
        using var delete=new HttpRequestMessage(HttpMethod.Delete,$"/api/subscriptions/{value.Id}");delete.Headers.TryAddWithoutValidation("If-Match",'"'+value.RowVersion+'"');Assert.Equal(HttpStatusCode.NotFound,(await client.SendAsync(delete)).StatusCode);
        Assert.Equal(0,(await Read<SubscriptionSummary>(await client.GetAsync("/api/subscriptions/summary"))).TotalSubscriptionCount);Assert.Empty(await Read<ExpiringTrialResponse[]>(await client.GetAsync("/api/subscriptions/trials/expiring")));
        client.DefaultRequestHeaders.Authorization=null;foreach(var path in new[]{"/api/subscriptions","/api/subscriptions/summary","/api/subscriptions/trials/expiring"})Assert.Equal(HttpStatusCode.Unauthorized,(await client.GetAsync(path)).StatusCode);
    }
    [Theory] [InlineData("/api/subscriptions?pageNumber=0")] [InlineData("/api/subscriptions?pageSize=101")] [InlineData("/api/subscriptions?status=Invalid")]
    [InlineData("/api/subscriptions/trials/expiring?withinDays=-1")] [InlineData("/api/subscriptions/trials/expiring?withinDays=366")]
    public async Task Invalid_queries_return_ProblemDetails(string path)
    { var result=await client.GetAsync(path);Assert.Equal(HttpStatusCode.BadRequest,result.StatusCode);Assert.Equal("application/problem+json",result.Content.Headers.ContentType?.MediaType); }
    [Fact] public async Task Invalid_body_and_cannot_supply_UserId_or_link_existing_plan()
    {
        Assert.Equal(HttpStatusCode.BadRequest,(await Post("/api/subscriptions",Request(j=>j["FirstBillingDate"]="2026-11-28"))).StatusCode);
        var body=JsonSerializer.SerializeToNode(Request(),Json)!.AsObject();body["userId"]=other.ToString();Assert.Equal(HttpStatusCode.BadRequest,(await Post("/api/subscriptions",body)).StatusCode);body.Remove("userId");body["paymentPlanId"]=Guid.NewGuid().ToString();Assert.Equal(HttpStatusCode.BadRequest,(await Post("/api/subscriptions",body)).StatusCode);
        await using var db=Db();Assert.Empty(await db.TrackedSubscriptions.ToArrayAsync());Assert.Empty(await db.PaymentPlans.ToArrayAsync());
    }
    [Fact] public async Task Concurrent_update_and_cancel_only_one_commits()
    {
        var value=await Create();var responses=await Task.WhenAll(Put($"/api/subscriptions/{value.Id}",Request(j=>{j["Name"]="Updated";j["RowVersion"]=value.RowVersion;})),Post($"/api/subscriptions/{value.Id}/cancel",new CancelSubscriptionRequest(new(2026,11,30),value.RowVersion)));
        Assert.Single(responses,x=>x.StatusCode==HttpStatusCode.OK);Assert.Single(responses,x=>x.StatusCode==HttpStatusCode.Conflict);
        await using var db=Db();Assert.Single(await db.TrackedSubscriptions.ToArrayAsync());Assert.Single(await db.PaymentPlans.ToArrayAsync());
    }
    [Fact] public async Task Concurrent_update_and_delete_only_one_commits_and_history_is_safe()
    {
        var value=await Create();using var delete=new HttpRequestMessage(HttpMethod.Delete,$"/api/subscriptions/{value.Id}");delete.Headers.TryAddWithoutValidation("If-Match",'"'+value.RowVersion+'"');
        var responses=await Task.WhenAll(Put($"/api/subscriptions/{value.Id}",Request(j=>{j["Amount"]=10;j["RowVersion"]=value.RowVersion;})),client.SendAsync(delete));
        Assert.Single(responses,x=>x.IsSuccessStatusCode);Assert.Single(responses,x=>x.StatusCode is HttpStatusCode.Conflict or HttpStatusCode.NotFound);
        await using var db=Db();Assert.Single(await db.PaymentPlans.ToArrayAsync());
    }
    [Fact] public async Task Concurrent_updates_only_one_rowversion_wins()
    {
        var value=await Create();var responses=await Task.WhenAll(Put($"/api/subscriptions/{value.Id}",Request(j=>{j["Amount"]=1;j["RowVersion"]=value.RowVersion;})),Put($"/api/subscriptions/{value.Id}",Request(j=>{j["Amount"]=2;j["RowVersion"]=value.RowVersion;})));
        Assert.Single(responses,x=>x.StatusCode==HttpStatusCode.OK);Assert.Single(responses,x=>x.StatusCode==HttpStatusCode.Conflict);
    }
    [Fact] public async Task Trial_date_update_cancels_old_keys_and_schedules_new_event()
    {
        var value=await Create();await using var db=Db();db.DeviceTokens.Add(new() { Id=Guid.NewGuid(),UserId=user,Platform="ios",PushToken="ExpoPushToken[trial0123456789abc]",LastSeenAtUtc=Now });await db.SaveChangesAsync();await Queue(db).ScheduleBatchAsync(null,Now,CancellationToken.None);
        Assert.Equal(HttpStatusCode.OK,(await Put($"/api/subscriptions/{value.Id}",Request(j=>{j["TrialEndDate"]="2026-11-29";j["FirstBillingDate"]="2026-11-30";j["RowVersion"]=value.RowVersion;}))).StatusCode);
        await Queue(db).ScheduleBatchAsync(null,Now,CancellationToken.None);
        var rows=await db.NotificationDeliveries.AsNoTracking().ToArrayAsync();Assert.Equal(8,rows.Length);
        Assert.All(rows.Where(x=>x.DueDate==new DateOnly(2026,11,28)),x=>Assert.Equal(DeliveryStatus.Cancelled,x.Status));
        Assert.All(rows.Where(x=>x.Status==DeliveryStatus.Pending),x=>Assert.Equal(new DateOnly(2026,11,29),x.DueDate));
    }
    private sealed class FailSave : Microsoft.EntityFrameworkCore.Diagnostics.SaveChangesInterceptor
    {
        public override ValueTask<Microsoft.EntityFrameworkCore.Diagnostics.InterceptionResult<int>> SavingChangesAsync(
            Microsoft.EntityFrameworkCore.Diagnostics.DbContextEventData data,Microsoft.EntityFrameworkCore.Diagnostics.InterceptionResult<int> result,CancellationToken ct=default)
            =>throw new InvalidOperationException("Injected subscription save failure");
    }
    [Fact] public async Task Failed_subscription_update_rolls_back_plan_rules_and_queue_cancellation()
    {
        var value=await Create();await using(var db=Db()) { db.DeviceTokens.Add(new() { Id=Guid.NewGuid(),UserId=user,Platform="ios",PushToken="ExpoPushToken[trial0123456789abc]",LastSeenAtUtc=Now });await db.SaveChangesAsync();await Queue(db).ScheduleBatchAsync(null,Now,CancellationToken.None); }
        await using(var failing=new PayAheadDbContext(new DbContextOptionsBuilder<PayAheadDbContext>().UseSqlServer(connection).AddInterceptors(new FailSave()).Options))
            await Assert.ThrowsAsync<InvalidOperationException>(()=>new SubscriptionStore(failing).UpdateAsync(user,value.Id,Request(j=>{j["Amount"]=123m;j["EnableTrialReminders"]=false;j["RowVersion"]=value.RowVersion;}),Now,new(2026,11,21),CancellationToken.None));
        await using var check=Db();Assert.Equal(99.1234m,(await check.PaymentPlans.SingleAsync()).Amount);Assert.Equal(value.RowVersion,Convert.ToBase64String((await check.TrackedSubscriptions.SingleAsync()).RowVersion));
        Assert.All(await check.ReminderRules.ToArrayAsync(),x=>Assert.True(x.IsEnabled));Assert.All(await check.NotificationDeliveries.ToArrayAsync(),x=>Assert.Equal(DeliveryStatus.Pending,x.Status));
    }
    [Fact] public async Task Active_subscription_keeps_month_end_anchor_in_linked_plan()
    {
        var value=await Create(j=>{j["Status"]="Active";j["StartDate"]="2026-01-01";j["FirstBillingDate"]="2026-01-31";j["TrialStartDate"]=null;j["TrialEndDate"]=null;j["AnchorDay"]=31;});
        Assert.Equal(new DateOnly(2026,11,30),value.NextBillingDate);Assert.Null(value.TrialDaysRemaining);Assert.Equal(SubscriptionStatus.Active,value.Status);
    }
    [Fact] public async Task SQL_link_uniqueness_and_ownership_prevent_duplicate_or_foreign_plan()
    {
        var value=await Create();await using var db=Db();
        db.TrackedSubscriptions.Add(new() { Id=Guid.NewGuid(),UserId=user,PaymentPlanId=value.PaymentPlanId,ProviderName="duplicate",Status=SubscriptionStatus.Active,StartDate=new(2026,11,1),CreatedAtUtc=Now,UpdatedAtUtc=Now });
        await Assert.ThrowsAsync<DbUpdateException>(()=>db.SaveChangesAsync());db.ChangeTracker.Clear();
        var foreignPlan=new PaymentPlan { Id=Guid.NewGuid(),UserId=other,Name="Foreign",Amount=1,StartDate=new(2026,11,1),CreatedAtUtc=Now,UpdatedAtUtc=Now };db.PaymentPlans.Add(foreignPlan);await db.SaveChangesAsync();
        db.TrackedSubscriptions.Add(new() { Id=Guid.NewGuid(),UserId=user,PaymentPlanId=foreignPlan.Id,ProviderName="foreign",Status=SubscriptionStatus.Active,StartDate=new(2026,11,1),CreatedAtUtc=Now,UpdatedAtUtc=Now });await Assert.ThrowsAsync<DbUpdateException>(()=>db.SaveChangesAsync());
    }
    private sealed class FakePushNotificationSender : IPushNotificationSender
    {
        public int Sends;public Task<PushResult> SendAsync(PushMessage message,CancellationToken ct) { Sends++;return Task.FromResult(new PushResult(PushResultKind.TicketAccepted,"test-only-ticket")); }
        public Task<PushResult> GetReceiptAsync(string ticket,CancellationToken ct)=>Task.FromResult(new PushResult(PushResultKind.ReceiptAccepted));
    }
    [Fact] public async Task Trial_reminder_uses_existing_worker_and_keys_do_not_collide_with_payment_rules()
    {
        var value=await Create();await using var db=Db();Assert.Equal(4,await db.ReminderRules.CountAsync(x=>x.Target==ReminderTarget.SubscriptionTrial));
        Assert.Equal(HttpStatusCode.Created,(await Post($"/api/payments/{value.PaymentPlanId}/reminders",new ReminderRequest(7))).StatusCode);
        db.DeviceTokens.Add(new() { Id=Guid.NewGuid(),UserId=user,Platform="ios",PushToken="ExpoPushToken[trial0123456789abc]",LastSeenAtUtc=Now });await db.SaveChangesAsync();
        await Queue(db).ScheduleBatchAsync(null,Now,CancellationToken.None);await Queue(db).ScheduleBatchAsync(null,Now,CancellationToken.None);
        Assert.True(await db.NotificationDeliveries.CountAsync()>=5);var fake=new FakePushNotificationSender();Assert.True(await new NotificationProcessor(Queue(db),fake,clock).ProcessOneAsync(CancellationToken.None));Assert.Equal(1,fake.Sends);
        var accepted=await db.NotificationDeliveries.AsNoTracking().SingleAsync(x=>x.Status==DeliveryStatus.AwaitingReceipt);var rule=await db.ReminderRules.FindAsync(accepted.ReminderRuleId);Assert.Equal(ReminderTarget.SubscriptionTrial,rule!.Target);Assert.Equal(value.TrialEndDate,accepted.DueDate);
    }
    [Theory] [InlineData("cancel")] [InlineData("delete")] [InlineData("disable")]
    public async Task Trial_delivery_is_suppressed_after_user_changes(string action)
    {
        var value=await Create(j=>j["AutoRenew"]=false);await using var db=Db();db.DeviceTokens.Add(new() { Id=Guid.NewGuid(),UserId=user,Platform="ios",PushToken="ExpoPushToken[trial0123456789abc]",LastSeenAtUtc=Now });await db.SaveChangesAsync();await Queue(db).ScheduleBatchAsync(null,Now,CancellationToken.None);
        Assert.Equal(4,await db.NotificationDeliveries.CountAsync());
        if(action=="cancel")await Post($"/api/subscriptions/{value.Id}/cancel",new CancelSubscriptionRequest(new(2026,11,30),value.RowVersion));
        else if(action=="delete") { using var delete=new HttpRequestMessage(HttpMethod.Delete,$"/api/subscriptions/{value.Id}");delete.Headers.TryAddWithoutValidation("If-Match",'"'+value.RowVersion+'"');await client.SendAsync(delete); }
        else await Put($"/api/subscriptions/{value.Id}",Request(j=>{j["RowVersion"]=value.RowVersion;j["EnableTrialReminders"]=false;}));
        await Queue(db).ScheduleBatchAsync(null,Now,CancellationToken.None);Assert.Null(await Queue(db).ClaimAsync(Guid.NewGuid(),Now,CancellationToken.None));
    }
    [Fact] public async Task Trial_delivery_is_revalidated_after_expiry_or_disabled_settings()
    {
        var value=await Create();await using var db=Db();db.DeviceTokens.Add(new() { Id=Guid.NewGuid(),UserId=user,Platform="ios",PushToken="ExpoPushToken[trial0123456789abc]",LastSeenAtUtc=Now });await db.SaveChangesAsync();await Queue(db).ScheduleBatchAsync(null,Now,CancellationToken.None);
        var claimed=(await Queue(db).ClaimAsync(Guid.NewGuid(),Now,CancellationToken.None))!;
        await db.UserSettings.Where(x=>x.UserId==user).ExecuteUpdateAsync(s=>s.SetProperty(x=>x.NotificationsEnabled,false));Assert.Null(await Queue(db).RevalidateAsync(claimed,Now,CancellationToken.None));
        await db.UserSettings.Where(x=>x.UserId==user).ExecuteUpdateAsync(s=>s.SetProperty(x=>x.NotificationsEnabled,true));await Queue(db).ScheduleBatchAsync(null,Now,CancellationToken.None);
        var second=(await Queue(db).ClaimAsync(Guid.NewGuid(),Now,CancellationToken.None))!;Assert.Null(await Queue(db).RevalidateAsync(second,Now.AddDays(8),CancellationToken.None));
    }
    [Fact] public async Task Migration_from_phase7_preserves_existing_premium_row_and_payment()
    {
        var legacyConnection=new SqlConnectionStringBuilder(connection) { InitialCatalog="PayAheadTest_"+Guid.NewGuid().ToString("N") }.ConnectionString;
        await using var db=new PayAheadDbContext(new DbContextOptionsBuilder<PayAheadDbContext>().UseSqlServer(legacyConnection).Options);
        try
        {
            await db.GetService<IMigrator>().MigrateAsync("20261021124526_DurableNotifications");
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO Users (Id,Email,NormalizedEmail,DisplayName,PasswordHash,IsActive,CreatedAtUtc,UpdatedAtUtc,FailedLoginCount) VALUES ({user},'legacy@test.invalid','LEGACY@TEST.INVALID','Legacy','hash',1,{Now},{Now},0)");
            var premium=Guid.NewGuid();await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO UserSubscriptions (Id,UserId,Provider,ProductId,ExternalSubscriptionId,Status) VALUES ({premium},{user},'store','premium','legacy-id','Active')");
            var plan=Guid.NewGuid();await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO PaymentPlans (Id,UserId,Name,Amount,CurrencyCode,Category,Frequency,Interval,StartDate,IsActive,CreatedAtUtc,UpdatedAtUtc) VALUES ({plan},{user},'Existing',1,'TRY','Other',0,1,{new DateOnly(2026,11,21)},1,{Now},{Now})");
            await db.Database.MigrateAsync();Assert.Equal(premium,(await db.UserSubscriptions.SingleAsync()).Id);Assert.Equal(plan,(await db.PaymentPlans.SingleAsync()).Id);Assert.Empty(await db.TrackedSubscriptions.ToArrayAsync());Assert.Empty(await db.Database.GetPendingMigrationsAsync());
        }
        finally { Assert.StartsWith("PayAheadTest_",db.Database.GetDbConnection().Database);await db.Database.EnsureDeletedAsync(); }
    }
}
