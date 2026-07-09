using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using StockTrace.Api.Contracts.Purchases;
using StockTrace.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace StockTrace.IntegrationTests.Authentication;

public sealed class AuthorizationTests(StockTraceWebApplicationFactory factory)
    : IClassFixture<StockTraceWebApplicationFactory>
{
    [Fact]
    public async Task ProtectedEndpointsRequireTokenAndRejectMissingPermission()
    {
        var anonymousClient = factory.CreateClient();
        var anonymousResponse = await anonymousClient.GetAsync("/api/master-data/products");
        Assert.Equal(HttpStatusCode.Unauthorized, anonymousResponse.StatusCode);

        var salesClient = factory.CreateClient();
        await LoginAsync(salesClient, "sales.user", "Sales@12345");

        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var supplierId = await dbContext.Suppliers.Where(x => !x.IsDeleted).Select(x => x.Id).FirstAsync();
        var warehouseId = await dbContext.Warehouses.Where(x => !x.IsDeleted).Select(x => x.Id).FirstAsync();
        var productId = await dbContext.Products.Where(x => !x.IsDeleted && x.IsActive).Select(x => x.Id).FirstAsync();

        var forbiddenResponse = await salesClient.PostAsJsonAsync("/api/purchase-receipts", new ReceivePurchaseRequest(
            $"AUTH-PR-{Guid.NewGuid():N}",
            supplierId,
            warehouseId,
            DateTimeOffset.UtcNow,
            [new ReceivePurchaseLineRequest(productId, 1m, 1m)]));

        Assert.Equal(HttpStatusCode.Forbidden, forbiddenResponse.StatusCode);
    }

    private static async Task LoginAsync(HttpClient client, string username, string password)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new { username, password });
        response.EnsureSuccessStatusCode();
        var login = await response.Content.ReadFromJsonAsync<LoginResponse>();
        ArgumentNullException.ThrowIfNull(login);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);
    }

    private sealed record LoginResponse(string AccessToken);
}
