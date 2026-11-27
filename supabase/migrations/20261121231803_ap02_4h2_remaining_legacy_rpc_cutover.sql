-- AP-02.4H2: 39 Bestandsfunktionen auf benannte Fachrechte 2.0.
-- Nur DemoDB. Alle Objekt-, Vertrags-, Signatur-, Eigen- und Prozessscopes bleiben erhalten.
-- Keine alten individuellen Gewaehrungen/Entzuege werden uebernommen.
DO $h2$
DECLARE rec record; rule record; body text; previous text; newer text; hit boolean; changed int:=0;
BEGIN
  IF (SELECT count(*) FROM public.fachrecht)<>55
  OR to_regprocedure('public.ap02_has_effective_right(text)') IS NULL
  OR ((EXISTS (SELECT 1 FROM auth.users) OR
      EXISTS (SELECT 1 FROM public.app_user) OR
      EXISTS (SELECT 1 FROM public.mitglied) OR
      EXISTS (SELECT 1 FROM public.parzelle) OR
      EXISTS (SELECT 1 FROM public.pachtverhaeltnis) OR
      EXISTS (SELECT 1 FROM public.dokument) OR
      EXISTS (SELECT 1 FROM public.kommunikationsvorgang) OR
      EXISTS (SELECT 1 FROM public.zaehler) OR
      EXISTS (SELECT 1 FROM public.arbeitsstunde) OR
      EXISTS (SELECT 1 FROM public.wartungsvertraege) OR
      EXISTS (SELECT 1 FROM public.vereinsfunktion) OR
      EXISTS (SELECT 1 FROM public.vereinsfunktion_besetzung) OR
      EXISTS (SELECT 1 FROM public.vereinsfunktion_verwendung) OR
      EXISTS (SELECT 1 FROM public.vereinsfunktion_vertretung)) AND NOT EXISTS (SELECT 1 FROM public.protected_admin_identity)) OR
    EXISTS (
      SELECT 1 FROM public.protected_admin_identity p
      WHERE NOT EXISTS (SELECT 1 FROM public.app_user_zusatzrolle z
        WHERE z.user_id=p.user_id AND z.zusatzrolle_code='admin')
    ) OR
    EXISTS (
    SELECT a.user_id FROM public.app_user a WHERE a.is_protected = true
    EXCEPT
    SELECT p.user_id FROM public.protected_admin_identity p
  )
  OR EXISTS (
    SELECT p.user_id FROM public.protected_admin_identity p
    EXCEPT
    SELECT a.user_id FROM public.app_user a WHERE a.is_protected = true
  ) THEN
    RAISE EXCEPTION 'Fachrechte-Kern nicht im erwarteten Zustand';
  END IF;
  CREATE TEMP TABLE ap02_h2_map (funktion text, alt text, fachrecht text,
    PRIMARY KEY(funktion,alt)) ON COMMIT DROP;
  INSERT INTO ap02_h2_map VALUES
('ap012_can_read_fachnummer_reservierung','4','vereinsdaten.alle_lesen'),
('beende_pacht_nachsorge','524288','pachtvertrag.verwalten'),
('beende_pachtverhaeltnis','524288','pachtvertrag.verwalten'),
('bereite_signierte_parzellen_vertragsaenderung_vor','524288','pachtvertrag.verwalten'),
('can_access_live_internal_data','4','vereinsdaten.alle_lesen'),
('create_mitglied_vereinsdokument_bereitstellungen_papier','268435456','vereinsunterlagen.verwalten'),
('create_vereinsdokument_fassung','268435456','vereinsunterlagen.verwalten'),
('dokumentiere_prepared_contract_papieruebergabe','524288','vertrag.vorbereiten'),
('enforce_own_open_arbeitsstunde_update','256','arbeitsstunden.freigeben'),
('ensure_wartungsvertrag_dokument_workflow','4','mitgliedschaft.verwalten'),
('ensure_wartungsvertrag_prepared_contract','4','mitgliedschaft.verwalten'),
('erfasse_historische_pachtberichtigung','524288','historische_vertraege.erfassen'),
('erstelle_pacht_nachsorge','524288','pachtvertrag.verwalten'),
('execute_hauptzaehler_meter_change','64','zaehlerwechsel.verwalten'),
('execute_meter_change','64','zaehlerwechsel.verwalten'),
('fn_berechne_pflichtstunden_status_authorized','131072','arbeitsstunden.lesen'),
('fn_berechne_pflichtstunden_status_authorized','256','arbeitsstunden.bearbeiten'),
('fn_pflichtstunden_sichtbare_mitglieder','131072','arbeitsstunden.lesen'),
('fn_pflichtstunden_sichtbare_mitglieder','256','arbeitsstunden.bearbeiten'),
('get_auslaufende_wartungsvertrag_zuordnungen','4','mitgliedschaft.verwalten'),
('get_mitglied_vereinsdokument_bereitstellung_historie','268435456','vereinsunterlagen.verwalten'),
('get_mitglied_vereinsdokument_status','268435456','vereinsunterlagen.verwalten'),
('get_wartungsvertrag_folgeentscheidung_detail','4','mitgliedschaft.verwalten'),
('ordne_nachsorge_protokoll_zu','524288','pachtvertrag.verwalten'),
('prepare_pachtvertrag_ersatz','524288','pachtvertrag.verwalten'),
('prepare_pachtvertrag_ersatz_vor_phase9','524288','pachtvertrag.verwalten'),
('rpc_hauptzaehler_jahresverbrauch_segmente','32','zaehler.lesen'),
('rpc_jahresverbrauch_haupt_und_unterzaehler','32','zaehler.lesen'),
('rpc_jahresverbrauch_haupt_und_unterzaehler_g6_basis','32','zaehler.lesen'),
('rpc_jahresverbrauch_haupt_unter_differenz','32','zaehler.lesen'),
('speichere_prepared_contract_signatur','524288','vertrag.vorbereiten'),
('start_prepared_contract_signature','524288','vertrag.vorbereiten'),
('start_prepared_contract_signature','4','mitgliedschaft.verwalten'),
('start_wartungsvertrag_grundlegende_aenderung','4','mitgliedschaft.verwalten'),
('trg_arbeitseinsatz_anmeldung_validate','1048576','arbeitseinsatz.verwalten'),
('wende_signierte_parzellen_vertragsaenderung_an','524288','pachtvertrag.verwalten'),
('ensure_prepared_contract_signatur_position','role:is_admin_or_vorstand','vertrag.vorbereiten'),
('mark_meter_defective','role:is_productive_admin_or_vorstand','zaehlerwechsel.verwalten'),
('mark_meter_defective','role:is_admin_or_vorstand','zaehlerwechsel.verwalten'),
('prepared_contract_signaturen_vollstaendig','role:is_admin_or_vorstand','vertrag.vorbereiten'),
('prepared_contract_snapshot_unterzeichner','role:is_admin_or_vorstand','vertrag.vorbereiten'),
('register_historisches_dokument','role:is_admin_or_vorstand','historische_vertraege.erfassen'),
('speichere_serienbrief_vorlage','role:is_admin_or_vorstand','vorlage.verwalten');
  IF EXISTS(SELECT 1 FROM ap02_h2_map mp LEFT JOIN public.fachrecht fr
        ON fr.code=mp.fachrecht AND fr.aktiv WHERE fr.code IS NULL) THEN
    RAISE EXCEPTION 'Ungültige Fachrechtzuordnung';
  END IF;
  FOR rec IN SELECT DISTINCT funktion FROM ap02_h2_map ORDER BY funktion LOOP
    IF (SELECT count(*) FROM pg_proc pr JOIN pg_namespace ns ON ns.oid=pr.pronamespace
       WHERE ns.nspname='public' AND pr.proname=rec.funktion AND pr.prokind='f')<>1 THEN
       RAISE EXCEPTION 'Erwartete Funktion fehlt oder ist ueberladen: %',rec.funktion;
    END IF;
    SELECT pg_get_functiondef(pr.oid) INTO body
    FROM pg_proc pr JOIN pg_namespace ns ON ns.oid=pr.pronamespace
    WHERE ns.nspname='public' AND pr.proname=rec.funktion AND pr.prokind='f';
    FOR rule IN SELECT * FROM ap02_h2_map WHERE funktion=rec.funktion ORDER BY alt LOOP
      newer:=format('public.ap02_has_effective_right(%L)',rule.fachrecht);
      IF left(rule.alt,5)='role:' THEN
        previous:=format('public.%s()',substr(rule.alt,6));
        hit:=position(previous IN body)>0;
        body:=replace(body,previous,newer);
      ELSE
        previous:=format('public.has_effective_permission(%s)',rule.alt);
        hit:=position(previous IN body)>0;
        body:=replace(body,previous,newer);
        previous:=format('public.has_effective_permission(%s::bigint)',rule.alt);
        hit:=hit OR position(previous IN body)>0;
        body:=replace(body,previous,newer);
      END IF;
      IF NOT hit THEN RAISE EXCEPTION 'Pruefung fehlt bei %: %',rec.funktion,rule.alt; END IF;
    END LOOP;
    IF body~* 'public[.]has_effective_permission[(]|public[.]is_admin_or_vorstand[(]|public[.]is_productive_admin_or_vorstand[(]' THEN
      RAISE EXCEPTION 'Unerwartete Altpruefung in %',rec.funktion;
    END IF;
    EXECUTE body;
    changed:=changed+1;
  END LOOP;
  IF changed<>39 THEN RAISE EXCEPTION 'Erwartet 39 Funktionen, erhalten %',changed; END IF;
END $h2$;
;
