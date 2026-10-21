using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using PayAhead.Application.Payments;
using PayAhead.Domain;
using PayAhead.Infrastructure;
using Xunit;

namespace PayAhead.IntegrationTests;

public sealed class PaymentApiTests : IClassFixture<SqlServerFixture>, IAsyncLifetime
{
    private readonly string connection;
    private readonly Guid owner = Guid.NewGuid();
    private readonly Guid other = Guid.NewGuid();
    private readonly PayAheadDbContext db;
    private readonly List<WebApplicationFactory<Program>> factories = [];
    private static readonly JsonSerializerOptions Json = CreateJson();

    public PaymentApiTests(SqlServerFixture server)
    {
        connection = new SqlConnectionStringBuilder(server.ConnectionString)
        { InitialCatalog = "PayAheadTest_" + Guid.NewGuid().ToString("N") }.ConnectionString;
        db = new(new DbContextOptionsBuilder<PayAheadDbContext>().UseSqlServer(connection).Options);
    }

    public async Task InitializeAsync()
    {
        await db.Database.MigrateAsync();
        foreach (var id in new[] { owner, other }) db.Users.Add(new User
        {
            Id = id, Email = $"{id:N}@example.invalid", NormalizedEmail = $"{id:N}@EXAMPLE.INVALID",
            DisplayName = "Test", PasswordHash = "test-only-placeholder", CreatedAtUtc = DateTime.UtcNow, UpdatedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        foreach (var factory in factories) await factory.DisposeAsync();
        if (!db.Database.GetDbConnection().Database.StartsWith("PayAheadTest_", StringComparison.Ordinal))
            throw new InvalidOperationException("Yalnızca izole test veritabanı temizlenebilir.");
        await db.Database.EnsureDeletedAsync();
        await db.DisposeAsync();
    }

    [Fact]
    public async Task Crud_persists_dtos_location_timestamps_and_concurrency_tokens()
    {
        using var client = Client(owner);
        var response = await client.PostAsJsonAsync("/api/payments", Body());
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await Payment(response);
        Assert.Equal($"http://localhost/api/payments/{created.Id}", response.Headers.Location!.ToString());
        Assert.Equal($"\"{created.RowVersion}\"", response.Headers.ETag!.Tag);
        Assert.Equal("Payment", created.Name); Assert.Equal("USD", created.CurrencyCode);
        Assert.Equal(owner, (await db.PaymentPlans.AsNoTracking().SingleAsync()).UserId);
        var list = await client.GetFromJsonAsync<PagedResult<PaymentResponse>>("/api/payments", Json);
        Assert.Single(list!.Items); Assert.Equal(1, list.TotalCount); Assert.Equal(1, list.TotalPages);
        Assert.Equal(20, list.PageSize); Assert.Equal(1, list.PageNumber);
        var get = await Payment(await client.GetAsync(response.Headers.Location));
        Assert.Equal(created, get);
        Assert.Equal(DateTimeKind.Utc, get.CreatedAtUtc.Kind);
        Assert.Equal(DateTimeKind.Utc, get.UpdatedAtUtc.Kind);
        var update = Body(); update["name"] = "Updated"; update["amount"] = 99.1234m;
        update["isActive"] = false; update["rowVersion"] = created.RowVersion;
        var put = await client.PutAsJsonAsync($"/api/payments/{created.Id}", update);
        Assert.Equal(HttpStatusCode.OK, put.StatusCode);
        var changed = await Payment(put);
        Assert.Equal("Updated", changed.Name); Assert.Equal(99.1234m, changed.Amount); Assert.False(changed.IsActive);
        Assert.Equal(created.CreatedAtUtc, changed.CreatedAtUtc); Assert.True(changed.UpdatedAtUtc > created.UpdatedAtUtc);
        Assert.NotEqual(created.RowVersion, changed.RowVersion);
        await Problem(await client.PutAsJsonAsync($"/api/payments/{created.Id}", update), HttpStatusCode.Conflict);
        await Problem(await Delete(client, created.Id, created.RowVersion), HttpStatusCode.Conflict);
        Assert.Equal(changed, await Payment(await client.GetAsync($"/api/payments/{created.Id}")));
        Assert.Equal(HttpStatusCode.NoContent, (await Delete(client, created.Id, changed.RowVersion)).StatusCode);
        await Problem(await client.GetAsync($"/api/payments/{created.Id}"), HttpStatusCode.NotFound);
        Assert.Empty(await db.PaymentPlans.AsNoTracking().ToListAsync());
    }

    [Fact]
    public async Task Openapi_documents_all_payment_operations_and_required_write_fields()
    {
        using var client = Client(owner);
        using var document = JsonDocument.Parse(await client.GetStringAsync("/swagger/v1/swagger.json"));
        var paths = document.RootElement.GetProperty("paths");
        Assert.True(paths.GetProperty("/api/payments").TryGetProperty("post", out _));
        Assert.True(paths.GetProperty("/api/payments").TryGetProperty("get", out _));
        var detail = paths.GetProperty("/api/payments/{id}");
        foreach (var method in new[] { "get", "put", "delete" }) Assert.True(detail.TryGetProperty(method, out _));
        var schemas = document.RootElement.GetProperty("components").GetProperty("schemas");
        var required = schemas.GetProperty("UpdatePaymentRequest").GetProperty("required").EnumerateArray().Select(x => x.GetString()).ToArray();
        Assert.Contains("rowVersion", required); Assert.Contains("name", required); Assert.Contains("startDate", required);
    }

    [Fact]
    public async Task Other_user_cannot_list_get_update_or_delete_owned_resources()
    {
        using var first = Client(owner); using var second = Client(other);
        var plan = await Payment(await first.PostAsJsonAsync("/api/payments", Body()));
        var list = await second.GetFromJsonAsync<PagedResult<PaymentResponse>>("/api/payments", Json);
        Assert.Empty(list!.Items); Assert.Equal(0, list.TotalCount);
        await Problem(await second.GetAsync($"/api/payments/{plan.Id}"), HttpStatusCode.NotFound);
        var update = Body(); update["rowVersion"] = plan.RowVersion;
        await Problem(await second.PutAsJsonAsync($"/api/payments/{plan.Id}", update), HttpStatusCode.NotFound);
        await Problem(await Delete(second, plan.Id, plan.RowVersion), HttpStatusCode.NotFound);
        Assert.Equal(plan, await Payment(await first.GetAsync($"/api/payments/{plan.Id}")));
        var malicious = Body(); malicious["userId"] = other;
        await Problem(await first.PostAsJsonAsync("/api/payments", malicious), HttpStatusCode.BadRequest);
        update["userId"] = other;
        await Problem(await first.PutAsJsonAsync($"/api/payments/{plan.Id}", update), HttpStatusCode.BadRequest);
        Assert.Equal(owner, (await db.PaymentPlans.AsNoTracking().SingleAsync()).UserId);
    }

    [Fact]
    public async Task Filtering_and_pagination_are_composed_and_sorting_is_deterministic()
    {
        using var client = Client(owner); using var otherClient = Client(other);
        foreach (var (name, currency, category, active) in new[]
        {
            ("Alpha One", "USD", "Bills", true), ("Alpha Two", "USD", "Bills", true),
            ("Alpha Three", "EUR", "Bills", true), ("Beta", "USD", "Other", false), ("Percent % literal", "TRY", "Bills", true)
        })
        {
            var body = Body(); body["name"] = name; body["currencyCode"] = currency; body["category"] = category; body["isActive"] = active;
            Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync("/api/payments", body)).StatusCode);
        }
        await otherClient.PostAsJsonAsync("/api/payments", Body());
        var filtered = await client.GetFromJsonAsync<PagedResult<PaymentResponse>>("/api/payments?currencyCode=usd&category=Bills&isActive=true&search=Alpha&pageSize=1", Json);
        Assert.Equal(2, filtered!.TotalCount); Assert.Equal(2, filtered.TotalPages); Assert.Single(filtered.Items);
        var second = await client.GetFromJsonAsync<PagedResult<PaymentResponse>>("/api/payments?currencyCode=USD&category=Bills&isActive=true&search=Alpha&pageSize=1&pageNumber=2", Json);
        Assert.NotEqual(filtered.Items[0].Id, second!.Items[0].Id);
        var inactive = await client.GetFromJsonAsync<PagedResult<PaymentResponse>>("/api/payments?isActive=false", Json);
        Assert.Equal("Beta", Assert.Single(inactive!.Items).Name);
        var percent = await client.GetFromJsonAsync<PagedResult<PaymentResponse>>("/api/payments?search=%25", Json);
        Assert.Equal("Percent % literal", Assert.Single(percent!.Items).Name);
        var injection = await client.GetFromJsonAsync<PagedResult<PaymentResponse>>("/api/payments?search=%27%20OR%201%3D1--", Json);
        Assert.Empty(injection!.Items);
        // Aynı tarihteki kayıtlar da Id bağlayıcı sıralamasıyla kararlıdır.
        await db.PaymentPlans.Where(x => x.UserId == owner).ExecuteUpdateAsync(set => set.SetProperty(x => x.CreatedAtUtc, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)));
        var all = await client.GetFromJsonAsync<PagedResult<PaymentResponse>>("/api/payments", Json);
        var again = await client.GetFromJsonAsync<PagedResult<PaymentResponse>>("/api/payments", Json);
        Assert.Equal(5, all!.TotalCount); Assert.Equal(all.Items.Select(x => x.Id), again!.Items.Select(x => x.Id));
        var empty = await client.GetFromJsonAsync<PagedResult<PaymentResponse>>("/api/payments?pageNumber=999", Json);
        Assert.Empty(empty!.Items); Assert.Equal(5, empty.TotalCount);
    }

    [Theory]
    [InlineData("name", "null")]
    [InlineData("name", "\"   \"")]
    [InlineData("amount", "0")]
    [InlineData("amount", "-1")]
    [InlineData("amount", "1.12345")]
    [InlineData("amount", "1000000000000000")]
    [InlineData("currencyCode", "\"ZZZ\"")]
    [InlineData("frequency", "\"Invalid\"")]
    [InlineData("frequency", "2")]
    [InlineData("interval", "0")]
    [InlineData("endDate", "\"2025-01-01\"")]
    [InlineData("startDate", "\"0001-01-01\"")]
    [InlineData("startDate", "\"not-a-date\"")]
    [InlineData("anchorDay", "32")]
    [InlineData("createdAtUtc", "\"2020-01-01T00:00:00Z\"")]
    public async Task Invalid_create_and_update_return_problem_details_and_leave_data_unchanged(string field, string value)
    {
        using var client = Client(owner);
        var existing = await Payment(await client.PostAsJsonAsync("/api/payments", Body()));
        var body = Body(); body[field] = JsonSerializer.Deserialize<JsonElement>(value);
        await Problem(await client.PostAsJsonAsync("/api/payments", body), HttpStatusCode.BadRequest);
        body["rowVersion"] = existing.RowVersion;
        await Problem(await client.PutAsJsonAsync($"/api/payments/{existing.Id}", body), HttpStatusCode.BadRequest);
        Assert.Equal(existing, await Payment(await client.GetAsync($"/api/payments/{existing.Id}")));
        Assert.Equal(1, await db.PaymentPlans.CountAsync());
    }

    [Theory]
    [InlineData("pageNumber=0")]
    [InlineData("pageSize=101")]
    [InlineData("pageSize=0")]
    [InlineData("pageNumber=2147483647")]
    [InlineData("currencyCode=ZZZ")]
    [InlineData("isActive=invalid")]
    public async Task Invalid_query_is_rejected(string query)
    {
        using var client = Client(owner);
        await Problem(await client.GetAsync("/api/payments?" + query), HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Missing_body_invalid_identifiers_and_missing_tokens_are_rejected()
    {
        using var client = Client(owner);
        await Problem(await client.PostAsJsonAsync("/api/payments", new { }), HttpStatusCode.BadRequest);
        await Problem(await client.PostAsync("/api/payments", new StringContent("{", System.Text.Encoding.UTF8, "application/json")), HttpStatusCode.BadRequest);
        await Problem(await client.GetAsync("/api/payments/not-a-guid"), HttpStatusCode.BadRequest);
        await Problem(await client.GetAsync($"/api/payments/{Guid.Empty}"), HttpStatusCode.NotFound);
        var plan = await Payment(await client.PostAsJsonAsync("/api/payments", Body()));
        await Problem(await client.PutAsJsonAsync($"/api/payments/{plan.Id}", Body()), HttpStatusCode.BadRequest);
        await Problem(await client.DeleteAsync($"/api/payments/{plan.Id}"), HttpStatusCode.BadRequest);
        await Problem(await Delete(client, plan.Id, "bad"), HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Delete_preserves_related_history_and_reminders(bool reminder)
    {
        using var client = Client(owner);
        var plan = await Payment(await client.PostAsJsonAsync("/api/payments", Body()));
        if (reminder) db.ReminderRules.Add(new() { Id = Guid.NewGuid(), UserId = owner, PaymentPlanId = plan.Id, DaysBefore = 1, TimeOfDay = new(9, 0) });
        else db.PaymentOccurrences.Add(new() { Id = Guid.NewGuid(), PaymentPlanId = plan.Id, DueDate = plan.StartDate, CreatedAtUtc = DateTime.UtcNow });
        await db.SaveChangesAsync();
        await Problem(await Delete(client, plan.Id, plan.RowVersion), HttpStatusCode.Conflict);
        Assert.True(await db.PaymentPlans.AnyAsync(x => x.Id == plan.Id));
        if (reminder) Assert.Equal(1, await db.ReminderRules.CountAsync());
        else Assert.Equal(1, await db.PaymentOccurrences.CountAsync());
    }

    [Fact]
    public async Task Two_writers_using_one_version_cannot_both_succeed()
    {
        using var client = Client(owner);
        var plan = await Payment(await client.PostAsJsonAsync("/api/payments", Body()));
        var first = Body(); first["rowVersion"] = plan.RowVersion; first["name"] = "Writer A";
        var second = Body(); second["rowVersion"] = plan.RowVersion; second["name"] = "Writer B";
        var responses = await Task.WhenAll(client.PutAsJsonAsync($"/api/payments/{plan.Id}", first), client.PutAsJsonAsync($"/api/payments/{plan.Id}", second));
        Assert.Single(responses, x => x.StatusCode == HttpStatusCode.OK);
        await Problem(Assert.Single(responses, x => x.StatusCode == HttpStatusCode.Conflict), HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Unauthenticated_requests_and_unprovisioned_identity_cannot_create_data()
    {
        using var anonymous = Client(owner, authenticated: false);
        await Problem(await anonymous.GetAsync("/api/payments"), HttpStatusCode.Unauthorized);
        await Problem(await anonymous.PostAsJsonAsync("/api/payments", Body()), HttpStatusCode.Unauthorized);
        using var missingUser = Client(Guid.NewGuid());
        await Problem(await missingUser.PostAsJsonAsync("/api/payments", Body()), HttpStatusCode.Unauthorized);
        Assert.Empty(await db.PaymentPlans.ToListAsync());
    }

    private HttpClient Client(Guid userId, bool authenticated = true)
    {
        var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder => builder.UseEnvironment("Development").UseRegressionPremium().UseTestIdentity()
            .UseSetting("ConnectionStrings:PayAhead", connection)
            .UseSetting("DevelopmentIdentity:Enabled", authenticated.ToString())
            .UseSetting("DevelopmentIdentity:UserId", userId.ToString()));
        factories.Add(factory);
        return factory.CreateClient();
    }
    private static Dictionary<string, object?> Body() => new()
    {
        ["name"] = " Payment ", ["description"] = "Test", ["amount"] = 12.3456m, ["currencyCode"] = " usd ",
        ["category"] = "Bills", ["frequency"] = "Monthly", ["interval"] = 1, ["startDate"] = "2026-01-31", ["anchorDay"] = 31
    };
    private static async Task<PaymentResponse> Payment(HttpResponseMessage response)
    {
        Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
        var text = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("userId", text); Assert.DoesNotContain("password", text, StringComparison.OrdinalIgnoreCase);
        return JsonSerializer.Deserialize<PaymentResponse>(text, Json)!;
    }
    private static async Task Problem(HttpResponseMessage response, HttpStatusCode expected)
    {
        Assert.Equal(expected, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal((int)expected, document.RootElement.GetProperty("status").GetInt32());
        Assert.True(document.RootElement.TryGetProperty("traceId", out _));
    }
    private static Task<HttpResponseMessage> Delete(HttpClient client, Guid id, string token)
    {
        var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/payments/{id}");
        request.Headers.TryAddWithoutValidation("If-Match", $"\"{token}\"");
        return client.SendAsync(request);
    }
    private static JsonSerializerOptions CreateJson()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter<PaymentFrequency>(allowIntegerValues: false));
        return options;
    }
}
