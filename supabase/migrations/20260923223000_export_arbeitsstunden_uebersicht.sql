-- Arbeitsstundenexport: eine Zeile je Hauptmitglied und Saison.
-- Die drei booleschen Parameter sind Einschlussfilter; ohne Auswahl kommt die Gesamtliste.
create or replace function public.rpc_export_arbeitsstunden_uebersicht(
    p_jahr integer default null,
    p_stunden_offen boolean default false,
    p_stunden_fertig boolean default false,
    p_wartungsvertraege boolean default false
)
returns table (
    mitglied_id bigint,
    nachname text,
    vorname text,
    jahr integer,
    pflichtstunden_soll numeric,
    geleistete_stunden numeric,
    offene_stunden numeric,
    hat_wartungsvertrag boolean,
    wartungsvertraege text,
    status text,
    regelgrund text
)
language sql
stable
security invoker
as $$
  with basis as (
    select distinct on (v.hauptmitglied_id, v.saison_id)
      v.hauptmitglied_id as mitglied_id,
      m.name as nachname,
      m.vorname,
      v.saison_jahr as jahr,
      coalesce(v.pflichtstunden_soll, 0) as pflichtstunden_soll,
      coalesce(v.geleistete_stunden, 0) as geleistete_stunden,
      coalesce(v.offene_stunden, 0) as offene_stunden,
      coalesce(v.hat_wartungsvertrag, false) as hat_wartungsvertrag,
      coalesce(v.regelgrund, '') as regelgrund
    from public.v_pflichtstunden_uebersicht v
    join public.mitglied m on m.id = v.hauptmitglied_id
    where m.aktiv = true
      and m.is_demo = false
      and (p_jahr is null or v.saison_jahr = p_jahr)
    order by v.hauptmitglied_id, v.saison_id desc
  ), liste as (
    select b.*, coalesce(string_agg(w.titel, ', ' order by w.titel), '') as wartungsvertraege
    from basis b
    left join public.wartungsvertrag_zuordnungen z
      on z.hauptmitglied_id = b.mitglied_id
      and z.gueltig_ab <= make_date(b.jahr, 12, 31)
      and (z.gueltig_bis is null or z.gueltig_bis >= make_date(b.jahr, 1, 1))
    left join public.wartungsvertraege w on w.id = z.wartungsvertrag_id
    group by b.mitglied_id, b.nachname, b.vorname, b.jahr, b.pflichtstunden_soll,
      b.geleistete_stunden, b.offene_stunden, b.hat_wartungsvertrag, b.regelgrund
  )
  select mitglied_id, nachname, vorname, jahr, pflichtstunden_soll, geleistete_stunden,
    offene_stunden, hat_wartungsvertrag, wartungsvertraege,
    case when hat_wartungsvertrag then 'Wartungsvertrag – befreit'
         when pflichtstunden_soll > 0 and offene_stunden > 0 then 'Stunden offen'
         when pflichtstunden_soll > 0 then 'Stunden fertig'
         else 'Befreit' end,
    regelgrund
  from liste
  where not (p_stunden_offen or p_stunden_fertig or p_wartungsvertraege)
     or (p_stunden_offen and not hat_wartungsvertrag and offene_stunden > 0)
     or (p_stunden_fertig and not hat_wartungsvertrag and pflichtstunden_soll > 0 and offene_stunden <= 0)
     or (p_wartungsvertraege and hat_wartungsvertrag)
  order by nachname, vorname;
$$;

grant execute on function public.rpc_export_arbeitsstunden_uebersicht(integer, boolean, boolean, boolean) to authenticated;

-- Die mobile Exportseite liest diese Definitionen dynamisch aus der Datenbank.
insert into public.app_export_definition
  (export_key, titel, beschreibung, quelle_typ, quelle_name, aktiv, standard_sortierung, standard_ausgabe, erlaubt_csv, erlaubt_pdf)
values
  ('arbeitsstunden_uebersicht', 'Arbeitsstundenübersicht',
   'Pflicht-, geleistete und offene Stunden einschließlich Wartungsverträgen.',
   'rpc', 'rpc_export_arbeitsstunden_uebersicht', true, 'nachname', 'rpc_export_arbeitsstunden_uebersicht', true, true)
on conflict (export_key) do update set
  titel = excluded.titel, beschreibung = excluded.beschreibung, quelle_typ = excluded.quelle_typ,
  quelle_name = excluded.quelle_name, aktiv = excluded.aktiv, standard_sortierung = excluded.standard_sortierung,
  standard_ausgabe = excluded.standard_ausgabe, erlaubt_csv = excluded.erlaubt_csv, erlaubt_pdf = excluded.erlaubt_pdf;

delete from public.app_export_filter_definition where export_key = 'arbeitsstunden_uebersicht';
insert into public.app_export_filter_definition (export_key, filter_key, label, typ, pflicht, sortierung) values
  ('arbeitsstunden_uebersicht', 'jahr', 'Jahr', 'zahl', false, 10),
  ('arbeitsstunden_uebersicht', 'stunden_offen', 'Stunden offen', 'boolean', false, 20),
  ('arbeitsstunden_uebersicht', 'stunden_fertig', 'Stunden fertig', 'boolean', false, 30),
  ('arbeitsstunden_uebersicht', 'wartungsvertraege', 'Wartungsverträge', 'boolean', false, 40);

delete from public.app_export_column_definition where export_key = 'arbeitsstunden_uebersicht';
insert into public.app_export_column_definition
  (export_key, column_key, label_kurz, label_lang, sortierung, standard_sichtbar, ist_sortierspalte) values
  ('arbeitsstunden_uebersicht', 'nachname', 'Name', 'Nachname', 10, true, true),
  ('arbeitsstunden_uebersicht', 'vorname', 'Vorname', 'Vorname', 20, true, false),
  ('arbeitsstunden_uebersicht', 'jahr', 'Jahr', 'Jahr', 30, true, false),
  ('arbeitsstunden_uebersicht', 'pflichtstunden_soll', 'Soll', 'Pflichtstunden Soll', 40, true, false),
  ('arbeitsstunden_uebersicht', 'geleistete_stunden', 'Geleistet', 'Geleistete Stunden', 50, true, false),
  ('arbeitsstunden_uebersicht', 'offene_stunden', 'Offen', 'Offene Stunden', 60, true, false),
  ('arbeitsstunden_uebersicht', 'status', 'Status', 'Status', 70, true, false),
  ('arbeitsstunden_uebersicht', 'wartungsvertraege', 'Wartungsvertrag', 'Wartungsverträge', 80, true, false),
  ('arbeitsstunden_uebersicht', 'regelgrund', 'Regelgrund', 'Befreiungsgrund', 90, true, false);
