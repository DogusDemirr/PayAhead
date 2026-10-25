using FluentValidation;
using FluentValidation.Results;
using PayAhead.Application.Notifications;
using PayAhead.Application.Payments;
using PayAhead.Domain;
using PayAhead.Domain.Recurrence;

namespace PayAhead.Application.Subscriptions;

public sealed class SubscriptionService(ISubscriptionStore store, ICurrentUser current, TimeProvider clock,
    IPaymentRecurrenceCalculator recurrence, IValidator<SubscriptionRequest> validator, IValidator<SubscriptionQuery> queryValidator) : ISubscriptionService
{
    private Guid User => current.UserId ?? throw new AuthenticationRequiredException();
    private async Task<SubscriptionContext> Context(CancellationToken ct)
    {
        var zone = NotificationPolicy.Zone(await store.TimeZoneAsync(User, ct), out var fallback);
        return new(DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(clock.GetUtcNow(), zone).DateTime), zone.Id, fallback);
    }
    public async Task<SubscriptionResponse> CreateAsync(SubscriptionRequest request, CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(request, ct); if (request.RowVersion is not null) Invalid("RowVersion", "Create için rowversion gönderilmez.");
        var context = await Context(ct); return Project(await store.CreateAsync(User, request, clock.GetUtcNow().UtcDateTime, context.Today, ct), context);
    }
    public async Task<SubscriptionResponse> GetAsync(Guid id, CancellationToken ct)
    { var context = await Context(ct); return Project(await store.FindAsync(User, id, ct) ?? throw new ResourceNotFoundException(), context); }
    public async Task<PagedResult<SubscriptionResponse>> ListAsync(SubscriptionQuery query, CancellationToken ct)
    {
        await queryValidator.ValidateAndThrowAsync(query, ct); var context = await Context(ct);
        var rows = (await store.LoadAsync(User, ct)).Select(x => Project(x, context)).AsEnumerable();
        if (query.Status.HasValue) rows = rows.Where(x => x.Status == query.Status);
        if (!string.IsNullOrWhiteSpace(query.ProviderName)) rows = rows.Where(x => string.Equals(x.ProviderName, query.ProviderName.Trim(), StringComparison.OrdinalIgnoreCase));
        if (query.CurrencyCode is not null) rows = rows.Where(x => x.CurrencyCode == query.CurrencyCode.Trim().ToUpperInvariant());
        if (!string.IsNullOrWhiteSpace(query.Search)) rows = rows.Where(x => x.Name.Contains(query.Search.Trim(), StringComparison.OrdinalIgnoreCase) || x.ProviderName.Contains(query.Search.Trim(), StringComparison.OrdinalIgnoreCase));
        var ordered = rows.OrderByDescending(x => x.CreatedAtUtc).ThenBy(x => x.Id).ToArray();
        return new(ordered.Skip((query.PageNumber - 1) * query.PageSize).Take(query.PageSize).ToArray(), query.PageNumber, query.PageSize, ordered.Length);
    }
    public async Task<SubscriptionResponse> UpdateAsync(Guid id, SubscriptionRequest request, CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(request, ct); Version(request.RowVersion); var context = await Context(ct);
        return Project(await store.UpdateAsync(User, id, request, clock.GetUtcNow().UtcDateTime, context.Today, ct), context);
    }
    public async Task<SubscriptionResponse> CancelAsync(Guid id, CancelSubscriptionRequest request, CancellationToken ct)
    {
        Version(request.RowVersion); var context = await Context(ct);
        if (request.AccessEndsOn is null || request.AccessEndsOn < context.Today) Invalid("AccessEndsOn", "Erişim bitişi bugün veya sonrası olmalıdır.");
        return Project(await store.CancelAsync(User, id, request, clock.GetUtcNow().UtcDateTime, context.Today, ct), context);
    }
    public Task DeleteAsync(Guid id, string? version, CancellationToken ct) => store.DeleteAsync(User, id, Version(version), clock.GetUtcNow().UtcDateTime, ct);
    public async Task<IReadOnlyList<ExpiringTrialResponse>> TrialsAsync(ExpiringTrialsQuery query, CancellationToken ct)
    {
        if (query.WithinDays is < 0 or > 365) Invalid("WithinDays", "WithinDays 0–365 olmalıdır.");
        var context = await Context(ct); return (await store.LoadAsync(User, ct)).Select(x => Project(x, context))
            .Where(x => x.Status == SubscriptionStatus.Trial && x.TrialStartDate <= context.Today && x.TrialDaysRemaining <= query.WithinDays)
            .OrderBy(x => x.TrialEndDate).ThenBy(x => x.Id).Select(x => new ExpiringTrialResponse(x.Id, x.Name, x.ProviderName, x.TrialEndDate!.Value, x.TrialDaysRemaining!.Value, x.FirstBillingDate, x.Amount, x.CurrencyCode)).ToArray();
    }
    public async Task<SubscriptionSummary> SummaryAsync(CancellationToken ct)
    {
        var context = await Context(ct); var rows = (await store.LoadAsync(User, ct)).Select(x => Project(x, context)).ToArray();
        var totals = rows.Where(x => x.Status is SubscriptionStatus.Active or SubscriptionStatus.Trial && x.AutoRenew)
            .GroupBy(x => x.CurrencyCode).OrderBy(x => x.Key, StringComparer.Ordinal)
            .Select(g => new MonthlyEquivalentTotal(g.Key, decimal.Round(g.Sum(x => MonthlyEquivalent(x.Amount, x.BillingFrequency, x.BillingInterval)), 4, MidpointRounding.ToEven))).ToArray();
        return new(rows.Length, rows.Count(x => x.Status == SubscriptionStatus.Active), rows.Count(x => x.Status == SubscriptionStatus.Trial), rows.Count(x => x.Status == SubscriptionStatus.Cancelled), rows.Count(x => x.Status == SubscriptionStatus.Expired), totals,
            rows.Count(x => x.Status == SubscriptionStatus.Trial && x.TrialStartDate <= context.Today && x.TrialDaysRemaining <= 7), rows.Where(x => x.NextBillingDate.HasValue).OrderBy(x => x.NextBillingDate).ThenBy(x => x.Id).FirstOrDefault(), context);
    }
    public static decimal MonthlyEquivalent(decimal amount, PaymentFrequency frequency, int interval)
        => frequency switch { PaymentFrequency.Monthly => amount / interval, PaymentFrequency.Yearly => amount / (12m * interval), PaymentFrequency.Weekly => amount * 52m / (12m * interval), PaymentFrequency.Daily => amount * 365m / (12m * interval), _ => throw new DomainRuleException("Abonelik sıklığı geçersiz.") };
    private SubscriptionResponse Project(SubscriptionRecord row, SubscriptionContext context)
    {
        var s = row.Subscription; var p = row.Plan; var status = s.EffectiveStatus(context.Today);
        var next = status is SubscriptionStatus.Trial or SubscriptionStatus.Active && s.AutoRenew ? recurrence.GetNextOccurrence(p, context.Today) : null;
        return new(s.Id, p.Id, p.Name, p.Description, s.ProviderName, p.Amount, p.CurrencyCode, p.Frequency, p.Interval, p.AnchorDay, s.StartDate, p.StartDate, next, s.TrialStartDate, s.TrialEndDate, s.TrialDaysRemaining(context.Today), status, s.AutoRenew, s.EnableTrialReminders,
            Utc(s.CancellationRequestedAtUtc), Utc(s.CancelledAtUtc), s.AccessEndsOn, DateTime.SpecifyKind(s.CreatedAtUtc, DateTimeKind.Utc), DateTime.SpecifyKind(s.UpdatedAtUtc, DateTimeKind.Utc), Convert.ToBase64String(s.RowVersion), context) { CatalogCategoryId = p.CatalogCategoryId, CatalogServiceId = p.CatalogServiceId, Category = p.Category };
    }
    private static DateTime? Utc(DateTime? value) => value is { } date ? DateTime.SpecifyKind(date, DateTimeKind.Utc) : null;
    private static byte[] Version(string? version)
    { if (!RowVersionToken.IsValid(version)) Invalid("RowVersion", "Geçerli rowversion gereklidir."); return Convert.FromBase64String(version!); }
    private static void Invalid(string field, string message) => throw new ValidationException([new ValidationFailure(field, message)]);
}
