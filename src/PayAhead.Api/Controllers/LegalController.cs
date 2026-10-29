using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PayAhead.Application.Auth;

namespace PayAhead.Api.Controllers;
[ApiController, AllowAnonymous, Route("api/legal")]
public sealed class LegalController : ControllerBase
{
    [HttpGet] public IActionResult Get() => Ok(LegalDocuments.Manifest);
}
