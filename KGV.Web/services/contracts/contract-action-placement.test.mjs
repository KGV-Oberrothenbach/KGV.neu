import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";
import test from "node:test";

test("membership and lease actions are limited to their domain views", async () => {
  const [page, memberMasterData, memberParcelDetail, leaseFlow] = await Promise.all([
    readFile(new URL("../../app/page.tsx", import.meta.url), "utf8"),
    readFile(new URL("../../features/members/MemberStammdaten.tsx", import.meta.url), "utf8"),
    readFile(new URL("../../features/parcels/MemberParcelDetail.tsx", import.meta.url), "utf8"),
    readFile(new URL("../../features/contracts/LeaseContractFlow.tsx", import.meta.url), "utf8"),
  ]);

  assert.doesNotMatch(page, /ContractComposer/);
  assert.match(page, /mitglied-dokumente" && selectedMemberId && <DocumentList/);
  assert.match(memberMasterData, /member && !member\.hauptmitglied_id && <>\s*<MembershipApplicationAction/);
  assert.match(memberParcelDetail, /selected\.isCurrent && assignment\.von_datum && <LeaseContractAction/);
  assert.match(leaseFlow, /parcelId: number; startDate: string/);
  assert.doesNotMatch(leaseFlow, /listEligibleLeaseParcels|fixedParcel/);
  assert.match(page, /<MemberSearch session=\{session\} selectedMemberId=\{selectedMemberId\} onSelect=\{\(memberId\) => \{ selectMember\(memberId\); setActiveId\("mitglied-stammdaten"\); \}\}/);
});
