export type Season = {
  id: number;
  jahr: number;
  pflichtstunden_soll: number;
  euro_pro_fehlstunde: number;
  bemerkung: string | null;
  pacht_pro_qm: number | null;
  pacht_faellig_am: string | null;
  mitgliedsbeitrag: number | null;
  mitgliedsbeitrag_nebenmitglied: number | null;
  aufnahmegebuehr: number | null;
  gebuehr_bauantrag: number | null;
};
