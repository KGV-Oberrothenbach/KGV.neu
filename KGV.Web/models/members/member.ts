export type Member = {
  id: number;
  vorname: string | null;
  name: string | null;
  email: string | null;
  aktiv: boolean;
  hauptmitglied_id: number | null;
  auth_user_id?: string | null;
  geburtsdatum: string | null;
  adresse: string | null;
  plz: string | null;
  ort: string | null;
  telefon: string | null;
  handy: string | null;
  whatsapp_einwilligung: boolean;
  email_rechnung_einwilligung: boolean;
  email_info_einwilligung: boolean;
  arbeitsstunden_altersregel_typ: string | null;
  mitglied_seit: string | null;
  mitglied_ende: string | null;
  bemerkung: string | null;
};

export type MemberWorkspaceInfo = Pick<Member, "id" | "vorname" | "name">;

export type MemberSearchResult = Pick<Member, "id" | "name" | "vorname" | "email" | "aktiv"> & {
  gartenNummern: string[];
};
