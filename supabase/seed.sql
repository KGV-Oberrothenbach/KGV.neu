-- AP-01 local-only fixture catalog.
-- This seed runs only after `supabase db reset --local`; it is never a migration
-- and must never be applied to the linked production database.

create table if not exists public.p11_ap01_test_fixture (
  fixture_id text primary key,
  rule_ids text[] not null,
  description text not null,
  data jsonb not null
);

insert into public.p11_ap01_test_fixture (fixture_id, rule_ids, description, data) values
  ('haupt-nebenmitglied', array['P11-R06', 'P11-R07'], 'Aktives Hauptmitglied mit fortbestehendem Nebenmitglied für Vertragswechsel.',
   '{"hauptmitglied":"P11-M-1001","nebenmitglied":"P11-M-1002","mitgliedschaft":"aktiv","pachtvertrag":"P11-L-1001"}'::jsonb),
  ('passive-mitgliedschaft', array['P11-R06'], 'Mitglied ohne aktive Pacht, aber mit fortbestehendem App-Zugang.',
   '{"mitglied":"P11-M-1003","mitgliedschaft":"passiv","app_zugang":true,"aktive_pacht":false}'::jsonb),
  ('pachtwechsel', array['P11-R07', 'P11-R08', 'P11-R09'], 'Nahtloser Vertragswechsel mit historischem Vorgängervertrag.',
   '{"parzelle":"P11-P-01","vorgaenger":"P11-L-1001","nachfolger":"P11-L-1002","uebergang":"nahtlos"}'::jsonb),
  ('nachsorge', array['P11-R08', 'P11-R10'], 'Vertragsende mit verbindlichem Nachsorgeende und ausstehender Übergabe.',
   '{"parzelle":"P11-P-02","vorheriger_paechter":"P11-M-1004","nachsorge_bis":"2026-12-31","uebergabeprotokoll":"faellig"}'::jsonb),
  ('zaehlerwechsel', array['P11-R14', 'P11-R15'], 'Aus- und Einbau mit getrennten Messwertsegmenten und Fotopflicht.',
   '{"parzelle":"P11-P-03","medium":"strom","altzaehler":"P11-Z-01","neuzaehler":"P11-Z-02","fotos_erforderlich":true}'::jsonb),
  ('leerstand', array['P11-R15'], 'Leerstandssegment ohne finanzielle Verbrauchsverteilung.',
   '{"parzelle":"P11-P-04","bezug_typ":"leerstand","umlagefaehig":false}'::jsonb),
  ('vereinsgarten', array['P11-R15'], 'Vereinsgartensegment ohne finanzielle Verbrauchsverteilung.',
   '{"parzelle":"P11-P-05","bezug_typ":"verein","umlagefaehig":false}'::jsonb),
  ('individuelle-rechte', array['P11-R01', 'P11-R02'], 'User mit Zählergewährung sowie privilegierte Rollen mit Entzug.',
   '{"user_mit_gewaehrung":"P11-U-1001","admin_mit_entzug":"P11-U-1002","vorstand_mit_entzug":"P11-U-1003","demo_user":"P11-U-9001"}'::jsonb)
on conflict (fixture_id) do update
set rule_ids = excluded.rule_ids,
    description = excluded.description,
    data = excluded.data;
