import { useState } from 'react';
import type { ApiResult } from '../api/client';
import { createPurchaseReceipt, getPurchaseReceipt } from '../api/services';
import { Field, ProductSelect, SupplierSelect, WarehouseSelect } from '../components/FormControls';
import { ResultPanel } from '../components/ResultPanel';
import { Section } from '../components/Section';
import type { MasterData } from '../models';
import { Permissions } from '../models';
import { nowIso, uniqueCode } from '../utils';

export function PurchasesPage({ data, permissions }: { data: MasterData; permissions: string[] }) {
  const [receiptNumber, setReceiptNumber] = useState(uniqueCode('PR-UI'));
  const [supplierId, setSupplierId] = useState(data.suppliers[0]?.id ?? '');
  const [warehouseId, setWarehouseId] = useState(data.warehouses[0]?.id ?? '');
  const [productId, setProductId] = useState(data.products[0]?.id ?? '');
  const [quantity, setQuantity] = useState('10');
  const [unitCost, setUnitCost] = useState('25');
  const [lookupId, setLookupId] = useState('');
  const [result, setResult] = useState<ApiResult<unknown> | null>(null);

  async function create() {
    const response = await createPurchaseReceipt({
      receiptNumber,
      supplierId,
      warehouseId,
      receivedAt: nowIso(),
      lines: [{ productId, quantity: Number(quantity), unitCost: Number(unitCost) }]
    });
    setResult(response);
    if (response.ok && typeof response.data === 'object' && response.data && 'id' in response.data) {
      setLookupId(String(response.data.id));
      setReceiptNumber(uniqueCode('PR-UI'));
    }
  }

  async function read() {
    setResult(await getPurchaseReceipt(lookupId));
  }

  return (
    <Section title="Purchase Receipts" description="Receive stock and read receipt documents.">
      <div className="grid two">
        <div className="panel form-panel">
          <h3>Create</h3>
          <Field label="Receipt Number" value={receiptNumber} onChange={setReceiptNumber} />
          <SupplierSelect data={data} value={supplierId} onChange={setSupplierId} />
          <WarehouseSelect data={data} value={warehouseId} onChange={setWarehouseId} />
          <ProductSelect data={data} value={productId} onChange={setProductId} />
          <Field label="Quantity" type="number" value={quantity} onChange={setQuantity} />
          <Field label="Unit Cost" type="number" value={unitCost} onChange={setUnitCost} />
          {permissions.includes(Permissions.PurchaseReceiptsCreate) ? (
            <button onClick={create}>Create receipt</button>
          ) : (
            <p>You do not have permission to create purchase receipts.</p>
          )}
        </div>
        <div className="panel form-panel">
          <h3>Read</h3>
          <Field label="Receipt Id" value={lookupId} onChange={setLookupId} />
          {permissions.includes(Permissions.PurchaseReceiptsRead) ? (
            <button onClick={read}>Get receipt</button>
          ) : (
            <p>You do not have permission to read purchase receipts.</p>
          )}
        </div>
      </div>
      <ResultPanel result={result} data={data} />
    </Section>
  );
}
