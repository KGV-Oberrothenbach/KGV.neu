import { type BrowserSession, readSupabase } from "../../lib/supabase-auth";
export type MemberName = { id: number; vorname: string | null; name: string | null };
export const listMemberNames = (session: BrowserSession, ids: number[]) => ids.length ? readSupabase<MemberName>(session, "mitglied", { select: "id,vorname,name", id: `in.(${ids.join(",")})`, limit: "500" }) : Promise.resolve([] as MemberName[]);
