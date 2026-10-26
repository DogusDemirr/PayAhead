namespace PayAhead.Domain;

public sealed class CatalogCategoryTranslation
{
    public Guid CategoryId { get; set; }
    public string CultureCode { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string SearchText { get; set; } = "";
}
