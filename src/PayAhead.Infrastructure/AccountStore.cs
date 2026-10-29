using System.Data;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PayAhead.Application.Auth;
using PayAhead.Application.Operations;
using PayAhead.Application.Payments;
using PayAhead.Domain;

namespace PayAhead.Infrastructure;

public sealed class AccountStore(PayAheadDbContext db, IPasswordService passwords, TimeProvider clock, IAvatarStorage? avatars = null) : IAccountStore
{
    public Task<byte[]> ExportAsync(Guid user, CancellationToken ct) => db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        return await ExportBody(user, tx, ct);
    });
    private async Task<byte[]> ExportBody(Guid user, Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction tx, CancellationToken ct)
    {
        var profile = await db.Users.AsNoTracking().Where(x => x.Id == user && x.IsActive).Select(x => new { x.Id, x.Email, x.DisplayName, x.CreatedAtUtc }).SingleOrDefaultAsync(ct) ?? throw new AuthenticationRequiredException();
        var plans = await db.PaymentPlans.AsNoTracking().Where(x => x.UserId == user).OrderBy(x => x.Id).Take(1001).Select(x => new { x.Id, x.Name, x.Description, x.Amount, x.CurrencyCode, x.Category, x.Frequency, x.Interval, x.StartDate, x.EndDate, x.AnchorDay, x.IsActive }).ToArrayAsync(ct);
        var occurrences = await db.PaymentOccurrences.AsNoTracking().Where(x => db.PaymentPlans.Any(p => p.Id == x.PaymentPlanId && p.UserId == user)).OrderBy(x => x.Id).Take(20001).Select(x => new { x.Id, x.PaymentPlanId, x.DueDate, x.Status, x.PaidAtUtc, x.ActualAmount }).ToArrayAsync(ct);
        var reminders = await db.ReminderRules.AsNoTracking().Where(x => x.UserId == user).OrderBy(x => x.Id).Take(10001).Select(x => new { x.Id, x.PaymentPlanId, x.Target, x.DaysBefore, x.TimeOfDay, x.IsEnabled, x.IsDeleted }).ToArrayAsync(ct);
        var tracked = await db.TrackedSubscriptions.AsNoTracking().Where(x => x.UserId == user).OrderBy(x => x.Id).Take(1001).Select(x => new { x.Id, x.PaymentPlanId, x.ProviderName, x.Status, x.StartDate, x.TrialStartDate, x.TrialEndDate, x.AutoRenew, x.AccessEndsOn, x.IsDeleted }).ToArrayAsync(ct);
        if (plans.Length > 1000 || occurrences.Length > 20000 || reminders.Length > 10000 || tracked.Length > 1000) throw new ExportLimitException();
        var settings = await db.UserSettings.AsNoTracking().Where(x => x.UserId == user).Select(x => new { x.LanguageCode, x.DefaultCurrencyCode, x.TimeZoneId, x.NotificationsEnabled }).SingleOrDefaultAsync(ct);
        var memberships = await db.UserSubscriptions.AsNoTracking().Where(x => x.UserId == user).Select(x => new { x.Provider, x.ProductId, x.Status, x.AutoRenew, x.CurrentPeriodEndUtc }).Take(101).ToArrayAsync(ct);
        var devices = await db.DeviceTokens.AsNoTracking().Where(x => x.UserId == user).Select(x => new { x.Id, x.Platform, x.IsActive, x.LastSeenAtUtc }).Take(1001).ToArrayAsync(ct);
        var legalNotice = await db.Set<LegalNoticeReceipt>().AsNoTracking().Where(x => x.UserId == user).Select(x => new { x.NoticeVersion, x.PresentedAtUtc, x.RecordedAtUtc }).SingleOrDefaultAsync(ct);
        var registrationLegal = await db.Set<RegistrationLegalConfirmation>().AsNoTracking().Where(x => x.UserId == user).OrderBy(x => x.DocumentId).Select(x => new { x.DocumentId, x.Kind, x.DocumentVersion, x.ConfirmedAtUtc }).ToArrayAsync(ct);
        if (memberships.Length > 100 || devices.Length > 1000) throw new ExportLimitException();
        var bytes = JsonSerializer.SerializeToUtf8Bytes(new { schemaVersion = 1, exportedAtUtc = clock.GetUtcNow(), profile, settings, plans, occurrences, reminders, trackedSubscriptions = tracked, memberships, devices, legalNotice, registrationLegal }, new JsonSerializerOptions(JsonSerializerDefaults.Web) { Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter(), new ExportUtcConverter() } });
        if (bytes.Length > 8 * 1024 * 1024) throw new ExportLimitException();
        await tx.CommitAsync(ct);
        return bytes;
    }
    private sealed class ExportUtcConverter : System.Text.Json.Serialization.JsonConverter<DateTime>
    {
        public override DateTime Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options) => throw new NotSupportedException();
        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options) => writer.WriteStringValue(DateTime.SpecifyKind(value, DateTimeKind.Utc));
    }
    public Task DeleteAsync(Guid user, string password, CancellationToken ct) => db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
    {
        db.ChangeTracker.Clear();
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var row = await db.Users.FromSqlInterpolated($"SELECT * FROM Users WITH(UPDLOCK,HOLDLOCK) WHERE Id={user}").SingleOrDefaultAsync(ct) ?? throw new AuthenticationRequiredException();
        if (!row.IsActive) { await tx.CommitAsync(ct); return; }
        if (!passwords.Verify(row, password, out _)) throw new AuthenticationRequiredException();
        var oldAvatar = row.AvatarKey; row.AvatarKey = null;
        var now = clock.GetUtcNow().UtcDateTime;
        // Retain only an inactive pseudonymous owner and anti-replay store bindings. No account transfer.
        row.IsActive = false; row.Email = $"deleted-{user:N}@deleted.invalid"; row.NormalizedEmail = row.Email.ToUpperInvariant(); row.DisplayName = "Deleted"; row.PasswordHash = ""; row.FailedLoginCount = 0; row.LoginLockedUntilUtc = null; row.UpdatedAtUtc = now;
        await db.SaveChangesAsync(ct);
        await db.RefreshTokens.Where(x => x.UserId == user).ExecuteUpdateAsync(s => s.SetProperty(x => x.RevokedAtUtc, now).SetProperty(x => x.ReplacedByTokenId, (Guid?)null), ct);
        await db.RefreshTokens.Where(x => x.UserId == user).ExecuteDeleteAsync(ct);
        await db.Set<PasswordResetToken>().Where(x => x.UserId == user).ExecuteDeleteAsync(ct);
        await db.Set<PasswordHistoryEntry>().Where(x => x.UserId == user).ExecuteDeleteAsync(ct);
        await db.Set<LegalNoticeReceipt>().Where(x => x.UserId == user).ExecuteDeleteAsync(ct);
        await db.Set<RegistrationLegalConfirmation>().Where(x => x.UserId == user).ExecuteDeleteAsync(ct);
        // Lock plans in a stable order to fence the notification scheduler before deleting dependent rows.
        _ = await db.PaymentPlans.FromSqlInterpolated($"SELECT * FROM PaymentPlans WITH(UPDLOCK,HOLDLOCK) WHERE UserId={user} ORDER BY Id").AsNoTracking().ToArrayAsync(ct);
        await db.NotificationDeliveries.Where(x => x.UserId == user).ExecuteDeleteAsync(ct);
        await db.ReminderRules.Where(x => x.UserId == user).ExecuteDeleteAsync(ct);
        await db.TrackedSubscriptions.Where(x => x.UserId == user).ExecuteDeleteAsync(ct);
        await db.PaymentOccurrences.Where(x => db.PaymentPlans.Any(p => p.Id == x.PaymentPlanId && p.UserId == user)).ExecuteDeleteAsync(ct);
        await db.PaymentPlans.Where(x => x.UserId == user).ExecuteDeleteAsync(ct);
        await db.DeviceTokens.Where(x => x.UserId == user).ExecuteDeleteAsync(ct);
        await db.UserSettings.Where(x => x.UserId == user).ExecuteDeleteAsync(ct);
        await db.AuditLogs.Where(x => x.UserId == user).ExecuteDeleteAsync(ct);
        var originals = await db.UserSubscriptions.Where(x => x.UserId == user && x.Provider == "Apple").Select(x => x.ExternalSubscriptionId).ToArrayAsync(ct);
        var appleHashes = originals.Select(PurchaseProtector.Hash).ToArray();
        await db.StoreWebhookInbox.Where(x => appleHashes.Contains(x.ReferenceHash) || db.StorePurchases.Any(p => p.UserId == user && p.TokenHash == EF.Functions.Collate(x.ReferenceHash, "Latin1_General_100_BIN2"))).ExecuteDeleteAsync(ct);
        await db.UserSubscriptions.Where(x => x.UserId == user).ExecuteUpdateAsync(s => s.SetProperty(x => x.IsStoreVerified, false).SetProperty(x => x.ProtectedEvidence, (string?)null).SetProperty(x => x.NextVerificationAtUtc, (DateTime?)null).SetProperty(x => x.UpdatedAtUtc, now), ct);
        await tx.CommitAsync(ct);
        if (oldAvatar is not null && avatars is not null) await avatars.DeleteAsync(oldAvatar, CancellationToken.None);
    });
}
