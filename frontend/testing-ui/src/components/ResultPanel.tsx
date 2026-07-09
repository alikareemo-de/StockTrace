import type { ApiResult } from '../api/client';
import type { MasterData } from '../models';

type Props = {
  title?: string;
  result: ApiResult<unknown> | null;
  data?: MasterData;
};

export function ResultPanel({ title = 'Result', result, data }: Props) {
  if (!result) {
    return (
      <section className="panel muted-panel">
        <h3>{title}</h3>
        <p>No request executed yet.</p>
      </section>
    );
  }

  return (
    <section className={`panel ${result.ok ? 'success-panel' : 'error-panel'}`}>
      <h3>
        {title} <span>Status: {result.status}</span>
      </h3>
      {!result.ok && <p className="error-text">{result.error}</p>}
      <pre>{JSON.stringify(resolveReferences(result.ok ? result.data : result.details ?? result.error, data), null, 2)}</pre>
    </section>
  );
}

function resolveReferences(value: unknown, data?: MasterData): unknown {
  if (!data || value === null || typeof value !== 'object') return value;
  if (Array.isArray(value)) return value.map((item) => resolveReferences(item, data));

  const source = value as Record<string, unknown>;
  const mapped: Record<string, unknown> = {};
  for (const [key, rawValue] of Object.entries(source)) {
    mapped[key] = typeof rawValue === 'string' ? formatReference(key, rawValue, data) : resolveReferences(rawValue, data);
  }

  return mapped;
}

function formatReference(key: string, value: string, data: MasterData): string {
  const normalizedKey = key.toLowerCase();
  if (normalizedKey.endsWith('supplierid')) {
    const supplier = data.suppliers.find((item) => item.id.toLowerCase() === value.toLowerCase());
    return supplier ? `${value} - ${supplier.code} ${supplier.name}` : value;
  }

  if (normalizedKey.endsWith('warehouseid')) {
    const warehouse = data.warehouses.find((item) => item.id.toLowerCase() === value.toLowerCase());
    return warehouse ? `${value} - ${warehouse.code} ${warehouse.name}` : value;
  }

  if (normalizedKey.endsWith('productid')) {
    const product = data.products.find((item) => item.id.toLowerCase() === value.toLowerCase());
    return product ? `${value} - ${product.sku} ${product.name}` : value;
  }

  if (normalizedKey.endsWith('categoryid')) {
    const category = data.categories.find((item) => item.id.toLowerCase() === value.toLowerCase());
    return category ? `${value} - ${category.name}` : value;
  }

  return value;
}
