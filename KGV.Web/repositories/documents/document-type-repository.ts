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

export async function documentTypeIsInUse(club: ClubContext, session: BrowserSession, documentTypeId: number) {
  const documents = await readSupabaseRest<{ id: number }>({ club, session }, "dokument", { select: "id", dokumenttyp_id: `eq.${documentTypeId}`, limit: "1" });
  return documents.length > 0;
}

export function saveDocumentType(club: ClubContext, session: BrowserSession, input: { id: number | null; bezeichnung: string; aktiv: boolean; sortierung: number; wirkungen: DocumentTypeEffect[] }) {
  return callSupabaseRpc<DocumentTypeRow[]>({ club, session }, "save_dokumenttyp", {
    p_dokumenttyp_id: input.id,
    p_bezeichnung: input.bezeichnung,
    p_aktiv: input.aktiv,
    p_sortierung: input.sortierung,
    p_wirkungen: input.wirkungen,
  });
}
