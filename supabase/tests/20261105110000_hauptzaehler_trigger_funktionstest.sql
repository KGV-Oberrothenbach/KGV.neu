-- G3.5-RPC-Funktionstest – ausschließlich auf einer Test-/DemoDB ausführen.
-- Der Test erzeugt eindeutig gekennzeichnete Daten und rollt sie vollständig
-- zurück. Er richtet dafür innerhalb der Transaktion einen temporären
-- Demo-App-Benutzer mit CanReadMeters (32) und CanManageMeters (64) ein und
-- setzt dessen JWT-Claims lokal. Es werden weder Auth-Benutzer noch dauerhafte
-- app_user- oder Zählerdaten geschrieben.

begin;

do $$
declare
  v_strom_nord_alt bigint;
  v_strom_sued bigint;
  v_wasser_nord bigint;
  v_wasser_sued bigint;
  v_strom_nord_neu bigint;
  v_reading public.zaehler_ablesung%rowtype;
  v_messstelle_id bigint;
  v_rfid_tag_uid text;
  v_count integer;
  v_test_user_id uuid := gen_random_uuid();
  v_test_mitglied_id bigint;
begin
  -- current_app_user_context verlangt einen realen Mitglied-/app_user-Link.
  -- Alle drei Testzeilen existieren ausschließlich bis zum ROLLBACK.
  insert into auth.users
    (id, aud, role, email, encrypted_password, email_confirmed_at,
     raw_app_meta_data, raw_user_meta_data, created_at, updated_at)
  values
    (v_test_user_id, 'authenticated', 'authenticated',
     'g3.5-rpc-' || v_test_user_id::text || '@test.invalid', '', now(),
     '{"provider":"email","providers":["email"]}'::jsonb,
     '{}'::jsonb, now(), now());

  -- Die DemoDB kann eine hinter der Tabelle liegende ID-Sequenz enthalten.
  -- Eine explizite, nur transaktionale ID hält den Test reproduzierbar, ohne
  -- die Sequenz zu verändern.
  select coalesce(max(id), 0) + 1000000 into v_test_mitglied_id from public.mitglied;
  insert into public.mitglied
    (id, name, vorname, auth_user_id, role, is_demo, aufnahme_status)
  values
    (v_test_mitglied_id, 'G3.5 Test', 'RPC', v_test_user_id, 'user', true, 'aufgenommen');

  insert into public.app_user
    (user_id, mitglied_id, role, is_demo_account, is_demo, is_protected,
     permission_grants, permission_revocations)
  values
    (v_test_user_id, v_test_mitglied_id, 'user', true, true, false, 96, 0)
  on conflict (user_id) do update
    set mitglied_id = excluded.mitglied_id,
        role = excluded.role,
        is_demo_account = excluded.is_demo_account,
        is_demo = excluded.is_demo,
        is_protected = excluded.is_protected,
        permission_grants = excluded.permission_grants,
        permission_revocations = excluded.permission_revocations;

  perform set_config('request.jwt.claim.sub', v_test_user_id::text, true);
  perform set_config('request.jwt.claim.role', 'authenticated', true);

  if auth.uid() is distinct from v_test_user_id then
    raise exception 'Der temporäre G3.5-Testbenutzer wurde nicht als auth.uid() gesetzt.';
  end if;
  if not public.has_effective_permission(32) then
    raise exception 'Der G3.5-Funktionstest benötigt CanReadMeters (32) für capture_meter_reading.' using errcode = '42501';
  end if;
  if not public.has_effective_permission(64) then
    raise exception 'Der G3.5-Funktionstest benötigt CanManageMeters (64) für execute_hauptzaehler_meter_change.' using errcode = '42501';
  end if;

  select count(*) into v_count
  from public.hauptzaehler_messstelle
  where (medium, anlagenbereich) in (
    ('strom', 'Nord'), ('strom', 'Süd'), ('wasser', 'Nord'), ('wasser', 'Süd')
  );
  if v_count <> 4 then
    raise exception 'Der Test erwartet genau vier kanonische Hauptzähler-Messstellen, gefunden: %.', v_count;
  end if;

  -- Bestehende Hauptzähler werden nie verändert. Der Test verlangt einen
  -- freien Teststand, damit der Partial-Unique-Index echt geprüft werden kann.
  if exists (
    select 1 from public.zaehler
    where ist_hauptzaehler = true and status = 'aktiv' and ausgebaut_am is null
  ) then
    raise exception 'Der G3.4-Funktionstest startet nur ohne aktive Hauptzähler und verändert keine Bestandsdaten.';
  end if;

  select id, rfid_tag_uid
    into v_messstelle_id, v_rfid_tag_uid
  from public.hauptzaehler_messstelle
  where medium = 'strom' and anlagenbereich = 'Nord';

  insert into public.zaehler
    (parzelle_id, ist_hauptzaehler, medium, anlagenbereich, zaehlernummer, eichdatum, eingebaut_am, status)
  values
    (null, true, 'strom',  'Nord', 'CX-G3.4-HZ-STROM-NORD-01',  date '2026-01-01', date '2026-01-01', 'aktiv'),
    (null, true, 'strom',  'Süd',  'CX-G3.4-HZ-STROM-SUED-01',  date '2026-01-01', date '2026-01-01', 'aktiv'),
    (null, true, 'wasser', 'Nord', 'CX-G3.4-HZ-WASSER-NORD-01', date '2026-01-01', date '2026-01-01', 'aktiv'),
    (null, true, 'wasser', 'Süd',  'CX-G3.4-HZ-WASSER-SUED-01', date '2026-01-01', date '2026-01-01', 'aktiv');

  -- IDs anhand der eindeutigen Testnummern laden.
  select id into v_strom_nord_alt from public.zaehler where zaehlernummer = 'CX-G3.4-HZ-STROM-NORD-01';
  select id into v_strom_sued from public.zaehler where zaehlernummer = 'CX-G3.4-HZ-STROM-SUED-01';
  select id into v_wasser_nord from public.zaehler where zaehlernummer = 'CX-G3.4-HZ-WASSER-NORD-01';
  select id into v_wasser_sued from public.zaehler where zaehlernummer = 'CX-G3.4-HZ-WASSER-SUED-01';

  if v_strom_nord_alt is null or v_strom_sued is null or v_wasser_nord is null or v_wasser_sued is null then
    raise exception 'Nicht alle vier Hauptzähler wurden angelegt.';
  end if;

  -- Der bestehende Partial-Unique-Index muss den zweiten aktiven Strom-Nord-
  -- Hauptzähler ablehnen.
  begin
    insert into public.zaehler
      (parzelle_id, ist_hauptzaehler, medium, anlagenbereich, zaehlernummer, eichdatum, eingebaut_am, status)
    values
      (null, true, 'strom', 'Nord', 'CX-G3.4-HZ-STROM-NORD-DUP', date '2026-01-01', date '2026-01-01', 'aktiv');
    raise exception 'Der Partial-Unique-Index hat den doppelten aktiven Hauptzähler nicht verhindert.';
  exception
    when unique_violation then null;
  end;

  -- Echte JEA-Ablesung über den produktiven RPC und dessen echte Enum-Signatur.
  select * into v_reading
  from public.capture_meter_reading(
    v_strom_nord_alt, date '2026-11-05', 100,
    'jae'::public.ablesung_art, null, null, null
  );
  if v_reading.id is null or v_reading.freigegeben is not true or v_reading.pruefstatus <> 'freigegeben' then
    raise exception 'Die JEA-Ablesung für den Hauptzähler wurde nicht korrekt gespeichert.';
  end if;

  -- Echten atomaren Hauptzählerwechsel mit der produktiven RPC-Signatur ausführen.
  select alter_zaehler_id, neuer_zaehler_id
    into v_strom_nord_alt, v_strom_nord_neu
  from public.execute_hauptzaehler_meter_change(
    v_strom_nord_alt, 'strom'::public.zaehler_medium, 'Nord',
    date '2026-11-06', 150, null,
    'CX-G3.4-HZ-STROM-NORD-02', date '2026-01-01', date '2026-11-06',
    0, null
  );

  if not exists (
    select 1 from public.zaehler
    where id = v_strom_nord_alt and ist_hauptzaehler = true and parzelle_id is null
      and medium = 'strom' and anlagenbereich = 'Nord'
      and status = 'ausgebaut' and ausgebaut_am = date '2026-11-06'
  ) then
    raise exception 'Der alte Hauptzähler wurde nicht korrekt historisiert.';
  end if;

  if not exists (
    select 1 from public.zaehler
    where id = v_strom_nord_neu and ist_hauptzaehler = true and parzelle_id is null
      and medium = 'strom' and anlagenbereich = 'Nord'
      and status = 'aktiv' and ausgebaut_am is null
  ) then
    raise exception 'Der neue Hauptzähler wurde nicht korrekt angelegt.';
  end if;

  if not exists (
    select 1 from public.zaehler_ablesung
    where zaehler_id = v_strom_nord_alt and art = 'ausbau'::public.ablesung_art
      and ablesedatum::date = date '2026-11-06' and stand = 150
      and freigegeben = true and pruefstatus = 'freigegeben'
  ) or not exists (
    select 1 from public.zaehler_ablesung
    where zaehler_id = v_strom_nord_neu and art = 'einbau'::public.ablesung_art
      and ablesedatum::date = date '2026-11-06' and stand = 0
      and freigegeben = true and pruefstatus = 'freigegeben'
  ) then
    raise exception 'Ausbau- oder Einbauablesung des Hauptzählerwechsels fehlt.';
  end if;

  -- Die Messstelle/RFID bleibt beim Wechsel dieselbe logische Entität.
  if not exists (
    select 1 from public.hauptzaehler_messstelle
    where id = v_messstelle_id
      and medium = 'strom' and anlagenbereich = 'Nord'
      and rfid_tag_uid is not distinct from v_rfid_tag_uid
  ) then
    raise exception 'Die Hauptzähler-Messstelle oder ihre RFID-Zuordnung wurde beim Wechsel verändert.';
  end if;
end;
$$;

rollback;
