using FluentValidation;
using PayAhead.Application.Payments;
using PayAhead.Domain;
using PayAhead.Domain.Recurrence;

namespace PayAhead.Application.Occurrences;

public sealed class OccurrenceService(IOccurrenceStore store, ICurrentUser currentUser, IPaymentRecurrenceCalculator calculator,
    TimeProvider clock, IValidator<SetOccurrenceStatusRequest> statusValidator, IValidator<OccurrenceListQuery> listValidator)
    : IOccurrenceService
{
    private Guid UserId => currentUser.UserId is { } id && id != Guid.Empty ? id : throw new AuthenticationRequiredException();

    public async Task<OccurrenceResponse> GetAsync(Guid paymentId, DateOnly dueDate, CancellationToken cancellationToken)
    {
        var snapshot = await store.ReadOwnedAsync(UserId, paymentId, dueDate, cancellationToken);
        if (snapshot.Occurrence is null)
        {
            if (!snapshot.Plan.IsActive) throw new ResourceNotFoundException();
            OccurrenceScheduleRules.EnsureValidDate(snapshot.Plan, dueDate, calculator);
        }
        return OccurrenceResponse.From(paymentId, dueDate, snapshot.Occurrence);
    }

    public async Task<PagedResult<OccurrenceResponse>> ListAsync(Guid paymentId, OccurrenceListQuery query, CancellationToken cancellationToken)
    {
        var userId = UserId;
        await listValidator.ValidateAndThrowAsync(query, cancellationToken);
        var result = await store.ListOwnedAsync(userId, paymentId, query, cancellationToken);
        return new(result.Items.Select(x => OccurrenceResponse.From(paymentId, x.DueDate, x)).ToArray(),
            result.PageNumber, result.PageSize, result.TotalCount);
    }

    public async Task<OccurrenceResponse> SetStatusAsync(Guid paymentId, DateOnly dueDate, SetOccurrenceStatusRequest request,
        CancellationToken cancellationToken)
    {
        var userId = UserId;
        await statusValidator.ValidateAndThrowAsync(request, cancellationToken);
        var snapshot = await store.MutateOwnedAsync(userId, paymentId, dueDate, (plan, occurrence) =>
        {
            if (!plan.IsActive) throw new ResourceConflictException("Pasif ödeme planının geçmişi salt okunurdur.");
            OccurrenceScheduleRules.EnsureValidDate(plan, dueDate, calculator);
            var status = request.Status!.Value;
            if (occurrence is null)
            {
                if (status == OccurrenceStatus.Pending) return new(null, false);
                if (request.RowVersion is not null) throw Conflict();
                return new(PaymentOccurrence.Create(paymentId, dueDate, status, request.ActualAmount, clock.GetUtcNow().UtcDateTime), true);
            }
            // Aynı durum/tutar tekrarında rowversion ve PaidAtUtc değişmez.
            if (status != OccurrenceStatus.Pending && occurrence.Status == status && occurrence.ActualAmount == request.ActualAmount)
                return new(occurrence, false);
            if (request.RowVersion is null || !occurrence.RowVersion.SequenceEqual(Convert.FromBase64String(request.RowVersion))) throw Conflict();
            if (status == OccurrenceStatus.Pending) return new(null, true);
            return new(occurrence, occurrence.ChangeStatus(status, request.ActualAmount, clock.GetUtcNow().UtcDateTime));
        }, cancellationToken);
        return OccurrenceResponse.From(paymentId, dueDate, snapshot.Occurrence);
    }

    private static ResourceConflictException Conflict() => new("Occurrence değişti; güncel rowVersion değerini okuyup yeniden deneyin.");
}
