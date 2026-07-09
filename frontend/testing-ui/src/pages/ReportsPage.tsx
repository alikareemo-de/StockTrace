import { useState } from 'react';
import type { ApiResult } from '../api/client';
import { getInventoryMovements } from '../api/services';
import { Field, ProductSelect, Select, SupplierSelect, WarehouseSelect } from '../components/FormControls';
import { ResultPanel } from '../components/ResultPanel';
import { Section } from '../components/Section';
import type { MasterData } from '../models';
import { Permissions } from '../models';

export function ReportsPage({ data, permissions }: { data: MasterData; permissions: string[] }) {
  const [warehouseId, setWarehouseId] = useState('');
  const [supplierId, setSupplierId] = useState('');
  const [categoryId, setCategoryId] = useState('');
  const [productId, setProductId] = useState('');
  const [from, setFrom] = useState('');
  const [to, setTo] = useState('');
  const [pageNumber, setPageNumber] = useState('1');
  const [pageSize, setPageSize] = useState('20');
  const [result, setResult] = useState<ApiResult<unknown> | null>(null);

  async function runReport() {
    setResult(await getInventoryMovements(reportParams()));
  }

  function reportParams() {
    return {
      warehouseId,
      supplierId,
      categoryId,
      productId,
      from,
      to,
      pageNumber,
      pageSize
    };
  }

  return (
    <Section title="Inventory Movement Report" description="Filter movement history by warehouse, supplier, category, product, and dates.">
      <div className="panel form-panel">
        <WarehouseSelect data={data} value={warehouseId} onChange={setWarehouseId} />
        <SupplierSelect data={data} value={supplierId} onChange={setSupplierId} />
        <Select label="Category" value={categoryId} onChange={setCategoryId}>
          <option value="">All categories</option>
          {data.categories.map((category) => (
            <option value={category.id} key={category.id}>
              {category.name}
            </option>
          ))}
        </Select>
        <ProductSelect data={data} value={productId} onChange={setProductId} />
        <Field label="From ISO" value={from} onChange={setFrom} />
        <Field label="To ISO" value={to} onChange={setTo} />
        <Field label="Page Number" type="number" value={pageNumber} onChange={setPageNumber} />
        <Field label="Page Size" type="number" value={pageSize} onChange={setPageSize} />
        {permissions.includes(Permissions.ReportsRead) && <button onClick={runReport}>Run report</button>}
      </div>
      <ResultPanel result={result} data={data} />
    </Section>
  );
}
