using PayAhead.Application.Catalog;
using PayAhead.Domain;
namespace PayAhead.Infrastructure;

public static class CatalogTranslationSeed
{
    public static IReadOnlyList<CatalogCategoryTranslation> All { get; } = Build();
    private static CatalogCategoryTranslation[] Build()
    {
        string[] cultures = ["de", "fr", "es", "it", "pt-BR", "nl", "ru", "ar", "zh-Hans", "ja", "ko", "hi"];
        using var stream = typeof(CatalogTranslationSeed).Assembly.GetManifestResourceStream("PayAhead.Infrastructure.Localization.CatalogNames.tsv") ?? throw new InvalidOperationException("Catalog translations missing.");
        using var reader = new StreamReader(stream); var translated = new Dictionary<string, string[]>();
        while (reader.ReadLine() is { } line)
        {
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#')) continue;
            var values = line.Split('|'); if (values.Length != cultures.Length + 1 || values.Any(string.IsNullOrWhiteSpace)) throw new InvalidOperationException("Invalid catalog translation row.");
            translated.Add(values[0], values[1..]);
        }
        var rows = new List<CatalogCategoryTranslation>();
        foreach (var category in CatalogSeed.Categories)
        {
            void Add(string culture, string name) => rows.Add(new() { CategoryId = category.Id, CultureCode = culture, DisplayName = name, SearchText = CatalogSearch.Normalize(name) });
            Add("tr", category.NameTr); Add("en", category.NameEn);
            if (!translated.TryGetValue(category.Code, out var names)) throw new InvalidOperationException("Missing category translation: " + category.Code);
            for (var i = 0; i < cultures.Length; i++) Add(cultures[i], names[i]);
        }
        return rows.ToArray();
    }
}
