-- P11 AP-02A / P11-R01, P11-R02: the role is only a permission template.
-- This migration intentionally replaces the central contract and the meter policies;
-- data scope and photo rules remain AP-02B/AP-06 work.

create or replace function public.permission_role_base(p_role text)
returns bigint language sql immutable set search_path to 'public', 'pg_temp' as $$
  select case p_role
    when 'admin' then 4278181879::bigint
    when 'vorstand' then 1920458231::bigint
    else 10::bigint
  end;
$$;

-- P11-R01: actual replacement-contract creation shares the same direct
-- caller binding as its wrapper and authorizes it with CanCreateMitglied.
create or replace function public.prepare_pachtvertrag_ersatz_vor_phase9(p_vorgaenger_fassung_id bigint, p_assigned_to uuid)
returns table(kommunikationsvorgang_id bigint)
language plpgsql security definer set search_path=public,pg_temp as $$
declare v_fassung public.pachtvertragsfassung%rowtype; v_quelle public.kommunikationsvorgang%rowtype; v_parzelle_id bigint; v_hauptmitglied_id bigint; v_entwurf jsonb; v_id bigint;
begin
 if auth.uid() is null or p_assigned_to is distinct from auth.uid() then raise exception 'Der Ersatzvorgang muss dem angemeldeten Benutzer zugewiesen werden' using errcode='42501'; end if;
 if not public.has_effective_permission(524288) then raise exception 'CanCreateMitglied required' using errcode='42501'; end if;
 select f.* into v_fassung from public.pachtvertragsfassung f where f.id=p_vorgaenger_fassung_id for update;
 if not found then raise exception 'Unbekannte Pachtvertragsfassung: %',p_vorgaenger_fassung_id using errcode='P0002'; end if;
 if v_fassung.nicht_mehr_massgeblich_am is not null or exists(select 1 from public.pachtvertragsfassung n where n.vorgaenger_fassung_id=v_fassung.id) then raise exception 'Nur die aktuell maßgebliche Vertragsfassung kann ersetzt werden' using errcode='23514'; end if;
 select pv.parzelle_id,min(pb.mitglied_id) into v_parzelle_id,v_hauptmitglied_id from public.pachtverhaeltnis pv join public.pachtverhaeltnis_beteiligung pb on pb.pachtverhaeltnis_id=pv.id and pb.rolle='hauptpaechter' and pb.gueltig_bis is null where pv.id=v_fassung.pachtverhaeltnis_id and pv.beendet_am is null group by pv.parzelle_id;
 if v_parzelle_id is null or v_hauptmitglied_id is null then raise exception 'Für die Vertragsfassung besteht kein aktives Pachtverhältnis' using errcode='23514'; end if;
 if public.is_demo_or_reviewer() and (not public.is_demo_mitglied_id(v_hauptmitglied_id) or not public.is_demo_parzelle_id(v_parzelle_id)) then raise exception 'Ersatzvertrag liegt außerhalb des zulässigen Demo-Scopes' using errcode='42501'; end if;
 select kv.* into v_quelle from public.dokument d join public.kommunikationsvorgang kv on kv.id=d.source_kommunikationsvorgang_id where d.id=v_fassung.dokument_id and kv.dokumenttyp='pachtvertrag' and kv.status='erledigt';
 if not found then raise exception 'Die maßgebliche Vertragsfassung besitzt keinen abgeschlossenen Prepared-Ursprung' using errcode='23514'; end if;
 v_entwurf:=jsonb_set(v_quelle.entwurf_daten,'{draft,effectiveDate}',to_jsonb(current_date::text),true);
 insert into public.kommunikationsvorgang(vorgangstyp,dokumenttyp,status,mitglied_id,parzelle_id,entwurf_daten,revision,zugewiesen_an,erstellt_von,bearbeitet_von,ersetzt_pachtvertragsfassung_id) values ('vertrag_signatur','pachtvertrag','vorbereitet',v_hauptmitglied_id,v_parzelle_id,v_entwurf,1,p_assigned_to,auth.uid(),auth.uid(),v_fassung.id) returning id into v_id;
 return query select v_id;
end;
$$;

-- P11-R01: preparing a replacement lease is a contract-administration action.
-- Keep the direct caller/assignment binding; only replace the role template.
create or replace function public.prepare_pachtvertrag_ersatz(p_vorgaenger_fassung_id bigint, p_assigned_to uuid)
returns table(kommunikationsvorgang_id bigint)
language plpgsql security definer set search_path = public, pg_temp as $$
declare v_id bigint; v_assigned_to uuid;
begin
 if auth.uid() is null or p_assigned_to is distinct from auth.uid() then raise exception 'Der Ersatzvorgang muss dem angemeldeten Benutzer zugewiesen werden' using errcode='42501'; end if;
 if not public.has_effective_permission(524288) then raise exception 'CanCreateMitglied required' using errcode='42501'; end if;
 perform 1 from public.pachtvertragsfassung where id=p_vorgaenger_fassung_id for update;
 if not found then raise exception 'Unbekannte Pachtvertragsfassung: %',p_vorgaenger_fassung_id using errcode='P0002'; end if;
 select id,zugewiesen_an into v_id,v_assigned_to from public.kommunikationsvorgang where ersetzt_pachtvertragsfassung_id=p_vorgaenger_fassung_id and status not in ('erledigt','abgebrochen') for update;
 if found then
   if v_assigned_to is distinct from p_assigned_to then raise exception 'Der aktive Ersatzvorgang ist einem anderen Benutzer zugewiesen' using errcode='40001'; end if;
   return query select v_id; return;
 end if;
 return query select * from public.prepare_pachtvertrag_ersatz_vor_phase9(p_vorgaenger_fassung_id,p_assigned_to);
end;
$$;

-- P11-R01: Discarding a prepared membership or lease contract changes the
-- contract workflow and archives its generated documents.  Both narrowly
-- scoped capabilities are required; neither service_role nor a role name is
-- a business authorization.
create or replace function public.discard_prepared_contract(p_kommunikationsvorgang_id bigint, p_expected_revision integer, p_discarded_by uuid)
returns table(id bigint, status text, final_dokument_id bigint)
language plpgsql security definer set search_path = public, pg_temp as $$
declare v_vorgang public.kommunikationsvorgang%rowtype;
begin
  if auth.role() is distinct from 'service_role' then raise exception 'Das Verwerfen ist nur serverseitig zulässig' using errcode='42501'; end if;
  if not exists (
    select 1 from public.app_user au where au.user_id=p_discarded_by
      and (((public.permission_role_base(au.role)|coalesce(au.permission_grants,0)) & ~coalesce(au.permission_revocations,0) & 524304)=524304)
  ) then raise exception 'Keine Berechtigung zum Verwerfen' using errcode='42501'; end if;
  select * into v_vorgang from public.kommunikationsvorgang kv where kv.id=p_kommunikationsvorgang_id for update;
  if not found then raise exception 'Unbekannter Kommunikationsvorgang' using errcode='P0002'; end if;
  if v_vorgang.vorgangstyp<>'vertrag_signatur' or v_vorgang.dokumenttyp not in ('mitgliedsantrag','pachtvertrag') then raise exception 'Der Vorgang ist kein Vertragsvorgang' using errcode='23514'; end if;
  if exists(select 1 from public.app_user au where au.user_id=p_discarded_by and au.is_demo_account) and not public.is_demo_mitglied_id(v_vorgang.mitglied_id) then raise exception 'Vertragsvorgang liegt außerhalb des zulässigen Demo-Scopes' using errcode='42501'; end if;
  if v_vorgang.status='verworfen' then return query select v_vorgang.id,v_vorgang.status,v_vorgang.final_dokument_id; return; end if;
  if v_vorgang.revision<>p_expected_revision or v_vorgang.zugewiesen_an is distinct from p_discarded_by then raise exception 'Der Vorgang wurde zwischenzeitlich geändert oder ist nicht zugewiesen' using errcode='40001'; end if;
  if v_vorgang.status not in ('vorbereitet','zur_signatur','letzte_pruefung') then raise exception 'Abgeschlossene Vertragsvorgänge können nicht verworfen werden' using errcode='23514'; end if;
  update public.dokument set archiviert_at=now(),archiviert_by=p_discarded_by,archiviert_begruendung='Verworfener Vertragsvorgang' where source_kommunikationsvorgang_id=v_vorgang.id and archiviert_at is null;
  update public.kommunikationsvorgang kv set status='verworfen',bearbeitet_von=p_discarded_by where kv.id=v_vorgang.id and kv.revision=p_expected_revision and kv.status in ('vorbereitet','zur_signatur','letzte_pruefung') and kv.zugewiesen_an=p_discarded_by;
  if not found then raise exception 'Der Vertragsvorgang wurde zwischenzeitlich geändert' using errcode='40001'; end if;
  return query select v_vorgang.id,'verworfen'::text,v_vorgang.final_dokument_id;
end;
$$;

-- P11-R01: This RPC is technically restricted to service_role, but its
-- business authorization belongs to the assigned actor passed by the trusted
-- server.  Evaluate that actor's effective CanCreateMitglied permission;
-- neither a role name nor service_role itself grants the workflow right.
create or replace function public.complete_prepared_mitgliedsantrag(p_kommunikationsvorgang_id bigint, p_expected_revision integer, p_created_by uuid)
returns table(vertreter_mitglied_id bigint, bereits_abgeschlossen boolean)
language plpgsql security definer set search_path = public, pg_temp as $$
declare v public.kommunikationsvorgang%rowtype; d public.dokument%rowtype; rep jsonb; representative_id bigint; current_relation record; start_date date; member_row public.mitglied%rowtype;
begin
  if auth.role() is distinct from 'service_role' then raise exception 'Der Mitgliedsantragsabschluss ist nur serverseitig zulässig' using errcode='42501'; end if;
  if not exists (
    select 1
    from public.app_user au
    where au.user_id = p_created_by
      and (((public.permission_role_base(au.role) | coalesce(au.permission_grants, 0))
            & ~coalesce(au.permission_revocations, 0) & 524288) = 524288)
  ) then raise exception 'Keine Berechtigung' using errcode='42501'; end if;
  select * into v from public.kommunikationsvorgang where id=p_kommunikationsvorgang_id for update;
  if not found or v.vorgangstyp <> 'vertrag_signatur' or v.dokumenttyp <> 'mitgliedsantrag' then raise exception 'Der Vorgang ist kein Mitgliedsantrag' using errcode='23514'; end if;
  if exists(select 1 from public.app_user au where au.user_id=p_created_by and au.is_demo_account)
     and not public.is_demo_mitglied_id(v.mitglied_id) then raise exception 'Mitgliedsantrag liegt außerhalb des zulässigen Demo-Scopes' using errcode='42501'; end if;
  if v.status='erledigt' then
    select vertreter_mitglied_id into representative_id from public.mitglied_gesetzlicher_vertreter where minderjaehriges_mitglied_id=v.mitglied_id and gueltig_bis is null order by id desc limit 1;
    return query select representative_id, true; return;
  end if;
  if v.status <> 'letzte_pruefung' or v.revision <> p_expected_revision or v.signatur_revision <> p_expected_revision or v.zugewiesen_an is distinct from p_created_by or v.final_dokument_id is null then raise exception 'Der Mitgliedsantrag ist nicht abschließbar' using errcode='40001'; end if;
  select * into d from public.dokument where id=v.final_dokument_id and archiviert_at is null for key share;
  if not found or d.source_kommunikationsvorgang_id is distinct from v.id or d.source_revision is distinct from v.revision then raise exception 'Finaldokument passt nicht zum Vorgang' using errcode='23514'; end if;
  select * into member_row from public.mitglied where id=v.mitglied_id for key share;
  if not found then raise exception 'Mitglied fehlt' using errcode='P0002'; end if;
  rep := v.entwurf_daten->'representative'; start_date := (v.entwurf_daten->>'startDate')::date;
  if rep is not null and jsonb_typeof(rep)='object' then
    if rep->>'mode'='existing' then
      representative_id := nullif(rep->>'memberId','')::bigint;
      if representative_id is null or representative_id=v.mitglied_id or not exists(select 1 from public.mitglied where id=representative_id) then raise exception 'Ungültiger gesetzlicher Vertreter' using errcode='23514'; end if;
    elsif rep->>'mode'='manual' then
      if coalesce(nullif(trim(rep->>'vorname'),''),'')='' or coalesce(nullif(trim(rep->>'nachname'),''),'')='' then raise exception 'Vertreterdaten sind unvollständig' using errcode='23514'; end if;
      insert into public.mitglied(hauptmitglied_id,vorname,name,adresse,plz,ort,mitglied_seit,whatsapp_einwilligung,aktiv,mitglied_ende)
      values (coalesce(member_row.hauptmitglied_id, member_row.id), trim(rep->>'vorname'), trim(rep->>'nachname'), case when coalesce((rep->>'adresseAbweichend')::boolean,false) then trim(rep->>'adresse') else member_row.adresse end, case when coalesce((rep->>'adresseAbweichend')::boolean,false) then trim(rep->>'plz') else member_row.plz end, case when coalesce((rep->>'adresseAbweichend')::boolean,false) then trim(rep->>'ort') else member_row.ort end, start_date,false,true,null) returning id into representative_id;
    else raise exception 'Vertretermodus fehlt' using errcode='23514'; end if;
    select * into current_relation from public.mitglied_gesetzlicher_vertreter where minderjaehriges_mitglied_id=v.mitglied_id and gueltig_ab<=start_date and (gueltig_bis is null or gueltig_bis>=start_date) order by (gueltig_bis is null) desc, gueltig_ab desc, id desc limit 1 for update;
    if found and current_relation.vertreter_mitglied_id=representative_id then
      update public.mitglied_gesetzlicher_vertreter set gueltig_ab=start_date,bemerkung='Automatisch aus Mitgliedsantrag übernommen.',updated_at=now() where id=current_relation.id;
    else
      if found then update public.mitglied_gesetzlicher_vertreter set gueltig_bis=start_date-1,updated_at=now() where id=current_relation.id; end if;
      insert into public.mitglied_gesetzlicher_vertreter(minderjaehriges_mitglied_id,vertreter_mitglied_id,gueltig_ab,gueltig_bis,bemerkung,created_at,updated_at) values(v.mitglied_id,representative_id,start_date,null,'Automatisch aus Mitgliedsantrag übernommen.',now(),now());
    end if;
  end if;
  update public.kommunikationsvorgang set status='erledigt',bearbeitet_von=p_created_by where id=v.id and status='letzte_pruefung' and revision=p_expected_revision and zugewiesen_an=p_created_by;
  if not found then raise exception 'Mitgliedsantrag wurde zwischenzeitlich geändert' using errcode='40001'; end if;
  return query select representative_id,false;
end; $$;

revoke all on function public.complete_prepared_mitgliedsantrag(bigint,integer,uuid) from public,anon,authenticated;
grant execute on function public.complete_prepared_mitgliedsantrag(bigint,integer,uuid) to service_role;

-- P11-R01: Lease-contract completion is a membership/lease-administration
-- workflow.  service_role is merely the trusted technical caller; the
-- assigned p_created_by must hold CanCreateMitglied (524288).
create or replace function public.complete_prepared_pachtvertrag_vor_b3(p_kommunikationsvorgang_id bigint, p_expected_revision integer, p_created_by uuid)
returns table(pachtverhaeltnis_id bigint, pachtvertragsfassung_id bigint, bereits_abgeschlossen boolean)
language plpgsql security definer set search_path = public, pg_temp as $$
declare
  v_vorgang public.kommunikationsvorgang%rowtype; v_final_dokument public.dokument%rowtype;
  v_aktive_verhaeltnisse integer; v_pachtverhaeltnis_id bigint; v_vorgaenger_id bigint;
  v_letzte_fassungen integer; v_start_text text; v_effective_date_text text; v_pachtbeginn date;
  v_vertragsdatum date; v_wirksam_ab date; v_include_mitpaechter boolean; v_mitpaechter_id bigint;
  v_aktive_mitpaechter integer; v_aktiver_mitpaechter_id bigint; v_fassung_id bigint;
begin
  if auth.role() is distinct from 'service_role' then raise exception 'Der Pachtvertragsabschluss ist nur serverseitig zulässig' using errcode = '42501'; end if;
  if p_kommunikationsvorgang_id is null or p_expected_revision is null or p_created_by is null then raise exception 'Kommunikationsvorgang, Revision und auslösender Benutzer sind erforderlich' using errcode = '22023'; end if;
  if not exists (
    select 1 from public.app_user au join public.mitglied m on m.id=au.mitglied_id and m.auth_user_id=au.user_id
    where au.user_id=p_created_by and au.mitglied_id is not null and au.mitglied_id>0
      and (((public.permission_role_base(au.role) | coalesce(au.permission_grants,0)) & ~coalesce(au.permission_revocations,0) & 524288)=524288)
  ) then raise exception 'Der auslösende Benutzer besitzt nicht das erforderliche Recht CanCreateMitglied' using errcode = '42501'; end if;
  select * into v_vorgang from public.kommunikationsvorgang where id=p_kommunikationsvorgang_id for update;
  if not found then raise exception 'Unbekannter Kommunikationsvorgang: %',p_kommunikationsvorgang_id using errcode='P0002'; end if;
  if v_vorgang.vorgangstyp<>'vertrag_signatur' or v_vorgang.dokumenttyp<>'pachtvertrag' then raise exception 'Der Vorgang ist kein vorbereiteter Pachtvertragsvorgang' using errcode='23514'; end if;
  if v_vorgang.revision<>p_expected_revision then raise exception 'Der Vertragsvorgang wurde zwischenzeitlich geändert' using errcode='40001'; end if;
  if v_vorgang.zugewiesen_an is distinct from p_created_by then raise exception 'Der Vertragsvorgang ist nicht dem auslösenden Benutzer zugewiesen' using errcode='42501'; end if;
  if exists(select 1 from public.app_user au where au.user_id=p_created_by and au.is_demo_account) and not public.is_demo_mitglied_id(v_vorgang.mitglied_id) then raise exception 'Pachtvertragsvorgang liegt außerhalb des zulässigen Demo-Scopes' using errcode='42501'; end if;
  if v_vorgang.final_dokument_id is null then raise exception 'Der Pachtvertragsvorgang enthält kein Finaldokument' using errcode='23514'; end if;
  select * into v_final_dokument from public.dokument where id=v_vorgang.final_dokument_id and archiviert_at is null for key share;
  if not found then raise exception 'Das Finaldokument existiert nicht oder ist archiviert' using errcode='23514'; end if;
  if v_final_dokument.source_kommunikationsvorgang_id is distinct from v_vorgang.id or v_final_dokument.source_revision is distinct from v_vorgang.revision then raise exception 'Das Finaldokument gehört nicht zur eingefrorenen Revision dieses Kommunikationsvorgangs' using errcode='23514'; end if;
  if v_vorgang.status='erledigt' then
    select f.pachtverhaeltnis_id,f.id into v_pachtverhaeltnis_id,v_fassung_id from public.pachtvertragsfassung f where f.dokument_id=v_vorgang.final_dokument_id;
    if found then return query select v_pachtverhaeltnis_id,v_fassung_id,true; end if;
    raise exception 'Der Vorgang ist bereits erledigt, besitzt aber keine registrierte Vertragsfassung' using errcode='23514';
  end if;
  if v_vorgang.status<>'letzte_pruefung' or v_vorgang.mitglied_id is null or v_vorgang.parzelle_id is null then raise exception 'Der Pachtvertragsvorgang ist nicht vollständig zur letzten Prüfung bereit' using errcode='23514'; end if;
  if v_vorgang.signatur_gestartet_am is null or v_vorgang.signatur_revision is null or v_vorgang.signatur_revision<>v_vorgang.revision or v_vorgang.signatur_revision<>p_expected_revision then raise exception 'Der Pachtvertragsvorgang besitzt keinen gültigen eingefrorenen Signaturstand' using errcode='23514'; end if;
  v_start_text:=v_vorgang.entwurf_daten#>>'{draft,startDate}';
  if v_start_text is null or v_start_text !~ '^\d{4}-\d{2}-\d{2}$' then raise exception 'Im vorbereiteten Pachtvertrag fehlt ein gültiger Pachtbeginn' using errcode='23514'; end if;
  v_pachtbeginn:=v_start_text::date; v_vertragsdatum:=(v_final_dokument.created_at at time zone 'Europe/Berlin')::date;
  v_include_mitpaechter:=coalesce((v_vorgang.entwurf_daten#>>'{draft,includeSecondaryMember}')::boolean,false);
  if v_include_mitpaechter then
    if coalesce(v_vorgang.entwurf_daten#>>'{draft,secondaryMemberId}','') !~ '^\d+$' then raise exception 'Der vorbereitete Erstvertrag enthält keine konkrete Mitpächter-ID' using errcode='23514'; end if;
    v_mitpaechter_id:=(v_vorgang.entwurf_daten#>>'{draft,secondaryMemberId}')::bigint;
  end if;
  perform pg_advisory_xact_lock(hashtextextended('pachtverhaeltnis:parzelle:'||v_vorgang.parzelle_id::text,0));
  select count(*),min(id) into v_aktive_verhaeltnisse,v_pachtverhaeltnis_id from public.pachtverhaeltnis where parzelle_id=v_vorgang.parzelle_id and beendet_am is null;
  if v_aktive_verhaeltnisse>1 then raise exception 'Inkonsistenter Bestand: mehrere aktive Pachtverhältnisse für die Parzelle' using errcode='23514'; end if;
  if v_aktive_verhaeltnisse=0 then
    select r.pachtverhaeltnis_id,r.pachtvertragsfassung_id into v_pachtverhaeltnis_id,v_fassung_id from public.register_pachtverhaeltnis_erstvertrag(v_vorgang.parzelle_id,v_vorgang.mitglied_id,v_pachtbeginn,v_vorgang.final_dokument_id,v_vertragsdatum,v_pachtbeginn,p_created_by,v_mitpaechter_id) r;
  else
    select count(*),min(mitglied_id) into v_aktive_mitpaechter,v_aktiver_mitpaechter_id from public.pachtverhaeltnis_beteiligung where pachtverhaeltnis_id=v_pachtverhaeltnis_id and rolle='mitpaechter' and gueltig_bis is null;
    if v_include_mitpaechter and (v_aktive_mitpaechter<>1 or v_aktiver_mitpaechter_id<>v_mitpaechter_id) then raise exception 'Beteiligungsänderungen bei einer Neufassung sind noch nicht unterstützt' using errcode='23514'; end if;
    if not v_include_mitpaechter and v_aktive_mitpaechter<>0 then raise exception 'Das Entfernen eines Mitpächters bei einer Neufassung ist noch nicht unterstützt' using errcode='23514'; end if;
    v_effective_date_text:=v_vorgang.entwurf_daten#>>'{draft,effectiveDate}';
    if v_effective_date_text is null or v_effective_date_text !~ '^\d{4}-\d{2}-\d{2}$' then raise exception 'Im vorbereiteten Pachtvertrag fehlt das Wirksamkeitsdatum der Neufassung' using errcode='23514'; end if;
    v_wirksam_ab:=v_effective_date_text::date;
    select count(*),min(f.id) into v_letzte_fassungen,v_vorgaenger_id from public.pachtvertragsfassung f where f.pachtverhaeltnis_id=v_pachtverhaeltnis_id and not exists(select 1 from public.pachtvertragsfassung n where n.vorgaenger_fassung_id=f.id);
    if v_letzte_fassungen=0 then raise exception 'Für das aktive Pachtverhältnis existiert keine aktuelle Vertragsfassung' using errcode='23514'; end if;
    if v_letzte_fassungen<>1 then raise exception 'Für das aktive Pachtverhältnis ist keine eindeutige aktuelle Vertragsfassung bestimmbar' using errcode='23514'; end if;
    select r.pachtvertragsfassung_id into v_fassung_id from public.register_pachtvertrags_neufassung(v_pachtverhaeltnis_id,v_vorgaenger_id,v_vorgang.final_dokument_id,v_vertragsdatum,v_wirksam_ab,p_created_by) r;
  end if;
  update public.kommunikationsvorgang set status='erledigt',bearbeitet_von=p_created_by where id=v_vorgang.id and status='letzte_pruefung' and revision=p_expected_revision and signatur_revision=p_expected_revision and final_dokument_id=v_vorgang.final_dokument_id and zugewiesen_an=p_created_by;
  if not found then raise exception 'Der Vertragsvorgang konnte nicht abgeschlossen werden' using errcode='40001'; end if;
  return query select v_pachtverhaeltnis_id,v_fassung_id,false;
end;
$$;

-- These two service-only helpers are mandatory internal dependencies of the
-- completion RPC.  Preserve their current full definitions and replace only
-- their duplicate actor-role guard with the same effective-right expression.
do $$
declare v_definition text; v_old text := 'and au.role in (''admin'', ''vorstand'')';
begin
  foreach v_definition in array array[
    'public.register_pachtverhaeltnis_erstvertrag(bigint,bigint,date,bigint,date,date,uuid,bigint)'::regprocedure::text,
    'public.register_pachtvertrags_neufassung(bigint,bigint,bigint,date,date,uuid)'::regprocedure::text
  ] loop
    select pg_get_functiondef(v_definition::regprocedure) into v_definition;
    if position(v_old in v_definition)=0 then raise exception 'Erwartete direkte Rollenprüfung in % nicht gefunden',v_definition::regprocedure; end if;
    v_definition:=replace(v_definition,v_old,'and (((public.permission_role_base(au.role) | coalesce(au.permission_grants, 0)) & ~coalesce(au.permission_revocations, 0) & 524288) = 524288)');
    execute v_definition;
  end loop;
end;
$$;

create or replace function public.known_permission_mask()
returns bigint language sql immutable set search_path to 'public', 'pg_temp' as $$
  select 4294967295::bigint;
$$;

create or replace function public.current_effective_permissions()
returns bigint language sql stable security definer set search_path to 'public', 'pg_temp' as $$
  select coalesce((
    select (public.permission_role_base(au.role) | coalesce(au.permission_grants, 0))
      & ~coalesce(au.permission_revocations, 0)
    from public.app_user au where au.user_id = auth.uid() limit 1
  ), 0::bigint);
$$;

create or replace function public.has_effective_permission(p_permission bigint)
returns boolean language sql stable security definer set search_path to 'public', 'pg_temp' as $$
  select coalesce(p_permission > 0
    and (p_permission & ~public.known_permission_mask()) = 0
    and (public.current_effective_permissions() & p_permission) = p_permission, false);
$$;

create or replace function public.has_any_effective_permission(p_permissions bigint)
returns boolean language sql stable security definer set search_path to 'public', 'pg_temp' as $$
  select coalesce(p_permissions > 0
    and (p_permissions & ~public.known_permission_mask()) = 0
    and (public.current_effective_permissions() & p_permissions) <> 0, false);
$$;

-- P11-R02: protected accounts retain only the management capability, not a role.
create or replace function public.update_app_user_access(p_user_id uuid, p_role text, p_permission_grants bigint default 0, p_permission_revocations bigint default 0)
returns void language plpgsql security definer set search_path = public, pg_temp as $$
declare
  v_target public.app_user%rowtype;
  v_known_mask bigint := public.known_permission_mask();
  v_requested_grants bigint := coalesce(p_permission_grants, 0);
  v_requested_revocations bigint := coalesce(p_permission_revocations, 0);
  v_grants bigint;
  v_revocations bigint;
  v_effective bigint;
begin
  if not public.has_effective_permission(512) then raise exception 'CanManageRoles required' using errcode = '42501'; end if;
  select * into v_target from public.app_user where user_id = p_user_id for update;
  if not found then raise exception 'Target app_user not found' using errcode = 'P0002'; end if;
  if p_role not in ('admin', 'vorstand', 'user') then raise exception 'Invalid role' using errcode = '22023'; end if;
  if v_requested_grants < 0 or v_requested_revocations < 0 then raise exception 'Invalid permission mask' using errcode = '22023'; end if;
  v_grants := (coalesce(v_target.permission_grants, 0) & ~v_known_mask) | (v_requested_grants & v_known_mask);
  v_revocations := (coalesce(v_target.permission_revocations, 0) & ~v_known_mask) | (v_requested_revocations & v_known_mask);
  v_effective := (public.permission_role_base(p_role) | (v_grants & v_known_mask)) & ~(v_revocations & v_known_mask);
  if v_target.is_protected and (v_effective & 512) <> 512 then
    raise exception 'Protected account must retain CanManageRoles' using errcode = '42501';
  end if;
  update public.app_user set role = p_role, permission_grants = v_grants, permission_revocations = v_revocations, updated_at = now() where user_id = p_user_id;
end;
$$;

-- P11-T01: direct table/RPC access cannot bypass an individual revocation.
drop policy if exists zaehler_ablesung_admin_full on public.zaehler_ablesung;
drop policy if exists zaehler_ablesung_select_assigned_or_admin on public.zaehler_ablesung;
create policy zaehler_ablesung_select_effective_permission on public.zaehler_ablesung for select to authenticated
  using (public.has_effective_permission(32) or exists (
    select 1 from public.zaehler z join public.parzellen_belegung pb on pb.parzelle_id = z.parzelle_id
    where z.id = zaehler_ablesung.zaehler_id and pb.mitglied_id = public.current_mitglied_id()));
create policy zaehler_ablesung_insert_effective_meter_permission on public.zaehler_ablesung for insert to authenticated
  with check (public.has_effective_permission(32));
create policy zaehler_ablesung_update_effective_meter_permission on public.zaehler_ablesung for update to authenticated
  using (public.has_any_effective_permission(64 | 128)) with check (public.has_any_effective_permission(64 | 128));

drop policy if exists zaehler_admin_full on public.zaehler;
drop policy if exists zaehler_select_assigned_or_admin on public.zaehler;
create policy zaehler_select_effective_permission on public.zaehler for select to authenticated
  using (public.has_effective_permission(32) or exists (
    select 1 from public.parzellen_belegung pb where pb.parzelle_id = zaehler.parzelle_id and pb.mitglied_id = public.current_mitglied_id()));
create policy zaehler_write_effective_meter_permission on public.zaehler for all to authenticated
  using (public.has_effective_permission(64)) with check (public.has_effective_permission(64));

revoke all on function public.has_any_effective_permission(bigint) from public;
grant execute on function public.has_any_effective_permission(bigint) to authenticated;
grant execute on function public.has_any_effective_permission(bigint) to service_role;
comment on function public.current_effective_permissions() is 'P11 AP-02A: (role base | individual grants) & ~individual revocations.';

-- Policy mapping (P11-R01). Demo predicates stay alongside the effective right;
-- service_role remains a technical execution role and is not a Fachrecht.
drop policy if exists app_export_definition_select_admin_vorstand on public.app_export_definition;
drop policy if exists app_export_filter_definition_select_admin_vorstand on public.app_export_filter_definition;
drop policy if exists app_export_column_definition_select_admin_vorstand on public.app_export_column_definition;
create policy app_export_definition_select_effective on public.app_export_definition for select to authenticated using (public.has_effective_permission(33554432));
create policy app_export_filter_definition_select_effective on public.app_export_filter_definition for select to authenticated using (public.has_effective_permission(33554432));
create policy app_export_column_definition_select_effective on public.app_export_column_definition for select to authenticated using (public.has_effective_permission(33554432));

drop policy if exists saison_admin_full on public.saison;
create policy saison_effective_configuration on public.saison for all to authenticated using (public.has_effective_permission(67108864)) with check (public.has_effective_permission(67108864));
drop policy if exists pdf_template_catalog_read_admin on public.pdf_template_catalog;
drop policy if exists pdf_template_catalog_write_admin on public.pdf_template_catalog;
create policy pdf_template_catalog_read_effective on public.pdf_template_catalog for select to authenticated using (public.has_effective_permission(134217728));
create policy pdf_template_catalog_write_effective on public.pdf_template_catalog for all to authenticated using (public.has_effective_permission(134217728)) with check (public.has_effective_permission(134217728));

drop policy if exists vereinsfunktion_admin_full on public.vereinsfunktion;
drop policy if exists vereinsfunktion_besetzung_admin_full on public.vereinsfunktion_besetzung;
drop policy if exists vereinsfunktion_vertretung_admin_full on public.vereinsfunktion_vertretung;
drop policy if exists vereinsfunktion_verwendung_admin_full on public.vereinsfunktion_verwendung;
create policy vereinsfunktion_effective on public.vereinsfunktion for all to authenticated using (public.has_effective_permission(536870912)) with check (public.has_effective_permission(536870912));
create policy vereinsfunktion_besetzung_effective on public.vereinsfunktion_besetzung for all to authenticated using (public.has_effective_permission(536870912)) with check (public.has_effective_permission(536870912));
create policy vereinsfunktion_vertretung_effective on public.vereinsfunktion_vertretung for all to authenticated using (public.has_effective_permission(536870912)) with check (public.has_effective_permission(536870912));
create policy vereinsfunktion_verwendung_effective on public.vereinsfunktion_verwendung for all to authenticated using (public.has_effective_permission(536870912)) with check (public.has_effective_permission(536870912));

drop policy if exists kommunikationsvorgang_admin_vorstand_all on public.kommunikationsvorgang;
create policy kommunikationsvorgang_effective on public.kommunikationsvorgang for all to authenticated using (public.has_effective_permission(1073741824)) with check (public.has_effective_permission(1073741824));
drop policy if exists serienbrief_vorlage_admin_vorstand_all on public.serienbrief_vorlage;
drop policy if exists serienbrief_vorlage_exportfeld_admin_vorstand_all on public.serienbrief_vorlage_exportfeld;
drop policy if exists serienbrief_vorlage_freifeld_admin_vorstand_all on public.serienbrief_vorlage_freifeld;
create policy serienbrief_vorlage_effective on public.serienbrief_vorlage for all to authenticated using (public.has_effective_permission(1073741824)) with check (public.has_effective_permission(1073741824));
create policy serienbrief_vorlage_exportfeld_effective on public.serienbrief_vorlage_exportfeld for all to authenticated using (public.has_effective_permission(1073741824)) with check (public.has_effective_permission(1073741824));
create policy serienbrief_vorlage_freifeld_effective on public.serienbrief_vorlage_freifeld for all to authenticated using (public.has_effective_permission(1073741824)) with check (public.has_effective_permission(1073741824));

create or replace function public.assert_export_access()
returns void language plpgsql security definer set search_path = public, pg_temp as $$
begin
  if not public.has_effective_permission(33554432) then
    raise exception 'CanManageExports required' using errcode = '42501';
  end if;
end;
$$;

-- Internal live-data access is a member-administration capability; demo scoping
-- remains enforced by the callers' existing can_access_demo_scope predicates.
create or replace function public.can_access_live_internal_data()
returns boolean language sql stable security definer set search_path to 'public', 'pg_temp' as $$
  select public.has_effective_permission(4);
$$;

-- Contract and document policy mapping: creation/contract administration (524288),
-- document management (16), historical-contract registration (16777216), and club
-- document administration (268435456). Existing owner and demo predicates stay intact.
drop policy if exists dokument_select on public.dokument;
drop policy if exists dokument_write on public.dokument;
create policy dokument_select_effective on public.dokument for select to authenticated using (
  (public.has_effective_permission(65536) or public.has_effective_permission(16)
   or mitglied_id = public.current_mitglied_id()
   or (parzelle_id is not null and exists (select 1 from public.parzellen_belegung b where b.parzelle_id=dokument.parzelle_id and b.mitglied_id=public.current_mitglied_id() and (b.von_datum is null or b.von_datum <= now()::date) and (b.bis_datum is null or b.bis_datum >= now()::date))))
  and (not public.is_demo_or_reviewer() or public.is_demo_dokument_scope(mitglied_id, parzelle_id)));
create policy dokument_write_effective on public.dokument for all to authenticated using (public.has_effective_permission(16)) with check (public.has_effective_permission(16));

drop policy if exists pachtverhaeltnis_admin_vorstand_select on public.pachtverhaeltnis;
drop policy if exists pachtverhaeltnis_beteiligung_admin_vorstand_select on public.pachtverhaeltnis_beteiligung;
drop policy if exists pachtvertragsfassung_admin_vorstand_select on public.pachtvertragsfassung;
create policy pachtverhaeltnis_contract_effective on public.pachtverhaeltnis for select to authenticated using (public.has_effective_permission(524288));
create policy pachtverhaeltnis_beteiligung_contract_effective on public.pachtverhaeltnis_beteiligung for select to authenticated using (public.has_effective_permission(524288));
create policy pachtvertragsfassung_contract_effective on public.pachtvertragsfassung for select to authenticated using (public.has_effective_permission(524288));

drop policy if exists mitglied_vereinsdokument_bereitstellung_read_admin_vorstand on public.mitglied_vereinsdokument_bereitstellung;
drop policy if exists mitglied_vereinsdokument_bereitstellung_storno_admin_vorstand on public.mitglied_vereinsdokument_bereitstellung;
drop policy if exists vereinsdokument_fassung_read_admin_vorstand on public.vereinsdokument_fassung;
drop policy if exists vereinsdokument_typ_admin_vorstand_all on public.vereinsdokument_typ;
create policy mitglied_vereinsdokument_bereitstellung_effective on public.mitglied_vereinsdokument_bereitstellung for select to authenticated using (public.has_effective_permission(268435456));
create policy mitglied_vereinsdokument_bereitstellung_storno_effective on public.mitglied_vereinsdokument_bereitstellung for update to authenticated using (storniert_am is null and public.has_effective_permission(268435456)) with check (public.has_effective_permission(268435456));
create policy vereinsdokument_fassung_effective on public.vereinsdokument_fassung for select to authenticated using (public.has_effective_permission(268435456));
create policy vereinsdokument_typ_effective on public.vereinsdokument_typ for all to authenticated using (public.has_effective_permission(268435456)) with check (public.has_effective_permission(268435456));

drop policy if exists prepared_contract_signatur_select_admin on public.prepared_contract_signatur;
drop policy if exists prepared_contract_signatur_snapshot_person_select_admin on public.prepared_contract_signatur_snapshot_person;
drop policy if exists prepared_contract_signatur_snapshot_quelle_select_admin on public.prepared_contract_signatur_snapshot_quelle;
create policy prepared_contract_signatur_effective on public.prepared_contract_signatur for select to authenticated using (public.has_effective_permission(16));
create policy prepared_contract_signatur_snapshot_person_effective on public.prepared_contract_signatur_snapshot_person for select to authenticated using (public.has_effective_permission(16));
create policy prepared_contract_signatur_snapshot_quelle_effective on public.prepared_contract_signatur_snapshot_quelle for select to authenticated using (public.has_effective_permission(16));

-- P11-R01: Starting the board review is the first administrative action in
-- the membership-admission workflow.  It is deliberately guarded by the
-- membership-creation right, not by the broad document-management right.
-- The workflow's readiness, final-document provenance and voter-snapshot
-- validations below are the historic implementation and remain unchanged.
create or replace function public.start_mitgliedsaufnahme_vorstandsbearbeitung(p_kommunikationsvorgang_id bigint)
returns bigint
language plpgsql
security definer
set search_path=public,pg_temp as $$
declare v public.kommunikationsvorgang%rowtype; b_id bigint; r record; p_id bigint; caller uuid:=auth.uid();
begin
 if caller is null or not public.has_effective_permission(524288) then raise exception 'Keine Berechtigung zum Start der Vorstandsbearbeitung' using errcode='42501'; end if;
 select * into v from public.kommunikationsvorgang where id=p_kommunikationsvorgang_id for update;
 if not found or v.vorgangstyp<>'vertrag_signatur' or v.dokumenttyp<>'mitgliedsantrag' or v.status<>'letzte_pruefung' or v.final_dokument_id is null or v.signatur_revision is distinct from v.revision then raise exception 'Mitgliedsantrag ist nicht zur Vorstandsbearbeitung bereit' using errcode='23514'; end if;
 if public.is_demo_or_reviewer() and not public.is_demo_mitglied_id(v.mitglied_id) then raise exception 'Mitgliedsantrag liegt außerhalb des zulässigen Demo-Scopes' using errcode='42501'; end if;
 select id into b_id from public.mitgliedsaufnahme_vorstandsbearbeitung where kommunikationsvorgang_id=v.id; if found then return b_id; end if;
 if not exists(select 1 from public.dokument d where d.id=v.final_dokument_id and d.archiviert_at is null and d.source_kommunikationsvorgang_id=v.id and d.source_revision=v.revision) then raise exception 'Finaldokument passt nicht zum Vorgang' using errcode='23514'; end if;
 insert into public.mitgliedsaufnahme_vorstandsbearbeitung(kommunikationsvorgang_id,gestartet_von_auth_user_id) values(v.id,caller) returning id into b_id;
 for r in select * from public.mitgliedsaufnahme_berechtigungen('mitgliedsaufnahme_abstimmung',current_date) loop
   insert into public.mitgliedsaufnahme_abstimmung_snapshot_person(vorstandsbearbeitung_id,mitglied_id,name_snapshot)
   select b_id,r.mitglied_id,coalesce(nullif(btrim(concat_ws(' ',m.vorname,m.name)),''),case when m.mitgliedsnummer is not null then 'Mgl.-Nr. '||m.mitgliedsnummer::text else 'Mitglied' end) from public.mitglied m where m.id=r.mitglied_id
   on conflict(vorstandsbearbeitung_id,mitglied_id) do update set name_snapshot=excluded.name_snapshot returning id into p_id;
   insert into public.mitgliedsaufnahme_abstimmung_snapshot_berechtigung(snapshot_person_id,berechtigungsart,vereinsfunktion_id,vereinsfunktion_besetzung_id,funktion_schluessel_snapshot,funktionsbezeichnung_snapshot,vertretung_id,vertretene_vereinsfunktion_id,vertretene_besetzung_id,vertretene_funktionsbezeichnung_snapshot) values(p_id,r.berechtigungsart,r.vereinsfunktion_id,r.vereinsfunktion_besetzung_id,r.funktion_schluessel,r.funktionsbezeichnung,r.vertretung_id,r.vertretene_vereinsfunktion_id,r.vertretene_besetzung_id,r.vertretene_funktionsbezeichnung) on conflict do nothing;
 end loop;
 if not exists(select 1 from public.mitgliedsaufnahme_abstimmung_snapshot_person where vorstandsbearbeitung_id=b_id) then raise exception 'Keine Stimmberechtigten konfiguriert' using errcode='23514'; end if;
 return b_id;
end;
$$;
