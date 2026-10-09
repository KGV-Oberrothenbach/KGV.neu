export type WorkAssignment = {
  id: number;
  titel: string | null;
  beschreibung: string | null;
  datum: string;
  start_uhrzeit: string | null;
  end_uhrzeit: string | null;
  treffpunkt: string | null;
  max_teilnehmer: number | string | null;
  stunden_wert: number;
  sichtbar_ab: string | null;
  sichtbar_bis: string | null;
  anmeldung_bis: string | null;
  aktiv: boolean;
};

export type WorkAssignmentRegistration = {
  id: number;
  arbeitseinsatz_id: number;
  mitglied_id: number;
  status: "angemeldet" | "abgesagt" | "teilgenommen" | "nicht_erschienen";
  bemerkung: string | null;
  angemeldet_am: string;
  updated_at: string;
};

export type WorkAssignmentMember = {
  id: number;
  vorname: string | null;
  name: string | null;
};
