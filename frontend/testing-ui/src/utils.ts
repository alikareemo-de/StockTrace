export function nowIso(): string {
  return new Date().toISOString();
}

export function uniqueCode(prefix: string): string {
  const stamp = new Date().toISOString().replace(/\D/g, '').slice(0, 14);
  return `${prefix}-${stamp}`;
}
