-- CX12: no-code reference export. It relies only on the metadata-driven gateway.
create or replace function public.rpc_export_parzellenliste(p_parameters jsonb default '{}'::jsonb)
returns setof jsonb
language sql
stable
security invoker
set search_path = public, pg_temp
as $$
  with settings as (
    select case lower(coalesce(nullif(btrim(p_parameters ->> 'belegung'), ''), 'alle'))
      when 'alle' then 'alle' when 'belegt' then 'belegt' when 'frei' then 'frei' else 'alle' end as belegung
  ), current_tenants as (
    select b.parzelle_id,
      string_agg(concat_ws(' ', m.vorname, m.name), ', ' order by m.name, m.vorname) as paechter
    from public.parzellen_belegung b
    join public.mitglied m on m.id = b.mitglied_id
    where b.von_datum <= current_date
      and (b.bis_datum is null or b.bis_datum >= current_date)
      and coalesce(m.is_demo, false) = false
    group by b.parzelle_id
  )
  select to_jsonb(r)
  from (
    select p.garten_nr, p."Anlage" as anlage, coalesce(t.paechter, '') as paechter,
      p.flaeche_qm, (t.parzelle_id is not null) as belegt
    from public.parzelle p
    left join current_tenants t on t.parzelle_id = p.id
    cross join settings s
    where p.aktiv = true
      and coalesce(p.is_demo, false) = false
      and (s.belegung = 'alle' or (s.belegung = 'belegt' and t.parzelle_id is not null) or (s.belegung = 'frei' and t.parzelle_id is null))
    order by
      case when nullif(btrim(p.garten_nr), '') is null then 1 else 0 end,
      nullif(substring(p.garten_nr from '^[[:space:]]*([0-9]+)'), '')::integer nulls last,
      p.garten_nr, p."Anlage", p.id
  ) r;
$$;

insert into public.app_export_definition
  (export_key, titel, beschreibung, quelle_typ, quelle_name, aktiv, standard_sortierung, standard_ausgabe, erlaubt_csv, erlaubt_pdf)
values ('parzellenliste', 'Parzellenliste', 'Übersicht der Parzellen mit aktueller Belegung', 'rpc', 'rpc_export_parzellenliste', true, 'garten_nr', 'pdf', true, true)
on conflict (export_key) do update set titel = excluded.titel, beschreibung = excluded.beschreibung, quelle_typ = excluded.quelle_typ, quelle_name = excluded.quelle_name, aktiv = true, standard_sortierung = excluded.standard_sortierung, standard_ausgabe = excluded.standard_ausgabe, erlaubt_csv = excluded.erlaubt_csv, erlaubt_pdf = excluded.erlaubt_pdf;

insert into public.app_export_filter_definition
  (export_key, filter_key, label, typ, optionen_json, parameter_name, wert_typ, standardwert_json, pflicht, sortierung)
values ('parzellenliste', 'belegung', 'Belegung', 'select', '[{"label":"Alle","value":"alle"},{"label":"Belegt","value":"belegt"},{"label":"Frei","value":"frei"}]'::jsonb, 'belegung', 'string', '"alle"'::jsonb, false, 10)
on conflict (export_key, filter_key) do update set label = excluded.label, typ = excluded.typ, optionen_json = excluded.optionen_json, parameter_name = excluded.parameter_name, wert_typ = excluded.wert_typ, standardwert_json = excluded.standardwert_json, pflicht = excluded.pflicht, sortierung = excluded.sortierung;

insert into public.app_export_column_definition
  (export_key, column_key, label_kurz, label_lang, sortierung, standard_sichtbar, ist_sortierspalte)
values
  ('parzellenliste', 'garten_nr', 'Garten', 'Gartennummer', 10, true, true),
  ('parzellenliste', 'anlage', 'Anlage', 'Anlage', 20, true, false),
  ('parzellenliste', 'paechter', 'Pächter', 'Aktuelle Pächter', 30, true, false),
  ('parzellenliste', 'flaeche_qm', 'Fläche', 'Fläche in m²', 40, true, false),
  ('parzellenliste', 'belegt', 'Belegt', 'Aktuell belegt', 50, true, false)
on conflict (export_key, column_key) do update set label_kurz = excluded.label_kurz, label_lang = excluded.label_lang, sortierung = excluded.sortierung, standard_sichtbar = excluded.standard_sichtbar, ist_sortierspalte = excluded.ist_sortierspalte;

revoke all on function public.rpc_export_parzellenliste(jsonb) from public;
