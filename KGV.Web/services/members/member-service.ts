import { type BrowserSession } from "../../lib/supabase-auth";
import { type MemberSearchResult } from "../../models/members/member";
import { getMemberWorkspaceInfo, listMembersForSearch } from "../../repositories/members/member-repository";
import { listCurrentGardenNumbersByMemberId } from "../parcels/parcel-service";

export async function loadMemberSearchResults(session: BrowserSession): Promise<MemberSearchResult[]> {
  const members = await listMembersForSearch(session);
  const gardenNumbersByMemberId = await listCurrentGardenNumbersByMemberId(session, members.map((member) => member.id));

  return members.map((member) => ({ ...member, gartenNummern: gardenNumbersByMemberId.get(member.id) ?? [] }));
}

export function filterMemberSearchResults(members: MemberSearchResult[], options: { query: string; includeInactive: boolean }) {
  const normalizedQuery = options.query.trim().toLocaleLowerCase("de");

  return members
    .filter((member) => options.includeInactive || member.aktiv)
    .filter((member) => !normalizedQuery || [member.name, member.vorname, member.email, String(member.id), member.gartenNummern.join(" ")]
      .filter(Boolean)
      .join(" ")
      .toLocaleLowerCase("de")
      .includes(normalizedQuery));
}

export const loadMemberWorkspaceInfo = getMemberWorkspaceInfo;
