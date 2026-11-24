import assert from "node:assert/strict";import test from "node:test";import { canUseSerialLetters } from "./serienbrief-access.ts";
test("FR2-09: Serienbrief-Erstellung is independent of Vorgang bearbeiten and old roles",()=>{
  const context=(effectivePermissions:number)=>({role:"admin" as const,effectivePermissions});
  assert.equal(canUseSerialLetters(context(0)),false);
  assert.equal(canUseSerialLetters(context(2**30)),false);
  assert.equal(canUseSerialLetters(context(2**32)),true);
  assert.equal(canUseSerialLetters({role:"user",effectivePermissions:2**32}),true);
  assert.equal(canUseSerialLetters(null),false);
});
