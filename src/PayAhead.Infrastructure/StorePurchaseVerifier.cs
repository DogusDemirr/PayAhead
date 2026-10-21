using System.Security.Cryptography;
using System.Text.Json;
using PayAhead.Application.Premium;
using PayAhead.Domain;

namespace PayAhead.Infrastructure;

public sealed class StorePurchaseVerifier(AppleStoreClient apple,GoogleStoreClient google) : IStorePurchaseVerifier
{
    public Task<VerifiedPurchase> VerifyAsync(Guid user,PurchaseRequest request,CancellationToken ct)
        => Safe(()=>request.Store switch
        {
            "Apple"=>apple.VerifyAsync(user,request.Evidence!,request.ProductId,ct),
            "Google"=>google.FetchAsync(user,request.Evidence!,request.ProductId,ct),
            _=>throw new StoreVerificationException()
        },ct);
    public Task<VerifiedPurchase> ReconcileAsync(UserSubscription subscription,string evidence,CancellationToken ct)
        => Safe(()=>subscription.Provider=="Apple"?apple.FetchAsync(subscription.UserId,subscription.ExternalSubscriptionId,ct):google.FetchAsync(subscription.UserId,evidence,null,ct),ct);
    private static async Task<VerifiedPurchase> Safe(Func<Task<VerifiedPurchase>> action,CancellationToken ct)
    {
        try { return await action(); }
        catch(OperationCanceledException)when(!ct.IsCancellationRequested){throw new StoreUnavailableException();}
        catch(HttpRequestException){throw new StoreUnavailableException();}
        catch(Exception ex)when(ex is JsonException or KeyNotFoundException or FormatException or InvalidOperationException or ArgumentException or CryptographicException){throw new StoreVerificationException();}
    }
}
