using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PayAhead.Application.Notifications;

namespace PayAhead.Api.Controllers;

[ApiController, Authorize, Route("api/payments/{paymentId:guid}/reminders")]
public sealed class RemindersController(INotificationManagementService service) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<ReminderResponse>> Create(Guid paymentId, ReminderRequest request, CancellationToken ct)
        => StatusCode(201, await service.WriteRuleAsync(paymentId, null, request, ct));
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ReminderResponse>>> List(Guid paymentId, CancellationToken ct)
        => Ok(await service.ListRulesAsync(paymentId, ct));
    [HttpPut("{reminderId:guid}")]
    public async Task<ActionResult<ReminderResponse>> Update(Guid paymentId, Guid reminderId, ReminderRequest request, CancellationToken ct)
        => Ok(await service.WriteRuleAsync(paymentId, reminderId, request, ct));
    [HttpDelete("{reminderId:guid}")]
    public async Task<IActionResult> Delete(Guid paymentId, Guid reminderId, CancellationToken ct)
    { await service.DeleteRuleAsync(paymentId, reminderId, ct); return NoContent(); }
}
[ApiController, Authorize, Route("api/devices")]
public sealed class DevicesController(INotificationManagementService service) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<DeviceResponse>> Register(DeviceRequest request, CancellationToken ct) => Ok(await service.WriteDeviceAsync(request, ct));
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<DeviceResponse>>> List(CancellationToken ct) => Ok(await service.ListDevicesAsync(ct));
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct) { await service.DeleteDeviceAsync(id, ct); return NoContent(); }
}
[ApiController, Authorize, Route("api/users/me/settings")]
public sealed class UserSettingsController(INotificationManagementService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<SettingsResponse>> Get(CancellationToken ct) => Ok(await service.GetSettingsAsync(ct));
    [HttpPut]
    public async Task<ActionResult<SettingsResponse>> Update(SettingsRequest request, CancellationToken ct) => Ok(await service.WriteSettingsAsync(request, ct));
}
