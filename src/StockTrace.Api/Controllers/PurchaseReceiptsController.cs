using StockTrace.Api.Auth;
using StockTrace.Api.Contracts.Purchases;
using StockTrace.Application.Purchases;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace StockTrace.Api.Controllers;

[ApiController]
[Route("api/purchase-receipts")]
public sealed class PurchaseReceiptsController(IPurchaseReceiptService service) : ControllerBase
{
    [HttpPost]
    [Authorize(Policy = AppPermissions.PurchaseReceiptsCreate)]
    [ProducesResponseType<PurchaseReceiptResult>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PurchaseReceiptResult>> Receive(
        ReceivePurchaseRequest request,
        CancellationToken cancellationToken)
    {
        var command = new ReceivePurchaseCommand(
            request.ReceiptNumber,
            request.SupplierId,
            request.WarehouseId,
            request.ReceivedAt,
            request.Lines.Select(x => new ReceivePurchaseLine(x.ProductId, x.Quantity, x.UnitCost)).ToArray());

        var result = await service.ReceiveAsync(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = AppPermissions.PurchaseReceiptsRead)]
    [ProducesResponseType<PurchaseReceiptResult>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PurchaseReceiptResult>> GetById(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await service.GetByIdAsync(id, cancellationToken));
    }
}
