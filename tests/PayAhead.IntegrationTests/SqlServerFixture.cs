using Testcontainers.MsSql;
using Xunit;

namespace PayAhead.IntegrationTests;

// Ortak geliştirme veritabanına bağlanılmaz. Her test ayrıca rastgele veritabanı kullanır.
public sealed class SqlServerFixture : IAsyncLifetime
{
    private MsSqlContainer? container;
    public string ConnectionString { get; private set; } = "";

    public async Task InitializeAsync()
    {
        var external = Environment.GetEnvironmentVariable("PAYAHEAD_TEST_SQLSERVER");
        if (!string.IsNullOrWhiteSpace(external))
        {
            ConnectionString = external;
            return;
        }
        container = new MsSqlBuilder().WithImage("mcr.microsoft.com/mssql/server:2022-latest").Build();
        await container.StartAsync();
        ConnectionString = container.GetConnectionString();
    }

    public async Task DisposeAsync()
    {
        if (container is not null) await container.DisposeAsync();
    }
}
