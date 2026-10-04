import { type BrowserSession } from "../../lib/supabase-auth";
import { type Member } from "../../models/members/member";
import { createSecondaryMember, getMemberById, getSecondaryMemberByMainMemberId, updateSecondaryMember } from "../../repositories/members/member-repository";

export type SecondaryMemberPermissions = { canManageSecondary: boolean };
export type SecondaryMemberCreateInput = Pick<Member, "vorname" | "name" | "adresse" | "plz" | "ort" | "telefon" | "handy" | "email" | "geburtsdatum" | "mitglied_seit" | "whatsapp_einwilligung"> & { adresseUebernehmen: boolean };
export type SecondaryMemberUpdateInput = Pick<Member, "adresse" | "plz" | "ort" | "telefon" | "handy" | "email" | "geburtsdatum" | "mitglied_seit" | "whatsapp_einwilligung">;

const emailPattern = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
const postalCodePattern = /^\d{5}$/;
const today = () => new Date().toISOString().slice(0, 10);

function requirePermission(permissions: SecondaryMemberPermissions) {
  if (!permissions.canManageSecondary) throw new Error("Für Nebenmitglieder besteht keine Bearbeitungsberechtigung.");
}

function validateContact(input: SecondaryMemberUpdateInput) {
  const adresse = input.adresse?.trim() ?? "";
  const plz = input.plz?.trim() ?? "";
  const ort = input.ort?.trim() ?? "";
  const email = input.email?.trim() || null;
  if (!adresse) throw new Error("Adresse ist Pflicht.");
  if (!postalCodePattern.test(plz)) throw new Error("PLZ muss 5-stellig sein.");
  if (!ort) throw new Error("Ort ist Pflicht.");
  if (email && !emailPattern.test(email)) throw new Error("E-Mail-Adresse ist nicht plausibel.");
  if (input.geburtsdatum && input.geburtsdatum > today()) throw new Error("Geburtsdatum darf nicht in der Zukunft liegen.");
  if (!input.mitglied_seit) throw new Error("Mitglied seit ist erforderlich.");
  if (input.mitglied_seit > today()) throw new Error("Mitglied seit darf nicht in der Zukunft liegen.");
  return { adresse, plz, ort, email };
}

export const loadSecondaryMember = getSecondaryMemberByMainMemberId;

export async function createNewSecondaryMember(session: BrowserSession, mainMemberId: number, input: SecondaryMemberCreateInput, permissions: SecondaryMemberPermissions) {
  requirePermission(permissions);
  const mainMember = await getMemberById(session, mainMemberId);
  if (!mainMember) throw new Error("Das Hauptmitglied konnte nicht geladen werden.");
  if (mainMember.hauptmitglied_id !== null) throw new Error("Nebenmitglieder können kein weiteres Nebenmitglied erhalten.");
  if (await getSecondaryMemberByMainMemberId(session, mainMemberId)) throw new Error("Für dieses Hauptmitglied besteht bereits ein Nebenmitglied.");
  const vorname = input.vorname?.trim() ?? "";
  const name = input.name?.trim() ?? "";
  if (!vorname) throw new Error("Vorname ist erforderlich.");
  if (!name) throw new Error("Nachname ist erforderlich.");
  const source = input.adresseUebernehmen
    ? { ...input, adresse: mainMember.adresse, plz: mainMember.plz, ort: mainMember.ort }
    : input;
  const contact = validateContact(source);
  const created = await createSecondaryMember(session, {
    hauptmitglied_id: mainMemberId,
    vorname,
    name,
    ...contact,
    telefon: input.telefon?.trim() || null,
    handy: input.handy?.trim() || null,
    geburtsdatum: input.geburtsdatum || null,
    mitglied_seit: input.mitglied_seit,
    whatsapp_einwilligung: Boolean(input.whatsapp_einwilligung),
    aktiv: true,
    mitglied_ende: null,
  });
  if (!created[0]) throw new Error("Nebenmitglied konnte nicht angelegt werden.");
  return created[0];
}

export async function updateExistingSecondaryMember(session: BrowserSession, mainMemberId: number, secondaryMemberId: number, input: SecondaryMemberUpdateInput, permissions: SecondaryMemberPermissions) {
  requirePermission(permissions);
  const current = await getMemberById(session, secondaryMemberId);
  if (!current || current.hauptmitglied_id !== mainMemberId) throw new Error("Das Nebenmitglied gehört nicht zu diesem Hauptmitglied.");
  const contact = validateContact(input);
  const updated = await updateSecondaryMember(session, secondaryMemberId, {
    ...contact,
    telefon: input.telefon?.trim() || null,
    handy: input.handy?.trim() || null,
    geburtsdatum: input.geburtsdatum || null,
    mitglied_seit: input.mitglied_seit,
    whatsapp_einwilligung: Boolean(input.whatsapp_einwilligung),
    email: current.auth_user_id == null ? contact.email : current.email,
  });
  if (!updated[0]) throw new Error("Die Änderung wurde nicht bestätigt.");
  return updated[0];
}
