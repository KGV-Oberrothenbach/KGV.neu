-- G7.4: Atomarer Prüfworkflow für offene Arbeitsstunden.
create or replace function public.g7_safe_work_hour_snapshot(p_value text)
returns jsonb
language plpgsql immutable
as $$
begin
  if nullif(btrim(p_value), '') is null then
    return '{}'::jsonb;
  end if;

  begin
    return p_value::jsonb;
  exception when others then
    return jsonb_build_object('legacy_text', p_value);
  end;
end;
$$;

do $$
declare
  v_constraint record;
  v_type text;
begin
  for v_constraint in
    select c.conname
    from pg_constraint c
    where c.conrelid = 'public.arbeitsstunde_pruefverlauf'::regclass
      and c.contype = 'f'
      and c.confrelid = 'public.arbeitsstunde'::regclass
  loop
    execute format('alter table public.arbeitsstunde_pruefverlauf drop constraint %I', v_constraint.conname);
  end loop;

  alter table public.arbeitsstunde_pruefverlauf
    alter column arbeitsstunde_id drop not null;

  select data_type into v_type
  from information_schema.columns
  where table_schema = 'public'
    and table_name = 'arbeitsstunde_pruefverlauf'
    and column_name = 'vorher_snapshot';

  if v_type <> 'jsonb' then
    alter table public.arbeitsstunde_pruefverlauf
      alter column vorher_snapshot drop default,
      alter column vorher_snapshot type jsonb
        using public.g7_safe_work_hour_snapshot(vorher_snapshot),
      alter column vorher_snapshot set default '{}'::jsonb,
      alter column vorher_snapshot set not null;
  end if;

  select data_type into v_type
  from information_schema.columns
  where table_schema = 'public'
    and table_name = 'arbeitsstunde_pruefverlauf'
    and column_name = 'nachher_snapshot';

  if v_type <> 'jsonb' then
    alter table public.arbeitsstunde_pruefverlauf
      alter column nachher_snapshot type jsonb
        using case
          when nachher_snapshot is null or nullif(btrim(nachher_snapshot), '') is null then null
          else public.g7_safe_work_hour_snapshot(nachher_snapshot)
        end;
  end if;
end;
$$;

drop policy if exists arbeitsstunde_legacy_review_update on public.arbeitsstunde;
drop policy if exists arbeitsstunde_legacy_review_delete on public.arbeitsstunde;
drop policy if exists arbeitsstunde_manage_work_hours_update on public.arbeitsstunde;
drop policy if exists arbeitsstunde_manage_work_hours_delete on public.arbeitsstunde;

drop policy if exists arbeitsstunde_pruefverlauf_select_authenticated on public.arbeitsstunde_pruefverlauf;
drop policy if exists arbeitsstunde_pruefverlauf_insert_authenticated on public.arbeitsstunde_pruefverlauf;
drop policy if exists arbeitsstunde_pruefverlauf_select_scoped on public.arbeitsstunde_pruefverlauf;
create policy arbeitsstunde_pruefverlauf_select_scoped on public.arbeitsstunde_pruefverlauf
  for select to authenticated
  using (
    (
      public.has_effective_permission(256)
      and (
        not public.is_demo_or_reviewer()
        or (
          coalesce(vorher_snapshot ->> 'mitglied_id', vorher_snapshot ->> 'MitgliedId', '') ~ '^[0-9]+$'
          and public.is_demo_mitglied_id((coalesce(vorher_snapshot ->> 'mitglied_id', vorher_snapshot ->> 'MitgliedId'))::bigint)
        )
      )
    )
    or (
      coalesce(vorher_snapshot ->> 'mitglied_id', vorher_snapshot ->> 'MitgliedId', '') ~ '^[0-9]+$'
      and (coalesce(vorher_snapshot ->> 'mitglied_id', vorher_snapshot ->> 'MitgliedId'))::integer = public.current_mitglied_id()
      and (
        not public.is_demo_or_reviewer()
        or public.is_demo_mitglied_id((coalesce(vorher_snapshot ->> 'mitglied_id', vorher_snapshot ->> 'MitgliedId'))::bigint)
      )
    )
  );

revoke insert, update, delete on table public.arbeitsstunde_pruefverlauf from authenticated;
grant select on table public.arbeitsstunde_pruefverlauf to authenticated;
revoke delete on table public.arbeitsstunde from authenticated;

create or replace function public.enforce_own_open_arbeitsstunde_update()
returns trigger
language plpgsql
security definer
set search_path = public
as $$
begin
  -- Nur die zentrale G7.4-RPC darf den Eigenprozess-Schutz für Review-Mutationen passieren.
  if current_setting('app.g7_work_hour_review', true) = 'true'
     and public.has_effective_permission(256) then
    return new;
  end if;

  if old.mitglied_id <> public.current_mitglied_id()
     or old.status <> 'offen'
     or old.freigegeben <> false
     or old.genehmigt_von is not null
     or old.genehmigt_am is not null
     or new.mitglied_id <> public.current_mitglied_id()
     or new.status <> 'offen'
     or new.freigegeben <> false
     or new.genehmigt_von is not null
     or new.genehmigt_am is not null then
    raise exception 'Nur eigene offene Arbeitsstunden dürfen bearbeitet werden.' using errcode = '42501';
  end if;

  if new.id is distinct from old.id
     or new.mitglied_id is distinct from old.mitglied_id
     or new.saison_id is distinct from old.saison_id
     or new.status is distinct from old.status
     or new.freigegeben is distinct from old.freigegeben
     or new.is_demo is distinct from old.is_demo
     or new.genehmigt_von is distinct from old.genehmigt_von
     or new.genehmigt_am is distinct from old.genehmigt_am then
    raise exception 'Nur Datum, Stunden und Art der Arbeit dürfen geändert werden.' using errcode = '42501';
  end if;

  if new.lockedbyuserid is not distinct from old.lockedbyuserid
     and new.lockat is not distinct from old.lockat then
    return new;
  end if;

  if new.lockedbyuserid = auth.uid()
     and new.lockat is not null
     and exists (
       select 1 from public.browser_edit_lock l
       where l.entity_type = 'arbeitsstunde'
         and l.entity_id = old.id::text
         and l.locked_by_user_id = auth.uid()
         and l.expires_at > now()
     ) then
    return new;
  end if;

  if old.lockedbyuserid = auth.uid()
     and new.lockedbyuserid is null
     and new.lockat is null then
    return new;
  end if;

  raise exception 'Arbeitsstunden-Sperren dürfen nur über den Browser-Lock geändert werden.' using errcode = '42501';
end;
$$;

create or replace function public.review_arbeitsstunde(
  p_arbeitsstunde_id integer,
  p_aktion text,
  p_begruendung text,
  p_datum date default null,
  p_stunden numeric default null,
  p_art_der_arbeit text default null
)
returns jsonb
language plpgsql
security definer
set search_path = public
as $$
declare
  v_entry public.arbeitsstunde%rowtype;
  v_before jsonb;
  v_after jsonb;
  v_reviewer_id integer;
  v_action text := lower(btrim(coalesce(p_aktion, '')));
  v_comment text := btrim(coalesce(p_begruendung, ''));
  v_audit_action text;
begin
  if p_arbeitsstunde_id is null or p_arbeitsstunde_id <= 0 then
    raise exception 'Eine gültige Arbeitsstunden-ID ist erforderlich.' using errcode = '22023';
  end if;
  if v_action not in ('freigeben', 'ablehnen', 'korrigieren', 'loeschen') then
    raise exception 'Die Prüfaktion ist ungültig.' using errcode = '22023';
  end if;
  if v_comment = '' then
    raise exception 'Für jede Prüfaktion ist eine Begründung erforderlich.' using errcode = '22023';
  end if;
  if not public.has_effective_permission(256) then
    raise exception 'Für die Prüfung fehlt ManageWorkHours.' using errcode = '42501';
  end if;

  v_reviewer_id := public.current_mitglied_id();
  if v_reviewer_id is null then
    raise exception 'Der Prüfer ist keinem Mitglied zugeordnet.' using errcode = '42501';
  end if;

  select * into v_entry
  from public.arbeitsstunde
  where id = p_arbeitsstunde_id
  for update;

  if not found then
    raise exception 'Die Arbeitsstunde existiert nicht mehr.' using errcode = 'P0002';
  end if;
  if public.is_demo_or_reviewer() and not public.is_demo_mitglied_id(v_entry.mitglied_id) then
    raise exception 'Demo- und Reviewer-Konten dürfen nur Demo-Mitglieder prüfen.' using errcode = '42501';
  end if;
  if v_entry.status <> 'offen' or v_entry.freigegeben <> false then
    raise exception 'Die Arbeitsstunde wurde bereits geprüft oder geändert.' using errcode = '40001';
  end if;
  if v_action = 'korrigieren'
     and (p_datum is null or p_stunden is null or p_stunden <= 0 or nullif(btrim(p_art_der_arbeit), '') is null) then
    raise exception 'Für die Korrektur sind Datum, Stunden größer als 0 und Art der Arbeit erforderlich.' using errcode = '22023';
  end if;

  v_before := to_jsonb(v_entry);
  perform set_config('app.g7_work_hour_review', 'true', true);

  if v_action = 'loeschen' then
    insert into public.arbeitsstunde_pruefverlauf (
      arbeitsstunde_id, aktion, begruendung, geprueft_von, geprueft_am, vorher_snapshot, nachher_snapshot
    ) values (
      v_entry.id, 'geloescht', v_comment, v_reviewer_id, now(), v_before, null
    );
    delete from public.arbeitsstunde where id = v_entry.id;
    return jsonb_build_object('arbeitsstunde_id', p_arbeitsstunde_id, 'aktion', 'geloescht');
  end if;

  if v_action = 'freigeben' then
    update public.arbeitsstunde
    set status = 'genehmigt', freigegeben = true, genehmigt_von = v_reviewer_id, genehmigt_am = now(), lockedbyuserid = null, lockat = null
    where id = v_entry.id
    returning to_jsonb(arbeitsstunde) into v_after;
    v_audit_action := 'freigegeben';
  elsif v_action = 'ablehnen' then
    update public.arbeitsstunde
    set status = 'abgelehnt', freigegeben = false, genehmigt_von = null, genehmigt_am = null, lockedbyuserid = null, lockat = null
    where id = v_entry.id
    returning to_jsonb(arbeitsstunde) into v_after;
    v_audit_action := 'abgelehnt';
  else
    update public.arbeitsstunde
    set datum = p_datum, stunden = p_stunden, art_der_arbeit = btrim(p_art_der_arbeit),
        status = 'genehmigt', freigegeben = true, genehmigt_von = v_reviewer_id, genehmigt_am = now(), lockedbyuserid = null, lockat = null
    where id = v_entry.id
    returning to_jsonb(arbeitsstunde) into v_after;
    v_audit_action := 'korrigiert';
  end if;

  insert into public.arbeitsstunde_pruefverlauf (
    arbeitsstunde_id, aktion, begruendung, geprueft_von, geprueft_am, vorher_snapshot, nachher_snapshot
  ) values (
    v_entry.id, v_audit_action, v_comment, v_reviewer_id, now(), v_before, v_after
  );

  return jsonb_build_object('arbeitsstunde_id', p_arbeitsstunde_id, 'aktion', v_audit_action, 'nachher_snapshot', v_after);
end;
$$;

revoke all on function public.review_arbeitsstunde(integer, text, text, date, numeric, text) from public, anon;
grant execute on function public.review_arbeitsstunde(integer, text, text, date, numeric, text) to authenticated;
