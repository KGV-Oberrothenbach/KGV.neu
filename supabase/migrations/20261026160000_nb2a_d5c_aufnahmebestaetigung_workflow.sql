-- NB-2A-D5C-2A: keep the admission decision authoritative and create one
-- retryable document-generation process for its admission confirmation.
-- Source only. This migration is deliberately not executed by this change.

create unique index if not exists ux_kommunikationsvorgang_aufnahmebestaetigung_quelle
  on public.kommunikationsvorgang (((entwurf_daten->>'mitgliedsantrag_kommunikationsvorgang_id')::bigint))
  where vorgangstyp='dokument_erzeugung'
    and dokumenttyp='aufnahmebestaetigung';

create or replace function public.ensure_aufnahmebestaetigung_dokumentvorgang(p_bearbeitung_id bigint)
returns public.kommunikationsvorgang
language plpgsql security definer set search_path=public,pg_temp as $$
declare
  v_bearbeitung public.mitgliedsaufnahme_vorstandsbearbeitung%rowtype;
  v_antrag public.kommunikationsvorgang%rowtype;
  v_mitglied public.mitglied%rowtype;
  v_dokumentvorgang public.kommunikationsvorgang%rowtype;
begin
  if auth.uid() is null or not public.kann_mitgliedsaufnahme_entscheiden() then
    raise exception 'Keine Berechtigung für die Aufnahmebestätigung.' using errcode='42501';
  end if;

  select * into v_bearbeitung
  from public.mitgliedsaufnahme_vorstandsbearbeitung
  where id=p_bearbeitung_id
  for update;
  if not found or v_bearbeitung.entscheidung<>'angenommen' then
    raise exception 'Eine Aufnahmebestätigung ist erst nach der positiven Aufnahmeentscheidung möglich.' using errcode='23514';
  end if;

  select * into v_antrag
  from public.kommunikationsvorgang
  where id=v_bearbeitung.kommunikationsvorgang_id
  for key share;
  if not found or v_antrag.vorgangstyp<>'vertrag_signatur' or v_antrag.dokumenttyp<>'mitgliedsantrag' or v_antrag.status<>'erledigt' then
    raise exception 'Der ursprüngliche Mitgliedsantrag ist nicht abgeschlossen.' using errcode='23514';
  end if;

  select * into v_mitglied
  from public.mitglied
  where id=v_antrag.mitglied_id
  for key share;
  if not found or v_mitglied.aufnahme_status<>'aufgenommen' or v_mitglied.aktiv is not true or v_mitglied.mitglied_seit is null then
    raise exception 'Das Mitglied ist noch nicht erfolgreich aufgenommen.' using errcode='23514';
  end if;

  -- Serializes retries for the concrete original membership-application process.
  perform pg_advisory_xact_lock(v_antrag.id);
  select * into v_dokumentvorgang
  from public.kommunikationsvorgang
  where vorgangstyp='dokument_erzeugung'
    and dokumenttyp='aufnahmebestaetigung'
    and (entwurf_daten->>'mitgliedsantrag_kommunikationsvorgang_id')::bigint=v_antrag.id
  for update;
  if found then
    return v_dokumentvorgang;
  end if;

  insert into public.kommunikationsvorgang
    (vorgangstyp,dokumenttyp,status,mitglied_id,entwurf_daten,revision)
  values
    ('dokument_erzeugung','aufnahmebestaetigung','vorbereitet',v_mitglied.id,
     jsonb_build_object(
       'schema_version','kgv-aufnahmebestaetigung/v1',
       'template_key','aufnahmebestaetigung',
       'template_version','v1',
       'mitgliedsantrag_kommunikationsvorgang_id',v_antrag.id,
       'mitgliedsaufnahme_vorstandsbearbeitung_id',v_bearbeitung.id,
       'mitglied_id',v_mitglied.id
     ),
     1)
  returning * into v_dokumentvorgang;
  return v_dokumentvorgang;
end;
$$;

-- The decision remains leading. Document creation itself is a separate,
-- retryable action. The process shell is created only after the membership and
-- original application have been completed successfully.
create or replace function public.entscheide_mitgliedsaufnahme(p_bearbeitung_id bigint,p_ergebnis text,p_weg text,p_sitzungsdatum date default null,p_begruendung text default null,p_aufnahmedatum date default null)
returns void language plpgsql security definer set search_path=public,pg_temp as $$
declare b public.mitgliedsaufnahme_vorstandsbearbeitung%rowtype; v public.kommunikationsvorgang%rowtype; caller uuid:=auth.uid(); cnt integer; yes_cnt integer;
begin
 if caller is null or p_ergebnis not in ('annehmen','ablehnen') or p_weg not in ('einstimmig','vorstandssitzung') or (p_ergebnis='annehmen' and p_aufnahmedatum is null) then raise exception 'Ungültige Entscheidung oder fehlendes Aufnahmedatum' using errcode='23514'; end if;
 select * into b from public.mitgliedsaufnahme_vorstandsbearbeitung where id=p_bearbeitung_id for update; if not found or b.entscheidung<>'offen' then raise exception 'Bearbeitung ist nicht offen' using errcode='40001'; end if;
 if not exists(select 1 from public.mitgliedsaufnahme_berechtigungen('mitgliedsaufnahme_entscheidung',current_date) x join public.app_user au on au.mitglied_id=x.mitglied_id where au.user_id=caller) then raise exception 'Keine aktuelle Entscheidungsberechtigung' using errcode='42501'; end if;
 if p_weg='vorstandssitzung' and p_sitzungsdatum is null then raise exception 'Sitzungsdatum erforderlich' using errcode='23514'; end if;
 if p_ergebnis='ablehnen' and nullif(btrim(p_begruendung),'') is null then raise exception 'Entscheidungsbegründung erforderlich' using errcode='23514'; end if;
 select * into v from public.kommunikationsvorgang where id=b.kommunikationsvorgang_id for update; if v.status<>'letzte_pruefung' then raise exception 'Kommunikationsvorgang ist nicht abschließbar' using errcode='40001'; end if;
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
 update public.mitgliedsaufnahme_vorstandsbearbeitung set entscheidung=case when p_ergebnis='annehmen' then 'angenommen' else 'abgelehnt' end,entscheidungsweg=p_weg,sitzungsdatum=p_sitzungsdatum,entscheidungsbegruendung=case when p_ergebnis='ablehnen' then nullif(btrim(p_begruendung),'') else null end,entschieden_am=now(),entschieden_von_auth_user_id=caller,updated_at=now() where id=b.id;
 if p_ergebnis='annehmen' then
   perform public.ensure_aufnahmebestaetigung_dokumentvorgang(b.id);
 end if;
end;
$$;

create or replace function public.complete_aufnahmebestaetigung(p_kommunikationsvorgang_id bigint,p_dokument_id bigint)
returns void language plpgsql security definer set search_path=public,pg_temp as $$
declare v_vorgang public.kommunikationsvorgang%rowtype; v_dokument public.dokument%rowtype;
begin
  if auth.uid() is null and auth.role() is distinct from 'service_role' then
    raise exception 'Anmeldung erforderlich.' using errcode='42501';
  end if;
  select * into v_vorgang from public.kommunikationsvorgang where id=p_kommunikationsvorgang_id for update;
  if not found or v_vorgang.vorgangstyp<>'dokument_erzeugung' or v_vorgang.dokumenttyp<>'aufnahmebestaetigung' then
    raise exception 'Aufnahmebestätigungsvorgang ist ungültig.' using errcode='23514';
  end if;
  select * into v_dokument from public.dokument where id=p_dokument_id and archiviert_at is null for key share;
  if not found or v_dokument.mitglied_id is distinct from v_vorgang.mitglied_id or v_dokument.source_kommunikationsvorgang_id is distinct from v_vorgang.id or v_dokument.source_revision is distinct from v_vorgang.revision then
    raise exception 'Das finale Dokument passt nicht zum Aufnahmebestätigungsvorgang.' using errcode='23514';
  end if;
  if v_vorgang.status='erledigt' and v_vorgang.final_dokument_id=v_dokument.id then
    return;
  end if;
  update public.kommunikationsvorgang
  set final_dokument_id=v_dokument.id,status='erledigt',fehlertext=null
  where id=v_vorgang.id and status='vorbereitet' and final_dokument_id is null;
  if not found then
    raise exception 'Aufnahmebestätigung ist nicht abschließbar.' using errcode='40001';
  end if;
end;
$$;

create or replace view public.mitgliedsaufnahme_vorstand_liste with (security_invoker=true) as
select b.id bearbeitung_id,b.kommunikationsvorgang_id,v.mitglied_id,v.final_dokument_id,spc.name_snapshot antragsteller,b.gestartet_am,b.entscheidung,
 count(sp.id) snapshot_personen,count(s.stimme) abgegebene_stimmen,count(s.stimme) filter(where s.stimme='ja') ja_stimmen,count(s.stimme) filter(where s.stimme='nein') nein_stimmen,
 (count(sp.id)>0 and count(s.stimme)=count(sp.id) and count(s.stimme) filter(where s.stimme='ja')=count(sp.id)) einstimmig_ja,
 (count(s.stimme) filter(where s.stimme='nein')>0) konflikt,
 confirmation.id aufnahmebestaetigung_vorgang_id,confirmation.status aufnahmebestaetigung_status,confirmation.final_dokument_id aufnahmebestaetigung_dokument_id
from public.mitgliedsaufnahme_vorstandsbearbeitung b
join public.kommunikationsvorgang v on v.id=b.kommunikationsvorgang_id
join public.mitglied m on m.id=v.mitglied_id
left join lateral(select coalesce(nullif(btrim(concat_ws(' ',m.vorname,m.name)),''),'Mitglied #'||m.id) name_snapshot) spc on true
left join lateral (
  select d.id,d.status,d.final_dokument_id
  from public.kommunikationsvorgang d
  where d.vorgangstyp='dokument_erzeugung'
    and d.dokumenttyp='aufnahmebestaetigung'
    and (d.entwurf_daten->>'mitgliedsantrag_kommunikationsvorgang_id')::bigint=v.id
  limit 1
) confirmation on true
left join public.mitgliedsaufnahme_abstimmung_snapshot_person sp on sp.vorstandsbearbeitung_id=b.id
left join lateral(select stimme from public.mitgliedsaufnahme_abstimmung_stimme where snapshot_person_id=sp.id order by version desc limit 1) s on true
group by b.id,v.mitglied_id,v.final_dokument_id,spc.name_snapshot,confirmation.id,confirmation.status,confirmation.final_dokument_id;

revoke all on function public.ensure_aufnahmebestaetigung_dokumentvorgang(bigint),public.complete_aufnahmebestaetigung(bigint,bigint) from public,anon;
grant execute on function public.ensure_aufnahmebestaetigung_dokumentvorgang(bigint) to authenticated,service_role;
grant execute on function public.complete_aufnahmebestaetigung(bigint,bigint) to service_role;
