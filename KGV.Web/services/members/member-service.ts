import { type BrowserSession } from "../../lib/supabase-auth";
import { type Member, type MemberSearchResult } from "../../models/members/member";
import { createMember, endMembership as endMembershipRpc, getMemberById, getMemberWorkspaceInfo, getSecondaryMemberByMainMemberId, listMembersForSearch, type MembershipEndDecision, updateMemberStammdaten } from "../../repositories/members/member-repository";
import { listCurrentGardenNumbersByMemberId } from "../parcels/parcel-service";

export async function loadMemberSearchResults(session: BrowserSession): Promise<MemberSearchResult[]> {
  const members = await listMembersForSearch(session);
  const gardenNumbersByMemberId = await listCurrentGardenNumbersByMemberId(session, members.map((member) => member.id));

  return members.map((member) => ({ ...member, gartenNummern: gardenNumbersByMemberId.get(member.id) ?? [] }));
}

export function filterMemberSearchResults(members: MemberSearchResult[], options: { query: string; includeInactive: boolean }) {
  const normalizedQuery = options.query.trim().toLocaleLowerCase("de");

  return members
    .filter((member) => options.includeInactive || member.aktiv)
    .filter((member) => !normalizedQuery || [member.name, member.vorname, member.email, String(member.id), member.gartenNummern.join(" ")]
      .filter(Boolean)
      .join(" ")
      .toLocaleLowerCase("de")
      .includes(normalizedQuery));
}

export const loadMemberWorkspaceInfo = getMemberWorkspaceInfo;

export type MemberEditPermissions = {
  actorMemberId: number | null;
  canEditOwnMember: boolean;
  canEditAllMembers: boolean;
};

export type MemberStammdatenInput = Pick<Member, "vorname" | "name" | "email" | "geburtsdatum" | "arbeitsstunden_altersregel_typ" | "adresse" | "plz" | "ort" | "telefon" | "handy" | "whatsapp_einwilligung" | "email_rechnung_einwilligung" | "email_info_einwilligung" | "mitglied_seit" | "bemerkung">;

export type MemberCreatePermissions = { canCreateMember: boolean };

const validArbeitsstundenAltersregelTypen = new Set(["mann80", "frau75"]);
const emailPattern = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

export function canEditMember(memberId: number, permissions: MemberEditPermissions) {
  return permissions.canEditAllMembers || (permissions.canEditOwnMember && permissions.actorMemberId === memberId);
}

export const getMember = getMemberById;
export const loadSecondaryMemberForMembershipEnd = getSecondaryMemberByMainMemberId;

export async function endMembership(session: BrowserSession, mainMemberId: number, decision: MembershipEndDecision | null) {
  if (!Number.isInteger(mainMemberId) || mainMemberId <= 0) throw new Error("Hauptmitglied ist ungültig.");

  const result = await endMembershipRpc(session, mainMemberId, decision);
  if (!result) throw new Error("Die Mitgliedschaft konnte nicht beendet werden.");
  if (!result.success) throw new Error(result.message || "Die Mitgliedschaft konnte nicht beendet werden.");
  if (!result.updated_main_member) throw new Error("Die aktualisierten Mitgliedsdaten fehlen.");

  return result;
}

export async function createMainMember(session: BrowserSession, input: MemberStammdatenInput, permissions: MemberCreatePermissions) {
  if (!permissions.canCreateMember) throw new Error("Für neue Mitglieder besteht keine Berechtigung.");
  const vorname = input.vorname?.trim() ?? "";
  const name = input.name?.trim() ?? "";
  if (!vorname) throw new Error("Vorname ist erforderlich.");
  if (!name) throw new Error("Nachname ist erforderlich.");
  if (!validArbeitsstundenAltersregelTypen.has(input.arbeitsstunden_altersregel_typ ?? "")) {
    throw new Error("Für Hauptmitglieder ist eine gültige Arbeitsstunden-Altersregel erforderlich.");
  }

  const created = await createMember(session, {
    hauptmitglied_id: null,
    vorname,
    name,
    email: input.email?.trim() || null,
    geburtsdatum: input.geburtsdatum || null,
    arbeitsstunden_altersregel_typ: input.arbeitsstunden_altersregel_typ,
    adresse: input.adresse?.trim() || null,
    plz: input.plz?.trim() || null,
    ort: input.ort?.trim() || null,
    telefon: input.telefon?.trim() || null,
    handy: input.handy?.trim() || null,
    whatsapp_einwilligung: Boolean(input.whatsapp_einwilligung),
    email_rechnung_einwilligung: Boolean(input.email_rechnung_einwilligung),
    email_info_einwilligung: Boolean(input.email_info_einwilligung),
    mitglied_seit: input.mitglied_seit || null,
    mitglied_ende: null,
    bemerkung: input.bemerkung?.trim() || null,
    aktiv: true,
  });
  if (!created[0]) throw new Error("Mitglied konnte nicht angelegt werden.");
  return created[0];
}

export async function updateExistingMember(session: BrowserSession, memberId: number, input: MemberStammdatenInput, permissions: MemberEditPermissions) {
  const current = await getMemberById(session, memberId);
  if (!current) throw new Error("Das Mitglied konnte nicht geladen werden.");
  if (!canEditMember(memberId, permissions)) throw new Error("Für dieses Mitglied besteht keine Bearbeitungsberechtigung.");
  const vorname = input.vorname?.trim() ?? "";
  const name = input.name?.trim() ?? "";
  if (!vorname) throw new Error("Vorname ist erforderlich.");
  if (!name) throw new Error("Nachname ist erforderlich.");
  if (current.hauptmitglied_id === null && !validArbeitsstundenAltersregelTypen.has(input.arbeitsstunden_altersregel_typ ?? "")) {
    throw new Error("Für Hauptmitglieder ist eine gültige Arbeitsstunden-Altersregel erforderlich.");
  }

  const requestedEmail = input.email?.trim() || null;
  const emailChanged = requestedEmail?.toLocaleLowerCase("de") !== current.email?.toLocaleLowerCase("de");
  if (current.auth_user_id === null && permissions.canEditAllMembers && emailChanged && (!requestedEmail || !emailPattern.test(requestedEmail))) {
    throw new Error("Bitte eine gültige E-Mail-Adresse angeben.");
  }

  const values = {
    vorname,
    name,
    email: current.auth_user_id === null && permissions.canEditAllMembers ? requestedEmail : current.email,
    geburtsdatum: input.geburtsdatum || null,
    arbeitsstunden_altersregel_typ: current.hauptmitglied_id === null ? input.arbeitsstunden_altersregel_typ : current.arbeitsstunden_altersregel_typ,
    adresse: input.adresse?.trim() || null,
    plz: input.plz?.trim() || null,
    ort: input.ort?.trim() || null,
    telefon: input.telefon?.trim() || null,
    handy: input.handy?.trim() || null,
    whatsapp_einwilligung: Boolean(input.whatsapp_einwilligung),
    email_rechnung_einwilligung: Boolean(input.email_rechnung_einwilligung),
    email_info_einwilligung: Boolean(input.email_info_einwilligung),
    mitglied_seit: input.mitglied_seit || null,
    bemerkung: input.bemerkung?.trim() || null,
  };
  const updated = await updateMemberStammdaten(session, memberId, values);
  if (!updated[0]) throw new Error("Die Änderung wurde nicht bestätigt.");
  return updated[0];
}
