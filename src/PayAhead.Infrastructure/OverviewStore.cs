using Microsoft.EntityFrameworkCore;
using PayAhead.Application.Overview;
using PayAhead.Domain;

namespace PayAhead.Infrastructure;

public sealed class OverviewStore(PayAheadDbContext db) : IOverviewStore
{
    public Task<string?> GetTimeZoneIdAsync(Guid userId, CancellationToken cancellationToken)
        => db.UserSettings.AsNoTracking().Where(x => x.UserId == userId).Select(x => x.TimeZoneId).SingleOrDefaultAsync(cancellationToken);

    public async Task<OverviewSnapshot> LoadAsync(Guid userId, DateOnly fromDate, DateOnly toDate, string? currencyCode, bool includeActiveCount, CancellationToken cancellationToken)
    {
        var owned = db.PaymentPlans.AsNoTracking().Where(x => x.UserId == userId);
        var activeCount = includeActiveCount ? await owned.CountAsync(x => x.IsActive, cancellationToken) : 0;
        if (currencyCode is not null) owned = owned.Where(x => x.CurrencyCode == currencyCode);
        var eligible = owned.Where(plan => plan.IsActive && plan.StartDate <= toDate && (plan.EndDate == null || plan.EndDate >= fromDate)
            || db.PaymentOccurrences.Any(occurrence => occurrence.PaymentPlanId == plan.Id && occurrence.DueDate >= fromDate && occurrence.DueDate <= toDate
                && (occurrence.Status == OccurrenceStatus.Paid || occurrence.Status == OccurrenceStatus.Skipped)));
        var plans = await eligible.OrderBy(x => x.Id).Take(OverviewLimits.MaximumPlans + 1).ToListAsync(cancellationToken);
        if (plans.Count > OverviewLimits.MaximumPlans) throw new DomainRuleException("En fazla 1000 ilgili plan okunabilir; aralığı veya para birimini daraltın.");
        if (plans.Count == 0) return new(plans, [], activeCount);
        var planIds = plans.Select(x => x.Id).ToArray();
        var occurrences = await (from occurrence in db.PaymentOccurrences.AsNoTracking()
                                 join plan in owned on occurrence.PaymentPlanId equals plan.Id
                                 where planIds.Contains(plan.Id) && occurrence.DueDate >= fromDate && occurrence.DueDate <= toDate
                                 select occurrence).OrderBy(x => x.PaymentPlanId).ThenBy(x => x.DueDate)
            .Take(OverviewLimits.MaximumResults + 1).ToListAsync(cancellationToken);
        if (occurrences.Count > OverviewLimits.MaximumResults) throw new DomainRuleException("En fazla 50000 kayıt okunabilir; tarih aralığını daraltın.");
        return new(plans, occurrences, activeCount);
    }
}
