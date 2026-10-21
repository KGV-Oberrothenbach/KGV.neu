import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";

const root = new URL("../../", import.meta.url);
const migration = await readFile(new URL("supabase/migrations/20261021123000_impressum_vereinsfunktionen.sql", root), "utf8");
const aclMigration = await readFile(new URL("supabase/migrations/20261021124000_fix_impressum_rpc_acl.sql", root), "utf8");

assert.match(migration, /language sql stable security definer\s+set search_path = public, pg_temp/i);
assert.match(migration, /revoke all on function public\.impressum_kontakte\(\) from public/i);
assert.match(migration, /grant execute on function public\.impressum_kontakte\(\) to authenticated, service_role/i);
assert.match(migration, /f\.aktiv[\s\S]*v\.verwendungszweck = 'impressum'[\s\S]*b\.gueltig_ab <= current_date[\s\S]*b\.gueltig_bis is null or b\.gueltig_bis >= current_date/i);
assert.match(migration, /case when v\.impressum_zeige_ladungsfaehige_anschrift then m\.adresse end/i);
assert.match(migration, /ux_vereinsfunktion_verwendung_impressum_hauptkontakt/i);
assert.match(migration, /alter column impressum_gruppe set not null/i);
assert.match(migration, /alter column impressum_zeige_ladungsfaehige_anschrift set not null/i);
assert.doesNotMatch(migration, /impressum_funktion_slot/i);
assert.doesNotMatch(migration, /vertretung/i);
assert.match(aclMigration, /revoke all on function public\.impressum_kontakte\(\) from public/i);
assert.match(aclMigration, /revoke all on function public\.impressum_kontakte\(\) from anon/i);
assert.match(aclMigration, /grant execute on function public\.impressum_kontakte\(\) to authenticated, service_role/i);
