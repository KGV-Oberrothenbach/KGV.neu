export type DocumentTypeEffect =
  | "mitgliedsantrag_nachweis"
  | "pachtvertrag_nachweis"
  | "wartungsvertrag_nachweis"
  | "pachtbezug_erforderlich"
  | "wartungsvertragsbezug_erforderlich";

export type DocumentType = {
  id: number;
  bezeichnung: string;
  aktiv: boolean;
  sortierung: number;
  wirkungen: DocumentTypeEffect[];
};

export type HistoricalLeaseRelationship = {
  pachtverhaeltnisId: number;
  gartenNr: string | null;
  anlage: string | null;
  rolle: "hauptpaechter" | "mitpaechter";
  gueltigAb: string;
  gueltigBis: string | null;
  urspruenglicherPachtbeginn: string | null;
  beendetAm: string | null;
};

export type HistoricalMaintenanceAssignment = {
  id: number;
  bezeichnung: string;
  gueltigAb: string;
  gueltigBis: string | null;
  hauptmitgliedId: number;
};

export type HistoricalDocumentRegistrationInput = {
  documentId: number;
  dokumenttypId: number;
  dokumentdatum: string;
  unterzeichnet: boolean;
  pachtverhaeltnisId: number | null;
  wartungsvertragZuordnungId: number | null;
};

export type PendingHistoricalDocumentRegistration = HistoricalDocumentRegistrationInput & {
  memberId: number;
};
