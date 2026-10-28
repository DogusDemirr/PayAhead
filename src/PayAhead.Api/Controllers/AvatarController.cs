using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PayAhead.Application.Auth;

namespace PayAhead.Api.Controllers;
[ApiController, Authorize, Route("api/users/me/avatar"), ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AvatarController(AvatarService service) : ControllerBase
{
    private string? Version() { var v = Request.Headers.IfMatch.ToString(); return v.Length == 14 && v[0] == '"' && v[^1] == '"' ? v[1..^1] : null; }
    [HttpGet] public async Task<IActionResult> Get(CancellationToken ct) => File(await service.ReadAsync(ct), "image/jpeg");
    [HttpPut, RequestSizeLimit(AvatarService.MaximumBytes + 65536)]
    public async Task<IActionResult> Put(IFormFile image, CancellationToken ct)
    {
        if (Request.Form.Files.Count != 1 || Request.Form.Keys.Count != 0) throw new AvatarException("avatar_invalid");
        await using var input = image.OpenReadStream(); return Ok(await service.UploadAsync(input, image.Length, Version(), ct));
    }
    [HttpDelete] public async Task<IActionResult> Delete(CancellationToken ct) => Ok(await service.DeleteAsync(Version(), ct));
}
