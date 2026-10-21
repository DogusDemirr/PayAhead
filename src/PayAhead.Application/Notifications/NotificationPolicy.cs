using System.Globalization;
using System.Text.RegularExpressions;
using PayAhead.Domain;
using PayAhead.Domain.Recurrence;

namespace PayAhead.Application.Notifications;

public static partial class NotificationPolicy
{
    public const int MaximumDaysBefore = 365;
    public const int MaximumRulesPerPlan = 10;
    public const int MaximumDevicesPerUser = 20;
    public static bool ValidToken(string? token) => token is not null && token.Length <= 512 && ExpoToken().IsMatch(token);
    [GeneratedRegex(@"^(ExponentPushToken|ExpoPushToken)\[[A-Za-z0-9_-]{10,200}\]$", RegexOptions.CultureInvariant)]
    private static partial Regex ExpoToken();
    public static bool TryTime(string? text, out TimeOnly time) => TimeOnly.TryParseExact(text, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out time);
    public static TimeZoneInfo Zone(string? id, out bool fallback)
    {
        try { if (!string.IsNullOrWhiteSpace(id)) { var zone = TimeZoneInfo.FindSystemTimeZoneById(id); fallback = false; return zone; } }
        catch (TimeZoneNotFoundException) { }
        catch (InvalidTimeZoneException) { }
        fallback = true; return TimeZoneInfo.Utc;
    }
    public static DateTime? ScheduledAt(DateOnly due, int daysBefore, TimeOnly time, TimeZoneInfo zone)
    {
        if (daysBefore is < 0 or > MaximumDaysBefore || due.DayNumber < daysBefore) return null;
        var local = due.AddDays(-daysBefore).ToDateTime(time, DateTimeKind.Unspecified);
        // Gap: first valid local minute. Fold: later UTC instant (standard-time side).
        for (var minute = 0; zone.IsInvalidTime(local) && minute < 1440; minute++)
        { if (local >= DateTime.MaxValue.AddMinutes(-1)) return null; local = local.AddMinutes(1); }
        if (zone.IsInvalidTime(local)) return null;
        if (zone.IsAmbiguousTime(local))
        {
            try { return new DateTimeOffset(local, zone.GetAmbiguousTimeOffsets(local).Min()).UtcDateTime; }
            catch (ArgumentException) { return null; }
        }
        try { return TimeZoneInfo.ConvertTimeToUtc(local, zone); }
        catch (ArgumentException) { return null; }
    }
    public static bool Eligible(PaymentPlan plan, ReminderRule rule, bool userActive, bool notificationsEnabled,
        bool deviceActive, DateOnly due, OccurrenceStatus? status, IPaymentRecurrenceCalculator recurrence)
        => userActive && notificationsEnabled && deviceActive && plan.IsActive && rule.IsEnabled && !rule.IsDeleted
            && status is null or OccurrenceStatus.Pending && recurrence.GetNextOccurrence(plan, due) == due;
    public static TimeSpan Backoff(int attempt) => TimeSpan.FromSeconds(Math.Min(3600, 30 * Math.Pow(2, Math.Clamp(attempt - 1, 0, 10))));
}
