# Postman Testing Guide

This guide tests the implemented inventory workflow end to end against the running API.

Base URL:

```text
http://localhost:5133
```

Common headers:

```text
Accept: application/json
Content-Type: application/json
Authorization: Bearer {{accessToken}}
```

Use `Content-Type` only for requests with a JSON body.
Use `Authorization` for every endpoint except login.

## 0. Login

Request:

```http
POST {{baseUrl}}/api/auth/login
```

Body:

```json
{
  "username": "admin",
  "password": "Admin@12345"
}
```

Expected:

```text
200 OK
```

Save the returned `accessToken` as a Postman environment variable named `accessToken`.
The full testing credentials list is available in `docs/TEST_USERS.txt`.

## 1. Get Seeded IDs

The application seeds master data at startup, but IDs are generated. Use SQL Server Management Studio, Azure Data Studio, or an equivalent SQL client to read them:

```sql
SELECT Id, Code, Name FROM Suppliers WHERE Code = 'SUP-001';
SELECT Id, Code, Name FROM Warehouses WHERE Code IN ('WH-001', 'WH-002');
SELECT Id, Sku, Name, CategoryId, DefaultLowStockThreshold FROM Products WHERE Sku = 'SKU-001';
```

You can get these values through the API:

```http
GET {{baseUrl}}/api/master-data/suppliers
GET {{baseUrl}}/api/master-data/warehouses
GET {{baseUrl}}/api/master-data/products
GET {{baseUrl}}/api/master-data/categories
```

Headers:

```text
Authorization: Bearer {{accessToken}}
```

Create these Postman environment variables:

```text
baseUrl = http://localhost:5133
supplierId = value from Suppliers.Id
mainWarehouseId = value for WH-001
secondaryWarehouseId = value for WH-002
sampleProductId = value for SKU-001
```

Optional runtime variables to fill from responses:

```text
purchaseReceiptId =
saleId =
stockTransferId =
```

## 2. Configure Low-Stock Threshold

Request:

```http
PUT {{baseUrl}}/api/master-data/warehouses/{{mainWarehouseId}}/products/{{sampleProductId}}/low-stock-threshold
```

Body:

```json
{
  "lowStockThreshold": 10
}
```

Expected:

```text
200 OK
```

Validation:

- `warehouseId` must exist.
- `productId` must exist and be active.
- `lowStockThreshold` cannot be negative.

## 3. Check Initial Availability

Request:

```http
GET {{baseUrl}}/api/inventory/availability?warehouseId={{mainWarehouseId}}&productId={{sampleProductId}}
```

Expected:

```text
200 OK
```

The response contains `warehouseId`, `productId`, `quantityOnHand`, and `lots`.

Validation:

- `warehouseId` must not be an empty GUID.
- `productId` must not be an empty GUID.

## 4. Receive Purchased Stock

Request:

```http
POST {{baseUrl}}/api/purchase-receipts
```

Body:

```json
{
  "receiptNumber": "PR-MANUAL-001",
  "supplierId": "{{supplierId}}",
  "warehouseId": "{{mainWarehouseId}}",
  "receivedAt": "2026-07-09T10:00:00Z",
  "lines": [
    {
      "productId": "{{sampleProductId}}",
      "quantity": 20,
      "unitCost": 25
    }
  ]
}
```

Expected:

```text
201 Created
```

Save the returned `id` as `purchaseReceiptId`.

Validation:

- `receiptNumber` is required and must be at most 50 characters after trimming.
- `supplierId` and `warehouseId` must exist.
- `lines` must contain at least one line.
- Each line needs a product, `quantity > 0`, and `unitCost >= 0`.
- Reusing the same `receiptNumber` returns `409 Conflict`.

## 5. Get Purchase Receipt

Request:

```http
GET {{baseUrl}}/api/purchase-receipts/{{purchaseReceiptId}}
```

Expected:

```text
200 OK
```

The response includes posted receipt lines, generated lot IDs, and lot numbers such as `PR-MANUAL-001-0001`.

## 6. Check Availability After Receipt

Request:

```http
GET {{baseUrl}}/api/inventory/availability?warehouseId={{mainWarehouseId}}&productId={{sampleProductId}}
```

Expected:

```text
200 OK
```

`quantityOnHand` should increase by `20`.

## 7. Create Sale

Request:

```http
POST {{baseUrl}}/api/sales
```

Body:

```json
{
  "saleNumber": "SALE-MANUAL-001",
  "warehouseId": "{{mainWarehouseId}}",
  "soldAt": "2026-07-09T11:00:00Z",
  "lines": [
    {
      "productId": "{{sampleProductId}}",
      "quantity": 5,
      "unitPrice": 40
    }
  ]
}
```

Expected:

```text
201 Created
```

Save the returned `id` as `saleId`.

Validation:

- `saleNumber` is required and must be at most 50 characters after trimming.
- `warehouseId` must exist.
- `lines` must contain at least one line.
- Each line needs a product, `quantity > 0`, and `unitPrice >= 0`.
- The same product cannot appear more than once in one sale.
- Reusing the same `saleNumber` returns `409 Conflict`.
- Requesting more stock than available returns `409 Conflict`.

## 8. Get Sale

Request:

```http
GET {{baseUrl}}/api/sales/{{saleId}}
```

Expected:

```text
200 OK
```

The response includes `costOfGoodsSold` and FIFO lot `allocations`.

## 9. Transfer Stock

Request:

```http
POST {{baseUrl}}/api/stock-transfers
```

Body:

```json
{
  "transferNumber": "TR-MANUAL-001",
  "sourceWarehouseId": "{{mainWarehouseId}}",
  "destinationWarehouseId": "{{secondaryWarehouseId}}",
  "transferredAt": "2026-07-09T12:00:00Z",
  "lines": [
    {
      "productId": "{{sampleProductId}}",
      "quantity": 3
    }
  ]
}
```

Expected:

```text
201 Created
```

Save the returned `id` as `stockTransferId`.

Validation:

- `transferNumber` is required and must be at most 50 characters after trimming.
- Source and destination warehouses must exist.
- Source and destination warehouses must be different.
- `lines` must contain at least one line.
- Each line needs a product and `quantity > 0`.
- The same product cannot appear more than once in one transfer.
- Reusing the same `transferNumber` returns `409 Conflict`.
- Requesting more stock than available in the source warehouse returns `409 Conflict`.

## 10. Get Stock Transfer

Request:

```http
GET {{baseUrl}}/api/stock-transfers/{{stockTransferId}}
```

Expected:

```text
200 OK
```

The response includes completed transfer details and source lot allocations.

## 11. Check Source and Destination Availability

Source warehouse:

```http
GET {{baseUrl}}/api/inventory/availability?warehouseId={{mainWarehouseId}}&productId={{sampleProductId}}
```

Destination warehouse:

```http
GET {{baseUrl}}/api/inventory/availability?warehouseId={{secondaryWarehouseId}}&productId={{sampleProductId}}
```

Expected:

```text
200 OK
```

The source decreases by `3`; the destination increases by `3`.

## 12. Inventory Movement Report

Request:

```http
GET {{baseUrl}}/api/reports/inventory-movements?warehouseId={{mainWarehouseId}}&productId={{sampleProductId}}&from=2026-07-09T00:00:00Z&to=2026-07-09T23:59:59Z&pageNumber=1&pageSize=20
```

Expected:

```text
200 OK
```

The response includes a paged list of movements such as:

```text
PurchaseReceipt
SaleIssue
TransferOut
```

Run the same report with `warehouseId={{secondaryWarehouseId}}` to see `TransferIn`.

Validation:

- `pageNumber` must be at least `1`.
- `pageSize` must be between `1` and `100`.
- `from` cannot be later than `to`.

## 13. Export Inventory Movement Report to Excel

Request:

```http
GET {{baseUrl}}/api/reports/inventory-movements/export?warehouseId={{mainWarehouseId}}&productId={{sampleProductId}}&from=2026-07-09T00:00:00Z&to=2026-07-09T23:59:59Z
```

Headers:

```text
Authorization: Bearer {{accessToken}}
```

Expected:

```text
200 OK
Content-Type: application/vnd.openxmlformats-officedocument.spreadsheetml.sheet
```

The response downloads an `.xlsx` file containing the filtered inventory movements.

## Error Response Shape

Validation errors return `400 Bad Request` with `HttpValidationProblemDetails`:

```json
{
  "title": "Validation failed.",
  "status": 400,
  "errors": {
    "lines": [
      "At least one line is required."
    ]
  }
}
```

Missing resources return `404 Not Found` with a problem title.

Business conflicts return `409 Conflict`, commonly for duplicate document numbers or insufficient stock.

## SignalR Low-Stock Notification

Hub URL:

```text
{{baseUrl}}/hubs/low-stock
```

Authentication:

```text
access_token={{accessToken}}
```

Client event:

```text
StockChanged
LowStockReached
```

`StockChanged` is published after successful stock-changing operations. `LowStockReached` is published when a product crosses from above its configured threshold to at or below it.

SignalR uses its own protocol, so it is usually easier to verify this with the integration test suite or a small SignalR client instead of a standard Postman HTTP request.
