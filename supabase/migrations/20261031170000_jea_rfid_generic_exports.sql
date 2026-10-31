-- JEA and RFID exports remain behind the metadata-driven rpc_execute_export gateway.
-- The RFID scan context remains the sole operational source of RFID data.

create or replace function public.rpc_export_jahresablesung_status(
  p_jahr integer default null,
  p_nur_fehlend boolean default false
)
returns table (
    jahr integer,
    parzelle text,
    medium text,
    zaehler_id bigint,
    zaehlernummer text,
    eingebaut_am date,
    jae_ablesedatum date,
    jae_stand numeric,
    pruefstatus text,
    status text,
    pachtbezug text,
    pachtwechsel_im_jahr boolean
)
language sql
stable
security invoker
set search_path = public, pg_temp
as $$
  with parameter as (
    select coalesce(p_jahr, extract(year from current_date)::integer) as jahr
  ), stichtag_zaehler as (
    select z.id, z.parzelle_id, z.medium::text as medium, z.zaehlernummer, z.eingebaut_am, p.jahr
    from public.zaehler z
    cross join parameter p
    where z.eingebaut_am <= make_date(p.jahr, 12, 31)
      and (z.ausgebaut_am is null or z.ausgebaut_am > make_date(p.jahr, 12, 31))
  ), letzte_jea as (
    select distinct on (a.zaehler_id, p.jahr)
      a.zaehler_id, p.jahr, a.ablesedatum, a.stand,
      coalesce(a.pruefstatus, case when a.freigegeben then 'freigegeben' else 'eingereicht' end) as pruefstatus,
      a.freigegeben
    from public.zaehler_ablesung a
    cross join parameter p
    where lower(a.art::text) = 'jea'
      and a.ablesedatum >= make_date(p.jahr, 1, 1)
      and a.ablesedatum < make_date(p.jahr + 1, 1, 1)
    order by a.zaehler_id, p.jahr, a.ablesedatum desc, a.id desc
  ), pachtbezug as (
    select distinct on (b.parzelle_id) b.parzelle_id, concat_ws(' ', m.vorname, m.name) as pachtbezug
    from public.parzellen_belegung b
    join public.mitglied m on m.id = b.mitglied_id
    cross join parameter p
    where b.von_datum <= make_date(p.jahr, 12, 31)
      and (b.bis_datum is null or b.bis_datum >= make_date(p.jahr, 12, 31))
    order by b.parzelle_id, b.von_datum desc, b.id desc
  ), pachtwechsel as (
    select b.parzelle_id, count(*) > 1 as pachtwechsel_im_jahr
    from public.parzellen_belegung b
    cross join parameter p
    where b.von_datum <= make_date(p.jahr, 12, 31)
      and (b.bis_datum is null or b.bis_datum >= make_date(p.jahr, 1, 1))
    group by b.parzelle_id
  )
  select
    z.jahr, concat_ws(' ', parzelle."Anlage", parzelle.garten_nr) as parzelle,
    case z.medium when 'strom' then 'Strom' when 'wasser' then 'Wasser' else z.medium end as medium,
    z.id as zaehler_id, z.zaehlernummer, z.eingebaut_am,
    j.ablesedatum as jae_ablesedatum, j.stand as jae_stand, j.pruefstatus,
    case
      when j.zaehler_id is null then 'JEA fehlt'
      when lower(coalesce(j.pruefstatus, '')) = 'abgelehnt' then 'JEA abgelehnt – neu erfassen'
      when j.freigegeben or lower(coalesce(j.pruefstatus, '')) = 'freigegeben' then 'JEA freigegeben'
      else 'JEA eingereicht'
    end as status,
    coalesce(pb.pachtbezug, 'Unbelegt') as pachtbezug,
    coalesce(pw.pachtwechsel_im_jahr, false) as pachtwechsel_im_jahr
  from stichtag_zaehler z
  join public.parzelle on parzelle.id = z.parzelle_id
  left join letzte_jea j on j.zaehler_id = z.id and j.jahr = z.jahr
  left join pachtbezug pb on pb.parzelle_id = z.parzelle_id
  left join pachtwechsel pw on pw.parzelle_id = z.parzelle_id
  where not coalesce(p_nur_fehlend, false)
     or j.zaehler_id is null
     or lower(coalesce(j.pruefstatus, '')) = 'abgelehnt'
  order by parzelle, medium, z.zaehlernummer;
$$;
revoke all on function public.rpc_export_jahresablesung_status(integer, boolean) from public, authenticated;

create or replace function public.rpc_export_jahresablesung_status_json(p_parameters jsonb default '{}'::jsonb)
returns setof jsonb
language sql
stable
security invoker
set search_path = public, pg_temp
as $$
  select to_jsonb(r)
  from public.rpc_export_jahresablesung_status(
    nullif(p_parameters ->> 'p_jahr', '')::integer,
    coalesce((p_parameters ->> 'p_nur_fehlend')::boolean, false)
  ) r;
$$;

update public.app_export_filter_definition
set label = 'Jahr', typ = 'select', optionen_json = to_jsonb('rpc_export_jahresablesung_jahre'::text),
    parameter_name = 'p_jahr', wert_typ = 'integer', standardwert_json = null, pflicht = false, sortierung = 10
where export_key = 'jahresablesung_status' and filter_key = 'p_jahr';

insert into public.app_export_filter_definition
  (export_key, filter_key, label, typ, optionen_json, parameter_name, wert_typ, standardwert_json, pflicht, sortierung)
values
  ('jahresablesung_status', 'nur_fehlend', 'Nur fehlende Jahresendablesungen', 'boolean', null, 'p_nur_fehlend', 'boolean', 'false'::jsonb, false, 20)
on conflict (export_key, filter_key) do update set
  label = excluded.label, typ = excluded.typ, optionen_json = excluded.optionen_json,
  parameter_name = excluded.parameter_name, wert_typ = excluded.wert_typ,
  standardwert_json = excluded.standardwert_json, pflicht = excluded.pflicht, sortierung = excluded.sortierung;

-- No RFID filter metadata exists in the tracked export metadata. Keep the RPC generic
-- and derive no implicit filter from field names or export keys.
create or replace function public.rpc_export_rfid_status(p_parameters jsonb default '{}'::jsonb)
returns setof jsonb
language sql
stable
security invoker
set search_path = public, pg_temp
as $$
  select to_jsonb(r)
  from public.v_rfid_scan_context r;
$$;
revoke all on function public.rpc_export_rfid_status(jsonb) from public, authenticated;

update public.app_export_definition
set quelle_typ = 'rpc',
    quelle_name = 'rpc_export_rfid_status',
    erlaubt_csv = true,
    erlaubt_pdf = true,
    standard_sortierung = coalesce(standard_sortierung, 'garten_nr')
where export_key = 'rfid_status';
