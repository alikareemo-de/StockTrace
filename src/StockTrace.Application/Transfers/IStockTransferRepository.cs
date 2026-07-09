using StockTrace.Domain.Inventory;
using StockTrace.Domain.Transfers;

namespace StockTrace.Application.Transfers;

public interface IStockTransferRepository
{
    Task<bool> TransferNumberExistsAsync(string transferNumber, CancellationToken cancellationToken);
    Task<IReadOnlySet<Guid>> GetExistingWarehouseIdsAsync(IEnumerable<Guid> warehouseIds, CancellationToken cancellationToken);
    Task<IReadOnlySet<Guid>> GetActiveProductIdsAsync(IEnumerable<Guid> productIds, CancellationToken cancellationToken);
    Task AddAsync(
        StockTransfer transfer,
        IReadOnlyCollection<InventoryBalance> newDestinationBalances,
        IReadOnlyCollection<InventoryMovement> movements,
        CancellationToken cancellationToken);
    Task<StockTransferResult?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
}
