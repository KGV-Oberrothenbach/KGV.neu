import test from "node:test";
import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";

const migration = await readFile(new URL("../migrations/20261109130000_p11_demo_cleanup_d5_physical_schema.sql", import.meta.url), "utf8");
const historicalColumns = [
  "arbeitseinsatz_anmeldung", "arbeitsstunde", "client_diagnostics_log", "dokument",
  "impressum_funktion_slot", "mitglied_gesetzlicher_vertreter", "mitglied_saison",
  "parzellen_belegung", "vereinskonfiguration", "wartungsvertrag_zuordnungen",
  "zaehler", "zaehler_ablesung",
];

test("D5 removes every historical DemoDB-only column without CASCADE", () => {
  for (const table of historicalColumns) {
    assert.match(migration, new RegExp(`alter table if exists public\\.${table} drop column if exists is_demo`, "i"));
  }
  const executableSql = migration.replace(/--.*$/gm, "");
  assert.doesNotMatch(executableSql, /drop\s+(?:column\s+)?[^;]*cascade/i);
});

test("D5 retains both legacy export overloads without Demo predicates", () => {
  const members = migration.match(/CREATE OR REPLACE FUNCTION public\.rpc_export_mitgliederliste\(\s*p_sortierung text[\s\S]*?\$function\$;/i)?.[0] ?? "";
  const rfid = migration.match(/CREATE OR REPLACE FUNCTION public\.rpc_export_rfid_status\(\s*p_anlage_filter text[\s\S]*?\$function\$;/i)?.[0] ?? "";
  assert.ok(members);
  assert.ok(rfid);
  assert.doesNotMatch(members, /is_demo/i);
  assert.doesNotMatch(rfid, /is_demo/i);
});
