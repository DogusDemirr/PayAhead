using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using PayAhead.Application.Auth;
using PayAhead.Domain;
using PayAhead.Infrastructure;
using Xunit;
namespace PayAhead.IntegrationTests;
public sealed class AccountSecurityIntegrationTests(SqlServerFixture server) : IClassFixture<SqlServerFixture>, IAsyncLifetime
{
    private string connection = ""; private readonly Guid user = Guid.NewGuid(); private readonly IdentityPasswordService passwords = new();
    private readonly Clock clock = new(); private const string Initial = "initial-long-password!";
    private PayAheadDbContext Db() => new(new DbContextOptionsBuilder<PayAheadDbContext>().UseSqlServer(connection).Options);
    public async Task InitializeAsync()
    {
        connection = new SqlConnectionStringBuilder(server.ConnectionString) { InitialCatalog = "PayAheadTest_" + Guid.NewGuid().ToString("N") }.ConnectionString;
        await using var db = Db(); await db.Database.MigrateAsync(); var row = new User { Id = user, Email = "test@example.invalid", NormalizedEmail = "TEST@EXAMPLE.INVALID", DisplayName = "Test", CreatedAtUtc = clock.GetUtcNow().UtcDateTime, UpdatedAtUtc = clock.GetUtcNow().UtcDateTime };
        row.PasswordHash = passwords.Hash(row, Initial); db.Users.Add(row); await db.SaveChangesAsync();
    }
    public async Task DisposeAsync() { await using var db = Db(); Assert.StartsWith("PayAheadTest_", db.Database.GetDbConnection().Database); await db.Database.EnsureDeletedAsync(); }
    private Task Change(PayAheadDbContext db, string current, string next) => new ProfileStore(db, passwords, clock).ChangePasswordAsync(user, new(current, next, next), CancellationToken.None);
    [Fact] public async Task Three_successes_limit_fourth_and_expiry_keeps_password_history()
    {
        await using var db = Db(); var start = clock.GetUtcNow(); var current = Initial;
        for (var i = 1; i <= 3; i++) { var next = $"normal-long-password-{i}!"; await Change(db, current, next); current = next; if (i < 3) clock.Now = clock.Now.AddDays(1); }
        var store = new ProfileStore(db, passwords, clock); var policy = await store.ReadSecurityAsync(user, default); Assert.Equal(0, policy.RemainingChanges); Assert.Equal(start.UtcDateTime.AddDays(30), policy.NextAvailableAtUtc);
        Assert.Equal("password_change_limit", (await Assert.ThrowsAsync<PasswordSecurityException>(() => Change(db, current, "normal-long-password-4!"))).Code);
        Assert.Equal(3, await db.Set<PasswordHistoryEntry>().CountAsync());
        clock.Now = start.AddDays(30); Assert.Equal(1, (await store.ReadSecurityAsync(user, default)).RemainingChanges); await Change(db, current, "normal-long-password-4!");
        Assert.Equal("password_reused", (await Assert.ThrowsAsync<PasswordSecurityException>(() => Change(db, "normal-long-password-4!", Initial))).Code);
        Assert.Equal(4, await db.Set<PasswordHistoryEntry>().CountAsync()); Assert.Equal(1, await db.Set<PasswordHistoryEntry>().CountAsync(x => x.NormalChangedAtUtc == null));
    }
    [Fact] public async Task Wrong_password_and_other_user_do_not_consume_allowance()
    {
        await using var db = Db(); await Assert.ThrowsAsync<CurrentPasswordException>(() => Change(db, "wrong-long-password!", "another-long-password!"));
        Assert.Empty(await db.Set<PasswordHistoryEntry>().ToArrayAsync()); Assert.Equal(3, (await new ProfileStore(db, passwords, clock).ReadSecurityAsync(user, default)).RemainingChanges);
        await Assert.ThrowsAsync<PayAhead.Application.Payments.AuthenticationRequiredException>(() => new ProfileStore(db, passwords, clock).ReadSecurityAsync(Guid.NewGuid(), default));
    }
    [Fact] public async Task Concurrent_instances_cannot_create_a_fourth_change()
    {
        await using var db = Db(); await Change(db, Initial, "first-long-password!"); await Change(db, "first-long-password!", "second-long-password!");
        async Task<bool> Attempt(string next) { await using var other = Db(); try { await Change(other, "second-long-password!", next); return true; } catch (Exception e) when (e is CurrentPasswordException or PasswordSecurityException) { return false; } }
        var results = await Task.WhenAll(Attempt("third-long-password-A!"), Attempt("third-long-password-B!")); Assert.Single(results, x => x); Assert.Equal(3, await db.Set<PasswordHistoryEntry>().CountAsync());
    }
    [Fact] public async Task Recovery_bypasses_normal_limit_but_enforces_all_known_hashes_and_single_use()
    {
        await using var db = Db(); var current = Initial;
        for (var i = 0; i < 3; i++) { var next = $"normal-limit-password-{i}!"; await Change(db, current, next); current = next; }
        var now = clock.GetUtcNow().UtcDateTime; db.ChangeTracker.Clear(); db.Set<PasswordResetToken>().Add(new() { Id = Guid.NewGuid(), UserId = user, TokenHash = new string('A', 64), CreatedAtUtc = now, ExpiresAtUtc = now.AddMinutes(15) }); await db.SaveChangesAsync();
        var reset = new PasswordResetStore(db);
        string Replacement(User row, IReadOnlyList<string> old, string candidate) { PasswordSecurity.ValidateNew(passwords, row, candidate, old); return passwords.Hash(row, candidate); }
        await Assert.ThrowsAsync<PasswordSecurityException>(() => reset.ConsumeAsync(new string('A', 64), now, (row, old) => Replacement(row, old, Initial), default));
        Assert.Null((await db.Set<PasswordResetToken>().AsNoTracking().SingleAsync()).ConsumedAtUtc);
        Assert.True(await reset.ConsumeAsync(new string('A', 64), now, (row, old) => Replacement(row, old, "recovered-long-password!"), default));
        Assert.False(await reset.ConsumeAsync(new string('A', 64), now, (row, old) => Replacement(row, old, "different-recovery-password!"), default));
        Assert.Equal(0, (await new ProfileStore(db, passwords, clock).ReadSecurityAsync(user, default)).RemainingChanges); Assert.Equal(4, await db.Set<PasswordHistoryEntry>().CountAsync());
    }
    [Fact] public async Task Notice_receipt_is_atomic_and_legacy_history_is_not_invented()
    {
        await using var db = Db(); Assert.Empty(await db.Set<PasswordHistoryEntry>().ToArrayAsync()); Assert.Empty(await db.Set<LegalNoticeReceipt>().ToArrayAsync());
        var other = new User { Id = Guid.NewGuid(), Email = "notice@example.invalid", NormalizedEmail = "NOTICE@EXAMPLE.INVALID", DisplayName = "Notice", CreatedAtUtc = clock.GetUtcNow().UtcDateTime, UpdatedAtUtc = clock.GetUtcNow().UtcDateTime }; other.PasswordHash = passwords.Hash(other, Initial);
        await new AuthStore(db).RegisterAsync(other, default, LegalDocuments.Version); var receipt = await db.Set<LegalNoticeReceipt>().SingleAsync(); Assert.Equal(LegalDocuments.Version, receipt.NoticeVersion); Assert.Equal(other.CreatedAtUtc, receipt.PresentedAtUtc);
    }
    [Fact] public async Task Account_deletion_removes_security_history_and_receipt_only_for_the_owner()
    {
        await using var db = Db(); await Change(db, Initial, "deletion-long-password!");
        db.Set<LegalNoticeReceipt>().Add(new() { UserId = user, NoticeVersion = LegalDocuments.Version, RecordedAtUtc = clock.GetUtcNow().UtcDateTime });
        var other = new User { Id = Guid.NewGuid(), Email = "retained@example.invalid", NormalizedEmail = "RETAINED@EXAMPLE.INVALID", DisplayName = "Retained", CreatedAtUtc = clock.GetUtcNow().UtcDateTime, UpdatedAtUtc = clock.GetUtcNow().UtcDateTime }; other.PasswordHash = passwords.Hash(other, Initial);
        await new AuthStore(db).RegisterAsync(other, default, LegalDocuments.Version);
        db.Set<PasswordHistoryEntry>().Add(new() { Id = Guid.NewGuid(), UserId = other.Id, PasswordHash = passwords.Hash(other, "other-retired-password!") }); await db.SaveChangesAsync();
        await new AccountStore(db, passwords, clock).DeleteAsync(user, "deletion-long-password!", default);
        Assert.Equal(other.Id, (await db.Set<PasswordHistoryEntry>().AsNoTracking().SingleAsync()).UserId);
        Assert.Equal(other.Id, (await db.Set<LegalNoticeReceipt>().AsNoTracking().SingleAsync()).UserId);
        Assert.True((await db.Users.AsNoTracking().SingleAsync(x => x.Id == other.Id)).IsActive);
    }
    [Fact] public async Task Maintenance_minimizes_expired_normal_timestamps_without_forgetting_retired_passwords()
    {
        await using var db = Db(); await Change(db, Initial, "maintenance-long-password!"); clock.Now = clock.Now.AddDays(30);
        await new MaintenanceStore(db, new PayAhead.Application.Operations.MaintenanceOptions()).CleanupAsync(clock.GetUtcNow().UtcDateTime, default);
        var history = await db.Set<PasswordHistoryEntry>().AsNoTracking().SingleAsync(); Assert.Null(history.NormalChangedAtUtc); Assert.True(passwords.VerifyStored(await db.Users.AsNoTracking().SingleAsync(), history.PasswordHash, Initial));
        Assert.Equal("password_reused", (await Assert.ThrowsAsync<PasswordSecurityException>(() => Change(db, "maintenance-long-password!", Initial))).Code);
    }
    private sealed class Clock : TimeProvider { public DateTimeOffset Now = new(2026, 10, 29, 12, 0, 0, TimeSpan.Zero); public override DateTimeOffset GetUtcNow() => Now; }
}
