using StockTrace.Api.Auth;
using StockTrace.Api.Contracts.Transfers;
using StockTrace.Application.Transfers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace StockTrace.Api.Controllers;

[ApiController]
[Route("api/stock-transfers")]
public sealed class StockTransfersController(IStockTransferService service) : ControllerBase
{
    [HttpPost]
    [Authorize(Policy = AppPermissions.StockTransfersCreate)]
    [ProducesResponseType<StockTransferResult>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<StockTransferResult>> Create(
        CreateStockTransferRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateStockTransferCommand(
            request.TransferNumber,
            request.SourceWarehouseId,
            request.DestinationWarehouseId,
            request.TransferredAt,
            request.Lines.Select(x => new CreateStockTransferLine(x.ProductId, x.Quantity)).ToArray());
        var result = await service.CreateAsync(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = AppPermissions.StockTransfersRead)]
    [ProducesResponseType<StockTransferResult>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StockTransferResult>> GetById(Guid id, CancellationToken cancellationToken) =>
        Ok(await service.GetByIdAsync(id, cancellationToken));
}
