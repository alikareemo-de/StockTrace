using StockTrace.Domain.Inventory;
using StockTrace.Domain.Purchasing;

namespace StockTrace.Application.Purchases;

public interface IPurchaseReceiptRepository
{
    Task<bool> ReceiptNumberExistsAsync(string receiptNumber, CancellationToken cancellationToken);
    Task<bool> SupplierExistsAsync(Guid supplierId, CancellationToken cancellationToken);
    Task<bool> WarehouseExistsAsync(Guid warehouseId, CancellationToken cancellationToken);
    Task<IReadOnlySet<Guid>> GetActiveProductIdsAsync(IEnumerable<Guid> productIds, CancellationToken cancellationToken);
    Task<IReadOnlyDictionary<Guid, decimal>> GetQuantitiesOnHandAsync(
        Guid warehouseId,
        IEnumerable<Guid> productIds,
        CancellationToken cancellationToken);
    Task AddAsync(
        PurchaseReceipt receipt,
        IReadOnlyCollection<InventoryLot> lots,
        IReadOnlyCollection<InventoryBalance> balances,
        IReadOnlyCollection<InventoryMovement> movements,
        CancellationToken cancellationToken);
    Task<PurchaseReceiptResult?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
}
