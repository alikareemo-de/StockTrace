import { useState } from 'react';
import type { ApiResult } from '../api/client';
import { createSale, getSale } from '../api/services';
import { Field, ProductSelect, WarehouseSelect } from '../components/FormControls';
import { ResultPanel } from '../components/ResultPanel';
import { Section } from '../components/Section';
import type { MasterData } from '../models';
import { Permissions } from '../models';
import { nowIso, uniqueCode } from '../utils';

export function SalesPage({ data, permissions }: { data: MasterData; permissions: string[] }) {
  const [saleNumber, setSaleNumber] = useState(uniqueCode('SALE-UI'));
  const [warehouseId, setWarehouseId] = useState(data.warehouses[0]?.id ?? '');
  const [productId, setProductId] = useState(data.products[0]?.id ?? '');
  const [quantity, setQuantity] = useState('1');
  const [unitPrice, setUnitPrice] = useState('40');
  const [lookupId, setLookupId] = useState('');
  const [result, setResult] = useState<ApiResult<unknown> | null>(null);

  async function create() {
    const response = await createSale({
      saleNumber,
      warehouseId,
      soldAt: nowIso(),
      lines: [{ productId, quantity: Number(quantity), unitPrice: Number(unitPrice) }]
    });
    setResult(response);
    if (response.ok && typeof response.data === 'object' && response.data && 'id' in response.data) {
      setLookupId(String(response.data.id));
      setSaleNumber(uniqueCode('SALE-UI'));
    }
  }

  async function read() {
    setResult(await getSale(lookupId));
  }

  return (
    <Section title="Sales" description="Issue stock with FIFO allocations.">
      <div className="grid two">
        <div className="panel form-panel">
          <h3>Create</h3>
          <Field label="Sale Number" value={saleNumber} onChange={setSaleNumber} />
          <WarehouseSelect data={data} value={warehouseId} onChange={setWarehouseId} />
          <ProductSelect data={data} value={productId} onChange={setProductId} />
          <Field label="Quantity" type="number" value={quantity} onChange={setQuantity} />
          <Field label="Unit Price" type="number" value={unitPrice} onChange={setUnitPrice} />
          {permissions.includes(Permissions.SalesCreate) ? (
            <button onClick={create}>Create sale</button>
          ) : (
            <p>You do not have permission to create sales.</p>
          )}
        </div>
        <div className="panel form-panel">
          <h3>Read</h3>
          <Field label="Sale Id" value={lookupId} onChange={setLookupId} />
          {permissions.includes(Permissions.SalesRead) ? (
            <button onClick={read}>Get sale</button>
          ) : (
            <p>You do not have permission to read sales.</p>
          )}
        </div>
      </div>
      <ResultPanel result={result} data={data} />
    </Section>
  );
}
