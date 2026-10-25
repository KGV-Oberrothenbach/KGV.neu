import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";
import test from "node:test";
const migration = new URL("../../supabase/migrations/20261023150000_wv4_vertragspartei_und_papieruebergabe.sql", import.meta.url);
const dispatchMigration = new URL("../../supabase/migrations/20261023160000_wv5_dispatch_party_context.sql", import.meta.url);
const service = new URL("../services/maintenance/maintenance-assignment-service.ts", import.meta.url);
const repository = new URL("../repositories/maintenance/maintenance-assignment-repository.ts", import.meta.url);
test("WV persists a distinct contractual party and preserves it for follow-up contracts", async () => {
 const [sql, source, repo] = await Promise.all([readFile(migration,"utf8"),readFile(service,"utf8"),readFile(repository,"utf8")]);
 for(const value of ["vertragspartei_mitglied_id","tatsächliche Vertragspartei","new.vertragspartei_mitglied_id is distinct from v.vertragspartei_mitglied_id","z.vertragspartei_mitglied_id","z.hauptmitglied_id","vertragspartei_mitglied_id: input.selectedMemberId","vertragspartei_mitglied_id: targetId"]) assert.ok((sql+source+repo).includes(value));
 assert.doesNotMatch(sql,/update public\.wartungsvertrag_zuordnungen set vertragspartei_mitglied_id=hauptmitglied_id/i);
});
test("paper evidence is captured only before signature and MA/PV obligations are explicit", async () => {
 const sql=await readFile(migration,"utf8");
 for(const value of ["dokumentiere_prepared_contract_papieruebergabe","v.status<>'vorbereitet'","'mitgliedsantrag' then array['satzung']","'pachtvertrag' then array['kleingartenordnung','beitragsordnung','arbeitsordnung']","bereitstellungsart','papier'","revision=v.revision+1"]) assert.ok(sql.includes(value));
 assert.doesNotMatch(sql,/anlage4|attachment4/i);
});
test("NM-WV dispatch uses exactly contractual party plus stored principal and never guesses legacy data", async () => {
 const sql=await readFile(dispatchMigration,"utf8");
 for(const value of ["vertragspartei_mitglied_id=z.hauptmitglied_id","rolle='nebenmitglied'","rolle='hauptmitglied'","wartungsvertrag_zuordnung","Never enumerate current secondary members", "tatsächliche Vertragspartei ist unbekannt", "es wird nicht geraten"]) assert.ok(sql.includes(value));
 assert.doesNotMatch(sql,/hauptmitglied_id.*from public\.mitglied.*hauptmitglied_id/i);
});
