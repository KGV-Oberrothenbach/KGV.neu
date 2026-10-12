-- G14.3: Exportfilter beschreiben RPC-Parameter und kontrollierte Startwerte selbst.
-- $currentSeason ist bewusst das einzige dynamische Symbol; es führt keinen Code aus.
alter table public.app_export_filter_definition
  add column if not exists parameter_name text;

alter table public.app_export_filter_definition
  add column if not exists standardwert_json jsonb;

comment on column public.app_export_filter_definition.parameter_name is
  'RPC-Parametername; ohne Wert wird filter_key verwendet.';
comment on column public.app_export_filter_definition.standardwert_json is
  'Optionaler JSON-Startwert; "$currentSeason" ist das einzige dynamische Symbol.';

-- Kompatibler Grundwert für alle existierenden Filter.
update public.app_export_filter_definition
set parameter_name = filter_key
where parameter_name is null or btrim(parameter_name) = '';

-- Die Arbeitsstunden-RPC hat historisch fachliche Filterkeys ohne p_-Präfix.
update public.app_export_filter_definition
set parameter_name = case filter_key
  when 'jahr' then 'p_jahr'
  when 'stunden_offen' then 'p_stunden_offen'
  when 'stunden_fertig' then 'p_stunden_fertig'
  when 'wartungsvertraege' then 'p_wartungsvertraege'
  when 'ansicht' then 'p_ansicht'
  else parameter_name
end
where export_key = 'arbeitsstunden_uebersicht';

-- Bestehende Web-Defaults werden explizit und ohne Feldnamen-Heuristik modelliert.
update public.app_export_filter_definition
set standardwert_json = '"$currentSeason"'::jsonb
where standardwert_json is null
  and (export_key, filter_key) in (
    ('arbeitsstunden_uebersicht', 'jahr'),
    ('jahresablesung_status', 'p_jahr')
  );

update public.app_export_filter_definition
set standardwert_json = 'false'::jsonb
where standardwert_json is null
  and export_key = 'arbeitsstunden_uebersicht'
  and filter_key in ('stunden_offen', 'stunden_fertig', 'wartungsvertraege');

update public.app_export_filter_definition
set standardwert_json = '"zusammenfassung"'::jsonb
where standardwert_json is null
  and export_key = 'arbeitsstunden_uebersicht'
  and filter_key = 'ansicht';
