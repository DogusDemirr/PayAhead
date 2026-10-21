using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Text.Json;
using Microsoft.IdentityModel.Tokens;
using PayAhead.Application.Premium;

namespace PayAhead.Infrastructure;

public sealed class GooglePushSignatureValidator(StoreHttp http)
{
    private JsonWebKeySet? keys; private DateTime expires; private readonly SemaphoreSlim gate = new(1);
    public async Task ValidateAsync(string? authorization, StoreOptions options, CancellationToken ct)
    {
        if (authorization is null || !authorization.StartsWith("Bearer ", StringComparison.Ordinal) || authorization.Length > 20_000) throw new StoreVerificationException();
        await gate.WaitAsync(ct);
        try
        {
            if (keys is null || expires < DateTime.UtcNow)
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, "https://www.googleapis.com/oauth2/v3/certs"); using var json = await http.JsonAsync(request, ct);
                keys = new JsonWebKeySet(json.RootElement.GetRawText()); expires = DateTime.UtcNow.AddMinutes(30);
            }
        }
        finally { gate.Release(); }
        try
        {
            var handler = new JwtSecurityTokenHandler { MapInboundClaims = false };
            var principal = handler.ValidateToken(authorization[7..], new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKeys = keys.GetSigningKeys(),
                ValidAlgorithms = [SecurityAlgorithms.RsaSha256],
                RequireSignedTokens = true,
                RequireExpirationTime = true,
                ValidateIssuer = true,
                ValidIssuers = ["https://accounts.google.com", "accounts.google.com"],
                ValidateAudience = true,
                ValidAudience = options.GooglePushAudience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromSeconds(15)
            }, out _);
            if (principal.FindFirst("email")?.Value != options.GooglePushEmail || principal.FindFirst("email_verified")?.Value != "true") throw new StoreVerificationException();
        }
        catch (SecurityTokenSignatureKeyNotFoundException) { expires = DateTime.MinValue; throw new StoreVerificationException(); }
        catch (SecurityTokenException) { throw new StoreVerificationException(); }
    }
}
public sealed class StoreWebhookAuthenticator(StoreOptions options, AppleSignedDataVerifier apple, GooglePushSignatureValidator google) : IStoreWebhookAuthenticator
{
    public async Task<VerifiedWebhook> AuthenticateAsync(string store, string body, string? authorization, CancellationToken ct)
    {
        try
        {
            using var json = JsonDocument.Parse(body);
            if (store == "Apple")
            {
                if (!options.AppleEnabled) throw new StoreUnavailableException();
                using var signed = apple.Verify(json.RootElement.GetProperty("signedPayload").GetString()!); var root = signed.RootElement;
                var data = root.GetProperty("data"); apple.App(data, true);
                using var transaction = apple.Verify(data.GetProperty("signedTransactionInfo").GetString()!); apple.App(transaction.RootElement);
                return new("Apple", root.GetProperty("notificationUUID").GetString()!, transaction.RootElement.GetProperty("originalTransactionId").GetString()!);
            }
            if (store != "Google") throw new StoreVerificationException(); if (!options.GoogleEnabled) throw new StoreUnavailableException();
            await google.ValidateAsync(authorization, options, ct);
            if (json.RootElement.GetProperty("subscription").GetString() != options.GooglePushSubscription) throw new StoreVerificationException();
            var message = json.RootElement.GetProperty("message"); using var payload = JsonDocument.Parse(Convert.FromBase64String(message.GetProperty("data").GetString()!));
            var p = payload.RootElement; if (p.GetProperty("packageName").GetString() != options.GooglePackageName) throw new StoreVerificationException();
            var id = message.GetProperty("messageId").GetString()!;
            if (p.TryGetProperty("subscriptionNotification", out var subscription)) return new("Google", id, subscription.GetProperty("purchaseToken").GetString()!);
            if (p.TryGetProperty("voidedPurchaseNotification", out var voided)) return new("Google", id, voided.GetProperty("purchaseToken").GetString()!);
            if (p.TryGetProperty("testNotification", out _)) return new("Google", id, "");
            throw new StoreVerificationException();
        }
        catch (Exception ex) when (ex is JsonException or KeyNotFoundException or FormatException or InvalidOperationException or ArgumentException) { throw new StoreVerificationException(); }
    }
}
