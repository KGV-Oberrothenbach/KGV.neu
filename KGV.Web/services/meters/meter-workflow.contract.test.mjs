import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";
import test from "node:test";

const read = (path) => readFile(new URL(path, import.meta.url), "utf8");

test("meter workflow RPCs are connected from productive Web and MAUI screens", async () => {
  const [migration, leaseFix, page, webService, repository, webRequests, navigation, mauiReading, mauiChange, mauiRequests, mauiService, shell] = await Promise.all([
    read("../../../supabase/migrations/20261030150000_meter_workflow_unification.sql"),
    read("../../../supabase/migrations/20261030151000_meter_change_request_lease_date.sql"),
    read("../../app/page.tsx"),
    read("meter-change-service.ts"),
    read("../../repositories/meters/meter-change-repository.ts"),
    read("../../features/meters/MeterChangeRequests.tsx"),
    read("../../services/workspace/navigation-service.ts"),
    read("../../../KGV.Maui/Pages/AblesungErfassenPage.cs"),
    read("../../../KGV.Maui/Pages/ZaehlerwechselAusbauPage.cs"),
    read("../../../KGV.Maui/Pages/MeterChangeRequestPages.cs"),
    read("../../../KGV.Infrastructure/Services/SupabaseService.cs"),
    read("../../../KGV.Maui/UserShell.cs"),
  ]);

  // Server authority and effective lease date.
  assert.match(migration, /execute_meter_change[\s\S]*for update[\s\S]*insert into public\.zaehler_ablesung[\s\S]*update public\.zaehler[\s\S]*insert into public\.zaehler/);
  assert.match(migration, /review_meter_change_request[\s\S]*execute_meter_change/);
  assert.match(leaseFix, /current_user_has_active_lease_for_parcel\(v_old\.parzelle_id,\s*p_ausbaudatum\)/);

  // Productive Web route -> service -> repository -> RPC, not LegacyMeterChange.
  assert.match(page, /<MeterChange session=.*canManageMeterChanges/);
  assert.doesNotMatch(page, /LegacyMeterChange/);
  assert.doesNotMatch(page, /completeMeterRemoval\(/);
  assert.doesNotMatch(page, /saveMeterRemovalReading\(/);
  assert.match(page, /performMeterChange\(/);
  assert.match(webService, /executeMeterChange\(/);
  assert.match(repository, /execute_meter_change/);
  assert.match(page, /<MemberMeterChangeRequest/);
  assert.match(page, /<MeterChangeRequestReview/);
  assert.match(navigation, /requestMeterChange[\s\S]*reviewMeterChanges/);
  assert.match(webRequests, /submit_own_meter_change_request/);
  assert.match(webRequests, /correct_meter_change_request/);
  assert.match(webRequests, /review_meter_change_request/);

  // Productive MAUI entry points -> service -> RPC.
  assert.match(mauiReading, /CaptureMeterReadingAsync\(/);
  assert.match(mauiChange, /ExecuteMeterChangeAsync\(/);
  assert.match(mauiRequests, /SubmitOwnMeterChangeRequestAsync\(/);
  assert.match(mauiRequests, /CorrectMeterChangeRequestAsync\(/);
  assert.match(mauiRequests, /ReviewMeterChangeRequestAsync\(/);
  assert.match(mauiRequests, /GetPachtbeziehungenForMitgliedAsync/);
  assert.match(mauiRequests, /GetActiveMeterCandidatesAsync/);
  assert.match(mauiRequests, /async Task RefreshMedia\(\)/);
  assert.match(mauiRequests, /_medium\.ItemsSource = media/);
  assert.match(mauiRequests, /_oldMeterId = null/);
  assert.match(mauiRequests, /contextVersion != _contextVersion/);
  assert.doesNotMatch(mauiRequests, /Placeholder="Alter Zähler-ID"/);
  assert.match(shell, /MeterChangeRequestPage/);
  assert.match(shell, /MeterChangeRequestReviewPage/);
  assert.match(mauiService, /"submit_own_meter_change_request"/);
  assert.match(mauiService, /"correct_meter_change_request"/);
  assert.match(mauiService, /"review_meter_change_request"/);
  assert.match(mauiService, /"execute_meter_change"/);
});
