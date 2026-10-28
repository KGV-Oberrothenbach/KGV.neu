import type { ClubContext } from "../../models/auth/club";
import { type BrowserSession } from "../../lib/supabase-auth";
import { resolveDocumentOpenUrl } from "../documents/document-open-service";
import { type Member } from "../../models/members/member";
import { listLeaseContractDocuments, listLegalRepresentativeRelations, listMembershipApplicationDocuments, type LeaseContractDocument } from "../../repositories/contracts/contract-repository";
import { getSecondaryMemberByMainMemberId, listEligibleLeaseContractParties } from "../../repositories/members/member-repository";
import { getActiveLeaseRelationshipStatus, listMemberLeaseRelationships, type ActiveLeaseRelationshipStatus } from "../../repositories/contracts/lease-relationship-repository";
import type { HistoricalLeaseRelationship } from "../../models/documents/document-type";
import { getLeaseParcel, listLeaseParcels, listLeaseParcelsForMember, type LeaseParcel } from "../../repositories/parcels/parcel-repository";
import { getSeasons } from "../seasons/season-service";
import type { Season } from "../../models/seasons/season";
import { getMember } from "../members/member-service";
import { getActiveClubConfiguration } from "../../repositories/configuration/club-configuration-repository";
import { determineFormDocumentStatus, getFormDocumentStatus, type FormDocumentStatus } from "./form-document-status";
import { isMinorAtStart, resolveActiveLegalRepresentativeRelation } from "./legal-representative-service";
import type { PreparedContractSignatorySelection } from "./prepared-contract-signatory-service";
import { calculateLeaseRent, canUseSecondLeaseTenant, isEligibleLeaseTenant } from "./lease-contract-lifecycle";
export { calculateLeaseRent } from "./lease-contract-lifecycle";

export type LeaseContractData = { member: Member; parcel: LeaseParcel; startDate: string; season: Season; pachtPerSqm: number; annualRent: number; runningYearRent: number; status: FormDocumentStatus; existingDocument: LeaseContractDocument | null; isEligible: boolean; eligibilityMessage: string | null; isMinor: boolean; legalRepresentative: Member | null; secondaryMember: Member | null; eligibleSecondaryMembers: Member[]; hasSecondaryMember: boolean; leaseRelationship: ActiveLeaseRelationshipStatus };
export type LeaseContractDraft = { startDate: string; parcelId: number | null; hasPreviousContract: boolean | null; previousContractDate: string | null; includeSecondaryMember: boolean; secondaryMemberId: number | null; effectiveDate: string | null; handoverDate: string; fixedUntil: string; gardenRulesVersion: string; gardenRulesHandedOverAt: string; additionalAgreements: string; attachment1: boolean; attachment2: boolean; attachment3: boolean; attachment4: boolean; clubSignatory: string; clubSignatorySelection: PreparedContractSignatorySelection | null };
export type PreparedLeaseContractRequest = { member_id: number; parcel_id: number; start_date: string; has_previous_contract: boolean; previous_contract_date?: string; include_secondary_member: boolean; secondary_member_id?: number; effective_date?: string; handover_date: string; additional_agreements?: string; club_signatory?: string; club_documents_snapshot?: string };
export type LeaseContractDocumentState = { status: FormDocumentStatus; existingDocument: LeaseContractDocument | null };
const yearOf = (date: string) => { const match = /^(\d{4})-\d{2}-\d{2}$/.exec(date); if (!match) throw new Error("Der Vertragsbeginn ist ungültig."); return Number(match[1]); };
export function isAssignmentActiveAt(assignment: { von_datum: string | null; bis_datum: string | null }, startDate: string) { if (!assignment.von_datum) return false; return assignment.von_datum <= startDate && (!assignment.bis_datum || assignment.bis_datum >= startDate); }
export function resolveLeaseContractDocument(documents: LeaseContractDocument[]) { return documents.find((document) => getFormDocumentStatus(document, "pachtvertrag") === "signiert") ?? documents.find((document) => getFormDocumentStatus(document, "pachtvertrag") === "unsigniert") ?? null; }
export async function loadLeaseContractDocumentState(club: ClubContext, session: BrowserSession, parcelId: number): Promise<LeaseContractDocumentState> { const documents = await listLeaseContractDocuments(club, session, parcelId); return { status: determineFormDocumentStatus(documents, "pachtvertrag"), existingDocument: resolveLeaseContractDocument(documents) }; }
export function openLeaseContractDocument(club: ClubContext, session: BrowserSession, document: LeaseContractDocument) { return resolveDocumentOpenUrl(club, session, document); }
export const emptyLeaseContractDraft = (startDate: string): LeaseContractDraft => ({ startDate, parcelId: null, hasPreviousContract: null, previousContractDate: null, includeSecondaryMember: false, secondaryMemberId: null, effectiveDate: null, handoverDate: startDate, fixedUntil: "", gardenRulesVersion: "", gardenRulesHandedOverAt: "", additionalAgreements: "", attachment1: false, attachment2: false, attachment3: false, attachment4: false, clubSignatory: "", clubSignatorySelection: null });
export function isLocalIsoDate(value: string | null) { const match = value ? /^(\d{4})-(\d{2})-(\d{2})$/.exec(value) : null; if (!match) return false; const year = Number(match[1]); const month = Number(match[2]); const day = Number(match[3]); if (month < 1 || month > 12 || day < 1) return false; const calendarDate = new Date(year, month - 1, day); return calendarDate.getFullYear() === year && calendarDate.getMonth() === month - 1 && calendarDate.getDate() === day; }
export function validateLeaseContractDraft(data: LeaseContractData | null, draft: LeaseContractDraft) { if (!isLocalIsoDate(draft.startDate)) throw new Error("Der Vertragsbeginn ist ungültig."); if (!draft.parcelId) throw new Error("Bitte eine Parzelle wählen."); if (!data || data.parcel.id !== draft.parcelId || data.startDate !== draft.startDate) throw new Error("Die Pachtvertragsbasis ist nicht aktuell geladen."); if (!isEligibleLeaseTenant(data.member)) throw new Error("Pächter 1 muss ein aktives aufgenommenes Mitglied sein."); if (draft.hasPreviousContract === null) throw new Error("Bitte entscheiden Sie, ob ein Altvertrag vorliegt."); if (draft.hasPreviousContract && !isLocalIsoDate(draft.previousContractDate)) throw new Error("Bitte das Datum des Altvertrags eingeben."); if (draft.effectiveDate !== null && !isLocalIsoDate(draft.effectiveDate)) throw new Error("Das Wirksamkeitsdatum ist ungültig."); if (data.leaseRelationship.relationshipId === null && draft.effectiveDate !== null) throw new Error("Ein Erstvertrag darf kein Wirksamkeitsdatum einer Neufassung enthalten."); if (data.leaseRelationship.relationshipId !== null && data.leaseRelationship.contractVersionCount === 0) throw new Error("Das aktive Pachtverhältnis besitzt keine Vertragsfassung."); if (data.leaseRelationship.relationshipId !== null && draft.effectiveDate === null) throw new Error("Für die Neufassung fehlt das Wirksamkeitsdatum."); if (data.isMinor && (draft.includeSecondaryMember || draft.secondaryMemberId !== null)) throw new Error("Bei Minderjährigen kann kein weiterer Pächter 2 aufgenommen werden."); if (!draft.includeSecondaryMember && draft.secondaryMemberId !== null) throw new Error("Ohne Pächter 2 darf kein zweites Mitglied gespeichert sein."); if (draft.includeSecondaryMember && (!draft.secondaryMemberId || !canUseSecondLeaseTenant(data.member.id, data.eligibleSecondaryMembers.find(member => member.id === draft.secondaryMemberId)))) throw new Error("Pächter 2 muss ein anderes aktives aufgenommenes Mitglied sein."); }
export function prepareLeaseContractRequest(data: LeaseContractData, draft: LeaseContractDraft): PreparedLeaseContractRequest { validateLeaseContractDraft(data, draft); return { member_id: data.member.id, parcel_id: draft.parcelId!, start_date: draft.startDate, has_previous_contract: draft.hasPreviousContract!, previous_contract_date: draft.hasPreviousContract ? draft.previousContractDate ?? undefined : undefined, include_secondary_member: data.isMinor ? false : draft.includeSecondaryMember, secondary_member_id: draft.includeSecondaryMember ? draft.secondaryMemberId ?? undefined : undefined, effective_date: draft.effectiveDate ?? undefined, handover_date: draft.handoverDate || draft.startDate, additional_agreements: draft.additionalAgreements || undefined, club_signatory: draft.clubSignatory || undefined }; }

export async function listEligibleLeaseParcels(club: ClubContext, session: BrowserSession, memberId: number, startDate: string) {
  const assignments = await listLeaseParcelsForMember(club, session, memberId);
  return listLeaseParcels(club, session, [...new Set(assignments.filter((item) => isAssignmentActiveAt(item, startDate)).map((item) => item.parzelle_id))]);
}

export async function loadMemberLeaseRelationships(club: ClubContext, session: BrowserSession, memberId: number): Promise<HistoricalLeaseRelationship[]> {
  const { participations, relationships, parcels } = await listMemberLeaseRelationships(club, session, memberId);
  const relationshipById = new Map(relationships.map((item) => [item.id, item]));
  const parcelById = new Map(parcels.map((item) => [item.id, item]));
  return participations.flatMap((participation) => {
    const relationship = relationshipById.get(participation.pachtverhaeltnis_id);
    if (!relationship) return [];
    const parcel = parcelById.get(relationship.parzelle_id);
    return [{
      pachtverhaeltnisId: relationship.id,
      gartenNr: parcel?.garten_nr ?? null,
      anlage: parcel?.Anlage ?? null,
      rolle: participation.rolle,
      gueltigAb: participation.gueltig_ab,
      gueltigBis: participation.gueltig_bis,
      urspruenglicherPachtbeginn: relationship.urspruenglicher_pachtbeginn,
      beendetAm: relationship.beendet_am,
    }];
  }).sort((left, right) => right.gueltigAb.localeCompare(left.gueltigAb) || right.pachtverhaeltnisId - left.pachtverhaeltnisId);
}

export async function loadLeaseContractData(club: ClubContext, session: BrowserSession, memberId: number, parcelId: number, startDate: string): Promise<LeaseContractData> {
  const [member, parcel, seasons, membershipDocuments, leaseDocuments, configuration, leaseRelationship, eligibleParties] = await Promise.all([
    getMember(club, session, memberId), getLeaseParcel(club, session, parcelId), getSeasons(club, session), listMembershipApplicationDocuments(club, session, memberId), listLeaseContractDocuments(club, session, parcelId),
    getActiveClubConfiguration(club, session),
    getActiveLeaseRelationshipStatus(club, session, parcelId),
    listEligibleLeaseContractParties(club, session),
  ]);
  if (!member) throw new Error("Mitglied wurde nicht gefunden.");
  if (member.hauptmitglied_id !== null) throw new Error("Pachtvertrag kann nur aus dem Hauptmitglied-Kontext erzeugt werden.");
  if (determineFormDocumentStatus(membershipDocuments, "mitgliedsantrag") !== "signed") throw new Error("Ein Pachtvertrag kann erst nach dem signierten Mitgliedsantrag erstellt werden.");
  if (!parcel) throw new Error("Parzelle wurde nicht gefunden.");
  const assignments = await listLeaseParcelsForMember(club, session, memberId);
  if (!assignments.some((assignment) => assignment.parzelle_id === parcelId && isAssignmentActiveAt(assignment, startDate))) throw new Error("Die Parzelle ist zum Vertragsbeginn nicht dem Mitglied zugeordnet.");
  const area = Number(parcel.flaeche_qm); if (!Number.isFinite(area) || area <= 0) throw new Error("Parzellenfläche fehlt oder ist ungültig.");
  const season = seasons.find((item) => item.jahr === yearOf(startDate)); if (!season) throw new Error(`Für die Saison ${yearOf(startDate)} fehlt der Pachtpreis.`);
  const pachtPerSqm = Number(season.pacht_pro_qm); if (!Number.isFinite(pachtPerSqm) || pachtPerSqm <= 0) throw new Error(`Für die Saison ${season.jahr} fehlt ein gültiger Pachtpreis pro Quadratmeter.`);
  if (!isLocalIsoDate(season.pacht_faellig_am)) throw new Error(`Für die Saison ${season.jahr} fehlt eine gültige Pachtfälligkeit.`);
  if (!configuration) throw new Error("Aktive Vereinskonfiguration fehlt für den Pachtvertrag.");
  const isMinor = isMinorAtStart(member, startDate);
  let legalRepresentative: Member | null = null;
  if (isMinor) { const relations = await listLegalRepresentativeRelations(club, session, memberId); const relation = resolveActiveLegalRepresentativeRelation(relations, startDate); if (!relation) throw new Error("Für dieses minderjährige Mitglied ist im signierten Mitgliedsantrag kein gesetzlicher Vertreter hinterlegt."); legalRepresentative = await getMember(club, session, relation.vertreter_mitglied_id); if (!legalRepresentative) throw new Error("Für dieses minderjährige Mitglied ist im signierten Mitgliedsantrag kein gesetzlicher Vertreter hinterlegt."); }
  const linkedSecondaryMember = isMinor ? null : await getSecondaryMemberByMainMemberId(club, session, memberId);
  const eligibleSecondaryMembers = isMinor ? [] : eligibleParties.filter(candidate => candidate.id !== memberId);
  const secondaryMember = linkedSecondaryMember && eligibleSecondaryMembers.some(candidate => candidate.id === linkedSecondaryMember.id) ? linkedSecondaryMember : null;
  const rents = calculateLeaseRent(area, pachtPerSqm, startDate);
  const existingDocument = resolveLeaseContractDocument(leaseDocuments); return { member, parcel, startDate, season, pachtPerSqm, ...rents, status: determineFormDocumentStatus(leaseDocuments, "pachtvertrag"), existingDocument, isEligible: true, eligibilityMessage: null, isMinor, legalRepresentative, secondaryMember, eligibleSecondaryMembers, hasSecondaryMember: Boolean(secondaryMember), leaseRelationship };
}
