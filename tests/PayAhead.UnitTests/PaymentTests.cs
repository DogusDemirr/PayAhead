using System.Globalization;
using FluentValidation;
using PayAhead.Application;
using PayAhead.Application.Payments;
using PayAhead.Domain;
using Xunit;

namespace PayAhead.UnitTests;

public sealed class PaymentTests
{
    [Fact]
    public async Task Missing_fields_are_all_rejected()
    {
        var result = await new CreatePaymentValidator().ValidateAsync(new CreatePaymentRequest());
        foreach (var field in new[] { "Name", "Amount", "CurrencyCode", "Category", "Frequency", "Interval", "StartDate" })
            Assert.Contains(result.Errors, x => x.PropertyName == field);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Empty_names_are_rejected(string? name)
        => Assert.False((await new CreatePaymentValidator().ValidateAsync(Request(name: name))).IsValid);

    [Theory]
    [InlineData(150, true)]
    [InlineData(151, false)]
    public async Task Name_length_is_limited(int length, bool valid)
        => Assert.Equal(valid, (await new CreatePaymentValidator().ValidateAsync(Request(name: new string('a', length)))).IsValid);

    [Theory]
    [InlineData("0", false)]
    [InlineData("-1", false)]
    [InlineData("0.0001", true)]
    [InlineData("1.00001", false)]
    [InlineData("999999999999999.9999", true)]
    [InlineData("1000000000000000", false)]
    public async Task Amount_matches_sql_precision_without_rounding(string value, bool valid)
        => Assert.Equal(valid, (await new CreatePaymentValidator().ValidateAsync(Request(amount: decimal.Parse(value, CultureInfo.InvariantCulture)))).IsValid);

    [Theory]
    [InlineData("TRY", true)]
    [InlineData(" usd ", true)]
    [InlineData("EUR", true)]
    [InlineData("JPY", true)]
    [InlineData("XCG", true)]
    [InlineData("ZZZ", false)]
    [InlineData("TL", false)]
    [InlineData("BGN", false)]
    [InlineData(null, false)]
    public async Task Currency_is_checked_against_current_iso_snapshot(string? code, bool valid)
        => Assert.Equal(valid, (await new CreatePaymentValidator().ValidateAsync(Request(currency: code))).IsValid);

    [Theory]
    [InlineData(-1, false)]
    [InlineData(0, true)]
    [InlineData(4, true)]
    [InlineData(5, false)]
    public async Task Frequency_must_be_defined(int value, bool valid)
        => Assert.Equal(valid, (await new CreatePaymentValidator().ValidateAsync(Request(frequency: (PaymentFrequency)value))).IsValid);

    [Theory]
    [InlineData(-1, false)]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(int.MaxValue, true)]
    public async Task Interval_must_be_positive(int value, bool valid)
        => Assert.Equal(valid, (await new CreatePaymentValidator().ValidateAsync(Request(interval: value))).IsValid);

    [Fact]
    public async Task Dates_and_optional_lengths_are_validated()
    {
        var validator = new CreatePaymentValidator();
        Assert.False((await validator.ValidateAsync(Request(end: new DateOnly(2025, 1, 1)))).IsValid);
        Assert.True((await validator.ValidateAsync(Request(end: new DateOnly(2026, 1, 31)))).IsValid);
        Assert.False((await validator.ValidateAsync(Request(start: default(DateOnly)))).IsValid);
        Assert.False((await validator.ValidateAsync(Request(anchor: 32))).IsValid);
        Assert.False((await validator.ValidateAsync(Request(description: new string('a', 2001)))).IsValid);
        Assert.False((await validator.ValidateAsync(Request(category: new string('a', 51)))).IsValid);
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData("bad", false)]
    [InlineData("AAAAAA==", false)]
    [InlineData("AAAAAAAAAAE=", true)]
    public async Task Update_requires_eight_byte_canonical_rowversion(string? token, bool valid)
        => Assert.Equal(valid, (await new UpdatePaymentValidator().ValidateAsync(Update(token))).IsValid);

    [Fact]
    public async Task Update_applies_the_same_write_rules()
    {
        var result = await new UpdatePaymentValidator().ValidateAsync(new UpdatePaymentRequest { RowVersion = "AAAAAAAAAAE=" });
        Assert.Contains(result.Errors, x => x.PropertyName == "Name");
        Assert.Contains(result.Errors, x => x.PropertyName == "Amount");
        Assert.Contains(result.Errors, x => x.PropertyName == "StartDate");
    }

    [Theory]
    [InlineData(0, 20, 0)]
    [InlineData(1, 20, 1)]
    [InlineData(20, 20, 1)]
    [InlineData(21, 20, 2)]
    [InlineData(int.MaxValue, 100, 21474837)]
    public void Total_pages_does_not_overflow(int total, int size, int expected)
        => Assert.Equal(expected, Pagination.TotalPages(total, size));

    [Theory]
    [InlineData(0, 20)]
    [InlineData(1, 0)]
    [InlineData(1, 101)]
    [InlineData(1000001, 100)]
    public async Task Pagination_limits_are_validated(int page, int size)
        => Assert.False((await new PaymentListValidator().ValidateAsync(new PaymentListQuery { PageNumber = page, PageSize = size })).IsValid);

    [Fact]
    public void Domain_update_preserves_identity_owner_and_creation_time()
    {
        var owner = Guid.NewGuid();
        var created = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var details = Details();
        var plan = PaymentPlan.Create(owner, details, created);
        var id = plan.Id;
        plan.UpdateDetails(details with { Name = "Changed", IsActive = false }, created.AddHours(1));
        Assert.Equal(id, plan.Id); Assert.Equal(owner, plan.UserId); Assert.Equal(created, plan.CreatedAtUtc);
        Assert.Equal(created.AddHours(1), plan.UpdatedAtUtc); Assert.False(plan.IsActive);
        Assert.Throws<DomainRuleException>(() => plan.UpdateDetails(details with { Amount = 0 }, created));
        Assert.Equal("Changed", plan.Name);
        Assert.Throws<DomainRuleException>(() => PaymentPlan.Create(Guid.Empty, details, created));
    }

    [Fact]
    public async Task Service_normalizes_filters_and_always_forwards_current_owner()
    {
        var store = new CapturingStore();
        var owner = Guid.NewGuid();
        var service = Service(store, owner);
        var page = await service.ListAsync(new() { PageNumber = 2, PageSize = 5, IsActive = false, Category = " Bills ", CurrencyCode = " usd ", Search = " Test " }, default);
        Assert.Equal(owner, store.Owner);
        Assert.Equal(new PaymentFilter(2, 5, false, "Bills", "USD", "Test"), store.Filter);
        Assert.Equal(0, page.TotalPages);
        Assert.Empty(page.Items);
        await Assert.ThrowsAsync<ValidationException>(() => service.ListAsync(new() { CurrencyCode = "ZZZ" }, default));
    }

    [Fact]
    public async Task Service_creates_owned_normalized_plan_and_requires_real_active_user()
    {
        var store = new CapturingStore();
        var owner = Guid.NewGuid();
        var response = await Service(store, owner).CreateAsync(Request(name: " Test ", currency: " usd "), default);
        Assert.Equal(owner, store.Added!.UserId); Assert.Equal("Test", response.Name); Assert.Equal("USD", response.CurrencyCode);
        Assert.Equal(FixedClock.Instant.UtcDateTime, response.CreatedAtUtc);
        store.Active = false;
        await Assert.ThrowsAsync<AuthenticationRequiredException>(() => Service(store, owner).CreateAsync(Request(), default));
        await Assert.ThrowsAsync<AuthenticationRequiredException>(() => Service(store, null).ListAsync(new(), default));
        await Assert.ThrowsAsync<ResourceNotFoundException>(() => Service(store, owner).GetAsync(Guid.NewGuid(), default));
    }

    internal static CreatePaymentRequest Request(string? name = "Payment", decimal amount = 10, string? currency = "TRY",
        PaymentFrequency frequency = PaymentFrequency.Monthly, int interval = 1, DateOnly? start = null,
        DateOnly? end = null, int? anchor = null, string? description = null, string category = "Bills") => new()
    {
        Name = name, Amount = amount, CurrencyCode = currency, Frequency = frequency, Interval = interval,
        StartDate = start ?? new DateOnly(2026, 1, 31), EndDate = end, AnchorDay = anchor, Description = description, Category = category
    };

    private static UpdatePaymentRequest Update(string? token) => new()
    {
        Name = "Payment", Amount = 10, CurrencyCode = "TRY", Category = "Bills", Frequency = PaymentFrequency.Monthly,
        Interval = 1, StartDate = new DateOnly(2026, 1, 31), RowVersion = token
    };
    private static PaymentPlanDetails Details() => new("Payment", null, 10, "TRY", "Bills", PaymentFrequency.Monthly, 1, new(2026, 1, 31), null, 31, true);
    private static PaymentService Service(CapturingStore store, Guid? owner) => new(store, new UserContext(owner), new FixedClock(), new CreatePaymentValidator(), new UpdatePaymentValidator(), new PaymentListValidator());
    private sealed class UserContext(Guid? id) : ICurrentUser { public Guid? UserId => id; }
    private sealed class FixedClock : TimeProvider
    {
        public static readonly DateTimeOffset Instant = new(2026, 10, 21, 12, 0, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => Instant;
    }
    private sealed class CapturingStore : IPaymentPlanStore
    {
        public bool Active { get; set; } = true;
        public Guid Owner { get; private set; }
        public PaymentFilter? Filter { get; private set; }
        public PaymentPlan? Added { get; private set; }
        public Task<bool> IsActiveUserAsync(Guid userId, CancellationToken cancellationToken) => Task.FromResult(Active);
        public Task<PaymentPlan?> FindOwnedAsync(Guid userId, Guid id, CancellationToken cancellationToken) => Task.FromResult<PaymentPlan?>(null);
        public Task<PagedResult<PaymentPlan>> ListOwnedAsync(Guid userId, PaymentFilter filter, CancellationToken cancellationToken)
        { Owner = userId; Filter = filter; return Task.FromResult(new PagedResult<PaymentPlan>([], filter.PageNumber, filter.PageSize, 0)); }
        public Task AddAsync(PaymentPlan plan, CancellationToken cancellationToken)
        { Added = plan; plan.RowVersion = new byte[8]; return Task.CompletedTask; }
        public Task UpdateAsync(PaymentPlan plan, byte[] expectedVersion, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task DeleteAsync(PaymentPlan plan, byte[] expectedVersion, CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
