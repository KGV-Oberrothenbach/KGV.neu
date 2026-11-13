-- AP-04.2: Eine Nachsorge endet nur durch einen fachlich wirksamen, bereits
-- begonnenen Nachpächtervertrag. Eine bloß künftige Beteiligung bleibt bewusst
-- ohne Wirkung. Der Trigger läuft im selben Vorgang wie die Vertragsanlage.

create or replace function public.complete_pacht_nachsorge_on_successor()
returns trigger
language plpgsql
security definer
set search_path = public, pg_temp
as $$
declare
  v_parzelle_id bigint;
  v_akteur_user_id uuid;
begin
  if new.rolle <> 'hauptpaechter'
     or new.gueltig_ab > public.kgv_local_today()
     or (new.gueltig_bis is not null and new.gueltig_bis < public.kgv_local_today()) then
    return new;
  end if;

  select pv.parzelle_id, coalesce(auth.uid(), pv.created_by, new.created_by)
    into v_parzelle_id, v_akteur_user_id
  from public.pachtverhaeltnis pv
  where pv.id = new.pachtverhaeltnis_id
    and pv.beendet_am is null;

  -- Der reguläre Fachprozess speichert den Akteur am Vertrag. Ohne belastbare
  -- Akteursbindung wird keine automatisch nicht nachvollziehbare Beendigung
  -- vorgenommen.
  if v_parzelle_id is null or v_akteur_user_id is null then
    return new;
  end if;

  with beendet as (
    update public.pacht_nachsorge n
       set beendet_am = least(public.kgv_local_today(), n.endet_am),
           beendigungsgrund = 'nachpaechteruebernahme',
           beendet_von_user_id = v_akteur_user_id,
           beendet_at = now()
     where n.parzelle_id = v_parzelle_id
       and n.beendet_am is null
       and n.begonnen_am <= public.kgv_local_today()
       and exists (
         select 1
         from public.pachtverhaeltnis alt
         where alt.id = n.pachtverhaeltnis_id
           and alt.beendet_am is not null
       )
     returning n.id
  )
  insert into public.pacht_nachsorge_ereignis
    (nachsorge_id, art, begruendung, akteur_user_id, nachfolger_pachtverhaeltnis_id)
  select id,
         'nachpaechteruebernahme',
         'Automatische Beendigung durch wirksame Nachpächterübernahme.',
         v_akteur_user_id,
         new.pachtverhaeltnis_id
  from beendet;

  return new;
end;
$$;

drop trigger if exists trg_complete_pacht_nachsorge_on_successor on public.pachtverhaeltnis_beteiligung;
create trigger trg_complete_pacht_nachsorge_on_successor
after insert or update of rolle, gueltig_ab, gueltig_bis on public.pachtverhaeltnis_beteiligung
for each row execute function public.complete_pacht_nachsorge_on_successor();

revoke all on function public.complete_pacht_nachsorge_on_successor() from public, anon, authenticated;
grant execute on function public.complete_pacht_nachsorge_on_successor() to service_role;
