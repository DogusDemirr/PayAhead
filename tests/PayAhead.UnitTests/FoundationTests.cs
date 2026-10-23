using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using PayAhead.Api;
using PayAhead.Domain;
using PayAhead.Infrastructure;
using Xunit;

namespace PayAhead.UnitTests;

public sealed class FoundationTests
{
    [Theory]
    [InlineData("Production")]
    [InlineData("Staging")]
    [InlineData("Development ")]
    public void Development_identity_is_rejected_outside_development(string environment)
    {
        Assert.Throws<InvalidOperationException>(() => DevelopmentIdentity.Validate(new TestEnvironment(environment), Config(true, Guid.NewGuid().ToString())));
    }

    [Fact]
    public void Development_identity_is_disabled_by_default()
        => Assert.Null(DevelopmentIdentity.Validate(new TestEnvironment("Production"), Config(false, null)));

    [Theory]
    [InlineData(null)]
    [InlineData("not-a-guid")]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    public void Development_identity_requires_explicit_valid_user(string? id)
        => Assert.Throws<InvalidOperationException>(() => DevelopmentIdentity.Validate(new TestEnvironment("Development"), Config(true, id)));

    [Fact]
    public void Development_identity_uses_only_configured_user()
    {
        var id = Guid.NewGuid();
        Assert.Equal(id, DevelopmentIdentity.Validate(new TestEnvironment("Development"), Config(true, id.ToString())));
    }

    [Fact]
    public void Sql_server_model_has_financial_precision_and_unique_occurrence_index()
    {
        using var db = new PayAheadDbContext(new DbContextOptionsBuilder<PayAheadDbContext>()
            .UseSqlServer("Server=localhost;Database=ModelOnly;Integrated Security=True").Options);
        var plan = db.Model.FindEntityType(typeof(PaymentPlan))!;
        Assert.Equal(19, plan.FindProperty(nameof(PaymentPlan.Amount))!.GetPrecision());
        Assert.Equal(4, plan.FindProperty(nameof(PaymentPlan.Amount))!.GetScale());
        Assert.True(plan.FindProperty(nameof(PaymentPlan.RowVersion))!.IsConcurrencyToken);
        var occurrence = db.Model.FindEntityType(typeof(PaymentOccurrence))!;
        Assert.Contains(occurrence.GetIndexes(), x => x.IsUnique && x.Properties.Select(p => p.Name)
            .SequenceEqual([nameof(PaymentOccurrence.PaymentPlanId), nameof(PaymentOccurrence.DueDate)]));
        Assert.Equal(16, db.Model.GetEntityTypes().Count());
        var reset = db.Model.FindEntityType(typeof(PasswordResetToken))!;
        Assert.Contains(reset.GetIndexes(), x => x.IsUnique && x.Properties.Single().Name == nameof(PasswordResetToken.TokenHash));
    }

    private static IConfiguration Config(bool enabled, string? id) => new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["DevelopmentIdentity:Enabled"] = enabled.ToString(),
            ["DevelopmentIdentity:UserId"] = id
        }).Build();

    private sealed class TestEnvironment(string name) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = name;
        public string ApplicationName { get; set; } = "PayAhead";
        public string ContentRootPath { get; set; } = ".";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
