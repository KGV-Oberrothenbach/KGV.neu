import { readSupabase, type BrowserSession } from "../../lib/supabase-auth";

export type MembershipApplicationDocument = {
  id: number;
  titel: string | null;
  dateiname: string | null;
  updated_at: string;
};

export function listMembershipApplicationDocuments(session: BrowserSession, memberId: number) {
  return readSupabase<MembershipApplicationDocument>(session, "dokument", {
    select: "id,titel,dateiname,updated_at",
    mitglied_id: `eq.${memberId}`,
    archiviert_at: "is.null",
    order: "updated_at.desc",
  });
}
