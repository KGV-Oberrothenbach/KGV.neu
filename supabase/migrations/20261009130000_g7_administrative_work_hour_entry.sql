-- G7.3: Administrative Arbeitsstunden folgen der effektiven Permission-Matrix.
create or replace function public.has_effective_permission(p_permission bigint)
returns boolean
language sql stable security definer
set search_path to 'public', 'pg_temp'
as $$
  select coalesce((
    (
      (
        case au.role
          when 'admin' then 1048567::bigint
          when 'vorstand' then 523767::bigint
          else 10::bigint
        end
        | coalesce(au.permission_grants, 0)
      )
      & ~coalesce(au.permission_revocations, 0)
      & p_permission
    ) = p_permission
  ), false)
  from public.app_user au
  where au.user_id = auth.uid()
  limit 1;
$$;

comment on function public.has_effective_permission(bigint) is
  'Wertet Rollen-Grundrechte sowie userbezogene Grants und Revocations aus der zentralen Permission-Matrix aus.';

drop policy if exists arbeitsstunde_admin_full on public.arbeitsstunde;
drop policy if exists arbeitsstunde_demo_admin_full on public.arbeitsstunde;
drop policy if exists arbeitsstunde_manage_work_hours on public.arbeitsstunde;
drop policy if exists arbeitsstunde_manage_work_hours_select on public.arbeitsstunde;
drop policy if exists arbeitsstunde_manage_work_hours_insert on public.arbeitsstunde;
drop policy if exists arbeitsstunde_manage_work_hours_update on public.arbeitsstunde;
drop policy if exists arbeitsstunde_manage_work_hours_delete on public.arbeitsstunde;
drop policy if exists arbeitsstunde_legacy_review_update on public.arbeitsstunde;
drop policy if exists arbeitsstunde_legacy_review_delete on public.arbeitsstunde;

create policy arbeitsstunde_manage_work_hours_select on public.arbeitsstunde
  for select to authenticated
  using (
    public.has_effective_permission(256)
    and (
      not public.is_demo_or_reviewer()
      or public.is_demo_mitglied_id(mitglied_id)
    )
  );

create policy arbeitsstunde_manage_work_hours_insert on public.arbeitsstunde
  for insert to authenticated
  with check (
    public.has_effective_permission(256)
    and (
      not public.is_demo_or_reviewer()
      or public.is_demo_mitglied_id(mitglied_id)
    )
    and status = 'genehmigt'
    and freigegeben = true
    and genehmigt_von = public.current_mitglied_id()
    and genehmigt_am is not null
  );

-- Der noch vorhandene Review-Prozess wird erst in G7.4 auf ein eigenes
-- Permission-Modell umgestellt. Bis dahin bleiben seine bisherigen Rollenrechte
-- unverändert; ManageWorkHours selbst erhält daraus keine UPDATE-/DELETE-Rechte.
create policy arbeitsstunde_legacy_review_update on public.arbeitsstunde
  for update to authenticated
  using (
    public.is_productive_admin_or_vorstand()
    or (
      public.is_restricted_demo_admin_or_vorstand()
      and public.is_demo_mitglied_id(mitglied_id)
    )
  )
  with check (
    public.is_productive_admin_or_vorstand()
    or (
      public.is_restricted_demo_admin_or_vorstand()
      and public.is_demo_mitglied_id(mitglied_id)
    )
  );

create policy arbeitsstunde_legacy_review_delete on public.arbeitsstunde
  for delete to authenticated
  using (
    public.is_productive_admin_or_vorstand()
    or (
      public.is_restricted_demo_admin_or_vorstand()
      and public.is_demo_mitglied_id(mitglied_id)
    )
  );

drop policy if exists arbeitsstunde_select_own_or_admin on public.arbeitsstunde;
create policy arbeitsstunde_select_own_or_admin on public.arbeitsstunde
  for select to authenticated
  using (
    (
      public.has_effective_permission(256)
      and (
        not public.is_demo_or_reviewer()
        or public.is_demo_mitglied_id(mitglied_id)
      )
    )
    or (
      mitglied_id = public.current_mitglied_id()
      and (
        not public.is_demo_or_reviewer()
        or public.is_demo_mitglied_id(mitglied_id)
      )
    )
  );

create or replace function public.enforce_administrative_arbeitsstunde_insert()
returns trigger
language plpgsql
security definer
set search_path = public
as $$
begin
  if new.status = 'genehmigt'
     or new.freigegeben = true
     or new.genehmigt_von is not null
     or new.genehmigt_am is not null then
    if not public.has_effective_permission(256) then
      raise exception 'Für die administrative Arbeitsstundenerfassung fehlt ManageWorkHours.' using errcode = '42501';
    end if;

    if public.current_mitglied_id() is null then
      raise exception 'Der Bearbeiter ist keinem Mitglied zugeordnet.' using errcode = '42501';
    end if;

    if public.is_demo_or_reviewer() and not public.is_demo_mitglied_id(new.mitglied_id) then
      raise exception 'Demo- und Reviewer-Konten dürfen nur Demo-Mitglieder bearbeiten.' using errcode = '42501';
    end if;

    -- Genehmiger und Zeitpunkt stammen ausschließlich aus dem aktuellen Kontext.
    new.status := 'genehmigt';
    new.freigegeben := true;
    new.genehmigt_von := public.current_mitglied_id();
    new.genehmigt_am := now();
  end if;

  return new;
end;
$$;

revoke all on function public.enforce_administrative_arbeitsstunde_insert() from public, anon, authenticated;

drop trigger if exists trg_enforce_administrative_arbeitsstunde_insert on public.arbeitsstunde;
create trigger trg_enforce_administrative_arbeitsstunde_insert
  before insert on public.arbeitsstunde
  for each row execute function public.enforce_administrative_arbeitsstunde_insert();

create or replace function public.enforce_own_open_arbeitsstunde_update()
returns trigger
language plpgsql
security definer
set search_path = public
as $$
begin
  -- Der Alt-Reviewprozess bleibt bis G7.4 für seine bestehenden Rollen offen.
  if public.is_productive_admin_or_vorstand()
     or public.is_restricted_demo_admin_or_vorstand() then
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
       select 1
       from public.browser_edit_lock l
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
