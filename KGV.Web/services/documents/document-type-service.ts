import type { ClubContext } from "../../models/auth/club";
import type { BrowserSession } from "../../lib/supabase-auth";
import type { DocumentType } from "../../models/documents/document-type";
import { documentTypeIsInUse, listActiveDocumentTypes, listDocumentTypes, saveDocumentType, saveDocumentTypeEffects } from "../../repositories/documents/document-type-repository";

export const documentTypeEffects = ["mitgliedsantrag_nachweis", "pachtvertrag_nachweis", "wartungsvertrag_nachweis", "pachtbezug_erforderlich", "wartungsvertragsbezug_erforderlich"] as const;
export const documentTypeEffectLabels: Record<DocumentType["wirkungen"][number], string> = {
  mitgliedsantrag_nachweis: "Liefert Mitgliedsantrag-Nachweis",
  pachtvertrag_nachweis: "Liefert Pachtvertrag-Nachweis",
  wartungsvertrag_nachweis: "Liefert Wartungsvertrag-Nachweis",
  pachtbezug_erforderlich: "Pachtbeziehung erforderlich",
  wartungsvertragsbezug_erforderlich: "Wartungsvertragszuordnung erforderlich",
};

export async function loadActiveDocumentTypes(club: ClubContext, session: BrowserSession): Promise<DocumentType[]> {
  const { types, effects } = await listActiveDocumentTypes(club, session);
  const effectsByType = new Map<number, DocumentType["wirkungen"]>();
  for (const effect of effects) effectsByType.set(effect.dokumenttyp_id, [...(effectsByType.get(effect.dokumenttyp_id) ?? []), effect.wirkung]);
  return types.map((type) => ({ ...type, wirkungen: effectsByType.get(type.id) ?? [] }));
}

export async function loadDocumentTypes(club: ClubContext, session: BrowserSession): Promise<DocumentType[]> {
  const { types, effects } = await listDocumentTypes(club, session);
  const effectsByType = new Map<number, DocumentType["wirkungen"]>();
  for (const effect of effects) effectsByType.set(effect.dokumenttyp_id, [...(effectsByType.get(effect.dokumenttyp_id) ?? []), effect.wirkung]);
  return types.map((type) => ({ ...type, wirkungen: effectsByType.get(type.id) ?? [] }));
}

export function isDocumentTypeInUse(club: ClubContext, session: BrowserSession, documentTypeId: number) {
  return documentTypeIsInUse(club, session, documentTypeId);
}

export function validateDocumentType(input: { bezeichnung: string; sortierung: number }) {
  if (!input.bezeichnung.trim()) throw new Error("Eine Bezeichnung ist erforderlich.");
  if (!Number.isInteger(input.sortierung)) throw new Error("Die Sortierung muss eine ganze Zahl sein.");
}

export async function saveManagedDocumentType(
  club: ClubContext, session: BrowserSession,
  input: { id: number | null; bezeichnung: string; aktiv: boolean; sortierung: number },
): Promise<number> {
  validateDocumentType(input);
  const rows = await saveDocumentType(club, session, { ...input, bezeichnung: input.bezeichnung.trim() });
  const id = rows?.[0]?.id;
  if (typeof id !== "number" || !Number.isSafeInteger(id) || id <= 0) throw new Error("Die Dokumenttyp-ID wurde nicht bestätigt.");
  return id;
}

export async function saveManagedDocumentEffects(
  club: ClubContext, session: BrowserSession, typeId: number,
  effects: DocumentType["wirkungen"], expectedEffects: DocumentType["wirkungen"],
): Promise<void> {
  if (!Number.isSafeInteger(typeId) || typeId <= 0)
    throw new Error("Die Dokumenttyp-ID ist ungültig.");
  for (const list of [effects, expectedEffects]) {
    if (list.some((effect) => !documentTypeEffects.includes(effect)) || new Set(list).size !== list.length)
      throw new Error("Ungültige oder doppelte Dokumentwirkung.");
  }
  await saveDocumentTypeEffects(club, session, typeId, effects, expectedEffects);
}
