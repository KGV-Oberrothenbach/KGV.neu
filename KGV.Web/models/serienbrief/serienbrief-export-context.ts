import type { ExportFilterValue } from "../export/export-filter";
export type SerienbriefEmpfaengerStatus = "hauptpaechter" | "legacy_eindeutig" | "nicht_ermittelbar" | "mehrdeutig";
export type SerienbriefExportFeld = { schluessel: string; anzeigename: string; gruppe: string | null; sortierung: number };
export type SerienbriefExportDatensatz = { mitgliedId: number | null; parzelleId: number | null; empfaengerStatus: SerienbriefEmpfaengerStatus; werte: Record<string, unknown> };
export type SerienbriefExportContext = { exportKey: string; exportTitel: string; filter: Record<string, ExportFilterValue>; felder: SerienbriefExportFeld[]; datensaetze: SerienbriefExportDatensatz[] };
