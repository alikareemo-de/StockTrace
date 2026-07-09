using System.Data;
using StockTrace.Domain.Catalog;
using StockTrace.Domain.Inventory;
using StockTrace.Domain.Partners;
using StockTrace.Domain.Purchasing;
using StockTrace.Domain.Warehousing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace StockTrace.Infrastructure.Persistence;

public sealed class DatabaseInitializer(ApplicationDbContext dbContext)
{
    public async Task InitialiseAsync(CancellationToken cancellationToken = default)
    {
        await using var migrationLock = await AcquireMigrationLockAsync(cancellationToken);
        await dbContext.Database.MigrateAsync(cancellationToken);
        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);
        await dbContext.Database.ExecuteSqlRawAsync(
            """
            DECLARE @result int;
            EXEC @result = sp_getapplock
                @Resource = 'StockTrace_DatabaseInitializer_Seed',
                @LockOwner = 'Transaction',
                @LockMode = 'Exclusive',
                @LockTimeout = 30000;

            IF @result < 0
                THROW 51000, 'Could not acquire database seed lock.', 1;
            """,
            cancellationToken);

        const string categoryName = "General";
        const string supplierCode = "SUP-001";
        const string mainWarehouseCode = "WH-001";
        const string secondaryWarehouseCode = "WH-002";
        const string productSku = "SKU-001";
        const string demoReceiptNumber = "PR-DEMO-001";

        var category = await dbContext.Categories
            .SingleOrDefaultAsync(x => x.Name == categoryName, cancellationToken);
        if (category is null)
        {
            category = new Category(categoryName);
            dbContext.Categories.Add(category);
        }

        var supplier = await dbContext.Suppliers
            .SingleOrDefaultAsync(x => x.Code == supplierCode, cancellationToken);
        if (supplier is null)
        {
            supplier = new Supplier(supplierCode, "Default Supplier");
            dbContext.Suppliers.Add(supplier);
        }

        var mainWarehouse = await dbContext.Warehouses
            .SingleOrDefaultAsync(x => x.Code == mainWarehouseCode, cancellationToken);
        if (mainWarehouse is null)
        {
            mainWarehouse = new Warehouse(mainWarehouseCode, "Main Warehouse", "Main Branch");
            dbContext.Warehouses.Add(mainWarehouse);
        }

        var secondaryWarehouse = await dbContext.Warehouses
            .SingleOrDefaultAsync(x => x.Code == secondaryWarehouseCode, cancellationToken);
        if (secondaryWarehouse is null)
        {
            secondaryWarehouse = new Warehouse(secondaryWarehouseCode, "Secondary Warehouse", "Secondary Branch");
            dbContext.Warehouses.Add(secondaryWarehouse);
        }

        var product = await dbContext.Products
            .SingleOrDefaultAsync(x => x.Sku == productSku, cancellationToken);
        if (product is null)
        {
            product = new Product(productSku, "Sample Product", category.Id, "Piece", 10);
            dbContext.Products.Add(product);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var hasDemoReceipt = await dbContext.PurchaseReceipts
            .AnyAsync(x => x.ReceiptNumber == demoReceiptNumber, cancellationToken);
        if (!hasDemoReceipt)
        {
            var receivedAt = new DateTimeOffset(2026, 7, 9, 9, 0, 0, TimeSpan.Zero);
            var receipt = new PurchaseReceipt(demoReceiptNumber, supplier.Id, mainWarehouse.Id, receivedAt);
            var line = receipt.AddLine(product.Id, 50, 25);
            receipt.Post();

            var lot = new InventoryLot(
                $"{demoReceiptNumber}-0001",
                product.Id,
                supplier.Id,
                line.Id,
                receivedAt,
                line.Quantity,
                line.UnitCost);
            var balance = new InventoryBalance(mainWarehouse.Id, lot, line.Quantity);
            var movement = new InventoryMovement(
                mainWarehouse.Id,
                product.Id,
                lot.Id,
                InventoryMovementType.PurchaseReceipt,
                line.Quantity,
                line.UnitCost,
                receivedAt,
                nameof(PurchaseReceipt),
                receipt.Id,
                receipt.Id);

            dbContext.AddRange(receipt, lot, balance, movement);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
    }

    private async Task<IAsyncDisposable> AcquireMigrationLockAsync(CancellationToken cancellationToken)
    {
        var connectionString = dbContext.Database.GetConnectionString()
            ?? throw new InvalidOperationException("Database connection string is not configured.");
        var builder = new SqlConnectionStringBuilder(connectionString)
        {
            InitialCatalog = "master"
        };

        var connection = new SqlConnection(builder.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            DECLARE @result int;
            EXEC @result = sp_getapplock
                @Resource = 'StockTrace_DatabaseInitializer_Migrate',
                @LockOwner = 'Session',
                @LockMode = 'Exclusive',
                @LockTimeout = 30000;

            IF @result < 0
                THROW 51001, 'Could not acquire database migration lock.', 1;
            """;
        await command.ExecuteNonQueryAsync(cancellationToken);

        return new SqlApplicationLock(connection);
    }

    private sealed class SqlApplicationLock(SqlConnection connection) : IAsyncDisposable
    {
        public async ValueTask DisposeAsync()
        {
            await using var command = connection.CreateCommand();
            command.CommandText =
                """
                EXEC sp_releaseapplock
                    @Resource = 'StockTrace_DatabaseInitializer_Migrate',
                    @LockOwner = 'Session';
                """;
            await command.ExecuteNonQueryAsync();
            await connection.DisposeAsync();
        }
    }
}
