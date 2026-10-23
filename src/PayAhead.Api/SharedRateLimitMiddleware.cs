using System.Security.Cryptography;
using System.Text;
using PayAhead.Application.Operations;

namespace PayAhead.Api;

public sealed class SharedRateLimitMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, ISharedRateLimiter limiter, JwtSettings jwt)
    {
        var path = context.Request.Path.Value?.TrimEnd('/').ToLowerInvariant() ?? "";
        var policy = path.StartsWith("/api/auth/", StringComparison.Ordinal) ? "auth" : path is "/api/premium/verify-purchase" or "/api/premium/restore" ? "purchase" : path.StartsWith("/api/webhooks/", StringComparison.Ordinal) ? "webhook" : path == "/api/users/me/export" || path == "/api/users/me" && HttpMethods.IsDelete(context.Request.Method) ? "privacy" : null;
        if (path == "/api/analytics") policy = "analytics";
        if (policy is null) { await next(context); return; }
        var identity = policy is "purchase" or "privacy" or "analytics" ? context.User.FindFirst("sub")?.Value : null;
        identity ??= context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var key = Convert.ToHexString(HMACSHA256.HashData(jwt.KeyBytes, Encoding.UTF8.GetBytes("PayAhead.Rate.v1:" + policy + ":" + identity)));
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(context.RequestAborted); deadline.CancelAfter(TimeSpan.FromSeconds(5));
        bool accepted;
        try { accepted = await limiter.AcquireAsync(key, policy switch { "auth" => 20, "purchase" => 10, "privacy" => 5, "analytics" => 30, _ => 60 }, deadline.Token); }
        catch (Exception ex) when (ex is OperationalUnavailableException || ex is OperationCanceledException && !context.RequestAborted.IsCancellationRequested)
        {
            OperationalTelemetry.Outcomes.Add(1, new KeyValuePair<string, object?>("category", "rate"), new("result", "unavailable"));
            await Results.Problem(statusCode: 503, title: "İstek koruması şu anda kullanılamıyor.").ExecuteAsync(context); return;
        }
        if (!accepted)
        {
            OperationalTelemetry.Outcomes.Add(1, new KeyValuePair<string, object?>("category", "rate"), new("result", "rejected"));
            context.Response.Headers.RetryAfter = "60";
            await Results.Problem(statusCode: 429, title: "İstek sınırı aşıldı.").ExecuteAsync(context); return;
        }
        await next(context);
    }
}
