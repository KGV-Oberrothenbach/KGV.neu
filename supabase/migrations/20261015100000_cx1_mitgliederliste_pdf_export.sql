-- CX1: canonical member-list data for the central PDF export.
-- The parameterized function is intentionally separate from the legacy no-arg
-- RPC so existing clients can continue to use their current export path.

create or replace function public.rpc_export_mitgliederliste(
  p_aktiv_filter text,
  p_sortierung text
)
returns setof jsonb
language sql
stable
security invoker
set search_path = public, pg_temp
as $$
  with settings as (
    select
      case lower(coalesce(nullif(btrim(p_aktiv_filter), ''), 'aktiv'))
        when 'alle' then 'alle'
        when 'aktiv' then 'aktiv'
        when 'inaktiv' then 'inaktiv'
        else 'aktiv'
      end as aktiv_filter,
      case lower(coalesce(nullif(btrim(p_sortierung), ''), 'name'))
        when 'name' then 'name'
        when 'mitgliedsnr' then 'mitgliedsnr'
        when 'garten' then 'garten'
        else 'name'
      end as sortierung
  ), members as (
    select
      m.*,
      (m.aktiv and (m.mitglied_ende is null or m.mitglied_ende >= current_date)) as ist_aktiv,
      exists (select 1 from public.app_user au where au.mitglied_id = m.id)
        or m.auth_user_id is not null as app_vorhanden
    from public.mitglied m
    where coalesce(m.is_demo, false) = false
  ), rows as (
    select
      m.*,
      coalesce(g.gaerten, '') as gaerten,
      coalesce(g.garten_sort_nr, 2147483647) as garten_sort_nr,
      coalesce(g.garten_sort_text, '') as garten_sort_text
    from members m
    left join lateral (
      select
        string_agg(p.garten_nr, ', ' order by
          nullif(substring(p.garten_nr from '^[[:space:]]*([0-9]+)'), '')::integer nulls last,
          p.garten_nr) as gaerten,
        min(nullif(substring(p.garten_nr from '^[[:space:]]*([0-9]+)'), '')::integer) as garten_sort_nr,
        min(p.garten_nr) as garten_sort_text
      from public.parzellen_belegung b
      join public.parzelle p on p.id = b.parzelle_id
      where b.mitglied_id = m.id
        and b.von_datum <= current_date
        and (b.bis_datum is null or b.bis_datum >= current_date)
        and coalesce(p.is_demo, false) = false
    ) g on true
  )
  select to_jsonb(r) || jsonb_build_object(
    'mitgliedsnr', r.id::text,
    'strasse_hsnr', coalesce(r.adresse, ''),
    'gartennummern', r.gaerten,
    'pachtgaerten', r.gaerten,
    'wa', r.whatsapp_einwilligung,
    're', r.email_rechnung_einwilligung,
    'info', r.email_info_einwilligung,
    'app', r.app_vorhanden,
    'app_vorhanden', r.app_vorhanden,
    'aktiv_export', r.ist_aktiv
  )
  from rows r
  cross join settings s
  where s.aktiv_filter = 'alle'
     or (s.aktiv_filter = 'aktiv' and r.ist_aktiv)
     or (s.aktiv_filter = 'inaktiv' and not r.ist_aktiv)
  order by
    case when s.sortierung = 'name' then lower(coalesce(r.name, '')) end,
    case when s.sortierung = 'name' then lower(coalesce(r.vorname, '')) end,
    case when s.sortierung = 'name' then r.id end,
    case when s.sortierung = 'mitgliedsnr' then r.id end,
    case when s.sortierung = 'garten' then r.garten_sort_nr end,
    case when s.sortierung = 'garten' then r.garten_sort_text end,
    case when s.sortierung = 'garten' then lower(coalesce(r.name, '')) end,
    case when s.sortierung = 'garten' then lower(coalesce(r.vorname, '')) end,
    r.id;
$$;
create or replace function public.rpc_export_mitgliederliste()
returns setof jsonb
language sql
stable
security invoker
set search_path = public, pg_temp
as $$
  select * from public.rpc_export_mitgliederliste('alle', 'name');
$$;
create or replace function public.rpc_execute_export(p_source_name text, p_parameters jsonb default '{}'::jsonb)
returns setof jsonb
language plpgsql
security definer
set search_path = public, pg_temp
as $$
begin
  perform public.assert_export_access();
  case p_source_name
    when 'rpc_export_arbeitsstunden_uebersicht' then
      return query select to_jsonb(r) from public.rpc_export_arbeitsstunden_uebersicht(
        nullif(p_parameters ->> 'p_jahr', '')::integer,
        coalesce((p_parameters ->> 'p_stunden_offen')::boolean, false),
        coalesce((p_parameters ->> 'p_stunden_fertig')::boolean, false),
        coalesce((p_parameters ->> 'p_wartungsvertraege')::boolean, false),
        coalesce(nullif(p_parameters ->> 'p_ansicht', ''), 'zusammenfassung')
      ) r;
    when 'rpc_export_jahresablesung_status' then
      return query select to_jsonb(r) from public.rpc_export_jahresablesung_status(nullif(p_parameters ->> 'p_jahr', '')::integer) r;
    when 'rpc_export_datenschutz_aktive_mitglieder' then
      return query select to_jsonb(r) from public.rpc_export_datenschutz_aktive_mitglieder() r;
    when 'rpc_export_zaehler_eichfaelligkeit' then
      return query select to_jsonb(r) from public.rpc_export_zaehler_eichfaelligkeit(nullif(p_parameters ->> 'p_medium', '')) r;
    when 'rpc_export_mitgliederliste' then
      return query select r from public.rpc_export_mitgliederliste(
        coalesce(nullif(p_parameters ->> 'aktiv_filter', ''), 'aktiv'),
        coalesce(nullif(p_parameters ->> 'sortierung', ''), 'name')
      ) r;
    else
      raise exception 'Unknown export source' using errcode = '22023';
  end case;
end;
$$;
revoke all on function public.rpc_export_mitgliederliste(text, text) from public;
revoke execute on function public.rpc_export_mitgliederliste(text, text) from authenticated;
grant execute on function public.rpc_execute_export(text, jsonb) to authenticated;
