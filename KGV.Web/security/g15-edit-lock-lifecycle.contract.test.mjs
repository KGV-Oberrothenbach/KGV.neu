import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";
import test from "node:test";

test("MAUI work-assignment editor cannot re-enable writes after losing its lock", async () => {
  const editor = await readFile(new URL("../../KGV.Maui/Pages/ArbeitseinsaetzeEditorPage.cs", import.meta.url), "utf8");
  const saveStart = editor.slice(editor.indexOf("private async Task<bool> SaveAsync"), editor.indexOf("private async Task CompleteSuccessfulSaveAsync"));
  const enabledState = editor.slice(editor.indexOf("private void SetEnabledState"), editor.indexOf("private Task NavigateToOverviewAsync"));
  const refresh = editor.slice(editor.indexOf("private async void OnEditLockRefresh"), editor.indexOf("private async Task ReleaseEditLockAsync"));

  assert.match(editor, /using Microsoft\.Maui\.Dispatching;/);
  assert.match(saveStart, /_editingEntryId\.HasValue && !_hasEditLock/);
  assert.match(enabledState, /var canEdit = enabled && \(!_editingEntryId\.HasValue \|\| _hasEditLock\)/);
  assert.match(enabledState, /_saveButton\.IsEnabled = canEdit/);
  assert.doesNotMatch(refresh, /_isBusy/);
  assert.match(refresh, /AcquireEditLockAsync\(\)/);
  assert.match(refresh, /_hasEditLock = false/);
});
