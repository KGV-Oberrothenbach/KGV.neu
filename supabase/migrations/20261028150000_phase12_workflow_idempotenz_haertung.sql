-- Phase 12: retry/parallel calls converge on the persisted workflow state.
-- Source only; this migration is deliberately not executed by this change.

create or replace function public.start_prepared_contract_signature(p_kommunikationsvorgang_id bigint,p_expected_revision integer,p_assigned_to uuid)
returns public.kommunikationsvorgang language plpgsql security definer set search_path=public,pg_temp as $$
declare v public.kommunikationsvorgang%rowtype; r record; p_id bigint; v_stichtag date := (now() at time zone 'Europe/Berlin')::date;
begin
 if auth.uid() is null or auth.uid() is distinct from p_assigned_to or not public.is_admin_or_vorstand() then raise exception 'Keine Berechtigung zum Signaturstart' using errcode='42501'; end if;
 select * into v from public.kommunikationsvorgang where id=p_kommunikationsvorgang_id for update;
 if not found or v.vorgangstyp<>'vertrag_signatur' or v.revision<>p_expected_revision or v.zugewiesen_an is distinct from p_assigned_to then raise exception 'Der Vertragsvorgang kann nicht zur Signatur gestartet werden' using errcode='40001'; end if;
 -- Lost-response retry: reuse revision, number, positions and frozen signatories.
 if v.status in ('zur_signatur','letzte_pruefung','erledigt') and v.signatur_gestartet_am is not null and v.signatur_revision=p_expected_revision
    and exists(select 1 from public.prepared_contract_signatur_snapshot_person p where p.kommunikationsvorgang_id=v.id and p.signatur_revision=p_expected_revision)
    and exists(select 1 from public.prepared_contract_signatur p where p.kommunikationsvorgang_id=v.id and p.signatur_revision=p_expected_revision) then return v; end if;
 if v.status<>'vorbereitet' or v.signatur_gestartet_am is not null or v.signatur_revision is not null then raise exception 'Der Vertragsvorgang kann nicht zur Signatur gestartet werden' using errcode='40001'; end if;
 if exists(select 1 from public.prepared_contract_signatur_snapshot_person p where p.kommunikationsvorgang_id=v.id and p.signatur_revision=v.revision) then raise exception 'Inkonsistenter Signatur-Snapshot vor dem Signaturstart' using errcode='23514'; end if;
 for r in select * from public.resolve_vertragsunterzeichner(v_stichtag) loop
   insert into public.prepared_contract_signatur_snapshot_person(kommunikationsvorgang_id,signatur_revision,mitglied_id,unterzeichner_name_snapshot)
   values(v.id,v.revision,r.mitglied_id,r.unterzeichner_name)
   on conflict(kommunikationsvorgang_id,signatur_revision,mitglied_id) do update set unterzeichner_name_snapshot=excluded.unterzeichner_name_snapshot returning id into p_id;
   insert into public.prepared_contract_signatur_snapshot_quelle(snapshot_person_id,berechtigungsart,vereinsfunktion_id,vereinsfunktion_bezeichnung_snapshot,vereinsfunktion_besetzung_id,besetzung_gueltig_ab_snapshot,besetzung_gueltig_bis_snapshot,vertretung_id,vertretung_gueltig_ab_snapshot,vertretung_gueltig_bis_snapshot,vertretene_vereinsfunktion_id,vertretene_funktion_bezeichnung_snapshot,vertretene_besetzung_id,vertretene_besetzung_gueltig_ab_snapshot,vertretene_besetzung_gueltig_bis_snapshot)
   values(p_id,r.berechtigungsart,r.vereinsfunktion_id,r.vereinsfunktion_bezeichnung,r.vereinsfunktion_besetzung_id,r.besetzung_gueltig_ab,r.besetzung_gueltig_bis,r.vertretung_id,r.vertretung_gueltig_ab,r.vertretung_gueltig_bis,r.vertretene_vereinsfunktion_id,r.vertretene_funktion_bezeichnung,r.vertretene_besetzung_id,r.vertretene_besetzung_gueltig_ab,r.vertretene_besetzung_gueltig_bis) on conflict do nothing;
 end loop;
 if (select count(*) from public.prepared_contract_signatur_snapshot_person p where p.kommunikationsvorgang_id=v.id and p.signatur_revision=v.revision)<2 then raise exception 'Für zwei Vereinsunterschriften sind mindestens zwei unterschiedliche natürliche Personen erforderlich' using errcode='23514'; end if;
 update public.kommunikationsvorgang set status='zur_signatur',signatur_gestartet_am=now(),signatur_revision=revision where id=v.id returning * into v;
 return v;
end; $$;
create or replace function public.speichere_prepared_contract_signatur(
 p_kommunikationsvorgang_id bigint,p_signatur_revision integer,p_signatur_schluessel text,p_mitglied_id bigint,p_unterzeichner_name_snapshot text,p_vereinsfunktion_besetzung_id bigint,p_vertretung_id bigint,p_vertretene_besetzung_id bigint,p_storage_path text,p_signatur_sha256 text,p_unterzeichnet_am timestamptz default now()
) returns public.prepared_contract_signatur language plpgsql security definer set search_path=public,pg_temp as $$
declare v_position public.prepared_contract_signatur%rowtype; v_source record; v_name text; v_other bigint;
begin
 if not public.is_admin_or_vorstand() or auth.uid() is null then raise exception 'Nur angemeldete administrative Vertragsbearbeitung darf Signaturen speichern.' using errcode='42501'; end if;
 if p_storage_path !~ ('^'||p_kommunikationsvorgang_id::text||'/'||p_signatur_revision::text||'/[^/]+/[0-9a-fA-F-]{36}[.]png$') or p_signatur_sha256 !~ '^[0-9a-f]{64}$' then raise exception 'Storage-Pfad oder Signatur-Hash ist ungültig.' using errcode='23514'; end if;
 if not exists(select 1 from storage.objects where bucket_id='prepared-contract-signatures' and name=p_storage_path) then raise exception 'Das referenzierte Signaturbild liegt nicht im privaten Storage.' using errcode='23514'; end if;
 if p_signatur_schluessel in ('vorstand_1','vorstand_2') then perform 1 from public.prepared_contract_signatur where kommunikationsvorgang_id=p_kommunikationsvorgang_id and signatur_revision=p_signatur_revision and signatur_schluessel in ('vorstand_1','vorstand_2') order by signatur_schluessel for update; end if;
 select * into v_position from public.prepared_contract_signatur where kommunikationsvorgang_id=p_kommunikationsvorgang_id and signatur_revision=p_signatur_revision and signatur_schluessel=p_signatur_schluessel for update;
 if not found then raise exception 'Die Signaturposition wurde nicht vorbereitet.' using errcode='P0002'; end if;
 -- Exact image/person/source retries return the binding row; differing input never overwrites it.
 if v_position.status='unterschrieben' then
   if v_position.mitglied_id is not distinct from p_mitglied_id and v_position.storage_path=p_storage_path and v_position.signatur_sha256=p_signatur_sha256
      and (v_position.unterzeichner_typ<>'vereinsunterzeichner' or (v_position.vereinsfunktion_besetzung_id is not distinct from p_vereinsfunktion_besetzung_id and v_position.vertretung_id is not distinct from p_vertretung_id and v_position.vertretene_besetzung_id is not distinct from p_vertretene_besetzung_id)) then return v_position; end if;
   raise exception 'Die Signaturposition wurde bereits abweichend unterschrieben.' using errcode='55000';
 elsif v_position.status<>'offen' then raise exception 'Die Signaturposition ist nicht unterschreibbar.' using errcode='55000'; end if;
 if v_position.unterzeichner_typ='vereinsunterzeichner' then
   select q.*,p.mitglied_id,p.unterzeichner_name_snapshot into v_source from public.prepared_contract_signatur_snapshot_person p join public.prepared_contract_signatur_snapshot_quelle q on q.snapshot_person_id=p.id
    where p.kommunikationsvorgang_id=p_kommunikationsvorgang_id and p.signatur_revision=p_signatur_revision and p.mitglied_id=p_mitglied_id and q.vereinsfunktion_besetzung_id is not distinct from p_vereinsfunktion_besetzung_id and q.vertretung_id is not distinct from p_vertretung_id and q.vertretene_besetzung_id is not distinct from p_vertretene_besetzung_id;
   if not found then raise exception 'Die Person oder Berechtigungsquelle gehört nicht zum eingefrorenen Signatur-Snapshot.' using errcode='42501'; end if;
   if p_signatur_schluessel in ('vorstand_1','vorstand_2') then select mitglied_id into v_other from public.prepared_contract_signatur where kommunikationsvorgang_id=p_kommunikationsvorgang_id and signatur_revision=p_signatur_revision and signatur_schluessel in ('vorstand_1','vorstand_2') and signatur_schluessel<>p_signatur_schluessel and status='unterschrieben'; if v_other=p_mitglied_id then raise exception 'Vorstand 1 und Vorstand 2 müssen unterschiedliche natürliche Personen sein.' using errcode='23514'; end if; end if;
   update public.prepared_contract_signatur set status='unterschrieben',mitglied_id=v_source.mitglied_id,unterzeichner_name_snapshot=v_source.unterzeichner_name_snapshot,vereinsfunktion_id=v_source.vereinsfunktion_id,vereinsfunktion_bezeichnung_snapshot=v_source.vereinsfunktion_bezeichnung_snapshot,vereinsfunktion_besetzung_id=v_source.vereinsfunktion_besetzung_id,vertretung_id=v_source.vertretung_id,vertretene_vereinsfunktion_id=v_source.vertretene_vereinsfunktion_id,vertretene_funktion_bezeichnung_snapshot=v_source.vertretene_funktion_bezeichnung_snapshot,vertretene_besetzung_id=v_source.vertretene_besetzung_id,storage_bucket='prepared-contract-signatures',storage_path=p_storage_path,signatur_sha256=p_signatur_sha256,unterzeichnet_am=p_unterzeichnet_am,unterzeichnet_von_auth_user_id=auth.uid() where id=v_position.id returning * into v_position;
 else
   select nullif(btrim(concat_ws(' ',vorname,name)),'') into v_name from public.mitglied where id=p_mitglied_id;
   v_name:=coalesce(v_name,nullif(btrim(p_unterzeichner_name_snapshot),'')); if v_name is null then raise exception 'Für die private Unterschrift fehlt ein nachvollziehbarer Personen-Snapshot.' using errcode='23514'; end if;
   update public.prepared_contract_signatur set status='unterschrieben',mitglied_id=p_mitglied_id,unterzeichner_name_snapshot=v_name,storage_bucket='prepared-contract-signatures',storage_path=p_storage_path,signatur_sha256=p_signatur_sha256,unterzeichnet_am=p_unterzeichnet_am,unterzeichnet_von_auth_user_id=auth.uid() where id=v_position.id returning * into v_position;
 end if;
 return v_position;
end; $$;
create or replace function public.entscheide_mitgliedsaufnahme(p_bearbeitung_id bigint,p_ergebnis text,p_weg text,p_sitzungsdatum date default null,p_begruendung text default null,p_aufnahmedatum date default null)
returns void language plpgsql security definer set search_path=public,pg_temp as $$
declare b public.mitgliedsaufnahme_vorstandsbearbeitung%rowtype; v public.kommunikationsvorgang%rowtype; m public.mitglied%rowtype; caller uuid:=auth.uid(); cnt integer; yes_cnt integer; v_expected text; v_reason text:=nullif(btrim(p_begruendung),'');
begin
 if caller is null or p_ergebnis not in ('annehmen','ablehnen') or p_weg not in ('einstimmig','vorstandssitzung') or (p_ergebnis='annehmen' and p_aufnahmedatum is null) then raise exception 'Ungültige Entscheidung oder fehlendes Aufnahmedatum' using errcode='23514'; end if;
 if p_weg='vorstandssitzung' and p_sitzungsdatum is null then raise exception 'Sitzungsdatum erforderlich' using errcode='23514'; end if;
 if p_ergebnis='ablehnen' and v_reason is null then raise exception 'Entscheidungsbegründung erforderlich' using errcode='23514'; end if;
 if not exists(select 1 from public.mitgliedsaufnahme_berechtigungen('mitgliedsaufnahme_entscheidung',current_date) x join public.app_user au on au.mitglied_id=x.mitglied_id where au.user_id=caller) then raise exception 'Keine aktuelle Entscheidungsberechtigung' using errcode='42501'; end if;
 select * into b from public.mitgliedsaufnahme_vorstandsbearbeitung where id=p_bearbeitung_id for update; if not found then raise exception 'Bearbeitung ist nicht offen' using errcode='40001'; end if;
 select * into v from public.kommunikationsvorgang where id=b.kommunikationsvorgang_id for update; if not found then raise exception 'Kommunikationsvorgang fehlt' using errcode='P0002'; end if;
 select * into m from public.mitglied where id=v.mitglied_id for update;
 v_expected:=case when p_ergebnis='annehmen' then 'angenommen' else 'abgelehnt' end;
 -- The decision, member update and confirmation shell commit atomically.
 if b.entscheidung<>'offen' then
   if b.entscheidung=v_expected and b.entscheidungsweg is not distinct from p_weg and b.sitzungsdatum is not distinct from p_sitzungsdatum
      and b.entscheidungsbegruendung is not distinct from (case when p_ergebnis='ablehnen' then v_reason else null end) and v.status='erledigt'
      and ((p_ergebnis='annehmen' and m.aufnahme_status='aufgenommen' and m.aktiv is true and m.mitglied_seit=p_aufnahmedatum
            and exists(select 1 from public.kommunikationsvorgang d where d.vorgangstyp='dokument_erzeugung' and d.dokumenttyp='aufnahmebestaetigung' and (d.entwurf_daten->>'mitgliedsantrag_kommunikationsvorgang_id')::bigint=v.id))
           or (p_ergebnis='ablehnen' and m.aufnahme_status='abgelehnt' and m.aktiv is false and m.mitglied_seit is null)) then return; end if;
   raise exception 'Bearbeitung wurde bereits abweichend entschieden' using errcode='40001';
 end if;
 if v.status<>'letzte_pruefung' then raise exception 'Kommunikationsvorgang ist nicht abschließbar' using errcode='40001'; end if;
 select count(*),count(*) filter(where s.stimme='ja') into cnt,yes_cnt from public.mitgliedsaufnahme_abstimmung_snapshot_person sp left join lateral(select stimme from public.mitgliedsaufnahme_abstimmung_stimme where snapshot_person_id=sp.id order by version desc limit 1) s on true where sp.vorstandsbearbeitung_id=b.id;
 if p_weg='einstimmig' and (cnt=0 or yes_cnt<>cnt) then raise exception 'Einstimmiger Weg verlangt vollständige Ja-Abstimmung' using errcode='23514'; end if;
 if p_ergebnis='annehmen' then
   perform set_config('kgv.mitgliedsaufnahme_abschluss','1',true);
   update public.mitglied set aufnahme_status='aufgenommen',aktiv=true,mitglied_seit=p_aufnahmedatum where id=v.mitglied_id;
   perform public.complete_prepared_mitgliedsantrag_aus_entscheidung(v.id,v.revision,caller,p_aufnahmedatum);
 else
   update public.mitglied set aufnahme_status='abgelehnt',aktiv=false,mitglied_seit=null where id=v.mitglied_id;
   update public.kommunikationsvorgang set status='erledigt',bearbeitet_von=caller where id=v.id;
 end if;
 update public.mitgliedsaufnahme_vorstandsbearbeitung set entscheidung=v_expected,entscheidungsweg=p_weg,sitzungsdatum=p_sitzungsdatum,entscheidungsbegruendung=case when p_ergebnis='ablehnen' then v_reason else null end,entschieden_am=now(),entschieden_von_auth_user_id=caller,updated_at=now() where id=b.id;
 if p_ergebnis='annehmen' then perform public.ensure_aufnahmebestaetigung_dokumentvorgang(b.id); end if;
end; $$;
create or replace function public.prepare_pachtvertrag_ersatz(p_vorgaenger_fassung_id bigint,p_assigned_to uuid)
returns table(kommunikationsvorgang_id bigint) language plpgsql security definer set search_path=public,pg_temp as $$
declare v_id bigint; v_assigned_to uuid;
begin
 if auth.uid() is null or p_assigned_to is distinct from auth.uid() then raise exception 'Der Ersatzvorgang muss dem angemeldeten Benutzer zugewiesen werden' using errcode='42501'; end if;
 if not exists(select 1 from public.app_user au where au.user_id=auth.uid() and au.role in ('admin','vorstand')) then raise exception 'Nur Admin oder Vorstand dürfen einen Ersatzvertrag vorbereiten' using errcode='42501'; end if;
 -- Locking the real predecessor serializes the no-active-successor check.
 perform 1 from public.pachtvertragsfassung where id=p_vorgaenger_fassung_id for update;
 if not found then raise exception 'Unbekannte Pachtvertragsfassung: %',p_vorgaenger_fassung_id using errcode='P0002'; end if;
 select id,zugewiesen_an into v_id,v_assigned_to from public.kommunikationsvorgang where ersetzt_pachtvertragsfassung_id=p_vorgaenger_fassung_id and status not in ('erledigt','abgebrochen') for update;
 if found then
   if v_assigned_to is distinct from p_assigned_to then raise exception 'Der aktive Ersatzvorgang ist einem anderen Benutzer zugewiesen' using errcode='40001'; end if;
   return query select v_id; return;
 end if;
 return query select * from public.prepare_pachtvertrag_ersatz_vor_phase9(p_vorgaenger_fassung_id,p_assigned_to);
end; $$;
revoke all on function public.prepare_pachtvertrag_ersatz_vor_phase9(bigint,uuid) from public,anon,authenticated,service_role;
grant execute on function public.prepare_pachtvertrag_ersatz_vor_phase9(bigint,uuid) to service_role;
-- Preserve established snapshot creation behind a retry-aware gate.
alter function public.prepare_contract_dispatch(bigint,bigint) rename to prepare_contract_dispatch_vor_phase12;
revoke all on function public.prepare_contract_dispatch_vor_phase12(bigint,bigint) from public,anon,authenticated;
grant execute on function public.prepare_contract_dispatch_vor_phase12(bigint,bigint) to service_role;
create function public.prepare_contract_dispatch(p_kommunikationsvorgang_id bigint,p_erneuter_versand_von_id bigint default null)
returns bigint language plpgsql security definer set search_path=public,pg_temp as $$
declare v public.kommunikationsvorgang%rowtype; v_id bigint;
begin
 if not public.kann_vertragsversand_ausfuehren() then raise exception 'Keine Berechtigung für Vertragsversand' using errcode='42501'; end if;
 select * into v from public.kommunikationsvorgang where id=p_kommunikationsvorgang_id for update;
 if not found then raise exception 'Unbekannter Vertragsvorgang' using errcode='P0002'; end if;
 if p_erneuter_versand_von_id is null then
   select z.id into v_id from public.versandinstanz z where z.kommunikationsvorgang_id=v.id and z.revision=coalesce(v.signatur_revision,v.revision) and z.erneuter_versand_von_id is null order by z.id limit 1 for update;
 else
   select z.id into v_id from public.versandinstanz z where z.kommunikationsvorgang_id=v.id and z.revision=coalesce(v.signatur_revision,v.revision) and z.erneuter_versand_von_id=p_erneuter_versand_von_id and z.status<>'versendet' order by z.id desc limit 1 for update;
 end if;
 if found then return v_id; end if;
 return public.prepare_contract_dispatch_vor_phase12(p_kommunikationsvorgang_id,p_erneuter_versand_von_id);
end; $$;
create or replace function public.claim_contract_dispatch(p_versandinstanz_id bigint)
returns table(id bigint,betreff text,mailtext text) language plpgsql security definer set search_path=public,pg_temp as $$
begin
 if not public.kann_vertragsversand_ausfuehren() then raise exception 'Keine Berechtigung für Vertragsversand' using errcode='42501'; end if;
 update public.versandinstanz set status='wird_versendet',beansprucht_am=now(),beansprucht_von=auth.uid(),letzter_fehler=null
 where versandinstanz.id=p_versandinstanz_id
   and (status in ('bereit','fehler','teilweise_fehlerhaft') or (status='wird_versendet' and beansprucht_am<now()-interval '15 minutes'))
   and exists(select 1 from public.versandinstanz_empfaenger_snapshot e where e.versandinstanz_id=p_versandinstanz_id and e.status in ('bereit','fehler'))
 returning versandinstanz.id,betreff_snapshot,text_snapshot into id,betreff,mailtext;
 if not found then raise exception 'Versandinstanz ist nicht beanspruchbar' using errcode='40001'; end if;
 return next;
end; $$;
revoke all on function public.start_prepared_contract_signature(bigint,integer,uuid),public.speichere_prepared_contract_signatur(bigint,integer,text,bigint,text,bigint,bigint,bigint,text,text,timestamptz),public.entscheide_mitgliedsaufnahme(bigint,text,text,date,text,date),public.prepare_pachtvertrag_ersatz(bigint,uuid),public.prepare_contract_dispatch(bigint,bigint),public.claim_contract_dispatch(bigint) from public,anon;
grant execute on function public.start_prepared_contract_signature(bigint,integer,uuid),public.speichere_prepared_contract_signatur(bigint,integer,text,bigint,text,bigint,bigint,bigint,text,text,timestamptz),public.entscheide_mitgliedsaufnahme(bigint,text,text,date,text,date),public.prepare_pachtvertrag_ersatz(bigint,uuid),public.prepare_contract_dispatch(bigint,bigint),public.claim_contract_dispatch(bigint) to authenticated,service_role;
comment on function public.prepare_contract_dispatch(bigint,bigint) is 'Returns the stable initial dispatch, reuses an unfinished explicit redispatch, and creates a new redispatch only after the previous one was sent.';
