import { readSupabaseRest } from "../../lib/supabase/rest-client";
import { writeWithBrowserEditLock } from "../../services/concurrency/edit-lock-service";
import { callSupabaseRpc } from "../../lib/supabase/rpc-client";
import type { ClubContext } from "../../models/auth/club";
import { type BrowserSession } from "../../lib/supabase-auth";
import { type Member, type MemberSearchResult, type MemberWorkspaceInfo } from "../../models/members/member";
export type MemberName = { id: number; vorname: string | null; name: string | null };
export const listMemberNames = (club: ClubContext, session: BrowserSession, ids: number[]) => ids.length ? readSupabaseRest<MemberName>({ club, session }, "mitglied", { select: "id,vorname,name", id: `in.(${ids.join(",")})`, limit: "500" }) : Promise.resolve([] as MemberName[]);

export const listMembersForSearch = (club: ClubContext, session: BrowserSession) => readSupabaseRest<Omit<MemberSearchResult, "gartenNummern">>({ club, session }, "mitglied", {
  select: "id,name,vorname,email,aktiv",
  order: "name.asc,vorname.asc",
  limit: "1000",
});

export const getMemberWorkspaceInfo = (club: ClubContext, session: BrowserSession, memberId: number) => readSupabaseRest<MemberWorkspaceInfo>({ club, session }, "mitglied", {
  select: "id,vorname,name",
  id: `eq.${memberId}`,
  limit: "1",
}).then((members) => members[0] ?? null);

const memberStammdatenSelect = "id,vorname,name,email,aktiv,hauptmitglied_id,auth_user_id,geburtsdatum,arbeitsstunden_altersregel_typ,adresse,plz,ort,telefon,handy,whatsapp_einwilligung,email_rechnung_einwilligung,email_info_einwilligung,mitglied_seit,mitglied_ende,bemerkung";

export const getMemberById = (club: ClubContext, session: BrowserSession, memberId: number) => readSupabaseRest<Member>({ club, session }, "mitglied", {
  select: memberStammdatenSelect,
  id: `eq.${memberId}`,
  limit: "1",
}).then((members) => members[0] ?? null);

export const updateMemberStammdaten = (club: ClubContext, session: BrowserSession, memberId: number, values: Record<string, unknown>) => writeWithBrowserEditLock<Member>({ club, session }, "mitglied", "PATCH", values, {
  id: `eq.${memberId}`,
});

export const createMember = (club: ClubContext, session: BrowserSession, values: Record<string, unknown>) => writeWithBrowserEditLock<Member>({ club, session }, "mitglied", "POST", values);

export const getSecondaryMemberByMainMemberId = (club: ClubContext, session: BrowserSession, mainMemberId: number) => readSupabaseRest<Member>({ club, session }, "mitglied", {
  select: memberStammdatenSelect,
  hauptmitglied_id: `eq.${mainMemberId}`,
  order: "id.asc",
  limit: "1",
}).then((members) => members[0] ?? null);

export type MembershipEndDecision = "end_secondary" | "promote_secondary";
export type MembershipEndRpcRow = {
  success: boolean;
  message: string;
  updated_main_member: Member | null;
  updated_secondary_member: Member | null;
  secondary_decision: MembershipEndDecision | null;
};

export const endMembership = async (club: ClubContext, session: BrowserSession, mainMemberId: number, decision: MembershipEndDecision | null) => {
  const rows = await callSupabaseRpc<MembershipEndRpcRow[]>({ club, session }, "end_membership", {
    p_main_member_id: mainMemberId,
    p_secondary_decision: decision,
  });
  return rows[0] ?? null;
};

export const createSecondaryMember = createMember;

export const updateSecondaryMember = (club: ClubContext, session: BrowserSession, memberId: number, values: Record<string, unknown>) => writeWithBrowserEditLock<Member>({ club, session }, "mitglied", "PATCH", values, {
  id: `eq.${memberId}`,
});
