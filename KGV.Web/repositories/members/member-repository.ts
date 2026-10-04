import { type BrowserSession, readSupabase, writeSupabase } from "../../lib/supabase-auth";
import { type Member, type MemberSearchResult, type MemberWorkspaceInfo } from "../../models/members/member";
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

const memberStammdatenSelect = "id,vorname,name,email,aktiv,hauptmitglied_id,auth_user_id,geburtsdatum,arbeitsstunden_altersregel_typ,adresse,plz,ort,telefon,handy,whatsapp_einwilligung,email_rechnung_einwilligung,email_info_einwilligung,mitglied_seit,mitglied_ende,bemerkung";

export const getMemberById = (session: BrowserSession, memberId: number) => readSupabase<Member>(session, "mitglied", {
  select: memberStammdatenSelect,
  id: `eq.${memberId}`,
  limit: "1",
}).then((members) => members[0] ?? null);

export const updateMemberStammdaten = (session: BrowserSession, memberId: number, values: Record<string, unknown>) => writeSupabase<Member>(session, "mitglied", "PATCH", values, {
  id: `eq.${memberId}`,
});

export const createMember = (session: BrowserSession, values: Record<string, unknown>) => writeSupabase<Member>(session, "mitglied", "POST", values);
