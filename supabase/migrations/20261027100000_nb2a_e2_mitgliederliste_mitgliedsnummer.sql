-- NB-2A-E2: The visible member-list number is always the stored canonical
-- member number. This migration is source-controlled only; do not apply it
-- without the separately approved database rollout.
create or replace function public.rpc_export_mitgliederliste(
  p_aktiv_filter text,
  p_sortierung text
) returns setof jsonb
language plpgsql stable security invoker
set search_path = public, pg_temp
as $$
begin
  if exists (
    select 1
    from public.mitglied m
    where not coalesce(m.is_demo, false)
      and (m.mitgliedsnummer is null or m.mitgliedsnummer <= 0)
  ) then
    raise exception 'MEMBER_NUMBER_MISSING'
      using errcode = '23514',
            message = 'Für den Mitgliederexport fehlt eine gültige Mitgliedsnummer.';
  end if;

  return query
  with settings as (
    select case lower(coalesce(nullif(btrim(p_aktiv_filter), ''), 'aktiv')) when 'alle' then 'alle' when 'inaktiv' then 'inaktiv' else 'aktiv' end as aktiv_filter,
           case lower(coalesce(nullif(btrim(p_sortierung), ''), 'name')) when 'mitgliedsnr' then 'mitgliedsnr' when 'garten' then 'garten' else 'name' end as sortierung
  ), base as (
    select m.*, coalesce(m.hauptmitglied_id, m.id) principal_member_id,
           m.aktiv and (m.mitglied_ende is null or m.mitglied_ende >= current_date) ist_aktiv,
           exists(select 1 from public.app_user au where au.mitglied_id = m.id) or m.auth_user_id is not null app_vorhanden
    from public.mitglied m where not coalesce(m.is_demo,false)
  ), grouped as (
    select b.*, hm.name hm_name, hm.vorname hm_vorname, hm.id hm_id,
           coalesce(g.gaerten,'') gaerten, coalesce(g.garten_sort_nr,2147483647) garten_sort_nr, coalesce(g.garten_sort_text,'') garten_sort_text
    from base b join base hm on hm.id=b.principal_member_id
    left join lateral (
      select string_agg(p.garten_nr, ', ' order by nullif(substring(p.garten_nr from '^[[:space:]]*([0-9]+)'), '')::integer nulls last, p.garten_nr) gaerten,
             min(nullif(substring(p.garten_nr from '^[[:space:]]*([0-9]+)'), '')::integer) garten_sort_nr, min(p.garten_nr) garten_sort_text
      from public.parzellen_belegung pb join public.parzelle p on p.id=pb.parzelle_id
      where pb.mitglied_id=hm.id and pb.von_datum<=current_date and (pb.bis_datum is null or pb.bis_datum>=current_date) and not coalesce(p.is_demo,false)
    ) g on true
  )
  select to_jsonb(g) || jsonb_build_object(
    'mitgliedsnr', g.mitgliedsnummer::text, 'mitgliedsnummer', g.mitgliedsnummer::text,
    'mitglied', concat_ws(', ', nullif(g.name,''), nullif(g.vorname,'')),
    'strasse_hsnr', coalesce(g.adresse,''), 'gartennummern', g.gaerten, 'pachtgaerten', g.gaerten,
    'adresse_block', concat_ws(E'\n',nullif(g.adresse,''),nullif(concat_ws(' ',nullif(g.plz,''),nullif(g.ort,'')),'')),
    'mobil', coalesce(g.handy,''), 'kontakt_block', concat_ws(E'\n',nullif(g.telefon,''),nullif(g.handy,'')),
    'wa', g.whatsapp_einwilligung, 're', g.email_rechnung_einwilligung, 'info', g.email_info_einwilligung,
    'app', g.app_vorhanden, 'app_vorhanden', g.app_vorhanden, 'aktiv_export', g.ist_aktiv
  )
  from grouped g cross join settings s
  where s.aktiv_filter='alle' or (s.aktiv_filter='aktiv' and g.ist_aktiv) or (s.aktiv_filter='inaktiv' and not g.ist_aktiv)
  order by
    case when s.sortierung='name' then lower(coalesce(g.hm_name,'')) end,
    case when s.sortierung='name' then lower(coalesce(g.hm_vorname,'')) end,
    case when s.sortierung='name' then g.hm_id end,
    case when s.sortierung='mitgliedsnr' then g.mitgliedsnummer end,
    case when s.sortierung='garten' then g.garten_sort_nr end,
    case when s.sortierung='garten' then g.garten_sort_text end,
    case when s.sortierung='garten' then lower(coalesce(g.hm_name,'')) end,
    g.principal_member_id,
    case when g.hauptmitglied_id is null then 0 else 1 end,
    lower(coalesce(g.name,'')), lower(coalesce(g.vorname,'')), g.mitgliedsnummer;
end;
$$;
