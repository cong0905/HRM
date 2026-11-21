/** Presentation only: keep decimal-string precision, removing insignificant trailing zeros. */
export function displayDecimal(value: string): string {
  return value.includes(".") ? value.replace(/0+$/, "").replace(/\.$/, "") : value;
}
