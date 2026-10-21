using Microsoft.Data.SqlClient;
using PayAhead.Api;
using PayAhead.Application.Premium;
using PayAhead.Testing;

if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") != "Testing") throw new InvalidOperationException("Test host requires Testing environment.");
var connection = Environment.GetEnvironmentVariable("ConnectionStrings__PayAhead") ?? "";
var database = new SqlConnectionStringBuilder(connection).InitialCatalog;
if (!database.StartsWith("PayAheadSmoke_", StringComparison.Ordinal)) throw new InvalidOperationException("Test host requires an isolated smoke database.");
var secret = Convert.FromBase64String(Environment.GetEnvironmentVariable("PAYAHEAD_TEST_STORE_KEY") ?? "");
if (secret.Length < 32) throw new InvalidOperationException("Test signing key is required.");
var fake = new TestStoreProvider(secret);
var app = PayAheadApi.Build(args, b =>
{
    // Loopback-only, non-production test executable. No fake code is included in PayAhead.Api deployment.
    b.Environment.EnvironmentName = "Development";
    b.WebHost.UseUrls("http://127.0.0.1:" + (Environment.GetEnvironmentVariable("PAYAHEAD_TEST_PORT") ?? "5099"));
}, services =>
{
    services.AddSingleton<IStorePurchaseVerifier>(fake); services.AddSingleton<IStoreWebhookAuthenticator>(fake);
});
app.Run();
