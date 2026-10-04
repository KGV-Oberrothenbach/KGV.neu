import { type BrowserSession, readSupabase } from "../../lib/supabase-auth";
import { type MemberSearchResult, type MemberWorkspaceInfo } from "../../models/members/member";
export type MemberName = { id: number; vorname: string | null; name: string | null };
export const listMemberNames = (session: BrowserSession, ids: number[]) => ids.length ? readSupabase<MemberName>(session, "mitglied", { select: "id,vorname,name", id: `in.(${ids.join(",")})`, limit: "500" }) : Promise.resolve([] as MemberName[]);

export const listMembersForSearch = (session: BrowserSession) => readSupabase<Omit<MemberSearchResult, "gartenNummern">>(session, "mitglied", {
  select: "id,name,vorname,email,aktiv",
  order: "name.asc,vorname.asc",
  limit: "1000",
});

export const getMemberWorkspaceInfo = (session: BrowserSession, memberId: number) => readSupabase<MemberWorkspaceInfo>(session, "mitglied", {
  select: "id,vorname,name",
  id: `eq.${memberId}`,
  limit: "1",
}).then((members) => members[0] ?? null);
