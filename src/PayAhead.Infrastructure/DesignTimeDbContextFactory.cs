using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace PayAhead.Infrastructure;

public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<PayAheadDbContext>
{
    public PayAheadDbContext CreateDbContext(string[] args)
    {
        var connection = Environment.GetEnvironmentVariable("ConnectionStrings__PayAhead")
            ?? throw new InvalidOperationException("ConnectionStrings__PayAhead ortam değişkenini ayarlayın.");
        return new(new DbContextOptionsBuilder<PayAheadDbContext>().UseSqlServer(connection).Options);
    }
}
