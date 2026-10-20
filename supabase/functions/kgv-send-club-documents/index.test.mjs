import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";
import test from "node:test";

test("optional SMTP_TEST_RECIPIENT replaces only the technical SMTP recipient", async () => {
  const source = await readFile(new URL("./index.ts", import.meta.url), "utf8");
  assert.match(source, /const testRecipient = String\(Deno\.env\.get\("SMTP_TEST_RECIPIENT"\) \?\? ""\)\.trim\(\)/);
  assert.match(source, /const recipient = testRecipient \|\| originalRecipient/);
  assert.match(source, /\[DEMO TEST\]/);
  assert.match(source, /Ursprünglich vorgesehener Empfänger/);
  assert.match(source, /to: recipient[\s\S]*attachments/);
  assert.match(source, /accepted[\s\S]*recipient\.toLowerCase/);
});
