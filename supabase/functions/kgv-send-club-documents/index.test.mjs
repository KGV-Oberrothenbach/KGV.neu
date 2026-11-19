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
test("P11 FIX-04: effective club document permission guards privileged send", async () => {
  const source = await readFile(new URL("./index.ts", import.meta.url), "utf8");
  const config = await readFile(new URL("../../config.toml", import.meta.url), "utf8");

  const authentication = source.indexOf("caller.auth.getUser()");
  const permission = source.indexOf('caller.rpc("has_effective_permission"');
  const memberLookup = source.indexOf('admin.from("mitglied")');

  assert.ok(authentication >= 0, "authenticated identity required");
  assert.ok(permission > authentication, "effective permission checked after identity");
  assert.ok(memberLookup > permission, "permission checked before privileged member access");

  assert.match(source, /permissionError\s*\|\|\s*permitted\s*!==\s*true/);
  assert.match(source, /Keine Berechtigung\./);
  assert.doesNotMatch(source, /item\.role\s*===\s*["'](?:admin|vorstand)["']/);

  const section = config.match(
    /\[functions\.kgv-send-club-documents\]([\s\S]*?)(?=\n\[|$)/
  );
  assert.ok(section, "function configuration exists");
  assert.match(section[1], /verify_jwt\s*=\s*true/);
});