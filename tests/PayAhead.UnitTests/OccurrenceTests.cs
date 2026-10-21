using FluentValidation;
using PayAhead.Application;
using PayAhead.Application.Occurrences;
using PayAhead.Application.Payments;
using PayAhead.Domain;
using PayAhead.Domain.Recurrence;
using Xunit;

namespace PayAhead.UnitTests;

public sealed class OccurrenceTests
{
    private static readonly DateTime Now = new(2026, 10, 21, 12, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData(OccurrenceStatus.Pending, OccurrenceStatus.Paid)]
    [InlineData(OccurrenceStatus.Pending, OccurrenceStatus.Skipped)]
    [InlineData(OccurrenceStatus.Paid, OccurrenceStatus.Pending)]
    [InlineData(OccurrenceStatus.Paid, OccurrenceStatus.Skipped)]
    [InlineData(OccurrenceStatus.Skipped, OccurrenceStatus.Paid)]
    [InlineData(OccurrenceStatus.Skipped, OccurrenceStatus.Pending)]
    public void All_state_transitions_apply_paid_fields_and_preserve_identity(OccurrenceStatus from, OccurrenceStatus to)
    {
        var occurrence = PaymentOccurrence.Create(Guid.NewGuid(), new(2026, 1, 31), from, from == OccurrenceStatus.Paid ? 10 : null, Now);
        var id = occurrence.Id;
        Assert.True(occurrence.ChangeStatus(to, to == OccurrenceStatus.Paid ? 20.1234m : null, Now.AddHours(1)));
        Assert.Equal(to, occurrence.Status);
        Assert.Equal(to == OccurrenceStatus.Paid ? Now.AddHours(1) : (DateTime?)null, occurrence.PaidAtUtc);
        Assert.Equal(to == OccurrenceStatus.Paid ? 20.1234m : (decimal?)null, occurrence.ActualAmount);
        Assert.Equal(id, occurrence.Id); Assert.Equal(Now, occurrence.CreatedAtUtc);
        Assert.Equal(new DateOnly(2026, 1, 31), occurrence.DueDate);
    }

    [Fact]
    public void Identical_paid_request_preserves_timestamp_and_amount_correction_records_new_time()
    {
        var occurrence = PaymentOccurrence.Create(Guid.NewGuid(), new(2026, 1, 31), OccurrenceStatus.Paid, 12, Now);
        Assert.False(occurrence.ChangeStatus(OccurrenceStatus.Paid, 12, Now.AddHours(1)));
        Assert.Equal(Now, occurrence.PaidAtUtc);
        Assert.True(occurrence.ChangeStatus(OccurrenceStatus.Paid, 13, Now.AddHours(2)));
        Assert.Equal(Now.AddHours(2), occurrence.PaidAtUtc);
    }

    [Theory]
    [InlineData(OccurrenceStatus.Paid, null)]
    [InlineData(OccurrenceStatus.Paid, "0")]
    [InlineData(OccurrenceStatus.Paid, "-1")]
    [InlineData(OccurrenceStatus.Paid, "1.12345")]
    [InlineData(OccurrenceStatus.Paid, "1000000000000000")]
    [InlineData(OccurrenceStatus.Pending, "1")]
    [InlineData(OccurrenceStatus.Skipped, "1")]
    [InlineData((OccurrenceStatus)999, null)]
    public async Task Invalid_status_and_amount_fail_domain_and_application_validation(OccurrenceStatus status, string? amount)
    {
        decimal? actual = amount is null ? null : decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture);
        var occurrence = PaymentOccurrence.Create(Guid.NewGuid(), new(2026, 1, 31), OccurrenceStatus.Skipped, null, Now);
        Assert.Throws<DomainRuleException>(() => occurrence.ChangeStatus(status, actual, Now));
        Assert.Equal(OccurrenceStatus.Skipped, occurrence.Status); Assert.Null(occurrence.ActualAmount);
        Assert.False((await new SetOccurrenceStatusValidator().ValidateAsync(new SetOccurrenceStatusRequest { Status = status, ActualAmount = actual })).IsValid);
    }

    [Fact]
    public async Task Missing_status_bad_token_non_utc_time_and_empty_plan_are_rejected()
    {
        var validator = new SetOccurrenceStatusValidator();
        Assert.False((await validator.ValidateAsync(new SetOccurrenceStatusRequest())).IsValid);
        Assert.False((await validator.ValidateAsync(new SetOccurrenceStatusRequest { Status = OccurrenceStatus.Skipped, RowVersion = "bad" })).IsValid);
        Assert.Throws<DomainRuleException>(() => PaymentOccurrence.Create(Guid.Empty, new(2026, 1, 31), OccurrenceStatus.Skipped, null, Now));
        Assert.Throws<DomainRuleException>(() => PaymentOccurrence.Create(Guid.NewGuid(), new(2026, 1, 31), OccurrenceStatus.Paid, 10, DateTime.SpecifyKind(Now, DateTimeKind.Unspecified)));
    }

    [Theory]
    [InlineData("2026-01-31", "2026-02-28", true)]
    [InlineData("2026-01-31", "2026-02-27", false)]
    [InlineData("2024-01-31", "2024-02-29", true)]
    [InlineData("2024-01-31", "2024-02-28", false)]
    [InlineData("2026-01-31", "2026-01-30", false)]
    [InlineData("2026-01-31", "2027-01-31", false)]
    public void Schedule_validation_reuses_recurrence_with_boundaries(string start, string due, bool valid)
    {
        var plan = Plan(); plan.StartDate = DateOnly.Parse(start); plan.EndDate = new(2026, 12, 31);
        Action action = () => OccurrenceScheduleRules.EnsureValidDate(plan, DateOnly.Parse(due), new PaymentRecurrenceCalculator());
        if (valid) action(); else Assert.Throws<DomainRuleException>(action);
    }

    [Fact]
    public async Task Virtual_pending_and_repeated_paid_do_not_create_duplicates_or_reset_time()
    {
        var store = new MemoryStore(); var clock = new MutableClock(); var service = Service(store, clock);
        var pending = await service.GetAsync(store.Plan.Id, store.Plan.StartDate, default);
        Assert.Equal(OccurrenceStatus.Pending, pending.Status); Assert.False(pending.IsRecorded); Assert.Null(pending.RowVersion);
        Assert.Equal(pending, await service.SetStatusAsync(store.Plan.Id, store.Plan.StartDate, new() { Status = OccurrenceStatus.Pending }, default));
        Assert.Equal(0, store.Writes);
        var request = new SetOccurrenceStatusRequest { Status = OccurrenceStatus.Paid, ActualAmount = 15 };
        var paid = await service.SetStatusAsync(store.Plan.Id, store.Plan.StartDate, request, default);
        clock.Instant = clock.Instant.AddHours(1);
        var repeated = await service.SetStatusAsync(store.Plan.Id, store.Plan.StartDate, request, default);
        Assert.Equal(paid, repeated); Assert.Equal(1, store.Writes);
        Assert.Equal(Now, paid.PaidAtUtc);
        var skipped = await service.SetStatusAsync(store.Plan.Id, store.Plan.StartDate,
            new() { Status = OccurrenceStatus.Skipped, RowVersion = paid.RowVersion }, default);
        Assert.Equal(OccurrenceStatus.Skipped, skipped.Status); Assert.Null(skipped.ActualAmount); Assert.Null(skipped.PaidAtUtc);
        await Assert.ThrowsAsync<ResourceConflictException>(() => service.SetStatusAsync(store.Plan.Id, store.Plan.StartDate,
            new() { Status = OccurrenceStatus.Paid, ActualAmount = 10, RowVersion = paid.RowVersion }, default));
        var reset = new SetOccurrenceStatusRequest { Status = OccurrenceStatus.Pending, RowVersion = skipped.RowVersion };
        var resetResult = await service.SetStatusAsync(store.Plan.Id, store.Plan.StartDate, reset, default);
        Assert.False(resetResult.IsRecorded); Assert.Null(store.Record); Assert.Null(resetResult.ActualAmount);
        Assert.Equal(resetResult, await service.SetStatusAsync(store.Plan.Id, store.Plan.StartDate, reset, default));
        Assert.Equal(3, store.Writes);
    }

    [Fact]
    public async Task Differing_mutation_needs_current_version_and_invalid_due_date_never_writes()
    {
        var store = new MemoryStore(); var service = Service(store, new MutableClock());
        await Assert.ThrowsAsync<DomainRuleException>(() => service.SetStatusAsync(store.Plan.Id, new(2026, 2, 27), new() { Status = OccurrenceStatus.Skipped }, default));
        Assert.Equal(0, store.Writes);
        await service.SetStatusAsync(store.Plan.Id, store.Plan.StartDate, new() { Status = OccurrenceStatus.Skipped }, default);
        await Assert.ThrowsAsync<ResourceConflictException>(() => service.SetStatusAsync(store.Plan.Id, store.Plan.StartDate, new() { Status = OccurrenceStatus.Pending }, default));
        Assert.Equal(OccurrenceStatus.Skipped, store.Record!.Status);
    }

    [Fact]
    public async Task Inactive_or_changed_schedule_preserves_stored_history_but_blocks_mutations()
    {
        var store = new MemoryStore(); var service = Service(store, new MutableClock());
        await service.SetStatusAsync(store.Plan.Id, store.Plan.StartDate, new() { Status = OccurrenceStatus.Paid, ActualAmount = 10 }, default);
        store.Plan.IsActive = false;
        Assert.Equal(OccurrenceStatus.Paid, (await service.GetAsync(store.Plan.Id, store.Plan.StartDate, default)).Status);
        await Assert.ThrowsAsync<ResourceNotFoundException>(() => service.GetAsync(store.Plan.Id, new(2026, 2, 28), default));
        await Assert.ThrowsAsync<ResourceConflictException>(() => service.SetStatusAsync(store.Plan.Id, store.Plan.StartDate,
            new() { Status = OccurrenceStatus.Paid, ActualAmount = 10 }, default));
        store.Plan.IsActive = true; store.Plan.StartDate = new(2026, 2, 1);
        Assert.Equal(OccurrenceStatus.Paid, (await service.GetAsync(store.Plan.Id, new(2026, 1, 31), default)).Status);
        await Assert.ThrowsAsync<DomainRuleException>(() => service.SetStatusAsync(store.Plan.Id, new(2026, 1, 31),
            new() { Status = OccurrenceStatus.Pending, RowVersion = Convert.ToBase64String(store.Record!.RowVersion) }, default));
    }

    [Theory]
    [InlineData(0, 20)]
    [InlineData(1, 101)]
    [InlineData(1000001, 20)]
    public async Task History_pagination_is_bounded(int page, int size)
        => Assert.False((await new OccurrenceListValidator().ValidateAsync(new OccurrenceListQuery { PageNumber = page, PageSize = size })).IsValid);

    [Fact]
    public async Task History_dates_status_and_missing_identity_are_validated()
    {
        Assert.False((await new OccurrenceListValidator().ValidateAsync(new OccurrenceListQuery { FromDate = new(2026, 2, 1), ToDate = new(2026, 1, 1) })).IsValid);
        Assert.False((await new OccurrenceListValidator().ValidateAsync(new OccurrenceListQuery { Status = (OccurrenceStatus)99 })).IsValid);
        var service = new OccurrenceService(new MemoryStore(), new UserContext(null), new PaymentRecurrenceCalculator(), new MutableClock(), new SetOccurrenceStatusValidator(), new OccurrenceListValidator());
        await Assert.ThrowsAsync<AuthenticationRequiredException>(() => service.GetAsync(Guid.NewGuid(), new(2026, 1, 31), default));
    }

    private static PaymentPlan Plan() => new() { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), StartDate = new(2026, 1, 31), EndDate = new(2026, 12, 31), Frequency = PaymentFrequency.Monthly, IsActive = true };
    private static OccurrenceService Service(MemoryStore store, MutableClock clock)
        => new(store, new UserContext(store.Plan.UserId), new PaymentRecurrenceCalculator(), clock, new SetOccurrenceStatusValidator(), new OccurrenceListValidator());
    private sealed class UserContext(Guid? id) : ICurrentUser { public Guid? UserId => id; }
    private sealed class MutableClock : TimeProvider
    {
        public DateTimeOffset Instant { get; set; } = new(Now);
        public override DateTimeOffset GetUtcNow() => Instant;
    }
    private sealed class MemoryStore : IOccurrenceStore
    {
        public PaymentPlan Plan { get; } = OccurrenceTests.Plan();
        public PaymentOccurrence? Record { get; private set; }
        public int Writes { get; private set; }
        public Task<OccurrenceSnapshot> ReadOwnedAsync(Guid userId, Guid paymentId, DateOnly date, CancellationToken cancellationToken)
            => Task.FromResult(new OccurrenceSnapshot(Plan, Record?.DueDate == date ? Record : null));
        public Task<PagedResult<PaymentOccurrence>> ListOwnedAsync(Guid userId, Guid paymentId, OccurrenceListQuery query, CancellationToken cancellationToken)
            => throw new NotSupportedException();
        public Task<OccurrenceSnapshot> MutateOwnedAsync(Guid userId, Guid paymentId, DateOnly date, Func<PaymentPlan, PaymentOccurrence?, OccurrenceMutation> mutate, CancellationToken cancellationToken)
        {
            var result = mutate(Plan, Record);
            if (result.HasChanges) { Writes++; Record = result.Occurrence; if (Record is not null) Record.RowVersion = BitConverter.GetBytes((long)Writes); }
            return Task.FromResult(new OccurrenceSnapshot(Plan, Record));
        }
    }
}
