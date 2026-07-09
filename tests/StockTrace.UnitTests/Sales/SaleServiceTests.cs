using StockTrace.Application.Abstractions;
using StockTrace.Application.Inventory;
using StockTrace.Application.Sales;
using StockTrace.Domain.Inventory;
using StockTrace.Domain.Sales;

namespace StockTrace.UnitTests.Sales;

public sealed class SaleServiceTests
{
    [Fact]
    public async Task CreateAsyncAllocatesOldestLotsFirst()
    {
        var warehouseId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var olderLot = CreateLot(productId, new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero), 4m, 2m);
        var newerLot = CreateLot(productId, new DateTimeOffset(2026, 2, 1, 0, 0, 0, TimeSpan.Zero), 6m, 3m);
        var olderBalance = new InventoryBalance(warehouseId, olderLot, 4m);
        var newerBalance = new InventoryBalance(warehouseId, newerLot, 6m);
        var repository = new FakeSaleRepository([newerBalance, olderBalance]);
        var service = new SaleService(repository, repository, repository, new NullLowStockNotificationService());

        var result = await service.CreateAsync(new CreateSaleCommand(
            "SALE-FIFO-001", warehouseId, DateTimeOffset.UtcNow,
            [new CreateSaleLine(productId, 7m, 8m)]), CancellationToken.None);

        var allocations = Assert.Single(result.Lines).Allocations.ToArray();
        Assert.Equal(2, allocations.Length);
        Assert.Equal(olderLot.Id, allocations[0].InventoryLotId);
        Assert.Equal(4m, allocations[0].Quantity);
        Assert.Equal(newerLot.Id, allocations[1].InventoryLotId);
        Assert.Equal(3m, allocations[1].Quantity);
        Assert.Equal(0m, olderBalance.QuantityOnHand);
        Assert.Equal(3m, newerBalance.QuantityOnHand);
        Assert.Equal(17m, result.Lines.Single().CostOfGoodsSold);
    }

    private static InventoryLot CreateLot(
        Guid productId,
        DateTimeOffset receivedAt,
        decimal quantity,
        decimal unitCost) =>
        new($"LOT-{Guid.NewGuid():N}", productId, Guid.NewGuid(), Guid.NewGuid(), receivedAt, quantity, unitCost);

    private sealed class FakeSaleRepository(IReadOnlyList<InventoryBalance> balances)
        : ISaleRepository, IInventoryStockRepository, ITransactionRunner
    {
        public Task<T> ExecuteAsync<T>(
            Func<CancellationToken, Task<T>> operation,
            CancellationToken cancellationToken) => operation(cancellationToken);

        public Task<bool> SaleNumberExistsAsync(string saleNumber, CancellationToken cancellationToken) =>
            Task.FromResult(false);

        public Task<bool> WarehouseExistsAsync(Guid warehouseId, CancellationToken cancellationToken) =>
            Task.FromResult(true);

        public Task<IReadOnlySet<Guid>> GetActiveProductIdsAsync(
            IEnumerable<Guid> productIds,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlySet<Guid>>(productIds.ToHashSet());

        public Task<IReadOnlyList<InventoryBalance>> GetAvailableBalancesForUpdateAsync(
            Guid warehouseId,
            Guid productId,
            CancellationToken cancellationToken) => Task.FromResult(balances);

        public Task<InventoryBalance?> GetBalanceForUpdateAsync(
            Guid warehouseId,
            Guid inventoryLotId,
            CancellationToken cancellationToken) =>
            Task.FromResult<InventoryBalance?>(null);

        public Task AddAsync(
            Sale sale,
            IReadOnlyCollection<InventoryMovement> movements,
            CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<SaleResult?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult<SaleResult?>(null);
    }

    private sealed class NullLowStockNotificationService : IInventoryRealtimeNotificationService
    {
        public Task PublishAsync(
            IReadOnlyCollection<LowStockCheck> checks,
            CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }
}
