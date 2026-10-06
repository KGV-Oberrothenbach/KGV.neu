import { type BrowserSession, readSupabase } from "../../lib/supabase-auth";
import { type Member } from "../../models/members/member";
import { listLeaseContractDocuments, listLegalRepresentativeRelations, listMembershipApplicationDocuments } from "../../repositories/contracts/contract-repository";
import { getSecondaryMemberByMainMemberId } from "../../repositories/members/member-repository";
import { getLeaseParcel, listLeaseParcels, listLeaseParcelsForMember, type LeaseParcel } from "../../repositories/parcels/parcel-repository";
import { listSeasons, type Season } from "../../repositories/seasons/season-repository";
import { getMember } from "../members/member-service";
import { determineFormDocumentStatus, type FormDocumentStatus } from "./form-document-status";
import { isMinorAtStart, resolveActiveLegalRepresentativeRelation } from "./legal-representative-service";

export type LeaseContractData = { member: Member; parcel: LeaseParcel; startDate: string; season: Season; pachtPerSqm: number; annualRent: number; runningYearRent: number; status: FormDocumentStatus; isEligible: boolean; eligibilityMessage: string | null; isMinor: boolean; legalRepresentative: Member | null; secondaryMember: Member | null; hasSecondaryMember: boolean };
const roundMoney = (value: number) => Math.round(value * 100) / 100;
const yearOf = (date: string) => { const match = /^(\d{4})-\d{2}-\d{2}$/.exec(date); if (!match) throw new Error("Der Vertragsbeginn ist ungültig."); return Number(match[1]); };
const monthOf = (date: string) => Number(date.slice(5, 7));
export function calculateLeaseRent(area: number, rate: number, startDate: string) { const annualRent = roundMoney(area * rate); const runningYearRent = roundMoney(annualRent * Math.max(1, Math.min(12, 13 - monthOf(startDate))) / 12); return { annualRent, runningYearRent }; }
export function isAssignmentActiveAt(assignment: { von_datum: string | null; bis_datum: string | null }, startDate: string) { if (!assignment.von_datum) return false; return assignment.von_datum <= startDate && (!assignment.bis_datum || assignment.bis_datum >= startDate); }

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
  return { member, parcel, startDate, season, pachtPerSqm, ...rents, status: determineFormDocumentStatus(leaseDocuments, "pachtvertrag"), isEligible: true, eligibilityMessage: null, isMinor, legalRepresentative, secondaryMember, hasSecondaryMember: Boolean(secondaryMember) };
}
