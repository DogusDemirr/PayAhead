using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PayAhead.Application.Auth;

namespace PayAhead.Api.Controllers;

[ApiController, Authorize, Route("api/users/me"), ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class ProfileController(ProfileService service) : ControllerBase
{
    [HttpGet("profile")] public async Task<IActionResult> Get(CancellationToken ct) => Ok(await service.ReadAsync(ct));
    [HttpPut("profile"), RequestSizeLimit(4096)] public async Task<IActionResult> Update(UpdateProfileRequest request, CancellationToken ct) => Ok(await service.UpdateAsync(request, ct));
    [HttpPost("change-password"), RequestSizeLimit(4096)] public async Task<IActionResult> ChangePassword(ChangePasswordRequest request, CancellationToken ct) { await service.ChangePasswordAsync(request, ct); return NoContent(); }
}
