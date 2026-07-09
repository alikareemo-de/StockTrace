using System.Net;
using System.Net.Http.Json;
using StockTrace.Api.Contracts.MasterData;
using StockTrace.Application.MasterData;
using StockTrace.IntegrationTests.Authentication;

namespace StockTrace.IntegrationTests.MasterData;

public sealed class MasterDataTests(StockTraceWebApplicationFactory factory)
    : IClassFixture<StockTraceWebApplicationFactory>
{
    [Fact]
    public async Task MasterDataEndpointsExposeSeedDataAndAllowThresholdConfiguration()
    {
        var client = factory.CreateClient();
        await client.LoginAsAdminAsync();

        var suppliers = await client.GetFromJsonAsync<IReadOnlyCollection<SupplierResult>>(
            "/api/master-data/suppliers");
        var warehouses = await client.GetFromJsonAsync<IReadOnlyCollection<WarehouseResult>>(
            "/api/master-data/warehouses");
        var products = await client.GetFromJsonAsync<IReadOnlyCollection<ProductResult>>(
            "/api/master-data/products");
        var categories = await client.GetFromJsonAsync<IReadOnlyCollection<CategoryResult>>(
            "/api/master-data/categories");

        Assert.NotNull(suppliers);
        Assert.NotNull(warehouses);
        Assert.NotNull(products);
        Assert.NotNull(categories);

        Assert.Contains(suppliers, x => x.Code == "SUP-001");
        var warehouse = Assert.Single(warehouses, x => x.Code == "WH-001");
        var product = Assert.Single(products, x => x.Sku == "SKU-001");
        Assert.Contains(categories, x => x.Id == product.CategoryId);

        var response = await client.PutAsJsonAsync(
            $"/api/master-data/warehouses/{warehouse.Id}/products/{product.Id}/low-stock-threshold",
            new SetLowStockThresholdRequest(7m));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var threshold = await response.Content.ReadFromJsonAsync<WarehouseProductThresholdResult>();
        Assert.NotNull(threshold);
        Assert.Equal(warehouse.Id, threshold.WarehouseId);
        Assert.Equal(product.Id, threshold.ProductId);
        Assert.Equal(7m, threshold.LowStockThreshold);
    }
}
