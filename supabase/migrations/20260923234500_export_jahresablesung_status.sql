-- Jahresendablesung: ein Zähler ist nur dann JEA-pflichtig, wenn er am 31.12.
-- des Auswertungsjahres noch eingebaut ist. Im Laufe des Jahres ausgebaute Zähler
-- werden über die Ausbauablesung abgeschlossen und erscheinen nicht als offen.
create or replace function public.rpc_export_jahresablesung_status(p_jahr integer default null)
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
      a.zaehler_id,
      p.jahr,
      a.ablesedatum,
      a.stand,
      coalesce(a.pruefstatus, case when a.freigegeben then 'freigegeben' else 'eingereicht' end) as pruefstatus,
      a.freigegeben
    from public.zaehler_ablesung a
    cross join parameter p
    where lower(a.art::text) = 'jea'
      and a.ablesedatum >= make_date(p.jahr, 1, 1)
      -- ablesedatum ist ein Zeitstempel; der gesamte 31.12. gehört zum Auswertungsjahr.
      and a.ablesedatum < make_date(p.jahr + 1, 1, 1)
    order by a.zaehler_id, p.jahr, a.ablesedatum desc, a.id desc
  ), pachtbezug as (
    select distinct on (b.parzelle_id)
      b.parzelle_id,
      concat_ws(' ', m.vorname, m.name) as pachtbezug
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
    z.jahr,
    concat_ws(' ', parzelle."Anlage", parzelle.garten_nr) as parzelle,
    case z.medium when 'strom' then 'Strom' when 'wasser' then 'Wasser' else z.medium end as medium,
    z.id as zaehler_id,
    z.zaehlernummer,
    z.eingebaut_am,
    j.ablesedatum as jae_ablesedatum,
    j.stand as jae_stand,
    j.pruefstatus,
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
  order by parzelle, medium, z.zaehlernummer;
$$;

grant execute on function public.rpc_export_jahresablesung_status(integer) to authenticated;

create or replace function public.rpc_export_jahresablesung_jahre()
returns table (label text, value text)
language sql
stable
security invoker
as $$
  select distinct extract(year from a.ablesedatum)::integer::text as label,
         extract(year from a.ablesedatum)::integer::text as value
  from public.zaehler_ablesung a
  union
  select distinct s.jahr::text, s.jahr::text
  from public.saison s
  order by 1 desc;
$$;

grant execute on function public.rpc_export_jahresablesung_jahre() to authenticated;

insert into public.app_export_definition
  (export_key, titel, beschreibung, quelle_typ, quelle_name, aktiv, standard_sortierung, standard_ausgabe, erlaubt_csv, erlaubt_pdf)
values
  ('jahresablesung_status', 'Jahresablesung – Status',
   'JEA-Status aller am Jahresende aktiven Zähler, einschließlich Pächterbezug und Wechselhinweis.',
   'rpc', 'rpc_export_jahresablesung_status', true, 'parzelle', 'csv', true, true)
on conflict (export_key) do update set
  titel = excluded.titel, beschreibung = excluded.beschreibung, quelle_typ = excluded.quelle_typ,
  quelle_name = excluded.quelle_name, aktiv = excluded.aktiv, standard_sortierung = excluded.standard_sortierung,
  standard_ausgabe = excluded.standard_ausgabe, erlaubt_csv = excluded.erlaubt_csv, erlaubt_pdf = excluded.erlaubt_pdf;

delete from public.app_export_filter_definition where export_key = 'jahresablesung_status';
insert into public.app_export_filter_definition (export_key, filter_key, label, typ, optionen_json, pflicht, sortierung) values
  ('jahresablesung_status', 'p_jahr', 'Jahr', 'select', to_jsonb('rpc_export_jahresablesung_jahre'::text), false, 10);

delete from public.app_export_column_definition where export_key = 'jahresablesung_status';
insert into public.app_export_column_definition
  (export_key, column_key, label_kurz, label_lang, sortierung, standard_sichtbar, ist_sortierspalte) values
  ('jahresablesung_status', 'jahr', 'Jahr', 'Auswertungsjahr', 10, true, false),
  ('jahresablesung_status', 'parzelle', 'Parzelle', 'Parzelle', 20, true, true),
  ('jahresablesung_status', 'medium', 'Medium', 'Medium', 30, true, false),
  ('jahresablesung_status', 'zaehlernummer', 'Zähler', 'Zählernummer', 40, true, false),
  ('jahresablesung_status', 'status', 'JEA-Status', 'Status der Jahresendablesung', 50, true, false),
  ('jahresablesung_status', 'jae_ablesedatum', 'JEA-Datum', 'Datum der Jahresendablesung', 60, true, false),
  ('jahresablesung_status', 'jae_stand', 'JEA-Stand', 'Zählerstand der Jahresendablesung', 70, true, false),
  ('jahresablesung_status', 'pruefstatus', 'Prüfung', 'Prüfstatus', 80, true, false),
  ('jahresablesung_status', 'pachtbezug', 'Pächter', 'Pächter am Jahresende', 90, true, true),
  ('jahresablesung_status', 'pachtwechsel_im_jahr', 'Wechsel', 'Pachtwechsel im Jahr', 100, true, false),
  ('jahresablesung_status', 'eingebaut_am', 'Einbau', 'Zähler eingebaut am', 110, false, false),
  ('jahresablesung_status', 'zaehler_id', 'Zähler-ID', 'Technische Zähler-ID', 120, false, false);
