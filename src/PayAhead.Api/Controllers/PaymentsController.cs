using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PayAhead.Application.Payments;

namespace PayAhead.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/payments")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
public sealed class PaymentsController(IPaymentService payments) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(PaymentResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<PaymentResponse>> Create([FromBody] CreatePaymentRequest request, CancellationToken cancellationToken)
    {
        var result = await payments.CreateAsync(request, cancellationToken);
        Response.Headers.ETag = $"\"{result.RowVersion}\"";
        return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<PaymentResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<PaymentResponse>>> List([FromQuery] PaymentListQuery query, CancellationToken cancellationToken)
        => Ok(await payments.ListAsync(query, cancellationToken));

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(PaymentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PaymentResponse>> Get(Guid id, CancellationToken cancellationToken)
    {
        var result = await payments.GetAsync(id, cancellationToken);
        Response.Headers.ETag = $"\"{result.RowVersion}\"";
        return Ok(result);
    }

    [HttpPut("{id}")]
    [ProducesResponseType(typeof(PaymentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PaymentResponse>> Update(Guid id, [FromBody] UpdatePaymentRequest request, CancellationToken cancellationToken)
    {
        var result = await payments.UpdateAsync(id, request, cancellationToken);
        Response.Headers.ETag = $"\"{result.RowVersion}\"";
        return Ok(result);
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(Guid id, [FromHeader(Name = "If-Match")] string? ifMatch, CancellationToken cancellationToken)
    {
        // Tek strong ETag kabul edilir. Wildcard, weak veya çoklu ETag geçersizdir.
        var token = ifMatch is { Length: 14 } && ifMatch[0] == '"' && ifMatch[^1] == '"' ? ifMatch[1..^1] : null;
        await payments.DeleteAsync(id, token, cancellationToken);
        return NoContent();
    }
}
