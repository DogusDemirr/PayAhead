using System.Text.Json.Serialization;
using PayAhead.Application.Payments;
using PayAhead.Domain;

namespace PayAhead.Application.Occurrences;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class SetOccurrenceStatusRequest
{
    public OccurrenceStatus? Status { get; init; }
    public decimal? ActualAmount { get; init; }
    public string? RowVersion { get; init; }
}

public sealed class OccurrenceListQuery
{
    public DateOnly? FromDate { get; init; }
    public DateOnly? ToDate { get; init; }
    public OccurrenceStatus? Status { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

public sealed record OccurrenceResponse(Guid? Id, Guid PaymentPlanId, DateOnly DueDate, OccurrenceStatus Status,
    decimal? ActualAmount, DateTime? PaidAtUtc, DateTime? CreatedAtUtc, string? RowVersion, bool IsRecorded)
{
    public static OccurrenceResponse From(Guid planId, DateOnly date, PaymentOccurrence? occurrence) => occurrence is null
        ? new(null, planId, date, OccurrenceStatus.Pending, null, null, null, null, false)
        : new(occurrence.Id, planId, date, occurrence.Status, occurrence.ActualAmount,
            occurrence.PaidAtUtc is { } paid ? DateTime.SpecifyKind(paid, DateTimeKind.Utc) : null,
            DateTime.SpecifyKind(occurrence.CreatedAtUtc, DateTimeKind.Utc), Convert.ToBase64String(occurrence.RowVersion), true);
}

public sealed record OccurrenceSnapshot(PaymentPlan Plan, PaymentOccurrence? Occurrence);
public sealed record OccurrenceMutation(PaymentOccurrence? Occurrence, bool HasChanges);

public interface IOccurrenceStore
{
    Task<OccurrenceSnapshot> ReadOwnedAsync(Guid userId, Guid paymentId, DateOnly date, CancellationToken cancellationToken);
    Task<PagedResult<PaymentOccurrence>> ListOwnedAsync(Guid userId, Guid paymentId, OccurrenceListQuery query, CancellationToken cancellationToken);
    // Callback sahipliği doğrulanmış ve kilitli plan/occurrence üzerinde, aynı transaction içinde çalışır.
    Task<OccurrenceSnapshot> MutateOwnedAsync(Guid userId, Guid paymentId, DateOnly date,
        Func<PaymentPlan, PaymentOccurrence?, OccurrenceMutation> mutate, CancellationToken cancellationToken);
}

public interface IOccurrenceService
{
    Task<OccurrenceResponse> GetAsync(Guid paymentId, DateOnly dueDate, CancellationToken cancellationToken);
    Task<PagedResult<OccurrenceResponse>> ListAsync(Guid paymentId, OccurrenceListQuery query, CancellationToken cancellationToken);
    Task<OccurrenceResponse> SetStatusAsync(Guid paymentId, DateOnly dueDate, SetOccurrenceStatusRequest request, CancellationToken cancellationToken);
}
