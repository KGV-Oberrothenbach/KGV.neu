import { callSupabaseRpc } from "../../lib/supabase/rpc-client";
import { invokeEdgeFunctionRaw } from "../../lib/supabase/edge-client";
import type { BrowserSession } from "../../lib/supabase-auth";
import type { ClubContext } from "../../models/auth/club";

export const recordMemberClubDocumentsPaper = (club: ClubContext, session: BrowserSession, memberId: number, versionIds: number[], deliveredAt: string) => callSupabaseRpc({ club, session }, "create_mitglied_vereinsdokument_bereitstellungen_papier", { p_mitglied_id: memberId, p_vereinsdokument_fassung_ids: versionIds, p_bereitgestellt_am: deliveredAt });
export async function sendMemberClubDocumentsDigital(club: ClubContext, session: BrowserSession, memberId: number, versionIds: number[]) {
  const response = await invokeEdgeFunctionRaw({ club, session }, "kgv-send-club-documents", { method: "POST", contentType: "application/json", body: JSON.stringify({ mitglied_id: memberId, vereinsdokument_fassung_ids: versionIds }) });
  const result = await response.json().catch(() => null); if (!response.ok || result?.success !== true) { const error = new Error(result?.message ?? "Der Versand konnte nicht bestätigt werden.") as Error & { code?: string }; error.code = result?.diagnostic_code; throw error; } return result;
}
