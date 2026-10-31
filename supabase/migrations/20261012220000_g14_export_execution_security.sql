-- G14.4: parameter values, protected server-side execution and legacy-source migration.
alter table public.app_export_filter_definition
  add column if not exists wert_typ text;
comment on column public.app_export_filter_definition.wert_typ is
  'Technischer RPC-Wertetyp: boolean, integer, number oder string.';
update public.app_export_filter_definition
set wert_typ = case
  when typ = 'boolean' then 'boolean'
  else 'string'
end
where wert_typ is null or btrim(wert_typ) = '';
update public.app_export_filter_definition
set wert_typ = 'integer'
where (export_key, filter_key) in (
  ('arbeitsstunden_uebersicht', 'jahr'),
  ('jahresablesung_status', 'p_jahr')
);
-- The operational RFID scan view stays available to its existing scan workflow.
-- This separate export view is only read through the protected export gateway.
create or replace view public.v_export_rfid_status with (security_invoker = true) as
select * from public.v_rfid_scan_context;
revoke all on table public.v_export_rfid_status from public, authenticated;
create or replace function public.assert_export_access()
returns void
language plpgsql
security definer
set search_path = public, pg_temp
as $$
begin
  if not exists (
    select 1 from public.current_app_user_context() c
    where c.role in ('admin', 'vorstand')
  ) then
    raise exception 'Export access requires admin or vorstand role' using errcode = '42501';
  end if;
end;
$$;
create or replace function public.rpc_export_mitgliederliste()
returns setof jsonb
language sql
stable
security invoker
set search_path = public, pg_temp
as $$
  select to_jsonb(m) || jsonb_build_object(
    'mitgliedsnr', m.id::text,
    'strasse_hsnr', coalesce(m.adresse, ''),
    'gaerten', coalesce(g.gaerten, ''),
    'gartennummern', coalesce(g.gaerten, ''),
    'pachtgaerten', coalesce(g.gaerten, ''),
    're', m.email_rechnung_einwilligung,
    'info', m.email_info_einwilligung,
    'wa', m.whatsapp_einwilligung
  )
  from public.mitglied m
  left join lateral (
    select string_agg(p.garten_nr, ', ' order by substring(p.garten_nr from '^[[:space:]]*([0-9]+)')::integer nulls last, p.garten_nr) as gaerten
    from public.parzellen_belegung b
    join public.parzelle p on p.id = b.parzelle_id
    where b.mitglied_id = m.id
      and (b.bis_datum is null or b.bis_datum >= current_date)
  ) g on true;
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
      return query select r from public.rpc_export_mitgliederliste() r;
    else
      raise exception 'Unknown export source' using errcode = '22023';
  end case;
end;
$$;
create or replace function public.rpc_execute_table_or_view_export(p_source_name text)
returns setof jsonb
language plpgsql
security definer
set search_path = public, pg_temp
as $$
begin
  perform public.assert_export_access();
  if p_source_name = 'v_export_rfid_status' then
    return query select to_jsonb(r) from public.v_export_rfid_status r;
    return;
  end if;
  raise exception 'Unknown export table or view source' using errcode = '22023';
end;
$$;
revoke all on function public.assert_export_access() from public;
revoke all on function public.rpc_execute_export(text, jsonb) from public;
revoke all on function public.rpc_execute_table_or_view_export(text) from public;
grant execute on function public.rpc_execute_export(text, jsonb) to authenticated;
grant execute on function public.rpc_execute_table_or_view_export(text) to authenticated;
-- Direct calls to sensitive export sources are replaced by the common checked gateway.
revoke execute on function public.rpc_export_arbeitsstunden_uebersicht(integer, boolean, boolean, boolean, text) from authenticated;
revoke execute on function public.rpc_export_jahresablesung_status(integer) from authenticated;
revoke execute on function public.rpc_export_datenschutz_aktive_mitglieder() from authenticated;
revoke execute on function public.rpc_export_zaehler_eichfaelligkeit(text) from authenticated;
revoke execute on function public.rpc_export_mitgliederliste() from authenticated;
update public.app_export_definition
set quelle_typ = 'rpc', quelle_name = 'rpc_export_mitgliederliste', standard_sortierung = coalesce(standard_sortierung, 'name')
where export_key = 'mitgliederliste';
update public.app_export_definition
set quelle_typ = 'view', quelle_name = 'v_export_rfid_status', standard_sortierung = coalesce(standard_sortierung, 'garten_nr')
where export_key = 'rfid_status';
