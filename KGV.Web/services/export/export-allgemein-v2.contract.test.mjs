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
const uploadScript = readFileSync(new URL("KGV.Core/Templates/export-allgemein/PUBLISH_v3.ps1", root), "utf8");

test("export-allgemein v2 stays immutable while v3 publishes the corrected artifact", () => {
  const normalizeLineEndings = (content, lineEnding) => Buffer.from(
    content.toString("utf8").replace(/\r?\n/g, lineEnding),
    "utf8",
  );
  const v2Checksum = createHash("sha256").update(normalizeLineEndings(spec, "\n")).digest("hex");
  const v3Checksum = createHash("sha256").update(normalizeLineEndings(v3, "\r\n")).digest("hex");
  assert.equal(v2Checksum, "3459429f728ae7ba28919f10482b9443b6c3a9f051b99418c1519ca2c296df1c");
  assert.equal(v3Checksum, "8d526a6e7ebf7180dad5c6e33ad98141ce48f985b0d846c4f8319b504c1675cd");
  assert.match(migration, new RegExp(v2Checksum));
  assert.equal(JSON.stringify(JSON.parse(v3.toString("utf8"))), JSON.stringify(JSON.parse(spec.toString("utf8"))));
  assert.match(publishV3, /'export-allgemein', 'v3'/);
  assert.match(publishV3, /templates\/export-allgemein\/v3\/template\.pdf/);
  assert.match(publishV3, /version <> 'v3'/);
  assert.match(publishV3, /set status = 'published', aktiv = true/);
  assert.match(publishV3, new RegExp(v3Checksum));
  assert.doesNotMatch(publishV3, /set field_spec_checksum/);
  assert.match(renderer, /\.eq\("status", "published"\)\.eq\("aktiv", true\)/);
  for (const field of ["exporttitel", "stand", "exporttabelle"]) assert.match(spec.toString("utf8"), new RegExp(`\\"name\\": \\"${field}\\"`));
});

test("v3 upload uses relative local sources and explicit immutable Storage URIs", () => {
  assert.match(uploadScript, /Push-Location -LiteralPath \$templateDirectory/);
  assert.match(uploadScript, /storage cp 'template\.pdf' "\$target\/template\.pdf" --content-type application\/pdf/);
  assert.match(uploadScript, /storage cp 'field-spec\.json' "\$target\/field-spec\.json" --content-type application\/json/);
  assert.match(uploadScript, /Pop-Location/);
  assert.doesNotMatch(uploadScript, /storage cp \$pdf/);
});
