import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";
import test from "node:test";
const source = await readFile(new URL("./maintenance-contract-flow-service.ts", import.meta.url), "utf8");
test("WV has exactly the persistent member and two board positions", () => { for (const key of ["mitglied", "vorstand_1", "vorstand_2"]) assert.match(source, new RegExp(`key: "${key}"`)); assert.doesNotMatch(source, /lease\.member|second_party|verlaengerung/); });
test("WV progress and next-open navigation are based on persisted positions", () => { assert.match(source, /position\.status === "unterschrieben"/); assert.match(source, /positions\.find/); });
