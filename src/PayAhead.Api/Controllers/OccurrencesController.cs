using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PayAhead.Application.Occurrences;
using PayAhead.Application.Payments;

namespace PayAhead.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/payments/{paymentId}/occurrences")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
[ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
public sealed class OccurrencesController(IOccurrenceService occurrences) : ControllerBase
{
    [HttpPut("{dueDate}/status")]
    [ProducesResponseType(typeof(OccurrenceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<OccurrenceResponse>> SetStatus(Guid paymentId, DateOnly dueDate,
        [FromBody] SetOccurrenceStatusRequest request, CancellationToken cancellationToken)
        => Ok(await occurrences.SetStatusAsync(paymentId, dueDate, request, cancellationToken));

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<OccurrenceResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<OccurrenceResponse>>> List(Guid paymentId,
        [FromQuery] OccurrenceListQuery query, CancellationToken cancellationToken)
        => Ok(await occurrences.ListAsync(paymentId, query, cancellationToken));

    [HttpGet("{dueDate}")]
    [ProducesResponseType(typeof(OccurrenceResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<OccurrenceResponse>> Get(Guid paymentId, DateOnly dueDate, CancellationToken cancellationToken)
        => Ok(await occurrences.GetAsync(paymentId, dueDate, cancellationToken));
}
