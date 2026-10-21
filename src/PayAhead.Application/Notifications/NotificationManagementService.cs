using FluentValidation;
using PayAhead.Application.Payments;

namespace PayAhead.Application.Notifications;

public sealed class ReminderValidator : AbstractValidator<ReminderRequest>
{
    public ReminderValidator()
    {
        RuleFor(x => x.DaysBefore).NotNull().InclusiveBetween(0, NotificationPolicy.MaximumDaysBefore);
        RuleFor(x => x.TimeOfDay).Must(x => NotificationPolicy.TryTime(x, out _)).WithMessage("Saat HH:mm biçiminde olmalıdır.");
    }
}
public sealed class DeviceValidator : AbstractValidator<DeviceRequest>
{
    public DeviceValidator()
    {
        RuleFor(x => x.Platform).Must(x => x is "ios" or "android");
        RuleFor(x => x.PushToken).Must(NotificationPolicy.ValidToken).WithMessage("Geçerli Expo push token gereklidir.");
        RuleFor(x => x.Id).Must(x => x is null || x != Guid.Empty);
    }
}
public sealed class SettingsValidator : AbstractValidator<SettingsRequest>
{
    public SettingsValidator()
    {
        RuleFor(x => x.LanguageCode).Must(x => x is "tr" or "en");
        RuleFor(x => x.DefaultCurrencyCode).Must(CurrencyCodes.Contains);
        RuleFor(x => x.TimeZoneId).NotEmpty().MaximumLength(100).Must(x => { NotificationPolicy.Zone(x, out var fallback); return !fallback; });
        RuleFor(x => x.NotificationsEnabled).NotNull();
    }
}
public sealed class NotificationManagementService(INotificationManagementStore store, ICurrentUser currentUser, TimeProvider clock,
    IValidator<ReminderRequest> rules, IValidator<DeviceRequest> devices, IValidator<SettingsRequest> settings) : INotificationManagementService
{
    private Guid User => currentUser.UserId ?? throw new AuthenticationRequiredException();
    public async Task<IReadOnlyList<ReminderResponse>> ListRulesAsync(Guid plan, CancellationToken ct)
        => (await store.ListRulesAsync(User, plan, ct)).Select(ReminderResponse.From).ToArray();
    public async Task<ReminderResponse> WriteRuleAsync(Guid plan, Guid? id, ReminderRequest request, CancellationToken ct)
    { var user = User; await rules.ValidateAndThrowAsync(request, ct); return ReminderResponse.From(await store.WriteRuleAsync(user, plan, id, request, ct)); }
    public Task DeleteRuleAsync(Guid plan, Guid id, CancellationToken ct) => store.DeleteRuleAsync(User, plan, id, ct);
    public Task<IReadOnlyList<DeviceResponse>> ListDevicesAsync(CancellationToken ct) => store.ListDevicesAsync(User, ct);
    public async Task<DeviceResponse> WriteDeviceAsync(DeviceRequest request, CancellationToken ct)
    { var user = User; await devices.ValidateAndThrowAsync(request, ct); return await store.WriteDeviceAsync(user, request, clock.GetUtcNow().UtcDateTime, ct); }
    public Task DeleteDeviceAsync(Guid id, CancellationToken ct) => store.DeleteDeviceAsync(User, id, clock.GetUtcNow().UtcDateTime, ct);
    public Task<SettingsResponse> GetSettingsAsync(CancellationToken ct) => store.GetSettingsAsync(User, ct);
    public async Task<SettingsResponse> WriteSettingsAsync(SettingsRequest request, CancellationToken ct)
    { var user = User; await settings.ValidateAndThrowAsync(request, ct); return await store.WriteSettingsAsync(user, request, clock.GetUtcNow().UtcDateTime, ct); }
}
