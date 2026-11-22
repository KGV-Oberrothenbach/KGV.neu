-- Mirrored DemoDB migration 20261122190917_fr2_05_meter_reading_rfid_rights
-- Source of truth in DemoDB: supabase_migrations.schema_migrations. Do not apply again here.
ALTER POLICY zaehler_select_effective_permission ON public.zaehler USING (
  public.ap02_has_effective_right('zaehler.lesen') OR public.ap02_has_effective_right('ablesung.erfassen')
  OR public.ap02_has_effective_right('ablesung.freigeben') OR public.ap02_has_effective_right('zaehlerwechsel.verwalten')
  OR public.ap02_has_effective_right('rfid.verwalten')
  OR EXISTS(SELECT 1 FROM public.parzellen_belegung pb WHERE pb.parzelle_id=zaehler.parzelle_id AND pb.mitglied_id=public.current_mitglied_id())
);
ALTER POLICY zaehler_ablesung_select_effective_permission ON public.zaehler_ablesung USING (
  public.ap02_has_effective_right('zaehler.lesen') OR public.ap02_has_effective_right('ablesung.erfassen')
  OR public.ap02_has_effective_right('ablesung.freigeben') OR public.ap02_has_effective_right('zaehlerwechsel.verwalten')
  OR public.can_access_own_meter_reading(id)
);
ALTER POLICY zaehler_ablesung_update_effective_meter_permission ON public.zaehler_ablesung
  USING(public.ap02_has_effective_right('ablesung.freigeben'))
  WITH CHECK(public.ap02_has_effective_right('ablesung.freigeben'));
ALTER POLICY parzelle_select_effective_or_own_lease ON public.parzelle USING (
  public.ap02_has_effective_right('parzelle.lesen') OR public.ap02_has_effective_right('parzelle.bearbeiten')
  OR public.ap02_has_effective_right('belegung.verwalten') OR public.ap02_has_effective_right('ablesung.erfassen')
  OR public.ap02_has_effective_right('ablesung.freigeben') OR public.ap02_has_effective_right('zaehlerwechsel.verwalten')
  OR public.ap02_has_effective_right('rfid.verwalten') OR public.can_access_own_lease_parcel(id)
);
ALTER POLICY hauptzaehler_messstelle_select_can_read_meters ON public.hauptzaehler_messstelle USING (
  public.ap02_has_effective_right('zaehler.lesen') OR public.ap02_has_effective_right('ablesung.erfassen')
  OR public.ap02_has_effective_right('zaehlerwechsel.verwalten') OR public.ap02_has_effective_right('rfid.verwalten')
);
ALTER POLICY app_setting_update_internal ON public.app_setting USING (
  public.ap02_has_effective_right('verein.konfiguration')
  OR (setting_key='allow_user_meter_reading_submissions' AND public.ap02_has_effective_right('ablesung.einstellungen'))
) WITH CHECK (
  public.ap02_has_effective_right('verein.konfiguration')
  OR (setting_key='allow_user_meter_reading_submissions' AND public.ap02_has_effective_right('ablesung.einstellungen'))
);
ALTER POLICY app_setting_write_internal ON public.app_setting WITH CHECK (
  public.ap02_has_effective_right('verein.konfiguration')
  OR (setting_key='allow_user_meter_reading_submissions' AND public.ap02_has_effective_right('ablesung.einstellungen'))
);
INSERT INTO public.app_setting(setting_key,bool_value,updated_at)
 VALUES('allow_user_meter_reading_submissions',false,now()) ON CONFLICT(setting_key) DO NOTHING;
CREATE OR REPLACE FUNCTION public.fr205_restrict_rfid_column_change()
RETURNS trigger LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog,public AS $$
BEGIN
 IF (NEW.rfid_strom IS DISTINCT FROM OLD.rfid_strom OR NEW.rfid_wasser IS DISTINCT FROM OLD.rfid_wasser)
    AND auth.uid() IS NOT NULL AND NOT public.ap02_has_effective_right('rfid.verwalten') THEN
   RAISE EXCEPTION 'Die RFID-Verwaltung erfordert rfid.verwalten' USING ERRCODE='42501';
 END IF;
 RETURN NEW;
END;
$$;
DROP TRIGGER IF EXISTS tr_fr205_restrict_rfid_column_change ON public.parzelle;
CREATE TRIGGER tr_fr205_restrict_rfid_column_change BEFORE UPDATE OF rfid_strom,rfid_wasser
 ON public.parzelle FOR EACH ROW EXECUTE FUNCTION public.fr205_restrict_rfid_column_change();
CREATE OR REPLACE FUNCTION public.assign_parzelle_rfid(p_parzelle_id bigint,p_medium public.zaehler_medium,p_rfid_tag_uid text)
RETURNS public.parzelle LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog,public AS $$
DECLARE v_uid text:=btrim(coalesce(p_rfid_tag_uid,'')); v_row public.parzelle;
BEGIN
 IF NOT public.ap02_has_effective_right('rfid.verwalten') THEN RAISE EXCEPTION 'RFID-Verwaltung nicht erlaubt' USING ERRCODE='42501'; END IF;
 IF p_medium IS NULL OR p_medium::text NOT IN('wasser','strom') THEN RAISE EXCEPTION 'Ungültiges Zählermedium' USING ERRCODE='23514'; END IF;
 IF v_uid='' THEN RAISE EXCEPTION 'RFID darf nicht leer sein' USING ERRCODE='23514'; END IF;
 IF EXISTS(SELECT 1 FROM public.parzelle p WHERE p.id<>p_parzelle_id AND
     (upper(btrim(coalesce(p.rfid_wasser,'')))=upper(v_uid) OR upper(btrim(coalesce(p.rfid_strom,'')))=upper(v_uid))) THEN
   RAISE EXCEPTION 'RFID ist bereits zugeordnet' USING ERRCODE='23505';
 END IF;
 IF p_medium::text='wasser' THEN
   UPDATE public.parzelle SET rfid_wasser=v_uid WHERE id=p_parzelle_id RETURNING * INTO v_row;
 ELSE
   UPDATE public.parzelle SET rfid_strom=v_uid WHERE id=p_parzelle_id RETURNING * INTO v_row;
 END IF;
 IF v_row.id IS NULL THEN RAISE EXCEPTION 'Parzelle nicht gefunden' USING ERRCODE='P0002'; END IF;
 RETURN v_row;
END;
$$;
REVOKE ALL ON FUNCTION public.assign_parzelle_rfid(bigint,public.zaehler_medium,text) FROM PUBLIC,anon;
GRANT EXECUTE ON FUNCTION public.assign_parzelle_rfid(bigint,public.zaehler_medium,text) TO authenticated;
CREATE OR REPLACE FUNCTION public.resolve_rfid_scan_context(p_rfid_tag_uid text)
 RETURNS TABLE(parzelle_id bigint,ist_hauptzaehler boolean,anlagenbereich text,anlage text,garten_nr text,medium text,rfid_tag_uid text,aktiver_zaehler_id bigint,zaehlernummer text,eichdatum date,eichfaellig_am date,eingebaut_am date,ausgebaut_am date,status text)
 LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog,public AS $$
DECLARE v_uid text:=upper(btrim(coalesce(p_rfid_tag_uid,'')));
BEGIN
 IF NOT(public.ap02_has_effective_right('zaehler.lesen') OR public.ap02_has_effective_right('ablesung.erfassen')
   OR public.ap02_has_effective_right('zaehlerwechsel.verwalten') OR public.ap02_has_effective_right('rfid.verwalten')) THEN
   RAISE EXCEPTION 'RFID-Kontext nicht lesbar' USING ERRCODE='42501';
 END IF;
 IF v_uid='' THEN RETURN; END IF;
 RETURN QUERY
 SELECT v.parzelle_id::bigint,false,null::text,v.anlage,v.garten_nr,v.medium::text,v.rfid_tag_uid,v.aktiver_zaehler_id::bigint,
    v.zaehlernummer,v.eichdatum,v.eichfaellig_am,v.eingebaut_am,v.ausgebaut_am,v.status::text
 FROM public.v_rfid_scan_context v WHERE upper(btrim(v.rfid_tag_uid))=v_uid
 UNION ALL
 SELECT null::bigint,true,h.anlagenbereich,h.anlagenbereich,null::text,h.medium,h.rfid_tag_uid,z.id,z.zaehlernummer,
   z.eichdatum,z.eichfaellig_am,z.eingebaut_am,z.ausgebaut_am,z.status::text
 FROM public.hauptzaehler_messstelle h LEFT JOIN public.zaehler z ON z.ist_hauptzaehler=true
   AND z.medium::text=h.medium AND z.anlagenbereich=h.anlagenbereich AND z.status='aktiv' AND z.ausgebaut_am IS NULL
 WHERE upper(btrim(h.rfid_tag_uid))=v_uid;
END;
$$;
REVOKE ALL ON FUNCTION public.resolve_rfid_scan_context(text) FROM PUBLIC,anon;
GRANT EXECUTE ON FUNCTION public.resolve_rfid_scan_context(text) TO authenticated;
