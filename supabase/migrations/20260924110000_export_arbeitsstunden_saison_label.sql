-- Die Auswahlwerte kommen bereits aus public.saison; die Oberfläche bezeichnet sie fachlich als Saison.
UPDATE public.app_export_filter_definition
SET label = 'Saison'
WHERE export_key = 'arbeitsstunden_uebersicht'
  AND filter_key = 'jahr';
