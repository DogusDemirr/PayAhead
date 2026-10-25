using System.Text.Json;
using PayAhead.Application.Catalog;
using PayAhead.Application.Payments;
using PayAhead.Application.Subscriptions;
using PayAhead.Domain;
using PayAhead.Infrastructure;
using Xunit;
namespace PayAhead.UnitTests;

public sealed class CatalogTests
{
    [Fact] public void Every_main_category_has_children_and_all_children_are_reachable_in_their_mode()
    {
        var categories = CatalogSeed.Categories;
        foreach (var parent in categories.Where(x => x.ParentId is null || x.ParentId == CatalogSeed.Id("digital")))
            Assert.Contains(categories, x => x.ParentId == parent.Id);
        foreach (var child in categories.Where(x => x.ParentId.HasValue))
        {
            var parent = Assert.Single(categories, x => x.Id == child.ParentId); Assert.True(parent.IsActive);
            foreach (var mode in new[] { CatalogApplicability.Payment, CatalogApplicability.Subscription })
                if (CatalogSearch.Applies(child.ApplicableTo, mode)) Assert.True(CatalogSearch.Applies(parent.ApplicableTo, mode));
        }
        Assert.Equal(10, categories.Count(x => x.ParentId == CatalogSeed.Id("news")));
        foreach (var link in CatalogSeed.Links)
        {
            var service = Assert.Single(CatalogSeed.Services, x => x.Id == link.ServiceId);
            var category = Assert.Single(categories, x => x.Id == link.CategoryId);
            Assert.True(service.ApplicableTo == category.ApplicableTo || service.ApplicableTo == CatalogApplicability.Both || category.ApplicableTo == CatalogApplicability.Both);
        }
    }
    [Fact] public void Permanent_codes_ids_and_tree_are_unique_and_acyclic()
    {
        var categories = CatalogSeed.Categories; Assert.Equal(116, categories.Count);
        Assert.Equal(categories.Count, categories.Select(x => x.Id).Distinct().Count()); Assert.Equal(categories.Count, categories.Select(x => x.Code).Distinct().Count());
        foreach (var category in categories)
        {
            Assert.Equal(CatalogSeed.Id(category.Code), category.Id); var visited = new HashSet<Guid>(); var current = category;
            while (current.ParentId is { } parent) { Assert.True(visited.Add(current.Id)); current = Assert.Single(categories, x => x.Id == parent); }
        }
        Assert.Equal(23, categories.Count(x => x.Code.StartsWith("transport.") && x.Code != "transport.fuel" && x.Code != "transport.service"));
    }
    [Theory]
    [InlineData("ARAÇ", "arac")][InlineData("İnternet", "internet")][InlineData("DOĞALGAZ", "dogalgaz")][InlineData("IŞIK", "isik")]
    public void Search_normalizes_Turkish_variants(string source, string expected) => Assert.Equal(expected, CatalogSearch.Normalize(source));
    [Fact] public void Service_catalog_is_separate_from_private_finance_and_membership()
    {
        Assert.Equal(CatalogSeed.Services.Count, CatalogSeed.Services.Select(x => x.Code).Distinct().Count());
        foreach (var s in CatalogSeed.Services) { Assert.Contains(CatalogSeed.Links, x => x.ServiceId == s.Id && x.CategoryId == s.CategoryId); Assert.Null(s.LogoKey); Assert.Contains(CatalogSeed.Categories, x => x.Id == s.CategoryId); }
        var shell = Assert.Single(CatalogSeed.Services, x => x.DisplayName == "Shell"); Assert.Equal(CatalogApplicability.Payment, shell.ApplicableTo); Assert.Equal(PaymentFrequency.Once, shell.DefaultRecurrenceType); Assert.Equal(4, CatalogSeed.Links.Count(x => x.ServiceId == shell.Id));
        Assert.False(Assert.Single(CatalogSeed.Services, x => x.Code == "apple-tv").IsActive); Assert.False(Assert.Single(CatalogSeed.Services, x => x.Code == "bp").IsActive);
    }
    [Fact] public void Omitted_PUT_catalog_fields_preserve_links_but_explicit_null_detaches()
    {
        var legacy = JsonSerializer.Deserialize<UpdatePaymentRequest>("{}")!; Assert.False(legacy.CatalogSelectionSpecified);
        var clear = JsonSerializer.Deserialize<UpdatePaymentRequest>("{\"CatalogCategoryId\":null,\"CatalogServiceId\":null}")!; Assert.True(clear.CatalogSelectionSpecified);
        Assert.False(JsonSerializer.Deserialize<SubscriptionRequest>("{}")!.CatalogSelectionSpecified);
    }
    [Theory]
    [InlineData(0, 20)][InlineData(1, 0)][InlineData(1, 101)]
    public void Catalog_query_is_bounded(int page, int size) => Assert.False(new CatalogQueryValidator().Validate(new CatalogQuery { PageNumber = page, PageSize = size }).IsValid);
    [Fact] public void Invalid_query_and_applicability_are_rejected()
    {
        var v = new CatalogQueryValidator(); Assert.False(v.Validate(new CatalogQuery { Search = new string('x', 101) }).IsValid); Assert.False(v.Validate(new CatalogQuery { CategoryId = Guid.Empty }).IsValid); Assert.False(v.Validate(new CatalogQuery { ApplicableTo = (CatalogApplicability)99 }).IsValid);
        Assert.True(CatalogSearch.Applies(CatalogApplicability.Both, CatalogApplicability.Subscription)); Assert.False(CatalogSearch.Applies(CatalogApplicability.Payment, CatalogApplicability.Subscription));
    }
}
