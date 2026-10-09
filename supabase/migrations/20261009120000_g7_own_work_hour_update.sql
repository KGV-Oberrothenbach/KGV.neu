-- G7.2: Eigene offene Arbeitsstunden dürfen nur in ihren Erfassungsfeldern geändert werden.
drop policy if exists arbeitsstunde_update_own_open on public.arbeitsstunde;
create policy arbeitsstunde_update_own_open on public.arbeitsstunde
  for update to authenticated
  using (
    public.current_mitglied_id() is not null
    and mitglied_id = public.current_mitglied_id()
    and status = 'offen'
    and freigegeben = false
    and genehmigt_von is null
    and genehmigt_am is null
    and (
      not public.is_demo_or_reviewer()
      or public.is_demo_mitglied_id(mitglied_id)
    )
  )
  with check (
    public.current_mitglied_id() is not null
    and mitglied_id = public.current_mitglied_id()
    and status = 'offen'
    and freigegeben = false
    and genehmigt_von is null
    and genehmigt_am is null
    and (
      not public.is_demo_or_reviewer()
      or public.is_demo_mitglied_id(mitglied_id)
    )
  );

create or replace function public.enforce_own_open_arbeitsstunde_update()
returns trigger
language plpgsql
security definer
set search_path = public
as $$
begin
  -- Bestehende Verwaltungsrechte bleiben unverändert.
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

  -- Lock-Felder dürfen nur durch den vorhandenen Browser-Lock gepflegt bzw.
  -- durch dessen Besitzer wieder freigegeben werden.
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

revoke all on function public.enforce_own_open_arbeitsstunde_update() from public, anon, authenticated;

drop trigger if exists trg_enforce_own_open_arbeitsstunde_update on public.arbeitsstunde;
create trigger trg_enforce_own_open_arbeitsstunde_update
  before update on public.arbeitsstunde
  for each row execute function public.enforce_own_open_arbeitsstunde_update();
