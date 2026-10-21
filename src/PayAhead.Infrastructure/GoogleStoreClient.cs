using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.IdentityModel.Tokens;
using PayAhead.Application.Premium;
using PayAhead.Domain;

namespace PayAhead.Infrastructure;

public sealed class GoogleStoreClient(StoreOptions options, StoreHttp http)
{
    public async Task<VerifiedPurchase> FetchAsync(Guid user, string token, string? expectedProduct, CancellationToken ct)
    {
        if (!options.GoogleEnabled) throw new StoreUnavailableException();
        using var rsa = RSA.Create(); rsa.ImportFromPem(options.GooglePrivateKeyPem); var now = DateTime.UtcNow;
        var jwt = new JwtSecurityToken(options.GoogleServiceAccountEmail, "https://oauth2.googleapis.com/token", [new Claim("scope", "https://www.googleapis.com/auth/androidpublisher")], now, now.AddMinutes(5), new SigningCredentials(new RsaSecurityKey(rsa), SecurityAlgorithms.RsaSha256));
        using var oauthRequest = new HttpRequestMessage(HttpMethod.Post, "https://oauth2.googleapis.com/token") { Content = new FormUrlEncodedContent(new Dictionary<string, string> { { "grant_type", "urn:ietf:params:oauth:grant-type:jwt-bearer" }, { "assertion", new JwtSecurityTokenHandler().WriteToken(jwt) } }) };
        using var auth = await http.JsonAsync(oauthRequest, ct); var access = auth.RootElement.GetProperty("access_token").GetString()!;
        var baseUrl = "https://androidpublisher.googleapis.com/androidpublisher/v3/applications/" + Uri.EscapeDataString(options.GooglePackageName);
        using var request = StoreHttp.Get(baseUrl + "/purchases/subscriptionsv2/tokens/" + Uri.EscapeDataString(token), access);
        using var response = await http.JsonAsync(request, ct);
        var result = Parse(response.RootElement, user, token, expectedProduct, options);
        if (response.RootElement.GetProperty("acknowledgementState").GetString() == "ACKNOWLEDGEMENT_STATE_PENDING" && result.State == MembershipState.Active && result.ExpiresAtUtc > now)
        {
            using var acknowledge = new HttpRequestMessage(HttpMethod.Post, baseUrl + "/purchases/subscriptions/" + Uri.EscapeDataString(result.ProductId) + "/tokens/" + Uri.EscapeDataString(token) + ":acknowledge")
            { Content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json") };
            acknowledge.Headers.Authorization = new("Bearer", access); using var ack = await http.JsonAsync(acknowledge, ct);
        }
        return result;
    }
    public static VerifiedPurchase Parse(JsonElement root, Guid user, string token, string? expectedProduct, StoreOptions options)
    {
        var environment = root.TryGetProperty("testPurchase", out var testPurchase) && testPurchase.ValueKind == JsonValueKind.Object ? "Sandbox" : "Production";
        if (environment != options.GoogleEnvironment) throw new StoreVerificationException();
        var account = root.GetProperty("externalAccountIdentifiers").GetProperty("obfuscatedExternalAccountId").GetString();
        if (account != SqlEntitlementService.AccountId(user)) throw new StoreVerificationException();
        var line = root.GetProperty("lineItems").EnumerateArray().OrderByDescending(x => DateTimeOffset.Parse(x.GetProperty("expiryTime").GetString()!, System.Globalization.CultureInfo.InvariantCulture)).First();
        var product = line.GetProperty("productId").GetString()!;
        if (!options.GoogleProducts.Contains(product, StringComparer.Ordinal) || expectedProduct is not null && product != expectedProduct) throw new StoreVerificationException();
        var expiry = DateTimeOffset.Parse(line.GetProperty("expiryTime").GetString()!, System.Globalization.CultureInfo.InvariantCulture).UtcDateTime;
        var state = root.GetProperty("subscriptionState").GetString() switch
        {
            "SUBSCRIPTION_STATE_ACTIVE" or "SUBSCRIPTION_STATE_CANCELED" => MembershipState.Active,
            "SUBSCRIPTION_STATE_IN_GRACE_PERIOD" => MembershipState.GracePeriod,
            "SUBSCRIPTION_STATE_ON_HOLD" or "SUBSCRIPTION_STATE_PAUSED" => MembershipState.BillingRetry,
            "SUBSCRIPTION_STATE_EXPIRED" => MembershipState.Expired,
            _ => MembershipState.Free
        };
        var transaction = line.TryGetProperty("latestSuccessfulOrderId", out var order) ? order.GetString() : root.TryGetProperty("latestOrderId", out order) ? order.GetString() : null;
        transaction ??= PurchaseProtector.Hash(token) + ":" + new DateTimeOffset(expiry).ToUnixTimeSeconds();
        var linked = root.TryGetProperty("linkedPurchaseToken", out var link) && link.ValueKind == JsonValueKind.String ? PurchaseProtector.Hash(link.GetString()!) : null;
        return new("Google", product, PurchaseProtector.Hash(token), transaction, PurchaseProtector.Hash(token), linked, user, state, expiry,
            line.TryGetProperty("autoRenewingPlan", out var renew) && renew.GetProperty("autoRenewEnabled").GetBoolean(), environment, token);
    }
}
