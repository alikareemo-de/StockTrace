import { useState } from 'react';
import type { ApiResult } from '../api/client';
import { setLowStockThreshold } from '../api/services';
import { Field, ProductSelect, WarehouseSelect } from '../components/FormControls';
import { ResultPanel } from '../components/ResultPanel';
import { Section } from '../components/Section';
import type { MasterData } from '../models';
import { Permissions } from '../models';

export function MasterDataPage({
  data,
  reload,
  permissions
}: {
  data: MasterData;
  reload: () => Promise<void>;
  permissions: string[];
}) {
  const [warehouseId, setWarehouseId] = useState(data.warehouses[0]?.id ?? '');
  const [productId, setProductId] = useState(data.products[0]?.id ?? '');
  const [threshold, setThreshold] = useState('10');
  const [result, setResult] = useState<ApiResult<unknown> | null>(null);

  async function saveThreshold() {
    const response = await setLowStockThreshold(warehouseId, productId, Number(threshold));
    setResult(response);
  }

  return (
    <Section title="Master Data" description="Read seed data and configure warehouse-product thresholds.">
      <div className="grid two">
        <div className="panel">
          <h3>Current Seed Data</h3>
          <button onClick={reload}>Reload master data</button>
          <pre>{JSON.stringify(data, null, 2)}</pre>
        </div>
        <div className="panel">
          <h3>Set Low-Stock Threshold</h3>
          <WarehouseSelect data={data} value={warehouseId} onChange={setWarehouseId} />
          <ProductSelect data={data} value={productId} onChange={setProductId} />
          {permissions.includes(Permissions.MasterDataManageThresholds) ? (
            <>
              <Field label="Threshold" type="number" value={threshold} onChange={setThreshold} />
              <button onClick={saveThreshold}>Save threshold</button>
            </>
          ) : (
            <p>You do not have permission to manage low-stock thresholds.</p>
          )}
          <ResultPanel result={result} data={data} />
        </div>
      </div>
    </Section>
  );
}
