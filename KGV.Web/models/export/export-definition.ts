export type ExportDefinition = {
  export_key: string;
  titel: string | null;
  beschreibung: string | null;
  quelle_typ: string | null;
  quelle_name: string | null;
  aktiv: boolean;
  standard_sortierung: string | null;
  standard_ausgabe: string | null;
  erlaubt_csv: boolean;
  erlaubt_pdf: boolean;
};
