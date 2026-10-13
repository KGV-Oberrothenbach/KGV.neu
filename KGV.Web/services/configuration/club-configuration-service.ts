import type { ClubContext } from "../../models/auth/club";
import { type BrowserSession } from "../../lib/supabase-auth";
import { clubConfigurationDraft, emptyClubConfiguration, type ClubConfigurationDraft, type ClubConfigurationRecord } from "../../models/configuration/club-configuration";
import { createClubConfiguration, getActiveClubConfiguration, updateClubConfiguration } from "../../repositories/configuration/club-configuration-repository";

const requiredFields: Array<[keyof ClubConfigurationDraft, string]> = [["vereinsname", "Vereinsname"], ["registerangabe", "Registerangabe"], ["strasse", "Straße"], ["plz", "PLZ"], ["ort", "Ort"], ["standard_email", "Standard-E-Mail"], ["kontoinhaber", "Kontoinhaber"], ["bankname", "Bankname"], ["iban", "IBAN"], ["bic", "BIC"], ["dokument_ort", "Dokumentort"]];
const bicPattern = /^[A-Z]{4}[A-Z]{2}[A-Z0-9]{2}(?:[A-Z0-9]{3})?$/;
const emailPattern = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

export function normalizeIban(value: string | null | undefined): string { return (value ?? "").replace(/\s+/g, "").toUpperCase(); }
export function normalizeBic(value: string | null | undefined): string { return (value ?? "").replace(/\s+/g, "").toUpperCase(); }
export function isValidIban(value: string): boolean {
  if (!/^[A-Z]{2}\d{2}[A-Z0-9]{11,30}$/.test(value)) return false;
  let remainder = 0;
  for (const character of `${value.slice(4)}${value.slice(0, 4)}`) for (const digit of (character >= "A" && character <= "Z" ? String(character.charCodeAt(0) - 55) : character)) remainder = (remainder * 10 + Number(digit)) % 97;
  return remainder === 1;
}
export function isValidBic(value: string): boolean { return bicPattern.test(value); }
function normalizeOptional(value: string | null | undefined): string | null { const normalized = value?.trim() ?? ""; return normalized || null; }
function normalizeDate(value: string | null | undefined): string | null | undefined { const date = normalizeOptional(value); if (date === null) return null; if (!/^\d{4}-\d{2}-\d{2}$/.test(date)) return undefined; const parsed = new Date(`${date}T00:00:00Z`); return Number.isNaN(parsed.getTime()) || parsed.toISOString().slice(0, 10) !== date ? undefined : date; }

export function normalizeAndValidateClubConfiguration(draft: ClubConfigurationDraft): { payload?: Omit<ClubConfigurationRecord, "id">; error?: string } {
  const normalized = { ...draft } as ClubConfigurationDraft;
  for (const key of Object.keys(normalized) as Array<keyof ClubConfigurationDraft>) if (key !== "id" && key !== "aktiv" && key !== "iban" && key !== "bic" && key !== "datenschutz_stand") normalized[key] = normalizeOptional(normalized[key] as string | null) as never;
  normalized.iban = normalizeIban(draft.iban) || null;
  normalized.bic = normalizeBic(draft.bic) || null;
  const datenschutzStand = normalizeDate(draft.datenschutz_stand);
  for (const [key, label] of requiredFields) if (!normalized[key]) return { error: `${label} ist erforderlich.` };
  if (!emailPattern.test(normalized.standard_email!)) return { error: "Die Standard-E-Mail ist ungültig." };
  if (!isValidIban(normalized.iban!)) return { error: "Die IBAN ist ungültig." };
  if (!isValidBic(normalized.bic!)) return { error: "Der BIC ist ungültig." };
  if (datenschutzStand === undefined) return { error: "Der Datenschutz-Stand muss ein gültiges Datum sein." };
  normalized.datenschutz_stand = datenschutzStand;
  const payload = { ...normalized };
  Reflect.deleteProperty(payload, "id");
  return { payload: { ...payload, aktiv: true } };
}
export async function loadClubConfiguration(club: ClubContext, session: BrowserSession): Promise<{ draft: ClubConfigurationDraft; exists: boolean }> { const record = await getActiveClubConfiguration(club, session); return record ? { draft: clubConfigurationDraft(record), exists: true } : { draft: emptyClubConfiguration(), exists: false }; }
export async function saveClubConfiguration(club: ClubContext, session: BrowserSession, draft: ClubConfigurationDraft): Promise<ClubConfigurationRecord> { const validation = normalizeAndValidateClubConfiguration(draft); if (!validation.payload) throw new Error(validation.error ?? "Vereinskonfiguration ist ungültig."); const rows = draft.id === null ? await createClubConfiguration(club, session, validation.payload) : await updateClubConfiguration(club, session, draft.id, validation.payload); if (!rows[0]) throw new Error("Vereinskonfiguration wurde nicht gespeichert."); return rows[0]; }
