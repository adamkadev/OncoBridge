export function asNumber(value: number | string): number {
  return typeof value === 'number' ? value : Number(value);
}

export function compareOrdinal(left: string, right: string): number {
  return left < right ? -1 : left > right ? 1 : 0;
}
