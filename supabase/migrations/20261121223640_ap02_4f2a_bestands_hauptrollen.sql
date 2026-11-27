-- AP-02.4F.2A: Hauptrollen aus gueltigen Vereinsfunktionen und alter technischer Vorstandrolle.
-- Gemeinsamer, einmaliger Demo-/VDB-Migrationsschritt nach F0 und F1.
-- KEINE Uebernahme der alten Admin-Rolle in die neue Admin-Zusatzrolle.
-- KEINE automatische Umrechnung der alten Grant-/Revoke-Bitmasken:
-- Diese bleiben zur nachfolgenden verlustfreien Einzelfall-Migration erhalten.

CREATE TEMP TABLE ap02_4f2_role_plan ON COMMIT DROP AS
WITH amtsstatus AS (
  SELECT a.user_id,a.role AS alte_rolle,a.is_protected,
    m.id AS gueltiges_mitglied,
    COALESCE(bool_or(f.schluessel='kassenwart'),false) AS kassenwart,
    COALESCE(bool_or(f.schluessel IN ('schriftfuehrer','schriftführer')),false) AS schriftfuehrer,
    COALESCE(bool_or(f.schluessel IN ('vorsitz_1','vorsitz_2','vorstandsvorsitzender','vertreter')),false) AS vorsitz,
    COALESCE(bool_or(f.schluessel='bauausschuss'),false) AS bauausschuss
  FROM public.app_user a
  LEFT JOIN public.mitglied m
    ON m.id=a.mitglied_id AND m.auth_user_id=a.user_id
  LEFT JOIN public.vereinsfunktion_besetzung b
    ON b.mitglied_id=m.id
   AND b.gueltig_ab<=current_date
   AND (b.gueltig_bis IS NULL OR b.gueltig_bis>=current_date)
  LEFT JOIN public.vereinsfunktion f
    ON f.id=b.vereinsfunktion_id AND f.aktiv
  GROUP BY a.user_id,a.role,a.is_protected,m.id
)
SELECT user_id,alte_rolle,is_protected,kassenwart,schriftfuehrer,vorsitz,bauausschuss,
 CASE
  WHEN kassenwart THEN 'vorstand_kassenwart'
  WHEN schriftfuehrer THEN 'vorstand_schriftfuehrer'
  WHEN vorsitz OR alte_rolle='vorstand' THEN 'vorstand'
  WHEN bauausschuss THEN 'bauausschuss'
  ELSE 'mitglied'
 END::text AS neue_hauptrolle
FROM amtsstatus
WHERE gueltiges_mitglied IS NOT NULL;

DO $guards$
BEGIN
 IF EXISTS (SELECT 1 FROM public.app_user WHERE role NOT IN ('admin','vorstand','user')) THEN
   RAISE EXCEPTION 'Unbekannte technische Altrolle' USING ERRCODE='55000';
 END IF;
 IF EXISTS (SELECT 1 FROM public.app_user_hauptrolle) THEN
   RAISE EXCEPTION 'Hauptrollen wurden bereits manuell gesetzt: Migration nicht ueberschreiben'
     USING ERRCODE='55000';
 END IF;
 IF EXISTS (
   SELECT 1 FROM public.protected_admin_identity p
   WHERE NOT EXISTS (SELECT 1 FROM ap02_4f2_role_plan q WHERE q.user_id=p.user_id)
 ) THEN RAISE EXCEPTION 'Geschuetztes Adminkonto ohne gueltige Mitgliedsbindung'
    USING ERRCODE='55000'; END IF;
 IF EXISTS (
   SELECT 1 FROM public.app_user_zusatzrolle z
   WHERE z.zusatzrolle_code='admin'
     AND NOT EXISTS (SELECT 1 FROM public.protected_admin_identity p WHERE p.user_id=z.user_id)
 ) THEN RAISE EXCEPTION 'Weitere Admin-Zusatzrollen vor Migration: manuell pruefen'
    USING ERRCODE='55000'; END IF;
 IF EXISTS (SELECT 1 FROM ap02_4f2_role_plan WHERE kassenwart AND schriftfuehrer) THEN
   RAISE EXCEPTION 'Mehrdeutig: Kassenwart und Schriftfuehrer zugleich'
    USING ERRCODE='55000'; END IF;
 IF EXISTS (
   SELECT 1 FROM ap02_4f2_role_plan q
   WHERE NOT EXISTS (SELECT 1 FROM public.hauptrolle h
                     WHERE h.code=q.neue_hauptrolle AND h.aktiv)
 ) THEN RAISE EXCEPTION 'Hauptrolle im neuen Katalog fehlt'
    USING ERRCODE='55000'; END IF;
 IF NOT EXISTS (SELECT 1 FROM public.vereinsfunktion) AND (EXISTS (SELECT 1 FROM auth.users) OR
      EXISTS (SELECT 1 FROM public.app_user) OR
      EXISTS (SELECT 1 FROM public.mitglied) OR
      EXISTS (SELECT 1 FROM public.parzelle) OR
      EXISTS (SELECT 1 FROM public.pachtverhaeltnis) OR
      EXISTS (SELECT 1 FROM public.dokument) OR
      EXISTS (SELECT 1 FROM public.kommunikationsvorgang) OR
      EXISTS (SELECT 1 FROM public.zaehler) OR
      EXISTS (SELECT 1 FROM public.arbeitsstunde) OR
      EXISTS (SELECT 1 FROM public.wartungsvertraege) OR
      EXISTS (SELECT 1 FROM public.vereinsfunktion_besetzung) OR
      EXISTS (SELECT 1 FROM public.vereinsfunktion_verwendung) OR
      EXISTS (SELECT 1 FROM public.vereinsfunktion_vertretung)) THEN
   RAISE EXCEPTION 'Vereinsfunktionen fehlen bei vorhandenem Bestand' USING ERRCODE='55000';
 END IF;
 IF EXISTS (SELECT 1 FROM public.vereinsfunktion) AND (
   (SELECT count(*) FROM public.vereinsfunktion
      WHERE aktiv AND bezeichnung='1. Vorsitzender'
        AND schluessel IN ('vorsitz_1','vorstandsvorsitzender'))<>1
   OR (SELECT count(*) FROM public.vereinsfunktion
      WHERE aktiv AND bezeichnung='2. Vorsitzender'
        AND schluessel IN ('vorsitz_2','vertreter'))<>1
   OR (SELECT count(*) FROM public.vereinsfunktion
      WHERE aktiv AND schluessel='bauausschuss' AND erlaubt_mehrfachbesetzung)<>1
 ) THEN RAISE EXCEPTION 'Vereinsfunktionen F0/F1 fehlen oder weichen ab'
    USING ERRCODE='55000'; END IF;
END
$guards$;

-- Vorsitz-spezifische Rechte gelten fuer beide Altschluesselvarianten.
-- Vereinsfunktion bleibt fachlich getrennt von der technischen Hauptrolle.
INSERT INTO public.vereinsfunktion_fachrecht
  (vereinsfunktion_schluessel,fachrecht_code)
SELECT f.schluessel, r.fachrecht_code
FROM public.vereinsfunktion f
CROSS JOIN (VALUES ('ablesung.einstellungen'),
  ('dokumentwirkung.verwalten'),('vereinsfunktion.verwalten')) AS r(fachrecht_code)
WHERE f.aktiv AND f.schluessel IN
 ('vorsitz_1','vorsitz_2','vorstandsvorsitzender','vertreter')
ON CONFLICT (vereinsfunktion_schluessel,fachrecht_code) DO NOTHING;

INSERT INTO public.app_user_hauptrolle
  (user_id,hauptrolle_code,gesetzt_von)
SELECT user_id,neue_hauptrolle,NULL::uuid
FROM ap02_4f2_role_plan;

INSERT INTO public.berechtigungs_audit
  (akteur,aktion,zieltyp,zielschluessel,vorher,nachher,herkunft)
SELECT NULL::uuid,'hauptrolle_bestand_migriert','app_user',q.user_id::text,
 jsonb_build_object('alte_technische_rolle',q.alte_rolle),
 jsonb_build_object('hauptrolle',q.neue_hauptrolle,
  'herleitung','gueltige_vereinsfunktion_und_legacy_vorstand_fallback'),
 'ap02.4f2a'
FROM ap02_4f2_role_plan q;

DO $verify$
BEGIN
 IF (SELECT count(*) FROM public.app_user_hauptrolle)
   <> (SELECT count(*) FROM ap02_4f2_role_plan)
 THEN RAISE EXCEPTION 'Hauptrollenzahl stimmt nicht'
   USING ERRCODE='55000'; END IF;
 IF EXISTS (SELECT 1 FROM ap02_4f2_role_plan q
    JOIN public.app_user_hauptrolle h ON h.user_id=q.user_id
    WHERE h.hauptrolle_code IS DISTINCT FROM q.neue_hauptrolle)
 THEN RAISE EXCEPTION 'Hauptrollenzuordnung stimmt nicht'
   USING ERRCODE='55000'; END IF;
 IF EXISTS (SELECT 1 FROM public.app_user_zusatzrolle z
    WHERE z.zusatzrolle_code='admin'
      AND NOT EXISTS (SELECT 1 FROM public.protected_admin_identity p WHERE p.user_id=z.user_id))
 THEN RAISE EXCEPTION 'Ungeplante Admin-Zusatzrolle nach Migration'
   USING ERRCODE='55000'; END IF;
END
$verify$;
