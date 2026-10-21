using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PayAhead.Application.Payments;
using PayAhead.Application.Subscriptions;

namespace PayAhead.Api.Controllers;

[ApiController,Authorize,Route("api/subscriptions")]
public sealed class SubscriptionsController(ISubscriptionService service) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<SubscriptionResponse>> Create(SubscriptionRequest request,CancellationToken ct)
    { var value=await service.CreateAsync(request,ct);Version(value);return CreatedAtAction(nameof(Get),new{id=value.Id},value); }
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<SubscriptionResponse>> Get(Guid id,CancellationToken ct)
    { var value=await service.GetAsync(id,ct);Version(value);return Ok(value); }
    [HttpGet]
    public async Task<ActionResult<PagedResult<SubscriptionResponse>>> List([FromQuery]SubscriptionQuery query,CancellationToken ct)=>Ok(await service.ListAsync(query,ct));
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<SubscriptionResponse>> Update(Guid id,SubscriptionRequest request,CancellationToken ct)
    { var value=await service.UpdateAsync(id,request,ct);Version(value);return Ok(value); }
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id,CancellationToken ct)
    {
        var header=Request.Headers.IfMatch.ToString();
        var token=header.Length>=2&&header[0]=='"'&&header[^1]=='"'?header[1..^1]:null;
        await service.DeleteAsync(id,token,ct);return NoContent();
    }
    [HttpPost("{id:guid}/cancel")]
    public async Task<ActionResult<SubscriptionResponse>> Cancel(Guid id,CancelSubscriptionRequest request,CancellationToken ct)
    { var value=await service.CancelAsync(id,request,ct);Version(value);return Ok(value); }
    [HttpGet("trials/expiring")]
    public async Task<ActionResult<IReadOnlyList<ExpiringTrialResponse>>> Trials([FromQuery]ExpiringTrialsQuery query,CancellationToken ct)=>Ok(await service.TrialsAsync(query,ct));
    [HttpGet("summary")]
    public async Task<ActionResult<SubscriptionSummary>> Summary(CancellationToken ct)=>Ok(await service.SummaryAsync(ct));
    private void Version(SubscriptionResponse value)=>Response.Headers.ETag='"'+value.RowVersion+'"';
}
