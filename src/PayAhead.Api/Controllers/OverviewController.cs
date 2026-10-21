using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PayAhead.Application.Overview;

namespace PayAhead.Api.Controllers;

[ApiController]
[Authorize]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
public sealed class OverviewController(IOverviewService overview) : ControllerBase
{
    [HttpGet("api/payments/upcoming")]
    [ProducesResponseType(typeof(UpcomingPaymentsResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<UpcomingPaymentsResponse>> Upcoming([FromQuery] UpcomingPaymentsQuery query, CancellationToken cancellationToken)
        => Ok(await overview.GetUpcomingAsync(query, cancellationToken));

    [HttpGet("api/dashboard/summary")]
    [ProducesResponseType(typeof(DashboardSummaryResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<DashboardSummaryResponse>> Summary([FromQuery] OverviewRangeQuery query, CancellationToken cancellationToken)
        => Ok(await overview.GetSummaryAsync(query, cancellationToken));

    [HttpGet("api/calendar")]
    [ProducesResponseType(typeof(CalendarResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<CalendarResponse>> Calendar([FromQuery] CalendarQuery query, CancellationToken cancellationToken)
        => Ok(await overview.GetCalendarAsync(query, cancellationToken));
}
