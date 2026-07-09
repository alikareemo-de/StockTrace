using StockTrace.Api.Auth;
using StockTrace.Application.Inventory;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace StockTrace.Api.Controllers;

[ApiController]
[Route("api/inventory")]
public sealed class InventoryController(IInventoryQueryService service) : ControllerBase
{
    [HttpGet("availability")]
    [Authorize(Policy = AppPermissions.InventoryRead)]
    [ProducesResponseType<InventoryAvailabilityResult>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<InventoryAvailabilityResult>> GetAvailability(
        [FromQuery] Guid warehouseId,
        [FromQuery] Guid productId,
        CancellationToken cancellationToken) =>
        Ok(await service.GetAvailabilityAsync(warehouseId, productId, cancellationToken));
}
