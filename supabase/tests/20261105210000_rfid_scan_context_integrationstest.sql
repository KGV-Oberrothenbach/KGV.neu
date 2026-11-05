-- G5-Integrationstest des gemeinsamen RFID-Resolververtrags.
-- Ausschließlich gegen Demo/TestDB ausführen. Alle Testdaten werden mit der
-- äußeren Transaktion vollständig zurückgerollt.

begin;

do $$
declare
  v_user_id uuid := gen_random_uuid();
  v_member_id bigint;
  v_denied_user_id uuid := gen_random_uuid();
  v_denied_member_id bigint;
  v_parcel_tag text;
  v_parcel_id bigint;
  v_meter_id bigint;
  v_count integer;
  v_medium text;
  v_area text;
  v_active_id bigint;
begin
  -- Berechtigter, ausschließlich transaktionaler Testbenutzer.
  insert into auth.users (id, aud, role, email, encrypted_password,
    email_confirmed_at, raw_app_meta_data, raw_user_meta_data, created_at, updated_at)
  values (v_user_id, 'authenticated', 'authenticated',
    'g5-rfid-' || v_user_id::text || '@test.invalid', '', now(),
    '{"provider":"email","providers":["email"]}'::jsonb, '{}'::jsonb, now(), now());
  insert into public.mitglied (name, vorname, auth_user_id, role, is_demo, aufnahme_status)
  values ('G5 Test', 'RFID', v_user_id, 'user', true, 'aufgenommen')
  returning id into v_member_id;
  insert into public.app_user (user_id, mitglied_id, role, is_demo_account, is_demo,
    is_protected, permission_grants, permission_revocations)
  values (v_user_id, v_member_id, 'user', true, true, false, 96, 0)
  on conflict (user_id) do update set
    mitglied_id = excluded.mitglied_id, role = excluded.role,
    is_demo_account = excluded.is_demo_account, is_demo = excluded.is_demo,
    is_protected = excluded.is_protected, permission_grants = excluded.permission_grants,
    permission_revocations = excluded.permission_revocations;
  perform set_config('request.jwt.claim.sub', v_user_id::text, true);
  perform set_config('request.jwt.claim.role', 'authenticated', true);

  -- Bestehender Parzellenpfad: Der Resolver muss ihn unverändert liefern.
  select p.id, coalesce(p.rfid_strom, p.rfid_wasser)
    into v_parcel_id, v_parcel_tag
  from public.parzelle p
  where coalesce(p.rfid_strom, p.rfid_wasser) is not null
  order by p.id
  limit 1;
  if v_parcel_tag is null then
    raise exception 'Der DemoDB-Test benötigt mindestens eine Parzellen-RFID.';
  end if;
  if not exists (
    select 1 from public.resolve_rfid_scan_context('  ' || lower(v_parcel_tag) || '  ') r
    where r.parzelle_id = v_parcel_id and r.ist_hauptzaehler = false
  ) then
    raise exception 'Die bestehende Parzellen-RFID wurde nicht unverändert aufgelöst.';
  end if;

  -- Vier logische Messstellen erhalten nur innerhalb der Transaktion stabile,
  -- eindeutige Tags; alle RFID-Zuordnungen werden beim ROLLBACK wiederhergestellt.
  update public.hauptzaehler_messstelle
  set rfid_tag_uid = 'CX-G5-' || upper(medium) || '-' || upper(anlagenbereich)
  where (medium, anlagenbereich) in
    (('strom', 'Nord'), ('strom', 'Süd'), ('wasser', 'Nord'), ('wasser', 'Süd'));

  if (select count(*) from public.hauptzaehler_messstelle
      where rfid_tag_uid like 'CX-G5-%') <> 4 then
    raise exception 'Es fehlen kanonische Hauptzähler-Messstellen für den G5-Test.';
  end if;
  if exists (select 1 from public.zaehler
             where ist_hauptzaehler and status = 'aktiv' and ausgebaut_am is null) then
    raise exception 'Der G5-Test verändert keine bereits aktiven Hauptzähler.';
  end if;

  insert into public.zaehler
    (parzelle_id, ist_hauptzaehler, medium, anlagenbereich, zaehlernummer,
     eichdatum, eingebaut_am, status)
  values
    (null, true, 'strom',  'Nord', 'CX-G5-STROM-NORD',  date '2026-01-01', date '2026-01-01', 'aktiv'),
    (null, true, 'strom',  'Süd',  'CX-G5-STROM-SUED',  date '2026-01-01', date '2026-01-01', 'aktiv'),
    (null, true, 'wasser', 'Nord', 'CX-G5-WASSER-NORD', date '2026-01-01', date '2026-01-01', 'aktiv'),
    (null, true, 'wasser', 'Süd',  'CX-G5-WASSER-SUED', date '2026-01-01', date '2026-01-01', 'aktiv');

  -- Alle vier Hauptzähler: Messstelle, Medium, Bereich und aktiver Zähler.
  for v_medium, v_area in
    select medium, anlagenbereich from public.hauptzaehler_messstelle
    where rfid_tag_uid like 'CX-G5-%' order by medium, anlagenbereich
  loop
    select count(*), max(r.aktiver_zaehler_id)
      into v_count, v_active_id
    from public.resolve_rfid_scan_context('cx-g5-' || upper(v_medium) || '-' || upper(v_area)) r
    where r.ist_hauptzaehler and r.parzelle_id is null
      and r.medium = v_medium and r.anlagenbereich = v_area;
    if v_count <> 1 or v_active_id is null then
      raise exception 'Hauptzähler-RFID %/% wurde nicht mit aktivem Zähler aufgelöst.', v_medium, v_area;
    end if;

    -- JEA für jede der vier Messstellen durch den produktiven RPC erfassen.
    perform public.capture_meter_reading(v_active_id, date '2026-11-05', 100,
      'jae'::public.ablesung_art, null, null, null);
  end loop;

  -- Kein aktiver Zähler: Messstelle/RFID bleibt erkennbar, Meterdaten sind NULL.
  update public.zaehler set status = 'ausgebaut', ausgebaut_am = date '2026-11-06'
  where zaehlernummer = 'CX-G5-WASSER-SUED';
  select count(*), max(r.aktiver_zaehler_id)
    into v_count, v_active_id
  from public.resolve_rfid_scan_context('CX-G5-WASSER-SÜD') r
  where r.ist_hauptzaehler and r.medium = 'wasser' and r.anlagenbereich = 'Süd';
  if v_count <> 1 or v_active_id is not null then
    raise exception 'Messstelle ohne aktiven Zähler wurde nicht korrekt zurückgegeben.';
  end if;

  -- Unbekannte sowie leere Tags liefern keine Zuordnung.
  if exists (select 1 from public.resolve_rfid_scan_context('CX-G5-UNBEKANNT'))
     or exists (select 1 from public.resolve_rfid_scan_context('  ')) then
    raise exception 'Unbekannte oder leere RFID wurde fälschlich aufgelöst.';
  end if;

  -- Der RPC hat EXECUTE, aber keine Datenfreigabe ohne CanReadMeters.
  insert into auth.users (id, aud, role, email, encrypted_password,
    email_confirmed_at, raw_app_meta_data, raw_user_meta_data, created_at, updated_at)
  values (v_denied_user_id, 'authenticated', 'authenticated',
    'g5-denied-' || v_denied_user_id::text || '@test.invalid', '', now(),
    '{"provider":"email","providers":["email"]}'::jsonb, '{}'::jsonb, now(), now());
  insert into public.mitglied (name, vorname, auth_user_id, role, is_demo, aufnahme_status)
  values ('G5 Test', 'Denied', v_denied_user_id, 'user', true, 'aufgenommen')
  returning id into v_denied_member_id;
  insert into public.app_user (user_id, mitglied_id, role, is_demo_account, is_demo,
    is_protected, permission_grants, permission_revocations)
  values (v_denied_user_id, v_denied_member_id, 'user', true, true, false, 0, 0)
  on conflict (user_id) do update set
    mitglied_id = excluded.mitglied_id, role = excluded.role,
    is_demo_account = excluded.is_demo_account, is_demo = excluded.is_demo,
    is_protected = excluded.is_protected, permission_grants = excluded.permission_grants,
    permission_revocations = excluded.permission_revocations;
  perform set_config('request.jwt.claim.sub', v_denied_user_id::text, true);
  begin
    perform 1 from public.resolve_rfid_scan_context(v_parcel_tag);
    raise exception 'Unberechtigter Benutzer konnte RFID-Kontext lesen.';
  exception when insufficient_privilege then null;
  end;
end;
$$;

rollback;
