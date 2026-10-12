export type ExportFilter = {
  export_key: string;
  filter_key: string;
  label: string | null;
  typ: string | null;
  optionen_json: unknown;
  parameter_name: string | null;
  standardwert_json: unknown;
  pflicht: boolean;
  sortierung: number;
};

export type ExportFilterValue = string | number | boolean | null;
export type ExportOption = { label: string; value: string };
