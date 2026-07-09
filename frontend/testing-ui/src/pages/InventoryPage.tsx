import { useState } from 'react';
import type { ApiResult } from '../api/client';
import { getAvailability } from '../api/services';
import { ProductSelect, WarehouseSelect } from '../components/FormControls';
import { ResultPanel } from '../components/ResultPanel';
import { Section } from '../components/Section';
import type { MasterData } from '../models';

export function InventoryPage({ data }: { data: MasterData }) {
  const [warehouseId, setWarehouseId] = useState(data.warehouses[0]?.id ?? '');
  const [productId, setProductId] = useState(data.products[0]?.id ?? '');
  const [result, setResult] = useState<ApiResult<unknown> | null>(null);

  async function submit() {
    setResult(await getAvailability(warehouseId, productId));
  }

  return (
    <Section title="Inventory Availability" description="Check stock by warehouse and product.">
      <div className="panel form-panel">
        <WarehouseSelect data={data} value={warehouseId} onChange={setWarehouseId} />
        <ProductSelect data={data} value={productId} onChange={setProductId} />
        <button onClick={submit}>Get availability</button>
      </div>
      <ResultPanel result={result} data={data} />
    </Section>
  );
}
