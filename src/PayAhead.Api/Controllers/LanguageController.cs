using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PayAhead.Application.Localization;
namespace PayAhead.Api.Controllers;

[ApiController, Authorize, Route("api/users/me/language")]
public sealed class LanguageController(LanguagePreferenceService service) : ControllerBase
{
    [HttpGet] public async Task<IActionResult> Get(CancellationToken ct) => Ok(await service.ReadAsync(ct));
    [HttpPut] public async Task<IActionResult> Put(LanguagePreferenceRequest request, CancellationToken ct) => Ok(await service.WriteAsync(request, ct));
}
