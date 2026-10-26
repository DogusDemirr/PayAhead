using PayAhead.Application.Catalog;
using PayAhead.Application.Localization;
using PayAhead.Application.Notifications;
using PayAhead.Infrastructure;
using Xunit;
namespace PayAhead.UnitTests;

public sealed class LocalizationTests
{
    [Fact] public void Generic_service_templates_are_complete_and_brands_and_private_values_are_unchanged()
    {
        using var stream = typeof(CatalogServiceNames).Assembly.GetManifestResourceStream("PayAhead.Infrastructure.Localization.ServiceNames.json")!;
        var data = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(stream)!;
        Assert.Equal(14, data.Count);
        foreach (var code in SupportedLanguages.All)
        {
            Assert.Equal(33, data[code].Count);
            foreach (var row in data[code])
            {
                Assert.Contains(CatalogSeed.Services, x => x.Code == row.Key);
                Assert.False(string.IsNullOrWhiteSpace(row.Value));
                Assert.Equal(row.Value, CatalogServiceNames.Name(row.Key, "untranslated", code));
                Assert.Contains(row.Key, CatalogServiceNames.Search(code, CatalogSearch.Normalize(row.Value)));
            }
            Assert.Equal("Netflix", CatalogServiceNames.Name("netflix", "Netflix", code));
            Assert.Equal("Private name", CatalogServiceNames.Name("user-entered", "Private name", code));
        }
        Assert.Equal(data["en"]["elektrik-faturasi"], CatalogServiceNames.Name("elektrik-faturasi", "original", "xx"));
    }
    [Fact] public void All_fourteen_languages_have_complete_unique_category_names_and_private_push_copy()
    {
        Assert.Equal(14, SupportedLanguages.All.Count);
        Assert.Equal(116 * 14, CatalogTranslationSeed.All.Count);
        Assert.Equal(CatalogTranslationSeed.All.Count, CatalogTranslationSeed.All.Select(x => (x.CategoryId, x.CultureCode)).Distinct().Count());
        foreach (var code in SupportedLanguages.All)
        {
            Assert.True(new SettingsValidator().Validate(new SettingsRequest(code, "TRY", "UTC", true)).IsValid);
            var rows = CatalogTranslationSeed.All.Where(x => x.CultureCode == code).ToArray(); Assert.Equal(116, rows.Length);
            Assert.All(rows, x => { Assert.False(string.IsNullOrWhiteSpace(x.DisplayName)); Assert.InRange(x.DisplayName.Length, 1, 150); Assert.Equal(CatalogSearch.Normalize(x.DisplayName), x.SearchText); });
            Assert.False(string.IsNullOrWhiteSpace(NotificationText.Body(code, false))); Assert.False(string.IsNullOrWhiteSpace(NotificationText.Body(code, true)));
        }
    }
    [Theory]
    [InlineData(null, "en")][InlineData("en-US", "en")][InlineData("de-DE", "de")][InlineData("pt-PT", "pt-BR")][InlineData("zh-CN", "zh-Hans")][InlineData("zh-Hant", "en")][InlineData("xx", "en")]
    public void Culture_resolution_is_bounded_and_does_not_treat_unsupported_Chinese_as_simplified(string? input, string expected) => Assert.Equal(expected, SupportedLanguages.Resolve(input));
}
