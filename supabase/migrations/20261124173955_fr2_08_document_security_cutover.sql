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
-- FR2-08 completion hardening. All checks execute in the same migration transaction.
-- A service-role client may call only the password-gated Edge path, never a user JWT.
-- The RPC deliberately accepts a verified actor ID supplied by the trusted Edge
-- function, then re-evaluates the live effective rights inside the write transaction.

-- Remove the permissive OR arm that exposed maintenance-document assignments.
DROP POLICY IF EXISTS dokument_wartungsvertrag_zuordnung_read_effective
  ON public.dokument_wartungsvertrag_zuordnung;
DROP POLICY IF EXISTS dokument_wartungsvertrag_zuordnung_read_fr208
  ON public.dokument_wartungsvertrag_zuordnung;
CREATE OR REPLACE FUNCTION public.fr208_can_read_maintenance_document_link(
  p_dokument_id bigint, p_zuordnung_id bigint
) RETURNS boolean LANGUAGE sql STABLE SECURITY DEFINER
SET search_path TO 'pg_catalog', 'public'
AS $f$
  SELECT auth.uid() IS NOT NULL AND EXISTS (
    SELECT 1
      FROM public.dokument d
      JOIN public.wartungsvertrag_zuordnungen wz ON wz.id=p_zuordnung_id
     WHERE d.id=p_dokument_id AND d.archiviert_at IS NULL
       AND (
         d.source_wartungsvertrag_zuordnung_id=wz.id
         OR d.mitglied_id IN (wz.hauptmitglied_id,wz.vertragspartei_mitglied_id)
       )
       AND (
         public.ap02_has_effective_right('vereinsdaten.alle_lesen')
         OR (
           public.ap02_has_effective_right('dokument.lesen')
           AND public.ap02_can_read_mitglied(wz.hauptmitglied_id)
           AND (d.mitglied_id IS NULL OR public.ap02_can_read_mitglied(d.mitglied_id))
         )
         OR (
           wz.hauptmitglied_id=public.current_mitglied_id()
           AND (public.can_access_own_member_history_document(d.id)
             OR public.can_access_own_historical_lease_document(d.id))
         )
       )
  );
$f$;
REVOKE ALL ON FUNCTION public.fr208_can_read_maintenance_document_link(bigint,bigint)
  FROM PUBLIC,anon;
GRANT EXECUTE ON FUNCTION public.fr208_can_read_maintenance_document_link(bigint,bigint)
  TO authenticated;
CREATE POLICY dokument_wartungsvertrag_zuordnung_read_fr208
  ON public.dokument_wartungsvertrag_zuordnung FOR SELECT TO authenticated
  USING (public.fr208_can_read_maintenance_document_link(dokument_id,wartungsvertrag_zuordnung_id));

-- Server-side existence check does not disclose document row IDs or content.
CREATE OR REPLACE FUNCTION public.fr208_dokumenttyp_ist_verwendet(p_dokumenttyp_id bigint)
RETURNS boolean LANGUAGE plpgsql STABLE SECURITY DEFINER
SET search_path TO 'pg_catalog','public'
AS $f$
BEGIN
  IF auth.uid() IS NULL OR NOT (
    public.ap02_has_effective_right('dokumenttyp.verwalten')
    OR public.ap02_has_effective_right('dokumentwirkung.verwalten')) THEN
    RAISE EXCEPTION 'Keine Berechtigung zur Dokumenttypenprüfung' USING ERRCODE='42501';
  END IF;
  IF p_dokumenttyp_id IS NULL OR p_dokumenttyp_id<=0 THEN
    RAISE EXCEPTION 'Ungültige Dokumenttyp-ID' USING ERRCODE='22023';
  END IF;
  RETURN EXISTS(SELECT 1 FROM public.dokument WHERE dokumenttyp_id=p_dokumenttyp_id);
END $f$;
REVOKE ALL ON FUNCTION public.fr208_dokumenttyp_ist_verwendet(bigint) FROM PUBLIC,anon;
GRANT EXECUTE ON FUNCTION public.fr208_dokumenttyp_ist_verwendet(bigint) TO authenticated;

-- Metadata-only save. The legacy signature remains for compatibility;
-- p_wirkungen is not allowed to mutate effects through this endpoint.
CREATE OR REPLACE FUNCTION public.save_dokumenttyp(
  p_dokumenttyp_id bigint,p_bezeichnung text,p_aktiv boolean,
  p_sortierung integer,p_wirkungen text[] DEFAULT NULL
) RETURNS TABLE(id bigint,bezeichnung text,aktiv boolean,sortierung integer)
LANGUAGE plpgsql SECURITY DEFINER SET search_path TO 'pg_catalog','public'
AS $f$
DECLARE v_id bigint;
BEGIN
  IF auth.uid() IS NULL OR NOT public.ap02_has_effective_right('dokumenttyp.verwalten') THEN
    RAISE EXCEPTION 'Keine Berechtigung für Dokumenttyp-Metadaten' USING ERRCODE='42501';
  END IF;
  IF NULLIF(btrim(p_bezeichnung),'') IS NULL OR p_sortierung IS NULL THEN
    RAISE EXCEPTION 'Bezeichnung und Sortierung erforderlich' USING ERRCODE='22023';
  END IF;
  IF p_dokumenttyp_id IS NULL THEN
    IF p_wirkungen IS NOT NULL AND cardinality(p_wirkungen)>0 THEN
      RAISE EXCEPTION 'Wirkungen separat speichern' USING ERRCODE='55000';
    END IF;
    v_id:=nextval('public.dokumenttyp_id_seq'::regclass);
    INSERT INTO public.dokumenttyp(id,code,bezeichnung,aktiv,sortierung)
      VALUES(v_id,format('dokumenttyp_%s',v_id),btrim(p_bezeichnung),COALESCE(p_aktiv,true),p_sortierung);
  ELSE
    SELECT dt.id INTO v_id FROM public.dokumenttyp dt
      WHERE dt.id=p_dokumenttyp_id FOR UPDATE;
    IF NOT FOUND THEN
      RAISE EXCEPTION 'Dokumenttyp existiert nicht' USING ERRCODE='22023';
    END IF;
    -- Old mixed-mode clients cannot silently overwrite a concurrent effect change.
    IF p_wirkungen IS NOT NULL
       AND (SELECT COALESCE(array_agg(dw.wirkung ORDER BY dw.wirkung),'{}'::text[])
              FROM public.dokumenttyp_wirkung dw WHERE dw.dokumenttyp_id=v_id)
          IS DISTINCT FROM
          (SELECT COALESCE(array_agg(item.wirkung ORDER BY item.wirkung),'{}'::text[])
              FROM unnest(p_wirkungen) item(wirkung)) THEN
      RAISE EXCEPTION 'Wirkungen wurden geändert; bitte separat aktualisieren' USING ERRCODE='55000';
    END IF;
    UPDATE public.dokumenttyp dt
      SET bezeichnung=btrim(p_bezeichnung),aktiv=COALESCE(p_aktiv,true),sortierung=p_sortierung
      WHERE dt.id=v_id;
  END IF;
  RETURN QUERY SELECT dt.id,dt.bezeichnung,dt.aktiv,dt.sortierung
    FROM public.dokumenttyp dt WHERE dt.id=v_id;
  RETURN;
END $f$;
REVOKE ALL ON FUNCTION public.save_dokumenttyp(bigint,text,boolean,integer,text[])
  FROM PUBLIC,anon;
GRANT EXECUTE ON FUNCTION public.save_dokumenttyp(bigint,text,boolean,integer,text[])
  TO authenticated;

-- The unversioned two-argument writer is not safe against a lost update.
REVOKE ALL ON FUNCTION public.fr208_save_dokumenttyp_wirkungen(bigint,text[])
  FROM PUBLIC,anon,authenticated;
CREATE OR REPLACE FUNCTION public.fr208_set_dokumenttyp_wirkungen_checked(
  p_dokumenttyp_id bigint,p_wirkungen text[],p_expected_wirkungen text[]
) RETURNS boolean LANGUAGE plpgsql SECURITY DEFINER
SET search_path TO 'pg_catalog','public'
AS $f$
DECLARE v_existing text[]; v_requested text[]; v_expected text[];
BEGIN
  IF auth.uid() IS NULL OR NOT public.ap02_has_effective_right('dokumentwirkung.verwalten') THEN
    RAISE EXCEPTION 'Keine Berechtigung für Dokumentwirkungen' USING ERRCODE='42501';
  END IF;
  IF p_dokumenttyp_id IS NULL OR p_dokumenttyp_id<=0
     OR p_wirkungen IS NULL OR p_expected_wirkungen IS NULL
     OR EXISTS(SELECT 1 FROM unnest(p_wirkungen) i(wirkung)
       WHERE wirkung IS NULL OR wirkung NOT IN
       ('mitgliedsantrag_nachweis','pachtvertrag_nachweis','wartungsvertrag_nachweis',
        'pachtbezug_erforderlich','wartungsvertragsbezug_erforderlich'))
     OR cardinality(p_wirkungen)<>(SELECT count(DISTINCT wirkung) FROM unnest(p_wirkungen) i(wirkung))
     OR cardinality(p_expected_wirkungen)<>(SELECT count(DISTINCT wirkung) FROM unnest(p_expected_wirkungen) i(wirkung))
  THEN RAISE EXCEPTION 'Ungültige Dokumentwirkungen' USING ERRCODE='22023';
  END IF;
  -- Shared parent-row serialization with document usage trigger.
  PERFORM 1 FROM public.dokumenttyp WHERE id=p_dokumenttyp_id FOR UPDATE;
  IF NOT FOUND THEN RAISE EXCEPTION 'Dokumenttyp existiert nicht' USING ERRCODE='22023'; END IF;
  SELECT COALESCE(array_agg(wirkung ORDER BY wirkung),'{}'::text[]) INTO v_existing
    FROM public.dokumenttyp_wirkung WHERE dokumenttyp_id=p_dokumenttyp_id;
  SELECT COALESCE(array_agg(wirkung ORDER BY wirkung),'{}'::text[]) INTO v_requested
    FROM unnest(p_wirkungen) i(wirkung);
  SELECT COALESCE(array_agg(wirkung ORDER BY wirkung),'{}'::text[]) INTO v_expected
    FROM unnest(p_expected_wirkungen) i(wirkung);
  -- Idempotent repeat is okay even when the original expected state is now old.
  IF v_existing IS NOT DISTINCT FROM v_requested THEN RETURN true; END IF;
  IF v_existing IS DISTINCT FROM v_expected THEN
    RAISE EXCEPTION 'Wirkungen wurden inzwischen geändert; bitte neu laden' USING ERRCODE='55000';
  END IF;
  IF EXISTS(SELECT 1 FROM public.dokument WHERE dokumenttyp_id=p_dokumenttyp_id) THEN
    RAISE EXCEPTION 'Historisch verwendete Dokumentwirkungen sind gesperrt' USING ERRCODE='55000';
  END IF;
  DELETE FROM public.dokumenttyp_wirkung WHERE dokumenttyp_id=p_dokumenttyp_id;
  INSERT INTO public.dokumenttyp_wirkung(dokumenttyp_id,wirkung)
    SELECT p_dokumenttyp_id,wirkung FROM unnest(v_requested) i(wirkung);
  RETURN true;
END $f$;
REVOKE ALL ON FUNCTION public.fr208_set_dokumenttyp_wirkungen_checked(bigint,text[],text[])
  FROM PUBLIC,anon;
GRANT EXECUTE ON FUNCTION public.fr208_set_dokumenttyp_wirkungen_checked(bigint,text[],text[])
  TO authenticated;

-- Every document usage obtains a shared lock on each referenced type. An
-- effects writer takes the conflicting exclusive lock on the same type row.
CREATE OR REPLACE FUNCTION public.fr208_lock_document_type_usage()
RETURNS trigger LANGUAGE plpgsql SECURITY DEFINER
SET search_path TO 'pg_catalog','public'
AS $f$
DECLARE v_ids bigint[];
BEGIN
  IF TG_OP='INSERT' THEN
    v_ids:=ARRAY[NEW.dokumenttyp_id];
  ELSE
    v_ids:=ARRAY[OLD.dokumenttyp_id,NEW.dokumenttyp_id];
  END IF;
  PERFORM 1 FROM public.dokumenttyp dt
    WHERE dt.id=ANY(v_ids) ORDER BY dt.id FOR SHARE;
  RETURN NEW;
END $f$;
DROP TRIGGER IF EXISTS fr208_lock_document_type_usage ON public.dokument;
CREATE TRIGGER fr208_lock_document_type_usage
  BEFORE INSERT OR UPDATE OF dokumenttyp_id ON public.dokument
  FOR EACH ROW EXECUTE FUNCTION public.fr208_lock_document_type_usage();

CREATE OR REPLACE FUNCTION public.fr208_guard_historical_document_effect()
RETURNS trigger LANGUAGE plpgsql SECURITY DEFINER
SET search_path TO 'pg_catalog','public'
AS $f$
DECLARE v_types bigint[];
BEGIN
  IF TG_OP='INSERT' THEN
    v_types:=ARRAY[NEW.dokumenttyp_id];
  ELSIF TG_OP='DELETE' THEN
    v_types:=ARRAY[OLD.dokumenttyp_id];
  ELSE
    v_types:=ARRAY[OLD.dokumenttyp_id,NEW.dokumenttyp_id];
  END IF;
  PERFORM 1 FROM public.dokumenttyp dt
    WHERE dt.id=ANY(v_types) ORDER BY dt.id FOR UPDATE;
  IF EXISTS(SELECT 1 FROM public.dokument d WHERE d.dokumenttyp_id=ANY(v_types)) THEN
    RAISE EXCEPTION 'Historisch verwendete Dokumentwirkungen sind gesperrt' USING ERRCODE='55000';
  END IF;
  IF TG_OP='DELETE' THEN RETURN OLD; END IF;
  RETURN NEW;
END $f$;
REVOKE ALL ON FUNCTION public.fr208_guard_historical_document_effect() FROM PUBLIC,anon,authenticated;
REVOKE ALL ON FUNCTION public.fr208_lock_document_type_usage() FROM PUBLIC,anon,authenticated;

-- A late contract attachment must take the parent document row lock and
-- cannot attach a lease/maintenance contract to an already archived document.
CREATE OR REPLACE FUNCTION public.fr208_guard_document_attachment()
RETURNS trigger LANGUAGE plpgsql SECURITY DEFINER
SET search_path TO 'pg_catalog','public'
AS $f$
DECLARE v_archive timestamptz;
BEGIN
  IF NEW.dokument_id IS NULL THEN RETURN NEW; END IF;
  SELECT archiviert_at INTO v_archive
    FROM public.dokument WHERE id=NEW.dokument_id FOR UPDATE;
  IF NOT FOUND OR v_archive IS NOT NULL THEN
    RAISE EXCEPTION 'Vertragsdokument fehlt oder ist archiviert' USING ERRCODE='55000';
  END IF;
  RETURN NEW;
END $f$;
DROP TRIGGER IF EXISTS fr208_guard_pacht_attachment ON public.pachtvertragsfassung;
CREATE TRIGGER fr208_guard_pacht_attachment
  BEFORE INSERT OR UPDATE OF dokument_id ON public.pachtvertragsfassung
  FOR EACH ROW EXECUTE FUNCTION public.fr208_guard_document_attachment();
DROP TRIGGER IF EXISTS fr208_guard_maintenance_attachment ON public.dokument_wartungsvertrag_zuordnung;
CREATE TRIGGER fr208_guard_maintenance_attachment
  BEFORE INSERT OR UPDATE OF dokument_id ON public.dokument_wartungsvertrag_zuordnung
  FOR EACH ROW EXECUTE FUNCTION public.fr208_guard_document_attachment();
REVOKE ALL ON FUNCTION public.fr208_guard_document_attachment() FROM PUBLIC,anon,authenticated;

-- Physical deletion of registered documents is forbidden (including service_role).
CREATE OR REPLACE FUNCTION public.fr208_block_document_delete()
RETURNS trigger LANGUAGE plpgsql SECURITY DEFINER
SET search_path TO 'pg_catalog','public'
AS $f$
BEGIN
  RAISE EXCEPTION 'Registrierte Dokumente dürfen nicht physisch gelöscht werden' USING ERRCODE='55000';
END $f$;
DROP TRIGGER IF EXISTS fr208_block_document_delete ON public.dokument;
CREATE TRIGGER fr208_block_document_delete BEFORE DELETE ON public.dokument
FOR EACH ROW EXECUTE FUNCTION public.fr208_block_document_delete();
REVOKE ALL ON FUNCTION public.fr208_block_document_delete() FROM PUBLIC,anon,authenticated;

-- This RPC is reachable exclusively with the trusted server's service_role.
-- The Edge function has already authenticated the end-user and verified the
-- archive password. Authorization is re-evaluated after the document lock.
CREATE OR REPLACE FUNCTION public.fr208_archive_dokument_service(
  p_dokument_id bigint,p_actor uuid,p_reason text
) RETURNS bigint LANGUAGE plpgsql SECURITY DEFINER
SET search_path TO 'pg_catalog','public'
AS $f$
DECLARE v_d public.dokument%ROWTYPE;
BEGIN
  IF auth.role() IS DISTINCT FROM 'service_role' THEN
    RAISE EXCEPTION 'Ausschließlich interner Archivdienst' USING ERRCODE='42501';
  END IF;
  IF p_dokument_id IS NULL OR p_dokument_id<=0 OR p_actor IS NULL
     OR length(btrim(COALESCE(p_reason,'')))<3 THEN
    RAISE EXCEPTION 'Dokument/Benutzer/Grund ungültig' USING ERRCODE='22023';
  END IF;
  -- The caller may supply only the user ID from verified auth.getUser(token).
  -- Transaction-local auth context is never accepted from an end-user RPC.
  PERFORM set_config('request.jwt.claim.sub',p_actor::text,true);
  SELECT d.* INTO v_d FROM public.dokument d WHERE d.id=p_dokument_id FOR UPDATE;
  IF NOT FOUND THEN
    RAISE EXCEPTION 'Dokument nicht vorhanden' USING ERRCODE='55000';
  END IF;
  IF NOT public.ap02_has_effective_right('dokument.archivieren') THEN
    RAISE EXCEPTION 'Archivierungsrecht fehlt' USING ERRCODE='42501';
  END IF;
  IF NOT (
    public.ap02_has_effective_right('vereinsdaten.alle_lesen')
    OR (v_d.mitglied_id IS NOT NULL AND public.ap02_can_read_mitglied(v_d.mitglied_id::bigint))
    OR (v_d.parzelle_id IS NOT NULL AND
      (public.ap02_has_effective_right('parzelle.lesen')
       OR public.can_access_own_lease_parcel(v_d.parzelle_id::bigint)))
  ) THEN
    RAISE EXCEPTION 'Dokument außerhalb der Berechtigung' USING ERRCODE='42501';
  END IF;
  IF v_d.archiviert_at IS NOT NULL OR v_d.unterzeichnet IS TRUE
     OR v_d.source_kommunikationsvorgang_id IS NOT NULL
     OR v_d.source_wartungsvertrag_zuordnung_id IS NOT NULL
     OR v_d.historical_contract_type IS NOT NULL
     OR v_d.historische_vertragsnummer IS NOT NULL
     OR EXISTS(SELECT 1 FROM public.pachtvertragsfassung WHERE dokument_id=v_d.id)
     OR EXISTS(SELECT 1 FROM public.dokument_wartungsvertrag_zuordnung WHERE dokument_id=v_d.id)
  THEN RAISE EXCEPTION 'Dokument ist archiviert oder vertraglich gesperrt' USING ERRCODE='55000';
  END IF;
  UPDATE public.dokument
    SET archiviert_at=clock_timestamp(),archiviert_by=p_actor,
        archiviert_begruendung=btrim(p_reason)
    WHERE id=v_d.id;
  RETURN v_d.id;
END $f$;
REVOKE ALL ON FUNCTION public.fr208_archive_dokument_service(bigint,uuid,text)
  FROM PUBLIC,anon,authenticated;
GRANT EXECUTE ON FUNCTION public.fr208_archive_dokument_service(bigint,uuid,text)
  TO service_role;

-- A permissive Storage DELETE using a subquery against RLS-protected
-- vereinsdokument_fassung can mistake a registered PDF for an orphan: an
-- administrator of club documents need not have the separate broad read right.
-- Never expose direct DELETE for this bucket. Failed, unregistered uploads
-- require a separate trusted, auditable maintenance path; registered revisions
-- and historical bytes must remain immutable.
DROP POLICY IF EXISTS vereinsunterlagen_cleanup_unregistered_fr208 ON storage.objects;
COMMIT;
