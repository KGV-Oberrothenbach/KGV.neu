-- CX10: metadata-driven, JSON-based export gateway.
create or replace function public.rpc_export_mitgliederliste_json(p_parameters jsonb default '{}'::jsonb) returns setof jsonb language sql stable security invoker set search_path = public, pg_temp as $$
  select * from public.rpc_export_mitgliederliste(coalesce(nullif(p_parameters ->> 'aktiv_filter', ''), 'aktiv'), coalesce(nullif(p_parameters ->> 'sortierung', ''), 'name'));
$$;
create or replace function public.rpc_export_arbeitsstunden_uebersicht_json(p_parameters jsonb default '{}'::jsonb) returns setof jsonb language sql stable security invoker set search_path = public, pg_temp as $$
  select to_jsonb(r) from public.rpc_export_arbeitsstunden_uebersicht(nullif(p_parameters ->> 'p_jahr', '')::integer, coalesce((p_parameters ->> 'p_stunden_offen')::boolean, false), coalesce((p_parameters ->> 'p_stunden_fertig')::boolean, false), coalesce((p_parameters ->> 'p_wartungsvertraege')::boolean, false), coalesce(nullif(p_parameters ->> 'p_ansicht', ''), 'zusammenfassung')) r;
$$;
create or replace function public.rpc_export_jahresablesung_status_json(p_parameters jsonb default '{}'::jsonb) returns setof jsonb language sql stable security invoker set search_path = public, pg_temp as $$
  select to_jsonb(r) from public.rpc_export_jahresablesung_status(nullif(p_parameters ->> 'p_jahr', '')::integer) r;
$$;
create or replace function public.rpc_export_datenschutz_aktive_mitglieder_json(p_parameters jsonb default '{}'::jsonb) returns setof jsonb language sql stable security invoker set search_path = public, pg_temp as $$
  select to_jsonb(r) from public.rpc_export_datenschutz_aktive_mitglieder() r;
$$;
create or replace function public.rpc_export_zaehler_eichfaelligkeit_json(p_parameters jsonb default '{}'::jsonb) returns setof jsonb language sql stable security invoker set search_path = public, pg_temp as $$
  select to_jsonb(r) from public.rpc_export_zaehler_eichfaelligkeit(nullif(p_parameters ->> 'p_medium', '')) r;
$$;

update public.app_export_definition set quelle_name = case export_key
  when 'mitgliederliste' then 'rpc_export_mitgliederliste_json'
  when 'arbeitsstunden_uebersicht' then 'rpc_export_arbeitsstunden_uebersicht_json'
  when 'jahresablesung_status' then 'rpc_export_jahresablesung_status_json'
  when 'datenschutz_aktive_mitglieder' then 'rpc_export_datenschutz_aktive_mitglieder_json'
  when 'zaehler_eichfaelligkeit' then 'rpc_export_zaehler_eichfaelligkeit_json'
end
where export_key in ('mitgliederliste', 'arbeitsstunden_uebersicht', 'jahresablesung_status', 'datenschutz_aktive_mitglieder', 'zaehler_eichfaelligkeit');

create or replace function public.rpc_execute_export(p_export_key text, p_parameters jsonb default '{}'::jsonb)
returns setof jsonb language plpgsql security definer set search_path = public, pg_temp as $$
declare source_name text;
begin
  perform public.assert_export_access();
  select quelle_name into source_name from public.app_export_definition
   where export_key = p_export_key and aktiv = true and quelle_typ = 'rpc' and nullif(btrim(quelle_name), '') is not null;
  if source_name is null then
    if exists (select 1 from public.app_export_definition where export_key = p_export_key) then
      raise exception 'EXPORT_NOT_AVAILABLE' using errcode = '22023';
    end if;
    raise exception 'EXPORT_NOT_FOUND' using errcode = '22023';
  end if;
  begin
    return query execute format('select * from public.%I($1)', source_name) using coalesce(p_parameters, '{}'::jsonb);
  exception when undefined_function then
    raise exception 'EXPORT_SOURCE_NOT_AVAILABLE' using errcode = '22023';
  end;
end;
$$;

revoke all on function public.rpc_execute_export(text, jsonb) from public;
grant execute on function public.rpc_execute_export(text, jsonb) to authenticated;

create or replace function public.rpc_export_cx10_test(p_parameters jsonb default '{}'::jsonb)
returns setof jsonb language sql stable security invoker set search_path = public, pg_temp as $$
  select jsonb_build_object('testwert', 'cx10', 'parameterwert', p_parameters -> 'testwert');
$$;
insert into public.app_export_definition (export_key, titel, beschreibung, quelle_typ, quelle_name, aktiv, standard_sortierung, standard_ausgabe, erlaubt_csv, erlaubt_pdf)
values ('cx10_test_export', 'CX10 Architekturtest', 'Technischer Testexport', 'rpc', 'rpc_export_cx10_test', false, 'testwert', 'csv', true, false)
on conflict (export_key) do update set quelle_typ = excluded.quelle_typ, quelle_name = excluded.quelle_name, aktiv = false;
insert into public.app_export_column_definition (export_key, column_key, label_kurz, label_lang, sortierung, standard_sichtbar, ist_sortierspalte)
values ('cx10_test_export', 'testwert', 'Test', 'Testwert', 10, true, false)
on conflict (export_key, column_key) do update set standard_sichtbar = true;
