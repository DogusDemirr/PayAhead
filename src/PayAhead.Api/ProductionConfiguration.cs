using Microsoft.Data.SqlClient;

namespace PayAhead.Api;

public static class ProductionConfiguration
{
    public static void Validate(IHostEnvironment environment, IConfiguration config)
    {
        if (environment.EnvironmentName is not ("Development" or "Testing" or "Staging" or "Production")) throw new InvalidOperationException("Unknown application environment.");
        if (environment.IsDevelopment() || environment.IsEnvironment("Testing")) return;
        var sql = new SqlConnectionStringBuilder(config.GetConnectionString("PayAhead"));
        if (sql.TrustServerCertificate || sql.Encrypt == SqlConnectionEncryptOption.Optional || sql.UserID.Equals("sa", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Production SQL requires certificate validation, encryption and a non-SA identity.");
        var hosts = config["AllowedHosts"]?.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? [];
        if (hosts.Length == 0 || hosts.Any(x => x.Contains('*'))) throw new InvalidOperationException("Explicit AllowedHosts is required.");
        if (config.GetValue<bool>("Notifications:UseFake")) throw new InvalidOperationException("Fake push provider is forbidden.");
        if (config.GetValue<bool>("Notifications:Enabled") && (config["Notifications:SendEndpoint"] is { } send && send != "https://exp.host/--/api/v2/push/send" || config["Notifications:ReceiptEndpoint"] is { } receipt && receipt != "https://exp.host/--/api/v2/push/getReceipts"))
            throw new InvalidOperationException("Production push requires official Expo endpoints.");
    }
    public static string[] CorsOrigins(IConfiguration config)
    {
        var origins = config.GetSection("Cors:Origins").Get<string[]>() ?? [];
        if (origins.Any(x => !Uri.TryCreate(x, UriKind.Absolute, out var uri) || uri.Scheme != "https" || uri.Host.Contains('*') || x != uri.GetLeftPart(UriPartial.Authority) || uri.UserInfo.Length != 0))
            throw new InvalidOperationException("CORS origins must be exact HTTPS origins.");
        return origins;
    }
}
