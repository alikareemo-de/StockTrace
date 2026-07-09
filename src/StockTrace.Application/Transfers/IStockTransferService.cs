namespace StockTrace.Application.Transfers;

public interface IStockTransferService
{
    Task<StockTransferResult> CreateAsync(CreateStockTransferCommand command, CancellationToken cancellationToken);
    Task<StockTransferResult> GetByIdAsync(Guid id, CancellationToken cancellationToken);
}
