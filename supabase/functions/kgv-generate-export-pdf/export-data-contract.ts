export type ExportRow = Record<string, unknown>;
export type ExportColumn = { column_key: string };

type ReportContract = {
  sourceName: string;
  fields: ReadonlySet<string>;
};

const reportContracts: Readonly<Record<string, ReportContract>> = {
  arbeitsstunden_uebersicht: {
    sourceName: "rpc_export_arbeitsstunden_uebersicht_json",
    fields: new Set([
      "mitglied_id",
      "garten_nr",
      "nachname",
      "vorname",
      "jahr",
      "pflichtstunden_soll",
      "geleistete_stunden",
      "wartungsvertrag_gutschrift_stunden",
      "offene_stunden",
      "hat_wartungsvertrag",
      "wartungsvertraege",
      "status",
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
    fields: new Set([
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
  if (contract && selectedKeys.some((key) => !contract.fields.has(key))) {
    throw new Error(`PARAM:Spalten passen nicht zum Export ${normalizedKey}.`);
  }

  return rows.map((row) => {
    if (contract && Object.keys(row).some((key) => !contract.fields.has(key))) {
      throw new Error(`PARAM:Datensatz enthält fremde Felder für den Export ${normalizedKey}.`);
    }

    const isolated: ExportRow = {};
    for (const key of selectedKeys) {
      if (Object.hasOwn(row, key)) isolated[key] = row[key];
    }
    return isolated;
  });
}
