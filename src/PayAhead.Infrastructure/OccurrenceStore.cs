using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using PayAhead.Application.Occurrences;
using PayAhead.Application.Payments;
using PayAhead.Domain;

namespace PayAhead.Infrastructure;

public sealed class OccurrenceStore(PayAheadDbContext db) : IOccurrenceStore
{
    private IQueryable<PaymentOccurrence> OwnedOccurrences(Guid userId, Guid paymentId)
        => from occurrence in db.PaymentOccurrences
           join plan in db.PaymentPlans on occurrence.PaymentPlanId equals plan.Id
           where plan.Id == paymentId && plan.UserId == userId
           select occurrence;

    public async Task<OccurrenceSnapshot> ReadOwnedAsync(Guid userId, Guid paymentId, DateOnly date, CancellationToken cancellationToken)
    {
        var plan = await db.PaymentPlans.AsNoTracking().SingleOrDefaultAsync(x => x.Id == paymentId && x.UserId == userId, cancellationToken)
            ?? throw new ResourceNotFoundException();
        var occurrence = await OwnedOccurrences(userId, paymentId).AsNoTracking().SingleOrDefaultAsync(x => x.DueDate == date, cancellationToken);
        return new(plan, occurrence);
    }

    public async Task<PagedResult<PaymentOccurrence>> ListOwnedAsync(Guid userId, Guid paymentId, OccurrenceListQuery query,
        CancellationToken cancellationToken)
    {
        if (!await db.PaymentPlans.AnyAsync(x => x.Id == paymentId && x.UserId == userId, cancellationToken)) throw new ResourceNotFoundException();
        var records = OwnedOccurrences(userId, paymentId).AsNoTracking();
        if (query.FromDate is { } from) records = records.Where(x => x.DueDate >= from);
        if (query.ToDate is { } to) records = records.Where(x => x.DueDate <= to);
        if (query.Status is { } status) records = records.Where(x => x.Status == status);
        var count = await records.CountAsync(cancellationToken);
        var items = await records.OrderByDescending(x => x.DueDate).ThenBy(x => x.Id)
            .Skip((query.PageNumber - 1) * query.PageSize).Take(query.PageSize).ToListAsync(cancellationToken);
        return new(items, query.PageNumber, query.PageSize, count);
    }

    public async Task<OccurrenceSnapshot> MutateOwnedAsync(Guid userId, Guid paymentId, DateOnly date,
        Func<PaymentPlan, PaymentOccurrence?, OccurrenceMutation> mutate, CancellationToken cancellationToken)
    {
        try
        {
            return await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
            {
                // Bu işlem kendi unit of work'üdür; başarısız denemelerin tracked durumunu tekrar kullanmaz.
                db.ChangeTracker.Clear();
                await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
                var plan = await db.PaymentPlans.FromSqlInterpolated($"SELECT * FROM [PaymentPlans] WITH (UPDLOCK, HOLDLOCK) WHERE [Id] = {paymentId} AND [UserId] = {userId}")
                    .SingleOrDefaultAsync(cancellationToken) ?? throw new ResourceNotFoundException();
                var existing = await db.PaymentOccurrences.SingleOrDefaultAsync(x => x.PaymentPlanId == plan.Id && x.DueDate == date, cancellationToken);
                var change = mutate(plan, existing);
                if (change.HasChanges)
                {
                    if (change.Occurrence is null)
                    {
                        if (existing is not null) db.PaymentOccurrences.Remove(existing);
                    }
                    else if (existing is null) db.PaymentOccurrences.Add(change.Occurrence);
                    await db.SaveChangesAsync(cancellationToken);
                }
                await transaction.CommitAsync(cancellationToken);
                return new OccurrenceSnapshot(plan, change.Occurrence);
            });
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 or 547 })
        {
            throw new ResourceConflictException("Occurrence veya bağlı plan değişti; işlem uygulanamadı.");
        }
        catch (SqlException ex) when (ex.Number is 1205 or 1222)
        {
            throw new ResourceConflictException("Eşzamanlı işlem tamamlanamadı; güncel kaydı okuyup yeniden deneyin.");
        }
    }
}
