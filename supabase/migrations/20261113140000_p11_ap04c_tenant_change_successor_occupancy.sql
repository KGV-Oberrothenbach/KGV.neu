-- AP-04.3: Der rechtliche Vertrag bleibt die Quelle der Vertragsparteien.
-- Die operative Belegung wird für neue Vertragsverhältnisse nur als eindeutig
-- zugeordnete, nicht überlappende Folgeprojektion geführt.
create extension if not exists btree_gist;

alter table public.parzellen_belegung
  add column if not exists pachtverhaeltnis_id bigint references public.pachtverhaeltnis(id) on delete restrict;

create unique index if not exists ux_parzellen_belegung_pachtverhaeltnis
  on public.parzellen_belegung(pachtverhaeltnis_id)
  where pachtverhaeltnis_id is not null;

alter table public.parzellen_belegung
  drop constraint if exists parzellen_belegung_zeitraum_ohne_ueberlappung;
alter table public.parzellen_belegung
  add constraint parzellen_belegung_zeitraum_ohne_ueberlappung
  exclude using gist (
    parzelle_id with =,
    daterange(von_datum, coalesce(bis_datum + 1, 'infinity'::date), '[)') with &&
  );

create or replace function public.sync_pachtverhaeltnis_belegung()
returns trigger language plpgsql security definer set search_path=public,pg_temp as $$
declare v_relation public.pachtverhaeltnis%rowtype;
begin
  if new.rolle <> 'hauptpaechter' then return new; end if;
  select * into v_relation from public.pachtverhaeltnis where id=new.pachtverhaeltnis_id for key share;
  if not found or v_relation.beendet_am is not null then return new; end if;
  insert into public.parzellen_belegung(parzelle_id,mitglied_id,von_datum,bis_datum,pachtverhaeltnis_id)
  values(v_relation.parzelle_id,new.mitglied_id,new.gueltig_ab,new.gueltig_bis,v_relation.id)
  on conflict (pachtverhaeltnis_id) where pachtverhaeltnis_id is not null do update
    set mitglied_id=excluded.mitglied_id,von_datum=excluded.von_datum,bis_datum=excluded.bis_datum;
  return new;
end $$;

drop trigger if exists trg_sync_pachtverhaeltnis_belegung on public.pachtverhaeltnis_beteiligung;
create trigger trg_sync_pachtverhaeltnis_belegung
after insert or update of mitglied_id, rolle, gueltig_ab, gueltig_bis on public.pachtverhaeltnis_beteiligung
for each row execute function public.sync_pachtverhaeltnis_belegung();

create or replace function public.close_pachtverhaeltnis_belegung()
returns trigger language plpgsql security definer set search_path=public,pg_temp as $$
begin
  if old.beendet_am is null and new.beendet_am is not null then
    update public.parzellen_belegung
       set bis_datum = new.beendet_am
     where pachtverhaeltnis_id = new.id
       and (bis_datum is null or bis_datum > new.beendet_am);
  end if;
  return new;
end $$;

drop trigger if exists trg_close_pachtverhaeltnis_belegung on public.pachtverhaeltnis;
create trigger trg_close_pachtverhaeltnis_belegung
after update of beendet_am on public.pachtverhaeltnis
for each row execute function public.close_pachtverhaeltnis_belegung();

revoke all on function public.sync_pachtverhaeltnis_belegung(), public.close_pachtverhaeltnis_belegung() from public, anon, authenticated;
