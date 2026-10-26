namespace PayAhead.Application.Localization;

public static class SupportedLanguages
{
    public static readonly IReadOnlyList<string> All = Array.AsReadOnly(new[] { "tr", "en", "de", "fr", "es", "it", "pt-BR", "nl", "ru", "ar", "zh-Hans", "ja", "ko", "hi" });
    public static bool Contains(string? value) => value is not null && All.Contains(value, StringComparer.Ordinal);
    public static string Resolve(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "en";
        foreach (var entry in value.Split(','))
        {
            var code = entry.Split(';')[0].Trim();
            if (code == "*") continue;
            var exact = All.FirstOrDefault(x => x.Equals(code, StringComparison.OrdinalIgnoreCase));
            if (exact is not null) return exact;
            if (code.StartsWith("zh", StringComparison.OrdinalIgnoreCase)) return code.Contains("Hant", StringComparison.OrdinalIgnoreCase) || code.EndsWith("-TW", StringComparison.OrdinalIgnoreCase) || code.EndsWith("-HK", StringComparison.OrdinalIgnoreCase) || code.EndsWith("-MO", StringComparison.OrdinalIgnoreCase) ? "en" : "zh-Hans";
            if (code.StartsWith("pt", StringComparison.OrdinalIgnoreCase)) return "pt-BR";
            var basic = All.FirstOrDefault(x => x.Equals(code.Split('-')[0], StringComparison.OrdinalIgnoreCase));
            if (basic is not null) return basic;
        }
        return "en";
    }
}
