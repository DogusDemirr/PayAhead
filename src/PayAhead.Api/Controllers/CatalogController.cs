using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PayAhead.Application.Catalog;
namespace PayAhead.Api.Controllers;

[ApiController, Authorize, Route("api/catalog")]
public sealed class CatalogController(CatalogReader reader) : ControllerBase
{
    [HttpGet("categories")] public async Task<IActionResult> Categories([FromQuery] CatalogQuery query, CancellationToken ct) => Ok(await reader.CategoriesAsync(query, ct));
    [HttpGet("categories/{id:guid}")] public async Task<IActionResult> Category(Guid id, CancellationToken ct, [FromQuery] string? locale = null) => Ok(await reader.CategoryAsync(id, ct, locale));
    [HttpGet("services")] public async Task<IActionResult> Services([FromQuery] CatalogQuery query, CancellationToken ct) => Ok(await reader.ServicesAsync(query, ct));
    [HttpGet("services/{id:guid}")] public async Task<IActionResult> Service(Guid id, CancellationToken ct, [FromQuery] string? locale = null) => Ok(await reader.ServiceAsync(id, ct, locale));
}
