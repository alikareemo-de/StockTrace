using StockTrace.Api.Auth;
using StockTrace.Application.Reports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace StockTrace.Api.Controllers;

[ApiController]
[Route("api/reports")]
public sealed class ReportsController(IInventoryReportService service) : ControllerBase
{
    [HttpGet("inventory-movements")]
    [Authorize(Policy = AppPermissions.ReportsRead)]
    [ProducesResponseType<PagedResult<InventoryMovementReportItem>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<InventoryMovementReportItem>>> GetInventoryMovements(
        [FromQuery] InventoryMovementReportQuery query,
        CancellationToken cancellationToken) =>
        Ok(await service.GetMovementsAsync(query, cancellationToken));
}
