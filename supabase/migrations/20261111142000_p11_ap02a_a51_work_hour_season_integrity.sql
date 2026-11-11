-- P11 A5.1 / R16: work-hour season is a server-owned derivative of its date.
create or replace function public.enforce_arbeitsstunde_saison()
returns trigger
language plpgsql
security definer
set search_path=public,pg_temp
as $$
declare v_saison_id integer;
begin
  if new.datum is null then
    raise exception 'Arbeitsstunden benötigen ein Arbeitsdatum.' using errcode='22023';
  end if;
  select id into v_saison_id from public.saison
  where jahr=extract(year from new.datum)::integer
  limit 1;
  if v_saison_id is null then
    raise exception 'Für das Arbeitsjahr ist keine Saison hinterlegt.' using errcode='P0002';
  end if;
  new.saison_id:=v_saison_id;
  return new;
end;
$$;
drop trigger if exists trg_enforce_arbeitsstunde_saison on public.arbeitsstunde;
create trigger trg_enforce_arbeitsstunde_saison
before insert or update of datum on public.arbeitsstunde
for each row execute function public.enforce_arbeitsstunde_saison();
