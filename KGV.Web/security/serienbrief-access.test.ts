import assert from "node:assert/strict";import test from "node:test";import { canUseSerialLetters } from "./serienbrief-access.ts";
test("allows only admin and board",()=>{assert.equal(canUseSerialLetters({role:"admin"} as any),true);assert.equal(canUseSerialLetters({role:"vorstand"} as any),true);assert.equal(canUseSerialLetters({role:"user"} as any),false);});
