-- The membership application is the sole commit point for legal-representative master data.
create or replace function public.complete_prepared_mitgliedsantrag(p_kommunikationsvorgang_id bigint, p_expected_revision integer, p_created_by uuid)
returns table(vertreter_mitglied_id bigint, bereits_abgeschlossen boolean)
language plpgsql security definer set search_path = public, pg_temp as $$
declare v public.kommunikationsvorgang%rowtype; d public.dokument%rowtype; rep jsonb; representative_id bigint; current_relation record; start_date date; member_row public.mitglied%rowtype;
begin
  if auth.role() is distinct from 'service_role' then raise exception 'Der Mitgliedsantragsabschluss ist nur serverseitig zulässig' using errcode='42501'; end if;
  if not exists (select 1 from public.app_user where user_id=p_created_by and role in ('admin','vorstand')) then raise exception 'Keine Berechtigung' using errcode='42501'; end if;
  select * into v from public.kommunikationsvorgang where id=p_kommunikationsvorgang_id for update;
  if not found or v.vorgangstyp <> 'vertrag_signatur' or v.dokumenttyp <> 'mitgliedsantrag' then raise exception 'Der Vorgang ist kein Mitgliedsantrag' using errcode='23514'; end if;
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
revoke all on function public.complete_prepared_mitgliedsantrag(bigint,integer,uuid) from public,anon,authenticated,service_role;
grant execute on function public.complete_prepared_mitgliedsantrag(bigint,integer,uuid) to service_role;
