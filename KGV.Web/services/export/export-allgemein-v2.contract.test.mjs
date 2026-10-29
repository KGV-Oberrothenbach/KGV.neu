import assert from "node:assert/strict";
import { createHash } from "node:crypto";
import { readFileSync } from "node:fs";
import test from "node:test";

const root = new URL("../../../", import.meta.url);
const spec = readFileSync(new URL("KGV.Core/Templates/export-allgemein/v2/field-spec.json", root));
const migration = readFileSync(new URL("supabase/migrations/20261023123000_publish_export_allgemein_v2.sql", root), "utf8");
const repair = readFileSync(new URL("supabase/migrations/20261029112000_repair_export_allgemein_v2_field_spec_checksum.sql", root), "utf8");

test("export-allgemein v2 catalog checksum matches the publishable field spec", () => {
  const checksum = createHash("sha256").update(spec).digest("hex");
  assert.equal(checksum, "8d526a6e7ebf7180dad5c6e33ad98141ce48f985b0d846c4f8319b504c1675cd");
  assert.match(migration, new RegExp(checksum));
  assert.match(repair, new RegExp(checksum));
  for (const field of ["exporttitel", "stand", "exporttabelle"]) assert.match(spec.toString("utf8"), new RegExp(`\\"name\\": \\"${field}\\"`));
});
