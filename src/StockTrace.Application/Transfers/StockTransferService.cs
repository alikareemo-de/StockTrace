using StockTrace.Application.Abstractions;
using StockTrace.Application.Common.Exceptions;
using StockTrace.Application.Inventory;
using StockTrace.Domain.Inventory;
using StockTrace.Domain.Transfers;

namespace StockTrace.Application.Transfers;

internal sealed class StockTransferService(
    IStockTransferRepository repository,
    IInventoryStockRepository stockRepository,
    ITransactionRunner transactionRunner,
    IInventoryRealtimeNotificationService inventoryRealtimeNotificationService) : IStockTransferService
{
    public async Task<StockTransferResult> CreateAsync(
        CreateStockTransferCommand command,
        CancellationToken cancellationToken)
    {
        Validate(command);
        var outcome = await transactionRunner.ExecuteAsync(
            token => CreateWithinTransactionAsync(command, token), cancellationToken);

        await inventoryRealtimeNotificationService.PublishAsync(outcome.LowStockChecks, cancellationToken);
        return outcome.Result;
    }

    public async Task<StockTransferResult> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        await repository.GetByIdAsync(id, cancellationToken)
        ?? throw new NotFoundException($"Stock transfer '{id}' was not found.");

    private async Task<StockTransferCreationOutcome> CreateWithinTransactionAsync(
        CreateStockTransferCommand command,
        CancellationToken cancellationToken)
    {
        var transferNumber = command.TransferNumber.Trim();
        if (await repository.TransferNumberExistsAsync(transferNumber, cancellationToken))
            throw new ConflictException($"Stock transfer '{transferNumber}' already exists.");

        var requiredWarehouseIds = new[] { command.SourceWarehouseId, command.DestinationWarehouseId };
        var existingWarehouseIds = await repository.GetExistingWarehouseIdsAsync(requiredWarehouseIds, cancellationToken);
        var missingWarehouseIds = requiredWarehouseIds.Except(existingWarehouseIds).ToArray();
        if (missingWarehouseIds.Length > 0)
            throw new NotFoundException($"Warehouses not found: {string.Join(", ", missingWarehouseIds)}.");

        var productIds = command.Lines.Select(x => x.ProductId).ToHashSet();
        var existingProductIds = await repository.GetActiveProductIdsAsync(productIds, cancellationToken);
        var missingProductIds = productIds.Except(existingProductIds).ToArray();
        if (missingProductIds.Length > 0)
            throw new NotFoundException($"Products not found or inactive: {string.Join(", ", missingProductIds)}.");

        var transfer = new StockTransfer(transferNumber, command.SourceWarehouseId, command.DestinationWarehouseId);
        var newDestinationBalances = new List<InventoryBalance>();
        var movements = new List<InventoryMovement>();
        var lowStockChecks = new List<LowStockCheck>();

        foreach (var requestedLine in command.Lines.OrderBy(x => x.ProductId))
        {
            var sourceBalances = await stockRepository.GetAvailableBalancesForUpdateAsync(
                command.SourceWarehouseId, requestedLine.ProductId, cancellationToken);
            var available = sourceBalances.Sum(x => x.QuantityOnHand);
            if (available < requestedLine.Quantity)
                throw new ConflictException(
                    $"Insufficient stock for product '{requestedLine.ProductId}'. Requested {requestedLine.Quantity}, available {available}.");

            var transferLine = transfer.AddLine(requestedLine.ProductId, requestedLine.Quantity);
            var remaining = requestedLine.Quantity;

            foreach (var sourceBalance in sourceBalances
                         .OrderBy(x => x.InventoryLot.ReceivedAt)
                         .ThenBy(x => x.InventoryLot.Id))
            {
                if (remaining == 0) break;
                var moved = Math.Min(remaining, sourceBalance.QuantityOnHand);
                sourceBalance.Decrease(moved);
                transferLine.AddAllocation(sourceBalance.InventoryLot, moved);

                var destinationBalance = await stockRepository.GetBalanceForUpdateAsync(
                    command.DestinationWarehouseId, sourceBalance.InventoryLotId, cancellationToken);
                if (destinationBalance is null)
                {
                    destinationBalance = new InventoryBalance(
                        command.DestinationWarehouseId, sourceBalance.InventoryLot, 0);
                    newDestinationBalances.Add(destinationBalance);
                }
                destinationBalance.Increase(moved);

                movements.Add(CreateMovement(
                    command.SourceWarehouseId, requestedLine.ProductId, sourceBalance.InventoryLot,
                    InventoryMovementType.TransferOut, -moved, command.TransferredAt, transfer.Id));
                movements.Add(CreateMovement(
                    command.DestinationWarehouseId, requestedLine.ProductId, sourceBalance.InventoryLot,
                    InventoryMovementType.TransferIn, moved, command.TransferredAt, transfer.Id));
                remaining -= moved;
            }

            lowStockChecks.Add(new LowStockCheck(
                command.SourceWarehouseId,
                requestedLine.ProductId,
                available,
                sourceBalances.Sum(x => x.QuantityOnHand),
                command.TransferredAt,
                nameof(StockTransfer)));
        }

        transfer.Complete(command.TransferredAt);
        await repository.AddAsync(transfer, newDestinationBalances, movements, cancellationToken);
        return new StockTransferCreationOutcome(Map(transfer), lowStockChecks);
    }

    private static InventoryMovement CreateMovement(
        Guid warehouseId,
        Guid productId,
        InventoryLot lot,
        InventoryMovementType type,
        decimal quantity,
        DateTimeOffset occurredAt,
        Guid transferId) =>
        new(warehouseId, productId, lot.Id, type, quantity, lot.UnitCost, occurredAt,
            nameof(StockTransfer), transferId, transferId);

    private static void Validate(CreateStockTransferCommand command)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(command.TransferNumber)) errors["transferNumber"] = ["Transfer number is required."];
        else if (command.TransferNumber.Trim().Length > 50) errors["transferNumber"] = ["Transfer number cannot exceed 50 characters."];
        if (command.SourceWarehouseId == Guid.Empty) errors["sourceWarehouseId"] = ["Source warehouse is required."];
        if (command.DestinationWarehouseId == Guid.Empty) errors["destinationWarehouseId"] = ["Destination warehouse is required."];
        if (command.SourceWarehouseId == command.DestinationWarehouseId)
            errors["destinationWarehouseId"] = ["Source and destination warehouses must be different."];
        if (command.Lines is null || command.Lines.Count == 0) errors["lines"] = ["At least one line is required."];

        if (command.Lines is not null)
        {
            var invalidLines = command.Lines.Select((line, index) => (line, index))
                .Where(x => x.line.ProductId == Guid.Empty || x.line.Quantity <= 0)
                .Select(x => $"Line {x.index + 1} must have a product and positive quantity.")
                .ToList();
            invalidLines.AddRange(command.Lines.GroupBy(x => x.ProductId).Where(x => x.Count() > 1)
                .Select(x => $"Product '{x.Key}' appears more than once."));
            if (invalidLines.Count > 0) errors["lines"] = invalidLines.ToArray();
        }

        if (errors.Count > 0) throw new ValidationException(errors);
    }

    private static StockTransferResult Map(StockTransfer transfer) => new(
        transfer.Id, transfer.TransferNumber, transfer.SourceWarehouseId, transfer.DestinationWarehouseId,
        transfer.Status.ToString(), transfer.CompletedAt,
        transfer.Lines.Select(line => new StockTransferLineResult(
            line.Id, line.ProductId, line.RequestedQuantity,
            line.Allocations.Select(x => new TransferAllocationResult(
                x.InventoryLotId, x.InventoryLot.LotNumber, x.InventoryLot.SupplierId, x.Quantity,
                x.InventoryLot.UnitCost)).ToArray())).ToArray());

    private sealed record StockTransferCreationOutcome(
        StockTransferResult Result,
        IReadOnlyCollection<LowStockCheck> LowStockChecks);
}
