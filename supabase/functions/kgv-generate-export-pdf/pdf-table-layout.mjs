import { normalizePdfText } from "./pdf-text-normalization.mjs";

export function wrapTableText(value, measure, width) {
  const lines = [];
  for (const explicitLine of normalizePdfText(value, true).split("\n")) {
    let line = "";
    for (const word of explicitLine.split(/\s+/).filter(Boolean)) {
      const candidate = line ? `${line} ${word}` : word;
      if (line && measure(candidate) > width) { lines.push(line); line = word; } else line = candidate;
    }
    if (line) lines.push(line);
  }
  return lines.length ? lines : [""];
}

export function paginateTableRows(rowHeights, usableHeight) {
  const pages = [[]];
  let used = 0;
  for (const height of rowHeights) {
    if (height > usableHeight) throw new Error("PARAM:Eine Exportzeile passt nicht vollständig auf eine PDF-Seite.");
    if (used && used + height > usableHeight) { pages.push([]); used = 0; }
    pages.at(-1).push(height);
    used += height;
  }
  return pages;
}
