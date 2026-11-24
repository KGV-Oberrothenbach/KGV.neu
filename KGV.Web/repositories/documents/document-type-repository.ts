import { readSupabaseRest } from "../../lib/supabase/rest-client";
import { callSupabaseRpc } from "../../lib/supabase/rpc-client";
import type { ClubContext } from "../../models/auth/club";
import type { BrowserSession } from "../../lib/supabase-auth";
import type { DocumentTypeEffect } from "../../models/documents/document-type";

type DocumentTypeRow = { id: number; bezeichnung: string; aktiv: boolean; sortierung: number };
type DocumentTypeEffectRow = { dokumenttyp_id: number; wirkung: DocumentTypeEffect };

export async function listActiveDocumentTypes(club: ClubContext, session: BrowserSession) {
  const [types, effects] = await Promise.all([
    readSupabaseRest<DocumentTypeRow>({ club, session }, "dokumenttyp", { select: "id,bezeichnung,aktiv,sortierung", aktiv: "eq.true", order: "sortierung.asc,bezeichnung.asc", limit: "500" }),
    readSupabaseRest<DocumentTypeEffectRow>({ club, session }, "dokumenttyp_wirkung", { select: "dokumenttyp_id,wirkung", limit: "2000" }),
  ]);
  return { types, effects };
}

export async function listDocumentTypes(club: ClubContext, session: BrowserSession) {
  const [types, effects] = await Promise.all([
    readSupabaseRest<DocumentTypeRow>({ club, session }, "dokumenttyp", { select: "id,bezeichnung,aktiv,sortierung", order: "sortierung.asc,bezeichnung.asc", limit: "2000" }),
    readSupabaseRest<DocumentTypeEffectRow>({ club, session }, "dokumenttyp_wirkung", { select: "dokumenttyp_id,wirkung", limit: "5000" }),
  ]);
  return { types, effects };
}

// Privileged EXISTS check, independent of the user's document SELECT visibility.
// Returns a boolean only; no third-party document IDs or content leave the server.
export function documentTypeIsInUse(club: ClubContext, session: BrowserSession, documentTypeId: number): Promise<boolean> {
  return callSupabaseRpc<boolean>({ club, session }, "fr208_dokumenttyp_ist_verwendet", { p_dokumenttyp_id: documentTypeId });
}

export function saveDocumentTypeEffects(
  club: ClubContext, session: BrowserSession, id: number,
  wirkungen: DocumentTypeEffect[], expectedWirkungen: DocumentTypeEffect[],
): Promise<void> {
  return callSupabaseRpc<boolean>({ club, session }, "fr208_set_dokumenttyp_wirkungen_checked", {
    p_dokumenttyp_id: id, p_wirkungen: wirkungen, p_expected_wirkungen: expectedWirkungen,
  }).then((confirmed) => {
    if (confirmed !== true) throw new Error("Die Speicherung der Dokumentwirkungen wurde nicht bestätigt.");
  });
}

// Metadata RPC does not write effects. Effect-only users cannot call this RPC.
export function saveDocumentType(
  club: ClubContext, session: BrowserSession,
  input: { id: number | null; bezeichnung: string; aktiv: boolean; sortierung: number },
): Promise<DocumentTypeRow[]> {
  return callSupabaseRpc<DocumentTypeRow[]>({ club, session }, "save_dokumenttyp", {
    p_dokumenttyp_id: input.id, p_bezeichnung: input.bezeichnung,
    p_aktiv: input.aktiv, p_sortierung: input.sortierung,
    p_wirkungen: null,
  });
}
