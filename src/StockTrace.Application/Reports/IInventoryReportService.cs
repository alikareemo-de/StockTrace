namespace StockTrace.Application.Reports;

public interface IInventoryReportService
{
    Task<PagedResult<InventoryMovementReportItem>> GetMovementsAsync(
        InventoryMovementReportQuery query,
        CancellationToken cancellationToken);
}
