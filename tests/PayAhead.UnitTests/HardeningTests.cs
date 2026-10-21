using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using PayAhead.Api;
using Xunit;

namespace PayAhead.UnitTests;

public sealed class HardeningTests
{
    private sealed class Env(string name) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = name;
        public string ApplicationName { get; set; } = "Tests";
        public string ContentRootPath { get; set; } = "";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
    private static IConfiguration Config(string key, string value) => new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
    {
        ["ConnectionStrings:PayAhead"] = "Server=sql.example;Database=PayAhead;User Id=runtime;Password=not-used;Encrypt=True;TrustServerCertificate=False",
        ["AllowedHosts"] = "api.example.test",
        [key] = value
    }).Build();
    [Theory]
    [InlineData("AllowedHosts", "*")]
    [InlineData("AllowedHosts", "")]
    [InlineData("ConnectionStrings:PayAhead", "Server=sql;User Id=sa;Encrypt=True;TrustServerCertificate=False")]
    [InlineData("ConnectionStrings:PayAhead", "Server=sql;User Id=runtime;Encrypt=False;TrustServerCertificate=False")]
    [InlineData("ConnectionStrings:PayAhead", "Server=sql;User Id=runtime;Encrypt=True;TrustServerCertificate=True")]
    [InlineData("Notifications:UseFake", "true")]
    public void Production_rejects_unsafe_configuration(string key, string value)
        => Assert.Throws<InvalidOperationException>(() => ProductionConfiguration.Validate(new Env("Production"), Config(key, value)));
    [Theory]
    [InlineData("Production")]
    [InlineData("Staging")]
    public void Secure_config_is_valid_in_both_deployment_environments(string environment)
        => ProductionConfiguration.Validate(new Env(environment), Config("Notifications:Enabled", "false"));
    [Theory]
    [InlineData("*")]
    [InlineData("http://client.test")]
    [InlineData("https://client.test/path")]
    [InlineData("https://*.test")]
    [InlineData("https://user:pass@client.test")]
    public void Cors_rejects_wildcards_and_non_origin_values(string origin)
        => Assert.Throws<InvalidOperationException>(() => ProductionConfiguration.CorsOrigins(Config("Cors:Origins:0", origin)));
    [Fact]
    public void Exact_HTTPS_origin_is_allowlisted_without_credentials()
        => Assert.Equal(new[] { "https://client.test" }, ProductionConfiguration.CorsOrigins(Config("Cors:Origins:0", "https://client.test")));
    [Fact]
    public void Unknown_environment_is_rejected()
        => Assert.Throws<InvalidOperationException>(() => ProductionConfiguration.Validate(new Env("Prodution"), Config("Notifications:Enabled", "false")));
}
