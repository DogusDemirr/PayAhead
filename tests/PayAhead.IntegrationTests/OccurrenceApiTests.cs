using System.Data.Common;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using PayAhead.Application;
using PayAhead.Application.Occurrences;
using PayAhead.Application.Payments;
using PayAhead.Domain;
using PayAhead.Domain.Recurrence;
using PayAhead.Infrastructure;
using Xunit;

namespace PayAhead.IntegrationTests;

public sealed class OccurrenceApiTests : IClassFixture<SqlServerFixture>, IAsyncLifetime
{
    private readonly string connection;
    private readonly PayAheadDbContext db;
    private readonly Guid owner = Guid.NewGuid();
    private readonly Guid other = Guid.NewGuid();
    private readonly List<WebApplicationFactory<Program>> factories = [];
    private static readonly JsonSerializerOptions Json = CreateJson();
    private PaymentPlan plan = null!;
    private string Root => $"/api/payments/{plan.Id}/occurrences";

    public OccurrenceApiTests(SqlServerFixture server)
    {
        connection = new SqlConnectionStringBuilder(server.ConnectionString)
        { InitialCatalog = "PayAheadTest_" + Guid.NewGuid().ToString("N") }.ConnectionString;
        db = new(new DbContextOptionsBuilder<PayAheadDbContext>().UseSqlServer(connection).Options);
    }

    public async Task InitializeAsync()
    {
        await db.Database.MigrateAsync();
        foreach (var id in new[] { owner, other }) db.Users.Add(new()
        {
            Id = id, Email = $"{id:N}@example.invalid", NormalizedEmail = $"{id:N}@EXAMPLE.INVALID", DisplayName = "Test",
            PasswordHash = "test-only-placeholder", CreatedAtUtc = DateTime.UtcNow, UpdatedAtUtc = DateTime.UtcNow
        });
        plan = PaymentPlan.Create(owner, new("Plan", null, 10, "TRY", "Bills", PaymentFrequency.Monthly, 1,
            new(2026, 1, 31), new(2026, 12, 31), null, true), DateTime.UtcNow);
        db.PaymentPlans.Add(plan); await db.SaveChangesAsync(); db.ChangeTracker.Clear();
    }

    public async Task DisposeAsync()
    {
        foreach (var factory in factories) await factory.DisposeAsync();
        if (!db.Database.GetDbConnection().Database.StartsWith("PayAheadTest_", StringComparison.Ordinal))
            throw new InvalidOperationException("Test veritabanı güvenlik kontrolü başarısız.");
        await db.Database.EnsureDeletedAsync(); await db.DisposeAsync();
    }

    [Fact]
    public async Task Virtual_pending_paid_skipped_and_reset_are_idempotent_and_isolated_by_due_date()
    {
        using var client = Client(owner);
        var pending = await Read(await client.GetAsync(Root + "/2026-01-31"));
        Assert.False(pending.IsRecorded); Assert.Null(pending.Id); Assert.Null(pending.RowVersion);
        Assert.Equal(OccurrenceStatus.Pending, pending.Status); Assert.Equal(0, await db.PaymentOccurrences.CountAsync());
        Assert.Equal(pending, await Read(await Put(client, "2026-01-31", "Pending")));
        Assert.Equal(0, await db.PaymentOccurrences.CountAsync());
        var paid = await Read(await Put(client, "2026-01-31", "Paid", 12.3456m));
        Assert.True(paid.IsRecorded); Assert.NotNull(paid.PaidAtUtc); Assert.Equal(DateTimeKind.Utc, paid.PaidAtUtc!.Value.Kind);
        Assert.Equal(12.3456m, paid.ActualAmount); Assert.NotNull(paid.RowVersion);
        Assert.Equal(paid, await Read(await Put(client, "2026-01-31", "Paid", 12.3456m)));
        await Put(client, "2026-02-28", "Skipped");
        var skipped = await Read(await Put(client, "2026-01-31", "Skipped", version: paid.RowVersion));
        Assert.Null(skipped.ActualAmount); Assert.Null(skipped.PaidAtUtc); Assert.NotEqual(paid.RowVersion, skipped.RowVersion);
        await Problem(await Put(client, "2026-01-31", "Paid", 20, paid.RowVersion), HttpStatusCode.Conflict);
        var paidAgain = await Read(await Put(client, "2026-01-31", "Paid", 20, skipped.RowVersion));
        Assert.Equal(paid.Id, paidAgain.Id); Assert.Equal(paid.CreatedAtUtc, paidAgain.CreatedAtUtc);
        var reset = await Read(await Put(client, "2026-01-31", "Pending", version: paidAgain.RowVersion));
        Assert.False(reset.IsRecorded); Assert.Null(reset.ActualAmount); Assert.Null(reset.PaidAtUtc);
        Assert.Equal(reset, await Read(await Put(client, "2026-01-31", "Pending", version: paidAgain.RowVersion)));
        var remaining = await db.PaymentOccurrences.AsNoTracking().SingleAsync();
        Assert.Equal(new DateOnly(2026, 2, 28), remaining.DueDate); Assert.Equal(OccurrenceStatus.Skipped, remaining.Status);
    }

    [Fact]
    public async Task History_is_recorded_only_filtered_paginated_and_available_when_inactive()
    {
        using var client = Client(owner);
        await Put(client, "2026-01-31", "Paid", 10); await Put(client, "2026-02-28", "Skipped"); await Put(client, "2026-03-31", "Paid", 12);
        var first = await client.GetFromJsonAsync<PagedResult<OccurrenceResponse>>(Root + "?pageSize=1", Json);
        Assert.Equal(3, first!.TotalCount); Assert.Equal(3, first.TotalPages); Assert.Equal(new DateOnly(2026, 3, 31), Assert.Single(first.Items).DueDate);
        var filtered = await client.GetFromJsonAsync<PagedResult<OccurrenceResponse>>(Root + "?fromDate=2026-01-31&toDate=2026-02-28&status=Paid", Json);
        Assert.Equal(new DateOnly(2026, 1, 31), Assert.Single(filtered!.Items).DueDate);
        var pending = await client.GetFromJsonAsync<PagedResult<OccurrenceResponse>>(Root + "?status=Pending", Json);
        Assert.Empty(pending!.Items); Assert.Equal(0, pending.TotalCount);
        await db.PaymentPlans.Where(x => x.Id == plan.Id).ExecuteUpdateAsync(x => x.SetProperty(p => p.IsActive, false));
        Assert.Equal(OccurrenceStatus.Paid, (await Read(await client.GetAsync(Root + "/2026-01-31"))).Status);
        Assert.Equal(3, (await client.GetFromJsonAsync<PagedResult<OccurrenceResponse>>(Root, Json))!.TotalCount);
        await Problem(await Put(client, "2026-01-31", "Paid", 10), HttpStatusCode.Conflict);
        await Problem(await client.GetAsync(Root + "/2026-04-30"), HttpStatusCode.NotFound);
    }

    [Theory]
    [InlineData("2026-01-30")]
    [InlineData("2026-02-27")]
    [InlineData("2027-01-31")]
    [InlineData("invalid")]
    [InlineData("2026-02-30")]
    public async Task Invalid_dates_never_become_pending_or_persisted(string date)
    {
        using var client = Client(owner);
        await Problem(await client.GetAsync(Root + "/" + date), HttpStatusCode.BadRequest);
        await Problem(await Put(client, date, "Skipped"), HttpStatusCode.BadRequest);
        Assert.Equal(0, await db.PaymentOccurrences.CountAsync());
    }

    [Theory]
    [InlineData("{\"status\":\"Paid\"}")]
    [InlineData("{\"status\":\"Paid\",\"actualAmount\":0}")]
    [InlineData("{\"status\":\"Paid\",\"actualAmount\":-1}")]
    [InlineData("{\"status\":\"Paid\",\"actualAmount\":1.00001}")]
    [InlineData("{\"status\":\"Paid\",\"actualAmount\":1000000000000000}")]
    [InlineData("{\"status\":\"Pending\",\"actualAmount\":1}")]
    [InlineData("{\"status\":\"Skipped\",\"actualAmount\":1}")]
    [InlineData("{\"status\":\"Unknown\"}")]
    [InlineData("{\"status\":1,\"actualAmount\":10}")]
    [InlineData("{\"status\":\"Skipped\",\"rowVersion\":\"bad\"}")]
    [InlineData("{\"status\":\"Skipped\",\"userId\":\"33333333-3333-4333-8333-333333333333\"}")]
    [InlineData("{}")]
    public async Task Invalid_bodies_return_problem_details_without_writes(string json)
    {
        using var client = Client(owner);
        await Problem(await client.PutAsync(Root + "/2026-01-31/status", new StringContent(json, System.Text.Encoding.UTF8, "application/json")), HttpStatusCode.BadRequest);
        Assert.Equal(0, await db.PaymentOccurrences.CountAsync());
    }

    [Theory]
    [InlineData("fromDate=2026-03-01&toDate=2026-02-01")]
    [InlineData("status=999")]
    [InlineData("pageSize=101")]
    [InlineData("pageNumber=0")]
    public async Task Invalid_history_filters_return_400(string query)
    {
        using var client = Client(owner);
        await Problem(await client.GetAsync(Root + "?" + query), HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Ownership_and_missing_plan_are_hidden_for_all_operations()
    {
        using var client = Client(owner); using var second = Client(other); using var anonymous = Client(owner, false);
        await Put(client, "2026-01-31", "Paid", 10);
        foreach (var root in new[] { Root, $"/api/payments/{Guid.NewGuid()}/occurrences", $"/api/payments/{Guid.Empty}/occurrences" })
        {
            await Problem(await second.GetAsync(root), HttpStatusCode.NotFound);
            await Problem(await second.GetAsync(root + "/2026-01-31"), HttpStatusCode.NotFound);
            await Problem(await second.PutAsJsonAsync(root + "/2026-01-31/status", new { status = "Skipped" }), HttpStatusCode.NotFound);
        }
        await Problem(await anonymous.GetAsync(Root), HttpStatusCode.Unauthorized);
        await Problem(await anonymous.PutAsJsonAsync(Root + "/2026-01-31/status", new { status = "Skipped" }), HttpStatusCode.Unauthorized);
        await Problem(await client.GetAsync("/api/payments/not-a-guid/occurrences"), HttpStatusCode.BadRequest);
        Assert.Equal(OccurrenceStatus.Paid, (await db.PaymentOccurrences.SingleAsync()).Status);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Concurrent_inserts_are_single_and_mixed_states_conflict(bool mixed)
    {
        using var client = Client(owner);
        var results = await Task.WhenAll(Put(client, "2026-01-31", "Paid", 10), Put(client, "2026-01-31", mixed ? "Skipped" : "Paid", mixed ? null : 10));
        if (mixed)
        {
            Assert.Single(results, x => x.StatusCode == HttpStatusCode.OK);
            await Problem(Assert.Single(results, x => x.StatusCode == HttpStatusCode.Conflict), HttpStatusCode.Conflict);
        }
        else Assert.Equal(await Read(results[0]), await Read(results[1]));
        Assert.Equal(1, await db.PaymentOccurrences.CountAsync());
    }

    [Fact]
    public async Task Existing_conflicting_writers_require_current_version_and_do_not_overwrite()
    {
        using var client = Client(owner);
        var paid = await Read(await Put(client, "2026-01-31", "Paid", 10));
        await Problem(await Put(client, "2026-01-31", "Skipped"), HttpStatusCode.Conflict);
        var results = await Task.WhenAll(Put(client, "2026-01-31", "Skipped", version: paid.RowVersion), Put(client, "2026-01-31", "Paid", 20, paid.RowVersion));
        Assert.Single(results, x => x.StatusCode == HttpStatusCode.OK);
        await Problem(Assert.Single(results, x => x.StatusCode == HttpStatusCode.Conflict), HttpStatusCode.Conflict);
        await using var second = Context();
        var stale = await second.PaymentOccurrences.SingleAsync();
        var current = await db.PaymentOccurrences.SingleAsync();
        current.ChangeStatus(OccurrenceStatus.Paid, 40, DateTime.UtcNow); await db.SaveChangesAsync();
        stale.ChangeStatus(OccurrenceStatus.Paid, 50, DateTime.UtcNow);
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => second.SaveChangesAsync());
    }

    [Fact]
    public async Task Unique_foreign_key_constraints_and_existing_plan_delete_protection_work()
    {
        using var client = Client(owner);
        await Put(client, "2026-01-31", "Skipped");
        db.PaymentOccurrences.Add(PaymentOccurrence.Create(plan.Id, new(2026, 1, 31), OccurrenceStatus.Skipped, null, DateTime.UtcNow));
        var duplicate = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        Assert.Contains(((SqlException)duplicate.InnerException!).Number, new[] { 2601, 2627 });
        db.ChangeTracker.Clear();
        db.PaymentOccurrences.Add(PaymentOccurrence.Create(Guid.NewGuid(), new(2026, 1, 31), OccurrenceStatus.Skipped, null, DateTime.UtcNow));
        var foreign = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        Assert.Equal(547, ((SqlException)foreign.InnerException!).Number); db.ChangeTracker.Clear();
        using var delete = new HttpRequestMessage(HttpMethod.Delete, $"/api/payments/{plan.Id}");
        delete.Headers.TryAddWithoutValidation("If-Match", $"\"{Convert.ToBase64String(plan.RowVersion)}\"");
        await Problem(await client.SendAsync(delete), HttpStatusCode.Conflict);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Store_translates_real_unique_or_foreign_key_failure_and_rolls_back(bool foreignKey)
    {
        using var client = Client(owner); await Put(client, "2026-01-31", "Skipped");
        await using var context = Context();
        var store = new OccurrenceStore(context);
        await Assert.ThrowsAsync<ResourceConflictException>(() => store.MutateOwnedAsync(owner, plan.Id, new(2026, 2, 28),
            (_, _) => new(PaymentOccurrence.Create(foreignKey ? Guid.NewGuid() : plan.Id, new(2026, 1, 31),
                OccurrenceStatus.Skipped, null, DateTime.UtcNow), true), default));
        Assert.Equal(1, await db.PaymentOccurrences.CountAsync());
        Assert.Equal(new DateOnly(2026, 1, 31), (await db.PaymentOccurrences.SingleAsync()).DueDate);
    }

    [Fact]
    public async Task Stored_history_survives_plan_schedule_update_without_accepting_invalid_future_mutation()
    {
        using var client = Client(owner);
        var paid = await Read(await Put(client, "2026-01-31", "Paid", 10));
        var update = await client.PutAsJsonAsync($"/api/payments/{plan.Id}", new
        {
            name = "Changed schedule", amount = 10, currencyCode = "TRY", category = "Bills", frequency = "Monthly", interval = 1,
            startDate = "2026-02-01", endDate = "2026-12-31", rowVersion = Convert.ToBase64String(plan.RowVersion)
        });
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        Assert.Equal(paid, await Read(await client.GetAsync(Root + "/2026-01-31")));
        await Problem(await Put(client, "2026-01-31", "Pending", version: paid.RowVersion), HttpStatusCode.BadRequest);
        Assert.Equal(OccurrenceStatus.Pending, (await Read(await client.GetAsync(Root + "/2026-02-01"))).Status);
        Assert.Equal(1, await db.PaymentOccurrences.CountAsync());
    }

    [Fact]
    public async Task Openapi_exposes_three_occurrence_operations_and_required_status()
    {
        using var client = Client(owner);
        using var document = JsonDocument.Parse(await client.GetStringAsync("/swagger/v1/swagger.json"));
        var paths = document.RootElement.GetProperty("paths");
        Assert.True(paths.GetProperty("/api/payments/{paymentId}/occurrences").TryGetProperty("get", out _));
        Assert.True(paths.GetProperty("/api/payments/{paymentId}/occurrences/{dueDate}").TryGetProperty("get", out _));
        Assert.True(paths.GetProperty("/api/payments/{paymentId}/occurrences/{dueDate}/status").TryGetProperty("put", out _));
        var required = document.RootElement.GetProperty("components").GetProperty("schemas").GetProperty("SetOccurrenceStatusRequest").GetProperty("required");
        Assert.Contains(required.EnumerateArray(), x => x.GetString() == "status");
    }

    [Fact]
    public async Task Failure_after_save_rolls_back_the_transaction()
    {
        await using var failing = Context(new ThrowAfterSave());
        await Assert.ThrowsAsync<InvalidOperationException>(() => Service(failing).SetStatusAsync(plan.Id, new(2026, 1, 31),
            new() { Status = OccurrenceStatus.Paid, ActualAmount = 10 }, default));
        Assert.Equal(0, await db.PaymentOccurrences.CountAsync());
        Assert.True(await db.PaymentPlans.AnyAsync(x => x.Id == plan.Id));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Concurrent_plan_change_or_delete_is_observed_before_occurrence_validation(bool delete)
    {
        var signal = new ParentReadSignal();
        await using var writing = Context(signal);
        await using var raw = new SqlConnection(connection); await raw.OpenAsync();
        await using var transaction = (SqlTransaction)await raw.BeginTransactionAsync();
        await using var command = raw.CreateCommand(); command.Transaction = transaction;
        command.CommandText = "SELECT Id FROM PaymentPlans WITH (UPDLOCK,HOLDLOCK) WHERE Id=@id";
        command.Parameters.AddWithValue("@id", plan.Id); await command.ExecuteScalarAsync();
        var pending = Service(writing).SetStatusAsync(plan.Id, new(2026, 2, 28), new() { Status = OccurrenceStatus.Paid, ActualAmount = 10 }, default);
        await signal.Started.Task.WaitAsync(TimeSpan.FromSeconds(10));
        command.CommandText = delete ? "DELETE FROM PaymentPlans WHERE Id=@id" : "UPDATE PaymentPlans SET Frequency=0 WHERE Id=@id";
        await command.ExecuteNonQueryAsync(); await transaction.CommitAsync();
        if (delete) await Assert.ThrowsAsync<ResourceNotFoundException>(() => pending);
        else await Assert.ThrowsAsync<DomainRuleException>(() => pending);
        Assert.Equal(0, await db.PaymentOccurrences.CountAsync());
    }

    private HttpClient Client(Guid id, bool authenticated = true)
    {
        var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder => builder.UseEnvironment("Development").UseTestIdentity()
            .UseSetting("ConnectionStrings:PayAhead", connection).UseSetting("DevelopmentIdentity:Enabled", authenticated.ToString())
            .UseSetting("DevelopmentIdentity:UserId", id.ToString()));
        factories.Add(factory); return factory.CreateClient();
    }
    private Task<HttpResponseMessage> Put(HttpClient client, string date, string status, decimal? amount = null, string? version = null)
        => client.PutAsJsonAsync(Root + $"/{date}/status", new { status, actualAmount = amount, rowVersion = version });
    private static async Task<OccurrenceResponse> Read(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<OccurrenceResponse>(Json))!;
    }
    private static async Task Problem(HttpResponseMessage response, HttpStatusCode status)
    {
        Assert.Equal(status, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal((int)status, json.RootElement.GetProperty("status").GetInt32());
        Assert.True(json.RootElement.TryGetProperty("traceId", out _));
    }
    private PayAheadDbContext Context(params IInterceptor[] interceptors) => new(new DbContextOptionsBuilder<PayAheadDbContext>()
        .UseSqlServer(connection, sql => sql.EnableRetryOnFailure(3)).AddInterceptors(interceptors).Options);
    private OccurrenceService Service(PayAheadDbContext context) => new(new OccurrenceStore(context), new CurrentOwner(owner),
        new PaymentRecurrenceCalculator(), TimeProvider.System, new SetOccurrenceStatusValidator(), new OccurrenceListValidator());
    private sealed class CurrentOwner(Guid owner) : ICurrentUser { public Guid? UserId => owner; }
    private sealed class ThrowAfterSave : SaveChangesInterceptor
    {
        public override ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData, int result, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("Test transaction rollback after SQL write");
    }
    private sealed class ParentReadSignal : DbCommandInterceptor
    {
        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData eventData,
            InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        {
            if (command.CommandText.Contains("UPDLOCK", StringComparison.Ordinal)) Started.TrySetResult();
            return ValueTask.FromResult(result);
        }
    }
    private static JsonSerializerOptions CreateJson()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter<OccurrenceStatus>(allowIntegerValues: false));
        return options;
    }
}
