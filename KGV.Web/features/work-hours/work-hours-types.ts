export type WorkHour = {
  id: number;
  mitglied_id: number;
  saison_id: number;
  datum: string;
  stunden: number;
  art_der_arbeit: string;
  status: string | null;
  freigegeben: boolean;
  genehmigt_von: number | null;
  genehmigt_am: string | null;
};

export type WorkHourHistory = {
  id: number;
  arbeitsstunde_id: number;
  aktion: string;
  begruendung: string;
  geprueft_von: number;
  geprueft_am: string;
  vorher_snapshot: WorkHour;
  nachher_snapshot: WorkHour | null;
};

/**
 * Werte aus der zentralen Datenbank-View v_pflichtstunden_uebersicht.
 * Die View ist die einzige Quelle für Pflichtstunden und Fehlbeträge.
 */
export type WorkHoursSummary = {
  hauptmitglied_id: number;
  saison_id: number;
  saison_jahr: number;
  regelgrund: string | null;
  ist_befreit: boolean;
  hat_wartungsvertrag: boolean;
  altersbefreit: boolean;
  eintritt_im_saisonjahr: boolean;
  eintritt_zweites_halbjahr: boolean;
  pflichtstunden_soll: number | null;
  geleistete_stunden: number | null;
  offene_stunden: number | null;
  euro_pro_fehlstunde: number | null;
  fehlbetrag: number | null;
};
