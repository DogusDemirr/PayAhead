using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using PayAhead.Application.Premium;
using PayAhead.Application.Payments;
using PayAhead.Domain;

namespace PayAhead.Infrastructure;

public sealed class PremiumService(PayAheadDbContext db,IStorePurchaseVerifier verifier,IStoreWebhookAuthenticator webhooks,
    IEntitlementService entitlements,PurchaseProtector protector,TimeProvider clock) : IPremiumService
{
    public async Task<EntitlementResponse> VerifyAsync(Guid user,PurchaseRequest request,CancellationToken ct)
    {
        if(request.Store is not ("Apple" or "Google")||string.IsNullOrWhiteSpace(request.ProductId)||request.ProductId.Length>200||string.IsNullOrWhiteSpace(request.Evidence)||request.Evidence.Length>100_000)throw new StoreVerificationException();
        await Serialized(user,async token=>
        {
            if(request.Store=="Google")
            {
                var hash=PurchaseProtector.Hash(request.Evidence);
                var known=await db.StorePurchases.AsNoTracking().SingleOrDefaultAsync(x=>x.Store=="Google"&&x.TokenHash==hash,token);
                if(known is not null)
                {
                    if(known.UserId!=user)throw new ResourceConflictException("Satın alma başka bir kullanıcıya bağlıdır.");
                    var root=await db.UserSubscriptions.AsNoTracking().SingleAsync(x=>x.Id==known.UserSubscriptionId,token);
                    var latest=protector.Unprotect(root.ProtectedEvidence!);
                    if(PurchaseProtector.Hash(latest)!=hash)return await verifier.ReconcileAsync(root,latest,token);
                }
            }
            return await verifier.VerifyAsync(user,request,token);
        },ct);
        return await entitlements.GetAsync(user,ct);
    }
    private async Task Serialized(Guid user,Func<CancellationToken,Task<VerifiedPurchase>> fetch,CancellationToken ct)
    {
        using var deadline=CancellationTokenSource.CreateLinkedTokenSource(ct);deadline.CancelAfter(TimeSpan.FromSeconds(25));
        try
        {
            await db.Database.CreateExecutionStrategy().ExecuteAsync(async()=>
            {
                db.ChangeTracker.Clear();await using var tx=await db.Database.BeginTransactionAsync(deadline.Token);
                await entitlements.LockUserAsync(user,deadline.Token);
                var value=await fetch(deadline.Token);
                await Persist(user,value,deadline.Token);await tx.CommitAsync(deadline.Token);
            });
        }
        catch(OperationCanceledException)when(!ct.IsCancellationRequested){throw new StoreUnavailableException();}
        catch(DbUpdateException ex)when(ex.InnerException is SqlException {Number:2601 or 2627}){throw new ResourceConflictException("Satın alma başka bir kayıtla ilişkilidir.");}
    }
    private async Task Persist(Guid user,VerifiedPurchase value,CancellationToken ct)
    {
        if(value.AccountUserId!=user||value.Store is not ("Apple" or "Google")||string.IsNullOrWhiteSpace(value.OriginalId)||value.OriginalId.Length>200
            ||string.IsNullOrWhiteSpace(value.TransactionId)||value.TransactionId.Length>200||value.ProductId.Length>200||value.ExpiresAtUtc.Kind!=DateTimeKind.Utc)throw new StoreVerificationException();
        var matches=await db.StorePurchases.Where(x=>x.Store==value.Store&&(x.TransactionId==value.TransactionId||value.TokenHash!=null&&x.TokenHash==value.TokenHash)).Take(2).ToListAsync(ct);
        if(matches.Count>1)throw new ResourceConflictException("Transaction ve token farklı satın almalara bağlıdır.");
        var purchase=matches.SingleOrDefault();
        var linked=value.LinkedTokenHash is null?null:await db.StorePurchases.SingleOrDefaultAsync(x=>x.Store==value.Store&&x.TokenHash==value.LinkedTokenHash,ct);
        var transaction=await db.StoreTransactions.SingleOrDefaultAsync(x=>x.Store==value.Store&&x.TransactionId==value.TransactionId,ct);
        if(transaction is not null&&transaction.UserId!=user)throw new ResourceConflictException("Transaction başka bir kullanıcıya bağlıdır.");
        if(purchase is not null&&purchase.UserId!=user||linked is not null&&linked.UserId!=user)throw new ResourceConflictException("Satın alma başka bir kullanıcıya bağlıdır.");
        var root=await db.UserSubscriptions.SingleOrDefaultAsync(x=>x.Provider==value.Store&&x.ExternalSubscriptionId==value.OriginalId,ct);
        var membershipId=purchase?.UserSubscriptionId??linked?.UserSubscriptionId;
        if(root is null&&membershipId.HasValue)root=await db.UserSubscriptions.SingleAsync(x=>x.Id==membershipId,ct);
        if(root is not null&&root.UserId!=user)throw new ResourceConflictException("Satın alma başka bir kullanıcıya bağlıdır.");
        var roots=new[]{root?.Id,purchase?.UserSubscriptionId,linked?.UserSubscriptionId,transaction?.UserSubscriptionId}.Where(x=>x.HasValue).Select(x=>x!.Value).Distinct().Take(2).Count();
        if(roots>1)throw new ResourceConflictException("Satın alma kayıtları farklı üyeliklere bağlıdır.");
        var now=clock.GetUtcNow().UtcDateTime;
        if(root is null)
        {
            if(await db.UserSubscriptions.CountAsync(x=>x.UserId==user,ct)>=100)throw new ResourceConflictException("Üyelik geçmişi teknik sınırı aşıldı.");
            root=new() {Id=Guid.NewGuid(),UserId=user,Provider=value.Store,ExternalSubscriptionId=value.OriginalId,CreatedAtUtc=now};db.UserSubscriptions.Add(root);
        }
        root.ProductId=value.ProductId;root.Status=value.State.ToString();root.CurrentPeriodEndUtc=value.ExpiresAtUtc;
        root.AutoRenew=value.AutoRenew;root.StoreEnvironment=value.Environment;root.IsStoreVerified=true;root.LastVerifiedAtUtc=now;root.UpdatedAtUtc=now;
        root.ProtectedEvidence=protector.Protect(value.ReconcileEvidence);
        root.NextVerificationAtUtc=now.AddHours(6);
        if(transaction is null)db.StoreTransactions.Add(new() {Id=Guid.NewGuid(),UserId=user,UserSubscriptionId=root.Id,Store=value.Store,TransactionId=value.TransactionId,CreatedAtUtc=now});
        if(purchase is null)db.StorePurchases.Add(new() {Id=Guid.NewGuid(),UserId=user,UserSubscriptionId=root.Id,Store=value.Store,TransactionId=value.TransactionId,TokenHash=value.TokenHash,CreatedAtUtc=now});
        // The token binding is immutable; Google's renewal order is refreshed on the membership row.
        await db.SaveChangesAsync(ct);
    }
    public async Task ReceiveAsync(string store,string body,string? authorization,CancellationToken ct)
    {
        using var deadline=CancellationTokenSource.CreateLinkedTokenSource(ct);deadline.CancelAfter(TimeSpan.FromSeconds(15));
        VerifiedWebhook value;
        try { value=await webhooks.AuthenticateAsync(store,body,authorization,deadline.Token); }
        catch(OperationCanceledException)when(!ct.IsCancellationRequested){throw new StoreUnavailableException();}
        catch(HttpRequestException){throw new StoreUnavailableException();}
        if(string.IsNullOrEmpty(value.EventId)||value.EventId.Length>200||value.Reference is null||value.Reference.Length>100_000||value.Store!=store)throw new StoreVerificationException();
        if(value.Reference.Length==0)return;
        db.StoreWebhookInbox.Add(new() {Id=Guid.NewGuid(),Store=store,EventId=value.EventId,ReferenceHash=PurchaseProtector.Hash(value.Reference),ProtectedReference=protector.Protect(value.Reference),CreatedAtUtc=clock.GetUtcNow().UtcDateTime,NextAttemptAtUtc=clock.GetUtcNow().UtcDateTime});
        try{await db.SaveChangesAsync(ct);}catch(DbUpdateException ex)when(ex.InnerException is SqlException {Number:2601 or 2627}) {db.ChangeTracker.Clear();}
    }
    public async Task ProcessAsync(CancellationToken ct)
    {
        var now=clock.GetUtcNow().UtcDateTime;var lease=Guid.NewGuid();
        var rows=await db.StoreWebhookInbox.FromSqlInterpolated($"""
            ;WITH candidate AS (SELECT TOP (1) * FROM StoreWebhookInbox WITH(UPDLOCK,READPAST,READCOMMITTEDLOCK)
              WHERE CompletedAtUtc IS NULL AND AttemptCount<10 AND NextAttemptAtUtc<={now} AND (LeaseExpiresAtUtc IS NULL OR LeaseExpiresAtUtc<={now}) ORDER BY NextAttemptAtUtc,Id)
            UPDATE candidate SET LeaseOwner={lease},LeaseExpiresAtUtc={now.AddMinutes(2)},AttemptCount=AttemptCount+1 OUTPUT inserted.*;
            """).AsNoTracking().ToListAsync(ct);
        if(rows.Count==0)
        {
            var stale=await db.UserSubscriptions.AsNoTracking().Where(x=>x.IsStoreVerified&&x.ProtectedEvidence!=null&&x.NextVerificationAtUtc<=now).OrderBy(x=>x.NextVerificationAtUtc).FirstOrDefaultAsync(ct);
            if(stale is not null)
            {
                try {await Refresh(stale,ct);}
                catch(OperationCanceledException)when(ct.IsCancellationRequested){throw;}
                catch(Exception) {await db.UserSubscriptions.Where(x=>x.Id==stale.Id).ExecuteUpdateAsync(s=>s.SetProperty(x=>x.NextVerificationAtUtc,now.AddMinutes(15)),ct);}
            }
            return;
        }
        var row=rows[0];
        try
        {
            var reference=protector.Unprotect(row.ProtectedReference);
            UserSubscription? root;
            if(row.Store=="Apple")root=await db.UserSubscriptions.AsNoTracking().SingleOrDefaultAsync(x=>x.Provider=="Apple"&&x.ExternalSubscriptionId==reference,ct);
            else
            {
                var hash=PurchaseProtector.Hash(reference);var purchase=await db.StorePurchases.AsNoTracking().SingleOrDefaultAsync(x=>x.Store=="Google"&&x.TokenHash==hash,ct);
                root=purchase is null?null:await db.UserSubscriptions.AsNoTracking().SingleAsync(x=>x.Id==purchase.UserSubscriptionId,ct);
            }
            if(root is not null)await Refresh(root,ct);
            await db.StoreWebhookInbox.Where(x=>x.Id==row.Id&&x.LeaseOwner==lease).ExecuteUpdateAsync(s=>s.SetProperty(x=>x.CompletedAtUtc,now).SetProperty(x=>x.ErrorCode,root==null?"Unbound":"Reconciled").SetProperty(x=>x.LeaseOwner,(Guid?)null).SetProperty(x=>x.LeaseExpiresAtUtc,(DateTime?)null),ct);
        }
        catch(OperationCanceledException)when(ct.IsCancellationRequested){throw;}
        catch(Exception)
        {
            await db.StoreWebhookInbox.Where(x=>x.Id==row.Id&&x.LeaseOwner==lease).ExecuteUpdateAsync(s=>s.SetProperty(x=>x.ErrorCode,"ReconcileFailed").SetProperty(x=>x.NextAttemptAtUtc,now.AddSeconds(Math.Min(3600,30*Math.Pow(2,row.AttemptCount-1))))
                .SetProperty(x=>x.LeaseOwner,(Guid?)null).SetProperty(x=>x.LeaseExpiresAtUtc,(DateTime?)null),ct);
        }
    }
    private Task Refresh(UserSubscription snapshot,CancellationToken ct)=>Serialized(snapshot.UserId,async token=>
    {
        var current=await db.UserSubscriptions.AsNoTracking().SingleAsync(x=>x.Id==snapshot.Id,token);
        return await verifier.ReconcileAsync(current,protector.Unprotect(current.ProtectedEvidence!),token);
    },ct);
}
