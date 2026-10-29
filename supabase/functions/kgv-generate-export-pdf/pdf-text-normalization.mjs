// Printable Unicode is deliberately preserved. Only controls that cannot be
// represented by the standard PDF fonts are normalized at the renderer boundary.
export function normalizePdfText(value, multiline) {
  const withNormalizedLineEndings = value.replace(/\r\n?|\n/g, multiline ? "\n" : " ");
  return withNormalizedLineEndings.replace(/[\u0000-\u0009\u000b\u000c\u000e-\u001f\u007f]/g, " ");
}
