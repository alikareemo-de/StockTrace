using System.Net;
using System.Net.Http.Json;
using StockTrace.Api.Contracts.Purchases;
using StockTrace.Api.Contracts.Transfers;
using StockTrace.Application.Purchases;
using StockTrace.Application.Transfers;
using StockTrace.Domain.Catalog;
using StockTrace.Domain.Inventory;
using StockTrace.IntegrationTests.Authentication;
using StockTrace.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace StockTrace.IntegrationTests.Transfers;

public sealed class StockTransferTests(StockTraceWebApplicationFactory factory)
    : IClassFixture<StockTraceWebApplicationFactory>
{
    [Fact]
    public async Task TransferMovesLotAtomicallyAndInsufficientTransferRollsBack()
    {
        var client = factory.CreateClient();
        await client.LoginAsAdminAsync();
        var data = await CreateTestDataAsync();
        var suffix = Guid.NewGuid().ToString("N");

        var receiptResponse = await client.PostAsJsonAsync("/api/purchase-receipts", new ReceivePurchaseRequest(
            $"TR-PR-{suffix}", data.SupplierId, data.SourceWarehouseId, DateTimeOffset.UtcNow,
            [new ReceivePurchaseLineRequest(data.ProductId, 10m, 5m)]));
        Assert.Equal(HttpStatusCode.Created, receiptResponse.StatusCode);
        var receipt = await receiptResponse.Content.ReadFromJsonAsync<PurchaseReceiptResult>();
        Assert.NotNull(receipt);
        var lotId = Assert.Single(receipt.Lines).InventoryLotId;

        var successfulResponse = await client.PostAsJsonAsync("/api/stock-transfers", new CreateStockTransferRequest(
            $"TR-OK-{suffix}", data.SourceWarehouseId, data.DestinationWarehouseId, DateTimeOffset.UtcNow,
            [new CreateStockTransferLineRequest(data.ProductId, 7m)]));
        Assert.Equal(HttpStatusCode.Created, successfulResponse.StatusCode);
        var transfer = await successfulResponse.Content.ReadFromJsonAsync<StockTransferResult>();
        Assert.NotNull(transfer);
        Assert.Equal("Completed", transfer.Status);
        Assert.Equal(lotId, Assert.Single(Assert.Single(transfer.Lines).Allocations).InventoryLotId);

        await AssertBalancesAndMovementsAsync(
            lotId, data.SourceWarehouseId, data.DestinationWarehouseId, transfer.Id, 3m, 7m);

        var failedNumber = $"TR-FAIL-{suffix}";
        var failedResponse = await client.PostAsJsonAsync("/api/stock-transfers", new CreateStockTransferRequest(
            failedNumber, data.SourceWarehouseId, data.DestinationWarehouseId, DateTimeOffset.UtcNow,
            [new CreateStockTransferLineRequest(data.ProductId, 4m)]));
        Assert.Equal(HttpStatusCode.Conflict, failedResponse.StatusCode);

        await AssertBalancesAndMovementsAsync(
            lotId, data.SourceWarehouseId, data.DestinationWarehouseId, transfer.Id, 3m, 7m);
        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.False(await dbContext.StockTransfers.AnyAsync(x => x.TransferNumber == failedNumber));
    }

    private async Task AssertBalancesAndMovementsAsync(
        Guid lotId,
        Guid sourceWarehouseId,
        Guid destinationWarehouseId,
        Guid transferId,
        decimal expectedSource,
        decimal expectedDestination)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var balances = await dbContext.InventoryBalances.AsNoTracking()
            .Where(x => x.InventoryLotId == lotId)
            .ToDictionaryAsync(x => x.WarehouseId, x => x.QuantityOnHand);
        Assert.Equal(expectedSource, balances[sourceWarehouseId]);
        Assert.Equal(expectedDestination, balances[destinationWarehouseId]);

        var movements = await dbContext.InventoryMovements.AsNoTracking()
            .Where(x => x.ReferenceId == transferId)
            .ToArrayAsync();
        Assert.Equal(2, movements.Length);
        Assert.Contains(movements, x => x.MovementType == InventoryMovementType.TransferOut && x.Quantity == -7m);
        Assert.Contains(movements, x => x.MovementType == InventoryMovementType.TransferIn && x.Quantity == 7m);
    }

    private async Task<(Guid ProductId, Guid SupplierId, Guid SourceWarehouseId, Guid DestinationWarehouseId)>
        CreateTestDataAsync()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var categoryId = await dbContext.Categories.Where(x => !x.IsDeleted).Select(x => x.Id).FirstAsync();
        var product = new Product($"TR-{Guid.NewGuid():N}", "Transfer Test Product", categoryId, "Piece");
        dbContext.Products.Add(product);
        await dbContext.SaveChangesAsync();
        var warehouseIds = await dbContext.Warehouses.Where(x => !x.IsDeleted)
            .OrderBy(x => x.Code).Select(x => x.Id).Take(2).ToArrayAsync();

        return (
            product.Id,
            await dbContext.Suppliers.Where(x => !x.IsDeleted).Select(x => x.Id).FirstAsync(),
            warehouseIds[0],
            warehouseIds[1]);
    }
}
