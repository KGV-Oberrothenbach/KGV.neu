import { type ExportColumn } from "./export-column";
import { type ExportDefinition } from "./export-definition";
import { type ExportFilterValue } from "./export-filter";

export type ExportResult = {
  definition: ExportDefinition;
  columns: ExportColumn[];
  rows: Array<Record<string, unknown>>;
  recordCount: number;
  filters: Record<string, ExportFilterValue>;
  standardSortierung: string | null;
};
