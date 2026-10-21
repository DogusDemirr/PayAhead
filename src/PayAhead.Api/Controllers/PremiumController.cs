using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PayAhead.Application;
using PayAhead.Application.Premium;
using PayAhead.Application.Payments;
using PayAhead.Infrastructure;

namespace PayAhead.Api.Controllers;

[ApiController, Authorize, Route("api/premium"), ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class PremiumController(ICurrentUser current, IEntitlementService entitlements, IPremiumService premium, MembershipOptions limits, StoreOptions stores) : ControllerBase
{
    private Guid UserId => current.UserId ?? throw new AuthenticationRequiredException();
    [AllowAnonymous, HttpGet("plans")]
    public IActionResult Plans() => Ok(new
    {
        free = limits.Free,
        premium = limits.Premium,
        appleProducts = stores.AppleProducts,
        googleProducts = stores.GoogleProducts,
        appleVerificationEnabled = stores.AppleEnabled,
        googleVerificationEnabled = stores.GoogleEnabled
    });
    [HttpGet("me")]
    [HttpGet("entitlements")]
    public async Task<ActionResult<EntitlementResponse>> Me(CancellationToken ct) => Ok(await entitlements.GetAsync(UserId, ct));
    [HttpPost("verify-purchase"), EnableRateLimiting("purchase"), RequestSizeLimit(150_000)]
    public async Task<ActionResult<EntitlementResponse>> Verify(PurchaseRequest request, CancellationToken ct) => Ok(await premium.VerifyAsync(UserId, request, ct));
    [HttpPost("restore"), EnableRateLimiting("purchase"), RequestSizeLimit(150_000)]
    public async Task<ActionResult<EntitlementResponse>> Restore(PurchaseRequest request, CancellationToken ct) => Ok(await premium.VerifyAsync(UserId, request, ct));
}
[ApiController, AllowAnonymous, Route("api/webhooks"), RequestSizeLimit(262_144)]
public sealed class StoreWebhooksController(IPremiumService premium) : ControllerBase
{
    [HttpPost("apple/subscriptions")]
    public Task<IActionResult> Apple(CancellationToken ct) => Receive("Apple", ct);
    [HttpPost("google/subscriptions")]
    public Task<IActionResult> Google(CancellationToken ct) => Receive("Google", ct);
    private async Task<IActionResult> Receive(string store, CancellationToken ct)
    {
        using var reader = new StreamReader(Request.Body); var body = await reader.ReadToEndAsync(ct);
        if (body.Length > 262_144) throw new StoreVerificationException();
        await premium.ReceiveAsync(store, body, Request.Headers.Authorization.ToString(), ct); return Accepted();
    }
}
