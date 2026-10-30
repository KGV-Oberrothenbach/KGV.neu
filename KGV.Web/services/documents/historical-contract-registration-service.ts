import type { ClubContext } from "../../models/auth/club";
import type { BrowserSession } from "../../lib/supabase-auth";
import { registerHistoricalContract, type HistoricalContractRegistrationInput } from "../../repositories/documents/historical-contract-registration-repository";

export function historicalContractErrorMessage(cause: unknown) {
  const message = cause instanceof Error ? cause.message : "";
  if (/Berechtigung|permission|42501/i.test(message)) return "Für die Nacherfassung historischer Verträge fehlt die Berechtigung.";
  if (/Pachtverhältnis|Pachtbeziehung|beteiligt/i.test(message)) return "Eine ausgewählte Pachtbeziehung passt nicht zum Vertragsdatum oder Hauptmitglied.";
  if (/klassifiziert|Prepared/i.test(message)) return "Dieses Dokument kann nicht als historischer Vertrag übernommen werden.";
  return message || "Der Altvertrag konnte nicht vollständig übernommen werden. Es wurde kein Vertragsstand gespeichert.";
}

export async function registerHistoricalContractForWeb(club: ClubContext, session: BrowserSession, input: HistoricalContractRegistrationInput) {
  try { return await registerHistoricalContract(club, session, input); }
  catch (cause) { throw new Error(historicalContractErrorMessage(cause)); }
}
