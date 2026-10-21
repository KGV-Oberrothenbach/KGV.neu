import { callSupabaseRpc } from "../../lib/supabase/rpc-client";
import type { ClubContext } from "../../models/auth/club";
import { type BrowserSession } from "../../lib/supabase-auth";

export type ImprintContactRow = { bezeichnung: string; besetzung_id: number; gruppen_schluessel: string; gruppen_bezeichnung: string; gruppen_sort_order: number; ist_hauptkontakt: boolean; zeige_ladungsfaehige_anschrift: boolean; vorname: string | null; nachname: string | null; handy: string | null; adresse: string | null; plz: string | null; ort: string | null };

export async function listImprintContacts(club: ClubContext, session: BrowserSession): Promise<ImprintContactRow[]> {
  return callSupabaseRpc<ImprintContactRow[]>({ club, session }, "impressum_kontakte", {});
}
