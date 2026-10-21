using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using PayAhead.Application.Payments;
using PayAhead.Domain;
using PayAhead.Application.Premium;

namespace PayAhead.Infrastructure;

public sealed class PaymentPlanStore(PayAheadDbContext db, IEntitlementService? entitlements = null) : IPaymentPlanStore
{
    private IEntitlementService Policy => entitlements ?? new SqlEntitlementService(db,new(),TimeProvider.System);
    public Task<bool> IsActiveUserAsync(Guid userId, CancellationToken cancellationToken)
        => db.Users.AnyAsync(x => x.Id == userId && x.IsActive, cancellationToken);

    public Task<PaymentPlan?> FindOwnedAsync(Guid userId, Guid id, CancellationToken cancellationToken)
        => db.PaymentPlans.SingleOrDefaultAsync(x => x.UserId == userId && x.Id == id, cancellationToken);

    public async Task<PagedResult<PaymentPlan>> ListOwnedAsync(Guid userId, PaymentFilter filter, CancellationToken cancellationToken)
    {
        var query = db.PaymentPlans.AsNoTracking().Where(x => x.UserId == userId);
        if (filter.IsActive is { } active) query = query.Where(x => x.IsActive == active);
        if (filter.Category is { } category) query = query.Where(x => x.Category == category);
        if (filter.CurrencyCode is { } currency) query = query.Where(x => x.CurrencyCode == currency);
        if (filter.Search is { } search) query = query.Where(x => x.Name.Contains(search));
        var count = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(x => x.CreatedAtUtc).ThenBy(x => x.Id)
            .Skip((filter.PageNumber - 1) * filter.PageSize).Take(filter.PageSize).ToListAsync(cancellationToken);
        return new(items, filter.PageNumber, filter.PageSize, count);
    }

    public async Task AddAsync(PaymentPlan plan, CancellationToken cancellationToken)
    {
        try { await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            await using var tx=await db.Database.BeginTransactionAsync(cancellationToken);
            await Policy.LockUserAsync(plan.UserId,cancellationToken);
            if(plan.IsActive)await Policy.EnsureIncreaseAsync(plan.UserId,LimitResource.PaymentPlans,null,1,cancellationToken);
            db.Entry(plan).State=EntityState.Added;await db.SaveChangesAsync(cancellationToken);await tx.CommitAsync(cancellationToken);
        }); }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 547 })
        {
            throw new ResourceConflictException("Kullanıcı kaydı değişti; işlem tamamlanamadı.");
        }
    }

    public async Task UpdateAsync(PaymentPlan plan, byte[] expectedVersion, CancellationToken cancellationToken)
    {
        await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
        await using var tx=await db.Database.BeginTransactionAsync(cancellationToken);
        await Policy.LockUserAsync(plan.UserId,cancellationToken);
        await EnsureIndependent(plan.Id, cancellationToken);
        var active=await db.PaymentPlans.AsNoTracking().Where(x=>x.Id==plan.Id&&x.UserId==plan.UserId).Select(x=>x.IsActive).SingleAsync(cancellationToken);
        if(!active&&plan.IsActive)await Policy.EnsureIncreaseAsync(plan.UserId,LimitResource.PaymentPlans,null,1,cancellationToken);
        db.Entry(plan).State=EntityState.Modified;
        db.Entry(plan).Property(x => x.RowVersion).OriginalValue = expectedVersion;
        // Aynı içerikli PUT da sürüm karşılaştırmasını veritabanında yapmalıdır.
        db.Entry(plan).Property(x => x.UpdatedAtUtc).IsModified = true;
        await db.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
        });
    }

    public async Task DeleteAsync(PaymentPlan plan, byte[] expectedVersion, CancellationToken cancellationToken)
    {
        await EnsureIndependent(plan.Id, cancellationToken);
        if (!plan.RowVersion.SequenceEqual(expectedVersion))
            throw new ResourceConflictException("Kaynak başka bir işlem tarafından değiştirildi.");
        if (await db.PaymentOccurrences.AnyAsync(x => x.PaymentPlanId == plan.Id, cancellationToken)
            || await db.ReminderRules.AnyAsync(x => x.PaymentPlanId == plan.Id && x.UserId == plan.UserId, cancellationToken))
            throw new ResourceConflictException("İlişkili geçmiş veya hatırlatma içeren ödeme planı silinemez.");
        db.Entry(plan).Property(x => x.RowVersion).OriginalValue = expectedVersion;
        db.PaymentPlans.Remove(plan);
        try { await db.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 547 })
        {
            // Kontrolden sonra eklenmiş bağımlı kaydı SQL FK korur; hiçbir geçmiş kaydı silinmez.
            throw new ResourceConflictException("İlişkili kayıtlar nedeniyle ödeme planı silinemedi.");
        }
    }
    private async Task EnsureIndependent(Guid plan, CancellationToken ct)
    {
        if (await db.TrackedSubscriptions.AnyAsync(x => x.PaymentPlanId == plan, ct))
            throw new ResourceConflictException("Bağlı ödeme planı subscription API üzerinden yönetilmelidir.");
    }
}
