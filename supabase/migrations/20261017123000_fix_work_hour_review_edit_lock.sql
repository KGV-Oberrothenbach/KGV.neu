-- Die Prüfansicht sperrt Arbeitsstunden vor der Review-RPC über den zentralen
-- Browser-Lock. Diese technische Sperrmutation ist keine fachliche Änderung
-- der Arbeitsstunde und darf daher nicht am Eigenbesitz-Schutz scheitern.
create or replace function public.enforce_own_open_arbeitsstunde_update()
returns trigger
language plpgsql
security definer
set search_path = public
as $$
declare
  v_assignment_date date;
begin
  -- Lock-Felder dürfen ausschließlich durch den Besitzer einer aktiven
  -- Browser-Sperre geändert werden. Die Prüfung steht vor dem Eigenprozess,
  -- damit auch ein berechtigter Prüfer fremde offene Arbeitsstunden sperren
  -- und anschließend über review_arbeitsstunde bearbeiten kann.
  if new.lockedbyuserid is distinct from old.lockedbyuserid
     or new.lockat is distinct from old.lockat then
    if (to_jsonb(new) - 'lockedbyuserid' - 'lockat')
       is distinct from (to_jsonb(old) - 'lockedbyuserid' - 'lockat') then
      raise exception 'Arbeitsstunden-Sperren dürfen nur ohne fachliche Änderung gepflegt werden.' using errcode = '42501';
    end if;

    if old.mitglied_id <> public.current_mitglied_id()
       and not public.has_effective_permission(256) then
      raise exception 'Für die Bearbeitung fremder Arbeitsstunden fehlt ManageWorkHours.' using errcode = '42501';
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
  end if;

  if current_setting('app.g7_work_hour_review', true) = 'true'
     and public.has_effective_permission(256) then
    if old.arbeitseinsatz_anmeldung_id is not null then
      if new.arbeitseinsatz_anmeldung_id is distinct from old.arbeitseinsatz_anmeldung_id then
        raise exception 'Die Arbeitseinsatzzuordnung ist unveränderlich.' using errcode = '42501';
      end if;
      select a.datum into v_assignment_date
      from public.arbeitseinsatz_anmeldung aa
      join public.arbeitseinsatz a on a.id = aa.arbeitseinsatz_id
      where aa.id = old.arbeitseinsatz_anmeldung_id;
      if new.datum <> v_assignment_date then
        raise exception 'Das Datum einer Einsatz-Arbeitsstunde entspricht immer dem Einsatzdatum.' using errcode = '42501';
      end if;
    end if;
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
     or new.genehmigt_von is distinct from old.genehmigt_von
     or new.genehmigt_am is distinct from old.genehmigt_am
     or new.arbeitseinsatz_anmeldung_id is distinct from old.arbeitseinsatz_anmeldung_id then
    raise exception 'Nur Datum, Stunden und Art der Arbeit dürfen geändert werden.' using errcode = '42501';
  end if;

  if old.arbeitseinsatz_anmeldung_id is not null then
    select a.datum into v_assignment_date
    from public.arbeitseinsatz_anmeldung aa
    join public.arbeitseinsatz a on a.id = aa.arbeitseinsatz_id
    where aa.id = old.arbeitseinsatz_anmeldung_id;
    if new.datum <> v_assignment_date then
      raise exception 'Das Datum einer Einsatz-Arbeitsstunde entspricht immer dem Einsatzdatum.' using errcode = '42501';
    end if;
  end if;

  return new;
end;
$$;
