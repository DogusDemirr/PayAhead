using System.Net;
using System.Text;
using Microsoft.Extensions.Configuration;
using PayAhead.Application.Notifications;
using PayAhead.Domain;
using PayAhead.Domain.Recurrence;
using PayAhead.Infrastructure;
using Xunit;

namespace PayAhead.UnitTests;

public sealed class NotificationTests
{
    [Theory] [InlineData(-1)] [InlineData(366)] [InlineData(null)]
    public void Reminder_days_are_bounded(int? days) => Assert.False(new ReminderValidator().Validate(new ReminderRequest(days)).IsValid);
    [Theory] [InlineData(0)] [InlineData(1)] [InlineData(3)] [InlineData(7)] [InlineData(365)]
    public void Reminder_supports_same_day_and_custom_positive_leads(int days) => Assert.True(new ReminderValidator().Validate(new ReminderRequest(days)).IsValid);
    [Theory] [InlineData("24:00")] [InlineData("9:00")] [InlineData("09:00:01")] [InlineData("")] [InlineData(null)]
    public void Reminder_time_is_exact_HH_mm(string? time) => Assert.False(new ReminderValidator().Validate(new ReminderRequest(1, time)).IsValid);
    [Theory] [InlineData(0, "2026-11-24T06:00:00Z")] [InlineData(3, "2026-11-21T06:00:00Z")] [InlineData(7, "2026-11-17T06:00:00Z")]
    public void Lead_and_timezone_are_converted_without_server_local_clock(int lead, string expected)
        => Assert.Equal(DateTime.Parse(expected).ToUniversalTime(), NotificationPolicy.ScheduledAt(new(2026,11,24), lead, new(9,0), TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul")));
    [Fact] public void DST_gap_advances_to_first_valid_minute()
        => Assert.Equal(new DateTime(2026,3,8,7,0,0,DateTimeKind.Utc), NotificationPolicy.ScheduledAt(new(2026,3,8), 0, new(2,30), TimeZoneInfo.FindSystemTimeZoneById("America/New_York")));
    [Fact] public void DST_fold_uses_later_UTC_instant()
        => Assert.Equal(new DateTime(2026,11,1,6,30,0,DateTimeKind.Utc), NotificationPolicy.ScheduledAt(new(2026,11,1), 0, new(1,30), TimeZoneInfo.FindSystemTimeZoneById("America/New_York")));
    [Theory] [InlineData(null)] [InlineData("invalid/timezone")]
    public void Missing_invalid_timezone_falls_back_to_UTC(string? id)
    { Assert.Equal(TimeZoneInfo.Utc, NotificationPolicy.Zone(id, out var fallback)); Assert.True(fallback); }
    [Fact] public void Minimum_date_subtraction_is_guarded() => Assert.Null(NotificationPolicy.ScheduledAt(DateOnly.MinValue, 1, new(9,0), TimeZoneInfo.Utc));
    [Theory]
    [InlineData(null,true,true,true,true,true,false,true)]
    [InlineData(OccurrenceStatus.Pending,true,true,true,true,true,false,true)]
    [InlineData(OccurrenceStatus.Paid,true,true,true,true,true,false,false)]
    [InlineData(OccurrenceStatus.Skipped,true,true,true,true,true,false,false)]
    [InlineData(null,false,true,true,true,true,false,false)]
    [InlineData(null,true,false,true,true,true,false,false)]
    [InlineData(null,true,true,false,true,true,false,false)]
    [InlineData(null,true,true,true,false,true,false,false)]
    [InlineData(null,true,true,true,true,false,false,false)]
    [InlineData(null,true,true,true,true,true,true,false)]
    public void Eligibility_checks_all_current_state(OccurrenceStatus? status, bool planActive, bool enabled, bool userActive, bool notifications, bool device, bool deleted, bool expected)
    {
        var plan = new PaymentPlan { StartDate = new(2026,11,24), IsActive = planActive };
        var rule = new ReminderRule { IsEnabled = enabled, IsDeleted = deleted };
        Assert.Equal(expected, NotificationPolicy.Eligible(plan, rule, userActive, notifications, device, plan.StartDate, status, new PaymentRecurrenceCalculator()));
    }
    [Fact] public void EndDate_and_schedule_edits_suppress_stale_due_dates()
    {
        var plan = new PaymentPlan { StartDate = new(2026,1,1), EndDate = new(2026,1,2), Frequency = PaymentFrequency.Daily };
        Assert.False(NotificationPolicy.Eligible(plan, new(), true,true,true,new(2026,1,3),null,new PaymentRecurrenceCalculator()));
    }
    [Theory] [InlineData("ios", "ExpoPushToken[0123456789abcdefgh]", true)] [InlineData("android", "ExponentPushToken[0123456789abcdefgh]", true)]
    [InlineData("web", "ExpoPushToken[0123456789abcdefgh]", false)] [InlineData("ios", "raw-token", false)] [InlineData("ios", "ExpoPushToken[bad token]", false)]
    public void Device_validation_accepts_only_supported_platform_and_Expo_shape(string platform, string token, bool valid)
        => Assert.Equal(valid, new DeviceValidator().Validate(new DeviceRequest(platform, token)).IsValid);
    [Theory] [InlineData(1,30)] [InlineData(2,60)] [InlineData(5,480)] [InlineData(99,3600)]
    public void Retry_backoff_is_exponential_and_bounded(int attempt, int seconds) => Assert.Equal(TimeSpan.FromSeconds(seconds), NotificationPolicy.Backoff(attempt));
    [Theory] [InlineData("tr","TRY","Europe/Istanbul",true)] [InlineData("en","usd","UTC",true)] [InlineData("de","TRY","UTC",false)] [InlineData("en","INVALID","UTC",false)] [InlineData("en","USD","badzone",false)]
    public void Settings_validate_language_currency_and_zone(string language, string currency, string zone, bool valid)
        => Assert.Equal(valid, new SettingsValidator().Validate(new SettingsRequest(language,currency,zone,true)).IsValid);
    [Fact] public void Enabled_provider_requires_credentials_and_HTTPS()
    {
        Assert.Throws<InvalidOperationException>(() => NotificationOptions.Load(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?> { ["Notifications:Enabled"]="true" }).Build()));
        Assert.Throws<InvalidOperationException>(() => NotificationOptions.Load(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?> { ["Notifications:Enabled"]="true", ["Notifications:AccessToken"]="test", ["Notifications:SendEndpoint"]="http://example.com" }).Build()));
    }
    private sealed class Factory(HttpMessageHandler handler) : IHttpClientFactory
    { public HttpClient CreateClient(string name) => new(handler, false) { Timeout = TimeSpan.FromSeconds(15) }; }
    private sealed class Handler(HttpStatusCode status, string body) : HttpMessageHandler
    {
        public string? Payload;
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        { Payload=await request.Content!.ReadAsStringAsync(cancellationToken); return new(status) { Content = new StringContent(body, Encoding.UTF8,"application/json") }; }
    }
    [Theory]
    [InlineData(200,"{\"data\":{\"status\":\"ok\",\"id\":\"ticket-123\"}}",PushResultKind.TicketAccepted)]
    [InlineData(200,"{\"data\":{\"status\":\"error\",\"details\":{\"error\":\"DeviceNotRegistered\"}}}",PushResultKind.InvalidDevice)]
    [InlineData(200,"{\"data\":{\"status\":\"error\",\"details\":{\"error\":\"MessageRateExceeded\"}}}",PushResultKind.Retryable)]
    [InlineData(200,"{\"data\":{\"status\":\"error\",\"details\":{\"error\":\"MessageTooBig\"}}}",PushResultKind.Permanent)]
    [InlineData(200,"malformed",PushResultKind.Retryable)] [InlineData(429,"{}",PushResultKind.Retryable)] [InlineData(503,"{}",PushResultKind.Retryable)] [InlineData(401,"{}",PushResultKind.Permanent)]
    public async Task Expo_ticket_and_errors_are_classified_without_claiming_delivery(int status, string body, PushResultKind expected)
    {
        var handler = new Handler((HttpStatusCode)status,body);
        var sender = new ExpoPushNotificationSender(new Factory(handler), new() { Enabled = true, AccessToken = "test-only" });
        var result = await sender.SendAsync(new(Guid.NewGuid(),"ExpoPushToken[0123456789abcdefgh]"), CancellationToken.None);
        Assert.Equal(expected,result.Kind); Assert.DoesNotContain("amount",handler.Payload!); Assert.DoesNotContain("email",handler.Payload!);
    }
    [Theory]
    [InlineData("{\"data\":{\"ticket\":{\"status\":\"ok\"}}}",PushResultKind.ReceiptAccepted)]
    [InlineData("{\"data\":{}}",PushResultKind.PendingReceipt)]
    [InlineData("{\"data\":{\"ticket\":{\"status\":\"error\",\"details\":{\"error\":\"DeviceNotRegistered\"}}}}",PushResultKind.InvalidDevice)]
    public async Task Expo_receipts_are_separate_from_tickets(string body, PushResultKind expected)
    {
        var sender = new ExpoPushNotificationSender(new Factory(new Handler(HttpStatusCode.OK,body)),new() { Enabled = true, AccessToken = "test-only" });
        Assert.Equal(expected,(await sender.GetReceiptAsync("ticket",CancellationToken.None)).Kind);
    }
    [Fact] public async Task Disabled_provider_never_produces_fake_success()
    {
        var sender = new ExpoPushNotificationSender(new Factory(new Handler(HttpStatusCode.OK,"{}")),new());
        Assert.Equal(PushResultKind.Permanent,(await sender.SendAsync(new(Guid.NewGuid(),"token"),CancellationToken.None)).Kind);
    }
    private sealed class RetryHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken ct)
        { var response=new HttpResponseMessage(HttpStatusCode.TooManyRequests);response.Headers.RetryAfter=new System.Net.Http.Headers.RetryConditionHeaderValue(TimeSpan.FromMinutes(10));return Task.FromResult(response); }
    }
    [Fact] public async Task Expo_rate_limit_preserves_Retry_After()
    {
        var sender=new ExpoPushNotificationSender(new Factory(new RetryHandler()),new() { Enabled=true,AccessToken="test-only" });
        var result=await sender.SendAsync(new(Guid.NewGuid(),"ExpoPushToken[0123456789abcdefgh]"),CancellationToken.None);
        Assert.Equal(PushResultKind.Retryable,result.Kind);Assert.Equal(TimeSpan.FromMinutes(10),result.RetryAfter);
    }
    [Fact] public async Task Expo_response_is_bounded_before_JSON_parsing()
    {
        var sender=new ExpoPushNotificationSender(new Factory(new Handler(HttpStatusCode.OK,new string('x',65537))),new() { Enabled=true,AccessToken="test-only" });
        var result=await sender.SendAsync(new(Guid.NewGuid(),"ExpoPushToken[0123456789abcdefgh]"),CancellationToken.None);
        Assert.Equal(PushResultKind.Retryable,result.Kind);Assert.Equal("ProviderProtocol",result.ErrorCode);
    }
    private sealed class QueueStub : INotificationQueue
    {
        public NotificationDelivery? Delivery=new() { Id=Guid.NewGuid() };
        public bool Suppressed;public int Validations,Completions;public PushResult? Result;
        public Task<Guid?> ScheduleBatchAsync(Guid? cursor,DateTime now,CancellationToken ct)=>Task.FromResult<Guid?>(null);
        public Task<NotificationDelivery?> ClaimAsync(Guid owner,DateTime now,CancellationToken ct)=>Task.FromResult(Delivery);
        public Task<PushMessage?> RevalidateAsync(NotificationDelivery delivery,DateTime now,CancellationToken ct)
        { Validations++;return Task.FromResult(Suppressed?null:new PushMessage(delivery.Id,"ExpoPushToken[0123456789abcdefgh]")); }
        public Task CompleteAsync(NotificationDelivery delivery,PushResult result,DateTime now,CancellationToken ct)
        { Completions++;Result=result;return Task.CompletedTask; }
        public Task<int> CleanupAsync(DateTime now,CancellationToken ct)=>Task.FromResult(0);
    }
    private sealed class SenderStub : IPushNotificationSender
    {
        public int Sends,Receipts;public bool ThrowTimeout;
        public Task<PushResult> SendAsync(PushMessage message,CancellationToken ct)
        { Sends++;if(ThrowTimeout)throw new TaskCanceledException("provider timeout contains secret");return Task.FromResult(new PushResult(PushResultKind.TicketAccepted,"test-ticket")); }
        public Task<PushResult> GetReceiptAsync(string id,CancellationToken ct)
        { Receipts++;return Task.FromResult(new PushResult(PushResultKind.PendingReceipt)); }
    }
    [Fact] public async Task Processor_empty_queue_never_calls_provider()
    {
        var queue=new QueueStub { Delivery=null };var sender=new SenderStub();
        Assert.False(await new NotificationProcessor(queue,sender,TimeProvider.System).ProcessOneAsync(CancellationToken.None));Assert.Equal(0,sender.Sends);
    }
    [Fact] public async Task Processor_revalidates_before_send_and_suppressed_work_is_not_sent()
    {
        var queue=new QueueStub { Suppressed=true };var sender=new SenderStub();
        Assert.True(await new NotificationProcessor(queue,sender,TimeProvider.System).ProcessOneAsync(CancellationToken.None));Assert.Equal(1,queue.Validations);Assert.Equal(0,sender.Sends);Assert.Equal(0,queue.Completions);
    }
    [Fact] public async Task Processor_timeout_is_retryable_without_exception_text()
    {
        var queue=new QueueStub();var sender=new SenderStub { ThrowTimeout=true };
        await new NotificationProcessor(queue,sender,TimeProvider.System).ProcessOneAsync(CancellationToken.None);
        Assert.Equal(PushResultKind.Retryable,queue.Result!.Kind);Assert.Equal("ProviderUnavailable",queue.Result.ErrorCode);Assert.Equal(1,queue.Validations);
    }
    [Fact] public async Task Processor_receipt_poll_does_not_repeat_send_or_skip_tracking_after_preference_changes()
    {
        var queue=new QueueStub { Delivery=new NotificationDelivery { ProviderMessageId="ticket" },Suppressed=true };var sender=new SenderStub();
        await new NotificationProcessor(queue,sender,TimeProvider.System).ProcessOneAsync(CancellationToken.None);
        Assert.Equal(0,sender.Sends);Assert.Equal(1,sender.Receipts);Assert.Equal(0,queue.Validations);Assert.Equal(PushResultKind.PendingReceipt,queue.Result!.Kind);
    }
}
