using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PayAhead.Application;
using PayAhead.Application.Operations;
using PayAhead.Application.Payments;

namespace PayAhead.Api.Controllers;

[ApiController, Authorize, Route("api/users/me"), ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AccountController(ICurrentUser current, IAccountStore store) : ControllerBase
{
    private Guid UserId => current.UserId ?? throw new AuthenticationRequiredException();
    [HttpGet("export")]
    public async Task<IActionResult> Export(CancellationToken ct)
        => File(await store.ExportAsync(UserId, ct), "application/json", "payahead-export.json");
    [HttpDelete, RequestSizeLimit(4096)]
    public async Task<IActionResult> Delete(DeleteAccountRequest request, CancellationToken ct)
    {
        if (request.CurrentPassword is null || request.CurrentPassword.Length is < 12 or > 128) throw new AuthenticationRequiredException();
        await store.DeleteAsync(UserId, request.CurrentPassword, ct);
        return NoContent();
    }
}
