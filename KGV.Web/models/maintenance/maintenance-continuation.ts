export type MaintenanceDecisionInitiator = "verein" | "mitglied" | "beidseitig";

export type ExpiringMaintenanceAssignment = {
  zuordnung_id: number; hauptmitglied_id: number; mitglied_name: string | null; wartungsvertrag_id: number; wartungsvertrag_titel: string;
  gueltig_ab: string; gueltig_bis: string; entscheidung: "verlaengern" | "nicht_verlaengern" | null; folgeentscheidung_id: number | null;
  folgezuordnung_id: number | null; zurueckgenommen_am: string | null; can_decide: boolean;
};

export type MaintenanceContinuationDetail = {
  vorgaenger_zuordnung_id: number; hauptmitglied_id: number; wartungsvertrag_id: number; vertragsnummer: number | null; gueltig_ab: string; gueltig_bis: string;
  folgeentscheidung_id: number | null; entscheidung: "verlaengern" | "nicht_verlaengern" | null; entscheidungsdatum: string | null;
  vorstandsbeschlussdatum: string | null; initiator: MaintenanceDecisionInitiator | null; begruendung: string | null;
  folgezuordnung_id: number | null; folge_wartungsvertrag_id: number | null; folge_wartungsvertrag_titel: string | null;
  folge_gueltig_ab: string | null; folge_gueltig_bis: string | null; kommunikationsvorgang_id: number | null; workflow_id: number | null;
  workflow_art: "erstvertrag" | "verlaengerungsmitteilung" | null; workflow_status: string | null; dokument_id: number | null;
  zurueckgenommen_am: string | null; ruecknahmebegruendung: string | null; can_decide: boolean; can_withdraw_nichtverlaengerung: boolean;
};
