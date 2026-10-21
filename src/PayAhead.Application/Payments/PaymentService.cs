using FluentValidation;
using FluentValidation.Results;
using PayAhead.Domain;

namespace PayAhead.Application.Payments;

public sealed class PaymentService(IPaymentPlanStore store, ICurrentUser currentUser, TimeProvider clock,
    IValidator<CreatePaymentRequest> createValidator, IValidator<UpdatePaymentRequest> updateValidator,
    IValidator<PaymentListQuery> listValidator) : IPaymentService
{
    private Guid UserId => currentUser.UserId is { } id && id != Guid.Empty ? id : throw new AuthenticationRequiredException();

    public async Task<PaymentResponse> CreateAsync(CreatePaymentRequest request, CancellationToken cancellationToken)
    {
        var userId = UserId;
        await createValidator.ValidateAndThrowAsync(request, cancellationToken);
        if (!await store.IsActiveUserAsync(userId, cancellationToken)) throw new AuthenticationRequiredException();
        var plan = PaymentPlan.Create(userId, Details(request), clock.GetUtcNow().UtcDateTime);
        await store.AddAsync(plan, cancellationToken);
        return PaymentResponse.From(plan);
    }

    public async Task<PagedResult<PaymentResponse>> ListAsync(PaymentListQuery query, CancellationToken cancellationToken)
    {
        var userId = UserId;
        await listValidator.ValidateAndThrowAsync(query, cancellationToken);
        var filter = new PaymentFilter(query.PageNumber, query.PageSize, query.IsActive, query.Category?.Trim(),
            query.CurrencyCode?.Trim().ToUpperInvariant(), query.Search?.Trim());
        var result = await store.ListOwnedAsync(userId, filter, cancellationToken);
        return new(result.Items.Select(PaymentResponse.From).ToArray(), result.PageNumber, result.PageSize, result.TotalCount);
    }

    public async Task<PaymentResponse> GetAsync(Guid id, CancellationToken cancellationToken)
        => PaymentResponse.From(await FindAsync(UserId, id, cancellationToken));

    public async Task<PaymentResponse> UpdateAsync(Guid id, UpdatePaymentRequest request, CancellationToken cancellationToken)
    {
        var userId = UserId;
        await updateValidator.ValidateAndThrowAsync(request, cancellationToken);
        var plan = await FindAsync(userId, id, cancellationToken);
        plan.UpdateDetails(Details(request), clock.GetUtcNow().UtcDateTime);
        await store.UpdateAsync(plan, Convert.FromBase64String(request.RowVersion!), cancellationToken);
        return PaymentResponse.From(plan);
    }

    public async Task DeleteAsync(Guid id, string? rowVersion, CancellationToken cancellationToken)
    {
        var userId = UserId;
        if (!RowVersionToken.IsValid(rowVersion)) throw new ValidationException([
            new ValidationFailure("If-Match", "If-Match başlığında çift tırnak içinde geçerli rowversion gereklidir.")]);
        var plan = await FindAsync(userId, id, cancellationToken);
        await store.DeleteAsync(plan, Convert.FromBase64String(rowVersion!), cancellationToken);
    }

    private async Task<PaymentPlan> FindAsync(Guid userId, Guid id, CancellationToken cancellationToken)
        => id != Guid.Empty ? await store.FindOwnedAsync(userId, id, cancellationToken) ?? throw new ResourceNotFoundException()
            : throw new ResourceNotFoundException();

    private static PaymentPlanDetails Details(PaymentWriteRequest request) => new(request.Name!.Trim(),
        request.Description?.Trim(), request.Amount!.Value, request.CurrencyCode!.Trim().ToUpperInvariant(),
        request.Category!.Trim(), request.Frequency!.Value, request.Interval!.Value, request.StartDate!.Value,
        request.EndDate, request.AnchorDay, request.IsActive);
}
