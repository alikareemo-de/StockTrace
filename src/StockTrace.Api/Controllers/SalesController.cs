using StockTrace.Api.Auth;
using StockTrace.Api.Contracts.Sales;
using StockTrace.Application.Sales;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace StockTrace.Api.Controllers;

[ApiController]
[Route("api/sales")]
public sealed class SalesController(ISaleService service) : ControllerBase
{
    [HttpPost]
    [Authorize(Policy = AppPermissions.SalesCreate)]
    [ProducesResponseType<SaleResult>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SaleResult>> Create(CreateSaleRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateSaleCommand(request.SaleNumber, request.WarehouseId, request.SoldAt,
            request.Lines.Select(x => new CreateSaleLine(x.ProductId, x.Quantity, x.UnitPrice)).ToArray());
        var result = await service.CreateAsync(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = AppPermissions.SalesRead)]
    [ProducesResponseType<SaleResult>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SaleResult>> GetById(Guid id, CancellationToken cancellationToken) =>
        Ok(await service.GetByIdAsync(id, cancellationToken));
}
