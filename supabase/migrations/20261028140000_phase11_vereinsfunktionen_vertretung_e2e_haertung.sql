-- Phase 11: immutable, unambiguous representations and purpose-bound resolution.
-- Source only. This migration is deliberately not executed by this change.

do $$
begin
  if exists (
    select 1
    from public.vereinsfunktion_vertretung a
    join public.vereinsfunktion_vertretung b
      on b.vertretene_besetzung_id=a.vertretene_besetzung_id
     and b.id>a.id
     and daterange(a.gueltig_ab,coalesce(a.gueltig_bis,'infinity'::date),'[]')
         && daterange(b.gueltig_ab,coalesce(b.gueltig_bis,'infinity'::date),'[]')
  ) then
    raise exception 'Überlappende Vertretungen derselben Besetzung müssen vor Phase 11 bereinigt werden';
  end if;
end;
$$;
create or replace function public.prevent_vereinsfunktion_vertretung_identity_change()
returns trigger
language plpgsql
set search_path=public,pg_temp as $$
begin
  if new.vertretene_besetzung_id is distinct from old.vertretene_besetzung_id
     or new.vertreter_besetzung_id is distinct from old.vertreter_besetzung_id
     or new.gueltig_ab is distinct from old.gueltig_ab then
    raise exception 'Vertretene Besetzung, vertretende Besetzung und Beginn einer Vertretung sind historisch und dürfen nicht geändert werden. Bitte die Vertretung beenden und eine neue anlegen.'
      using errcode='23514';
  end if;
  return new;
end;
$$;
drop trigger if exists trg_vereinsfunktion_vertretung_identity_immutable
  on public.vereinsfunktion_vertretung;
create trigger trg_vereinsfunktion_vertretung_identity_immutable
before update of vertretene_besetzung_id,vertreter_besetzung_id,gueltig_ab
on public.vereinsfunktion_vertretung
for each row execute function public.prevent_vereinsfunktion_vertretung_identity_change();
create or replace function public.validate_vereinsfunktion_vertretung_eindeutig()
returns trigger
language plpgsql
set search_path=public,pg_temp as $$
begin
  -- validate_vereinsfunktion_vertretung locks this assignment as well, so
  -- concurrent inserts for the same represented position are serialized.
  perform 1
  from public.vereinsfunktion_besetzung
  where id=new.vertretene_besetzung_id
  for update;

  if exists (
    select 1
    from public.vereinsfunktion_vertretung v
    where v.vertretene_besetzung_id=new.vertretene_besetzung_id
      and v.id is distinct from new.id
      and daterange(v.gueltig_ab,coalesce(v.gueltig_bis,'infinity'::date),'[]')
          && daterange(new.gueltig_ab,coalesce(new.gueltig_bis,'infinity'::date),'[]')
  ) then
    raise exception 'Für eine Besetzung darf zu einem Stichtag nur eine Vertretung gelten.'
      using errcode='23514';
  end if;
  return new;
end;
$$;
drop trigger if exists trg_vereinsfunktion_vertretung_eindeutig
  on public.vereinsfunktion_vertretung;
create trigger trg_vereinsfunktion_vertretung_eindeutig
before insert or update of vertretene_besetzung_id,gueltig_ab,gueltig_bis
on public.vereinsfunktion_vertretung
for each row execute function public.validate_vereinsfunktion_vertretung_eindeutig();
-- A representation is considered only when the representative's own assignment
-- and function are valid at the requested date. No recursive/transitive lookup
-- is performed. If that source is no longer valid, the represented assignment
-- remains the direct holder of its purpose instead of granting an empty source.
create or replace function public.mitgliedsaufnahme_berechtigungen(
  p_verwendungszweck text,
  p_stichtag date default current_date
)
returns table(
  mitglied_id bigint,
  name_snapshot text,
  berechtigungsart text,
  vereinsfunktion_id bigint,
  vereinsfunktion_besetzung_id bigint,
  funktion_schluessel text,
  funktionsbezeichnung text,
  vertretung_id bigint,
  vertretene_vereinsfunktion_id bigint,
  vertretene_besetzung_id bigint,
  vertretene_funktionsbezeichnung text
)
language sql
stable
security definer
set search_path=public,pg_temp as $$
  with basis as (
    select b.id besetzung_id,b.mitglied_id,f.id funktion_id,
           f.schluessel,f.bezeichnung
    from public.vereinsfunktion_verwendung u
    join public.vereinsfunktion f on f.id=u.vereinsfunktion_id and f.aktiv
    join public.vereinsfunktion_besetzung b on b.vereinsfunktion_id=f.id
    where u.verwendungszweck=p_verwendungszweck
      and b.gueltig_ab<=p_stichtag
      and (b.gueltig_bis is null or b.gueltig_bis>=p_stichtag)
  ), gueltige_vertretung as (
    select b.besetzung_id vertretene_besetzung_id,b.funktion_id vertretene_funktion_id,
           b.bezeichnung vertretene_bezeichnung,v.id vertretung_id,
           rb.id ausuebende_besetzung_id,rb.mitglied_id,
           rf.id funktion_id,rf.schluessel,rf.bezeichnung
    from basis b
    join public.vereinsfunktion_vertretung v
      on v.vertretene_besetzung_id=b.besetzung_id
     and v.gueltig_ab<=p_stichtag
     and (v.gueltig_bis is null or v.gueltig_bis>=p_stichtag)
    join public.vereinsfunktion_besetzung rb
      on rb.id=v.vertreter_besetzung_id
     and rb.gueltig_ab<=p_stichtag
     and (rb.gueltig_bis is null or rb.gueltig_bis>=p_stichtag)
    join public.vereinsfunktion rf
      on rf.id=rb.vereinsfunktion_id and rf.aktiv
  ), aufgeloest as (
    select b.mitglied_id,'direkt'::text berechtigungsart,
           b.funktion_id,b.besetzung_id ausuebende_besetzung_id,
           b.schluessel,b.bezeichnung,null::bigint vertretung_id,
           null::bigint vertretene_funktion_id,null::bigint vertretene_besetzung_id,
           null::text vertretene_bezeichnung
    from basis b
    where not exists (
      select 1 from gueltige_vertretung v
      where v.vertretene_besetzung_id=b.besetzung_id
    )
    union all
    select v.mitglied_id,'vertretung'::text,v.funktion_id,
           v.ausuebende_besetzung_id,v.schluessel,v.bezeichnung,v.vertretung_id,
           v.vertretene_funktion_id,v.vertretene_besetzung_id,v.vertretene_bezeichnung
    from gueltige_vertretung v
  )
  select a.mitglied_id,
         coalesce(nullif(btrim(concat_ws(' ',m.vorname,m.name)),''),
                  case when m.mitgliedsnummer is not null
                       then 'Mgl.-Nr. '||m.mitgliedsnummer::text else 'Mitglied' end),
         a.berechtigungsart,a.funktion_id,a.ausuebende_besetzung_id,
         a.schluessel,a.bezeichnung,a.vertretung_id,a.vertretene_funktion_id,
         a.vertretene_besetzung_id,a.vertretene_bezeichnung
  from aufgeloest a
  join public.mitglied m on m.id=a.mitglied_id;
$$;
revoke all on function public.prevent_vereinsfunktion_vertretung_identity_change()
  from public,anon,authenticated;
revoke all on function public.validate_vereinsfunktion_vertretung_eindeutig()
  from public,anon,authenticated;
revoke all on function public.mitgliedsaufnahme_berechtigungen(text,date)
  from public,anon,authenticated;
grant execute on function public.mitgliedsaufnahme_berechtigungen(text,date)
  to service_role;
