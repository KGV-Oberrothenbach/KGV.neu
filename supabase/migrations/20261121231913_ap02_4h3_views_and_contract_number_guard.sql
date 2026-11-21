-- AP-02.4H3: letzte Legacy-SQL-View-Pruefungen und Fachnummervergabe.
-- Demo zuerst. Sicherer Erhalt der Views (security_invoker=true) und Nummernserien.
DO $h3$
DECLARE src text; n integer:=0;
BEGIN
 IF (SELECT count(*) FROM public.fachrecht)<>55
 OR to_regprocedure('public.ap02_has_effective_right(text)') IS NULL
 THEN RAISE EXCEPTION 'Fachrechte 2.0 fehlen'; END IF;
 SELECT pg_get_functiondef(p.oid) INTO src
 FROM pg_proc p JOIN pg_namespace ns ON ns.oid=p.pronamespace
 WHERE ns.nspname='public' AND p.proname='reserve_fachnummer' AND p.prokind='f';
 IF src IS NULL OR position('public.is_admin_or_vorstand()' IN src)=0 THEN
   RAISE EXCEPTION 'Alte Fachnummer-Pruefung nicht vorhanden';
 END IF;
 src:=replace(src,'public.is_admin_or_vorstand()',
  '(CASE p_art
    WHEN ''mitgliedsantrag'' THEN public.ap02_has_effective_right(''mitglied.anlegen'')
    WHEN ''pachtvertrag'' THEN public.ap02_has_effective_right(''pachtvertrag.verwalten'')
    WHEN ''wartungsvertrag'' THEN public.ap02_has_effective_right(''mitgliedschaft.verwalten'')
    WHEN ''historischer_kombivertrag'' THEN public.ap02_has_effective_right(''historische_vertraege.erfassen'')
    ELSE false END)');
 EXECUTE src;
 -- Read-only annual duty hour report. Preserve member-own visibility and service use.
 SELECT pg_get_viewdef('public.v_pflichtstunden_uebersicht'::regclass,true) INTO src;
 IF position('has_effective_permission(131072::bigint)' IN src)=0
 OR position('has_effective_permission(256::bigint)' IN src)=0 THEN
   RAISE EXCEPTION 'Pflichtstunden-View-Basis unerwartet';
 END IF;
 src:=replace(src,'has_effective_permission(131072::bigint)',
     'public.ap02_has_effective_right(''arbeitsstunden.lesen'')');
 src:=replace(src,'has_effective_permission(256::bigint)',
     'public.ap02_has_effective_right(''arbeitsstunden.bearbeiten'')');
 EXECUTE format('CREATE OR REPLACE VIEW public.v_pflichtstunden_uebersicht WITH (security_invoker=true) AS %s',src);
 -- Maintenance read report: no blanket board role bypass.
 SELECT pg_get_viewdef('public.v_wartungsvertrag_zuordnung_nachweisstatus'::regclass,true) INTO src;
 IF position('is_admin_or_vorstand()' IN src)=0 THEN
   RAISE EXCEPTION 'Wartungsvertrag-View-Basis unerwartet';
 END IF;
 src:=replace(src,'is_admin_or_vorstand()',
    'public.ap02_has_effective_right(''mitgliedschaft.verwalten'')');
 EXECUTE format('CREATE OR REPLACE VIEW public.v_wartungsvertrag_zuordnung_nachweisstatus WITH (security_invoker=true) AS %s',src);
END $h3$;;
