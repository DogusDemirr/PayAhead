namespace PayAhead.Domain;

public enum CatalogApplicability { Payment, Subscription, Both }
public sealed class CatalogCategory
{
    public Guid Id { get; set; }
    public Guid? ParentId { get; set; }
    public string Code { get; set; } = "";
    public string NameTr { get; set; } = "";
    public string NameEn { get; set; } = "";
    public string IconKey { get; set; } = "other";
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public CatalogApplicability ApplicableTo { get; set; } = CatalogApplicability.Both;
    public string SearchText { get; set; } = "";
}
public sealed class CatalogService
{
    public Guid Id { get; set; }
    public string Code { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public Guid CategoryId { get; set; }
    public string? LogoKey { get; set; }
    public string SearchKeywords { get; set; } = "";
    public string SearchText { get; set; } = "";
    public int SortOrder { get; set; }
    public bool IsPopular { get; set; }
    public bool IsActive { get; set; } = true;
    public CatalogApplicability ApplicableTo { get; set; } = CatalogApplicability.Both;
    public PaymentFrequency? DefaultRecurrenceType { get; set; }
    public string? CountryCode { get; set; }
}
public sealed class CatalogServiceCategory
{
    public Guid ServiceId { get; set; }
    public Guid CategoryId { get; set; }
}
