namespace StockTrace.Application.Sales;

public interface ISaleService
{
    Task<SaleResult> CreateAsync(CreateSaleCommand command, CancellationToken cancellationToken);
    Task<SaleResult> GetByIdAsync(Guid id, CancellationToken cancellationToken);
}
