import { getMember } from "../members/member-service";
import { listSeasons, type Season } from "../../repositories/seasons/season-repository";
import { listMembershipApplicationDocuments, type MembershipApplicationDocument } from "../../repositories/contracts/contract-repository";
import { type BrowserSession } from "../../lib/supabase-auth";
import { type Member } from "../../models/members/member";

export type MembershipApplicationStatus = "none" | "unsigned" | "signed";

export type MembershipApplicationData = {
  member: Member;
  startDate: string;
  season: Season;
  annualMemberFee: number;
  proratedMemberFee: number;
  contributionMonths: number;
  admissionFee: number;
  status: MembershipApplicationStatus;
};

const roundFee = (value: number) => Math.round(value * 100) / 100;

export function calculateMembershipApplicationContribution(annualMemberFee: number, startDate: string, seasonYear: number) {
  const start = new Date(`${startDate}T12:00:00Z`);
  const isSameYear = start.getUTCFullYear() === seasonYear;
  const contributionMonths = isSameYear ? Math.max(1, 12 - start.getUTCMonth()) : 12;
  return { contributionMonths, proratedMemberFee: roundFee(annualMemberFee * contributionMonths / 12) };
}

export function determineMembershipApplicationStatus(documents: MembershipApplicationDocument[]): MembershipApplicationStatus {
  const applications = documents.filter((document) => /mitgliedsantrag/i.test(`${document.titel ?? ""} ${document.dateiname ?? ""}`));
  if (!applications.length) return "none";
  return applications.some((document) => /signiert/i.test(`${document.titel ?? ""} ${document.dateiname ?? ""}`) && !/unsigniert/i.test(`${document.titel ?? ""} ${document.dateiname ?? ""}`))
    ? "signed"
    : "unsigned";
}

export async function loadMembershipApplicationData(session: BrowserSession, memberId: number, today = new Date()): Promise<MembershipApplicationData> {
  const [member, seasons, documents] = await Promise.all([getMember(session, memberId), listSeasons(session), listMembershipApplicationDocuments(session, memberId)]);
  if (!member) throw new Error("Mitglied konnte nicht geladen werden.");
  const startDate = member.mitglied_seit || today.toISOString().slice(0, 10);
  const seasonYear = today.getFullYear();
  const season = seasons.find((item) => item.jahr === seasonYear);
  if (!season) throw new Error(`Für die Saison ${seasonYear} fehlt der Mitgliedsbeitrag.`);
  const annualMemberFee = member.hauptmitglied_id ? season.mitgliedsbeitrag_nebenmitglied : season.mitgliedsbeitrag;
  if (annualMemberFee === null || annualMemberFee === undefined || annualMemberFee < 0) throw new Error(`Für die Saison ${seasonYear} fehlt ein gültiger Mitgliedsbeitrag.`);
  const contribution = calculateMembershipApplicationContribution(annualMemberFee, startDate, seasonYear);
  return {
    member,
    startDate,
    season,
    annualMemberFee: roundFee(annualMemberFee),
    proratedMemberFee: contribution.proratedMemberFee,
    contributionMonths: contribution.contributionMonths,
    admissionFee: roundFee(season.aufnahmegebuehr ?? 0),
    status: determineMembershipApplicationStatus(documents),
  };
}
