# StockTrace Self-Setup Guide

This project is prepared to run after cloning with minimal manual setup.

## Requirements

- .NET 9 SDK
- SQL Server LocalDB or SQL Server reachable at `localhost`
- Node.js 20+ for the testing UI

## Quick start

From the repository root:

```powershell
.\start-dev.ps1
```

To verify the entire repository before pushing to GitHub:

```powershell
.\build-all.ps1
```

This opens the backend and frontend in separate PowerShell windows and opens the browser.

Manual startup is also possible:

```powershell
dotnet restore StockTrace.sln
dotnet run --project src\StockTrace.Api\StockTrace.Api.csproj
```

On first backend startup, the application automatically:

1. Connects to SQL Server using `ConnectionStrings:DefaultConnection`.
2. Creates the database if it does not exist.
3. Applies all EF Core migrations.
4. Seeds deterministic master data.
5. Seeds one demo purchase receipt and stock lot for immediate testing.

Swagger:

```text
http://localhost:5133/swagger
```

Run the testing UI:

```powershell
cd frontend\testing-ui
npm install
npm run dev
```

Frontend:

```text
http://127.0.0.1:5173
```

## Configuration files

Backend:

- `src/StockTrace.Api/appsettings.json`
- `src/StockTrace.Api/appsettings.Development.json`

Important backend settings:

- `ConnectionStrings:DefaultConnection`
- `Database:InitializeOnStartup`
- `Cors:AllowedOrigins`
- `Jwt`
- `Authentication:TestUsers`

Frontend:

- `frontend/testing-ui/.env.development`
- `frontend/testing-ui/.env.example`

Important frontend settings:

- `VITE_API_BASE_URL`
- `VITE_DEV_SERVER_PORT`

## Default seeded data

Master data:

- Category: `General`
- Supplier: `SUP-001` / `Default Supplier`
- Warehouses:
  - `WH-001` / `Main Warehouse`
  - `WH-002` / `Secondary Warehouse`
- Product: `SKU-001` / `Sample Product`
- Demo receipt: `PR-DEMO-001`, 50 pieces into `WH-001`

Test users:

See `docs/TEST_USERS.txt`.

Default admin:

```text
Username: admin
Password: Admin@12345
```

## Self-setup behavior

`Database:InitializeOnStartup` controls startup migration and seeding.

```json
{
  "Database": {
    "InitializeOnStartup": true
  }
}
```

Keep it enabled for local review and hiring-task evaluation. In production-like environments, it can be disabled and migrations can be applied by the deployment pipeline.

The seed process is idempotent. It checks existing business keys such as SKU, supplier code, warehouse code, and receipt number before inserting data.

## Excel export

Login with a user that has `Reports.Export`, such as `admin` or `reporter`, then use:

```http
GET /api/reports/inventory-movements/export
```

The testing UI also provides an `Export Excel` button on the Reports page.

## GitHub readiness

The repository is configured to ignore:

- `bin/`
- `obj/`
- `node_modules/`
- frontend `dist/`
- local `.env`
- SQL Server database files

`.env.example` and `.env.development` are intentionally included for easy local startup.
