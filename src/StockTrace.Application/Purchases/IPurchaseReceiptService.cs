namespace StockTrace.Application.Purchases;

public interface IPurchaseReceiptService
{
    Task<PurchaseReceiptResult> ReceiveAsync(ReceivePurchaseCommand command, CancellationToken cancellationToken);
    Task<PurchaseReceiptResult> GetByIdAsync(Guid id, CancellationToken cancellationToken);
}
