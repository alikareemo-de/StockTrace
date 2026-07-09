using StockTrace.Application.Common.Exceptions;

namespace StockTrace.Application.Reports;

internal sealed class InventoryReportService(IInventoryReportRepository repository) : IInventoryReportService
{
    public Task<PagedResult<InventoryMovementReportItem>> GetMovementsAsync(
        InventoryMovementReportQuery query,
        CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, string[]>();
        if (query.PageNumber < 1) errors["pageNumber"] = ["Page number must be at least 1."];
        if (query.PageSize is < 1 or > 100) errors["pageSize"] = ["Page size must be between 1 and 100."];
        if (query.From.HasValue && query.To.HasValue && query.From > query.To)
            errors["dateRange"] = ["The from date cannot be later than the to date."];
        if (errors.Count > 0) throw new ValidationException(errors);

        return repository.GetMovementsAsync(query, cancellationToken);
    }
}
