-- AP-02.4L: alte Rechte-RPCs schliessen, ohne historische Bit-Felder oder Daten zu veraendern.
-- Dieser Cutover gilt nur fuer DemoDB. ProduktivDB bleibt ausdruecklich unveraendert.
-- Alte Werte in app_user und Loesch-Reservierungen bleiben zur Nachvollziehbarkeit erhalten.
DO $pre$ BEGIN
 IF to_regprocedure('public.ap02_has_effective_right(text)') IS NULL
   OR to_regprocedure('public.ap02_can_read_mitglied(bigint)') IS NULL
   OR (SELECT count(*) FROM public.fachrecht)<>55
   OR NOT EXISTS(SELECT 1 FROM public.protected_admin_identity)
 THEN RAISE EXCEPTION 'Fachrechte 2.0 noch nicht sicher bereit'; END IF;
 IF EXISTS(SELECT 1 FROM pg_policies WHERE schemaname='public'
  AND (coalesce(qual,'')||coalesce(with_check,'')) ~* 'has_effective_permission[(]|is_admin_or_vorstand[(]|is_admin[(]')
 THEN RAISE EXCEPTION 'RLS benutzt noch Altrechte'; END IF;
 IF EXISTS(SELECT 1 FROM pg_views WHERE schemaname='public'
  AND definition ~* 'has_effective_permission[(]|is_admin_or_vorstand[(]|is_admin[(]')
 THEN RAISE EXCEPTION 'View benutzt noch Altrechte'; END IF;
END $pre$;

CREATE OR REPLACE FUNCTION public.can_read_mitglied(p_mitglied_id bigint,p_auth_user_id uuid)
RETURNS boolean LANGUAGE sql STABLE SECURITY DEFINER SET search_path TO pg_catalog,public
AS $fn$
 SELECT coalesce(
    p_auth_user_id IS NOT NULL AND p_auth_user_id = auth.uid()
    AND public.ap02_can_read_mitglied(p_mitglied_id),false)
$fn$;

-- Diese Hilfen bleiben als Kompatibilitaetssignatur erhalten, erlauben aber KEINE Aktion.
CREATE OR REPLACE FUNCTION public.has_effective_permission(p_permission bigint)
RETURNS boolean LANGUAGE sql STABLE SECURITY DEFINER SET search_path TO pg_catalog,public
AS $fn$ SELECT false $fn$;

CREATE OR REPLACE FUNCTION public.has_any_effective_permission(p_permissions bigint)
RETURNS boolean LANGUAGE sql STABLE SECURITY DEFINER SET search_path TO pg_catalog,public
AS $fn$ SELECT false $fn$;

CREATE OR REPLACE FUNCTION public.current_effective_permissions()
RETURNS bigint LANGUAGE sql STABLE SECURITY DEFINER SET search_path TO pg_catalog,public
AS $fn$ SELECT 0::bigint $fn$;

CREATE OR REPLACE FUNCTION public.permission_role_base(p_role text)
RETURNS bigint LANGUAGE sql IMMUTABLE SET search_path TO pg_catalog,public
AS $fn$ SELECT 0::bigint $fn$;

CREATE OR REPLACE FUNCTION public.is_admin()
RETURNS boolean LANGUAGE sql STABLE SECURITY DEFINER SET search_path TO pg_catalog,public
AS $fn$ SELECT false $fn$;

CREATE OR REPLACE FUNCTION public.is_admin_or_vorstand()
RETURNS boolean LANGUAGE sql STABLE SECURITY DEFINER SET search_path TO pg_catalog,public
AS $fn$ SELECT false $fn$;

CREATE OR REPLACE FUNCTION public.is_productive_admin_or_vorstand()
RETURNS boolean LANGUAGE sql STABLE SECURITY DEFINER SET search_path TO pg_catalog,public
AS $fn$ SELECT false $fn$;

-- Historische individuelle Rollenbits duerfen nicht neu gesetzt werden.
CREATE OR REPLACE FUNCTION public.update_app_user_access(
  p_user_id uuid,p_role text,p_permission_grants bigint DEFAULT 0,p_permission_revocations bigint DEFAULT 0)
RETURNS void LANGUAGE plpgsql SECURITY DEFINER SET search_path TO pg_catalog,public
AS $fn$
BEGIN
 RAISE EXCEPTION 'Alte Rechteverwaltung ist deaktiviert. Verwenden Sie Fachrechte 2.0.'
 USING ERRCODE='42501';
END $fn$;

DO $post$ BEGIN
 IF public.has_effective_permission(1) OR public.has_effective_permission(524288)
  OR public.has_any_effective_permission(4294967295)
  OR public.current_effective_permissions()<>0
  OR public.permission_role_base('admin')<>0
  OR public.is_admin() OR public.is_admin_or_vorstand() THEN
  RAISE EXCEPTION 'Legacy-Berechtigung noch aktiv'; END IF;
END $post$;;
