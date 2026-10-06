import { getMember } from "../members/member-service";
import { listMembersForSearch } from "../../repositories/members/member-repository";
import { listLegalRepresentativeRelations } from "../../repositories/contracts/contract-repository";
import { type BrowserSession } from "../../lib/supabase-auth";
import { type Member } from "../../models/members/member";

export type LegalRepresentativeMode = "existing" | "manual";
export type LegalRepresentativeDraft = { mode: LegalRepresentativeMode; memberId: number | null; vorname: string; nachname: string; adresseAbweichend: boolean; adresse: string; plz: string; ort: string };
export type LegalRepresentativeOption = { id: number; label: string };
export const emptyLegalRepresentativeDraft = (): LegalRepresentativeDraft => ({ mode: "existing", memberId: null, vorname: "", nachname: "", adresseAbweichend: false, adresse: "", plz: "", ort: "" });

function localDate(value: string) { const [year, month, day] = value.split("-").map(Number); return new Date(year, month - 1, day); }
export function isMinorAtStart(member: Member, startDate: string) {
  if (!member.geburtsdatum) return false;
  const birth = localDate(member.geburtsdatum); const start = localDate(startDate);
  let age = start.getFullYear() - birth.getFullYear();
  if (birth > new Date(start.getFullYear() - age, start.getMonth(), start.getDate())) age--;
  return age < 18;
}

export function validateLegalRepresentative(draft: LegalRepresentativeDraft) {
  if (draft.mode === "existing") { if (!draft.memberId) throw new Error("Bitte ein vorhandenes Mitglied als gesetzlichen Vertreter auswählen."); return; }
  if (!draft.vorname.trim() || !draft.nachname.trim()) throw new Error("Bitte Vorname und Nachname des gesetzlichen Vertreters eingeben.");
  if (draft.adresseAbweichend && (!draft.adresse.trim() || !draft.plz.trim() || !draft.ort.trim())) throw new Error("Bitte die abweichende Anschrift des gesetzlichen Vertreters vollständig eingeben.");
}

export async function loadLegalRepresentativeContext(session: BrowserSession, member: Member, startDate: string) {
  if (!isMinorAtStart(member, startDate)) return { isMinor: false, options: [] as LegalRepresentativeOption[], draft: emptyLegalRepresentativeDraft() };
  const [members, relations] = await Promise.all([listMembersForSearch(session), listLegalRepresentativeRelations(session, member.id)]);
  const current = relations.find((relation) => relation.gueltig_ab <= startDate && (!relation.gueltig_bis || relation.gueltig_bis >= startDate));
  const options = members.filter((item) => item.id !== member.id).map((item) => ({ id: item.id, label: [item.vorname, item.name].filter(Boolean).join(" ") || `Mitglied #${item.id}` })).sort((a, b) => a.label.localeCompare(b.label, "de"));
  if (!current) return { isMinor: true, options, draft: { ...emptyLegalRepresentativeDraft(), mode: (options.length ? "existing" : "manual") as LegalRepresentativeMode } };
  const representative = await getMember(session, current.vertreter_mitglied_id);
  return { isMinor: true, options, draft: representative ? { mode: "existing" as const, memberId: representative.id, vorname: representative.vorname ?? "", nachname: representative.name ?? "", adresseAbweichend: false, adresse: "", plz: "", ort: "" } : emptyLegalRepresentativeDraft() };
}
