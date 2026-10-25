using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using PayAhead.Api;
using PayAhead.Application.Catalog;
using PayAhead.Domain;
using PayAhead.Infrastructure;
using Xunit;
namespace PayAhead.IntegrationTests;

public sealed class CatalogIntegrationTests(SqlServerFixture sql) : IClassFixture<SqlServerFixture>, IAsyncLifetime
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() } };
    private string connection = ""; private readonly Guid user = Guid.NewGuid(), other = Guid.NewGuid(); private WebApplicationFactory<Program> factory = null!; private HttpClient client = null!;
    private PayAheadDbContext Db() => new(new DbContextOptionsBuilder<PayAheadDbContext>().UseSqlServer(connection).Options);
    public async Task InitializeAsync()
    {
        connection = new SqlConnectionStringBuilder(sql.ConnectionString) { InitialCatalog = "PayAheadTest_" + Guid.NewGuid().ToString("N") }.ConnectionString;
        await using var db = Db(); await db.Database.MigrateAsync();
        foreach (var id in new[] { user, other }) { db.Users.Add(new() { Id = id, Email = id + "@example.invalid", NormalizedEmail = (id + "@EXAMPLE.INVALID").ToUpperInvariant(), DisplayName = "Catalog test", PasswordHash = "seed-only", CreatedAtUtc = DateTime.UtcNow, UpdatedAtUtc = DateTime.UtcNow }); db.UserSettings.Add(new() { UserId = id, CreatedAtUtc = DateTime.UtcNow, UpdatedAtUtc = DateTime.UtcNow }); }
        await db.SaveChangesAsync();
        factory = new WebApplicationFactory<Program>().WithWebHostBuilder(b => b.UseEnvironment("Development").UseSetting("ConnectionStrings:PayAhead", connection).UseSetting("DevelopmentIdentity:Enabled", "false").UseSetting("Notifications:Enabled", "false"));
        client = factory.CreateClient(); Authenticate(user);
    }
    private void Authenticate(Guid id) => client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", new TokenService(factory.Services.GetRequiredService<JwtSettings>(), TimeProvider.System).CreateAccessToken(id).Token);
    public async Task DisposeAsync() { client.Dispose(); await factory.DisposeAsync(); await using var db = Db(); Assert.StartsWith("PayAheadTest_", db.Database.GetDbConnection().Database); await db.Database.EnsureDeletedAsync(); }
    private static Guid C(string code) => CatalogSeed.Id(code); private static Guid S(string code) => CatalogSeed.Id("service." + code);
    private static JsonObject Payment(Guid? category = null, Guid? service = null) => new() { ["name"] = "Private custom name", ["description"] = "Private", ["amount"] = 12.3456m, ["currencyCode"] = "TRY", ["category"] = "Legacy label", ["frequency"] = "Once", ["interval"] = 1, ["startDate"] = "2026-10-25", ["isActive"] = true, ["catalogCategoryId"] = category, ["catalogServiceId"] = service };
    private static JsonObject Subscription(Guid? category = null, Guid? service = null) => new() { ["name"] = "Private subscription", ["providerName"] = "Private provider", ["amount"] = 12.3456m, ["currencyCode"] = "TRY", ["billingFrequency"] = "Monthly", ["billingInterval"] = 1, ["startDate"] = "2026-10-25", ["firstBillingDate"] = "2026-10-25", ["status"] = "Active", ["autoRenew"] = true, ["enableTrialReminders"] = false, ["catalogCategoryId"] = category, ["catalogServiceId"] = service };
    private async Task<JsonObject> Read(HttpResponseMessage response, HttpStatusCode expected = HttpStatusCode.OK) { Assert.Equal(expected, response.StatusCode); return JsonNode.Parse(await response.Content.ReadAsStringAsync())!.AsObject(); }
    [Fact] public async Task Categories_services_search_filters_and_active_detail_work()
    {
        var cats = (await client.GetFromJsonAsync<CatalogCategoryResponse[]>("/api/catalog/categories?applicableTo=Subscription", Json))!; Assert.DoesNotContain(cats, x => x.Code == "transport"); Assert.Contains(cats, x => x.Code == "video" && x.ParentId == C("digital"));
        foreach (var search in new[] { "arac", "ARAÇ" }) { var found = await client.GetFromJsonAsync<CatalogCategoryResponse[]>("/api/catalog/categories?search=" + Uri.EscapeDataString(search), Json); Assert.Contains(found!, x => x.Code == "transport"); }
        foreach (var search in new[] { "net", "spot", "dogalgaz", "turk", "Gemini", "Max" }) { var result = await Read(await client.GetAsync("/api/catalog/services?search=" + Uri.EscapeDataString(search))); Assert.True(result["totalCount"]!.GetValue<int>() > 0); }
        var filtered = await Read(await client.GetAsync($"/api/catalog/services?categoryId={C("transport.diesel")}&applicableTo=Payment&pageSize=1")); Assert.Single(filtered["items"]!.AsArray()); Assert.True(filtered["totalCount"]!.GetValue<int>() > 1);
        var shell = await Read(await client.GetAsync($"/api/catalog/services/{S("shell")}")); Assert.Equal(4, shell["categoryIds"]!.AsArray().Count);
        Assert.Equal("Payment", shell["applicableTo"]!.GetValue<string>());
        Assert.Equal("Once", shell["defaultRecurrenceType"]!.GetValue<string>());
        var categoryWire = JsonNode.Parse(await client.GetStringAsync("/api/catalog/categories"))!.AsArray();
        Assert.All(categoryWire, x => Assert.Contains(x!["applicableTo"]!.GetValue<string>(), new[] { "Payment", "Subscription", "Both" }));
        var inactive = await Read(await client.GetAsync($"/api/catalog/services/{S("bp")}")); Assert.False(inactive["isActive"]!.GetValue<bool>());
    }
    [Fact] public async Task Expanded_category_paths_and_hierarchical_search_return_services_in_correct_mode()
    {
        foreach (var code in new[] { "transport", "transport.fuel", "transport.service", "transport.inspection", "transport.vehicle-tax" })
        {
            var result = await Read(await client.GetAsync($"/api/catalog/services?categoryId={C(code)}&applicableTo=Payment&pageSize=100"));
            Assert.NotEmpty(result["items"]!.AsArray());
        }
        var shell = await Read(await client.GetAsync($"/api/catalog/services?categoryId={C("transport.fuel")}&search=Shell&applicableTo=Payment"));
        Assert.Equal("shell", Assert.Single(shell["items"]!.AsArray())!["code"]!.GetValue<string>());
        var hidden = await Read(await client.GetAsync($"/api/catalog/services?categoryId={C("transport")}&applicableTo=Subscription")); Assert.Empty(hidden["items"]!.AsArray());
        var news = await client.GetFromJsonAsync<CatalogCategoryResponse[]>($"/api/catalog/categories?categoryId={C("news")}&applicableTo=Subscription", Json); Assert.Equal(10, news!.Length);
        var papers = await Read(await client.GetAsync($"/api/catalog/services?categoryId={C("news.digital-newspaper")}&applicableTo=Subscription")); Assert.Contains(papers["items"]!.AsArray(), x => x!["code"]!.GetValue<string>() == "new-york-times");
        foreach (var text in new[] { "ARAÇ", "arac", "Haber", "gazete" }) Assert.True((await Read(await client.GetAsync("/api/catalog/services?search=" + Uri.EscapeDataString(text))))["totalCount"]!.GetValue<int>() > 0);
    }
    [Fact] public async Task Coverage_migration_is_additive_and_preserves_existing_catalog_and_linked_private_record()
    {
        await using var db = Db(); var migrator = db.GetService<IMigrator>(); await migrator.MigrateAsync("20261025134913_CatalogFoundation");
        var oldCategories = await db.CatalogCategories.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(); var oldServices = await db.CatalogServices.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync();
        var request = Payment(C("video"), S("netflix")); var created = await Read(await client.PostAsJsonAsync("/api/payments", request), HttpStatusCode.Created); var id = created["id"]!.GetValue<Guid>();
        var before = await db.PaymentPlans.AsNoTracking().SingleAsync(x => x.Id == id);
        await migrator.MigrateAsync(); await CatalogSeed.EnsureMissingAsync(db); await CatalogSeed.EnsureMissingAsync(db);
        Assert.Equal(116, await db.CatalogCategories.CountAsync()); Assert.Equal(104, await db.CatalogServices.CountAsync()); Assert.Equal(245, await db.CatalogServiceCategories.CountAsync());
        foreach (var old in oldCategories) Assert.Equal(JsonSerializer.Serialize(old), JsonSerializer.Serialize(await db.CatalogCategories.AsNoTracking().SingleAsync(x => x.Id == old.Id)));
        foreach (var old in oldServices) Assert.Equal(JsonSerializer.Serialize(old), JsonSerializer.Serialize(await db.CatalogServices.AsNoTracking().SingleAsync(x => x.Id == old.Id)));
        var after = await db.PaymentPlans.AsNoTracking().SingleAsync(x => x.Id == id); Assert.Equal(JsonSerializer.Serialize(before), JsonSerializer.Serialize(after)); Assert.False(db.Database.HasPendingModelChanges());
    }
    [Theory]
    [InlineData("pageSize=101")][InlineData("pageNumber=0")][InlineData("applicableTo=99")][InlineData("countryCode=invalid")]
    public async Task Invalid_filters_are_400(string query) => Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/api/catalog/services?" + query)).StatusCode);
    [Fact] public async Task Anonymous_write_and_invalid_catalog_selection_are_rejected()
    {
        client.DefaultRequestHeaders.Authorization = null; Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/catalog/categories")).StatusCode); Authenticate(user);
        Assert.Equal(HttpStatusCode.MethodNotAllowed, (await client.PostAsJsonAsync("/api/catalog/services", new { displayName = "Injected" })).StatusCode);
        foreach (var body in new[] { Payment(Guid.NewGuid()), Payment(C("transport.petrol"), S("netflix")), Payment(C("transport.petrol"), S("bp")), Payment(null, S("shell")) }) Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/payments", body)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/subscriptions", Subscription(C("transport.petrol"), S("shell")))).StatusCode);
        await using var db = Db(); Assert.Empty(await db.PaymentPlans.ToArrayAsync());
    }
    [Fact] public async Task Catalog_payment_custom_names_legacy_put_isolation_and_concurrency_are_preserved()
    {
        var created = await Read(await client.PostAsJsonAsync("/api/payments", Payment(C("transport.petrol"), S("shell"))), HttpStatusCode.Created); var id = created["id"]!.GetValue<Guid>();
        Assert.Equal("Private custom name", created["name"]!.GetValue<string>()); Assert.Equal("Benzin", created["category"]!.GetValue<string>());
        var update = Payment(); update.Remove("catalogCategoryId"); update.Remove("catalogServiceId"); update["rowVersion"] = created["rowVersion"]!.GetValue<string>(); update["name"] = "Changed private name";
        var changed = await Read(await client.PutAsJsonAsync($"/api/payments/{id}", update)); Assert.Equal(S("shell"), changed["catalogServiceId"]!.GetValue<Guid>()); Assert.Equal("Benzin", changed["category"]!.GetValue<string>());
        Assert.Equal(HttpStatusCode.Conflict, (await client.PutAsJsonAsync($"/api/payments/{id}", update)).StatusCode);
        Authenticate(other); Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/payments/{id}")).StatusCode); Authenticate(user);
        await using var db = Db(); var service = await db.CatalogServices.SingleAsync(x => x.Id == S("shell")); service.IsActive = false; await db.SaveChangesAsync();
        update["rowVersion"] = changed["rowVersion"]!.GetValue<string>(); Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync($"/api/payments/{id}", update)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/payments", Payment(C("transport.petrol"), S("shell")))).StatusCode);
        Assert.Equal(CatalogSeed.Services.Count, await db.CatalogServices.CountAsync());
    }
    [Fact] public async Task Subscription_uses_single_linked_plan_and_totals_do_not_double_count_catalog()
    {
        var request = Subscription(C("video"), S("netflix")); var created = await Read(await client.PostAsJsonAsync("/api/subscriptions", request), HttpStatusCode.Created); var id = created["id"]!.GetValue<Guid>();
        Assert.Equal(C("video"), created["catalogCategoryId"]!.GetValue<Guid>());
        await using var db = Db(); Assert.Single(await db.PaymentPlans.ToArrayAsync()); Assert.Single(await db.TrackedSubscriptions.ToArrayAsync());
        var dashboard = await Read(await client.GetAsync("/api/dashboard/summary?fromDate=2026-10-25&toDate=2026-10-25")); var calendar = await Read(await client.GetAsync("/api/calendar?year=2026&month=10")); Assert.Single(calendar["days"]!.AsArray()); Assert.Single(calendar["days"]![0]!["payments"]!.AsArray());
        request.Remove("catalogCategoryId"); request.Remove("catalogServiceId"); request["rowVersion"] = created["rowVersion"]!.GetValue<string>(); request["description"] = "Only metadata changed";
        var updated = await Read(await client.PutAsJsonAsync($"/api/subscriptions/{id}", request)); Assert.Equal(S("netflix"), updated["catalogServiceId"]!.GetValue<Guid>());
        var after = await Read(await client.GetAsync("/api/dashboard/summary?fromDate=2026-10-25&toDate=2026-10-25")); Assert.Equal(dashboard["upcomingTotalsByCurrency"]!.ToJsonString(), after["upcomingTotalsByCurrency"]!.ToJsonString());
        Assert.Equal(12.3456m, (await db.PaymentPlans.AsNoTracking().SingleAsync()).Amount);
    }
    [Fact] public async Task Seed_is_idempotent_preserves_operator_labels_and_rejects_FK_code_duplicates()
    {
        await using var db = Db(); var c = await db.CatalogCategories.SingleAsync(x => x.Code == "other"); c.NameTr = "Operator label"; await db.SaveChangesAsync();
        await CatalogSeed.EnsureMissingAsync(db); await CatalogSeed.EnsureMissingAsync(db); Assert.Equal(116, await db.CatalogCategories.CountAsync()); Assert.Equal(CatalogSeed.Services.Count, await db.CatalogServices.CountAsync()); Assert.Equal("Operator label", (await db.CatalogCategories.AsNoTracking().SingleAsync(x => x.Id == c.Id)).NameTr);
        db.CatalogCategories.Add(new() { Id = Guid.NewGuid(), Code = "other", NameTr = "duplicate", NameEn = "duplicate" }); await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync()); db.ChangeTracker.Clear();
        db.PaymentPlans.Add(new() { Id = Guid.NewGuid(), UserId = user, Name = "Bad FK", Amount = 1, StartDate = new(2026, 10, 25), CreatedAtUtc = DateTime.UtcNow, UpdatedAtUtc = DateTime.UtcNow, CatalogCategoryId = C("transport.petrol"), CatalogServiceId = S("netflix") }); await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }
    [Fact] public async Task Migration_upgrade_and_rollback_preserve_legacy_financial_rows()
    {
        await using var db = Db(); var migrator = db.GetService<IMigrator>(); await migrator.MigrateAsync("20261023190720_PasswordResetTokens"); var plan = Guid.NewGuid();
        var legacyAmount = new SqlParameter("LegacyAmount", System.Data.SqlDbType.Decimal) { Precision = 19, Scale = 4, Value = 123.4567m };
        await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO PaymentPlans (Id,UserId,Name,Description,Amount,CurrencyCode,Category,Frequency,Interval,StartDate,EndDate,AnchorDay,IsActive,CreatedAtUtc,UpdatedAtUtc) VALUES ({plan},{user},{"Legacy exact name"},NULL,{legacyAmount},{"TRY"},{"Legacy category"},{0},{1},{new DateOnly(2026,10,25)},NULL,NULL,{true},{DateTime.UtcNow},{DateTime.UtcNow})");
        await migrator.MigrateAsync(); var legacy = await db.PaymentPlans.AsNoTracking().SingleAsync(); Assert.Equal("Legacy exact name", legacy.Name); Assert.Equal("Legacy category", legacy.Category); Assert.Equal(123.4567m, legacy.Amount); Assert.Null(legacy.CatalogCategoryId); Assert.Null(legacy.CatalogServiceId);
        await migrator.MigrateAsync("20261023190720_PasswordResetTokens"); await migrator.MigrateAsync(); Assert.Equal(123.4567m, (await db.PaymentPlans.AsNoTracking().SingleAsync()).Amount); Assert.Equal(116, await db.CatalogCategories.CountAsync()); Assert.False(db.Database.HasPendingModelChanges());
    }
    [Fact] public async Task Custom_catalog_payment_does_not_bypass_free_plan_limits()
    {
        for (var i = 0; i < 5; i++) Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync("/api/payments", Payment(C("other")))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync("/api/payments", Payment(C("other")))).StatusCode);
    }
}
