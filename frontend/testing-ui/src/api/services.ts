import { apiGet, apiPost, apiPut, setAccessToken } from './client';
import type {
  AuthUser,
  Category,
  InventoryAvailability,
  MasterData,
  Product,
  PurchaseReceiptRequest,
  SaleRequest,
  StockTransferRequest,
  Supplier,
  Warehouse
} from '../models';

export async function login(username: string, password: string) {
  const result = await apiPost<AuthUser>('/api/auth/login', { username, password });
  if (result.ok) {
    setAccessToken(result.data.accessToken);
    localStorage.setItem('stocktrace.user', JSON.stringify(result.data));
  }

  return result;
}

export function logout() {
  setAccessToken(null);
  localStorage.removeItem('stocktrace.user');
}

export function getStoredUser(): AuthUser | null {
  const raw = localStorage.getItem('stocktrace.user');
  if (!raw) return null;

  try {
    const user = JSON.parse(raw) as AuthUser;
    setAccessToken(user.accessToken);
    return user;
  } catch {
    logout();
    return null;
  }
}

export async function loadMasterData(): Promise<MasterData> {
  const [categories, suppliers, warehouses, products] = await Promise.all([
    unwrap(apiGet<Category[]>('/api/master-data/categories')),
    unwrap(apiGet<Supplier[]>('/api/master-data/suppliers')),
    unwrap(apiGet<Warehouse[]>('/api/master-data/warehouses')),
    unwrap(apiGet<Product[]>('/api/master-data/products'))
  ]);

  return { categories, suppliers, warehouses, products };
}

export function setLowStockThreshold(warehouseId: string, productId: string, lowStockThreshold: number) {
  return apiPut(`/api/master-data/warehouses/${warehouseId}/products/${productId}/low-stock-threshold`, {
    lowStockThreshold
  });
}

export function getAvailability(warehouseId: string, productId: string) {
  const query = new URLSearchParams({ warehouseId, productId });
  return apiGet<InventoryAvailability>(`/api/inventory/availability?${query}`);
}

export function createPurchaseReceipt(request: PurchaseReceiptRequest) {
  return apiPost('/api/purchase-receipts', request);
}

export function getPurchaseReceipt(id: string) {
  return apiGet(`/api/purchase-receipts/${id}`);
}

export function createSale(request: SaleRequest) {
  return apiPost('/api/sales', request);
}

export function getSale(id: string) {
  return apiGet(`/api/sales/${id}`);
}

export function createStockTransfer(request: StockTransferRequest) {
  return apiPost('/api/stock-transfers', request);
}

export function getStockTransfer(id: string) {
  return apiGet(`/api/stock-transfers/${id}`);
}

export function getInventoryMovements(params: Record<string, string>) {
  const query = new URLSearchParams();
  Object.entries(params).forEach(([key, value]) => {
    if (value) query.set(key, value);
  });

  return apiGet(`/api/reports/inventory-movements?${query}`);
}

async function unwrap<T>(promise: ReturnType<typeof apiGet<T>>): Promise<T> {
  const result = await promise;
  if (!result.ok) throw new Error(result.error);
  return result.data;
}
