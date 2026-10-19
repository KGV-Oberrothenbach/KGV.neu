import { callSupabaseRpc } from "../../lib/supabase/rpc-client";
import type { BrowserSession } from "../../lib/supabase-auth";
import type { ClubContext } from "../../models/auth/club";
import type { MemberClubDocumentStatus, MemberClubDocumentStatusItem } from "../../models/club-documents/member-club-document-status";

export async function getMemberClubDocumentStatus(club: ClubContext, session: BrowserSession, memberId: number, date: string): Promise<MemberClubDocumentStatus> {
  const documents = await callSupabaseRpc<MemberClubDocumentStatusItem[]>({ club, session }, "get_mitglied_vereinsdokument_status", { p_mitglied_id: memberId, p_stichtag: date });
  return {
    documents,
    missingDocuments: documents.filter((item) => item.status !== "bereitgestellt"),
    complete: documents.length > 0 && documents.every((item) => item.status === "bereitgestellt"),
    hasMissingClubVersion: documents.some((item) => item.status === "keine_aktuelle_fassung"),
  };
}
