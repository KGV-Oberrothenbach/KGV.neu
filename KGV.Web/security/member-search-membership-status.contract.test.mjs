import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";

const service = await readFile(new URL("../services/members/member-service.ts", import.meta.url), "utf8");
const repository = await readFile(new URL("../repositories/members/member-repository.ts", import.meta.url), "utf8");
const maui = await readFile(new URL("../../KGV.Maui/ViewModels/MemberSearchViewModel.cs", import.meta.url), "utf8");

assert.match(service, /member\.aufnahme_status === "aufgenommen"/);
assert.match(service, /member\.mitglied_seit && member\.mitglied_seit <= today/);
assert.match(service, /member\.mitglied_ende && member\.mitglied_ende < today/);
assert.match(service, /member\.aufnahme_status === "antrag_offen"/);
assert.doesNotMatch(service, /includeFormer \|\| member\.aktiv/);
assert.match(repository, /aufnahme_status,mitglied_seit,mitglied_ende/);
assert.match(maui, /GetMitgliederForSearchAsync/);
assert.match(maui, /ShowFormerMembers/);
assert.match(maui, /m\.IsCurrentMember \|\| m\.IsOpenApplication/);
