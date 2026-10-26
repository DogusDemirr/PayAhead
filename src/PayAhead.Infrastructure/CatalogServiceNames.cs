using System.Text.Json;
using PayAhead.Application.Catalog;
using PayAhead.Application.Localization;

namespace PayAhead.Infrastructure;

// Only curated generic templates have entries. Brand and user-entered names remain unchanged.
public static class CatalogServiceNames
{
    private static readonly Dictionary<string, Dictionary<string, string>> Names = Load();
    private static Dictionary<string, Dictionary<string, string>> Load()
    {
        using var stream = typeof(CatalogServiceNames).Assembly.GetManifestResourceStream("PayAhead.Infrastructure.Localization.ServiceNames.json")!;
        return JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(stream)!;
    }
    public static string Name(string code, string original, string? locale) =>
        Names.GetValueOrDefault(SupportedLanguages.Resolve(locale))?.GetValueOrDefault(code)
        ?? Names.GetValueOrDefault("en")?.GetValueOrDefault(code) ?? original;
    public static string[] Search(string? locale, string normalized) =>
        Names.GetValueOrDefault(SupportedLanguages.Resolve(locale))?.Where(x => CatalogSearch.Normalize(x.Value).Contains(normalized, StringComparison.Ordinal)).Select(x => x.Key).ToArray() ?? [];
}
