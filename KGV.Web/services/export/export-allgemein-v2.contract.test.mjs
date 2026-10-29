import assert from "node:assert/strict";
import { createHash } from "node:crypto";
import { readFileSync } from "node:fs";
import test from "node:test";

const root = new URL("../../../", import.meta.url);
const spec = readFileSync(new URL("KGV.Core/Templates/export-allgemein/v2/field-spec.json", root));
const migration = readFileSync(new URL("supabase/migrations/20261023123000_publish_export_allgemein_v2.sql", root), "utf8");
const v3 = readFileSync(new URL("KGV.Core/Templates/export-allgemein/v3/field-spec.json", root));
const publishV3 = readFileSync(new URL("supabase/migrations/20261029112000_repair_export_allgemein_v2_field_spec_checksum.sql", root), "utf8");
const renderer = readFileSync(new URL("supabase/functions/kgv-generate-export-pdf/index.ts", root), "utf8");

test("export-allgemein v2 stays immutable while v3 publishes the corrected artifact", () => {
  const checksum = createHash("sha256").update(spec).digest("hex");
  assert.equal(checksum, "8d526a6e7ebf7180dad5c6e33ad98141ce48f985b0d846c4f8319b504c1675cd");
  assert.match(migration, new RegExp(checksum));
  assert.equal(createHash("sha256").update(v3).digest("hex"), checksum);
  assert.match(publishV3, /'export-allgemein', 'v3'/);
  assert.match(publishV3, /templates\/export-allgemein\/v3\/template\.pdf/);
  assert.match(publishV3, /version <> 'v3'/);
  assert.match(publishV3, /set status = 'published', aktiv = true/);
  assert.match(publishV3, new RegExp(checksum));
  assert.doesNotMatch(publishV3, /set field_spec_checksum/);
  assert.match(renderer, /\.eq\("status", "published"\)\.eq\("aktiv", true\)/);
  for (const field of ["exporttitel", "stand", "exporttabelle"]) assert.match(spec.toString("utf8"), new RegExp(`\\"name\\": \\"${field}\\"`));
});
