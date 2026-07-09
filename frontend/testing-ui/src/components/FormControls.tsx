import type { MasterData } from '../models';

type SelectProps = {
  label: string;
  value: string;
  onChange: (value: string) => void;
  children: React.ReactNode;
};

export function Field({
  label,
  value,
  onChange,
  type = 'text'
}: {
  label: string;
  value: string | number;
  onChange: (value: string) => void;
  type?: string;
}) {
  return (
    <label className="field">
      <span>{label}</span>
      <input type={type} value={value} onChange={(event) => onChange(event.target.value)} />
    </label>
  );
}

export function Select({ label, value, onChange, children }: SelectProps) {
  return (
    <label className="field">
      <span>{label}</span>
      <select value={value} onChange={(event) => onChange(event.target.value)}>
        {children}
      </select>
    </label>
  );
}

export function ProductSelect({
  data,
  value,
  onChange
}: {
  data: MasterData;
  value: string;
  onChange: (value: string) => void;
}) {
  return (
    <Select label="Product" value={value} onChange={onChange}>
      <option value="">Select product</option>
      {data.products.map((product) => (
        <option value={product.id} key={product.id}>
          {product.sku} - {product.name}
        </option>
      ))}
    </Select>
  );
}

export function WarehouseSelect({
  label = 'Warehouse',
  data,
  value,
  onChange
}: {
  label?: string;
  data: MasterData;
  value: string;
  onChange: (value: string) => void;
}) {
  return (
    <Select label={label} value={value} onChange={onChange}>
      <option value="">Select warehouse</option>
      {data.warehouses.map((warehouse) => (
        <option value={warehouse.id} key={warehouse.id}>
          {warehouse.code} - {warehouse.name}
        </option>
      ))}
    </Select>
  );
}

export function SupplierSelect({
  data,
  value,
  onChange
}: {
  data: MasterData;
  value: string;
  onChange: (value: string) => void;
}) {
  return (
    <Select label="Supplier" value={value} onChange={onChange}>
      <option value="">Select supplier</option>
      {data.suppliers.map((supplier) => (
        <option value={supplier.id} key={supplier.id}>
          {supplier.code} - {supplier.name}
        </option>
      ))}
    </Select>
  );
}
