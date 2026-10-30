import assert from "node:assert/strict";
import {readFile} from "node:fs/promises";
import test from "node:test";
const read=p=>readFile(new URL(p,import.meta.url),"utf8");
test("Web persists one immutable operation snapshot before upload, reuses it and resets only explicitly",async()=>{
 const source=await read("../../features/documents/HistoricalContractRegistrationWorkflow.tsx");
 assert.equal((source.match(/crypto\.randomUUID\(\)/g)??[]).length,1);
 assert.match(source,/operationId: operationId\.current!/);
 assert.match(source,/pending\.current\?\.input\.operationId/);
 assert.ok(source.indexOf("localStorage.setItem(pendingKey")<source.indexOf("await loadHistoricalContractOperation"));
 assert.match(source,/draft\.input\.operationId/);assert.match(source,/draft\.input, documentId/);
 assert.match(source,/club\.supabaseUrl[\s\S]*club\.vereinId[\s\S]*session\.user\.id/);
 assert.match(source,/Neuen Altvertrag beginnen/);assert.match(source,/submitting\.current/);
 assert.doesNotMatch(source,/catch[^}]*randomUUID/);
 const rpc=await read("../../repositories/documents/historical-contract-registration-repository.ts");
 assert.match(rpc,/p_operation_id: input\.operationId/);
 const upload=await read("../../repositories/documents/document-upload-repository.ts");assert.match(upload,/form\.set\("operation_id", operationId\)/);
});
test("MAUI persists before networking and uses the same ID for upload and RPC without a queue",async()=>{
 const source=await read("../../../KGV.Maui/Pages/HistoricalContractRegistrationPage.cs");
 assert.equal((source.match(/Guid\.NewGuid\(\)/g)??[]).length,3); // New wizard, explicit next contract, unrelated file-view cache name.
 assert.match(source,/private Guid _operationId = Guid\.NewGuid/);
 assert.match(source,/var operationId = _operationId/);
 assert.match(source,/HistoricalOperationId = operationId/);assert.match(source,/ends, status, operationId/);
 assert.ok(source.indexOf("await SavePendingAsync();",source.indexOf("private async Task SubmitAsync"))<source.indexOf("await _supabase.GetHistoricalContractOperationAsync"));
 assert.match(source,/context\.SupabaseUrl[\s\S]*context\.VereinId[\s\S]*user/);
 assert.match(source,/await _supabase.RegisterHistoricalContractAsync\(_pending\.Registration\)/);
 assert.doesNotMatch(source,/new HistoricalDocumentRegistrationQueue|Enqueue/);
 const service=await read("../../../KGV.Infrastructure/Services/SupabaseService.cs");assert.match(service,/p_operation_id = request\.OperationId/);assert.match(service,/"operation_id"/);
});
