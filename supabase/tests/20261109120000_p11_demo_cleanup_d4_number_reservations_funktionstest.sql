-- P11 Demo-Bereinigung D4: project-local Fachnummer keys and RPC contract.
begin;

create temporary table p11_d4_existing_reservations
on commit drop
as table public.fachnummer_reservierung;

do $$
declare
  v_columns smallint[];
begin
  select conkey into v_columns
  from pg_constraint
  where conrelid = 'public.fachnummer_reservierung'::regclass
    and conname = 'fachnummer_reservierung_pkey';
  if v_columns is distinct from array[
    (select attnum::smallint from pg_attribute
     where attrelid = 'public.fachnummer_reservierung'::regclass and attname = 'nummer')
  ] then
    raise exception 'D4 primary key is not nummer-only';
  end if;

  select conkey into v_columns
  from pg_constraint
  where conrelid = 'public.fachnummer_reservierung'::regclass
    and conname = 'fachnummer_reservierung_quelle_key';
  if v_columns is distinct from array[
    (select attnum::smallint from pg_attribute
     where attrelid = 'public.fachnummer_reservierung'::regclass and attname = 'quelle_art'),
    (select attnum::smallint from pg_attribute
     where attrelid = 'public.fachnummer_reservierung'::regclass and attname = 'quelle_id')
  ] then
    raise exception 'D4 source key is not (quelle_art, quelle_id)';
  end if;

  if to_regprocedure('public.reserve_fachnummer(text,text,bigint)') is null
     or to_regprocedure('public.reserve_fachnummer(text,text,bigint,boolean)') is not null then
    raise exception 'D4 reserve_fachnummer signature switch is incomplete';
  end if;
  if pg_get_viewdef('public.v_wartungsvertrag_fachnummer'::regclass, true) ilike '%is_demo%' then
    raise exception 'D4 maintenance number view still depends on is_demo';
  end if;
  if exists (
    select 1 from pg_indexes
    where schemaname = 'public' and tablename = 'fachnummer_reservierung'
      and indexdef ilike '%is_demo%'
  ) then
    raise exception 'D4 left a Demo-dependent Fachnummer uniqueness index';
  end if;
end;
$$;

select set_config('request.jwt.claim.role', 'service_role', true);

do $$
declare
  v_first integer;
  v_second integer;
begin
  v_first := public.reserve_fachnummer('mitgliedsantrag', 'kommunikationsvorgang', 94101);
  v_second := public.reserve_fachnummer('mitgliedsantrag', 'kommunikationsvorgang', 94101);
  if v_first is distinct from v_second or v_first not between 10000 and 19999 then
    raise exception 'D4 member application reservation is not idempotent or outside 10xxx';
  end if;

  v_first := public.reserve_fachnummer('pachtvertrag', 'kommunikationsvorgang', 94201);
  v_second := public.reserve_fachnummer('pachtvertrag', 'kommunikationsvorgang', 94201);
  if v_first is distinct from v_second or v_first not between 20000 and 29999 then
    raise exception 'D4 lease reservation is not idempotent or outside 20xxx';
  end if;

  v_first := public.reserve_fachnummer('wartungsvertrag', 'wartungsvertrag', 94901);
  v_second := public.reserve_fachnummer('wartungsvertrag', 'wartungsvertrag', 94901);
  if v_first is distinct from v_second or v_first not between 90000 and 99999 then
    raise exception 'D4 maintenance reservation is not idempotent or outside 90xxx';
  end if;

  begin
    perform public.reserve_fachnummer('wartungsvertrag', 'kommunikationsvorgang', 94201);
    raise exception 'D4 allowed one source to change its reservation kind';
  exception when sqlstate '22023' then null;
  end;
end;
$$;

-- Minimal registered historical contract for the real 80xxx RPC path.
insert into auth.users(
  id, aud, role, email, encrypted_password, email_confirmed_at,
  raw_app_meta_data, raw_user_meta_data, created_at, updated_at
) values (
  '00000000-0000-0000-0000-00000000d401'::uuid,
  'authenticated', 'authenticated', 'p11-d4-historical@example.test', '', now(),
  '{}'::jsonb, '{}'::jsonb, now(), now()
);
insert into public.mitglied(id, name, vorname, aktiv, aufnahme_status)
values (94801, 'D4', 'Historisch', true, 'aufgenommen');
insert into public.parzelle(id, garten_nr, "Anlage", aktiv)
values (94801, 'D4-801', 'D4', true);
insert into public.pachtverhaeltnis(id, parzelle_id, urspruenglicher_pachtbeginn)
values (94801, 94801, date '2010-01-01');
insert into public.pachtverhaeltnis_beteiligung(
  id, pachtverhaeltnis_id, mitglied_id, rolle, gueltig_ab
) values (94801, 94801, 94801, 'hauptpaechter', date '2010-01-01');
insert into public.dokument(
  id, mitglied_id, storage_path, titel, mime_type, historical_contract_type, created_by
) values (
  94801, 94801, 'd4/94801.pdf', 'D4 historischer Vertrag', 'application/pdf',
  'historischer_pv_folgevertrag', '00000000-0000-0000-0000-00000000d401'::uuid
);
update public.dokument
set dokumenttyp_id = (
      select id from public.dokumenttyp where code = 'historischer_pv_folgevertrag'
    ),
    dokumentdatum = date '2010-01-01'
where id = 94801;
insert into public.pachtvertragsfassung(
  id, pachtverhaeltnis_id, dokument_id, fassungstyp, wirksam_ab
) values (94801, 94801, 94801, 'import', date '2010-01-01');

do $$
declare
  v_first integer;
  v_second integer;
begin
  select nummer into v_first
  from public.reserve_historische_vertragsnummer(94801);
  select nummer into v_second
  from public.reserve_historische_vertragsnummer(94801);
  if v_first is distinct from v_second or v_first not between 80000 and 89999 then
    raise exception 'D4 historical reservation is not idempotent or outside 80xxx';
  end if;
  if (select historische_vertragsnummer from public.dokument where id = 94801) is distinct from v_first then
    raise exception 'D4 historical RPC did not persist its reserved number';
  end if;
end;
$$;

-- Isolate the number trigger from unrelated prepared-contract guards while
-- executing the real trg_reserve_vertragsnummer function.
alter table public.kommunikationsvorgang disable trigger user;
alter table public.kommunikationsvorgang enable trigger trg_reserve_vertragsnummer;
insert into public.kommunikationsvorgang(
  id, vorgangstyp, dokumenttyp, status, mitglied_id, entwurf_daten, revision
) values (94701, 'vertrag_signatur', 'mitgliedsantrag', 'vorbereitet', 94801, '{}'::jsonb, 1);
update public.kommunikationsvorgang set status = 'zur_signatur' where id = 94701;
do $$
begin
  if not exists (
    select 1 from public.fachnummer_reservierung
    where art = 'mitgliedsantrag' and quelle_art = 'kommunikationsvorgang'
      and quelle_id = 94701 and nummer between 10000 and 19999
  ) then
    raise exception 'D4 contract number trigger did not reserve 10xxx';
  end if;
end;
$$;
alter table public.kommunikationsvorgang enable trigger user;

do $$
declare
  v_number integer;
begin
  select nummer into v_number
  from public.fachnummer_reservierung
  where quelle_art = 'kommunikationsvorgang' and quelle_id = 94101;

  begin
    insert into public.fachnummer_reservierung(nummer, art, quelle_art, quelle_id)
    values (v_number, 'mitgliedsantrag', 'kommunikationsvorgang', 94102);
    raise exception 'D4 allowed one number for a different source';
  exception when unique_violation then null;
  end;

  begin
    insert into public.fachnummer_reservierung(nummer, art, quelle_art, quelle_id)
    values (19999, 'mitgliedsantrag', 'kommunikationsvorgang', 94101);
    raise exception 'D4 allowed a duplicate source binding';
  exception when unique_violation then null;
  end;
end;
$$;

-- The rows present before the test must be byte-for-byte unchanged.
do $$
begin
  if exists (
    (select * from p11_d4_existing_reservations
     except select * from public.fachnummer_reservierung)
    union all
    (select current_row.* from public.fachnummer_reservierung current_row
     join p11_d4_existing_reservations old_row
       on old_row.nummer = current_row.nummer
     except select * from p11_d4_existing_reservations)
  ) then
    raise exception 'D4 changed an existing Fachnummer reservation';
  end if;
end;
$$;

rollback;
