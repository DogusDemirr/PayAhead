using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using PayAhead.Application.Notifications;

namespace PayAhead.Infrastructure;

public sealed class NotificationOptions
{
    public bool Enabled { get; init; }
    public string SendEndpoint { get; init; } = "https://exp.host/--/api/v2/push/send";
    public string ReceiptEndpoint { get; init; } = "https://exp.host/--/api/v2/push/getReceipts";
    public string? AccessToken { get; init; }
    public int PollSeconds { get; init; } = 30;
    public static NotificationOptions Load(IConfiguration configuration)
    {
        var value = configuration.GetSection("Notifications").Get<NotificationOptions>() ?? new();
        if (value.PollSeconds is < 5 or > 300) throw new InvalidOperationException("Notification poll süresi 5–300 saniye olmalıdır.");
        if (value.Enabled && (string.IsNullOrWhiteSpace(value.AccessToken) || !ValidEndpoint(value.SendEndpoint) || !ValidEndpoint(value.ReceiptEndpoint)))
            throw new InvalidOperationException("Notifications etkin ise HTTPS Expo endpoint ve access token gereklidir.");
        return value;
    }
    private static bool ValidEndpoint(string endpoint) => Uri.TryCreate(endpoint, UriKind.Absolute, out var uri) && uri.Scheme == "https" && uri.UserInfo.Length == 0;
}
public sealed class ExpoPushNotificationSender(IHttpClientFactory clients, NotificationOptions options) : IPushNotificationSender
{
    public Task<PushResult> SendAsync(PushMessage message, CancellationToken ct)
        => Call(options.SendEndpoint, new
        {
            to = message.PushToken,
            title = "PayAhead",
            body = message.SubscriptionId.HasValue ? "Bir deneme bitişi hatırlatmanız var." : "Bir ödeme hatırlatmanız var.",
            data = new { notificationId = message.DeliveryId, kind = message.SubscriptionId.HasValue ? "subscription" : "payment", resourceId = message.SubscriptionId ?? message.PaymentPlanId },
            channelId = "payahead-reminders",
            sound = "default",
            ttl = 3600
        }, null, ct);
    public Task<PushResult> GetReceiptAsync(string messageId, CancellationToken ct)
        => Call(options.ReceiptEndpoint, new { ids = new[] { messageId } }, messageId, ct);
    private async Task<PushResult> Call(string endpoint, object payload, string? receiptId, CancellationToken ct)
    {
        if (!options.Enabled || string.IsNullOrWhiteSpace(options.AccessToken)) return new(PushResultKind.Permanent, ErrorCode: "NotConfigured");
        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint) { Content = JsonContent.Create(payload) };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.AccessToken);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(TimeSpan.FromSeconds(15));
        using var response = await clients.CreateClient("ExpoPush").SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token);
        if (response.StatusCode == HttpStatusCode.TooManyRequests || (int)response.StatusCode >= 500)
        {
            var retry = response.Headers.RetryAfter?.Delta ?? (response.Headers.RetryAfter?.Date - DateTimeOffset.UtcNow);
            return new(PushResultKind.Retryable, ErrorCode: "ProviderUnavailable", RetryAfter: retry is null ? null : TimeSpan.FromSeconds(Math.Clamp(retry.Value.TotalSeconds, 1, 3600)));
        }
        if (!response.IsSuccessStatusCode) return new(PushResultKind.Permanent, ErrorCode: response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden ? "InvalidCredentials" : "ProviderRejected");
        try
        {
            // Bound the provider response before parsing; never persist/log its free-form message.
            await using var stream = await response.Content.ReadAsStreamAsync(timeout.Token);
            using var buffer = new MemoryStream();
            var chunk = new byte[4096];
            int count;
            while ((count = await stream.ReadAsync(chunk.AsMemory(), timeout.Token)) != 0)
            {
                if (buffer.Length + count > 64 * 1024) return new(PushResultKind.Retryable, ErrorCode: "ProviderProtocol");
                await buffer.WriteAsync(chunk.AsMemory(0, count), timeout.Token);
            }
            using var json = JsonDocument.Parse(buffer.ToArray());
            if (!json.RootElement.TryGetProperty("data", out var data)) return new(PushResultKind.Retryable, ErrorCode: "ProviderProtocol");
            if (receiptId is not null)
            {
                if (data.ValueKind != JsonValueKind.Object || !data.TryGetProperty(receiptId, out data)) return new(PushResultKind.PendingReceipt);
            }
            else if (data.ValueKind == JsonValueKind.Array && data.GetArrayLength() == 1) data = data[0];
            if (data.ValueKind != JsonValueKind.Object || !data.TryGetProperty("status", out var status)) return new(PushResultKind.Retryable, ErrorCode: "ProviderProtocol");
            if (status.GetString() == "ok")
            {
                if (receiptId is not null) return new(PushResultKind.ReceiptAccepted);
                if (data.TryGetProperty("id", out var id) && id.ValueKind == JsonValueKind.String && id.GetString() is { Length: > 0 and <= 128 } ticket)
                    return new(PushResultKind.TicketAccepted, ticket);
                return new(PushResultKind.Retryable, ErrorCode: "ProviderProtocol");
            }
            var code = data.TryGetProperty("details", out var details) && details.TryGetProperty("error", out var error) && error.ValueKind == JsonValueKind.String ? error.GetString() : null;
            return code switch
            {
                "DeviceNotRegistered" => new(PushResultKind.InvalidDevice, ErrorCode: code),
                "MessageRateExceeded" => new(receiptId is null ? PushResultKind.Retryable : PushResultKind.Resend, ErrorCode: code),
                "MessageTooBig" or "InvalidCredentials" or "MismatchSenderId" => new(PushResultKind.Permanent, ErrorCode: code),
                _ => new(PushResultKind.Permanent, ErrorCode: "ProviderRejected")
            };
        }
        catch (JsonException) { return new(PushResultKind.Retryable, ErrorCode: "ProviderProtocol"); }
        catch (InvalidOperationException) { return new(PushResultKind.Retryable, ErrorCode: "ProviderProtocol"); }
    }
}
