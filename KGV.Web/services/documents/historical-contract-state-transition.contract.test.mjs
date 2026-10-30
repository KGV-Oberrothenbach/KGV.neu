import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";

const migration = await readFile(new URL("../../../supabase/migrations/20261030120000_g6b2_historical_contract_state_transitions.sql", import.meta.url), "utf8");
assert.match(migration, /can_register_historical_contracts/);
assert.match(migration, /p_vertragspartei_mitglied_ids bigint\[\]/);
assert.match(migration, /p_mitgliedschaftsenden jsonb/);
assert.match(migration, /p_aktiv_status_aenderungen jsonb/);
assert.match(migration, /pachtverhaeltnis_beteiligung/);
assert.match(migration, /p_vertragsdatum - 1/);
assert.match(migration, /register_historischen_pachtvertrag_g6a/);
assert.match(migration, /return query select \* from public\.register_historischen_pachtvertrag_g6a/);
