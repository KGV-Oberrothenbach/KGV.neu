export type Appointment = {
  id: number;
  titel: string | null;
  beschreibung: string | null;
  datum: string;
  start_uhrzeit: string | null;
  end_uhrzeit: string | null;
  sichtbar_ab: string | null;
  sichtbar_bis: string | null;
  aktiv: boolean;
};

export type AppointmentDraft = Partial<Appointment>;
