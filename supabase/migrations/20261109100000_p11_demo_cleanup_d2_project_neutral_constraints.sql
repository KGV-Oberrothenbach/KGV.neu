begin;

-- D2 keeps the legacy columns temporarily for D3/D5 compatibility, but the
-- project itself is now the only data boundary.  These validated constraints
-- make the neutral value a database invariant even when trigger execution is
-- bypassed by maintenance tooling.

alter table public.app_user
  add constraint app_user_d2_project_neutral_demo_check
  check (is_demo_account = false);

alter table public.auth_allowlist
  add constraint auth_allowlist_d2_project_neutral_demo_check
  check (allow_demo_access = false);

alter table public.arbeitseinsatz
  add constraint arbeitseinsatz_d2_project_neutral_demo_check
  check (is_demo = false);

alter table public.bekanntmachung
  add constraint bekanntmachung_d2_project_neutral_demo_check
  check (is_demo = false);

alter table public.historical_contract_operation
  add constraint historical_contract_operation_d2_project_neutral_demo_check
  check (is_demo = false);

alter table public.mitglied
  add constraint mitglied_d2_project_neutral_demo_check
  check (is_demo = false);

alter table public.parzelle
  add constraint parzelle_d2_project_neutral_demo_check
  check (is_demo = false);

alter table public.saison
  add constraint saison_d2_project_neutral_demo_check
  check (is_demo = false);

alter table public.termin
  add constraint termin_d2_project_neutral_demo_check
  check (is_demo = false);

alter table public.wartungsvertraege
  add constraint wartungsvertraege_d2_project_neutral_demo_check
  check (is_demo = false);

commit;
