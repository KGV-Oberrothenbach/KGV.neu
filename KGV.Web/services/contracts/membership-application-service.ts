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

function dateParts(value: string) {
  const match = /^(\d{4})-(\d{2})-(\d{2})$/.exec(value);
  if (!match) throw new Error("Das Eintrittsdatum ist ungültig.");
  return { year: Number(match[1]), month: Number(match[2]) };
}

function localDateValue(today: Date) {
  const month = String(today.getMonth() + 1).padStart(2, "0");
  const day = String(today.getDate()).padStart(2, "0");
  return `${today.getFullYear()}-${month}-${day}`;
}

export function calculateMembershipApplicationContribution(annualMemberFee: number, startDate: string, seasonYear: number) {
  const start = dateParts(startDate);
  const isSameYear = start.year === seasonYear;
  const contributionMonths = isSameYear ? Math.max(1, 13 - start.month) : 12;
  return { contributionMonths, proratedMemberFee: roundFee(annualMemberFee * contributionMonths / 12) };
}

function membershipApplicationStatus(document: MembershipApplicationDocument) {
  const fileName = (document.dateiname ?? "").trim().split(/[\\/]/).pop() ?? "";
  const title = (document.titel ?? "").trim();
  const current = /^.+-\d+-\d{4}-\d{2}-\d{2}-mitgliedsantrag-(signiert|unsigniert)\.pdf$/i.exec(fileName);
  const legacy = /^mitgliedsantrag-\((signiert|unsigniert)\)_\d{4}-\d{2}-\d{2}_\d{2}-\d{2}-\d{2}\.pdf$/i.exec(fileName);
  const definedTitle = /^mitgliedsantrag\s*\((signiert|unsigniert)\)$/i.exec(title);
  return (current ?? legacy ?? definedTitle)?.[1].toLocaleLowerCase("de") ?? null;
}

export function determineMembershipApplicationStatus(documents: MembershipApplicationDocument[]): MembershipApplicationStatus {
  const statuses = documents.map(membershipApplicationStatus).filter((status): status is "signiert" | "unsigniert" => status !== null);
  if (!statuses.length) return "none";
  return statuses.includes("signiert") ? "signed" : "unsigned";
}

export async function loadMembershipApplicationData(session: BrowserSession, memberId: number, today = new Date()): Promise<MembershipApplicationData> {
  const [member, seasons, documents] = await Promise.all([getMember(session, memberId), listSeasons(session), listMembershipApplicationDocuments(session, memberId)]);
  if (!member) throw new Error("Mitglied konnte nicht geladen werden.");
  const startDate = member.mitglied_seit || localDateValue(today);
  const seasonYear = today.getFullYear();
  const contributionSeason = seasons.find((item) => item.jahr === seasonYear);
  if (!contributionSeason) throw new Error(`Für die Saison ${seasonYear} fehlt der Mitgliedsbeitrag.`);
  const annualMemberFee = member.hauptmitglied_id ? contributionSeason.mitgliedsbeitrag_nebenmitglied : contributionSeason.mitgliedsbeitrag;
  if (annualMemberFee === null || annualMemberFee === undefined || annualMemberFee < 0) throw new Error(`Für die Saison ${seasonYear} fehlt ein gültiger Mitgliedsbeitrag.`);
  const startYear = dateParts(startDate).year;
  const admissionSeason = seasons.find((item) => item.jahr === startYear);
  if (!admissionSeason) throw new Error(`Für die Saison ${startYear} fehlt die Aufnahmegebühr.`);
  if (admissionSeason.aufnahmegebuehr === null || admissionSeason.aufnahmegebuehr === undefined || admissionSeason.aufnahmegebuehr < 0) throw new Error(`Für die Saison ${startYear} fehlt eine gültige Aufnahmegebühr.`);
  const contribution = calculateMembershipApplicationContribution(annualMemberFee, startDate, seasonYear);
  return {
    member,
    startDate,
    season: contributionSeason,
    annualMemberFee: roundFee(annualMemberFee),
    proratedMemberFee: contribution.proratedMemberFee,
    contributionMonths: contribution.contributionMonths,
    admissionFee: roundFee(admissionSeason.aufnahmegebuehr),
    status: determineMembershipApplicationStatus(documents),
  };
}
