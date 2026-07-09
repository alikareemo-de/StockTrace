using StockTrace.Domain.Inventory;
using StockTrace.Domain.Sales;

namespace StockTrace.Application.Sales;

public interface ISaleRepository
{
    Task<bool> SaleNumberExistsAsync(string saleNumber, CancellationToken cancellationToken);
    Task<bool> WarehouseExistsAsync(Guid warehouseId, CancellationToken cancellationToken);
    Task<IReadOnlySet<Guid>> GetActiveProductIdsAsync(IEnumerable<Guid> productIds, CancellationToken cancellationToken);
    Task AddAsync(Sale sale, IReadOnlyCollection<InventoryMovement> movements, CancellationToken cancellationToken);
    Task<SaleResult?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
}
