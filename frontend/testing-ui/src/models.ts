export type Category = {
  id: string;
  name: string;
};

export type Supplier = {
  id: string;
  code: string;
  name: string;
};

export type Warehouse = {
  id: string;
  code: string;
  name: string;
  branchName: string;
};

export type Product = {
  id: string;
  sku: string;
  name: string;
  categoryId: string;
  categoryName: string;
  unitOfMeasure: string;
  defaultLowStockThreshold: number;
  isActive: boolean;
};

export type MasterData = {
  categories: Category[];
  suppliers: Supplier[];
  warehouses: Warehouse[];
  products: Product[];
};

export type AuthUser = {
  accessToken: string;
  expiresAt: string;
  username: string;
  displayName: string;
  role: string;
  permissions: string[];
};

export const Permissions = {
  MasterDataRead: 'MasterData.Read',
  MasterDataManageThresholds: 'MasterData.ManageThresholds',
  InventoryRead: 'Inventory.Read',
  PurchaseReceiptsRead: 'PurchaseReceipts.Read',
  PurchaseReceiptsCreate: 'PurchaseReceipts.Create',
  SalesRead: 'Sales.Read',
  SalesCreate: 'Sales.Create',
  StockTransfersRead: 'StockTransfers.Read',
  StockTransfersCreate: 'StockTransfers.Create',
  ReportsRead: 'Reports.Read',
  ReportsExport: 'Reports.Export',
  RealtimeRead: 'Realtime.Read'
} as const;

export type PurchaseReceiptRequest = {
  receiptNumber: string;
  supplierId: string;
  warehouseId: string;
  receivedAt: string;
  lines: Array<{
    productId: string;
    quantity: number;
    unitCost: number;
  }>;
};

export type SaleRequest = {
  saleNumber: string;
  warehouseId: string;
  soldAt: string;
  lines: Array<{
    productId: string;
    quantity: number;
    unitPrice: number;
  }>;
};

export type StockTransferRequest = {
  transferNumber: string;
  sourceWarehouseId: string;
  destinationWarehouseId: string;
  transferredAt: string;
  lines: Array<{
    productId: string;
    quantity: number;
  }>;
};

export type InventoryAvailability = {
  warehouseId: string;
  productId: string;
  quantityOnHand: number;
  lots: Array<{
    inventoryLotId: string;
    lotNumber: string;
    supplierId: string;
    receivedAt: string;
    quantityOnHand: number;
    unitCost: number;
  }>;
};

export type StockChangedAlert = {
  warehouseId: string;
  warehouseName: string;
  productId: string;
  productSku: string;
  productName: string;
  quantityBefore: number;
  quantityAfter: number;
  occurredAt: string;
  triggeredBy: string;
};

export type LowStockAlert = {
  warehouseId: string;
  warehouseName: string;
  productId: string;
  productSku: string;
  productName: string;
  threshold: number;
  quantityOnHand: number;
  occurredAt: string;
  triggeredBy: string;
};
