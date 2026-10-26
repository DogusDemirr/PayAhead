using System.Globalization;
using System.Text;
using System.Text.Json.Serialization;
using FluentValidation;
using PayAhead.Application.Payments;
using PayAhead.Domain;

namespace PayAhead.Application.Catalog;

// Omitted fields in old PUT clients preserve links; explicit null/null detaches them.
public abstract class CatalogReferenceRequest
{
    private Guid? category, service;
    public Guid? CatalogCategoryId { get => category; init { category = value; CatalogSelectionSpecified = true; } }
    public Guid? CatalogServiceId { get => service; init { service = value; CatalogSelectionSpecified = true; } }
    [JsonIgnore] public bool CatalogSelectionSpecified { get; private set; }
}
public sealed record CatalogCategoryResponse(Guid Id, Guid? ParentId, string Code, string NameTr, string NameEn, string IconKey, int SortOrder, bool IsActive, CatalogApplicability ApplicableTo)
{
    public string DisplayName { get; init; } = NameEn;
    public string CultureCode { get; init; } = "en";
    public bool UsedLanguageFallback { get; init; }
    public static CatalogCategoryResponse From(CatalogCategory c) => new(c.Id, c.ParentId, c.Code, c.NameTr, c.NameEn, c.IconKey, c.SortOrder, c.IsActive, c.ApplicableTo);
}
public sealed record CatalogServiceResponse(Guid Id, string Code, string DisplayName, Guid CategoryId, IReadOnlyList<Guid> CategoryIds, string? LogoKey, string SearchKeywords, int SortOrder, bool IsPopular, bool IsActive, CatalogApplicability ApplicableTo, PaymentFrequency? DefaultRecurrenceType, string? CountryCode);
public sealed class CatalogQuery
{
    public string? Locale { get; init; }
    public Guid? CategoryId { get; init; }
    public CatalogApplicability? ApplicableTo { get; init; }
    public string? Search { get; init; }
    public bool? Popular { get; init; }
    public string? CountryCode { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 30;
}
public sealed class CatalogQueryValidator : AbstractValidator<CatalogQuery>
{
    public CatalogQueryValidator()
    {
        RuleFor(x => x.Locale).Must(x => x is null || PayAhead.Application.Localization.SupportedLanguages.Contains(x));
        RuleFor(x => x.CategoryId).Must(x => x != Guid.Empty);
        RuleFor(x => x.ApplicableTo).IsInEnum().When(x => x.ApplicableTo.HasValue);
        RuleFor(x => x.Search).MaximumLength(100);
        RuleFor(x => x.CountryCode).Must(x => x is null || x.Length == 2 && x.All(c => c is >= 'A' and <= 'Z'));
        RuleFor(x => x.PageNumber).InclusiveBetween(1, 10000); RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
public static class CatalogSearch
{
    public static string Normalize(string? value)
    {
        var text = (value ?? "").Replace('ı', 'i').Normalize(NormalizationForm.FormD);
        return string.Concat(text.Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)).ToLowerInvariant().Trim();
    }
    public static bool Applies(CatalogApplicability value, CatalogApplicability? target) => target is null or CatalogApplicability.Both || value == CatalogApplicability.Both || value == target;
}
public interface ICatalogStore
{
    Task<IReadOnlyList<CatalogCategoryResponse>> CategoriesAsync(CatalogQuery query, CancellationToken ct);
    Task<CatalogCategoryResponse?> CategoryAsync(Guid id, CancellationToken ct, string? locale = null);
    Task<PagedResult<CatalogServiceResponse>> ServicesAsync(CatalogQuery query, CancellationToken ct);
    Task<CatalogServiceResponse?> ServiceAsync(Guid id, CancellationToken ct, string? locale = null);
}
public sealed class CatalogReader(ICatalogStore store, IValidator<CatalogQuery> validator)
{
    public async Task<IReadOnlyList<CatalogCategoryResponse>> CategoriesAsync(CatalogQuery query, CancellationToken ct) { await validator.ValidateAndThrowAsync(query, ct); return await store.CategoriesAsync(query, ct); }
    public async Task<PagedResult<CatalogServiceResponse>> ServicesAsync(CatalogQuery query, CancellationToken ct) { await validator.ValidateAndThrowAsync(query, ct); return await store.ServicesAsync(query, ct); }
    public async Task<CatalogCategoryResponse> CategoryAsync(Guid id, CancellationToken ct, string? locale = null)
    {
        await validator.ValidateAndThrowAsync(new CatalogQuery { Locale = locale }, ct);
        return await store.CategoryAsync(id, ct, locale) ?? throw new ResourceNotFoundException();
    }
    public async Task<CatalogServiceResponse> ServiceAsync(Guid id, CancellationToken ct, string? locale = null) { await validator.ValidateAndThrowAsync(new CatalogQuery { Locale = locale }, ct); return await store.ServiceAsync(id, ct, locale) ?? throw new ResourceNotFoundException(); }
}
