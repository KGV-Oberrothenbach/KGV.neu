-- NB-2A-E3: expose only stored canonical membership numbers in the two
-- person-centred standard exports. Source-controlled only; do not apply
-- without a separately approved database rollout.

drop function if exists public.rpc_export_arbeitsstunden_uebersicht(integer, boolean, boolean, boolean, text);

create function public.rpc_export_arbeitsstunden_uebersicht(
  p_jahr integer default null, p_stunden_offen boolean default false,
  p_stunden_fertig boolean default false, p_wartungsvertraege boolean default false,
  p_ansicht text default 'zusammenfassung'
)
returns table(
  mitglied_id bigint, mitgliedsnummer bigint, garten_nr text, nachname text, vorname text, jahr integer,
  pflichtstunden_soll numeric, geleistete_stunden numeric, wartungsvertrag_gutschrift_stunden numeric,
  offene_stunden numeric, hat_wartungsvertrag boolean, wartungsvertraege text, status text, regelgrund text,
  zeilentyp text, leistendes_mitglied text, stunden_dieses_mitglieds numeric,
  datum date, taetigkeit text, sortierung integer
)
language plpgsql stable security invoker
set search_path = public, pg_temp
as $$
begin
  if exists (
    select 1
    from public.v_pflichtstunden_uebersicht v
    join public.mitglied m on m.id = v.hauptmitglied_id
    where m.aktiv and not coalesce(m.is_demo, false)
      and (p_jahr is null or v.saison_jahr = p_jahr)
      and (m.mitgliedsnummer is null or m.mitgliedsnummer <= 0)
  ) then
    raise exception 'MEMBER_NUMBER_MISSING'
      using errcode = '23514', message = 'Für den Arbeitsstundenexport fehlt eine gültige Mitgliedsnummer.';
  end if;

  return query
  with basis as (
    select distinct on (v.hauptmitglied_id, v.saison_id)
      v.hauptmitglied_id as mitglied_id, m.mitgliedsnummer, m.name as nachname, m.vorname,
      v.saison_jahr as jahr, v.saison_id,
      coalesce(v.pflichtstunden_soll, 0) as pflichtstunden_soll,
      coalesce(v.geleistete_stunden, 0) as geleistete_stunden,
      coalesce(v.wartungsvertrag_gutschrift_stunden, 0) as wartungsvertrag_gutschrift_stunden,
      coalesce(v.offene_stunden, 0) as offene_stunden,
      coalesce(v.hat_wartungsvertrag, false) as hat_wartungsvertrag,
      coalesce(v.regelgrund, '') as regelgrund
    from public.v_pflichtstunden_uebersicht v
    join public.mitglied m on m.id = v.hauptmitglied_id
    where m.aktiv and not coalesce(m.is_demo, false)
      and (p_jahr is null or v.saison_jahr = p_jahr)
    order by v.hauptmitglied_id, v.saison_id desc
  ), gaerten as (
    select b.mitglied_id, b.jahr,
      coalesce(string_agg(distinct p.garten_nr, ', ' order by p.garten_nr), '') as garten_nr
    from basis b
    left join public.parzellen_belegung pb on pb.mitglied_id = b.mitglied_id
      and pb.von_datum <= make_date(b.jahr, 12, 31)
      and (pb.bis_datum is null or pb.bis_datum >= make_date(b.jahr, 1, 1))
    left join public.parzelle p on p.id = pb.parzelle_id
    group by b.mitglied_id, b.jahr
  ), summary as (
    select b.*, g.garten_nr, coalesce(string_agg(distinct w.titel, ', ' order by w.titel), '') as wartungsvertraege
    from basis b
    left join gaerten g on g.mitglied_id = b.mitglied_id and g.jahr = b.jahr
    left join public.wartungsvertrag_zuordnungen z on z.hauptmitglied_id = b.mitglied_id
      and z.gueltig_ab <= make_date(b.jahr, 12, 31)
      and (z.gueltig_bis is null or z.gueltig_bis >= make_date(b.jahr, 1, 1))
    left join public.wartungsvertraege w on w.id = z.wartungsvertrag_id
    group by b.mitglied_id, b.mitgliedsnummer, b.nachname, b.vorname, b.jahr, b.saison_id,
      b.pflichtstunden_soll, b.geleistete_stunden, b.wartungsvertrag_gutschrift_stunden,
      b.offene_stunden, b.hat_wartungsvertrag, b.regelgrund, g.garten_nr
  ), filtered as (
    select * from summary
    where not (p_stunden_offen or p_stunden_fertig or p_wartungsvertraege)
       or (p_stunden_offen and offene_stunden > 0)
       or (p_stunden_fertig and pflichtstunden_soll > 0 and offene_stunden <= 0)
       or (p_wartungsvertraege and hat_wartungsvertrag)
       or regelgrund = 'altersbefreiung'
  ), detail as (
    select f.mitglied_id, a.mitglied_id as leistender_id, a.datum::date, a.art_der_arbeit, a.stunden,
      trim(concat_ws(' ', lm.vorname, lm.name)) as leistendes_mitglied
    from filtered f
    join public.arbeitsstunde a on a.saison_id = f.saison_id
    join public.mitglied lm on lm.id = a.mitglied_id
    where a.freigegeben and coalesce(lm.hauptmitglied_id, lm.id) = f.mitglied_id
  ), rows as (
    select f.*, 'Zusammenfassung'::text as zeilentyp, ''::text as leistendes_mitglied,
      null::numeric as stunden_dieses_mitglieds, null::date as datum, ''::text as taetigkeit, 0 as sortierung
    from filtered f
    union all
    select f.*, 'Davon Mitglied'::text, d.leistendes_mitglied, sum(d.stunden), null::date, ''::text, 1
    from filtered f join detail d on d.mitglied_id = f.mitglied_id
    where lower(coalesce(p_ansicht, 'zusammenfassung')) in ('pro_mitglied', 'einzeln')
    group by f.mitglied_id, f.mitgliedsnummer, f.garten_nr, f.nachname, f.vorname, f.jahr, f.saison_id,
      f.pflichtstunden_soll, f.geleistete_stunden, f.wartungsvertrag_gutschrift_stunden,
      f.offene_stunden, f.hat_wartungsvertrag, f.wartungsvertraege, f.regelgrund, d.leistendes_mitglied
    union all
    select f.*, 'Einzelbuchung'::text, d.leistendes_mitglied, d.stunden, d.datum, coalesce(d.art_der_arbeit, ''), 2
    from filtered f join detail d on d.mitglied_id = f.mitglied_id
    where lower(coalesce(p_ansicht, 'zusammenfassung')) = 'einzeln'
  )
  select r.mitglied_id, r.mitgliedsnummer, r.garten_nr, r.nachname, r.vorname, r.jahr, r.pflichtstunden_soll, r.geleistete_stunden,
    r.wartungsvertrag_gutschrift_stunden, r.offene_stunden, r.hat_wartungsvertrag, r.wartungsvertraege,
    case when r.regelgrund = 'wartungsvertrag' then 'Wartungsvertrag – befreit'
         when r.pflichtstunden_soll > 0 and r.offene_stunden > 0 then 'Stunden offen'
         when r.pflichtstunden_soll > 0 then 'Stunden fertig' else 'Befreit' end,
    r.regelgrund, r.zeilentyp, r.leistendes_mitglied, r.stunden_dieses_mitglieds, r.datum, r.taetigkeit, r.sortierung
  from rows r
  order by case when nullif(r.garten_nr, '') is null then 1 else 0 end,
    substring(r.garten_nr from '^[[:space:]]*([0-9]+)')::integer nulls last,
    r.garten_nr, r.nachname, r.vorname, r.sortierung, r.leistendes_mitglied, r.datum;
end;
$$;

grant execute on function public.rpc_export_arbeitsstunden_uebersicht(integer, boolean, boolean, boolean, text) to authenticated;

drop function if exists public.rpc_export_datenschutz_aktive_mitglieder();

create function public.rpc_export_datenschutz_aktive_mitglieder()
returns table (
  garten_nr text, mitgliedsnummer bigint, name text, email text,
  email_info boolean, email_rechnung boolean, whatsapp boolean
)
language plpgsql stable security invoker
set search_path = public, pg_temp
as $$
begin
  if exists (
    select 1
    from public.mitglied m
    where m.aktiv = true
      and (m.mitglied_ende is null or m.mitglied_ende >= current_date)
      and not coalesce(m.is_demo, false)
      and (m.mitgliedsnummer is null or m.mitgliedsnummer <= 0)
  ) then
    raise exception 'MEMBER_NUMBER_MISSING'
      using errcode = '23514', message = 'Für den Datenschutzexport fehlt eine gültige Mitgliedsnummer.';
  end if;

  return query
  with aktive_gaerten as (
    select pb.mitglied_id, string_agg(distinct p.garten_nr, ', ' order by p.garten_nr) as garten_nr
    from public.parzellen_belegung pb
    join public.parzelle p on p.id = pb.parzelle_id
    where (pb.von_datum is null or pb.von_datum <= current_date)
      and (pb.bis_datum is null or pb.bis_datum >= current_date)
      and nullif(btrim(p.garten_nr), '') is not null
    group by pb.mitglied_id
  ), basis as (
    select m.id, m.hauptmitglied_id, m.mitgliedsnummer, coalesce(m.hauptmitglied_id, m.id) as gruppen_id,
      coalesce(eigener_garten.garten_nr, '') as garten_nr,
      coalesce(gruppen_garten.garten_nr, '') as gruppen_garten_nr,
      concat_ws(' ', m.vorname, m.name) as name, coalesce(m.email, '') as email,
      coalesce(m.email_info_einwilligung, false) as email_info,
      coalesce(m.email_rechnung_einwilligung, false) as email_rechnung,
      coalesce(m.whatsapp_einwilligung, false) as whatsapp
    from public.mitglied m
    left join aktive_gaerten eigener_garten on eigener_garten.mitglied_id = m.id
    left join aktive_gaerten gruppen_garten on gruppen_garten.mitglied_id = coalesce(m.hauptmitglied_id, m.id)
    where m.aktiv = true
      and (m.mitglied_ende is null or m.mitglied_ende >= current_date)
      and not coalesce(m.is_demo, false)
  )
  select b.garten_nr, b.mitgliedsnummer, b.name, b.email, b.email_info, b.email_rechnung, b.whatsapp
  from basis b
  order by case when nullif(b.gruppen_garten_nr, '') is null then 1 else 0 end,
    substring(b.gruppen_garten_nr from '^[[:space:]]*([0-9]+)')::integer nulls last,
    b.gruppen_garten_nr, b.gruppen_id,
    case when b.hauptmitglied_id is null then 0 else 1 end,
    b.name, b.email;
end;
$$;

grant execute on function public.rpc_export_datenschutz_aktive_mitglieder() to authenticated;

-- Keep the JSON wrappers explicit after the return shapes changed.
create or replace function public.rpc_export_arbeitsstunden_uebersicht_json(p_parameters jsonb default '{}'::jsonb)
returns setof jsonb language sql stable security invoker set search_path = public, pg_temp as $$
  select to_jsonb(r) from public.rpc_export_arbeitsstunden_uebersicht(nullif(p_parameters ->> 'p_jahr', '')::integer, coalesce((p_parameters ->> 'p_stunden_offen')::boolean, false), coalesce((p_parameters ->> 'p_stunden_fertig')::boolean, false), coalesce((p_parameters ->> 'p_wartungsvertraege')::boolean, false), coalesce(nullif(p_parameters ->> 'p_ansicht', ''), 'zusammenfassung')) r;
$$;
create or replace function public.rpc_export_datenschutz_aktive_mitglieder_json(p_parameters jsonb default '{}'::jsonb)
returns setof jsonb language sql stable security invoker set search_path = public, pg_temp as $$
  select to_jsonb(r) from public.rpc_export_datenschutz_aktive_mitglieder() r;
$$;

insert into public.app_export_column_definition
  (export_key, column_key, label_kurz, label_lang, sortierung, standard_sichtbar, ist_sortierspalte)
values
  ('arbeitsstunden_uebersicht', 'mitgliedsnummer', 'Mitgliedsnummer', 'Mitgliedsnummer', 20, true, false),
  ('datenschutz_aktive_mitglieder', 'mitgliedsnummer', 'Mitgliedsnummer', 'Mitgliedsnummer', 20, true, false)
on conflict (export_key, column_key) do update set
  label_kurz = excluded.label_kurz, label_lang = excluded.label_lang,
  sortierung = excluded.sortierung, standard_sichtbar = excluded.standard_sichtbar,
  ist_sortierspalte = excluded.ist_sortierspalte;

update public.app_export_column_definition
set sortierung = 30
where export_key = 'arbeitsstunden_uebersicht' and column_key = 'nachname';
update public.app_export_column_definition
set sortierung = 40
where export_key = 'arbeitsstunden_uebersicht' and column_key = 'vorname';
update public.app_export_column_definition
set sortierung = case column_key
  when 'name' then 30 when 'email' then 40 when 'email_info' then 50
  when 'email_rechnung' then 60 when 'whatsapp' then 70 else sortierung end
where export_key = 'datenschutz_aktive_mitglieder'
  and column_key in ('name', 'email', 'email_info', 'email_rechnung', 'whatsapp');
