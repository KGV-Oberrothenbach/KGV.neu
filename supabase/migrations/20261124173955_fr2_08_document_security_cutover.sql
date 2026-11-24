-- FR2-08: Dokumentrechte, Dokumenttypen/-wirkungen, Vorlagen, private Dateien.
-- ONLY DemoDB after independent plan/review. No existing data or files are changed.
-- Intended baseline: FR2-07 migration 20261124155223.
BEGIN;
DO $guard$
BEGIN
  IF NOT EXISTS (SELECT 1 FROM supabase_migrations.schema_migrations WHERE version='20261124155223') THEN
    RAISE EXCEPTION 'FR2-07 ist nicht migriert; FR2-08 abgebrochen' USING ERRCODE='23514';
  END IF;
  IF to_regclass('public.dokument') IS NULL OR to_regclass('public.dokumenttyp_wirkung') IS NULL OR
     to_regclass('public.vereinsdokument_fassung') IS NULL OR to_regclass('public.app_user_fachrecht_override') IS NULL OR
     to_regprocedure('public.ap02_has_effective_right(text)') IS NULL OR
     to_regprocedure('public.ap02_can_read_mitglied(bigint)') IS NULL OR
     to_regprocedure('public.save_dokumenttyp(bigint,text,boolean,integer,text[])') IS NULL THEN
    RAISE EXCEPTION 'FR2-08 Basis fehlt; abgebrochen' USING ERRCODE='23514';
  END IF;
  IF NOT EXISTS (SELECT 1 FROM pg_policies WHERE schemaname='public' AND tablename='dokument' AND policyname='dokument_select_effective') THEN
    RAISE EXCEPTION 'FR2-08 Dokument-RLS-Basis abweichend' USING ERRCODE='23514';
  END IF;
END $guard$;

-- Bind ordinary uploads to an object the caller may actually administer.
CREATE OR REPLACE FUNCTION public.fr208_can_write_document_owner(p_owner_kind text,p_owner_id bigint)
RETURNS boolean LANGUAGE sql STABLE SECURITY DEFINER
SET search_path TO 'pg_catalog','public'
AS $f$
 SELECT auth.uid() IS NOT NULL
   AND p_owner_id IS NOT NULL AND p_owner_id>0
   AND public.ap02_has_effective_right('dokument.verwalten')
   AND CASE p_owner_kind
     WHEN 'mitglied' THEN public.ap02_can_read_mitglied(p_owner_id)
     WHEN 'parzelle' THEN
       public.ap02_has_effective_right('vereinsdaten.alle_lesen')
       OR public.ap02_has_effective_right('parzelle.lesen')
       OR public.can_access_own_lease_parcel(p_owner_id)
     ELSE false END;
$f$;
REVOKE ALL ON FUNCTION public.fr208_can_write_document_owner(text,bigint) FROM PUBLIC,anon;
GRANT EXECUTE ON FUNCTION public.fr208_can_write_document_owner(text,bigint) TO authenticated;

-- Read, manage and archive are deliberately separate. Self/historical/board
-- document visibility remains governed by the already established narrow RPCs.
DROP POLICY dokument_select_effective ON public.dokument;
CREATE POLICY dokument_select_effective ON public.dokument FOR SELECT TO authenticated
USING (
  (public.ap02_has_effective_right('dokument.lesen') AND (
      (mitglied_id IS NOT NULL AND public.ap02_can_read_mitglied(mitglied_id::bigint))
      OR (parzelle_id IS NOT NULL AND (
          public.ap02_has_effective_right('parzelle.lesen')
          OR public.can_access_own_lease_parcel(parzelle_id::bigint)))
  ))
  OR public.ap02_has_effective_right('vereinsdaten.alle_lesen')
  OR public.can_access_own_member_history_document(id)
  OR public.can_access_own_historical_lease_document(id)
  OR (parzelle_id IS NOT NULL AND EXISTS (
      SELECT 1 FROM public.parzellen_belegung b
      WHERE b.parzelle_id = dokument.parzelle_id
        AND b.mitglied_id = public.current_mitglied_id()
        AND (b.von_datum IS NULL OR b.von_datum <= public.kgv_local_now()::date)
        AND (b.bis_datum IS NULL OR b.bis_datum >= public.kgv_local_now()::date)
  ))
);

-- Explicit object scope for archival without granting a document read or
-- management right. Called as authenticated user immediately before the Edge
-- service's password-protected, service-role archival update.
CREATE OR REPLACE FUNCTION public.fr208_can_archive_dokument(p_dokument_id bigint)
RETURNS boolean LANGUAGE sql STABLE SECURITY DEFINER
SET search_path TO 'pg_catalog','public'
AS $f$
  SELECT auth.uid() IS NOT NULL
   AND public.ap02_has_effective_right('dokument.archivieren')
   AND EXISTS (
     SELECT 1 FROM public.dokument d
     WHERE d.id=p_dokument_id AND d.archiviert_at IS NULL
       AND d.source_kommunikationsvorgang_id IS NULL
       AND d.source_wartungsvertrag_zuordnung_id IS NULL
       AND NOT EXISTS (SELECT 1 FROM public.pachtvertragsfassung pf WHERE pf.dokument_id=d.id)
       AND NOT EXISTS (SELECT 1 FROM public.dokument_wartungsvertrag_zuordnung wz WHERE wz.dokument_id=d.id)
       AND (
         public.ap02_has_effective_right('vereinsdaten.alle_lesen')
         OR (d.mitglied_id IS NOT NULL AND public.ap02_can_read_mitglied(d.mitglied_id::bigint))
         OR (d.parzelle_id IS NOT NULL AND (
              public.ap02_has_effective_right('parzelle.lesen')
              OR public.can_access_own_lease_parcel(d.parzelle_id::bigint)))
       )
   );
$f$;
REVOKE ALL ON FUNCTION public.fr208_can_archive_dokument(bigint) FROM PUBLIC, anon;
GRANT EXECUTE ON FUNCTION public.fr208_can_archive_dokument(bigint) TO authenticated;

-- Admin's agreed broad read is explicit, without granting writes.
DROP POLICY IF EXISTS serienbrief_vorlage_read_fr208 ON public.serienbrief_vorlage;
CREATE POLICY serienbrief_vorlage_read_fr208 ON public.serienbrief_vorlage FOR SELECT TO authenticated
USING (public.ap02_has_effective_right('vereinsdaten.alle_lesen'));
DROP POLICY IF EXISTS serienbrief_vorlage_exportfeld_read_fr208 ON public.serienbrief_vorlage_exportfeld;
CREATE POLICY serienbrief_vorlage_exportfeld_read_fr208 ON public.serienbrief_vorlage_exportfeld FOR SELECT TO authenticated
USING (public.ap02_has_effective_right('vereinsdaten.alle_lesen'));
DROP POLICY IF EXISTS serienbrief_vorlage_freifeld_read_fr208 ON public.serienbrief_vorlage_freifeld;
CREATE POLICY serienbrief_vorlage_freifeld_read_fr208 ON public.serienbrief_vorlage_freifeld FOR SELECT TO authenticated
USING (public.ap02_has_effective_right('vereinsdaten.alle_lesen'));
DROP POLICY IF EXISTS vereinsdokument_fassung_read_fr208 ON public.vereinsdokument_fassung;
CREATE POLICY vereinsdokument_fassung_read_fr208 ON public.vereinsdokument_fassung FOR SELECT TO authenticated
USING (public.ap02_has_effective_right('vereinsdaten.alle_lesen'));
DROP POLICY IF EXISTS vereinsdokument_typ_read_fr208 ON public.vereinsdokument_typ;
CREATE POLICY vereinsdokument_typ_read_fr208 ON public.vereinsdokument_typ FOR SELECT TO authenticated
USING (public.ap02_has_effective_right('vereinsdaten.alle_lesen'));
DROP POLICY IF EXISTS mitglied_vereinsdokument_bereitstellung_read_fr208 ON public.mitglied_vereinsdokument_bereitstellung;
CREATE POLICY mitglied_vereinsdokument_bereitstellung_read_fr208 ON public.mitglied_vereinsdokument_bereitstellung FOR SELECT TO authenticated
USING (public.ap02_has_effective_right('vereinsdaten.alle_lesen'));
DROP POLICY IF EXISTS dokument_wartungsvertrag_zuordnung_read_fr208 ON public.dokument_wartungsvertrag_zuordnung;
CREATE POLICY dokument_wartungsvertrag_zuordnung_read_fr208 ON public.dokument_wartungsvertrag_zuordnung FOR SELECT TO authenticated
USING (
 public.ap02_has_effective_right('dokument.lesen')
 OR public.ap02_has_effective_right('vereinsdaten.alle_lesen')
 OR EXISTS (SELECT 1 FROM public.dokument d WHERE d.id=dokument_id)
);

-- Preserve types/effects already in use. Only changes to effect assignments
-- require dokumentwirkung.verwalten, independent of type metadata authority.
CREATE OR REPLACE FUNCTION public.save_dokumenttyp(
  p_dokumenttyp_id bigint, p_bezeichnung text, p_aktiv boolean,
  p_sortierung integer, p_wirkungen text[] DEFAULT '{}'::text[]
) RETURNS TABLE(id bigint, bezeichnung text, aktiv boolean, sortierung integer)
LANGUAGE plpgsql SECURITY DEFINER SET search_path TO 'pg_catalog','public'
AS $f$
DECLARE
  v_id bigint;
  v_wirkungen text[] := COALESCE(p_wirkungen,ARRAY[]::text[]);
  v_existing text[];
  v_requested text[];
  v_effects_changed boolean;
BEGIN
  IF auth.uid() IS NULL OR NOT public.ap02_has_effective_right('dokumenttyp.verwalten') THEN
    RAISE EXCEPTION 'Keine Berechtigung für Dokumenttypen' USING ERRCODE='42501';
  END IF;
  IF NULLIF(btrim(p_bezeichnung),'') IS NULL OR p_sortierung IS NULL THEN
    RAISE EXCEPTION 'Bezeichnung und Sortierung erforderlich' USING ERRCODE='22023';
  END IF;
  IF EXISTS(SELECT 1 FROM unnest(v_wirkungen) item(wirkung) WHERE wirkung IS NULL OR wirkung NOT IN
      ('mitgliedsantrag_nachweis','pachtvertrag_nachweis','wartungsvertrag_nachweis','pachtbezug_erforderlich','wartungsvertragsbezug_erforderlich'))
     OR cardinality(v_wirkungen) <> (SELECT count(DISTINCT wirkung) FROM unnest(v_wirkungen) item(wirkung)) THEN
    RAISE EXCEPTION 'Unzulässige oder doppelte Dokumentwirkung' USING ERRCODE='22023';
  END IF;
  SELECT COALESCE(array_agg(wirkung ORDER BY wirkung),ARRAY[]::text[]) INTO v_requested
     FROM unnest(v_wirkungen) item(wirkung);
  IF p_dokumenttyp_id IS NULL THEN
    v_effects_changed := cardinality(v_requested)>0;
    IF v_effects_changed AND NOT public.ap02_has_effective_right('dokumentwirkung.verwalten') THEN
      RAISE EXCEPTION 'Keine Berechtigung für Dokumentwirkungen' USING ERRCODE='42501';
    END IF;
    v_id:=nextval('public.dokumenttyp_id_seq'::regclass);
    INSERT INTO public.dokumenttyp(id,code,bezeichnung,aktiv,sortierung)
      VALUES(v_id,format('dokumenttyp_%s',v_id),btrim(p_bezeichnung),COALESCE(p_aktiv,true),p_sortierung);
  ELSE
    SELECT dt.id INTO v_id FROM public.dokumenttyp dt WHERE dt.id=p_dokumenttyp_id FOR UPDATE;
    IF NOT FOUND THEN RAISE EXCEPTION 'Der Dokumenttyp existiert nicht' USING ERRCODE='22023'; END IF;
    SELECT COALESCE(array_agg(dw.wirkung ORDER BY dw.wirkung),ARRAY[]::text[]) INTO v_existing
      FROM public.dokumenttyp_wirkung dw WHERE dw.dokumenttyp_id=v_id;
    v_effects_changed := v_existing IS DISTINCT FROM v_requested;
    IF v_effects_changed AND NOT public.ap02_has_effective_right('dokumentwirkung.verwalten') THEN
      RAISE EXCEPTION 'Keine Berechtigung für Dokumentwirkungen' USING ERRCODE='42501';
    END IF;
    UPDATE public.dokumenttyp dt SET bezeichnung=btrim(p_bezeichnung),aktiv=COALESCE(p_aktiv,true),sortierung=p_sortierung
      WHERE dt.id=v_id;
  END IF;
  IF v_effects_changed THEN
    DELETE FROM public.dokumenttyp_wirkung dw WHERE dw.dokumenttyp_id=v_id;
    INSERT INTO public.dokumenttyp_wirkung(dokumenttyp_id,wirkung)
      SELECT v_id,wirkung FROM unnest(v_requested) item(wirkung);
  END IF;
  RETURN QUERY SELECT dt.id,dt.bezeichnung,dt.aktiv,dt.sortierung FROM public.dokumenttyp dt WHERE dt.id=v_id;
  RETURN;
END $f$;
REVOKE ALL ON FUNCTION public.save_dokumenttyp(bigint,text,boolean,integer,text[]) FROM PUBLIC, anon;
GRANT EXECUTE ON FUNCTION public.save_dokumenttyp(bigint,text,boolean,integer,text[]) TO authenticated;

-- Effects may also be administered by a dedicated effect-only administrator.
-- Metadata cannot be edited from this endpoint.
CREATE OR REPLACE FUNCTION public.fr208_save_dokumenttyp_wirkungen(p_dokumenttyp_id bigint,p_wirkungen text[])
RETURNS void LANGUAGE plpgsql SECURITY DEFINER SET search_path TO 'pg_catalog','public'
AS $f$
DECLARE v_requested text[]; v_existing text[];
BEGIN
  IF auth.uid() IS NULL OR NOT public.ap02_has_effective_right('dokumentwirkung.verwalten') THEN
    RAISE EXCEPTION 'Keine Berechtigung für Dokumentwirkungen' USING ERRCODE='42501';
  END IF;
  IF p_dokumenttyp_id IS NULL OR p_wirkungen IS NULL
    OR EXISTS(SELECT 1 FROM unnest(p_wirkungen) item(wirkung) WHERE wirkung IS NULL OR wirkung NOT IN
        ('mitgliedsantrag_nachweis','pachtvertrag_nachweis','wartungsvertrag_nachweis','pachtbezug_erforderlich','wartungsvertragsbezug_erforderlich'))
    OR cardinality(p_wirkungen)<>(SELECT count(DISTINCT wirkung) FROM unnest(p_wirkungen) item(wirkung)) THEN
    RAISE EXCEPTION 'Ungültige Dokumentwirkungen' USING ERRCODE='22023';
  END IF;
  PERFORM 1 FROM public.dokumenttyp WHERE id=p_dokumenttyp_id FOR UPDATE;
  IF NOT FOUND THEN RAISE EXCEPTION 'Dokumenttyp existiert nicht' USING ERRCODE='22023'; END IF;
  SELECT COALESCE(array_agg(wirkung ORDER BY wirkung),ARRAY[]::text[]) INTO v_requested FROM unnest(p_wirkungen) item(wirkung);
  SELECT COALESCE(array_agg(wirkung ORDER BY wirkung),ARRAY[]::text[]) INTO v_existing FROM public.dokumenttyp_wirkung WHERE dokumenttyp_id=p_dokumenttyp_id;
  IF v_existing IS NOT DISTINCT FROM v_requested THEN RETURN; END IF;
  DELETE FROM public.dokumenttyp_wirkung WHERE dokumenttyp_id=p_dokumenttyp_id;
  INSERT INTO public.dokumenttyp_wirkung(dokumenttyp_id,wirkung) SELECT p_dokumenttyp_id,wirkung FROM unnest(v_requested) item(wirkung);
END $f$;
REVOKE ALL ON FUNCTION public.fr208_save_dokumenttyp_wirkungen(bigint,text[]) FROM PUBLIC, anon;
GRANT EXECUTE ON FUNCTION public.fr208_save_dokumenttyp_wirkungen(bigint,text[]) TO authenticated;

-- Effects on already classified documents must not reinterpret historical proofs.
-- Supabase service role and all RPC callers are subject to this history guard.
CREATE OR REPLACE FUNCTION public.fr208_guard_historical_document_effect()
RETURNS trigger LANGUAGE plpgsql SECURITY DEFINER
SET search_path TO 'pg_catalog','public'
AS $f$
DECLARE v_type bigint;
BEGIN
  v_type := CASE WHEN TG_OP='DELETE' THEN OLD.dokumenttyp_id ELSE NEW.dokumenttyp_id END;
  IF EXISTS(SELECT 1 FROM public.dokument d WHERE d.dokumenttyp_id=v_type) THEN
    RAISE EXCEPTION 'Wirkungen eines bereits verwendeten Dokumenttyps sind historisch gesperrt'
      USING ERRCODE='55000';
  END IF;
  IF TG_OP='DELETE' THEN RETURN OLD; END IF;
  RETURN NEW;
END $f$;
DROP TRIGGER IF EXISTS fr208_guard_historical_document_effect ON public.dokumenttyp_wirkung;
CREATE TRIGGER fr208_guard_historical_document_effect
BEFORE INSERT OR UPDATE OR DELETE ON public.dokumenttyp_wirkung
FOR EACH ROW EXECUTE FUNCTION public.fr208_guard_historical_document_effect();

-- Private Storage: no legacy role-bit exceptions, every policy rechecks live rights.
DROP POLICY IF EXISTS pdf_templates_write_admin ON storage.objects;
DROP POLICY IF EXISTS pdf_templates_read_admin ON storage.objects;
CREATE POLICY pdf_templates_read_fr208 ON storage.objects FOR SELECT TO authenticated
  USING (bucket_id='pdf-templates' AND (public.ap02_has_effective_right('vorlage.verwalten') OR public.ap02_has_effective_right('vereinsdaten.alle_lesen')));
CREATE POLICY pdf_templates_insert_fr208 ON storage.objects FOR INSERT TO authenticated
  WITH CHECK (bucket_id='pdf-templates' AND public.ap02_has_effective_right('vorlage.verwalten'));
-- Existing template bytes and hash-bound versions cannot be overwritten or deleted.
-- Template administration creates new immutable paths via INSERT only.

DROP POLICY IF EXISTS vereinsunterlagen_read_admin_vorstand ON storage.objects;
DROP POLICY IF EXISTS vereinsunterlagen_insert_admin_vorstand ON storage.objects;
DROP POLICY IF EXISTS vereinsunterlagen_cleanup_unregistered_admin_vorstand ON storage.objects;
CREATE POLICY vereinsunterlagen_read_fr208 ON storage.objects FOR SELECT TO authenticated
USING (bucket_id='vereinsregelwerke' AND (storage.foldername(name))[1]='vereinsunterlagen'
  AND (public.ap02_has_effective_right('vereinsunterlagen.verwalten') OR public.ap02_has_effective_right('vereinsdaten.alle_lesen')));
CREATE POLICY vereinsunterlagen_insert_fr208 ON storage.objects FOR INSERT TO authenticated
WITH CHECK (bucket_id='vereinsregelwerke' AND (storage.foldername(name))[1]='vereinsunterlagen'
  AND public.ap02_has_effective_right('vereinsunterlagen.verwalten'));
-- No UPDATE policy: historical revisions are immutable, including their bytes.
CREATE POLICY vereinsunterlagen_cleanup_unregistered_fr208 ON storage.objects FOR DELETE TO authenticated
USING (bucket_id='vereinsregelwerke' AND (storage.foldername(name))[1]='vereinsunterlagen'
  AND public.ap02_has_effective_right('vereinsunterlagen.verwalten')
  AND NOT EXISTS (SELECT 1 FROM public.vereinsdokument_fassung f WHERE f.storage_path=objects.name));

-- Elevated relation privileges are not needed by authenticated frontends.
REVOKE TRUNCATE, TRIGGER, REFERENCES ON TABLE
  public.dokument, public.dokumenttyp, public.dokumenttyp_wirkung,
  public.vereinsdokument_fassung, public.vereinsdokument_typ,
  public.serienbrief_vorlage,public.serienbrief_vorlage_exportfeld,public.serienbrief_vorlage_freifeld
  FROM anon, authenticated;
REVOKE INSERT, UPDATE, DELETE ON TABLE public.dokument,public.dokumenttyp,public.dokumenttyp_wirkung FROM anon,authenticated;
-- RLS has no DELETE policy for document and historic versions; explicitly deny.
REVOKE DELETE ON TABLE public.dokument,public.vereinsdokument_fassung FROM anon,authenticated;
COMMIT;
