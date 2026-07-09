using StockTrace.Application.Common.Exceptions;
using StockTrace.Application.Inventory;
using StockTrace.Application.Purchases;
using StockTrace.Domain.Inventory;
using StockTrace.Domain.Purchasing;

namespace StockTrace.UnitTests.Purchases;

public sealed class PurchaseReceiptServiceTests
{
    private static readonly Guid SupplierId = Guid.NewGuid();
    private static readonly Guid WarehouseId = Guid.NewGuid();
    private static readonly Guid ProductId = Guid.NewGuid();

    [Fact]
    public async Task ReceiveAsyncWithValidCommandCreatesPostedTraceableInventory()
    {
        var repository = new FakePurchaseReceiptRepository();
        var notifications = new CapturingLowStockNotificationService();
        var service = new PurchaseReceiptService(repository, notifications);
        var receivedAt = new DateTimeOffset(2026, 7, 8, 10, 0, 0, TimeSpan.Zero);
        var command = new ReceivePurchaseCommand("PR-001", SupplierId, WarehouseId, receivedAt,
            [new ReceivePurchaseLine(ProductId, 12.5m, 7.25m)]);

        var result = await service.ReceiveAsync(command, CancellationToken.None);

        Assert.Equal("Posted", result.Status);
        Assert.Single(result.Lines);
        Assert.NotNull(repository.Receipt);
        Assert.Equal(PurchaseReceiptStatus.Posted, repository.Receipt.Status);
        var lot = Assert.Single(repository.Lots);
        Assert.Equal(SupplierId, lot.SupplierId);
        Assert.Equal(ProductId, lot.ProductId);
        Assert.Equal(12.5m, Assert.Single(repository.Balances).QuantityOnHand);
        var movement = Assert.Single(repository.Movements);
        Assert.Equal(InventoryMovementType.PurchaseReceipt, movement.MovementType);
        Assert.Equal(12.5m, movement.Quantity);
        Assert.Equal(repository.Receipt.Id, movement.CorrelationId);
        var notification = Assert.Single(notifications.Checks);
        Assert.Equal(0m, notification.QuantityBefore);
        Assert.Equal(12.5m, notification.QuantityAfter);
        Assert.Equal(nameof(PurchaseReceipt), notification.TriggeredBy);
    }

    [Fact]
    public async Task ReceiveAsyncWhenReceiptNumberExistsThrowsConflict()
    {
        var repository = new FakePurchaseReceiptRepository { ReceiptExists = true };
        var service = new PurchaseReceiptService(repository, new CapturingLowStockNotificationService());
        var command = ValidCommand();

        await Assert.ThrowsAsync<ConflictException>(() =>
            service.ReceiveAsync(command, CancellationToken.None));
    }

    [Fact]
    public async Task ReceiveAsyncWithoutLinesThrowsValidationException()
    {
        var service = new PurchaseReceiptService(
            new FakePurchaseReceiptRepository(), new CapturingLowStockNotificationService());
        var command = ValidCommand() with { Lines = [] };

        var exception = await Assert.ThrowsAsync<ValidationException>(() =>
            service.ReceiveAsync(command, CancellationToken.None));

        Assert.Contains("lines", exception.Errors.Keys);
    }

    [Fact]
    public async Task ReceiveAsyncWhenProductDoesNotExistThrowsNotFound()
    {
        var repository = new FakePurchaseReceiptRepository { ProductExists = false };
        var service = new PurchaseReceiptService(repository, new CapturingLowStockNotificationService());

        await Assert.ThrowsAsync<NotFoundException>(() =>
            service.ReceiveAsync(ValidCommand(), CancellationToken.None));
    }

    private static ReceivePurchaseCommand ValidCommand() =>
        new("PR-001", SupplierId, WarehouseId, DateTimeOffset.UtcNow,
            [new ReceivePurchaseLine(ProductId, 10, 5)]);

    private sealed class FakePurchaseReceiptRepository : IPurchaseReceiptRepository
    {
        public bool ReceiptExists { get; init; }
        public bool ProductExists { get; init; } = true;
        public PurchaseReceipt? Receipt { get; private set; }
        public IReadOnlyCollection<InventoryLot> Lots { get; private set; } = [];
        public IReadOnlyCollection<InventoryBalance> Balances { get; private set; } = [];
        public IReadOnlyCollection<InventoryMovement> Movements { get; private set; } = [];

        public Task<bool> ReceiptNumberExistsAsync(string receiptNumber, CancellationToken cancellationToken) =>
            Task.FromResult(ReceiptExists);

        public Task<bool> SupplierExistsAsync(Guid supplierId, CancellationToken cancellationToken) =>
            Task.FromResult(true);

        public Task<bool> WarehouseExistsAsync(Guid warehouseId, CancellationToken cancellationToken) =>
            Task.FromResult(true);

        public Task<IReadOnlySet<Guid>> GetActiveProductIdsAsync(
            IEnumerable<Guid> productIds,
            CancellationToken cancellationToken)
        {
            IReadOnlySet<Guid> result = ProductExists ? productIds.ToHashSet() : new HashSet<Guid>();
            return Task.FromResult(result);
        }

        public Task<IReadOnlyDictionary<Guid, decimal>> GetQuantitiesOnHandAsync(
            Guid warehouseId,
            IEnumerable<Guid> productIds,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyDictionary<Guid, decimal>>(
                productIds.ToDictionary(x => x, _ => 0m));

        public Task AddAsync(
            PurchaseReceipt receipt,
            IReadOnlyCollection<InventoryLot> lots,
            IReadOnlyCollection<InventoryBalance> balances,
            IReadOnlyCollection<InventoryMovement> movements,
            CancellationToken cancellationToken)
        {
            Receipt = receipt;
            Lots = lots;
            Balances = balances;
            Movements = movements;
            return Task.CompletedTask;
        }

        public Task<PurchaseReceiptResult?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult<PurchaseReceiptResult?>(null);
    }

    private sealed class CapturingLowStockNotificationService : IInventoryRealtimeNotificationService
    {
        public IReadOnlyCollection<LowStockCheck> Checks { get; private set; } = [];

        public Task PublishAsync(
            IReadOnlyCollection<LowStockCheck> checks,
            CancellationToken cancellationToken)
        {
            Checks = checks;
            return Task.CompletedTask;
        }
    }
}
