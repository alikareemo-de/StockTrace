using System.Net;
using System.Net.Http.Json;
using StockTrace.Api.Contracts.Purchases;
using StockTrace.Api.Contracts.Sales;
using StockTrace.Application.Purchases;
using StockTrace.Domain.Catalog;
using StockTrace.IntegrationTests.Authentication;
using StockTrace.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace StockTrace.IntegrationTests.Sales;

public sealed class ConcurrentSalesTests(StockTraceWebApplicationFactory factory)
    : IClassFixture<StockTraceWebApplicationFactory>
{
    [Fact]
    public async Task ConcurrentSalesAgainstLimitedStockAllowOnlyOneSale()
    {
        var client = factory.CreateClient();
        await client.LoginAsAdminAsync();
        var masterData = await GetMasterDataAsync();
        var suffix = Guid.NewGuid().ToString("N");

        var receiptResponse = await client.PostAsJsonAsync("/api/purchase-receipts", new ReceivePurchaseRequest(
            $"CONC-PR-{suffix}", masterData.SupplierId, masterData.WarehouseId, DateTimeOffset.UtcNow,
            [new ReceivePurchaseLineRequest(masterData.ProductId, 10m, 4m)]));
        Assert.Equal(HttpStatusCode.Created, receiptResponse.StatusCode);
        var receipt = await receiptResponse.Content.ReadFromJsonAsync<PurchaseReceiptResult>();
        Assert.NotNull(receipt);

        var firstRequest = new CreateSaleRequest($"CONC-S1-{suffix}", masterData.WarehouseId, DateTimeOffset.UtcNow,
            [new CreateSaleLineRequest(masterData.ProductId, 7m, 8m)]);
        var secondRequest = new CreateSaleRequest($"CONC-S2-{suffix}", masterData.WarehouseId, DateTimeOffset.UtcNow,
            [new CreateSaleLineRequest(masterData.ProductId, 7m, 8m)]);

        var responses = await Task.WhenAll(
            client.PostAsJsonAsync("/api/sales", firstRequest),
            client.PostAsJsonAsync("/api/sales", secondRequest));

        Assert.Single(responses, x => x.StatusCode == HttpStatusCode.Created);
        Assert.Single(responses, x => x.StatusCode == HttpStatusCode.Conflict);

        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var lotId = Assert.Single(receipt.Lines).InventoryLotId;
        var finalBalance = await dbContext.InventoryBalances.AsNoTracking()
            .SingleAsync(x => x.InventoryLotId == lotId);
        Assert.Equal(3m, finalBalance.QuantityOnHand);
        Assert.Equal(1, await dbContext.Sales.CountAsync(x =>
            x.SaleNumber == firstRequest.SaleNumber || x.SaleNumber == secondRequest.SaleNumber));
    }

    private async Task<(Guid ProductId, Guid SupplierId, Guid WarehouseId)> GetMasterDataAsync()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var categoryId = await dbContext.Categories.Where(x => !x.IsDeleted).Select(x => x.Id).FirstAsync();
        var product = new Product($"CONC-{Guid.NewGuid():N}", "Concurrency Test Product", categoryId, "Piece");
        dbContext.Products.Add(product);
        await dbContext.SaveChangesAsync();

        return (
            product.Id,
            await dbContext.Suppliers.Where(x => !x.IsDeleted).Select(x => x.Id).FirstAsync(),
            await dbContext.Warehouses.Where(x => !x.IsDeleted).Select(x => x.Id).FirstAsync());
    }
}
