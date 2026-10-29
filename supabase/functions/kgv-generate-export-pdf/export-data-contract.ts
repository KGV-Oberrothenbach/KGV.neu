export type ExportRow = Record<string, unknown>;
export type ExportColumn = { column_key: string };

export class ExportContractError extends Error {
  readonly exportKey: string;
  readonly requestedColumns: string[];
  readonly allowedColumns: string[];
  readonly invalidColumns: string[];
  constructor(exportKey: string, requestedColumns: string[], allowedColumns: string[], invalidColumns: string[]) {
    super(`PARAM:Spalten passen nicht zum Export ${exportKey}.`);
    this.name = "ExportContractError";
    this.exportKey = exportKey;
    this.requestedColumns = requestedColumns;
    this.allowedColumns = allowedColumns;
    this.invalidColumns = invalidColumns;
  }
}

type ReportContract = { sourceName: string; requiredFields: ReadonlySet<string>; optionalFields: ReadonlySet<string> };

const reportContracts: Readonly<Record<string, ReportContract>> = {
  mitgliederliste: {
    sourceName: "rpc_export_mitgliederliste_json",
    requiredFields: new Set(["mitgliedsnummer", "mitglied"]),
    optionalFields: new Set(["id", "mitglied_id", "mitgliedsnr", "mitgliedsnummer", "mitglied", "name", "vorname", "adresse_block", "kontakt_block", "strasse_hsnr", "plz", "ort", "telefon", "handy", "email", "geburtsdatum", "mitglied_seit", "bemerkung", "gartennummern", "pachtgaerten", "wa", "re", "info", "aktiv_export", "app", "app_vorhanden"]),
  },
  arbeitsstunden_uebersicht: {
    sourceName: "rpc_export_arbeitsstunden_uebersicht_json",
    requiredFields: new Set([
      "mitglied_id",
      "mitgliedsnummer",
      "garten_nr",
      "nachname",
      "vorname",
      "jahr",
      "pflichtstunden_soll",
      "geleistete_stunden",
      "wartungsvertrag_gutschrift_stunden",
      "offene_stunden",
      "status",
    ]),
    optionalFields: new Set([
      "hat_wartungsvertrag",
      "wartungsvertraege",
      "regelgrund",
      "zeilentyp",
      "leistendes_mitglied",
      "stunden_dieses_mitglieds",
      "datum",
      "taetigkeit",
      "sortierung",
    ]),
  },
  zaehler_eichfaelligkeit: {
    sourceName: "rpc_export_zaehler_eichfaelligkeit_json",
    requiredFields: new Set(),
    optionalFields: new Set([
      "parzelle",
      "medium",
      "zaehler_id",
      "zaehlernummer",
      "rfid",
      "zaehler_status",
      "eingebaut_am",
      "eichdatum",
      "eichfaellig_am",
      "tage_bis_faellig",
      "faelligkeitsstatus",
    ]),
  },
};

export function isolateExportRows(
  exportKey: string,
  sourceName: string,
  columns: ExportColumn[],
  rows: ExportRow[],
): ExportRow[] {
  const normalizedKey = exportKey.toLowerCase();
  const contract = reportContracts[normalizedKey];
  if (contract && sourceName !== contract.sourceName) {
    throw new Error(`PARAM:Exportquelle passt nicht zum Export ${normalizedKey}.`);
  }

  const selectedKeys = columns.map((column) => column.column_key);
  const allowedFields = contract ? new Set([...contract.requiredFields, ...contract.optionalFields]) : null;
  const invalidColumns = allowedFields ? selectedKeys.filter((key) => !allowedFields.has(key)) : [];
  if (allowedFields && invalidColumns.length) throw new ExportContractError(normalizedKey, selectedKeys, [...allowedFields].sort(), invalidColumns);

  return rows.map((row) => {
    if (contract && [...contract.requiredFields].some((key) => !Object.hasOwn(row, key))) throw new Error(`PARAM:Datensatz enthält erforderliche Felder nicht für den Export ${normalizedKey}.`);

    const isolated: ExportRow = {};
    for (const key of selectedKeys) {
      if (Object.hasOwn(row, key)) isolated[key] = row[key];
    }
    return isolated;
  });
}
