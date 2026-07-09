using StockTrace.Application.Common.Exceptions;
using StockTrace.Application.Inventory;
using StockTrace.Domain.Inventory;
using StockTrace.Domain.Purchasing;

namespace StockTrace.Application.Purchases;

internal sealed class PurchaseReceiptService(
    IPurchaseReceiptRepository repository,
    IInventoryRealtimeNotificationService inventoryRealtimeNotificationService) : IPurchaseReceiptService
{
    public async Task<PurchaseReceiptResult> ReceiveAsync(
        ReceivePurchaseCommand command,
        CancellationToken cancellationToken)
    {
        Validate(command);

        var receiptNumber = command.ReceiptNumber.Trim();
        if (await repository.ReceiptNumberExistsAsync(receiptNumber, cancellationToken))
            throw new ConflictException($"Purchase receipt '{receiptNumber}' already exists.");
        if (!await repository.SupplierExistsAsync(command.SupplierId, cancellationToken))
            throw new NotFoundException($"Supplier '{command.SupplierId}' was not found.");
        if (!await repository.WarehouseExistsAsync(command.WarehouseId, cancellationToken))
            throw new NotFoundException($"Warehouse '{command.WarehouseId}' was not found.");

        var requestedProductIds = command.Lines.Select(x => x.ProductId).ToHashSet();
        var existingProductIds = await repository.GetActiveProductIdsAsync(requestedProductIds, cancellationToken);
        var missingProductIds = requestedProductIds.Except(existingProductIds).ToArray();
        if (missingProductIds.Length > 0)
            throw new NotFoundException($"Products not found or inactive: {string.Join(", ", missingProductIds)}.");

        var quantitiesBefore = await repository.GetQuantitiesOnHandAsync(
            command.WarehouseId, requestedProductIds, cancellationToken);

        var receipt = new PurchaseReceipt(receiptNumber, command.SupplierId, command.WarehouseId, command.ReceivedAt);
        var lots = new List<InventoryLot>(command.Lines.Count);
        var balances = new List<InventoryBalance>(command.Lines.Count);
        var movements = new List<InventoryMovement>(command.Lines.Count);

        var lineNumber = 0;
        foreach (var requestedLine in command.Lines)
        {
            lineNumber++;
            var line = receipt.AddLine(requestedLine.ProductId, requestedLine.Quantity, requestedLine.UnitCost);
            var lot = new InventoryLot(
                $"{receiptNumber}-{lineNumber:D4}",
                requestedLine.ProductId,
                command.SupplierId,
                line.Id,
                command.ReceivedAt,
                requestedLine.Quantity,
                requestedLine.UnitCost);

            lots.Add(lot);
            balances.Add(new InventoryBalance(command.WarehouseId, lot, requestedLine.Quantity));
            movements.Add(new InventoryMovement(
                command.WarehouseId,
                requestedLine.ProductId,
                lot.Id,
                InventoryMovementType.PurchaseReceipt,
                requestedLine.Quantity,
                requestedLine.UnitCost,
                command.ReceivedAt,
                nameof(PurchaseReceipt),
                receipt.Id,
                receipt.Id));
        }

        receipt.Post();
        await repository.AddAsync(receipt, lots, balances, movements, cancellationToken);

        var stockChecks = command.Lines
            .GroupBy(x => x.ProductId)
            .Select(group =>
            {
                var quantityBefore = quantitiesBefore.GetValueOrDefault(group.Key);
                return new LowStockCheck(
                    command.WarehouseId,
                    group.Key,
                    quantityBefore,
                    quantityBefore + group.Sum(x => x.Quantity),
                    command.ReceivedAt,
                    nameof(PurchaseReceipt));
            })
            .ToArray();

        await inventoryRealtimeNotificationService.PublishAsync(stockChecks, cancellationToken);
        return Map(receipt, lots);
    }

    public async Task<PurchaseReceiptResult> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var result = await repository.GetByIdAsync(id, cancellationToken);
        return result ?? throw new NotFoundException($"Purchase receipt '{id}' was not found.");
    }

    private static void Validate(ReceivePurchaseCommand command)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(command.ReceiptNumber)) errors["receiptNumber"] = ["Receipt number is required."];
        else if (command.ReceiptNumber.Trim().Length > 50) errors["receiptNumber"] = ["Receipt number cannot exceed 50 characters."];
        if (command.SupplierId == Guid.Empty) errors["supplierId"] = ["Supplier is required."];
        if (command.WarehouseId == Guid.Empty) errors["warehouseId"] = ["Warehouse is required."];
        if (command.Lines is null || command.Lines.Count == 0) errors["lines"] = ["At least one line is required."];

        if (command.Lines is not null)
        {
            var lineErrors = command.Lines
                .Select((line, index) => (line, index))
                .Where(x => x.line.ProductId == Guid.Empty || x.line.Quantity <= 0 || x.line.UnitCost < 0)
                .Select(x => $"Line {x.index + 1} must have a product, positive quantity, and non-negative unit cost.")
                .ToArray();
            if (lineErrors.Length > 0) errors["lines"] = lineErrors;
        }

        if (errors.Count > 0) throw new ValidationException(errors);
    }

    private static PurchaseReceiptResult Map(PurchaseReceipt receipt, IReadOnlyList<InventoryLot> lots)
    {
        var lines = receipt.Lines.Zip(lots, (line, lot) => new PurchaseReceiptLineResult(
            line.Id, line.ProductId, line.Quantity, line.UnitCost, lot.Id, lot.LotNumber)).ToArray();

        return new PurchaseReceiptResult(receipt.Id, receipt.ReceiptNumber, receipt.SupplierId,
            receipt.WarehouseId, receipt.ReceivedAt, receipt.Status.ToString(), lines);
    }
}
