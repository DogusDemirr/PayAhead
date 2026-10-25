using FluentValidation;
using FluentValidation.Results;
using Microsoft.EntityFrameworkCore;
using PayAhead.Application.Catalog;
using PayAhead.Application.Payments;
using PayAhead.Domain;
namespace PayAhead.Infrastructure;

public sealed class CatalogStore(PayAheadDbContext db) : ICatalogStore
{
    public async Task<IReadOnlyList<CatalogCategoryResponse>> CategoriesAsync(CatalogQuery query, CancellationToken ct)
    {
        var rows = db.CatalogCategories.AsNoTracking().Where(x => x.IsActive);
        if (query.ApplicableTo is { } target && target != CatalogApplicability.Both) rows = rows.Where(x => x.ApplicableTo == target || x.ApplicableTo == CatalogApplicability.Both);
        if (!string.IsNullOrWhiteSpace(query.Search)) { var text = CatalogSearch.Normalize(query.Search); var matches = await CategoryScope(null, text, ct); rows = rows.Where(x => matches.Contains(x.Id)); }
        if (query.CategoryId is { } parent) rows = rows.Where(x => x.ParentId == parent);
        return (await rows.OrderBy(x => x.SortOrder).ThenBy(x => x.Code).Take(250).ToArrayAsync(ct)).Select(CatalogCategoryResponse.From).ToArray();
    }
    public async Task<CatalogCategoryResponse?> CategoryAsync(Guid id, CancellationToken ct)
    { var c = await db.CatalogCategories.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct); return c is null ? null : CatalogCategoryResponse.From(c); }
    public async Task<PagedResult<CatalogServiceResponse>> ServicesAsync(CatalogQuery query, CancellationToken ct)
    {
        var rows = db.CatalogServices.AsNoTracking().Where(x => x.IsActive && db.CatalogCategories.Any(c => c.Id == x.CategoryId && c.IsActive));
        if (query.ApplicableTo is { } target && target != CatalogApplicability.Both) rows = rows.Where(x => x.ApplicableTo == target || x.ApplicableTo == CatalogApplicability.Both);
        if (!string.IsNullOrWhiteSpace(query.Search)) { var text = CatalogSearch.Normalize(query.Search); var matches = await CategoryScope(null, text, ct); rows = rows.Where(x => x.SearchText.Contains(text) || db.CatalogServiceCategories.Any(c => c.ServiceId == x.Id && matches.Contains(c.CategoryId))); }
        if (query.Popular is { } popular) rows = rows.Where(x => x.IsPopular == popular);
        if (query.CountryCode is { } country) rows = rows.Where(x => x.CountryCode == null || x.CountryCode == country);
        if (query.CategoryId is { } category) { var scope = await CategoryScope(category, null, ct); rows = rows.Where(x => db.CatalogServiceCategories.Any(c => c.ServiceId == x.Id && scope.Contains(c.CategoryId))); }
        var count = await rows.CountAsync(ct); var page = await rows.OrderBy(x => x.SortOrder).ThenBy(x => x.Code).Skip((query.PageNumber - 1) * query.PageSize).Take(query.PageSize).ToArrayAsync(ct);
        return new(await Project(page, ct), query.PageNumber, query.PageSize, count);
    }
    public async Task<CatalogServiceResponse?> ServiceAsync(Guid id, CancellationToken ct)
    { var s = await db.CatalogServices.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct); return s is null ? null : (await Project([s], ct))[0]; }
    private async Task<Guid[]> CategoryScope(Guid? root, string? search, CancellationToken ct)
    {
        var categories = await db.CatalogCategories.AsNoTracking().Where(x => x.IsActive).Select(x => new { x.Id, x.ParentId, x.SearchText }).Take(250).ToArrayAsync(ct);
        var ids = categories.Where(x => root.HasValue ? x.Id == root : x.SearchText.Contains(search!, StringComparison.Ordinal)).Select(x => x.Id).ToHashSet();
        // Bounded fixed point also terminates if operator data accidentally contains a cycle.
        while (true) { var added = false; foreach (var c in categories) if (c.ParentId is { } p && ids.Contains(p)) added |= ids.Add(c.Id); if (!added) break; }
        return ids.ToArray();
    }
    private async Task<CatalogServiceResponse[]> Project(CatalogService[] services, CancellationToken ct)
    {
        var ids = services.Select(x => x.Id).ToArray(); var links = await db.CatalogServiceCategories.AsNoTracking().Where(x => ids.Contains(x.ServiceId)).ToArrayAsync(ct);
        return services.Select(s => new CatalogServiceResponse(s.Id, s.Code, s.DisplayName, s.CategoryId, links.Where(x => x.ServiceId == s.Id).Select(x => x.CategoryId).Order().ToArray(), s.LogoKey, s.SearchKeywords, s.SortOrder, s.IsPopular, s.IsActive, s.ApplicableTo, s.DefaultRecurrenceType, s.CountryCode)).ToArray();
    }
    public async Task<CatalogCategory?> ValidateSelectionAsync(Guid? categoryId, Guid? serviceId, CatalogApplicability target, bool unchanged, CancellationToken ct)
    {
        if (categoryId is null && serviceId is null) return null;
        if (categoryId is null) Invalid("CatalogCategoryId", "Hizmet için kategori seçilmelidir.");
        var category = await db.CatalogCategories.AsNoTracking().SingleOrDefaultAsync(x => x.Id == categoryId, ct);
        if (category is null || !CatalogSearch.Applies(category.ApplicableTo, target) || !unchanged && !category.IsActive) Invalid("CatalogCategoryId", "Kategori bulunamadı, pasif veya bu kayıt türüne uygun değil.");
        if (serviceId is { } id)
        {
            var service = await db.CatalogServices.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct);
            if (service is null || !CatalogSearch.Applies(service.ApplicableTo, target) || !unchanged && !service.IsActive || !await db.CatalogServiceCategories.AnyAsync(x => x.ServiceId == id && x.CategoryId == categoryId, ct)) Invalid("CatalogServiceId", "Hizmet bulunamadı, pasif veya kategori/kayıt türüne uygun değil.");
        }
        return category;
    }
    private static void Invalid(string field, string message) => throw new ValidationException([new ValidationFailure(field, message)]);
}
