export type CommunicationStatus = "entwurf" | "vorbereitet" | "zur_signatur" | "letzte_pruefung" | "mail_bereit" | "wird_versendet" | "druck_bereit" | "erledigt" | "fehler" | "abgebrochen";
export type CommunicationType = "vertrag_signatur" | "mail" | "druck";

export type CommunicationItem = {
  id: number; vorgangstyp: CommunicationType; dokumenttyp: string; status: CommunicationStatus;
  mitglied_id: number | null; parzelle_id: number | null; revision: number; signatur_revision: number | null; signatur_gestartet_am: string | null;
  empfaenger_email: string | null; betreff: string | null; mailtext: string | null; versendet_am: string | null; fehlertext: string | null;
  gedruckt_am: string | null; archiviert_am: string | null; zugewiesen_an: string | null;
  erstellt_am: string; erstellt_von: string | null; bearbeitet_am: string; bearbeitet_von: string | null;
  mitglied: { id: number; vorname: string | null; name: string | null } | null;
  parzelle: { id: number; garten_nr: string | null } | null;
};

export type CommunicationArea = "signature" | "mail" | "print" | "done";
export type AssignmentFilter = "mine" | "unassigned" | "all";
