-- P11 Demo-Bereinigung D2
-- Projekttrennungen ersetzen den datensatzbezogenen Demo-Scope. Die Spalten
-- bleiben bis D5 aus Kompatibilitaetsgruenden erhalten, werden aber ausser bei
-- der in D4 separat migrierten Fachnummernverwaltung neutral gehalten.

begin;

-- Bestehende Marker ausserhalb der konfliktkritischen Nummernverwaltung
-- neutralisieren. Es werden keine fachlichen Zeilen geloescht oder vereinigt.
update public.app_user set is_demo_account = false where is_demo_account is distinct from false;
update public.auth_allowlist set allow_demo_access = false where allow_demo_access is distinct from false;
update public.arbeitseinsatz set is_demo = false where is_demo is distinct from false;
update public.bekanntmachung set is_demo = false where is_demo is distinct from false;
update public.historical_contract_operation set is_demo = false where is_demo is distinct from false;
update public.mitglied set is_demo = false where is_demo is distinct from false;
update public.parzelle set is_demo = false where is_demo is distinct from false;
update public.saison set is_demo = false where is_demo is distinct from false;
update public.termin set is_demo = false where is_demo is distinct from false;
update public.wartungsvertraege set is_demo = false where is_demo is distinct from false;

alter table public.app_user alter column is_demo_account set default false;
alter table public.auth_allowlist alter column allow_demo_access set default false;
alter table public.arbeitseinsatz alter column is_demo set default false;
alter table public.bekanntmachung alter column is_demo set default false;
alter table public.historical_contract_operation alter column is_demo set default false;
alter table public.mitglied alter column is_demo set default false;
alter table public.parzelle alter column is_demo set default false;
alter table public.saison alter column is_demo set default false;
alter table public.termin alter column is_demo set default false;
alter table public.wartungsvertraege alter column is_demo set default false;

create or replace function public.p11_d2_force_is_demo_false()
returns trigger
language plpgsql
set search_path = public, pg_temp
as $$
begin
  new.is_demo := false;
  return new;
end;
$$;

create or replace function public.p11_d2_force_app_user_demo_false()
returns trigger
language plpgsql
set search_path = public, pg_temp
as $$
begin
  new.is_demo_account := false;
  return new;
end;
$$;

create or replace function public.p11_d2_force_allow_demo_access_false()
returns trigger
language plpgsql
set search_path = public, pg_temp
as $$
begin
  new.allow_demo_access := false;
  return new;
end;
$$;

drop trigger if exists p11_d2_project_neutral_scope on public.app_user;
create trigger p11_d2_project_neutral_scope before insert or update of is_demo_account on public.app_user
for each row execute function public.p11_d2_force_app_user_demo_false();

drop trigger if exists p11_d2_project_neutral_scope on public.auth_allowlist;
create trigger p11_d2_project_neutral_scope before insert or update of allow_demo_access on public.auth_allowlist
for each row execute function public.p11_d2_force_allow_demo_access_false();

drop trigger if exists p11_d2_project_neutral_scope on public.arbeitseinsatz;
create trigger p11_d2_project_neutral_scope before insert or update of is_demo on public.arbeitseinsatz
for each row execute function public.p11_d2_force_is_demo_false();
drop trigger if exists p11_d2_project_neutral_scope on public.bekanntmachung;
create trigger p11_d2_project_neutral_scope before insert or update of is_demo on public.bekanntmachung
for each row execute function public.p11_d2_force_is_demo_false();
drop trigger if exists p11_d2_project_neutral_scope on public.historical_contract_operation;
create trigger p11_d2_project_neutral_scope before insert or update of is_demo on public.historical_contract_operation
for each row execute function public.p11_d2_force_is_demo_false();
drop trigger if exists p11_d2_project_neutral_scope on public.mitglied;
create trigger p11_d2_project_neutral_scope before insert or update of is_demo on public.mitglied
for each row execute function public.p11_d2_force_is_demo_false();
drop trigger if exists p11_d2_project_neutral_scope on public.parzelle;
create trigger p11_d2_project_neutral_scope before insert or update of is_demo on public.parzelle
for each row execute function public.p11_d2_force_is_demo_false();
drop trigger if exists p11_d2_project_neutral_scope on public.saison;
create trigger p11_d2_project_neutral_scope before insert or update of is_demo on public.saison
for each row execute function public.p11_d2_force_is_demo_false();
drop trigger if exists p11_d2_project_neutral_scope on public.termin;
create trigger p11_d2_project_neutral_scope before insert or update of is_demo on public.termin
for each row execute function public.p11_d2_force_is_demo_false();
drop trigger if exists p11_d2_project_neutral_scope on public.wartungsvertraege;
create trigger p11_d2_project_neutral_scope before insert or update of is_demo on public.wartungsvertraege
for each row execute function public.p11_d2_force_is_demo_false();

-- Historische Helfersignaturen bleiben bis D5 fuer bestehende Aufrufer
-- erhalten, treffen jedoch keine datensatzbezogene Demo-Entscheidung mehr.
create or replace function public.is_demo_user() returns boolean
language sql stable security definer set search_path = public, pg_temp
as $$ select false; $$;
create or replace function public.is_playstore_reviewer() returns boolean
language sql stable security definer set search_path = public, pg_temp
as $$ select false; $$;
create or replace function public.is_demo_or_reviewer() returns boolean
language sql stable security definer set search_path = public, pg_temp
as $$ select false; $$;
create or replace function public.can_access_demo_scope() returns boolean
language sql stable security definer set search_path = public, pg_temp
as $$ select true; $$;
create or replace function public.is_productive_admin_or_vorstand() returns boolean
language sql stable security definer set search_path = public, pg_temp
as $$ select public.is_admin_or_vorstand(); $$;
create or replace function public.is_restricted_demo_admin_or_vorstand() returns boolean
language sql stable security definer set search_path = public, pg_temp
as $$ select false; $$;
create or replace function public.is_demo_app_user_row(p_mitglied_id bigint, p_is_demo_account boolean) returns boolean
language sql stable security definer set search_path = public, pg_temp
as $$ select false; $$;
create or replace function public.is_demo_mitglied_id(p_mitglied_id bigint) returns boolean
language sql stable security definer set search_path = public, pg_temp
as $$ select false; $$;
create or replace function public.is_demo_parzelle_id(p_parzelle_id bigint) returns boolean
language sql stable security definer set search_path = public, pg_temp
as $$ select false; $$;
create or replace function public.is_demo_zaehler_id(p_zaehler_id bigint) returns boolean
language sql stable security definer set search_path = public, pg_temp
as $$ select false; $$;
create or replace function public.is_demo_dokument_scope(p_mitglied_id bigint, p_parzelle_id bigint) returns boolean
language sql stable security definer set search_path = public, pg_temp
as $$ select false; $$;
create or replace function public.is_demo_member_parzelle_scope(p_mitglied_id bigint, p_parzelle_id bigint) returns boolean
language sql stable security definer set search_path = public, pg_temp
as $$ select false; $$;
create or replace function public.is_demo_member_arbeitseinsatz_scope(p_mitglied_id bigint, p_arbeitseinsatz_id bigint) returns boolean
language sql stable security definer set search_path = public, pg_temp
as $$ select false; $$;
create or replace function public.is_demo_member_wartungsvertrag_scope(p_hauptmitglied_id bigint, p_wartungsvertrag_id bigint) returns boolean
language sql stable security definer set search_path = public, pg_temp
as $$ select false; $$;
create or replace function public.is_demo_impressum_slot_scope(p_mitglied_id bigint) returns boolean
language sql stable security definer set search_path = public, pg_temp
as $$ select false; $$;

-- Datensatzbezogene Demo-Trigger entfallen; normale Fachvalidierungen bleiben.
drop trigger if exists termin_enforce_demo_scope on public.termin;
drop trigger if exists bekanntmachung_enforce_demo_scope on public.bekanntmachung;
create or replace function public.enforce_appointment_announcement_demo_scope()
returns trigger language plpgsql set search_path = public, pg_temp
as $$ begin new.is_demo := false; return new; end; $$;

-- Arbeitseinsaetze und Arbeitsstunden: Rechte, Eigenbezug, Status und
-- Zeitfenster bleiben erhalten; nur die Demo-Kopplung wird entfernt.
drop policy if exists arbeitseinsatz_manage_delete on public.arbeitseinsatz;
create policy arbeitseinsatz_manage_delete on public.arbeitseinsatz for delete to authenticated
using (public.has_effective_permission(1048576));
drop policy if exists arbeitseinsatz_manage_insert on public.arbeitseinsatz;
create policy arbeitseinsatz_manage_insert on public.arbeitseinsatz for insert to authenticated
with check (public.has_effective_permission(1048576));
drop policy if exists arbeitseinsatz_manage_select on public.arbeitseinsatz;
create policy arbeitseinsatz_manage_select on public.arbeitseinsatz for select to authenticated
using (public.has_effective_permission(1048576));
drop policy if exists arbeitseinsatz_manage_update on public.arbeitseinsatz;
create policy arbeitseinsatz_manage_update on public.arbeitseinsatz for update to authenticated
using (public.has_effective_permission(1048576)) with check (public.has_effective_permission(1048576));
drop policy if exists arbeitseinsatz_select_visible_authenticated on public.arbeitseinsatz;
create policy arbeitseinsatz_select_visible_authenticated on public.arbeitseinsatz for select to authenticated
using (aktiv = true and (sichtbar_ab is null or sichtbar_ab <= public.kgv_local_now()) and (sichtbar_bis is null or sichtbar_bis >= public.kgv_local_now()));

drop policy if exists arbeitseinsatz_anmeldung_manage_select on public.arbeitseinsatz_anmeldung;
create policy arbeitseinsatz_anmeldung_manage_select on public.arbeitseinsatz_anmeldung for select to authenticated
using (public.has_effective_permission(1048576));
drop policy if exists arbeitseinsatz_anmeldung_select_own on public.arbeitseinsatz_anmeldung;
create policy arbeitseinsatz_anmeldung_select_own on public.arbeitseinsatz_anmeldung for select to authenticated
using (mitglied_id = public.current_mitglied_id());

drop policy if exists arbeitsstunde_insert_own_open on public.arbeitsstunde;
create policy arbeitsstunde_insert_own_open on public.arbeitsstunde for insert to authenticated
with check (mitglied_id = public.current_mitglied_id() and status = 'offen' and freigegeben = false and genehmigt_von is null and genehmigt_am is null and arbeitseinsatz_anmeldung_id is null);
drop policy if exists arbeitsstunde_manage_work_hours_insert on public.arbeitsstunde;
create policy arbeitsstunde_manage_work_hours_insert on public.arbeitsstunde for insert to authenticated
with check (public.has_effective_permission(256) and arbeitseinsatz_anmeldung_id is null and status = 'genehmigt' and freigegeben = true and genehmigt_von = public.current_mitglied_id() and genehmigt_am is not null);
drop policy if exists arbeitsstunde_manage_work_hours_select on public.arbeitsstunde;
create policy arbeitsstunde_manage_work_hours_select on public.arbeitsstunde for select to authenticated
using (public.has_effective_permission(256));
drop policy if exists arbeitsstunde_select_own_or_admin on public.arbeitsstunde;
create policy arbeitsstunde_select_own_or_admin on public.arbeitsstunde for select to authenticated
using (public.has_effective_permission(256) or mitglied_id = public.current_mitglied_id());
drop policy if exists arbeitsstunde_update_own_open on public.arbeitsstunde;
create policy arbeitsstunde_update_own_open on public.arbeitsstunde for update to authenticated
using (public.current_mitglied_id() is not null and mitglied_id = public.current_mitglied_id() and status = 'offen' and freigegeben = false and genehmigt_von is null and genehmigt_am is null)
with check (public.current_mitglied_id() is not null and mitglied_id = public.current_mitglied_id() and status = 'offen' and freigegeben = false and genehmigt_von is null and genehmigt_am is null);
drop policy if exists arbeitsstunde_pruefverlauf_select_scoped on public.arbeitsstunde_pruefverlauf;
create policy arbeitsstunde_pruefverlauf_select_scoped on public.arbeitsstunde_pruefverlauf for select to authenticated
using (
  public.has_effective_permission(256)
  or (coalesce(vorher_snapshot->>'mitglied_id', vorher_snapshot->>'MitgliedId', '') ~ '^[0-9]+$'
      and coalesce(vorher_snapshot->>'mitglied_id', vorher_snapshot->>'MitgliedId')::integer = public.current_mitglied_id())
);

-- Termine und Bekanntmachungen.
drop policy if exists termin_manage_insert on public.termin;
create policy termin_manage_insert on public.termin for insert to authenticated with check (public.has_effective_permission(2097152));
drop policy if exists termin_manage_select on public.termin;
create policy termin_manage_select on public.termin for select to authenticated using (public.has_effective_permission(2097152));
drop policy if exists termin_manage_update on public.termin;
create policy termin_manage_update on public.termin for update to authenticated using (public.has_effective_permission(2097152)) with check (public.has_effective_permission(2097152));
drop policy if exists termin_select_visible_authenticated on public.termin;
create policy termin_select_visible_authenticated on public.termin for select to authenticated
using (aktiv = true and (sichtbar_ab is null or sichtbar_ab <= public.kgv_local_now()) and (sichtbar_bis is null or sichtbar_bis >= public.kgv_local_now()));

drop policy if exists bekanntmachung_manage_insert on public.bekanntmachung;
create policy bekanntmachung_manage_insert on public.bekanntmachung for insert to authenticated with check (public.has_effective_permission(4194304));
drop policy if exists bekanntmachung_manage_select on public.bekanntmachung;
create policy bekanntmachung_manage_select on public.bekanntmachung for select to authenticated using (public.has_effective_permission(4194304));
drop policy if exists bekanntmachung_manage_update on public.bekanntmachung;
create policy bekanntmachung_manage_update on public.bekanntmachung for update to authenticated using (public.has_effective_permission(4194304)) with check (public.has_effective_permission(4194304));
drop policy if exists bekanntmachung_select_visible_authenticated on public.bekanntmachung;
create policy bekanntmachung_select_visible_authenticated on public.bekanntmachung for select to authenticated
using (aktiv = true and (sichtbar_ab is null or sichtbar_ab <= public.kgv_local_now()) and (sichtbar_bis is null or sichtbar_bis >= public.kgv_local_now()));

-- Demo-only Vollzugriffspolicies entfallen. Die bestehenden fachlichen
-- Admin-/Effektivrechtspolicies bleiben unveraendert aktiv.
drop policy if exists dokument_demo_admin_full on public.dokument;
drop policy if exists impressum_funktion_slot_demo_admin_full on public.impressum_funktion_slot;
drop policy if exists mitglied_demo_admin_full on public.mitglied;
drop policy if exists mitglied_saison_demo_admin_full on public.mitglied_saison;
drop policy if exists parzelle_demo_admin_full on public.parzelle;
drop policy if exists parzellen_begehung_feststellung_demo_admin_full on public.parzellen_begehung_feststellung;
drop policy if exists parzellen_belegung_demo_admin_full on public.parzellen_belegung;
drop policy if exists parzellen_protokoll_demo_admin_full on public.parzellen_protokoll;
drop policy if exists parzellen_protokoll_ablesung_demo_admin_full on public.parzellen_protokoll_ablesung;
drop policy if exists wartungsvertraege_demo_admin_full on public.wartungsvertraege;
drop policy if exists wartungsvertrag_zuordnungen_demo_admin_full on public.wartungsvertrag_zuordnungen;
drop policy if exists zaehler_demo_admin_full on public.zaehler;
drop policy if exists zaehler_ablesung_demo_admin_full on public.zaehler_ablesung;

drop policy if exists dokument_select_effective on public.dokument;
create policy dokument_select_effective on public.dokument for select to authenticated
using (
  public.has_effective_permission(65536) or public.has_effective_permission(16)
  or mitglied_id = public.current_mitglied_id()
  or (parzelle_id is not null and exists (
    select 1 from public.parzellen_belegung b
    where b.parzelle_id = dokument.parzelle_id
      and b.mitglied_id = public.current_mitglied_id()
      and (b.von_datum is null or b.von_datum <= now()::date)
      and (b.bis_datum is null or b.bis_datum >= now()::date)
  ))
);
drop policy if exists impressum_funktion_slot_select_authenticated on public.impressum_funktion_slot;
create policy impressum_funktion_slot_select_authenticated on public.impressum_funktion_slot for select to authenticated using (true);
drop policy if exists mitglied_select_own_or_admin on public.mitglied;
create policy mitglied_select_own_or_admin on public.mitglied for select to authenticated
using (public.is_productive_admin_or_vorstand() or id = public.current_mitglied_id() or (auth_user_id is not null and auth_user_id = auth.uid()));
drop policy if exists mitglied_saison_select_own_or_admin on public.mitglied_saison;
create policy mitglied_saison_select_own_or_admin on public.mitglied_saison for select to authenticated
using (public.is_productive_admin_or_vorstand() or mitglied_id = public.current_mitglied_id());
drop policy if exists parzelle_select_assigned_or_admin on public.parzelle;
create policy parzelle_select_assigned_or_admin on public.parzelle for select to authenticated
using (public.is_productive_admin_or_vorstand() or exists (select 1 from public.parzellen_belegung pb where pb.parzelle_id = parzelle.id and pb.mitglied_id = public.current_mitglied_id()));
drop policy if exists parzellen_belegung_select_own_or_admin on public.parzellen_belegung;
create policy parzellen_belegung_select_own_or_admin on public.parzellen_belegung for select to authenticated
using (public.is_productive_admin_or_vorstand() or mitglied_id = public.current_mitglied_id());
drop policy if exists wartungsvertraege_select_assigned_or_admin on public.wartungsvertraege;
create policy wartungsvertraege_select_assigned_or_admin on public.wartungsvertraege for select to authenticated
using (public.is_productive_admin_or_vorstand() or exists (select 1 from public.wartungsvertrag_zuordnungen wz where wz.wartungsvertrag_id = wartungsvertraege.id and wz.hauptmitglied_id = public.current_mitglied_id()));
drop policy if exists wartungsvertrag_zuordnungen_select_own_or_admin on public.wartungsvertrag_zuordnungen;
create policy wartungsvertrag_zuordnungen_select_own_or_admin on public.wartungsvertrag_zuordnungen for select to authenticated
using (public.is_productive_admin_or_vorstand() or hauptmitglied_id = public.current_mitglied_id());

commit;
