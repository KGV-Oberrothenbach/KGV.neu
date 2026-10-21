import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";
import test from "node:test";

test("W3 time model uses inclusive date boundaries and the three shared status terms", async () => {
  const source = await readFile(new URL("./WartungsvertragZeitstatus.cs", import.meta.url), "utf8");
  assert.match(source, /gueltigAb\.Date > today/);
  assert.match(source, /gueltigBis\.Value\.Date < today/);
  assert.match(source, /"Aktuell"/);
  assert.match(source, /"Zukünftig"/);
  assert.match(source, /"Historisch"/);
});
