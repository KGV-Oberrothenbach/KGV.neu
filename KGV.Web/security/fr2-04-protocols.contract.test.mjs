import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import test from 'node:test';
const root = new URL('../../', import.meta.url);
const load = (p) => readFileSync(new URL(p, root), 'utf8');

test('FR2-04.4 protocol navigation uses the separate begehung right', () => {
  const web = load('KGV.Web/app/page.tsx');
  const shell = load('KGV.Maui/AdminShell.cs');
  const maui = load('KGV.Maui/Pages/ParzellenProtokollePage.cs');
  assert.match(web, /canEditMemberProtocols: hasRight\("begehung\.verwalten"\)/);
  assert.match(web, /selectedMemberId && hasRight\("begehung\.verwalten"\) && <ParcelProtocolsWorkspace/);
  assert.match(shell, /_memberProtokolleItem\.IsVisible = hasMember && PermissionChecks\.CanManageBegehungen/);
  assert.match(maui, /private async Task<bool> SaveDraftAsync\(\)[\s\S]*?CanManageBegehungen/);
  assert.match(maui, /private async Task CreatePdfAsync\(\)[\s\S]*?CanManageBegehungen/);
});

test('FR2-04.4 contract edge protects protocol finalization and object context', () => {
  const edge = load('supabase/functions/kgv-generate-contract/index.ts');
  assert.match(edge, /type === "parzellenprotokoll" \? "begehung\.verwalten"/);
  assert.match(edge, /Number\(protocol\.parzelle_id\) !== parcelId/);
  assert.match(edge, /Number\(protocol\.vorstand2_mitglied_id\) !== board2Id/);
  assert.match(edge, /protocol\.status !== "entwurf"/);
  assert.match(edge, /protocol\.dokument_id != null/);
  assert.match(edge, /safe\(protocol\.protokoll_typ\) !== safe\(body\.protocol_type\)/);
  assert.match(edge, /\.eq\("status", "entwurf"\)\.is\("dokument_id", null\)/);
  assert.match(edge, /const forbidden = rawMessage\.startsWith\("AUTH:"\)/);
});

test('FR2-04.4 web persists current protocol draft before generating final PDF', () => {
  const web = load('KGV.Web/app/parcel-workspaces.tsx');
  assert.match(web, /const id = await saveDraft\(\); if \(!id \|\| !parcel\) return;/);
  assert.doesNotMatch(web, /draftId \?\? await saveDraft\(\)/);
});
