-- Synthetic baseline for the separately configured DemoDB project.
-- The schema is identical to VereinsDB; only the project-local data differ.
do $$
declare
  v_actor uuid := '00000000-0000-0000-0000-00000000d600';
  v_d01_member bigint;
  v_d02_member bigint;
  v_d01_parcel bigint;
  v_d02_parcel bigint;
begin
  insert into auth.users(
    id, aud, role, email, encrypted_password, email_confirmed_at,
    raw_app_meta_data, raw_user_meta_data, created_at, updated_at
  ) values (
    v_actor, 'authenticated', 'authenticated',
    'synthetic.seed-actor@invalid.example', '', now(),
    '{"provider":"email","providers":["email"]}', '{}', now(), now()
  ) on conflict (id) do nothing;

  if not exists (select 1 from public.saison where jahr = 2026) then
    insert into public.saison(
      id, jahr, pflichtstunden_soll, euro_pro_fehlstunde, bemerkung,
      pacht_pro_qm, mitgliedsbeitrag, mitgliedsbeitrag_nebenmitglied,
      aufnahmegebuehr, gebuehr_bauantrag, pacht_faellig_am
    )
    -- The historical trg_set_jahr derives jahr from id on insert.  Use the
    -- calendar year as the synthetic season id so both values stay 2026.
    select 2026, 2026, 8, 25,
      'Synthetische Saison für lokale DemoDB-Prüfungen.',
      0.50, 50, 25, 10, 0, date '2026-03-31'
    where not exists (select 1 from public.saison where id = 2026);
  end if;

  select id into v_d01_member from public.mitglied
   where email = 'synthetic.d01@invalid.example' order by id limit 1;
  if v_d01_member is null then
    select coalesce(max(id), 0) + 1 into v_d01_member from public.mitglied;
    insert into public.mitglied(
      id, name, vorname, email, auth_user_id, mitglied_seit, aktiv, role,
      aufnahme_status
    ) values (
      v_d01_member, 'Vorstand', 'Synthetisch',
      'synthetic.d01@invalid.example', v_actor, date '2020-01-01', true,
      'admin', 'aufgenommen'
    );
  end if;

  update public.mitglied
     set auth_user_id = v_actor
   where id = v_d01_member and auth_user_id is distinct from v_actor;

  insert into public.app_user(
    user_id, mitglied_id, role, permission_grants, permission_revocations,
    is_protected
  ) values (
    v_actor, v_d01_member, 'user', public.known_permission_mask(), 0, false
  ) on conflict (user_id) do update
    set mitglied_id = excluded.mitglied_id,
        role = excluded.role,
        permission_grants = excluded.permission_grants,
        permission_revocations = excluded.permission_revocations,
        is_protected = excluded.is_protected;

  select id into v_d02_member from public.mitglied
   where email = 'synthetic.d02@invalid.example' order by id limit 1;
  if v_d02_member is null then
    select coalesce(max(id), 0) + 1 into v_d02_member from public.mitglied;
    insert into public.mitglied(
      id, name, vorname, email, mitglied_seit, aktiv, role, aufnahme_status,
      ist_kgv
    ) values (
      v_d02_member, 'Kleingartenverein Oberrothenbach e.V.', '.',
      'synthetic.d02@invalid.example', date '2020-01-01', true, 'user',
      'aufgenommen', true
    );
  end if;

  select id into v_d01_parcel from public.parzelle
   where garten_nr = 'D01' order by id limit 1;
  if v_d01_parcel is null then
    select coalesce(max(id), 0) + 1 into v_d01_parcel from public.parzelle;
    insert into public.parzelle(id, garten_nr, "Anlage", aktiv)
    values (v_d01_parcel, 'D01', 'DemoDB', true);
  end if;

  select id into v_d02_parcel from public.parzelle
   where garten_nr = 'D02' order by id limit 1;
  if v_d02_parcel is null then
    select coalesce(max(id), 0) + 1 into v_d02_parcel from public.parzelle;
    insert into public.parzelle(id, garten_nr, "Anlage", aktiv, ist_vereinsgarten)
    values (v_d02_parcel, 'D02', 'DemoDB', true, true);
  end if;

  if not exists (
    select 1 from public.parzellen_belegung
     where parzelle_id = v_d01_parcel and bis_datum is null
  ) then
    insert into public.parzellen_belegung(
      id, parzelle_id, mitglied_id, von_datum
    )
    select coalesce(max(id), 0) + 1, v_d01_parcel, v_d01_member,
      date '2020-01-01'
    from public.parzellen_belegung;
  end if;

  if not exists (
    select 1 from public.parzellen_belegung
     where parzelle_id = v_d02_parcel and bis_datum is null
  ) then
    insert into public.parzellen_belegung(
      id, parzelle_id, mitglied_id, von_datum
    )
    select coalesce(max(id), 0) + 1, v_d02_parcel, v_d02_member,
      date '2020-01-01'
    from public.parzellen_belegung;
  end if;

  -- Several legacy tables use sequences although their project seed rows use
  -- stable explicit ids.  Advance those sequences so later scenario files can
  -- safely rely on their normal defaults.
  perform setval(
    pg_get_serial_sequence('public.mitglied', 'id'),
    (select greatest(coalesce(max(id), 1), 1) from public.mitglied),
    true
  );
  perform setval(
    pg_get_serial_sequence('public.parzelle', 'id'),
    (select greatest(coalesce(max(id), 1), 1) from public.parzelle),
    true
  );
  perform setval(
    pg_get_serial_sequence('public.parzellen_belegung', 'id'),
    (select greatest(coalesce(max(id), 1), 1) from public.parzellen_belegung),
    true
  );
end;
$$;

select set_config(
  'request.jwt.claim.sub',
  '00000000-0000-0000-0000-00000000d600',
  false
);
select set_config('request.jwt.claim.role', 'authenticated', false);
