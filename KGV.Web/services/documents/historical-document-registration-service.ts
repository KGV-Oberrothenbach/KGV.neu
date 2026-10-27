import type { ClubContext } from "../../models/auth/club";
import type { BrowserSession } from "../../lib/supabase-auth";
import type { HistoricalDocumentRegistrationInput, PendingHistoricalDocumentRegistration } from "../../models/documents/document-type";
import { registerHistoricalDocument } from "../../repositories/documents/historical-document-registration-repository";

const storageKey = (memberId: number) => `kgv.altimport.pending.member.${memberId}`;

export function loadPendingHistoricalDocumentRegistration(memberId: number): PendingHistoricalDocumentRegistration | null {
  if (typeof window === "undefined") return null;
  try {
    const value = JSON.parse(window.sessionStorage.getItem(storageKey(memberId)) ?? "null") as PendingHistoricalDocumentRegistration | null;
    return value?.memberId === memberId && Number.isInteger(value.documentId) ? value : null;
  } catch { return null; }
}

export function savePendingHistoricalDocumentRegistration(value: PendingHistoricalDocumentRegistration) {
  if (typeof window !== "undefined") window.sessionStorage.setItem(storageKey(value.memberId), JSON.stringify(value));
}

export function clearPendingHistoricalDocumentRegistration(memberId: number) {
  if (typeof window !== "undefined") window.sessionStorage.removeItem(storageKey(memberId));
}

export async function registerHistoricalDocumentForMember(club: ClubContext, session: BrowserSession, memberId: number, input: HistoricalDocumentRegistrationInput) {
  try {
    const rows = await registerHistoricalDocument(club, session, input);
    return rows[0] ?? null;
  } catch (cause) {
    throw new Error(registrationErrorMessage(cause));
  }
}

function registrationErrorMessage(cause: unknown) {
  const message = cause instanceof Error ? cause.message : "";
  if (/bereits abweichend|unvollständig klassifiziert/i.test(message)) return "Dieses Dokument ist bereits anders klassifiziert und wurde nicht überschrieben.";
  if (/Pachtverhältnis|required.*Pacht|Pacht-Nachweis/i.test(message)) return "Die gewählte Pachtbeziehung passt nicht zum Dokumentdatum oder zum Mitglied.";
  if (/Wartungsvertragszuordnung|required.*Wartung|Hauptmitglied/i.test(message)) return "Die gewählte Wartungsvertragszuordnung passt nicht zum Mitglied oder Dokumentdatum.";
  if (/Dokumenttyp.*inaktiv|Dokumentdatum|Signaturstatus/i.test(message)) return "Die fachlichen Angaben sind nicht vollständig oder nicht mehr gültig. Bitte prüfen Sie die Auswahl.";
  if (/archiviert|Prepared-Dokumente/i.test(message)) return "Dieses Dokument kann nicht als historisches Dokument registriert werden.";
  return "Die Datei wurde bereits gespeichert, die fachliche Zuordnung konnte aber noch nicht abgeschlossen werden. Bitte prüfen Sie die Angaben und versuchen Sie die Zuordnung erneut.";
}
