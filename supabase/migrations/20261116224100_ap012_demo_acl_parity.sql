-- AP-01.2 / Entwurf B – 25 bekannte Demo-Mehrfreigaben zurücknehmen.
-- Baseline: GitHub main 94e125c1bcd86992fec084e1f084e04e4de8db0a.
-- NUR ENTWURF. Nicht ausgeführt. Kein globales GRANT/REVOKE über public.*.
-- Dieselben REVOKEs sind in Produktion überwiegend No-ops.
-- Die zwei bereits durch AP-01.1 bereinigten ACL-Differenzen bleiben unberührt.
BEGIN;

-- Die 25 konkret inventarisierten Relationen hatten in Demo anon=ALL;
-- Produktion hat für diese 25 Relationen keine entsprechenden anon-Grants.
REVOKE ALL PRIVILEGES ON TABLE
  public.app_user,
  public.arbeitseinsatz,
  public.arbeitseinsatz_anmeldung,
  public.arbeitsstunde,
  public.auth_allowlist,
  public.bekanntmachung,
  public.client_diagnostics_log,
  public.dokument,
  public.impressum_funktion_slot,
  public.mitglied,
  public.mitglied_saison,
  public.parzelle,
  public.parzellen_belegung,
  public.saison,
  public.termin,
  public.v_aktive_zaehler,
  public.v_rfid_scan_context,
  public.v_startseite_arbeitseinsatz,
  public.v_startseite_bekanntmachungen,
  public.v_startseite_termine,
  public.v_zaehler_eichstatus,
  public.wartungsvertraege,
  public.wartungsvertrag_zuordnungen,
  public.zaehler,
  public.zaehler_ablesung
FROM anon;

-- In Demo (not Prod), authenticated also had ALL on auth_allowlist.
-- No normal client requires raw access to the authentication allowlist.
-- Keep supabase_auth_admin and service_role grants untouched.
REVOKE ALL PRIVILEGES ON TABLE public.auth_allowlist FROM authenticated;

-- Six read-only views have authenticated=ALL in Demo but SELECT only in Prod.
REVOKE ALL PRIVILEGES ON TABLE
  public.v_aktive_zaehler,
  public.v_rfid_scan_context,
  public.v_startseite_arbeitseinsatz,
  public.v_startseite_bekanntmachungen,
  public.v_startseite_termine,
  public.v_zaehler_eichstatus
FROM authenticated;
GRANT SELECT ON TABLE
  public.v_aktive_zaehler,
  public.v_rfid_scan_context,
  public.v_startseite_arbeitseinsatz,
  public.v_startseite_bekanntmachungen,
  public.v_startseite_termine,
  public.v_zaehler_eichstatus
TO authenticated;

COMMIT;
