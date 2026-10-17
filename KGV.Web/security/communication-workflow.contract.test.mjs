import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";

const migration = await readFile(
  new URL("../../supabase/migrations/20261017130000_kommunikationsvorgang_foundation.sql", import.meta.url),
  "utf8",
);
const tableDefinition = migration.match(/create table public\.kommunikationsvorgang \([\s\S]*?\n\);/i)?.[0] ?? "";

assert.match(migration, /create table public\.kommunikationsvorgang/i, "creates the central table");
assert.match(migration, /entwurf_daten jsonb not null default '\{\}'::jsonb/i, "stores JSONB drafts");
assert.match(migration, /revision integer not null default 1 check \(revision >= 1\)/i, "starts revisions at one");
assert.match(migration, /vorgangstyp in \('vertrag_signatur', 'mail', 'druck'\)/i, "limits workflow types");
assert.match(migration, /status in \([\s\S]*'entwurf'[\s\S]*'abgebrochen'/i, "limits status values");
assert.match(migration, /references public\.mitglied\(id\) on delete restrict/i, "preserves member-linked history");
assert.match(migration, /references public\.parzelle\(id\) on delete restrict/i, "preserves plot-linked history");
assert.doesNotMatch(tableDefinition, /verein_id/i, "does not add an artificial association id");
assert.doesNotMatch(migration, /old\.status = 'erledigt'/i, "keeps erledigt terminal by defining no outbound transition");
assert.doesNotMatch(migration, /old\.status = 'abgebrochen'/i, "keeps abgebrochen terminal by defining no outbound transition");
assert.match(migration, /old\.status = 'vorbereitet' and new\.status in \('entwurf'/i, "allows vorbereitet back to entwurf");
assert.match(migration, /old\.signatur_gestartet_am is not null[\s\S]*new\.entwurf_daten is distinct from old\.entwurf_daten/i, "locks signed draft data");
assert.match(migration, /alter table public\.kommunikationsvorgang enable row level security/i, "enables RLS");
assert.match(migration, /current_app_user_context\(\)[\s\S]*c\.role in \('admin', 'vorstand'\)/i, "uses app-user role context");
assert.doesNotMatch(migration, /c\.role in \('admin', 'vorstand', 'user'\)/i, "does not grant the user role workflow access");
assert.match(migration, /grant select, insert, update, delete on table public\.kommunikationsvorgang to authenticated/i, "grants authenticated Data API access subject to RLS");
assert.match(migration, /grant all on table public\.kommunikationsvorgang to service_role/i, "keeps service role access");
