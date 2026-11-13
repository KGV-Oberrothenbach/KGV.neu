-- AP-04.4 regression repair: trigger records have table-specific row types.
-- Do not reference fields of the other protected table in a shared predicate.
create or replace function public.guard_abgeschlossene_pachtdaten()
returns trigger language plpgsql security definer set search_path=public,pg_temp as $$
begin
 if tg_table_name='pachtverhaeltnis' then
   if old.beendet_am is not null then
     raise exception 'Abgeschlossene Pachtdaten bleiben Originale; eine Berichtigung ist ergänzend zu erfassen.' using errcode='42501';
   end if;
 elsif tg_table_name='pachtverhaeltnis_beteiligung' then
   if old.gueltig_bis is not null then
     raise exception 'Abgeschlossene Pachtdaten bleiben Originale; eine Berichtigung ist ergänzend zu erfassen.' using errcode='42501';
   end if;
 end if;
 return case when tg_op='DELETE' then old else new end;
end $$;
revoke all on function public.guard_abgeschlossene_pachtdaten() from public,anon,authenticated;
