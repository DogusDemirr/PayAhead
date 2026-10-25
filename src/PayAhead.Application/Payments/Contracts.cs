using System.Text.Json.Serialization;
using PayAhead.Domain;

namespace PayAhead.Application.Payments;

public abstract class PaymentWriteRequest : PayAhead.Application.Catalog.CatalogReferenceRequest
{
    public string? Name { get; init; }
    public string? Description { get; init; }
    public decimal? Amount { get; init; }
    public string? CurrencyCode { get; init; }
    public string? Category { get; init; }
    public PaymentFrequency? Frequency { get; init; }
    public int? Interval { get; init; }
    public DateOnly? StartDate { get; init; }
    public DateOnly? EndDate { get; init; }
    public int? AnchorDay { get; init; }
    public bool IsActive { get; init; } = true;
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class CreatePaymentRequest : PaymentWriteRequest;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class UpdatePaymentRequest : PaymentWriteRequest
{
    public string? RowVersion { get; init; }
}

public sealed class PaymentListQuery
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public bool? IsActive { get; init; }
    public string? Category { get; init; }
    public string? CurrencyCode { get; init; }
    public string? Search { get; init; }
}

public sealed record PaymentFilter(int PageNumber, int PageSize, bool? IsActive, string? Category, string? CurrencyCode, string? Search);

public sealed record PaymentResponse(Guid Id, string Name, string? Description, decimal Amount, string CurrencyCode,
    string Category, PaymentFrequency Frequency, int Interval, DateOnly StartDate, DateOnly? EndDate, int? AnchorDay,
    bool IsActive, DateTime CreatedAtUtc, DateTime UpdatedAtUtc, string RowVersion)
{
    public Guid? CatalogCategoryId { get; init; }
    public Guid? CatalogServiceId { get; init; }
    public static PaymentResponse From(PaymentPlan plan) => new(plan.Id, plan.Name, plan.Description, plan.Amount,
        plan.CurrencyCode, plan.Category, plan.Frequency, plan.Interval, plan.StartDate, plan.EndDate, plan.AnchorDay,
        plan.IsActive, DateTime.SpecifyKind(plan.CreatedAtUtc, DateTimeKind.Utc),
        DateTime.SpecifyKind(plan.UpdatedAtUtc, DateTimeKind.Utc), Convert.ToBase64String(plan.RowVersion)) { CatalogCategoryId = plan.CatalogCategoryId, CatalogServiceId = plan.CatalogServiceId };
}

public static class Pagination
{
    public static int TotalPages(int totalCount, int pageSize)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(totalCount);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pageSize);
        return totalCount / pageSize + (totalCount % pageSize == 0 ? 0 : 1);
    }
}

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int PageNumber, int PageSize, int TotalCount)
{
    public int TotalPages => Pagination.TotalPages(TotalCount, PageSize);
}

public sealed class ResourceNotFoundException() : Exception("Ödeme planı bulunamadı.");
public sealed class ResourceConflictException(string message) : Exception(message);
public sealed class AuthenticationRequiredException() : Exception("Geçerli kullanıcı kimliği gereklidir.");

public interface IPaymentPlanStore
{
    Task<bool> IsActiveUserAsync(Guid userId, CancellationToken cancellationToken);
    Task<PaymentPlan?> FindOwnedAsync(Guid userId, Guid id, CancellationToken cancellationToken);
    Task<PagedResult<PaymentPlan>> ListOwnedAsync(Guid userId, PaymentFilter filter, CancellationToken cancellationToken);
    Task AddAsync(PaymentPlan plan, CancellationToken cancellationToken);
    Task UpdateAsync(PaymentPlan plan, byte[] expectedVersion, CancellationToken cancellationToken);
    Task DeleteAsync(PaymentPlan plan, byte[] expectedVersion, CancellationToken cancellationToken);
}

public interface IPaymentService
{
    Task<PaymentResponse> CreateAsync(CreatePaymentRequest request, CancellationToken cancellationToken);
    Task<PagedResult<PaymentResponse>> ListAsync(PaymentListQuery query, CancellationToken cancellationToken);
    Task<PaymentResponse> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<PaymentResponse> UpdateAsync(Guid id, UpdatePaymentRequest request, CancellationToken cancellationToken);
    Task DeleteAsync(Guid id, string? rowVersion, CancellationToken cancellationToken);
}
