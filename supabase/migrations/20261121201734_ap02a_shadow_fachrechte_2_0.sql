-- AP-02.1A | ADDITIVES SHADOW-SCHEMA | KGV Fachrechte 2.0
-- Basis: AP-01 Katalog 21.11.2026, 55 Rechte in 10 Gruppen.
-- Keine Umschaltung der alten RLS/RPC/Edge- oder Client-Autorisierung.
-- Keine Benutzer-Hauptrollen automatisch zuweisen (Altrole admin/vorstand/user ist fachlich uneindeutig).
-- Nur lokal erzeugen und pruefen; Migration erst nach gesonderter Demo-Freigabe anwenden.
-- Keine personenbezogenen UUIDs als Literale im Repository.


CREATE TABLE public.fachrecht_gruppe (
  code text PRIMARY KEY CHECK (code ~ '^[A-J]$'),
  titel text NOT NULL CHECK (btrim(titel) <> ''),
  sort_order integer NOT NULL UNIQUE CHECK (sort_order BETWEEN 1 AND 10),
  aktiv boolean NOT NULL DEFAULT true
);
CREATE TABLE public.fachrecht (
  code text PRIMARY KEY CHECK (code ~ '^[a-z_]+[.][a-z_]+$'),
  gruppe_code text NOT NULL REFERENCES public.fachrecht_gruppe(code),
  titel text NOT NULL CHECK (btrim(titel) <> ''),
  ressourcen_typ text NOT NULL,
  objekt_scope text NOT NULL,
  operation text NOT NULL,
  status text NOT NULL,
  katalog_version text NOT NULL DEFAULT '2.0',
  aktiv boolean NOT NULL DEFAULT true
);
CREATE TABLE public.hauptrolle (
  code text PRIMARY KEY,
  titel text NOT NULL,
  aktiv boolean NOT NULL DEFAULT true
);
CREATE TABLE public.hauptrolle_fachrecht (
  hauptrolle_code text NOT NULL REFERENCES public.hauptrolle(code),
  fachrecht_code text NOT NULL REFERENCES public.fachrecht(code),
  PRIMARY KEY(hauptrolle_code,fachrecht_code)
);
CREATE TABLE public.zusatzrolle (
  code text PRIMARY KEY,
  titel text NOT NULL,
  aktiv boolean NOT NULL DEFAULT true
);
CREATE TABLE public.zusatzrolle_fachrecht (
  zusatzrolle_code text NOT NULL REFERENCES public.zusatzrolle(code),
  fachrecht_code text NOT NULL REFERENCES public.fachrecht(code),
  PRIMARY KEY(zusatzrolle_code,fachrecht_code)
);
CREATE TABLE public.app_user_hauptrolle (
  user_id uuid PRIMARY KEY REFERENCES public.app_user(user_id) ON DELETE CASCADE,
  hauptrolle_code text NOT NULL REFERENCES public.hauptrolle(code),
  gesetzt_am timestamptz NOT NULL DEFAULT now(),
  gesetzt_von uuid
);
CREATE TABLE public.app_user_zusatzrolle (
  user_id uuid NOT NULL REFERENCES public.app_user(user_id) ON DELETE CASCADE,
  zusatzrolle_code text NOT NULL REFERENCES public.zusatzrolle(code),
  gesetzt_am timestamptz NOT NULL DEFAULT now(),
  gesetzt_von uuid,
  PRIMARY KEY(user_id,zusatzrolle_code)
);
CREATE TABLE public.vereinsfunktion_fachrecht (
  vereinsfunktion_schluessel text NOT NULL REFERENCES public.vereinsfunktion(schluessel),
  fachrecht_code text NOT NULL REFERENCES public.fachrecht(code),
  PRIMARY KEY (vereinsfunktion_schluessel,fachrecht_code)
);
CREATE TABLE public.app_user_fachrecht_override (
  user_id uuid NOT NULL REFERENCES public.app_user(user_id) ON DELETE CASCADE,
  fachrecht_code text NOT NULL REFERENCES public.fachrecht(code),
  entscheidung text NOT NULL CHECK (entscheidung IN ('grant','deny')),
  grund text NOT NULL DEFAULT '',
  gesetzt_von uuid,
  gesetzt_am timestamptz NOT NULL DEFAULT now(),
  PRIMARY KEY(user_id,fachrecht_code)
);
-- Absichtlich KEIN ON DELETE CASCADE: das Schutzregister ueberlebt Loeschversuche.
CREATE TABLE public.protected_admin_identity (
  user_id uuid PRIMARY KEY,
  registriert_am timestamptz NOT NULL DEFAULT now()
);
CREATE TABLE public.berechtigungs_audit (
  id bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  zeit timestamptz NOT NULL DEFAULT now(),
  akteur uuid,
  aktion text NOT NULL CHECK (btrim(aktion) <> ''),
  zieltyp text NOT NULL CHECK (btrim(zieltyp) <> ''),
  zielschluessel text NOT NULL,
  vorher jsonb,
  nachher jsonb,
  request_id uuid,
  herkunft text NOT NULL DEFAULT 'ap02'
);
CREATE INDEX idx_ap02_override_fachrecht ON public.app_user_fachrecht_override(fachrecht_code);
CREATE INDEX idx_ap02_extra_role ON public.app_user_zusatzrolle(zusatzrolle_code);
CREATE INDEX idx_ap02_member_function_right ON public.vereinsfunktion_fachrecht(fachrecht_code);


INSERT INTO public.fachrecht_gruppe(code,titel,sort_order) VALUES
  ('A', 'Mitglieder & Mitgliedschaft', 1),
  ('B', 'Parzellen, Pacht & Wertermittlung', 2),
  ('C', 'Zähler & Ablesungen', 3),
  ('D', 'Finanzen & Jahresabschluss', 4),
  ('E', 'Arbeitsstunden & Arbeitseinsätze', 5),
  ('F', 'Dokumente & Vorlagen', 6),
  ('G', 'Verträge, Vorgänge & Kommunikation', 7),
  ('H', 'Vereinsorganisation', 8),
  ('I', 'Auswertungen & Export', 9),
  ('J', 'Benutzer & Systemsicherheit', 10);


INSERT INTO public.fachrecht(code,gruppe_code,titel,ressourcen_typ,objekt_scope,operation,status,aktiv) VALUES
  ('mitglied.lesen', 'A', 'Mitglieder lesen', 'mitglied', 'fremd_zulaessig', 'lesen', 'katalogisiert', true),
  ('mitglied.bearbeiten', 'A', 'Mitglieder bearbeiten', 'mitglied', 'fremd_zulaessig', 'benannte Aktion', 'katalogisiert', true),
  ('mitglied.anlegen', 'A', 'Mitglieder anlegen', 'mitglied', 'vorgang', 'benannte Aktion', 'katalogisiert', true),
  ('mitgliedschaft.verwalten', 'A', 'Mitgliedschaft verwalten', 'mitgliedschaft', 'vorgang', 'benannte Aktion', 'katalogisiert', true),
  ('mitgliedschaft.entscheiden', 'A', 'Aufnahmeentscheidung treffen', 'aufnahmeentscheidung', 'amts_und_prozessgebunden', 'benannte Aktion', 'katalogisiert', true),
  ('gesetzliche_vertreter.verwalten', 'A', 'Gesetzliche Vertreter verwalten', 'mitglied_gesetzlicher_vertreter', 'fremd_zulaessig', 'benannte Aktion', 'katalogisiert', true),
  ('parzelle.lesen', 'B', 'Parzellen lesen', 'parzelle', 'zulaessige_parzellen', 'lesen', 'katalogisiert', true),
  ('parzelle.bearbeiten', 'B', 'Parzellen bearbeiten', 'parzelle', 'zulaessige_parzellen', 'benannte Aktion', 'katalogisiert', true),
  ('belegung.verwalten', 'B', 'Belegungen verwalten', 'parzellen_belegung', 'vorgang', 'benannte Aktion', 'katalogisiert', true),
  ('pachtvertrag.verwalten', 'B', 'Pachtverträge verwalten', 'pachtverhaeltnis', 'vorgang', 'benannte Aktion', 'katalogisiert', true),
  ('historische_vertraege.erfassen', 'B', 'Historische Verträge erfassen', 'historical_contract_operation', 'vorgang', 'benannte Aktion', 'katalogisiert', true),
  ('begehung.verwalten', 'B', 'Begehungen und Protokolle verwalten', 'parzellen_protokoll', 'zugewiesen', 'benannte Aktion', 'katalogisiert', true),
  ('wertermittlung.bearbeiten', 'B', 'Wertermittlungen bearbeiten', 'wertermittlung', 'zugewiesen', 'benannte Aktion', 'nicht_implementiert', false),
  ('zaehler.lesen', 'C', 'Zählerdaten lesen', 'zaehler', 'zulaessiger_zugriff', 'lesen', 'katalogisiert', true),
  ('ablesung.erfassen', 'C', 'Ablesungen erfassen', 'zaehler_ablesung', 'zugewiesen_oder_eigen', 'benannte Aktion', 'katalogisiert', true),
  ('ablesung.freigeben', 'C', 'Eingereichte Ablesungen freigeben', 'zaehler_ablesung', 'eingereicht', 'benannte Aktion', 'katalogisiert', true),
  ('zaehlerwechsel.verwalten', 'C', 'Zählerwechsel verwalten', 'zaehlerwechsel_antrag', 'vorgang', 'benannte Aktion', 'katalogisiert', true),
  ('rfid.verwalten', 'C', 'RFID-Zuordnungen verwalten', 'zaehler', 'vorgang', 'benannte Aktion', 'katalogisiert', true),
  ('ablesung.einstellungen', 'C', 'Ablesungseinstellungen verwalten', 'app_setting', 'konfiguration', 'benannte Aktion', 'katalogisiert', true),
  ('finanzen.lesen', 'D', 'Finanzdaten lesen', 'jahresabschluss', 'zulaessig', 'lesen', 'katalogisiert', true),
  ('finanzen.rechnungen', 'D', 'Rechnungen und Vorauszahlungen bearbeiten', 'jahresabschluss_rechnung', 'vorgang', 'benannte Aktion', 'katalogisiert', true),
  ('finanzen.kostenarten', 'D', 'Kosten- und Umlagearten verwalten', 'kostenart', 'konfiguration', 'benannte Aktion', 'katalogisiert', true),
  ('jahresabschluss.vorbereiten', 'D', 'Jahresabschluss vorbereiten', 'jahresabschluss', 'saison', 'benannte Aktion', 'katalogisiert', true),
  ('jahresabschluss.finalisieren', 'D', 'Jahresabschluss endgültig abschließen', 'jahresabschluss', 'saison_und_zeitpunkt', 'benannte Aktion', 'gesperrt_fachentscheidung', false),
  ('jahresabschluss.korrigieren', 'D', 'Korrekturabschluss durchführen', 'jahresabschluss_korrektur', 'saison', 'benannte Aktion', 'gesperrt_fachentscheidung', false),
  ('arbeitsstunden.lesen', 'E', 'Arbeitsstunden lesen', 'arbeitsstunde', 'zulaessig', 'lesen', 'katalogisiert', true),
  ('arbeitsstunden.bearbeiten', 'E', 'Arbeitsstunden erfassen und bearbeiten', 'arbeitsstunde', 'zugewiesen', 'benannte Aktion', 'katalogisiert', true),
  ('arbeitsstunden.freigeben', 'E', 'Arbeitsstunden freigeben', 'arbeitsstunde', 'pruefvorgang', 'benannte Aktion', 'katalogisiert', true),
  ('arbeitseinsatz.verwalten', 'E', 'Arbeitseinsätze verwalten', 'arbeitseinsatz', 'vorgang', 'benannte Aktion', 'katalogisiert', true),
  ('dokument.lesen', 'F', 'Dokumente lesen', 'dokument', 'einzeldokument', 'lesen', 'katalogisiert', true),
  ('dokument.verwalten', 'F', 'Dokumente verwalten', 'dokument', 'einzeldokument', 'benannte Aktion', 'katalogisiert', true),
  ('dokument.archivieren', 'F', 'Dokumente archivieren', 'dokument', 'einzeldokument', 'benannte Aktion', 'katalogisiert', true),
  ('dokumenttyp.verwalten', 'F', 'Dokumenttypen verwalten', 'dokumenttyp', 'konfiguration', 'benannte Aktion', 'katalogisiert', true),
  ('dokumentwirkung.verwalten', 'F', 'Dokumentwirkungen verwalten', 'dokumenttyp_wirkung', 'rechtliche_wirkung', 'benannte Aktion', 'katalogisiert', true),
  ('vorlage.verwalten', 'F', 'PDF- und Serienbriefvorlagen verwalten', 'pdf_template_catalog', 'vorlage', 'benannte Aktion', 'katalogisiert', true),
  ('vereinsunterlagen.verwalten', 'F', 'Vereinsunterlagen verwalten', 'vereinsdokument_fassung', 'dokumenttyp', 'benannte Aktion', 'katalogisiert', true),
  ('vorgang.lesen', 'G', 'Vorgänge lesen', 'kommunikationsvorgang', 'zugewiesen', 'lesen', 'katalogisiert', true),
  ('vorgang.bearbeiten', 'G', 'Vorgänge bearbeiten', 'kommunikationsvorgang', 'zugewiesen', 'benannte Aktion', 'katalogisiert', true),
  ('vertrag.vorbereiten', 'G', 'Vertragsunterlagen vorbereiten', 'kommunikationsvorgang', 'prozess', 'benannte Aktion', 'katalogisiert', true),
  ('vertrag.versenden', 'G', 'Vertragsversand durchführen', 'versandinstanz', 'prozess', 'benannte Aktion', 'katalogisiert', true),
  ('druck.verwalten', 'G', 'Druckausgabe verwalten', 'dokument', 'prozess', 'benannte Aktion', 'katalogisiert', true),
  ('serienbrief.erstellen', 'G', 'Serienbriefe erstellen', 'serienbrief_vorlage', 'daten_scope', 'benannte Aktion', 'katalogisiert', true),
  ('vorstandsvorgang.bearbeiten', 'G', 'Vorstandsvorgänge bearbeiten', 'mitgliedsaufnahme_vorstandsbearbeitung', 'zugewiesen', 'benannte Aktion', 'katalogisiert', true),
  ('termin.verwalten', 'H', 'Termine verwalten', 'termin', 'verein', 'benannte Aktion', 'katalogisiert', true),
  ('bekanntmachung.verwalten', 'H', 'Bekanntmachungen verwalten', 'bekanntmachung', 'verein', 'benannte Aktion', 'katalogisiert', true),
  ('vereinsfunktion.verwalten', 'H', 'Vereinsfunktionen verwalten', 'vereinsfunktion_besetzung', 'verein', 'benannte Aktion', 'katalogisiert', true),
  ('verein.konfiguration', 'H', 'Vereinskonfiguration verwalten', 'vereinskonfiguration', 'verein', 'benannte Aktion', 'katalogisiert', true),
  ('saison.verwalten', 'H', 'Saisonverwaltung', 'saison', 'saison', 'benannte Aktion', 'katalogisiert', true),
  ('auswertung.lesen', 'I', 'Auswertungen lesen', 'app_export_definition', 'daten_scope', 'lesen', 'katalogisiert', true),
  ('export.ausfuehren', 'I', 'Daten exportieren', 'app_export_definition', 'daten_scope', 'benannte Aktion', 'katalogisiert', true),
  ('benutzer.lesen', 'J', 'Benutzerkonten lesen', 'app_user', 'zulassig', 'lesen', 'katalogisiert', true),
  ('benutzer.verwalten', 'J', 'Benutzerkonten verwalten', 'app_user', 'admingebunden', 'benannte Aktion', 'katalogisiert', true),
  ('berechtigungen.lesen', 'J', 'Rollen und Berechtigungen lesen', 'app_user', 'zulassig', 'lesen', 'katalogisiert', true),
  ('berechtigungen.verwalten', 'J', 'Rollen und Berechtigungen verwalten', 'app_user', 'admingebunden', 'benannte Aktion', 'katalogisiert', true),
  ('vereinsdaten.alle_lesen', 'J', 'Alle Vereinsdaten lesen', 'diverse', 'admin_lesezweck', 'lesen', 'katalogisiert', true);


INSERT INTO public.hauptrolle(code,titel) VALUES
  ('mitglied', 'Mitglied'),
  ('bauausschuss', 'Bauausschuss'),
  ('vorstand', 'Vorstand'),
  ('vorstand_schriftfuehrer', 'Vorstand-Schriftführer'),
  ('vorstand_kassenwart', 'Vorstand-Kassenwart');


INSERT INTO public.zusatzrolle(code,titel) VALUES
  ('wertermittlung', 'Wertermittlung'),
  ('admin', 'Admin');


INSERT INTO public.hauptrolle_fachrecht(hauptrolle_code,fachrecht_code) VALUES
  ('vorstand', 'mitglied.lesen'),
  ('vorstand_schriftfuehrer', 'mitglied.lesen'),
  ('vorstand_kassenwart', 'mitglied.lesen'),
  ('bauausschuss', 'parzelle.lesen'),
  ('vorstand', 'parzelle.lesen'),
  ('vorstand_schriftfuehrer', 'parzelle.lesen'),
  ('vorstand_kassenwart', 'parzelle.lesen'),
  ('bauausschuss', 'begehung.verwalten'),
  ('vorstand_kassenwart', 'finanzen.lesen'),
  ('vorstand_kassenwart', 'finanzen.rechnungen'),
  ('vorstand_kassenwart', 'jahresabschluss.vorbereiten'),
  ('vorstand_schriftfuehrer', 'dokument.verwalten'),
  ('vorstand_schriftfuehrer', 'vereinsunterlagen.verwalten'),
  ('vorstand', 'vorgang.lesen'),
  ('vorstand_schriftfuehrer', 'vorgang.lesen'),
  ('vorstand_kassenwart', 'vorgang.lesen'),
  ('vorstand_schriftfuehrer', 'serienbrief.erstellen'),
  ('vorstand_schriftfuehrer', 'bekanntmachung.verwalten');


INSERT INTO public.zusatzrolle_fachrecht(zusatzrolle_code,fachrecht_code) VALUES
  ('wertermittlung', 'parzelle.lesen'),
  ('wertermittlung', 'wertermittlung.bearbeiten'),
  ('admin', 'benutzer.lesen'),
  ('admin', 'benutzer.verwalten'),
  ('admin', 'berechtigungen.lesen'),
  ('admin', 'berechtigungen.verwalten'),
  ('admin', 'vereinsdaten.alle_lesen');


INSERT INTO public.vereinsfunktion_fachrecht(vereinsfunktion_schluessel,fachrecht_code)
SELECT p.vereinsfunktion_schluessel,p.fachrecht_code
FROM (VALUES
  ('vorsitz_1', 'ablesung.einstellungen'),
  ('vorsitz_2', 'ablesung.einstellungen'),
  ('vorsitz_1', 'dokumentwirkung.verwalten'),
  ('vorsitz_2', 'dokumentwirkung.verwalten'),
  ('vorsitz_1', 'vereinsfunktion.verwalten'),
  ('vorsitz_2', 'vereinsfunktion.verwalten')
) AS p(vereinsfunktion_schluessel,fachrecht_code)
JOIN public.vereinsfunktion vf ON vf.schluessel=p.vereinsfunktion_schluessel;


INSERT INTO public.protected_admin_identity(user_id)
SELECT au.user_id FROM public.app_user au WHERE au.is_protected = true;
-- Bestehende geschuetzte Identitaet besitzt die unverlierbare Admin-Zusatzrolle
-- im Schattenmodell; bestehende app_user.role bleibt unveraendert.
INSERT INTO public.app_user_zusatzrolle(user_id,zusatzrolle_code)
SELECT pai.user_id,'admin' FROM public.protected_admin_identity pai
JOIN public.app_user au ON au.user_id=pai.user_id;


-- Schema-Schutz: neue Tabellen sind nicht direkt fuer Browser/MAUI schreibbar.
-- Auch bei default privileges kein versehentlicher Data-API Zugriff.
DO $ap02$
DECLARE v_table text;
BEGIN
  FOREACH v_table IN ARRAY ARRAY[
    'fachrecht_gruppe','fachrecht','hauptrolle','hauptrolle_fachrecht',
    'zusatzrolle','zusatzrolle_fachrecht','app_user_hauptrolle',
    'app_user_zusatzrolle','vereinsfunktion_fachrecht',
    'app_user_fachrecht_override','protected_admin_identity','berechtigungs_audit'
  ] LOOP
    EXECUTE format('ALTER TABLE public.%I ENABLE ROW LEVEL SECURITY',v_table);
    EXECUTE format('REVOKE ALL ON TABLE public.%I FROM PUBLIC, anon, authenticated',v_table);
  END LOOP;
END;
$ap02$;
REVOKE ALL ON SEQUENCE public.berechtigungs_audit_id_seq FROM PUBLIC, anon, authenticated;


-- Schutz auch bei Service-Role- oder FK-Kaskaden: Loeschung/Entzug der
-- geschuetzten Admin-Zusatzrolle wird vor physischer Mutation abgewiesen.
CREATE FUNCTION public.ap02_schutz_admin_zusatzrolle()
RETURNS trigger LANGUAGE plpgsql SECURITY DEFINER
SET search_path = public, pg_temp AS $fn$
BEGIN
  IF TG_OP='DELETE' AND OLD.zusatzrolle_code='admin' AND
     EXISTS (SELECT 1 FROM public.protected_admin_identity p WHERE p.user_id=OLD.user_id)
  THEN
    RAISE EXCEPTION 'Geschuetzte Admin-Zusatzrolle darf nicht entzogen werden' USING ERRCODE='42501';
  END IF;
  IF TG_OP='UPDATE' AND OLD.zusatzrolle_code='admin' AND
     EXISTS (SELECT 1 FROM public.protected_admin_identity p WHERE p.user_id=OLD.user_id)
     AND (NEW.user_id IS DISTINCT FROM OLD.user_id OR NEW.zusatzrolle_code IS DISTINCT FROM 'admin')
  THEN
    RAISE EXCEPTION 'Geschuetzte Admin-Zusatzrolle darf nicht veraendert werden' USING ERRCODE='42501';
  END IF;
  IF TG_OP='DELETE' THEN
    RETURN OLD;
  END IF;
  RETURN NEW;
END;
$fn$;
CREATE TRIGGER trg_ap02_admin_role_guard
BEFORE UPDATE OR DELETE ON public.app_user_zusatzrolle
FOR EACH ROW EXECUTE FUNCTION public.ap02_schutz_admin_zusatzrolle();

CREATE FUNCTION public.ap02_immutable_block()
RETURNS trigger LANGUAGE plpgsql SECURITY DEFINER
SET search_path = public, pg_temp AS $fn$
BEGIN
  RAISE EXCEPTION 'Geschuetzte AP-02 Daten duerfen nicht aktualisiert, geloescht oder geleert werden'
    USING ERRCODE='42501';
END;
$fn$;
CREATE TRIGGER trg_ap02_admin_role_no_truncate
BEFORE TRUNCATE ON public.app_user_zusatzrolle
FOR EACH STATEMENT EXECUTE FUNCTION public.ap02_immutable_block();
CREATE TRIGGER trg_ap02_protected_identity_immutable
BEFORE UPDATE OR DELETE OR TRUNCATE ON public.protected_admin_identity
FOR EACH STATEMENT EXECUTE FUNCTION public.ap02_immutable_block();
CREATE TRIGGER trg_ap02_audit_immutable
BEFORE UPDATE OR DELETE OR TRUNCATE ON public.berechtigungs_audit
FOR EACH STATEMENT EXECUTE FUNCTION public.ap02_immutable_block();
-- TO DISCUSS: Fuer geplante externe Recovery ausschliesslich kontrollierte
-- DB-Administrationsmigration mit separatem Audit, nie App- oder Edge-API.


-- Wertermittlung nur bei Nicht-Vorstandshauptrolle (bidirektionale Invariante).
CREATE FUNCTION public.ap02_pruefe_zusatzrolle()
RETURNS trigger LANGUAGE plpgsql SECURITY DEFINER
SET search_path=public, pg_temp AS $fn$
BEGIN
  IF NEW.zusatzrolle_code='wertermittlung' AND NOT EXISTS (
    SELECT 1 FROM public.app_user_hauptrolle h
    WHERE h.user_id=NEW.user_id AND h.hauptrolle_code IN ('mitglied','bauausschuss')
  ) THEN
    RAISE EXCEPTION 'Wertermittlung erfordert Hauptrolle Mitglied oder Bauausschuss'
      USING ERRCODE='23514';
  END IF;
  RETURN NEW;
END;
$fn$;
CREATE TRIGGER trg_ap02_extra_role_valid
BEFORE INSERT OR UPDATE ON public.app_user_zusatzrolle
FOR EACH ROW EXECUTE FUNCTION public.ap02_pruefe_zusatzrolle();
CREATE FUNCTION public.ap02_pruefe_hauptrolle()
RETURNS trigger LANGUAGE plpgsql SECURITY DEFINER
SET search_path=public, pg_temp AS $fn$
BEGIN
  IF NEW.hauptrolle_code IN ('vorstand','vorstand_schriftfuehrer','vorstand_kassenwart')
     AND EXISTS (SELECT 1 FROM public.app_user_zusatzrolle z
                 WHERE z.user_id=NEW.user_id AND z.zusatzrolle_code='wertermittlung') THEN
    RAISE EXCEPTION 'Vorstandshauptrolle und Wertermittlung sind unvereinbar'
      USING ERRCODE='23514';
  END IF;
  RETURN NEW;
END;
$fn$;
CREATE TRIGGER trg_ap02_main_role_valid
BEFORE INSERT OR UPDATE ON public.app_user_hauptrolle
FOR EACH ROW EXECUTE FUNCTION public.ap02_pruefe_hauptrolle();


-- Read-only Schattenauswertung der NEUEN fachlichen Rechte des aktuellen
-- Nutzers. NICHT mit dem Legacy-Zugang verknuepft, KEINE Freigabe fuer
-- Fachaktionen! Bei fehlender Hauptrolle bleibt das Grundpaket leer.
CREATE FUNCTION public.ap02_preview_effective_rights()
RETURNS TABLE (
  code text, gruppe text, titel text, erlaubt boolean,
  individuell_entzogen boolean, quellen text[]
)
LANGUAGE sql STABLE SECURITY DEFINER
SET search_path=public, pg_temp AS $fn$
WITH kontext AS (
  SELECT u.user_id, u.mitglied_id, h.hauptrolle_code
  FROM public.app_user u
  LEFT JOIN public.app_user_hauptrolle h ON h.user_id=u.user_id
  WHERE u.user_id=auth.uid() AND u.mitglied_id IS NOT NULL
), kandidaten AS (
  SELECT r.fachrecht_code AS right_code, 'hauptrolle:'||h.hauptrolle_code AS origin
  FROM kontext k JOIN public.app_user_hauptrolle h ON h.user_id=k.user_id
  JOIN public.hauptrolle_fachrecht r ON r.hauptrolle_code=h.hauptrolle_code
  UNION ALL
  SELECT r.fachrecht_code, 'zusatzrolle:'||z.zusatzrolle_code
  FROM kontext k JOIN public.app_user_zusatzrolle z ON z.user_id=k.user_id
  JOIN public.zusatzrolle_fachrecht r ON r.zusatzrolle_code=z.zusatzrolle_code
  WHERE z.zusatzrolle_code<>'wertermittlung'
     OR k.hauptrolle_code IN ('mitglied','bauausschuss')
  UNION ALL
  SELECT r.fachrecht_code, 'vereinsfunktion:'||f.schluessel
  FROM kontext k
  JOIN public.vereinsfunktion_besetzung b ON b.mitglied_id=k.mitglied_id
  JOIN public.vereinsfunktion f ON f.id=b.vereinsfunktion_id AND f.aktiv
  JOIN public.vereinsfunktion_fachrecht r ON r.vereinsfunktion_schluessel=f.schluessel
  WHERE b.gueltig_ab<=current_date
    AND (b.gueltig_bis IS NULL OR b.gueltig_bis>=current_date)
  UNION ALL
  SELECT o.fachrecht_code, 'individuelle_gewaehrung'
  FROM kontext k JOIN public.app_user_fachrecht_override o ON o.user_id=k.user_id
  WHERE o.entscheidung='grant'
), quellenliste AS (
  SELECT k.right_code, array_agg(DISTINCT k.origin ORDER BY k.origin) AS origins
  FROM kandidaten k GROUP BY k.right_code
)
SELECT f.code, f.gruppe_code, f.titel,
  (
    EXISTS(SELECT 1 FROM kontext)
    AND f.aktiv AND q.right_code IS NOT NULL
    AND o.entscheidung IS DISTINCT FROM 'deny'
    AND (f.code NOT IN ('benutzer.verwalten','berechtigungen.verwalten') OR
         EXISTS(SELECT 1 FROM kontext k JOIN public.app_user_zusatzrolle z
                ON z.user_id=k.user_id AND z.zusatzrolle_code='admin'))
  ) AS erlaubt,
  coalesce(o.entscheidung='deny',false) AS individuell_entzogen,
  coalesce(q.origins,ARRAY[]::text[]) AS quellen
FROM public.fachrecht f
LEFT JOIN quellenliste q ON q.right_code=f.code
LEFT JOIN public.app_user_fachrecht_override o
 ON o.fachrecht_code=f.code AND o.user_id=(SELECT user_id FROM kontext)
ORDER BY f.gruppe_code,f.code;
$fn$;
-- Default EXECUTE TO PUBLIC wird hier explizit entzogen.
REVOKE ALL ON FUNCTION public.ap02_preview_effective_rights() FROM PUBLIC, anon, authenticated;
GRANT EXECUTE ON FUNCTION public.ap02_preview_effective_rights() TO authenticated;
REVOKE ALL ON FUNCTION public.ap02_schutz_admin_zusatzrolle() FROM PUBLIC, anon, authenticated;
REVOKE ALL ON FUNCTION public.ap02_immutable_block() FROM PUBLIC, anon, authenticated;
REVOKE ALL ON FUNCTION public.ap02_pruefe_zusatzrolle() FROM PUBLIC, anon, authenticated;
REVOKE ALL ON FUNCTION public.ap02_pruefe_hauptrolle() FROM PUBLIC, anon, authenticated;


-- KEIN Legacy-Umschalter, KEINE Migration der bestehenden app_user.role,
-- KEINE RLS-/Edge-/Auth-Aenderung in diesem Teilpaket.
COMMENT ON FUNCTION public.ap02_preview_effective_rights() IS
  'AP02.1A preview only, niemals als alleiniger Fachobjekt-/Amts-/Vorgangs-Guard verwenden';
