-- AP-02.4G1: 77 RLS-Policies von numerischen Legacy-Bits auf benannte Fachrechte 2.0.
-- Bestands-Scopes (Eigenzugriff, Sichtbarkeit, Prozessbedingungen) bleiben erhalten.
-- Keine Aenderung an Vorstands-Berechtigungsmatrix, Vereinsdaten oder historischen Dokumenten.
-- demo-getestet, ProduktivDB weiterhin gesperrt.
DO $pre$ BEGIN
  IF (SELECT count(*) FROM public.fachrecht) <> 55 OR to_regprocedure('public.ap02_has_effective_right(text)') IS NULL THEN RAISE EXCEPTION 'AP02-Rechtekern fehlt'; END IF;
  IF (SELECT count(*) FROM public.protected_admin_identity) < 1 THEN RAISE EXCEPTION 'Geschuetzte Admin-Identitaet fehlt'; END IF;
  IF (SELECT count(*) FROM pg_policies WHERE schemaname='public' AND (coalesce(qual,'')||coalesce(with_check,'')) ~* 'has_effective_permission[(]|is_admin_or_vorstand[(]|is_admin[(]') <> 77 THEN RAISE EXCEPTION 'RLS-Altbestand weicht ab'; END IF;
END $pre$;
DROP POLICY IF EXISTS "app_export_column_definition_select_effective" ON public."app_export_column_definition";
CREATE POLICY "app_export_column_definition_select_effective" ON public."app_export_column_definition" AS PERMISSIVE FOR SELECT TO "authenticated" USING (public.ap02_has_effective_right('auswertung.lesen'));

DROP POLICY IF EXISTS "app_export_definition_select_effective" ON public."app_export_definition";
CREATE POLICY "app_export_definition_select_effective" ON public."app_export_definition" AS PERMISSIVE FOR SELECT TO "authenticated" USING (public.ap02_has_effective_right('auswertung.lesen'));

DROP POLICY IF EXISTS "app_export_filter_definition_select_effective" ON public."app_export_filter_definition";
CREATE POLICY "app_export_filter_definition_select_effective" ON public."app_export_filter_definition" AS PERMISSIVE FOR SELECT TO "authenticated" USING (public.ap02_has_effective_right('auswertung.lesen'));

DROP POLICY IF EXISTS "app_setting_write_internal" ON public."app_setting";
CREATE POLICY "app_setting_write_internal" ON public."app_setting" AS PERMISSIVE FOR INSERT TO "authenticated" WITH CHECK (public.ap02_has_effective_right('verein.konfiguration'));

DROP POLICY IF EXISTS "app_setting_update_internal" ON public."app_setting";
CREATE POLICY "app_setting_update_internal" ON public."app_setting" AS PERMISSIVE FOR UPDATE TO "authenticated" USING (public.ap02_has_effective_right('verein.konfiguration')) WITH CHECK (public.ap02_has_effective_right('verein.konfiguration'));

DROP POLICY IF EXISTS "arbeitseinsatz_manage_delete" ON public."arbeitseinsatz";
CREATE POLICY "arbeitseinsatz_manage_delete" ON public."arbeitseinsatz" AS PERMISSIVE FOR DELETE TO "authenticated" USING (public.ap02_has_effective_right('arbeitseinsatz.verwalten'));

DROP POLICY IF EXISTS "arbeitseinsatz_manage_insert" ON public."arbeitseinsatz";
CREATE POLICY "arbeitseinsatz_manage_insert" ON public."arbeitseinsatz" AS PERMISSIVE FOR INSERT TO "authenticated" WITH CHECK (public.ap02_has_effective_right('arbeitseinsatz.verwalten'));

DROP POLICY IF EXISTS "arbeitseinsatz_manage_select" ON public."arbeitseinsatz";
CREATE POLICY "arbeitseinsatz_manage_select" ON public."arbeitseinsatz" AS PERMISSIVE FOR SELECT TO "authenticated" USING (public.ap02_has_effective_right('arbeitseinsatz.verwalten'));

DROP POLICY IF EXISTS "arbeitseinsatz_manage_update" ON public."arbeitseinsatz";
CREATE POLICY "arbeitseinsatz_manage_update" ON public."arbeitseinsatz" AS PERMISSIVE FOR UPDATE TO "authenticated" USING (public.ap02_has_effective_right('arbeitseinsatz.verwalten')) WITH CHECK (public.ap02_has_effective_right('arbeitseinsatz.verwalten'));

DROP POLICY IF EXISTS "arbeitseinsatz_anmeldung_manage_select" ON public."arbeitseinsatz_anmeldung";
CREATE POLICY "arbeitseinsatz_anmeldung_manage_select" ON public."arbeitseinsatz_anmeldung" AS PERMISSIVE FOR SELECT TO "authenticated" USING (public.ap02_has_effective_right('arbeitseinsatz.verwalten'));

DROP POLICY IF EXISTS "arbeitsstunde_manage_work_hours_insert" ON public."arbeitsstunde";
CREATE POLICY "arbeitsstunde_manage_work_hours_insert" ON public."arbeitsstunde" AS PERMISSIVE FOR INSERT TO "authenticated" WITH CHECK (((public.ap02_has_effective_right('arbeitsstunden.bearbeiten') AND public.ap02_has_effective_right('arbeitsstunden.freigeben')) AND (arbeitseinsatz_anmeldung_id IS NULL) AND (status = 'genehmigt'::text) AND (freigegeben = true) AND (genehmigt_von = current_mitglied_id()) AND (genehmigt_am IS NOT NULL)));

DROP POLICY IF EXISTS "arbeitsstunde_manage_work_hours_select" ON public."arbeitsstunde";
CREATE POLICY "arbeitsstunde_manage_work_hours_select" ON public."arbeitsstunde" AS PERMISSIVE FOR SELECT TO "authenticated" USING (public.ap02_has_effective_right('arbeitsstunden.lesen'));

DROP POLICY IF EXISTS "arbeitsstunde_select_own_or_admin" ON public."arbeitsstunde";
CREATE POLICY "arbeitsstunde_select_own_or_admin" ON public."arbeitsstunde" AS PERMISSIVE FOR SELECT TO "authenticated" USING ((public.ap02_has_effective_right('arbeitsstunden.lesen') OR (mitglied_id = current_mitglied_id())));

DROP POLICY IF EXISTS "arbeitsstunde_pruefverlauf_select_scoped" ON public."arbeitsstunde_pruefverlauf";
CREATE POLICY "arbeitsstunde_pruefverlauf_select_scoped" ON public."arbeitsstunde_pruefverlauf" AS PERMISSIVE FOR SELECT TO "authenticated" USING ((public.ap02_has_effective_right('arbeitsstunden.lesen') OR ((COALESCE((vorher_snapshot ->> 'mitglied_id'::text), (vorher_snapshot ->> 'MitgliedId'::text), ''::text) ~ '^[0-9]+$'::text) AND ((COALESCE((vorher_snapshot ->> 'mitglied_id'::text), (vorher_snapshot ->> 'MitgliedId'::text)))::integer = current_mitglied_id()))));

DROP POLICY IF EXISTS "bekanntmachung_manage_insert" ON public."bekanntmachung";
CREATE POLICY "bekanntmachung_manage_insert" ON public."bekanntmachung" AS PERMISSIVE FOR INSERT TO "authenticated" WITH CHECK (public.ap02_has_effective_right('bekanntmachung.verwalten'));

DROP POLICY IF EXISTS "bekanntmachung_manage_select" ON public."bekanntmachung";
CREATE POLICY "bekanntmachung_manage_select" ON public."bekanntmachung" AS PERMISSIVE FOR SELECT TO "authenticated" USING (public.ap02_has_effective_right('bekanntmachung.verwalten'));

DROP POLICY IF EXISTS "bekanntmachung_manage_update" ON public."bekanntmachung";
CREATE POLICY "bekanntmachung_manage_update" ON public."bekanntmachung" AS PERMISSIVE FOR UPDATE TO "authenticated" USING (public.ap02_has_effective_right('bekanntmachung.verwalten')) WITH CHECK (public.ap02_has_effective_right('bekanntmachung.verwalten'));

DROP POLICY IF EXISTS "dokument_insert_effective" ON public."dokument";
CREATE POLICY "dokument_insert_effective" ON public."dokument" AS PERMISSIVE FOR INSERT TO "authenticated" WITH CHECK (public.ap02_has_effective_right('dokument.verwalten'));

DROP POLICY IF EXISTS "dokument_select_effective" ON public."dokument";
CREATE POLICY "dokument_select_effective" ON public."dokument" AS PERMISSIVE FOR SELECT TO "authenticated" USING ((public.ap02_has_effective_right('dokument.lesen') OR public.ap02_has_effective_right('dokument.verwalten') OR can_access_own_member_history_document(id) OR can_access_own_historical_lease_document(id) OR ((parzelle_id IS NOT NULL) AND (EXISTS ( SELECT 1
   FROM parzellen_belegung b
  WHERE ((b.parzelle_id = dokument.parzelle_id) AND (b.mitglied_id = current_mitglied_id()) AND ((b.von_datum IS NULL) OR (b.von_datum <= (kgv_local_now())::date)) AND ((b.bis_datum IS NULL) OR (b.bis_datum >= (kgv_local_now())::date))))))));

DROP POLICY IF EXISTS "dokument_update_effective" ON public."dokument";
CREATE POLICY "dokument_update_effective" ON public."dokument" AS PERMISSIVE FOR UPDATE TO "authenticated" USING (public.ap02_has_effective_right('dokument.verwalten')) WITH CHECK (public.ap02_has_effective_right('dokument.verwalten'));

DROP POLICY IF EXISTS "dokument_wartungsvertrag_zuordnung_read_effective" ON public."dokument_wartungsvertrag_zuordnung";
CREATE POLICY "dokument_wartungsvertrag_zuordnung_read_effective" ON public."dokument_wartungsvertrag_zuordnung" AS PERMISSIVE FOR SELECT TO "authenticated" USING (public.ap02_has_effective_right('dokument.verwalten'));

DROP POLICY IF EXISTS "dokumenttyp_write_effective" ON public."dokumenttyp";
CREATE POLICY "dokumenttyp_write_effective" ON public."dokumenttyp" AS PERMISSIVE FOR ALL TO "authenticated" USING (public.ap02_has_effective_right('dokumenttyp.verwalten')) WITH CHECK (public.ap02_has_effective_right('dokumenttyp.verwalten'));

DROP POLICY IF EXISTS "dokumenttyp_wirkung_write_admin" ON public."dokumenttyp_wirkung";
CREATE POLICY "dokumenttyp_wirkung_write_admin" ON public."dokumenttyp_wirkung" AS PERMISSIVE FOR ALL TO "authenticated" USING (public.ap02_has_effective_right('dokumentwirkung.verwalten')) WITH CHECK (public.ap02_has_effective_right('dokumentwirkung.verwalten'));

DROP POLICY IF EXISTS "hauptzaehler_messstelle_select_can_read_meters" ON public."hauptzaehler_messstelle";
CREATE POLICY "hauptzaehler_messstelle_select_can_read_meters" ON public."hauptzaehler_messstelle" AS PERMISSIVE FOR SELECT TO "authenticated" USING (public.ap02_has_effective_right('zaehler.lesen'));

DROP POLICY IF EXISTS "jahresabschluss_admin_full" ON public."jahresabschluss";
CREATE POLICY "jahresabschluss_admin_full" ON public."jahresabschluss" AS PERMISSIVE FOR ALL TO "authenticated" USING (public.ap02_has_effective_right('jahresabschluss.vorbereiten')) WITH CHECK (public.ap02_has_effective_right('jahresabschluss.vorbereiten'));

DROP POLICY IF EXISTS "jahresabschluss_korrektur_admin_full" ON public."jahresabschluss_korrektur";
CREATE POLICY "jahresabschluss_korrektur_admin_full" ON public."jahresabschluss_korrektur" AS PERMISSIVE FOR ALL TO "authenticated" USING (public.ap02_has_effective_right('jahresabschluss.korrigieren')) WITH CHECK (public.ap02_has_effective_right('jahresabschluss.korrigieren'));

DROP POLICY IF EXISTS "jahresabschluss_korrektur_position_admin_full" ON public."jahresabschluss_korrektur_position";
CREATE POLICY "jahresabschluss_korrektur_position_admin_full" ON public."jahresabschluss_korrektur_position" AS PERMISSIVE FOR ALL TO "authenticated" USING (public.ap02_has_effective_right('jahresabschluss.korrigieren')) WITH CHECK (public.ap02_has_effective_right('jahresabschluss.korrigieren'));

DROP POLICY IF EXISTS "jahresabschluss_position_write_admin" ON public."jahresabschluss_position";
CREATE POLICY "jahresabschluss_position_write_admin" ON public."jahresabschluss_position" AS PERMISSIVE FOR ALL TO "authenticated" USING (public.ap02_has_effective_right('jahresabschluss.vorbereiten')) WITH CHECK (public.ap02_has_effective_right('jahresabschluss.vorbereiten'));

DROP POLICY IF EXISTS "jahresabschluss_position_select_own_or_admin" ON public."jahresabschluss_position";
CREATE POLICY "jahresabschluss_position_select_own_or_admin" ON public."jahresabschluss_position" AS PERMISSIVE FOR SELECT TO "authenticated" USING ((public.ap02_has_effective_right('finanzen.lesen') OR (mitglied_id = current_mitglied_id())));

DROP POLICY IF EXISTS "jahresabschluss_rechnung_admin_full" ON public."jahresabschluss_rechnung";
CREATE POLICY "jahresabschluss_rechnung_admin_full" ON public."jahresabschluss_rechnung" AS PERMISSIVE FOR ALL TO "authenticated" USING (public.ap02_has_effective_right('finanzen.rechnungen')) WITH CHECK (public.ap02_has_effective_right('finanzen.rechnungen'));

DROP POLICY IF EXISTS "jahresabschluss_rechnung_zuordnung_admin_full" ON public."jahresabschluss_rechnung_zuordnung";
CREATE POLICY "jahresabschluss_rechnung_zuordnung_admin_full" ON public."jahresabschluss_rechnung_zuordnung" AS PERMISSIVE FOR ALL TO "authenticated" USING (public.ap02_has_effective_right('finanzen.rechnungen')) WITH CHECK (public.ap02_has_effective_right('finanzen.rechnungen'));

DROP POLICY IF EXISTS "jahresabschluss_verbrauch_write_admin" ON public."jahresabschluss_verbrauch";
CREATE POLICY "jahresabschluss_verbrauch_write_admin" ON public."jahresabschluss_verbrauch" AS PERMISSIVE FOR ALL TO "authenticated" USING (public.ap02_has_effective_right('jahresabschluss.vorbereiten')) WITH CHECK (public.ap02_has_effective_right('jahresabschluss.vorbereiten'));

DROP POLICY IF EXISTS "jahresabschluss_verbrauch_select_own_or_admin" ON public."jahresabschluss_verbrauch";
CREATE POLICY "jahresabschluss_verbrauch_select_own_or_admin" ON public."jahresabschluss_verbrauch" AS PERMISSIVE FOR SELECT TO "authenticated" USING ((public.ap02_has_effective_right('finanzen.lesen') OR (mitglied_id = current_mitglied_id())));

DROP POLICY IF EXISTS "jahresabschluss_vorauszahlung_admin_full" ON public."jahresabschluss_vorauszahlung";
CREATE POLICY "jahresabschluss_vorauszahlung_admin_full" ON public."jahresabschluss_vorauszahlung" AS PERMISSIVE FOR ALL TO "authenticated" USING (public.ap02_has_effective_right('finanzen.rechnungen')) WITH CHECK (public.ap02_has_effective_right('finanzen.rechnungen'));

DROP POLICY IF EXISTS "kommunikationsvorgang_effective" ON public."kommunikationsvorgang";
CREATE POLICY "kommunikationsvorgang_effective" ON public."kommunikationsvorgang" AS PERMISSIVE FOR ALL TO "authenticated" USING (public.ap02_has_effective_right('vorgang.bearbeiten')) WITH CHECK (public.ap02_has_effective_right('vorgang.bearbeiten'));

DROP POLICY IF EXISTS "kostenart_write_admin" ON public."kostenart";
CREATE POLICY "kostenart_write_admin" ON public."kostenart" AS PERMISSIVE FOR ALL TO "authenticated" USING (public.ap02_has_effective_right('finanzen.kostenarten')) WITH CHECK (public.ap02_has_effective_right('finanzen.kostenarten'));

DROP POLICY IF EXISTS "mgv_read" ON public."mitglied_gesetzlicher_vertreter";
CREATE POLICY "mgv_read" ON public."mitglied_gesetzlicher_vertreter" AS PERMISSIVE FOR SELECT TO "authenticated" USING ((public.ap02_has_effective_right('mitglied.lesen') OR (EXISTS ( SELECT 1
   FROM app_user au
  WHERE ((au.user_id = auth.uid()) AND (au.mitglied_id = ANY (ARRAY[mitglied_gesetzlicher_vertreter.minderjaehriges_mitglied_id, mitglied_gesetzlicher_vertreter.vertreter_mitglied_id])))))));

DROP POLICY IF EXISTS "mitglied_vereinsdokument_bereitstellung_effective" ON public."mitglied_vereinsdokument_bereitstellung";
CREATE POLICY "mitglied_vereinsdokument_bereitstellung_effective" ON public."mitglied_vereinsdokument_bereitstellung" AS PERMISSIVE FOR SELECT TO "authenticated" USING (public.ap02_has_effective_right('vereinsunterlagen.verwalten'));

DROP POLICY IF EXISTS "mitglied_vereinsdokument_bereitstellung_storno_effective" ON public."mitglied_vereinsdokument_bereitstellung";
CREATE POLICY "mitglied_vereinsdokument_bereitstellung_storno_effective" ON public."mitglied_vereinsdokument_bereitstellung" AS PERMISSIVE FOR UPDATE TO "authenticated" USING (((storniert_am IS NULL) AND public.ap02_has_effective_right('vereinsunterlagen.verwalten'))) WITH CHECK (public.ap02_has_effective_right('vereinsunterlagen.verwalten'));

DROP POLICY IF EXISTS "pachtverhaeltnis_contract_effective_or_own_history" ON public."pachtverhaeltnis";
CREATE POLICY "pachtverhaeltnis_contract_effective_or_own_history" ON public."pachtverhaeltnis" AS PERMISSIVE FOR SELECT TO "authenticated" USING ((public.ap02_has_effective_right('pachtvertrag.verwalten') OR can_access_own_lease_relationship(id)));

DROP POLICY IF EXISTS "pachtverhaeltnis_beteiligung_contract_effective_or_own_history" ON public."pachtverhaeltnis_beteiligung";
CREATE POLICY "pachtverhaeltnis_beteiligung_contract_effective_or_own_history" ON public."pachtverhaeltnis_beteiligung" AS PERMISSIVE FOR SELECT TO "authenticated" USING ((public.ap02_has_effective_right('pachtvertrag.verwalten') OR can_access_own_lease_relationship(pachtverhaeltnis_id)));

DROP POLICY IF EXISTS "pachtvertragsfassung_contract_effective_or_own_history" ON public."pachtvertragsfassung";
CREATE POLICY "pachtvertragsfassung_contract_effective_or_own_history" ON public."pachtvertragsfassung" AS PERMISSIVE FOR SELECT TO "authenticated" USING ((public.ap02_has_effective_right('pachtvertrag.verwalten') OR can_access_own_lease_relationship(pachtverhaeltnis_id)));

DROP POLICY IF EXISTS "parzelle_write_effective" ON public."parzelle";
CREATE POLICY "parzelle_write_effective" ON public."parzelle" AS PERMISSIVE FOR ALL TO "authenticated" USING (public.ap02_has_effective_right('parzelle.bearbeiten')) WITH CHECK (public.ap02_has_effective_right('parzelle.bearbeiten'));

DROP POLICY IF EXISTS "parzelle_select_effective_or_own_lease" ON public."parzelle";
CREATE POLICY "parzelle_select_effective_or_own_lease" ON public."parzelle" AS PERMISSIVE FOR SELECT TO "authenticated" USING ((public.ap02_has_effective_right('parzelle.lesen') OR can_access_own_lease_parcel(id)));

DROP POLICY IF EXISTS "parzellen_belegung_write_effective" ON public."parzellen_belegung";
CREATE POLICY "parzellen_belegung_write_effective" ON public."parzellen_belegung" AS PERMISSIVE FOR ALL TO "authenticated" USING (public.ap02_has_effective_right('belegung.verwalten')) WITH CHECK (public.ap02_has_effective_right('belegung.verwalten'));

DROP POLICY IF EXISTS "parzellen_belegung_select_effective_or_own_lease" ON public."parzellen_belegung";
CREATE POLICY "parzellen_belegung_select_effective_or_own_lease" ON public."parzellen_belegung" AS PERMISSIVE FOR SELECT TO "authenticated" USING ((public.ap02_has_effective_right('parzelle.lesen') OR can_access_own_lease_occupancy(id)));

DROP POLICY IF EXISTS "pdf_template_catalog_write_effective" ON public."pdf_template_catalog";
CREATE POLICY "pdf_template_catalog_write_effective" ON public."pdf_template_catalog" AS PERMISSIVE FOR ALL TO "authenticated" USING (public.ap02_has_effective_right('vorlage.verwalten')) WITH CHECK (public.ap02_has_effective_right('vorlage.verwalten'));

DROP POLICY IF EXISTS "pdf_template_catalog_read_effective" ON public."pdf_template_catalog";
CREATE POLICY "pdf_template_catalog_read_effective" ON public."pdf_template_catalog" AS PERMISSIVE FOR SELECT TO "authenticated" USING (public.ap02_has_effective_right('vorlage.verwalten'));

DROP POLICY IF EXISTS "prepared_contract_signatur_effective" ON public."prepared_contract_signatur";
CREATE POLICY "prepared_contract_signatur_effective" ON public."prepared_contract_signatur" AS PERMISSIVE FOR SELECT TO "authenticated" USING (public.ap02_has_effective_right('pachtvertrag.verwalten'));

DROP POLICY IF EXISTS "prepared_contract_signatur_snapshot_person_effective" ON public."prepared_contract_signatur_snapshot_person";
CREATE POLICY "prepared_contract_signatur_snapshot_person_effective" ON public."prepared_contract_signatur_snapshot_person" AS PERMISSIVE FOR SELECT TO "authenticated" USING (public.ap02_has_effective_right('pachtvertrag.verwalten'));

DROP POLICY IF EXISTS "prepared_contract_signatur_snapshot_quelle_effective" ON public."prepared_contract_signatur_snapshot_quelle";
CREATE POLICY "prepared_contract_signatur_snapshot_quelle_effective" ON public."prepared_contract_signatur_snapshot_quelle" AS PERMISSIVE FOR SELECT TO "authenticated" USING (public.ap02_has_effective_right('pachtvertrag.verwalten'));

DROP POLICY IF EXISTS "saison_effective_configuration" ON public."saison";
CREATE POLICY "saison_effective_configuration" ON public."saison" AS PERMISSIVE FOR ALL TO "authenticated" USING (public.ap02_has_effective_right('saison.verwalten')) WITH CHECK (public.ap02_has_effective_right('saison.verwalten'));

DROP POLICY IF EXISTS "serienbrief_vorlage_effective" ON public."serienbrief_vorlage";
CREATE POLICY "serienbrief_vorlage_effective" ON public."serienbrief_vorlage" AS PERMISSIVE FOR ALL TO "authenticated" USING (public.ap02_has_effective_right('vorlage.verwalten')) WITH CHECK (public.ap02_has_effective_right('vorlage.verwalten'));

DROP POLICY IF EXISTS "serienbrief_vorlage_exportfeld_effective" ON public."serienbrief_vorlage_exportfeld";
CREATE POLICY "serienbrief_vorlage_exportfeld_effective" ON public."serienbrief_vorlage_exportfeld" AS PERMISSIVE FOR ALL TO "authenticated" USING (public.ap02_has_effective_right('vorlage.verwalten')) WITH CHECK (public.ap02_has_effective_right('vorlage.verwalten'));

DROP POLICY IF EXISTS "serienbrief_vorlage_freifeld_effective" ON public."serienbrief_vorlage_freifeld";
CREATE POLICY "serienbrief_vorlage_freifeld_effective" ON public."serienbrief_vorlage_freifeld" AS PERMISSIVE FOR ALL TO "authenticated" USING (public.ap02_has_effective_right('vorlage.verwalten')) WITH CHECK (public.ap02_has_effective_right('vorlage.verwalten'));

DROP POLICY IF EXISTS "termin_manage_insert" ON public."termin";
CREATE POLICY "termin_manage_insert" ON public."termin" AS PERMISSIVE FOR INSERT TO "authenticated" WITH CHECK (public.ap02_has_effective_right('termin.verwalten'));

DROP POLICY IF EXISTS "termin_manage_select" ON public."termin";
CREATE POLICY "termin_manage_select" ON public."termin" AS PERMISSIVE FOR SELECT TO "authenticated" USING (public.ap02_has_effective_right('termin.verwalten'));

DROP POLICY IF EXISTS "termin_manage_update" ON public."termin";
CREATE POLICY "termin_manage_update" ON public."termin" AS PERMISSIVE FOR UPDATE TO "authenticated" USING (public.ap02_has_effective_right('termin.verwalten')) WITH CHECK (public.ap02_has_effective_right('termin.verwalten'));

DROP POLICY IF EXISTS "umlageart_write_admin" ON public."umlageart";
CREATE POLICY "umlageart_write_admin" ON public."umlageart" AS PERMISSIVE FOR ALL TO "authenticated" USING (public.ap02_has_effective_right('finanzen.kostenarten')) WITH CHECK (public.ap02_has_effective_right('finanzen.kostenarten'));

DROP POLICY IF EXISTS "vereinsdokument_fassung_effective" ON public."vereinsdokument_fassung";
CREATE POLICY "vereinsdokument_fassung_effective" ON public."vereinsdokument_fassung" AS PERMISSIVE FOR SELECT TO "authenticated" USING (public.ap02_has_effective_right('vereinsunterlagen.verwalten'));

DROP POLICY IF EXISTS "vereinsdokument_typ_effective" ON public."vereinsdokument_typ";
CREATE POLICY "vereinsdokument_typ_effective" ON public."vereinsdokument_typ" AS PERMISSIVE FOR ALL TO "authenticated" USING (public.ap02_has_effective_right('vereinsunterlagen.verwalten')) WITH CHECK (public.ap02_has_effective_right('vereinsunterlagen.verwalten'));

DROP POLICY IF EXISTS "vereinsfunktion_effective" ON public."vereinsfunktion";
CREATE POLICY "vereinsfunktion_effective" ON public."vereinsfunktion" AS PERMISSIVE FOR ALL TO "authenticated" USING (public.ap02_has_effective_right('vereinsfunktion.verwalten')) WITH CHECK (public.ap02_has_effective_right('vereinsfunktion.verwalten'));

DROP POLICY IF EXISTS "vereinsfunktion_besetzung_effective" ON public."vereinsfunktion_besetzung";
CREATE POLICY "vereinsfunktion_besetzung_effective" ON public."vereinsfunktion_besetzung" AS PERMISSIVE FOR ALL TO "authenticated" USING (public.ap02_has_effective_right('vereinsfunktion.verwalten')) WITH CHECK (public.ap02_has_effective_right('vereinsfunktion.verwalten'));

DROP POLICY IF EXISTS "vereinsfunktion_vertretung_effective" ON public."vereinsfunktion_vertretung";
CREATE POLICY "vereinsfunktion_vertretung_effective" ON public."vereinsfunktion_vertretung" AS PERMISSIVE FOR ALL TO "authenticated" USING (public.ap02_has_effective_right('vereinsfunktion.verwalten')) WITH CHECK (public.ap02_has_effective_right('vereinsfunktion.verwalten'));

DROP POLICY IF EXISTS "vereinsfunktion_verwendung_effective" ON public."vereinsfunktion_verwendung";
CREATE POLICY "vereinsfunktion_verwendung_effective" ON public."vereinsfunktion_verwendung" AS PERMISSIVE FOR ALL TO "authenticated" USING (public.ap02_has_effective_right('vereinsfunktion.verwalten')) WITH CHECK (public.ap02_has_effective_right('vereinsfunktion.verwalten'));

DROP POLICY IF EXISTS "wartungsvertraege_write_effective" ON public."wartungsvertraege";
CREATE POLICY "wartungsvertraege_write_effective" ON public."wartungsvertraege" AS PERMISSIVE FOR ALL TO "authenticated" USING (public.ap02_has_effective_right('mitgliedschaft.verwalten')) WITH CHECK (public.ap02_has_effective_right('mitgliedschaft.verwalten'));

DROP POLICY IF EXISTS "wartungsvertraege_read_effective_or_own" ON public."wartungsvertraege";
CREATE POLICY "wartungsvertraege_read_effective_or_own" ON public."wartungsvertraege" AS PERMISSIVE FOR SELECT TO "authenticated" USING ((public.ap02_has_effective_right('mitgliedschaft.verwalten') OR (EXISTS ( SELECT 1
   FROM wartungsvertrag_zuordnungen z
  WHERE ((z.wartungsvertrag_id = wartungsvertraege.id) AND (z.hauptmitglied_id = current_mitglied_id()))))));

DROP POLICY IF EXISTS "wartungsvertrag_dokument_workflow_manage_effective" ON public."wartungsvertrag_dokument_workflow";
CREATE POLICY "wartungsvertrag_dokument_workflow_manage_effective" ON public."wartungsvertrag_dokument_workflow" AS PERMISSIVE FOR ALL TO "authenticated" USING (public.ap02_has_effective_right('mitgliedschaft.verwalten')) WITH CHECK (public.ap02_has_effective_right('mitgliedschaft.verwalten'));

DROP POLICY IF EXISTS "wartungsvertrag_grundlegende_aenderung_manage_effective" ON public."wartungsvertrag_grundlegende_aenderung";
CREATE POLICY "wartungsvertrag_grundlegende_aenderung_manage_effective" ON public."wartungsvertrag_grundlegende_aenderung" AS PERMISSIVE FOR SELECT TO "authenticated" USING (public.ap02_has_effective_right('mitgliedschaft.verwalten'));

DROP POLICY IF EXISTS "wartungsvertrag_zuordnungen_insert_effective" ON public."wartungsvertrag_zuordnungen";
CREATE POLICY "wartungsvertrag_zuordnungen_insert_effective" ON public."wartungsvertrag_zuordnungen" AS PERMISSIVE FOR INSERT TO "authenticated" WITH CHECK (public.ap02_has_effective_right('mitgliedschaft.verwalten'));

DROP POLICY IF EXISTS "wartungsvertrag_zuordnungen_select_own_or_manage_effective" ON public."wartungsvertrag_zuordnungen";
CREATE POLICY "wartungsvertrag_zuordnungen_select_own_or_manage_effective" ON public."wartungsvertrag_zuordnungen" AS PERMISSIVE FOR SELECT TO "authenticated" USING ((public.ap02_has_effective_right('mitgliedschaft.verwalten') OR (hauptmitglied_id = current_mitglied_id())));

DROP POLICY IF EXISTS "wartungsvertrag_zuordnungen_update_effective" ON public."wartungsvertrag_zuordnungen";
CREATE POLICY "wartungsvertrag_zuordnungen_update_effective" ON public."wartungsvertrag_zuordnungen" AS PERMISSIVE FOR UPDATE TO "authenticated" USING (public.ap02_has_effective_right('mitgliedschaft.verwalten')) WITH CHECK (public.ap02_has_effective_right('mitgliedschaft.verwalten'));

DROP POLICY IF EXISTS "zaehler_write_effective_meter_permission" ON public."zaehler";
CREATE POLICY "zaehler_write_effective_meter_permission" ON public."zaehler" AS PERMISSIVE FOR ALL TO "authenticated" USING (public.ap02_has_effective_right('zaehlerwechsel.verwalten')) WITH CHECK (public.ap02_has_effective_right('zaehlerwechsel.verwalten'));

DROP POLICY IF EXISTS "zaehler_select_effective_permission" ON public."zaehler";
CREATE POLICY "zaehler_select_effective_permission" ON public."zaehler" AS PERMISSIVE FOR SELECT TO "authenticated" USING ((public.ap02_has_effective_right('zaehler.lesen') OR (EXISTS ( SELECT 1
   FROM parzellen_belegung pb
  WHERE ((pb.parzelle_id = zaehler.parzelle_id) AND (pb.mitglied_id = current_mitglied_id()))))));

DROP POLICY IF EXISTS "zaehler_ablesung_insert_effective_meter_permission" ON public."zaehler_ablesung";
CREATE POLICY "zaehler_ablesung_insert_effective_meter_permission" ON public."zaehler_ablesung" AS PERMISSIVE FOR INSERT TO "authenticated" WITH CHECK (public.ap02_has_effective_right('ablesung.erfassen'));

DROP POLICY IF EXISTS "zaehler_ablesung_select_effective_permission" ON public."zaehler_ablesung";
CREATE POLICY "zaehler_ablesung_select_effective_permission" ON public."zaehler_ablesung" AS PERMISSIVE FOR SELECT TO "authenticated" USING ((public.ap02_has_effective_right('zaehler.lesen') OR can_access_own_meter_reading(id)));

DROP POLICY IF EXISTS "zaehlerwechsel_antrag_select_own_or_manager" ON public."zaehlerwechsel_antrag";
CREATE POLICY "zaehlerwechsel_antrag_select_own_or_manager" ON public."zaehlerwechsel_antrag" AS PERMISSIVE FOR SELECT TO "authenticated" USING (((antragsteller_mitglied_id = current_mitglied_id()) OR public.ap02_has_effective_right('zaehlerwechsel.verwalten')));


-- Lesezugriff ist von der Bearbeitung eines Kommunikationsvorgangs getrennt.
DROP POLICY IF EXISTS "ap02_kommunikation_lesen" ON public."kommunikationsvorgang";
CREATE POLICY "ap02_kommunikation_lesen" ON public."kommunikationsvorgang" FOR SELECT TO "authenticated"
 USING (public.ap02_has_effective_right('vorgang.lesen'));

-- Ein Recht zum VORBEREITEN darf keinen Jahresabschluss finalisieren.
CREATE OR REPLACE FUNCTION public.ap02_guard_jahresabschluss_status()
RETURNS trigger LANGUAGE plpgsql SECURITY INVOKER SET search_path TO pg_catalog, public AS $guard$
BEGIN
  IF TG_OP='UPDATE' THEN
    IF TG_TABLE_NAME='jahresabschluss' AND NEW.status IS DISTINCT FROM OLD.status
       AND NEW.status='abgeschlossen'
       AND NOT public.ap02_has_effective_right('jahresabschluss.finalisieren') THEN
      RAISE EXCEPTION 'Keine Berechtigung fuer finalen Jahresabschluss' USING ERRCODE='42501';
    END IF;
    IF TG_TABLE_NAME='jahresabschluss_korrektur' AND NEW IS DISTINCT FROM OLD
       AND NOT public.ap02_has_effective_right('jahresabschluss.korrigieren') THEN
      RAISE EXCEPTION 'Korrekturabschluss derzeit gesperrt' USING ERRCODE='42501';
    END IF;
  END IF;
  RETURN NEW;
END $guard$;
REVOKE ALL ON FUNCTION public.ap02_guard_jahresabschluss_status() FROM PUBLIC, anon, authenticated;
DROP TRIGGER IF EXISTS ap02_jahresabschluss_status_guard ON public.jahresabschluss;
CREATE TRIGGER ap02_jahresabschluss_status_guard BEFORE UPDATE ON public.jahresabschluss
FOR EACH ROW EXECUTE FUNCTION public.ap02_guard_jahresabschluss_status();
DROP TRIGGER IF EXISTS ap02_jahresabschluss_korrektur_guard ON public.jahresabschluss_korrektur;
CREATE TRIGGER ap02_jahresabschluss_korrektur_guard BEFORE UPDATE ON public.jahresabschluss_korrektur
FOR EACH ROW EXECUTE FUNCTION public.ap02_guard_jahresabschluss_status();

DO $post$ BEGIN
 IF EXISTS(SELECT 1 FROM pg_policies WHERE schemaname='public' AND
 (coalesce(qual,'')||coalesce(with_check,'')) ~* 'has_effective_permission[(]|is_admin_or_vorstand[(]|is_admin[(]') THEN
 RAISE EXCEPTION 'Alte RLS-Berechtigungspruefung verblieben'; END IF;
 IF EXISTS (SELECT 1 FROM public.fachrecht WHERE code IN ('jahresabschluss.finalisieren','jahresabschluss.korrigieren') AND aktiv) THEN
 RAISE EXCEPTION 'Gesperrte Finanz-Fachrechte unerwartet aktiviert'; END IF;
END $post$;
;
