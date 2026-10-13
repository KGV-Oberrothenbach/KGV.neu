import type { ClubContext } from "../../models/auth/club";
import { type BrowserSession } from "../../lib/supabase-auth";
import { type ExportColumn } from "../../models/export/export-column";
import { type ExportDefinition } from "../../models/export/export-definition";
import { type ExportFilter, type ExportFilterValue, type ExportOption } from "../../models/export/export-filter";
import { getActiveExportDefinitions, getExportColumns, getExportFilters } from "../../repositories/export/export-definition-repository";
import { readExportOptions } from "../../repositories/export/export-data-repository";

export const CURRENT_SEASON_DEFAULT = "$currentSeason";
export type ExportMetadata = { filters: ExportFilter[]; columns: ExportColumn[]; values: Record<string, ExportFilterValue>; options: Record<string, ExportOption[]> };

function orderBySortierung<T extends { sortierung: number }>(items: T[]): T[] { return [...items].sort((left, right) => left.sortierung - right.sortierung); }
export const sortExportFilters = (filters: ExportFilter[]) => orderBySortierung(filters);
export const sortExportColumns = (columns: ExportColumn[]) => orderBySortierung(columns);
export function getExportParameterName(filter: ExportFilter): string { return filter.parameter_name?.trim() || filter.filter_key; }

function staticOptions(value: unknown): ExportOption[] {
  if (Array.isArray(value)) return value.map((item) => typeof item === "object" && item !== null ? { label: String((item as { label?: unknown }).label ?? (item as { value?: unknown }).value ?? ""), value: String((item as { value?: unknown }).value ?? (item as { label?: unknown }).label ?? "") } : { label: String(item), value: String(item) });
  if (typeof value === "string" && value.trim().startsWith("[")) { try { return staticOptions(JSON.parse(value)); } catch { return []; } }
  return [];
}
export function getExportOptionsSource(value: unknown): { kind: "static"; options: ExportOption[] } | { kind: "rpc"; rpcName: string } {
  const options = staticOptions(value);
  if (options.length || Array.isArray(value)) return { kind: "static", options };
  if (typeof value === "string" && value.trim().startsWith("rpc_")) return { kind: "rpc", rpcName: value.trim() };
  return { kind: "static", options: [] };
}
export async function loadExportFilterOptions(club: ClubContext, session: BrowserSession, filter: ExportFilter): Promise<ExportOption[]> {
  const source = getExportOptionsSource(filter.optionen_json);
  if (source.kind === "static") return source.options;
  const rows = await readExportOptions(club, session, source.rpcName);
  return rows.map((item) => ({ label: String(item.label ?? item.value ?? ""), value: String(item.value ?? item.label ?? "") }));
}
export function getExportDefaultValue(filter: ExportFilter, season: number): ExportFilterValue {
  if (filter.standardwert_json === CURRENT_SEASON_DEFAULT) return season;
  if (filter.standardwert_json === null || filter.standardwert_json === undefined) return null;
  if (typeof filter.standardwert_json === "string" || typeof filter.standardwert_json === "number" || typeof filter.standardwert_json === "boolean") return filter.standardwert_json;
  throw new Error(`Exportfilter „${filter.filter_key}“ besitzt einen ungültigen Standardwert.`);
}
export function getExportDefaultValues(filters: ExportFilter[], season: number): Record<string, ExportFilterValue> { return Object.fromEntries(filters.map((filter) => [filter.filter_key, getExportDefaultValue(filter, season)])); }
export function buildExportRpcPayload(filters: ExportFilter[], values: Record<string, ExportFilterValue>): Record<string, ExportFilterValue> {
  return Object.fromEntries(filters.map((filter) => {
    const raw = values[filter.filter_key] ?? null;
    const value = normalizeExportFilterValue(filter, raw);
    return [getExportParameterName(filter), value];
  }));
}
export function normalizeExportFilterValue(filter: ExportFilter, raw: ExportFilterValue): ExportFilterValue {
  if (raw === "") return null;
  if (raw === null) return null;
  if (filter.wert_typ === "boolean") {
    if (typeof raw === "boolean") return raw;
    if (raw === "true") return true;
    if (raw === "false") return false;
    throw new Error(`Exportfilter „${filter.filter_key}“ erwartet einen booleschen Wert.`);
  }
  if (filter.wert_typ === "integer") {
    const value = typeof raw === "number" ? raw : Number(raw);
    if (!Number.isInteger(value)) throw new Error(`Exportfilter „${filter.filter_key}“ erwartet eine ganze Zahl.`);
    return value;
  }
  if (filter.wert_typ === "number") {
    const value = typeof raw === "number" ? raw : Number(raw);
    if (!Number.isFinite(value)) throw new Error(`Exportfilter „${filter.filter_key}“ erwartet eine Zahl.`);
    return value;
  }
  return raw;
}
export function validateExportDefinition(definition: ExportDefinition): void {
  if (!definition.export_key.trim()) throw new Error("Exportdefinition ohne export_key.");
  if (!definition.quelle_typ?.trim() || !definition.quelle_name?.trim()) throw new Error(`Export „${definition.export_key}“ besitzt keine vollständige Quelle.`);
}
export function validateExportFilter(filter: ExportFilter): void {
  if (!filter.filter_key.trim()) throw new Error("Exportfilter ohne filter_key.");
  if (filter.pflicht && !filter.label?.trim()) throw new Error(`Pflichtfilter „${filter.filter_key}“ besitzt keine Beschriftung.`);
}
export function prepareExportDefinitions(definitions: ExportDefinition[]): ExportDefinition[] { definitions.forEach(validateExportDefinition); return definitions; }
export async function loadExportDefinitions(club: ClubContext, session: BrowserSession): Promise<ExportDefinition[]> { return prepareExportDefinitions(await getActiveExportDefinitions(club, session)); }
export async function loadExportMetadata(club: ClubContext, session: BrowserSession, exportKey: string, season: number): Promise<ExportMetadata> {
  const [filters, columns] = await Promise.all([getExportFilters(club, session, exportKey), getExportColumns(club, session, exportKey)]);
  const orderedFilters = sortExportFilters(filters); const orderedColumns = sortExportColumns(columns); orderedFilters.forEach(validateExportFilter);
  const loadedOptions = await Promise.all(orderedFilters.map(async (filter) => [filter.filter_key, await loadExportFilterOptions(club, session, filter)] as const));
  return { filters: orderedFilters, columns: orderedColumns, values: getExportDefaultValues(orderedFilters, season), options: Object.fromEntries(loadedOptions) };
}
