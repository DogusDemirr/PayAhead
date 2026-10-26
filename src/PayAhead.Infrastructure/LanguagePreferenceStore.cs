using Microsoft.EntityFrameworkCore;
using PayAhead.Application.Localization;
using PayAhead.Application.Payments;
using PayAhead.Domain;
namespace PayAhead.Infrastructure;

public sealed class LanguagePreferenceStore(PayAheadDbContext db) : ILanguagePreferenceStore
{
    public async Task<LanguagePreferenceResponse> ReadAsync(Guid user, CancellationToken ct)
        => new(SupportedLanguages.Resolve(await db.UserSettings.AsNoTracking().Where(x => x.UserId == user).Select(x => x.LanguageCode).SingleOrDefaultAsync(ct)));
    public Task<LanguagePreferenceResponse> WriteAsync(Guid user, string language, DateTime now, CancellationToken ct)
        => db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            db.ChangeTracker.Clear(); await using var transaction = await db.Database.BeginTransactionAsync(ct);
            if (await db.Users.FromSqlInterpolated($"SELECT * FROM Users WITH (UPDLOCK,HOLDLOCK) WHERE Id={user} AND IsActive=1").SingleOrDefaultAsync(ct) is null) throw new AuthenticationRequiredException();
            var settings = await db.UserSettings.SingleOrDefaultAsync(x => x.UserId == user, ct);
            if (settings is null) { settings = new UserSettings { UserId = user, CreatedAtUtc = now }; db.UserSettings.Add(settings); }
            settings.LanguageCode = language; settings.UpdatedAtUtc = now;
            await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct); return new LanguagePreferenceResponse(language);
        });
}
