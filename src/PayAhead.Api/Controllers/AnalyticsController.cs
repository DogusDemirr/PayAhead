using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PayAhead.Application.Overview;

namespace PayAhead.Api.Controllers;

[ApiController, Authorize, Route("api/analytics")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AnalyticsController(IAnalyticsService analytics) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<AnalyticsResponse>> Get([FromQuery] CalendarQuery query, CancellationToken ct)
        => Ok(await analytics.GetAnalyticsAsync(query, ct));
}
