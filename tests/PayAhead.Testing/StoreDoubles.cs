using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.IdentityModel.Tokens;
using PayAhead.Application.Premium;
using PayAhead.Domain;
using PayAhead.Infrastructure;

namespace PayAhead.Testing;

// Compiled only into tests and the guarded smoke host. Never referenced by shipping API projects.
public sealed record TestStoreProof(Guid UserId, string ProductId, string OriginalId, string TransactionId, DateTime ExpiresAtUtc,
    MembershipState State, long Revision, string EventId = "event", bool AutoRenew = true, string Store = "Google");
public sealed class TestStoreProvider(byte[] secret) : IStorePurchaseVerifier, IStoreWebhookAuthenticator
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };
    private readonly ConcurrentDictionary<string, TestStoreProof> current = new();
    private readonly ConcurrentDictionary<string, string> bindings = new();
    public string Sign(TestStoreProof proof)
    {
        var body = Base64UrlEncoder.Encode(JsonSerializer.SerializeToUtf8Bytes(proof, Json));
        return body + "." + Base64UrlEncoder.Encode(HMACSHA256.HashData(secret, Encoding.ASCII.GetBytes(body)));
    }
    private TestStoreProof Read(string evidence)
    {
        try
        {
            var parts = evidence.Split('.'); if (parts.Length != 2 || !CryptographicOperations.FixedTimeEquals(Base64UrlEncoder.DecodeBytes(parts[1]), HMACSHA256.HashData(secret, Encoding.ASCII.GetBytes(parts[0])))) throw new StoreVerificationException();
            var proof = JsonSerializer.Deserialize<TestStoreProof>(Base64UrlEncoder.DecodeBytes(parts[0]), Json)!;
            current.AddOrUpdate(proof.OriginalId, proof, (_, old) => proof.Revision > old.Revision ? proof : old); return proof;
        }
        catch (Exception ex) when (ex is FormatException or JsonException or IndexOutOfRangeException) { throw new StoreVerificationException(); }
    }
    private VerifiedPurchase Verified(TestStoreProof p, string evidence) => new(p.Store, p.ProductId, p.OriginalId, p.TransactionId, p.Store == "Google" ? PurchaseProtector.Hash(evidence) : null, null, p.UserId, p.State, p.ExpiresAtUtc, p.AutoRenew, "Sandbox", evidence);
    public Task<VerifiedPurchase> VerifyAsync(Guid user, PurchaseRequest request, CancellationToken ct)
    {
        var p = Read(request.Evidence!); if (p.UserId != user || p.ProductId != request.ProductId || p.Store != request.Store || p.ProductId != "test.premium") throw new StoreVerificationException();
        bindings.TryAdd(p.OriginalId, request.Evidence!);
        return Task.FromResult(Verified(current[p.OriginalId], request.Evidence!));
    }
    public Task<VerifiedPurchase> ReconcileAsync(UserSubscription subscription, string evidence, CancellationToken ct)
    {
        var old = Read(evidence); var p = current[old.OriginalId]; return Task.FromResult(Verified(p, evidence));
    }
    public Task<VerifiedWebhook> AuthenticateAsync(string store, string body, string? authorization, CancellationToken ct)
    {
        using var json = JsonDocument.Parse(body); var proof = Read(json.RootElement.GetProperty("signedPayload").GetString()!);
        if (proof.Store != store) throw new StoreVerificationException();
        return Task.FromResult(new VerifiedWebhook(store, proof.EventId, store == "Google" ? bindings.GetValueOrDefault(proof.OriginalId, Sign(proof)) : proof.OriginalId));
    }
}
