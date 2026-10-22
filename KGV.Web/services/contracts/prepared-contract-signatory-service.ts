import type { BrowserSession } from "../../lib/supabase-auth";
import type { ClubContext } from "../../models/auth/club";
import { listPreparedContractAuthorizedSignatories, type PreparedContractAuthorizedSignatory } from "../../repositories/contracts/prepared-contract-signature-repository";

export type PreparedContractSignatorySelection = PreparedContractAuthorizedSignatory;

export const preparedContractSignatoryLabel = (value: PreparedContractAuthorizedSignatory) => {
  const representation = value.berechtigungsart === "vertretung" ? `, Vertretung für ${value.vertretene_funktion_bezeichnung ?? "Funktion"}` : "";
  return `${value.unterzeichner_name} · ${value.vereinsfunktion_bezeichnung}${representation}`;
};

export async function loadPreparedContractSignatories(club: ClubContext, session: BrowserSession, date: string) {
  return listPreparedContractAuthorizedSignatories(club, session, date);
}
