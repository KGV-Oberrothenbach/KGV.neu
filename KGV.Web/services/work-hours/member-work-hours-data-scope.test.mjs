import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";
import test from "node:test";
import ts from "typescript";

async function loadTypeScriptModule(path, replacements = []) {
  let source = await readFile(new URL(path, import.meta.url), "utf8");
  for (const [specifier, replacement] of replacements) {
    source = source.replaceAll(JSON.stringify(specifier), JSON.stringify(replacement));
  }
  const { outputText } = ts.transpileModule(source, {
    compilerOptions: { module: ts.ModuleKind.ESNext, target: ts.ScriptTarget.ES2022 },
  });
  return `data:text/javascript;base64,${Buffer.from(outputText).toString("base64")}`;
}

test("member work-hours requests keep member and season filters without bypassing RLS demo scope", async () => {
  const supabaseAuthUrl = await loadTypeScriptModule("../../lib/supabase-auth.ts");
  const repositoryUrl = await loadTypeScriptModule(
    "../../repositories/work-hours/work-hours-repository.ts",
    [["../../lib/supabase-auth", supabaseAuthUrl]],
  );
  const serviceUrl = await loadTypeScriptModule(
    "./work-hours-service.ts",
    [["../../repositories/work-hours/work-hours-repository", repositoryUrl]],
  );
  const { readSupabase } = await import(supabaseAuthUrl);
  const { loadMemberWorkHoursWorkspace } = await import(serviceUrl);
  const requests = [];

  globalThis.window = {
    localStorage: {
      getItem(key) {
        assert.equal(key, "kgv.browser.club.v1");
        return JSON.stringify({
          vereinsCode: "test",
          vereinsname: "Testverein",
          supabaseUrl: "https://example.supabase.co",
          supabasePublishableKey: "test-key",
        });
      },
    },
  };
  globalThis.fetch = async (input) => {
    requests.push(new URL(input));
    return new Response("[]", { status: 200, headers: { "content-type": "application/json" } });
  };

  const session = { accessToken: "test-token", refreshToken: "test-refresh", expiresAt: 0, user: { id: "test-user" }, isDemoAccount: false };
  await loadMemberWorkHoursWorkspace(session, 314, 2026);

  const workHoursRequest = requests.find((request) => request.pathname === "/rest/v1/arbeitsstunde");
  assert.ok(workHoursRequest, "the member work-hours repository should request arbeitsstunde");
  assert.deepEqual(Object.fromEntries(workHoursRequest.searchParams), {
    select: "id,mitglied_id,saison_id,datum,stunden,art_der_arbeit,status,freigegeben,genehmigt_von,genehmigt_am,arbeitseinsatz_anmeldung_id",
    mitglied_id: "eq.314",
    saison_id: "eq.2026",
    order: "datum.desc,id.desc",
    limit: "500",
  });

  await readSupabase(session, "mitglied", { select: "id", id: "eq.314" });
  const memberRequest = requests.find((request) => request.pathname === "/rest/v1/mitglied");
  assert.equal(memberRequest.searchParams.get("is_demo"), "eq.false");

  await readSupabase({ ...session, isDemoAccount: true }, "mitglied", { select: "id", id: "eq.314" });
  const demoMemberRequest = requests.filter((request) => request.pathname === "/rest/v1/mitglied").at(-1);
  assert.equal(demoMemberRequest.searchParams.get("is_demo"), "eq.true");

  const component = await readFile(new URL("../../features/work-hours/MemberWorkHours.tsx", import.meta.url), "utf8");
  assert.match(component, /loadMemberWorkHoursWorkspace\(session, memberId, saisonId\)/);
  const page = await readFile(new URL("../../app/page.tsx", import.meta.url), "utf8");
  assert.match(page, /activeId === "mitglied-arbeitsstunden" && selectedMemberId && <MemberWorkHours session=\{session\} memberId=\{selectedMemberId\} saisonId=\{workspaceContext\.saisonId\}/);
});
