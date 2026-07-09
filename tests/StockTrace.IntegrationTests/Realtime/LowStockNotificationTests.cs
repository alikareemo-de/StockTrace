using System.Net;
using System.Net.Http.Json;
using StockTrace.Api.Contracts.Purchases;
using StockTrace.Api.Contracts.Sales;
using StockTrace.Api.Realtime;
using StockTrace.Application.Inventory;
using StockTrace.Domain.Catalog;
using StockTrace.IntegrationTests.Authentication;
using StockTrace.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace StockTrace.IntegrationTests.Realtime;

public sealed class LowStockNotificationTests(StockTraceWebApplicationFactory factory)
    : IClassFixture<StockTraceWebApplicationFactory>
{
    [Fact]
    public async Task SaleCrossingThresholdPublishesLowStockAlert()
    {
        var client = factory.CreateClient();
        var accessToken = await client.LoginAsAdminAsync();
        var data = await CreateTestDataAsync();
        var receivedStockChangedAlert =
            new TaskCompletionSource<StockChangedAlert>(TaskCreationOptions.RunContinuationsAsynchronously);
        var receivedLowStockAlert =
            new TaskCompletionSource<LowStockAlert>(TaskCreationOptions.RunContinuationsAsynchronously);

        var purchaseResponse = await client.PostAsJsonAsync("/api/purchase-receipts", new ReceivePurchaseRequest(
            $"PR-LOW-{Guid.NewGuid():N}",
            data.SupplierId,
            data.WarehouseId,
            DateTimeOffset.UtcNow.AddMinutes(-5),
            [new ReceivePurchaseLineRequest(data.ProductId, 10m, 4m)]));
        Assert.Equal(HttpStatusCode.Created, purchaseResponse.StatusCode);

        await using var connection = new HubConnectionBuilder()
            .WithUrl(new Uri(factory.Server.BaseAddress, LowStockHub.Route), options =>
            {
                options.Transports = HttpTransportType.LongPolling;
                options.HttpMessageHandlerFactory = _ => factory.Server.CreateHandler();
                options.AccessTokenProvider = () => Task.FromResult<string?>(accessToken);
            })
            .Build();

        connection.On<StockChangedAlert>(nameof(ILowStockClient.StockChanged), alert =>
        {
            if (alert.ProductId == data.ProductId)
            {
                receivedStockChangedAlert.TrySetResult(alert);
            }
        });

        connection.On<LowStockAlert>(nameof(ILowStockClient.LowStockReached), alert =>
        {
            receivedLowStockAlert.TrySetResult(alert);
        });

        await connection.StartAsync();

        var saleResponse = await client.PostAsJsonAsync("/api/sales", new CreateSaleRequest(
            $"SALE-LOW-{Guid.NewGuid():N}",
            data.WarehouseId,
            DateTimeOffset.UtcNow,
            [new CreateSaleLineRequest(data.ProductId, 6m, 9m)]));

        Assert.Equal(HttpStatusCode.Created, saleResponse.StatusCode);

        var stockChangedAlert = await receivedStockChangedAlert.Task.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.Equal(data.WarehouseId, stockChangedAlert.WarehouseId);
        Assert.Equal(data.ProductId, stockChangedAlert.ProductId);
        Assert.Equal(data.ProductSku, stockChangedAlert.ProductSku);
        Assert.Equal(10m, stockChangedAlert.QuantityBefore);
        Assert.Equal(4m, stockChangedAlert.QuantityAfter);
        Assert.Equal("Sale", stockChangedAlert.TriggeredBy);

        var alert = await receivedLowStockAlert.Task.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.Equal(data.WarehouseId, alert.WarehouseId);
        Assert.Equal(data.ProductId, alert.ProductId);
        Assert.Equal(data.ProductSku, alert.ProductSku);
        Assert.Equal(5m, alert.Threshold);
        Assert.Equal(4m, alert.QuantityOnHand);
        Assert.Equal("Sale", alert.TriggeredBy);
    }

    private async Task<(Guid ProductId, string ProductSku, Guid SupplierId, Guid WarehouseId)> CreateTestDataAsync()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var suffix = Guid.NewGuid().ToString("N");
        var category = new Category($"LowStock-{suffix}");
        var sku = $"LOW-{suffix[..8]}";
        var product = new Product(sku, $"Low Stock Product {suffix}", category.Id, "Piece", 5m);
        dbContext.AddRange(category, product);
        await dbContext.SaveChangesAsync();

        return (
            product.Id,
            sku,
            await dbContext.Suppliers.Where(x => !x.IsDeleted).Select(x => x.Id).FirstAsync(),
            await dbContext.Warehouses.Where(x => !x.IsDeleted).Select(x => x.Id).FirstAsync());
    }
}
