using StockTrace.Api.Auth;
using StockTrace.Api.Contracts.MasterData;
using StockTrace.Application.MasterData;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace StockTrace.Api.Controllers;

[ApiController]
[Route("api/master-data")]
public sealed class MasterDataController(IMasterDataService service) : ControllerBase
{
    [HttpGet("categories")]
    [Authorize(Policy = AppPermissions.MasterDataRead)]
    [ProducesResponseType<IReadOnlyCollection<CategoryResult>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<CategoryResult>>> GetCategories(
        CancellationToken cancellationToken) =>
        Ok(await service.GetCategoriesAsync(cancellationToken));

    [HttpGet("suppliers")]
    [Authorize(Policy = AppPermissions.MasterDataRead)]
    [ProducesResponseType<IReadOnlyCollection<SupplierResult>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<SupplierResult>>> GetSuppliers(
        CancellationToken cancellationToken) =>
        Ok(await service.GetSuppliersAsync(cancellationToken));

    [HttpGet("warehouses")]
    [Authorize(Policy = AppPermissions.MasterDataRead)]
    [ProducesResponseType<IReadOnlyCollection<WarehouseResult>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<WarehouseResult>>> GetWarehouses(
        CancellationToken cancellationToken) =>
        Ok(await service.GetWarehousesAsync(cancellationToken));

    [HttpGet("products")]
    [Authorize(Policy = AppPermissions.MasterDataRead)]
    [ProducesResponseType<IReadOnlyCollection<ProductResult>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<ProductResult>>> GetProducts(
        CancellationToken cancellationToken) =>
        Ok(await service.GetProductsAsync(cancellationToken));

    [HttpPut("warehouses/{warehouseId:guid}/products/{productId:guid}/low-stock-threshold")]
    [Authorize(Policy = AppPermissions.MasterDataManageThresholds)]
    [ProducesResponseType<WarehouseProductThresholdResult>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WarehouseProductThresholdResult>> SetLowStockThreshold(
        Guid warehouseId,
        Guid productId,
        SetLowStockThresholdRequest request,
        CancellationToken cancellationToken) =>
        Ok(await service.SetLowStockThresholdAsync(
            warehouseId, productId, request.LowStockThreshold, cancellationToken));
}
