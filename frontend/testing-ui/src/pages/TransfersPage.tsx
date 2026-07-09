import { useState } from 'react';
import type { ApiResult } from '../api/client';
import { createStockTransfer, getStockTransfer } from '../api/services';
import { Field, ProductSelect, WarehouseSelect } from '../components/FormControls';
import { ResultPanel } from '../components/ResultPanel';
import { Section } from '../components/Section';
import type { MasterData } from '../models';
import { Permissions } from '../models';
import { nowIso, uniqueCode } from '../utils';

export function TransfersPage({ data, permissions }: { data: MasterData; permissions: string[] }) {
  const [transferNumber, setTransferNumber] = useState(uniqueCode('TR-UI'));
  const [sourceWarehouseId, setSourceWarehouseId] = useState(data.warehouses[0]?.id ?? '');
  const [destinationWarehouseId, setDestinationWarehouseId] = useState(data.warehouses[1]?.id ?? '');
  const [productId, setProductId] = useState(data.products[0]?.id ?? '');
  const [quantity, setQuantity] = useState('1');
  const [lookupId, setLookupId] = useState('');
  const [result, setResult] = useState<ApiResult<unknown> | null>(null);

  async function create() {
    const response = await createStockTransfer({
      transferNumber,
      sourceWarehouseId,
      destinationWarehouseId,
      transferredAt: nowIso(),
      lines: [{ productId, quantity: Number(quantity) }]
    });
    setResult(response);
    if (response.ok && typeof response.data === 'object' && response.data && 'id' in response.data) {
      setLookupId(String(response.data.id));
      setTransferNumber(uniqueCode('TR-UI'));
    }
  }

  async function read() {
    setResult(await getStockTransfer(lookupId));
  }

  return (
    <Section title="Stock Transfers" description="Move stock between warehouses atomically.">
      <div className="grid two">
        <div className="panel form-panel">
          <h3>Create</h3>
          <Field label="Transfer Number" value={transferNumber} onChange={setTransferNumber} />
          <WarehouseSelect label="Source Warehouse" data={data} value={sourceWarehouseId} onChange={setSourceWarehouseId} />
          <WarehouseSelect
            label="Destination Warehouse"
            data={data}
            value={destinationWarehouseId}
            onChange={setDestinationWarehouseId}
          />
          <ProductSelect data={data} value={productId} onChange={setProductId} />
          <Field label="Quantity" type="number" value={quantity} onChange={setQuantity} />
          {permissions.includes(Permissions.StockTransfersCreate) ? (
            <button onClick={create}>Create transfer</button>
          ) : (
            <p>You do not have permission to create transfers.</p>
          )}
        </div>
        <div className="panel form-panel">
          <h3>Read</h3>
          <Field label="Transfer Id" value={lookupId} onChange={setLookupId} />
          {permissions.includes(Permissions.StockTransfersRead) ? (
            <button onClick={read}>Get transfer</button>
          ) : (
            <p>You do not have permission to read transfers.</p>
          )}
        </div>
      </div>
      <ResultPanel result={result} data={data} />
    </Section>
  );
}
