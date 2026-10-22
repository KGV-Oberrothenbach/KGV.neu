import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";
import test from "node:test";
const [flow,entry] = await Promise.all([readFile(new URL("./MaintenanceContractSignatureFlow.tsx", import.meta.url),"utf8"),readFile(new URL("./MemberMaintenanceContracts.tsx", import.meta.url),"utf8")]);
test("actual WV member maintenance entry opens the persistent signature flow",()=>{assert.match(entry,/MaintenanceContractSignatureFlow/);assert.match(entry,/setSigningAssignmentId\(assignment\.id\)/);assert.match(flow,/ensure_wartungsvertrag_prepared_contract/);});
test("WV UI persists individual slots and finalizes without signature payload",()=>{assert.match(flow,/maintenanceContractPersistentSlots/);assert.match(flow,/uploadPreparedContractSignature/);assert.match(flow,/savePreparedContractSignature/);assert.match(flow,/kommunikationsvorgang_id:contractId/);assert.doesNotMatch(flow,/signature_member/);});
test("WV UI uses structured club signatories and excludes the other board member",()=>{assert.match(flow,/PreparedContractSignatorySelector/);assert.match(flow,/excludeMemberId=\{excluded\}/);assert.match(flow,/signatory,storagePath/);});
