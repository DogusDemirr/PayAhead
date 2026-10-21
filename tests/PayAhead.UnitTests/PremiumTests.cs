using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using PayAhead.Application.Premium;
using PayAhead.Domain;
using PayAhead.Infrastructure;
using Xunit;

namespace PayAhead.UnitTests;

public sealed class PremiumTests
{
    private static readonly DateTime Now = DateTime.UtcNow;
    [Theory]
    [InlineData(MembershipState.Active, true, true)]
    [InlineData(MembershipState.GracePeriod, true, true)]
    [InlineData(MembershipState.BillingRetry, true, false)]
    [InlineData(MembershipState.Revoked, true, false)]
    [InlineData(MembershipState.Expired, true, false)]
    [InlineData(MembershipState.Free, true, false)]
    [InlineData(MembershipState.Active, false, false)]
    public void Only_fresh_store_verified_active_or_grace_grants_access(MembershipState state, bool verified, bool premium)
    { var value = new UserSubscription { Status = state.ToString(), IsStoreVerified = verified, CurrentPeriodEndUtc = Now.AddDays(1), LastVerifiedAtUtc = Now }; Assert.Equal(premium, MembershipRules.GrantsPremium(value, Now, TimeSpan.FromHours(24))); }
    [Theory]
    [InlineData(-1, 0, false)]
    [InlineData(1, -25, false)]
    [InlineData(1, 0, true)]
    [InlineData(0, 0, false)]
    public void Expiration_and_verification_freshness_are_fail_closed(int endDays, int verifiedHours, bool premium)
    { var value = new UserSubscription { Status = "Active", IsStoreVerified = true, CurrentPeriodEndUtc = Now.AddDays(endDays), LastVerifiedAtUtc = Now.AddHours(verifiedHours) }; Assert.Equal(premium, MembershipRules.GrantsPremium(value, Now, TimeSpan.FromHours(24))); }
    [Fact]
    public void Disabling_auto_renew_keeps_paid_access_until_period_end()
    { var value = new UserSubscription { Status = "Active", AutoRenew = false, IsStoreVerified = true, CurrentPeriodEndUtc = Now.AddHours(1), LastVerifiedAtUtc = Now }; Assert.True(MembershipRules.GrantsPremium(value, Now, TimeSpan.FromHours(24))); }
    [Fact]
    public void Central_free_limits_match_product_contract()
    { var options = new MembershipOptions(); Assert.Equal(new PlanLimits(5, 1, 3), options.Free); Assert.True(options.Premium.ActivePaymentPlans > 5); Assert.True(options.Premium.ActiveRemindersPerPlan > 1); }
    [Fact]
    public void Evidence_is_authenticated_encrypted_randomized_and_rotatable()
    {
        var options = new StoreOptions { EncryptionKeys = new() { ["v1"] = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)) } }; var protector = new PurchaseProtector(options);
        var first = protector.Protect("private-purchase-token"); Assert.DoesNotContain("private", first); Assert.NotEqual(first, protector.Protect("private-purchase-token")); Assert.Equal("private-purchase-token", protector.Unprotect(first));
        var bytes = Convert.FromBase64String(first.Split('.')[1]); bytes[^1] ^= 1; Assert.Throws<StoreUnavailableException>(() => protector.Unprotect("v1." + Convert.ToBase64String(bytes)));
        Assert.Throws<StoreUnavailableException>(() => new PurchaseProtector(new()).Protect("token"));
    }
    [Theory]
    [InlineData("AppleEnabled")]
    [InlineData("GoogleEnabled")]
    public void Enabled_store_requires_real_configuration(string setting)
        => Assert.Throws<InvalidOperationException>(() => StoreOptions.Load(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Stores:" + setting] = "true" }).Build()));
    [Theory]
    [InlineData("bad")]
    [InlineData("e30.e30.bad")]
    [InlineData("eyJhbGciOiJub25lIn0.e30.")]
    public void Apple_never_accepts_unsigned_or_malformed_proofs(string jws) => Assert.Throws<StoreVerificationException>(() => new AppleSignedDataVerifier(new()).Verify(jws));
    [Theory]
    [InlineData("wrong", "Production")]
    [InlineData("com.payahead", "Sandbox")]
    public void Apple_app_identity_and_environment_are_checked(string bundle, string environment)
    { using var json = JsonDocument.Parse(JsonSerializer.Serialize(new { bundleId = bundle, environment })); Assert.Throws<StoreVerificationException>(() => new AppleSignedDataVerifier(new() { AppleBundleId = "com.payahead" }).App(json.RootElement)); }
    private static JsonDocument Google(string state, string account, string product = "premium", bool renew = true) => JsonDocument.Parse(JsonSerializer.Serialize(new
    {
        subscriptionState = state,
        acknowledgementState = "ACKNOWLEDGEMENT_STATE_ACKNOWLEDGED",
        externalAccountIdentifiers = new { obfuscatedExternalAccountId = account },
        lineItems = new[] { new { productId = product, expiryTime = DateTimeOffset.UtcNow.AddDays(2).ToString("O"), latestSuccessfulOrderId = "order", autoRenewingPlan = new { autoRenewEnabled = renew } } }
    }));
    [Theory]
    [InlineData("SUBSCRIPTION_STATE_ACTIVE", MembershipState.Active)]
    [InlineData("SUBSCRIPTION_STATE_CANCELED", MembershipState.Active)]
    [InlineData("SUBSCRIPTION_STATE_IN_GRACE_PERIOD", MembershipState.GracePeriod)]
    [InlineData("SUBSCRIPTION_STATE_ON_HOLD", MembershipState.BillingRetry)]
    [InlineData("SUBSCRIPTION_STATE_EXPIRED", MembershipState.Expired)]
    [InlineData("SUBSCRIPTION_STATE_PENDING", MembershipState.Free)]
    public void Google_lifecycle_mapping_is_explicit(string state, MembershipState expected)
    {
        var user = Guid.NewGuid(); using var json = Google(state, SqlEntitlementService.AccountId(user));
        Assert.Equal(expected, GoogleStoreClient.Parse(json.RootElement, user, "token", "premium", new() { GoogleProducts = ["premium"] }).State);
    }
    [Fact]
    public void Google_rejects_different_account_product_and_environment()
    {
        var user = Guid.NewGuid(); using var json = Google("SUBSCRIPTION_STATE_ACTIVE", SqlEntitlementService.AccountId(user));
        Assert.Throws<StoreVerificationException>(() => GoogleStoreClient.Parse(json.RootElement, Guid.NewGuid(), "token", "premium", new() { GoogleProducts = ["premium"] }));
        Assert.Throws<StoreVerificationException>(() => GoogleStoreClient.Parse(json.RootElement, user, "token", "invalid", new() { GoogleProducts = ["premium"] }));
        Assert.Throws<StoreVerificationException>(() => GoogleStoreClient.Parse(json.RootElement, user, "token", "premium", new() { GoogleProducts = ["premium"], GoogleEnvironment = "Sandbox" }));
    }
    private sealed class Factory(string body) : IHttpClientFactory
    {
        private sealed class Handler(string json) : HttpMessageHandler
        { protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json) }); }
        public HttpClient CreateClient(string name) => new(new Handler(body));
    }
    [Fact]
    public void Google_null_testPurchase_is_Production_and_linked_token_is_hashed()
    {
        var user = Guid.NewGuid(); using var source = Google("SUBSCRIPTION_STATE_ACTIVE", SqlEntitlementService.AccountId(user));
        var node = System.Text.Json.Nodes.JsonNode.Parse(source.RootElement.GetRawText())!; node["testPurchase"] = null; node["linkedPurchaseToken"] = "previous-private-token";
        using var json = JsonDocument.Parse(node.ToJsonString());
        var result = GoogleStoreClient.Parse(json.RootElement, user, "new-token", "premium", new() { GoogleProducts = ["premium"] });
        Assert.Equal("Production", result.Environment); Assert.Equal(PurchaseProtector.Hash("previous-private-token"), result.LinkedTokenHash);
    }
    [Theory]
    [InlineData("valid")]
    [InlineData("audience")]
    [InlineData("email")]
    [InlineData("signature")]
    [InlineData("expired")]
    public async Task Google_push_OIDC_is_cryptographically_validated(string scenario)
    {
        using var rsa = RSA.Create(2048); using var other = RSA.Create(2048); var p = rsa.ExportParameters(false);
        var jwks = JsonSerializer.Serialize(new { keys = new[] { new { kty = "RSA", kid = "unit", alg = "RS256", use = "sig", n = Base64UrlEncoder.Encode(p.Modulus!), e = Base64UrlEncoder.Encode(p.Exponent!) } } });
        var now = DateTime.UtcNow;
        var token = new JwtSecurityToken("https://accounts.google.com", scenario == "audience" ? "wrong" : "https://api/push",
            [new Claim("email", scenario == "email" ? "attacker" : "push@test.invalid"), new Claim("email_verified", "true", ClaimValueTypes.Boolean)], now.AddHours(-1), scenario == "expired" ? now.AddMinutes(-1) : now.AddMinutes(5),
            new SigningCredentials(new RsaSecurityKey(scenario == "signature" ? other : rsa) { KeyId = "unit" }, SecurityAlgorithms.RsaSha256));
        var validator = new GooglePushSignatureValidator(new StoreHttp(new Factory(jwks)));
        var task = validator.ValidateAsync("Bearer " + new JwtSecurityTokenHandler().WriteToken(token), new() { GooglePushAudience = "https://api/push", GooglePushEmail = "push@test.invalid" }, CancellationToken.None);
        if (scenario == "valid") await task; else await Assert.ThrowsAsync<StoreVerificationException>(() => task);
    }
}
