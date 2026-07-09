using System.Net;
using System.Net.Http.Json;
using StockTrace.Api.Contracts.Purchases;
using StockTrace.Application.Reports;
using StockTrace.Domain.Catalog;
using StockTrace.IntegrationTests.Authentication;
using StockTrace.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace StockTrace.IntegrationTests.Reports;

public sealed class InventoryMovementReportTests(StockTraceWebApplicationFactory factory)
    : IClassFixture<StockTraceWebApplicationFactory>
{
    [Fact]
    public async Task ReportAppliesRequiredFiltersAndPagination()
    {
        var client = factory.CreateClient();
        await client.LoginAsAdminAsync();
        var data = await CreateTestDataAsync();
        var occurredAt = DateTimeOffset.UtcNow;
        var receiptNumber = $"REP-{Guid.NewGuid():N}";
        var receiptResponse = await client.PostAsJsonAsync("/api/purchase-receipts", new ReceivePurchaseRequest(
            receiptNumber, data.SupplierId, data.WarehouseId, occurredAt,
            [new ReceivePurchaseLineRequest(data.ProductId, 5m, 12m)]));
        Assert.Equal(HttpStatusCode.Created, receiptResponse.StatusCode);

        var matchingUrl = BuildUrl(
            data.WarehouseId, data.SupplierId, data.CategoryId, data.ProductId,
            occurredAt.AddMinutes(-1), occurredAt.AddMinutes(1));
        var result = await client.GetFromJsonAsync<PagedResult<InventoryMovementReportItem>>(matchingUrl);
        Assert.NotNull(result);
        var item = Assert.Single(result.Items);
        Assert.Equal(receiptNumber, item.LotNumber[..receiptNumber.Length]);
        Assert.Equal(5m, item.Quantity);
        Assert.Equal(60m, item.Value);
        Assert.Equal(1, result.PageNumber);
        Assert.Equal(10, result.PageSize);

        Assert.Equal(0, await GetTotalAsync(client, BuildUrl(
            data.WarehouseId, Guid.NewGuid(), data.CategoryId, data.ProductId,
            occurredAt.AddMinutes(-1), occurredAt.AddMinutes(1))));
        Assert.Equal(0, await GetTotalAsync(client, BuildUrl(
            data.WarehouseId, data.SupplierId, Guid.NewGuid(), data.ProductId,
            occurredAt.AddMinutes(-1), occurredAt.AddMinutes(1))));
        Assert.Equal(0, await GetTotalAsync(client, BuildUrl(
            Guid.NewGuid(), data.SupplierId, data.CategoryId, data.ProductId,
            occurredAt.AddMinutes(-1), occurredAt.AddMinutes(1))));
        Assert.Equal(0, await GetTotalAsync(client, BuildUrl(
            data.WarehouseId, data.SupplierId, data.CategoryId, data.ProductId,
            occurredAt.AddDays(1), occurredAt.AddDays(2))));
    }

    private static async Task<int> GetTotalAsync(HttpClient client, string url)
    {
        var result = await client.GetFromJsonAsync<PagedResult<InventoryMovementReportItem>>(url);
        return Assert.IsType<PagedResult<InventoryMovementReportItem>>(result).TotalCount;
    }

    private static string BuildUrl(
        Guid warehouseId,
        Guid supplierId,
        Guid categoryId,
        Guid productId,
        DateTimeOffset from,
        DateTimeOffset to) =>
        $"/api/reports/inventory-movements?warehouseId={warehouseId}&supplierId={supplierId}" +
        $"&categoryId={categoryId}&productId={productId}" +
        $"&from={Uri.EscapeDataString(from.ToString("O"))}&to={Uri.EscapeDataString(to.ToString("O"))}" +
        "&pageNumber=1&pageSize=10";

    private async Task<(Guid ProductId, Guid CategoryId, Guid SupplierId, Guid WarehouseId)> CreateTestDataAsync()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var suffix = Guid.NewGuid().ToString("N");
        var category = new Category($"Report-{suffix}");
        var product = new Product($"REP-{suffix}", "Report Test Product", category.Id, "Piece");
        dbContext.AddRange(category, product);
        await dbContext.SaveChangesAsync();

        return (
            product.Id,
            category.Id,
            await dbContext.Suppliers.Where(x => !x.IsDeleted).Select(x => x.Id).FirstAsync(),
            await dbContext.Warehouses.Where(x => !x.IsDeleted).Select(x => x.Id).FirstAsync());
    }
}
