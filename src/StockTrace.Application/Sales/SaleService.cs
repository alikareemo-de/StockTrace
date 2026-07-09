using StockTrace.Application.Abstractions;
using StockTrace.Application.Common.Exceptions;
using StockTrace.Application.Inventory;
using StockTrace.Domain.Inventory;
using StockTrace.Domain.Sales;

namespace StockTrace.Application.Sales;

internal sealed class SaleService(
    ISaleRepository repository,
    IInventoryStockRepository stockRepository,
    ITransactionRunner transactionRunner,
    IInventoryRealtimeNotificationService inventoryRealtimeNotificationService) : ISaleService
{
    public async Task<SaleResult> CreateAsync(CreateSaleCommand command, CancellationToken cancellationToken)
    {
        Validate(command);
        var outcome = await transactionRunner.ExecuteAsync(
            token => CreateWithinTransactionAsync(command, token), cancellationToken);

        await inventoryRealtimeNotificationService.PublishAsync(outcome.LowStockChecks, cancellationToken);
        return outcome.Result;
    }

    public async Task<SaleResult> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        await repository.GetByIdAsync(id, cancellationToken)
        ?? throw new NotFoundException($"Sale '{id}' was not found.");

    private async Task<SaleCreationOutcome> CreateWithinTransactionAsync(
        CreateSaleCommand command,
        CancellationToken cancellationToken)
    {
        var saleNumber = command.SaleNumber.Trim();
        if (await repository.SaleNumberExistsAsync(saleNumber, cancellationToken))
            throw new ConflictException($"Sale '{saleNumber}' already exists.");
        if (!await repository.WarehouseExistsAsync(command.WarehouseId, cancellationToken))
            throw new NotFoundException($"Warehouse '{command.WarehouseId}' was not found.");

        var productIds = command.Lines.Select(x => x.ProductId).ToHashSet();
        var existingProductIds = await repository.GetActiveProductIdsAsync(productIds, cancellationToken);
        var missingProductIds = productIds.Except(existingProductIds).ToArray();
        if (missingProductIds.Length > 0)
            throw new NotFoundException($"Products not found or inactive: {string.Join(", ", missingProductIds)}.");

        var sale = new Sale(saleNumber, command.WarehouseId, command.SoldAt);
        var movements = new List<InventoryMovement>();
        var lowStockChecks = new List<LowStockCheck>();

        // Stable lock order reduces deadlock risk when multi-line sales compete.
        foreach (var requestedLine in command.Lines.OrderBy(x => x.ProductId))
        {
            var balances = await stockRepository.GetAvailableBalancesForUpdateAsync(
                command.WarehouseId, requestedLine.ProductId, cancellationToken);
            var available = balances.Sum(x => x.QuantityOnHand);
            if (available < requestedLine.Quantity)
                throw new ConflictException(
                    $"Insufficient stock for product '{requestedLine.ProductId}'. Requested {requestedLine.Quantity}, available {available}.");

            var saleLine = sale.AddLine(requestedLine.ProductId, requestedLine.Quantity, requestedLine.UnitPrice);
            var remaining = requestedLine.Quantity;

            foreach (var balance in balances.OrderBy(x => x.InventoryLot.ReceivedAt).ThenBy(x => x.InventoryLot.Id))
            {
                if (remaining == 0) break;
                var allocated = Math.Min(remaining, balance.QuantityOnHand);
                balance.Decrease(allocated);
                saleLine.AddAllocation(balance.InventoryLot, allocated);
                movements.Add(new InventoryMovement(
                    command.WarehouseId,
                    requestedLine.ProductId,
                    balance.InventoryLotId,
                    InventoryMovementType.SaleIssue,
                    -allocated,
                    balance.InventoryLot.UnitCost,
                    command.SoldAt,
                    nameof(Sale),
                    sale.Id,
                    sale.Id));
                remaining -= allocated;
            }

            lowStockChecks.Add(new LowStockCheck(
                command.WarehouseId,
                requestedLine.ProductId,
                available,
                balances.Sum(x => x.QuantityOnHand),
                command.SoldAt,
                nameof(Sale)));
        }

        sale.Post();
        await repository.AddAsync(sale, movements, cancellationToken);
        return new SaleCreationOutcome(Map(sale), lowStockChecks);
    }

    private static void Validate(CreateSaleCommand command)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(command.SaleNumber)) errors["saleNumber"] = ["Sale number is required."];
        else if (command.SaleNumber.Trim().Length > 50) errors["saleNumber"] = ["Sale number cannot exceed 50 characters."];
        if (command.WarehouseId == Guid.Empty) errors["warehouseId"] = ["Warehouse is required."];
        if (command.Lines is null || command.Lines.Count == 0) errors["lines"] = ["At least one line is required."];

        if (command.Lines is not null)
        {
            var invalidLines = command.Lines.Select((line, index) => (line, index))
                .Where(x => x.line.ProductId == Guid.Empty || x.line.Quantity <= 0 || x.line.UnitPrice < 0)
                .Select(x => $"Line {x.index + 1} must have a product, positive quantity, and non-negative unit price.")
                .ToList();
            var duplicateProducts = command.Lines.GroupBy(x => x.ProductId).Where(x => x.Count() > 1).Select(x => x.Key);
            invalidLines.AddRange(duplicateProducts.Select(x => $"Product '{x}' appears more than once."));
            if (invalidLines.Count > 0) errors["lines"] = invalidLines.ToArray();
        }

        if (errors.Count > 0) throw new ValidationException(errors);
    }

    private static SaleResult Map(Sale sale) => new(
        sale.Id, sale.SaleNumber, sale.WarehouseId, sale.SoldAt, sale.Status.ToString(),
        sale.Lines.Select(line => new SaleLineResult(
            line.Id, line.ProductId, line.Quantity, line.UnitPrice,
            line.Allocations.Sum(x => x.Quantity * x.UnitCost),
            line.Allocations.Select(x => new SaleAllocationResult(
                x.InventoryLotId, x.InventoryLot.LotNumber, x.InventoryLot.SupplierId, x.Quantity, x.UnitCost)).ToArray())).ToArray());

    private sealed record SaleCreationOutcome(
        SaleResult Result,
        IReadOnlyCollection<LowStockCheck> LowStockChecks);
}
