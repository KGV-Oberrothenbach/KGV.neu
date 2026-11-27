"use client";
import { useAuth, useSelectedClub } from "../auth/AuthProvider";

import { useEffect, useState } from "react";
import { type BrowserSession } from "../../lib/supabase-auth";
import { type ExportColumn } from "../../models/export/export-column";
import { type ExportDefinition } from "../../models/export/export-definition";
import { type ExportFilter, type ExportFilterValue, type ExportOption } from "../../models/export/export-filter";
import { loadExportDefinitions, loadExportMetadata } from "../../services/export/export-metadata-service";
import { executeExport } from "../../services/export/export-service";
import { authorizedExportDefinitions, chooseAuthorizedExportKey } from "../../services/export/export-access";
import type { ExportResult } from "../../models/export/export-result";
import { downloadExportCsv } from "../../services/export/export-csv-service";
import { generateExportPdf } from "../../services/export/export-pdf-service";
import { defaultExportColumnKeys, exportFileName, selectedExportColumns } from "../../services/export/export-output-service";
import { ExportColumns } from "./ExportColumns";
import { ExportResults } from "./ExportResults";
import { downloadJaeXlsx } from "../../services/export/jae-xlsx-service";
import { buildSerienbriefExportContext } from "../../services/export/serienbrief-export-service";
import type { SerienbriefExportContext } from "../../models/serienbrief/serienbrief-export-context";
import { SerienbriefStart } from "../serienbrief/SerienbriefStart";

export function ExportCenter({ session, season, canExport, canCreateSerialLetters, canExportJae }: { session: BrowserSession; season: number; canExport: boolean; canCreateSerialLetters: boolean; canExportJae: boolean }) {
  const club = useSelectedClub();
  const { fachrechteV2 } = useAuth();
  type MetadataOwner = { club: typeof club; session: BrowserSession; key: string; season: number };
  const [catalog, setCatalog] = useState<{ club: typeof club; session: BrowserSession; items: ExportDefinition[] } | null>(null);
  const [selectedKey, setSelectedKey] = useState("");
  const [metadataOwner, setMetadataOwner] = useState<MetadataOwner | null>(null);
  const [filters, setFilters] = useState<ExportFilter[]>([]);
  const [columns, setColumns] = useState<ExportColumn[]>([]);
  const [values, setValues] = useState<Record<string, ExportFilterValue>>({});
  const [options, setOptions] = useState<Record<string, ExportOption[]>>({});
  const [runResult, setResult] = useState<{ owner: MetadataOwner; value: ExportResult } | null>(null);
  const [selectedColumnKeys, setSelectedColumnKeys] = useState<string[]>([]);
  const [message, setMessage] = useState("");
  const [busy, setBusy] = useState(false);
  const [serienbriefContext, setSerienbriefContext] = useState<SerienbriefExportContext | null>(null);
  const catalogReady = catalog !== null && catalog.club === club && catalog.session === session;
  const definitions = canExport && catalogReady ? authorizedExportDefinitions(catalog.items, fachrechteV2) : [];
  const activeKey = chooseAuthorizedExportKey(definitions, selectedKey);
  const selected = definitions.find((item) => item.export_key === activeKey) ?? null;
  const metadataReady = metadataOwner !== null && activeKey !== "" && metadataOwner.key === activeKey
    && metadataOwner.club === club && metadataOwner.session === session && metadataOwner.season === season;
  const result = metadataReady && runResult !== null && runResult.owner === metadataOwner ? runResult.value : null;
  const visibleColumns = metadataReady ? selectedExportColumns(columns, selectedColumnKeys) : [];

  useEffect(() => {
    setCatalog(null);
    if (!canExport) return;
    let cancelled = false;
    loadExportDefinitions(club, session).then((items) => {
      if (!cancelled) setCatalog({ club, session, items });
    }).catch((cause: Error) => { if (!cancelled) setMessage(cause.message); });
    return () => { cancelled = true; };
  }, [session, canExport, club]);

  useEffect(() => {
    if (selectedKey !== activeKey) setSelectedKey(activeKey);
  }, [activeKey, selectedKey]);

  useEffect(() => {
    setMetadataOwner(null);
    setResult(null);
    setSerienbriefContext(null);
    setFilters([]);
    setColumns([]);
    setSelectedColumnKeys([]);
    setValues({});
    setOptions({});
    if (!canExport || !activeKey) return;
    setMessage("");
    let cancelled = false;
    const owner = { club, session, key: activeKey, season };
    loadExportMetadata(club, session, activeKey, season).then((metadata) => {
      if (cancelled) return;
      setFilters(metadata.filters);
      setColumns(metadata.columns);
      setSelectedColumnKeys(defaultExportColumnKeys(metadata.columns));
      setValues(metadata.values);
      setOptions(metadata.options);
      setMetadataOwner(owner);
    }).catch((cause: Error) => { if (!cancelled) setMessage(cause.message); });
    return () => { cancelled = true; };
  }, [season, session, activeKey, club, canExport]);

  async function run() {
    if (!canExport || !selected || !metadataReady || !metadataOwner) return;
    const owner = metadataOwner;
    setResult(null); setSerienbriefContext(null); setBusy(true); setMessage("");
    try {
      const next = await executeExport(club, session, { definition: selected, filters, columns, values });
      setResult({ owner, value: next });
      setMessage(`${next.recordCount} Datensätze geladen.`);
    } catch (cause) {
      setMessage(cause instanceof Error ? cause.message : "Export konnte nicht ausgeführt werden.");
    } finally { setBusy(false); }
  }
  if (!canExport) return <section className="data-workspace"><p className="notice">Die Fachrechte auswertung.lesen und export.ausfuehren sind erforderlich.</p></section>;
  if (metadataReady && canCreateSerialLetters && serienbriefContext) return <SerienbriefStart context={serienbriefContext} club={club} session={session} onBack={() => setSerienbriefContext(null)} />;
  return <section className="data-workspace export-center">{message && <p className="notice" role="status">{message}</p>}<div className="export-controls"><label>Exportdefinition<select value={activeKey} disabled={!definitions.length} onChange={(event) => setSelectedKey(event.target.value)}>{definitions.map((item) => <option key={item.export_key} value={item.export_key}>{item.titel ?? item.export_key}</option>)}</select></label>{catalogReady && !definitions.length && <p className="notice">Für keine verfügbare Exportdefinition liegen alle benötigten Quellfachrechte vor.</p>}{selected?.beschreibung && <p>{selected.beschreibung}</p>}<div className="export-filters">{(metadataReady ? filters : []).map((filter) => <label key={filter.filter_key}>{filter.label ?? filter.filter_key}{filter.typ === "boolean" ? <input type="checkbox" checked={Boolean(values[filter.filter_key])} onChange={(event) => setValues({ ...values, [filter.filter_key]: event.target.checked })} /> : options[filter.filter_key]?.length ? <select value={String(values[filter.filter_key] ?? "")} onChange={(event) => setValues({ ...values, [filter.filter_key]: event.target.value })}><option value="">Alle</option>{options[filter.filter_key].map((option) => <option key={option.value} value={option.value}>{option.label}</option>)}</select> : <input type={filter.wert_typ === "integer" || filter.wert_typ === "number" ? "number" : "text"} value={String(values[filter.filter_key] ?? "")} onChange={(event) => setValues({ ...values, [filter.filter_key]: event.target.value })} />}</label>)}</div><ExportColumns columns={metadataReady ? columns : []} selectedKeys={metadataReady ? selectedColumnKeys : []} onChange={setSelectedColumnKeys} /><div className="editor-actions"><button disabled={busy || !selected || !metadataReady} onClick={run}>{busy ? "Wird ausgeführt …" : "Ausführen"}</button><button className="secondary-action" disabled={busy || !canExportJae} onClick={() => { if (!canExportJae) return; setBusy(true); downloadJaeXlsx(club, session, season).catch((cause: Error) => setMessage(cause.message)).finally(() => setBusy(false)); }}>JAE-XLSX speichern</button><button className="secondary-action" disabled={!result || !selected?.erlaubt_csv || !visibleColumns.length} onClick={() => result && downloadExportCsv(exportFileName(result, "csv"), result, visibleColumns)}>Als CSV speichern</button><button className="secondary-action" disabled={busy || !selected?.erlaubt_pdf || !visibleColumns.length} onClick={() => { if (!selected || !metadataReady) return; setBusy(true); generateExportPdf(club, session, { export_key: selected.export_key, format: "pdf", parameters: Object.fromEntries(filters.map((filter) => [filter.parameter_name?.trim() || filter.filter_key, values[filter.filter_key] ?? null])), columns: visibleColumns.map((column) => column.column_key), orientation: "auto" }).catch((cause: Error) => setMessage(cause.message)).finally(() => setBusy(false)); }}>Als PDF speichern</button><button className="secondary-action" disabled={!result || busy || !selected?.serienbrief_erlaubt || !canCreateSerialLetters} onClick={() => { if (!canCreateSerialLetters || !result) return; try { setSerienbriefContext(buildSerienbriefExportContext(result)); } catch (cause) { setMessage(cause instanceof Error ? cause.message : "Der Serienbrief-Datenvertrag ist ungültig."); } }}>Serienbrief erstellen</button></div></div>{result && <ExportResults result={result} columns={visibleColumns} />}</section>;
}
