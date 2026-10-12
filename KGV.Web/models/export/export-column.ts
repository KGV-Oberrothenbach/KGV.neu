export type ExportColumn = {
  export_key: string;
  column_key: string;
  label_kurz: string | null;
  label_lang: string | null;
  sortierung: number;
  standard_sichtbar: boolean;
  ist_sortierspalte: boolean;
};
