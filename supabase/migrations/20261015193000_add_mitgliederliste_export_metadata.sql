-- CX7: make the central members-list export available through the metadata-driven export center.

insert into public.app_export_definition
  (export_key, titel, beschreibung, quelle_typ, quelle_name, aktiv, standard_sortierung, standard_ausgabe, erlaubt_csv, erlaubt_pdf)
values
  ('mitgliederliste', 'Mitgliederliste', 'Mitgliederliste mit Kontakt-, Garten- und Einwilligungsdaten',
   'rpc', 'rpc_export_mitgliederliste', true, 'name', 'pdf', true, true)
on conflict (export_key) do update set
  titel = excluded.titel,
  beschreibung = excluded.beschreibung,
  quelle_typ = excluded.quelle_typ,
  quelle_name = excluded.quelle_name,
  aktiv = excluded.aktiv,
  standard_sortierung = excluded.standard_sortierung,
  standard_ausgabe = excluded.standard_ausgabe,
  erlaubt_csv = excluded.erlaubt_csv,
  erlaubt_pdf = excluded.erlaubt_pdf;

delete from public.app_export_filter_definition
where export_key = 'mitgliederliste'
  and filter_key not in ('aktiv_filter', 'sortierung');

insert into public.app_export_filter_definition
  (export_key, filter_key, label, typ, optionen_json, parameter_name, wert_typ, standardwert_json, pflicht, sortierung)
values
  ('mitgliederliste', 'aktiv_filter', 'Mitglieder', 'select',
   '[{"label":"Aktive","value":"aktiv"},{"label":"Alle","value":"alle"},{"label":"Inaktive","value":"inaktiv"}]'::jsonb,
   'aktiv_filter', 'string', '"aktiv"'::jsonb, false, 10),
  ('mitgliederliste', 'sortierung', 'Sortierung', 'select',
   '[{"label":"Name","value":"name"},{"label":"Mitgliedsnummer","value":"mitgliedsnr"},{"label":"Garten","value":"garten"}]'::jsonb,
   'sortierung', 'string', '"name"'::jsonb, false, 20)
on conflict (export_key, filter_key) do update set
  label = excluded.label,
  typ = excluded.typ,
  optionen_json = excluded.optionen_json,
  parameter_name = excluded.parameter_name,
  wert_typ = excluded.wert_typ,
  standardwert_json = excluded.standardwert_json,
  pflicht = excluded.pflicht,
  sortierung = excluded.sortierung;

delete from public.app_export_column_definition
where export_key = 'mitgliederliste'
  and column_key not in (
    'mitgliedsnr', 'name', 'vorname', 'strasse_hsnr', 'plz', 'ort',
    'telefon', 'handy', 'email', 'geburtsdatum', 'mitglied_seit',
    'gartennummern', 'wa', 're', 'info', 'app', 'aktiv_export', 'bemerkung'
  );

insert into public.app_export_column_definition
  (export_key, column_key, label_kurz, label_lang, sortierung, standard_sichtbar, ist_sortierspalte)
values
  ('mitgliederliste', 'mitgliedsnr', 'Mitglieds-Nr.', 'Mitgliedsnummer', 10, true, false),
  ('mitgliederliste', 'name', 'Name', 'Nachname', 20, true, true),
  ('mitgliederliste', 'vorname', 'Vorname', 'Vorname', 30, true, false),
  ('mitgliederliste', 'strasse_hsnr', 'Straße', 'Straße und Hausnummer', 40, true, false),
  ('mitgliederliste', 'plz', 'PLZ', 'Postleitzahl', 50, true, false),
  ('mitgliederliste', 'ort', 'Ort', 'Ort', 60, true, false),
  ('mitgliederliste', 'telefon', 'Telefon', 'Telefon', 70, true, false),
  ('mitgliederliste', 'handy', 'Mobil', 'Mobiltelefon', 80, true, false),
  ('mitgliederliste', 'email', 'E-Mail', 'E-Mail', 90, true, false),
  ('mitgliederliste', 'geburtsdatum', 'Geburtsdatum', 'Geburtsdatum', 100, true, false),
  ('mitgliederliste', 'mitglied_seit', 'Mitglied seit', 'Mitglied seit', 110, true, false),
  ('mitgliederliste', 'gartennummern', 'Gärten', 'Gartennummern', 120, true, false),
  ('mitgliederliste', 'wa', 'WhatsApp', 'WhatsApp-Einwilligung', 130, true, false),
  ('mitgliederliste', 're', 'Rechnung', 'E-Mail-Rechnung-Einwilligung', 140, true, false),
  ('mitgliederliste', 'info', 'Info', 'E-Mail-Info-Einwilligung', 150, true, false),
  ('mitgliederliste', 'app', 'App', 'App vorhanden', 160, true, false),
  ('mitgliederliste', 'aktiv_export', 'Aktiv', 'Aktiv', 170, true, false),
  ('mitgliederliste', 'bemerkung', 'Bemerkung', 'Bemerkung', 180, true, false)
on conflict (export_key, column_key) do update set
  label_kurz = excluded.label_kurz,
  label_lang = excluded.label_lang,
  sortierung = excluded.sortierung,
  standard_sichtbar = excluded.standard_sichtbar,
  ist_sortierspalte = excluded.ist_sortierspalte;
