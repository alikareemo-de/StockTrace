namespace StockTrace.Application.Reports;

public interface IInventoryReportRepository
{
    Task<PagedResult<InventoryMovementReportItem>> GetMovementsAsync(
        InventoryMovementReportQuery query,
        CancellationToken cancellationToken);
}
