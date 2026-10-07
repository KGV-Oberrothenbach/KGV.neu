import { createDocumentOpenUrl, openDriveDocument, type BrowserSession, readSupabase } from "../../lib/supabase-auth";
import { type Member } from "../../models/members/member";
import { listLeaseContractDocuments, listLegalRepresentativeRelations, listMembershipApplicationDocuments, type LeaseContractDocument } from "../../repositories/contracts/contract-repository";
import { getSecondaryMemberByMainMemberId } from "../../repositories/members/member-repository";
import { getLeaseParcel, listLeaseParcels, listLeaseParcelsForMember, type LeaseParcel } from "../../repositories/parcels/parcel-repository";
import { listSeasons, type Season } from "../../repositories/seasons/season-repository";
import { getMember } from "../members/member-service";
import { determineFormDocumentStatus, getFormDocumentStatus, type FormDocumentStatus } from "./form-document-status";
import { isMinorAtStart, resolveActiveLegalRepresentativeRelation } from "./legal-representative-service";

export type LeaseContractData = { member: Member; parcel: LeaseParcel; startDate: string; season: Season; pachtPerSqm: number; annualRent: number; runningYearRent: number; status: FormDocumentStatus; existingDocument: LeaseContractDocument | null; isEligible: boolean; eligibilityMessage: string | null; isMinor: boolean; legalRepresentative: Member | null; secondaryMember: Member | null; hasSecondaryMember: boolean };
export type LeaseContractDraft = { startDate: string; parcelId: number | null; hasPreviousContract: boolean | null; previousContractDate: string | null; includeSecondaryMember: boolean };
export type PreparedLeaseContractRequest = { member_id: number; parcel_id: number; start_date: string; has_previous_contract: boolean; previous_contract_date?: string; include_secondary_member: boolean };
export type LeaseContractDocumentState = { status: FormDocumentStatus; existingDocument: LeaseContractDocument | null };
const roundMoney = (value: number) => Math.round(value * 100) / 100;
const yearOf = (date: string) => { const match = /^(\d{4})-\d{2}-\d{2}$/.exec(date); if (!match) throw new Error("Der Vertragsbeginn ist ungültig."); return Number(match[1]); };
const monthOf = (date: string) => Number(date.slice(5, 7));
export function calculateLeaseRent(area: number, rate: number, startDate: string) { const annualRent = roundMoney(area * rate); const runningYearRent = roundMoney(annualRent * Math.max(1, Math.min(12, 13 - monthOf(startDate))) / 12); return { annualRent, runningYearRent }; }
export function isAssignmentActiveAt(assignment: { von_datum: string | null; bis_datum: string | null }, startDate: string) { if (!assignment.von_datum) return false; return assignment.von_datum <= startDate && (!assignment.bis_datum || assignment.bis_datum >= startDate); }
export function resolveLeaseContractDocument(documents: LeaseContractDocument[]) { return documents.find((document) => getFormDocumentStatus(document, "pachtvertrag") === "signiert") ?? documents.find((document) => getFormDocumentStatus(document, "pachtvertrag") === "unsigniert") ?? null; }
export async function loadLeaseContractDocumentState(session: BrowserSession, parcelId: number): Promise<LeaseContractDocumentState> { const documents = await listLeaseContractDocuments(session, parcelId); return { status: determineFormDocumentStatus(documents, "pachtvertrag"), existingDocument: resolveLeaseContractDocument(documents) }; }
export async function openLeaseContractDocument(session: BrowserSession, document: LeaseContractDocument) { if (document.drive_file_id) return openDriveDocument(session, document.id); if (document.bucket && document.storage_path) return createDocumentOpenUrl(session, document.bucket, document.storage_path); throw new Error("Für den Pachtvertrag fehlt eine sichere Ablage."); }
export const emptyLeaseContractDraft = (startDate: string): LeaseContractDraft => ({ startDate, parcelId: null, hasPreviousContract: null, previousContractDate: null, includeSecondaryMember: false });
export function isLocalIsoDate(value: string | null) { const match = value ? /^(\d{4})-(\d{2})-(\d{2})$/.exec(value) : null; if (!match) return false; const year = Number(match[1]); const month = Number(match[2]); const day = Number(match[3]); if (month < 1 || month > 12 || day < 1) return false; const calendarDate = new Date(year, month - 1, day); return calendarDate.getFullYear() === year && calendarDate.getMonth() === month - 1 && calendarDate.getDate() === day; }
export function validateLeaseContractDraft(data: LeaseContractData | null, draft: LeaseContractDraft) { if (!isLocalIsoDate(draft.startDate)) throw new Error("Der Vertragsbeginn ist ungültig."); if (!draft.parcelId) throw new Error("Bitte eine Parzelle wählen."); if (!data || data.parcel.id !== draft.parcelId || data.startDate !== draft.startDate) throw new Error("Die Pachtvertragsbasis ist nicht aktuell geladen."); if (data.status !== "none") throw new Error(data.status === "signed" ? "Ein signierter Pachtvertrag ist bereits vorhanden." : "Ein unsignierter Pachtvertrag ist bereits vorhanden."); if (draft.hasPreviousContract === null) throw new Error("Bitte entscheiden Sie, ob ein Altvertrag vorliegt."); if (draft.hasPreviousContract && !isLocalIsoDate(draft.previousContractDate)) throw new Error("Bitte das Datum des Altvertrags eingeben."); if (data.isMinor && draft.includeSecondaryMember) throw new Error("Bei Minderjährigen kann kein Nebenmitglied als Pächter 2 aufgenommen werden."); if (draft.includeSecondaryMember && !data.secondaryMember) throw new Error("Für Pächter 2 ist kein Nebenmitglied vorhanden."); }
export function prepareLeaseContractRequest(data: LeaseContractData, draft: LeaseContractDraft): PreparedLeaseContractRequest { validateLeaseContractDraft(data, draft); return { member_id: data.member.id, parcel_id: draft.parcelId!, start_date: draft.startDate, has_previous_contract: draft.hasPreviousContract!, previous_contract_date: draft.hasPreviousContract ? draft.previousContractDate ?? undefined : undefined, include_secondary_member: data.isMinor ? false : draft.includeSecondaryMember }; }

export async function listEligibleLeaseParcels(session: BrowserSession, memberId: number, startDate: string) {
  const assignments = await listLeaseParcelsForMember(session, memberId);
  return listLeaseParcels(session, [...new Set(assignments.filter((item) => isAssignmentActiveAt(item, startDate)).map((item) => item.parzelle_id))]);
}

export async function loadLeaseContractData(session: BrowserSession, memberId: number, parcelId: number, startDate: string): Promise<LeaseContractData> {
  const [member, parcel, seasons, membershipDocuments, leaseDocuments, configuration] = await Promise.all([
    getMember(session, memberId), getLeaseParcel(session, parcelId), listSeasons(session), listMembershipApplicationDocuments(session, memberId), listLeaseContractDocuments(session, parcelId),
    readSupabase<{ id: number }>(session, "vereinskonfiguration", { select: "id", aktiv: "eq.true", order: "updated_at.desc", limit: "1" }),
  ]);
  if (!member) throw new Error("Mitglied wurde nicht gefunden.");
  if (member.hauptmitglied_id !== null) throw new Error("Pachtvertrag kann nur aus dem Hauptmitglied-Kontext erzeugt werden.");
  if (determineFormDocumentStatus(membershipDocuments, "mitgliedsantrag") !== "signed") throw new Error("Ein Pachtvertrag kann erst nach dem signierten Mitgliedsantrag erstellt werden.");
  if (!parcel) throw new Error("Parzelle wurde nicht gefunden.");
  const assignments = await listLeaseParcelsForMember(session, memberId);
  if (!assignments.some((assignment) => assignment.parzelle_id === parcelId && isAssignmentActiveAt(assignment, startDate))) throw new Error("Die Parzelle ist zum Vertragsbeginn nicht dem Mitglied zugeordnet.");
  const area = Number(parcel.flaeche_qm); if (!Number.isFinite(area) || area <= 0) throw new Error("Parzellenfläche fehlt oder ist ungültig.");
  const season = seasons.find((item) => item.jahr === yearOf(startDate)); if (!season) throw new Error(`Für die Saison ${yearOf(startDate)} fehlt der Pachtpreis.`);
  const pachtPerSqm = Number(season.pacht_pro_qm); if (!Number.isFinite(pachtPerSqm) || pachtPerSqm <= 0) throw new Error(`Für die Saison ${season.jahr} fehlt ein gültiger Pachtpreis pro Quadratmeter.`);
  if (!configuration[0]) throw new Error("Aktive Vereinskonfiguration fehlt für den Pachtvertrag.");
  const isMinor = isMinorAtStart(member, startDate);
  let legalRepresentative: Member | null = null;
  if (isMinor) { const relations = await listLegalRepresentativeRelations(session, memberId); const relation = resolveActiveLegalRepresentativeRelation(relations, startDate); if (!relation) throw new Error("Für dieses minderjährige Mitglied ist im signierten Mitgliedsantrag kein gesetzlicher Vertreter hinterlegt."); legalRepresentative = await getMember(session, relation.vertreter_mitglied_id); if (!legalRepresentative) throw new Error("Für dieses minderjährige Mitglied ist im signierten Mitgliedsantrag kein gesetzlicher Vertreter hinterlegt."); }
  const secondaryMember = isMinor ? null : await getSecondaryMemberByMainMemberId(session, memberId);
  const rents = calculateLeaseRent(area, pachtPerSqm, startDate);
  const existingDocument = resolveLeaseContractDocument(leaseDocuments); return { member, parcel, startDate, season, pachtPerSqm, ...rents, status: determineFormDocumentStatus(leaseDocuments, "pachtvertrag"), existingDocument, isEligible: true, eligibilityMessage: null, isMinor, legalRepresentative, secondaryMember, hasSecondaryMember: Boolean(secondaryMember) };
}
