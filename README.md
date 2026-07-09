# StockTrace Inventory Module

Production-oriented inventory module built with ASP.NET Core and Clean Architecture.

## Delivered scope

- Purchase receiving with immutable inventory lots and posted inventory movements.
- Inventory availability by warehouse and product.
- Sales issuing with FIFO lot allocation and insufficient-stock protection.
- Stock transfers between warehouses while preserving lot identity and cost.
- Inventory movement reporting with filtering, paging, Excel export, and SQL indexes for the main query paths.
- Real-time stock-change and low-stock notifications through SignalR.
- JWT authentication, role/permission authorization, and Swagger bearer-token support.
- Read-only master-data endpoints and warehouse-product low-stock threshold configuration.
- SQL Server persistence through EF Core migrations and deterministic startup seeding.
- Unit and integration tests covering the main business workflows, concurrency, reporting, transfers, and SignalR notifications.

## Solution structure

- `StockTrace.Domain`: business model and invariants; has no infrastructure dependencies.
- `StockTrace.Application`: use cases and application abstractions.
- `StockTrace.Infrastructure`: persistence and external service implementations.
- `StockTrace.Api`: HTTP API composition root.
- `StockTrace.UnitTests`: isolated business-logic tests.
- `StockTrace.IntegrationTests`: API and SQL Server integration tests.

Dependencies point inward: Domain is independent, Application depends on Domain, and Infrastructure implements Application abstractions. API is the composition root.

## Build

```powershell
dotnet restore StockTrace.sln
dotnet build StockTrace.sln --configuration Release --no-restore
dotnet test StockTrace.sln --configuration Release --no-build
```

To build and test everything, including the frontend testing UI:

```powershell
.\build-all.ps1
```

## Run locally

The API defaults to SQL Server on `localhost` and database `StockTraceDb`.

Fastest way to run both backend and frontend:

```powershell
.\start-dev.ps1
```

To stop the development processes:

```powershell
.\stop-dev.ps1
```

Manual backend run:

```powershell
dotnet run --project src/StockTrace.Api
```

The backend is self-setup by default. On first startup it creates the database, applies EF Core migrations, and seeds required demo data when `Database:InitializeOnStartup` is `true`.

When the API starts, Swagger is available at:

```text
http://localhost:5133/swagger
```

If the port differs, use the port printed by `dotnet run`.

Testing UI:

```powershell
cd frontend/testing-ui
npm install
npm run dev
```

Open:

```text
http://127.0.0.1:5173
```

For a focused setup guide, see [docs/SETUP.md](docs/SETUP.md).

## Persistence

The application uses EF Core 9 with SQL Server. The default development connection targets the default SQL Server instance on `localhost` using Windows Authentication and can be overridden through `ConnectionStrings__DefaultConnection`.

```powershell
dotnet tool restore
dotnet tool run dotnet-ef database update --project src/StockTrace.Infrastructure --startup-project src/StockTrace.Api
dotnet run --project src/StockTrace.Api
```

Database migration and deterministic master-data/demo-data seeding run at API startup when `Database:InitializeOnStartup` is enabled. Disable this setting in environments where migrations are deployed separately.

Startup seeding creates:

- Category: `General`
- Supplier: `SUP-001` / `Default Supplier`
- Warehouses: `WH-001` / `Main Warehouse`, `WH-002` / `Secondary Warehouse`
- Product: `SKU-001` / `Sample Product` with a default low-stock threshold of `10`
- Demo purchase receipt: `PR-DEMO-001` with 50 units in the main warehouse

IDs are generated at seed time. For manual testing, read the IDs from the database or use the Postman guide in [docs/POSTMAN_TESTING_GUIDE.md](docs/POSTMAN_TESTING_GUIDE.md).

The seed process is idempotent and checks business keys before inserting records, so repeated application startup does not duplicate seeded data.

### Inventory model

- Every purchase receipt line creates one immutable inventory lot carrying the supplier, receipt source, receipt date, original quantity, and unit cost.
- `InventoryBalances` stores the current quantity per warehouse and lot for fast availability checks. A SQL check constraint prevents negative stock and `rowversion` supports optimistic concurrency.
- `InventoryMovements` is the append-only historical ledger. It is separate from the current balance so reporting does not reconstruct stock for every command.
- Sale and transfer allocations preserve exact lot traceability. Transfers keep the original lot identity and cost.
- Operational documents and ledger records are never soft-deleted. Soft delete applies only to category, product, supplier, and warehouse master data.
- Soft-deleted master records are filtered explicitly by master-data queries instead of a global EF filter. This intentionally keeps required historical relationships visible in inventory and audit reports.

### Important indexes

- Unique product SKU, supplier code, warehouse code, and document numbers protect business identifiers.
- `InventoryLots(ProductId, ReceivedAt, Id)` supports deterministic FIFO allocation.
- `InventoryLots(SupplierId, ProductId)` supports supplier-origin reporting.
- Unique `InventoryBalances(WarehouseId, InventoryLotId)` guarantees one balance row per lot and warehouse.
- `InventoryMovements(WarehouseId, ProductId, OccurredAt)` supports the main report filters.
- `InventoryMovements(InventoryLotId, OccurredAt)` supports lot history.
- Sale and transfer allocation indexes support tracing quantities back to their source lot.

## Authentication and authorization

All API endpoints are protected by JWT authentication except `POST /api/auth/login`.
Swagger supports bearer tokens through the `Authorize` button.

Testing users are documented in [docs/TEST_USERS.txt](docs/TEST_USERS.txt). These credentials are for local testing only. The same users are configured in `Authentication:TestUsers` inside `src/StockTrace.Api/appsettings.json`, so reviewers can adjust them without changing code.

1. Call `POST /api/auth/login` with a username and password.
2. Copy the returned `accessToken`.
3. Send protected requests with `Authorization: Bearer <accessToken>`.

Authorization is permission-based. Roles are only a convenient grouping for permissions.

## API summary

| Method | Endpoint | Purpose |
| --- | --- | --- |
| `POST` | `/api/auth/login` | Login and receive a JWT access token. |
| `GET` | `/api/auth/me` | Read current authenticated user claims. |
| `GET` | `/api/master-data/categories` | Read active categories. |
| `GET` | `/api/master-data/suppliers` | Read active suppliers. |
| `GET` | `/api/master-data/warehouses` | Read active warehouses. |
| `GET` | `/api/master-data/products` | Read active products. |
| `PUT` | `/api/master-data/warehouses/{warehouseId}/products/{productId}/low-stock-threshold` | Configure a warehouse-specific low-stock threshold. |
| `POST` | `/api/purchase-receipts` | Receive purchased stock into a warehouse. |
| `GET` | `/api/purchase-receipts/{id}` | Read a purchase receipt. |
| `GET` | `/api/inventory/availability` | Read stock availability by warehouse and product. |
| `POST` | `/api/sales` | Issue stock using FIFO allocation. |
| `GET` | `/api/sales/{id}` | Read a sale and its lot allocations. |
| `POST` | `/api/stock-transfers` | Transfer stock between warehouses. |
| `GET` | `/api/stock-transfers/{id}` | Read a transfer and its lot allocations. |
| `GET` | `/api/reports/inventory-movements` | Read paged inventory movement history. |
| `GET` | `/api/reports/inventory-movements/export` | Export inventory movement history to Excel. |
| SignalR | `/hubs/low-stock` | Subscribe to `StockChanged` and `LowStockReached` notifications. |

## Business rules

- Document numbers are unique per document type.
- Product lines must be positive quantities and cannot duplicate the same product within a sale or transfer.
- Unit cost and unit price cannot be negative.
- Sales and transfers fail with `409 Conflict` when available stock is not enough.
- FIFO allocation is deterministic by lot received date, then lot ID.
- Warehouse-specific low-stock thresholds override the product default threshold.
- Purchase receipts, sales, transfers, lots, balances, and movements are never soft-deleted.
- Master data uses soft delete semantics, while historical documents keep their relationships visible for audit and reporting.

## Manual testing

Use [docs/POSTMAN_TESTING_GUIDE.md](docs/POSTMAN_TESTING_GUIDE.md) for a full Postman sequence that starts with seeded master data, receives stock, sells stock, transfers stock, checks availability, and verifies inventory movement reporting.

## Configuration

Backend settings:

- `ConnectionStrings:DefaultConnection`
- `Database:InitializeOnStartup`
- `Cors:AllowedOrigins`
- `Jwt`
- `Authentication:TestUsers`

Frontend settings:

- `frontend/testing-ui/.env.development`
- `frontend/testing-ui/.env.example`
- `VITE_API_BASE_URL`
- `VITE_DEV_SERVER_PORT`

## Assumptions and trade-offs

- Authentication is implemented with local deterministic test users rather than ASP.NET Core Identity to keep the hiring-task scope focused and lightweight.
- Master-data mutation is intentionally limited to low-stock threshold configuration, which is required for real-time low-stock alerts.
- The current implementation uses SQL Server row-level update locks for stock allocation paths to prevent overselling under concurrent requests.
- Real-time notifications are published after successful stock operations so failed stock operations do not emit misleading alerts.
