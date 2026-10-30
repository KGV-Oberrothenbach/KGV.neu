-- G11b3: each configured club has its own database (the existing single-club schema).
-- Operation identity is scoped by that database, actor and demo context, never by business heuristics.
create table if not exists public.historical_contract_operation (
 operation_id uuid primary key,
 created_by uuid not null references auth.users(id),
 mitglied_id bigint not null references public.mitglied(id),
 is_demo boolean not null,
 contract_type text not null check(contract_type in ('historischer_ma_pv_erstvertrag','historischer_pv_folgevertrag')),
 upload_request jsonb not null,
 drive_file_id text not null unique,
 file_name text not null,
 document_id bigint unique references public.dokument(id),
 registration_request jsonb,
 registration_result jsonb,
 created_at timestamptz not null default now(),
 check ((registration_request is null) = (registration_result is null))
);
alter table public.historical_contract_operation enable row level security;
revoke all on public.historical_contract_operation from public, anon, authenticated;
grant all on public.historical_contract_operation to service_role;

create or replace function public.begin_historical_contract_upload(p_operation_id uuid,p_actor uuid,p_member_id bigint,p_contract_type text,p_upload_request jsonb,p_drive_file_id text,p_file_name text)
returns jsonb language plpgsql security definer set search_path=public,pg_temp as $$
declare v_op public.historical_contract_operation%rowtype; v_demo boolean;
begin
 if auth.role() is distinct from 'service_role' then raise exception 'Backend required' using errcode='42501'; end if;
 if p_operation_id is null or p_actor is null or p_upload_request is null or p_drive_file_id is null or p_file_name is null then raise exception 'Incomplete operation' using errcode='22023'; end if;
 select coalesce(m.is_demo,false) into strict v_demo from public.mitglied m where m.id=p_member_id;
 insert into public.historical_contract_operation(operation_id,created_by,mitglied_id,is_demo,contract_type,upload_request,drive_file_id,file_name)
 values(p_operation_id,p_actor,p_member_id,v_demo,p_contract_type,p_upload_request,p_drive_file_id,p_file_name)
 on conflict(operation_id) do nothing;
 select o.* into strict v_op from public.historical_contract_operation o where o.operation_id=p_operation_id for update;
 if v_op.created_by is distinct from p_actor or v_op.mitglied_id is distinct from p_member_id or v_op.is_demo is distinct from v_demo
 or v_op.contract_type is distinct from p_contract_type or v_op.upload_request is distinct from p_upload_request then
 raise exception 'Vorgangs-ID wurde bereits mit anderen Uploaddaten verwendet.' using errcode='22023'; end if;
 return to_jsonb(v_op);
end; $$;

create or replace function public.finish_historical_contract_upload(p_operation_id uuid,p_storage_path text)
returns jsonb language plpgsql security definer set search_path=public,pg_temp as $$
declare v_op public.historical_contract_operation%rowtype; v_doc public.dokument%rowtype;
begin
 if auth.role() is distinct from 'service_role' then raise exception 'Backend required' using errcode='42501'; end if;
 select o.* into strict v_op from public.historical_contract_operation o where o.operation_id=p_operation_id for update;
 if v_op.document_id is null then
  insert into public.dokument(mitglied_id,bucket,storage_path,drive_file_id,titel,dateiname,mime_type,size_bytes,created_by,historical_contract_type)
  values(v_op.mitglied_id,'dokumente',p_storage_path,v_op.drive_file_id,v_op.upload_request->>'title',v_op.file_name,v_op.upload_request->>'mime',
   (v_op.upload_request->>'size')::bigint,v_op.created_by,v_op.contract_type) returning * into v_doc;
  update public.historical_contract_operation o set document_id=v_doc.id where o.operation_id=p_operation_id;
 else select d.* into strict v_doc from public.dokument d where d.id=v_op.document_id; end if;
 return to_jsonb(v_doc);
end; $$;
revoke all on function public.begin_historical_contract_upload(uuid,uuid,bigint,text,jsonb,text,text) from public,anon,authenticated;
revoke all on function public.finish_historical_contract_upload(uuid,text) from public,anon,authenticated;
grant execute on function public.begin_historical_contract_upload(uuid,uuid,bigint,text,jsonb,text,text),public.finish_historical_contract_upload(uuid,text) to service_role;

create or replace function public.register_historischen_pachtvertrag_g11b1(
  p_dokument_id bigint,
  p_dokumenttyp_code text,
  p_vertragsdatum date,
  p_unterzeichnet boolean,
  p_pachtverhaeltnis_ids bigint[],
  p_vorherige_enddaten jsonb default '{}'::jsonb,
  p_mitglied_seit_korrektur_mitglied_id bigint default null,
  p_mitglied_seit_korrektur_datum date default null,
  p_vertragspartei_mitglied_ids bigint[] default null,
  p_mitgliedschaftsenden jsonb default '[]'::jsonb,
  p_aktiv_status_aenderungen jsonb default '[]'::jsonb
)
returns table(dokument_id bigint, dokumenttyp_id bigint, pachtverhaeltnis_id bigint, pachtvertragsfassung_id bigint, historische_vertragsnummer integer)
language plpgsql security definer set search_path = public, pg_temp as $$
#variable_conflict error
declare
  v_document public.dokument%rowtype;
  v_relation public.pachtverhaeltnis%rowtype;
  v_current public.pachtverhaeltnis_beteiligung%rowtype;
  v_member public.mitglied%rowtype;
  v_party_id bigint;
  v_relation_id bigint;
  v_command jsonb;
  v_command_member_id bigint;
  v_end_date date;
  v_active boolean;
  v_document_demo boolean;
  v_actor_demo boolean;
  v_predecessor_start date;
  v_successor_start date;
  v_next_party_start date;
  v_party_end date;
  v_removed_ids bigint[] := '{}'::bigint[];
  v_context_ids bigint[];
  v_today date := public.kgv_local_now()::date;
  v_has_effective_lease boolean;
begin
  if not coalesce(public.can_register_historical_contracts(), false) then
    raise exception 'Für die Nacherfassung historischer Verträge fehlt die Berechtigung.' using errcode = '42501';
  end if;
  if p_vertragsdatum is null or not isfinite(p_vertragsdatum) or p_unterzeichnet is null
     or p_dokumenttyp_code is null or p_dokumenttyp_code not in ('historischer_ma_pv_erstvertrag', 'historischer_pv_folgevertrag') then
    raise exception 'Vertragsart, endliches Vertragsdatum und Signaturstatus sind erforderlich.' using errcode = '22023';
  end if;
  if coalesce(cardinality(p_pachtverhaeltnis_ids), 0) = 0
     or exists (select 1 from unnest(p_pachtverhaeltnis_ids) as ids(id) where ids.id is null or ids.id <= 0)
     or exists (select 1 from unnest(p_pachtverhaeltnis_ids) as ids(id) group by ids.id having count(*) > 1)
     or coalesce(cardinality(p_vertragspartei_mitglied_ids), 0) = 0
     or exists (select 1 from unnest(p_vertragspartei_mitglied_ids) as ids(id) where ids.id is null or ids.id <= 0)
     or exists (select 1 from unnest(p_vertragspartei_mitglied_ids) as ids(id) group by ids.id having count(*) > 1) then
    raise exception 'Eindeutige Pachtbeziehungen und vorhandene Vertragsparteien sind erforderlich.' using errcode = '22023';
  end if;
  if jsonb_typeof(p_vorherige_enddaten) is distinct from 'object'
     or jsonb_typeof(p_mitgliedschaftsenden) is distinct from 'array'
     or jsonb_typeof(p_aktiv_status_aenderungen) is distinct from 'array' then
    raise exception 'Ungültiges Format der bestätigten Änderungen.' using errcode = '22023';
  end if;
  if exists (select 1 from jsonb_object_keys(p_vorherige_enddaten) as keys(id)
    where not exists (select 1 from unnest(p_pachtverhaeltnis_ids) as ids(id) where ids.id::text = keys.id)) then
    raise exception 'Vorgängerenddaten dürfen nur ausgewählte Pachtbeziehungen betreffen.' using errcode = '22023';
  end if;

  select d.* into v_document from public.dokument as d where d.id = p_dokument_id for update;
  if not found or v_document.archiviert_at is not null or v_document.mitglied_id is null then
    raise exception 'Das Vertragsdokument benötigt ein vorhandenes Mitglied.' using errcode = 'P0002';
  end if;
  if not (v_document.mitglied_id = any(p_vertragspartei_mitglied_ids)) then
    raise exception 'Das Dokumentmitglied muss Vertragspartei sein.' using errcode = '23514';
  end if;
  select coalesce(m.is_demo, false) into v_document_demo from public.mitglied as m where m.id = v_document.mitglied_id;
  if auth.role() is distinct from 'service_role' then
    select c.is_demo_account into v_actor_demo from public.current_app_user_context() as c;
    if not found or v_actor_demo is distinct from v_document_demo then
      raise exception 'Der Vertrag liegt außerhalb des freigegebenen Datenbereichs.' using errcode = '42501';
    end if;
  end if;
  if exists (select 1 from unnest(p_vertragspartei_mitglied_ids) as ids(id)
    where not exists (select 1 from public.mitglied as m where m.id = ids.id and coalesce(m.is_demo, false) = v_document_demo)) then
    raise exception 'Eine Vertragspartei fehlt oder liegt außerhalb des Vertragsbereichs.' using errcode = '42501';
  end if;

  -- Lock selected relations in canonical order. Establish the predecessor context
  -- from the unchanged timeline, never from client-supplied member commands.
  for v_relation_id in select ids.id from unnest(p_pachtverhaeltnis_ids) as ids(id) order by ids.id loop
    select r.* into v_relation from public.pachtverhaeltnis as r where r.id = v_relation_id for update;
    if not found then
      raise exception 'Unbekannte Pachtbeziehung.' using errcode = 'P0002';
    end if;
    if (v_document.parzelle_id is not null and v_document.parzelle_id <> v_relation.parzelle_id)
       or not exists (select 1 from public.pachtverhaeltnis_beteiligung as b
         where b.pachtverhaeltnis_id = v_relation_id and b.mitglied_id = v_document.mitglied_id)
       or exists (select 1 from public.pachtverhaeltnis_beteiligung as b join public.mitglied as m on m.id = b.mitglied_id
         where b.pachtverhaeltnis_id = v_relation_id and coalesce(m.is_demo, false) <> v_document_demo) then
      raise exception 'Die Pachtbeziehung gehört nicht zum Vertragskontext.' using errcode = '42501';
    end if;
    if v_relation.beendet_am < p_vertragsdatum then
      raise exception 'Die Pachtbeziehung war am Vertragsdatum bereits beendet.' using errcode = '23514';
    end if;
    if exists (select 1 from public.pachtvertragsfassung as f
      where f.pachtverhaeltnis_id = v_relation_id and f.wirksam_ab = p_vertragsdatum and f.dokument_id <> p_dokument_id) then
      raise exception 'Für dieses Datum besteht bereits ein anderer Vertragsstand.' using errcode = '23514';
    end if;
    select max(f.wirksam_ab) into v_predecessor_start from public.pachtvertragsfassung as f
      where f.pachtverhaeltnis_id = v_relation_id and f.wirksam_ab < p_vertragsdatum;
    select array(select distinct b.mitglied_id from public.pachtverhaeltnis_beteiligung as b
      where b.pachtverhaeltnis_id = v_relation_id and b.gueltig_ab <= v_predecessor_start
        and (b.gueltig_bis is null or b.gueltig_bis >= v_predecessor_start)
        and not (b.mitglied_id = any(p_vertragspartei_mitglied_ids))) || v_removed_ids into v_removed_ids;
  end loop;
  select array(select distinct ids.id from unnest(p_vertragspartei_mitglied_ids || v_removed_ids) as ids(id)) into v_context_ids;
  -- Also serialize membership consequences for persons present on other parcels.
  perform m.id from public.mitglied as m where m.id = any(v_context_ids) order by m.id for update;

  if p_mitglied_seit_korrektur_mitglied_id is not null or p_mitglied_seit_korrektur_datum is not null then
    if p_dokumenttyp_code <> 'historischer_ma_pv_erstvertrag'
       or p_mitglied_seit_korrektur_mitglied_id is null or p_mitglied_seit_korrektur_datum is null
       or not isfinite(p_mitglied_seit_korrektur_datum)
       or p_mitglied_seit_korrektur_datum > p_vertragsdatum
       or not (p_mitglied_seit_korrektur_mitglied_id = any(p_vertragspartei_mitglied_ids)) then
      raise exception 'Mitglied-seit-Korrekturen sind nur für eine Aufnahmeperson dieses MA+PV-Erstvertrags zulässig.' using errcode = '23514';
    end if;
  end if;
  if exists (select 1 from jsonb_array_elements(p_mitgliedschaftsenden) as cmd(value)
      group by cmd.value ->> 'mitglied_id' having count(*) > 1)
     or exists (select 1 from jsonb_array_elements(p_aktiv_status_aenderungen) as cmd(value)
      group by cmd.value ->> 'mitglied_id' having count(*) > 1) then
    raise exception 'Mitgliedsänderungen dürfen keine doppelten Befehle enthalten.' using errcode = '22023';
  end if;
  for v_command in select cmd.value from jsonb_array_elements(p_mitgliedschaftsenden) as cmd(value) loop
    v_command_member_id := (v_command ->> 'mitglied_id')::bigint;
    v_end_date := (v_command ->> 'mitglied_ende')::date;
    if p_dokumenttyp_code <> 'historischer_pv_folgevertrag'
       or v_command_member_id is null or v_end_date is null or not isfinite(v_end_date)
       or not (v_command_member_id = any(v_removed_ids))
       or v_command_member_id = any(p_vertragspartei_mitglied_ids) then
      raise exception 'Ein Mitgliedschaftsende ist nur für eine herausfallende Vorgängerpartei dieses Folgevertrags zulässig.' using errcode = '23514';
    end if;
  end loop;
  for v_command in select cmd.value from jsonb_array_elements(p_aktiv_status_aenderungen) as cmd(value) loop
    v_command_member_id := (v_command ->> 'mitglied_id')::bigint;
    if v_command_member_id is null or not (v_command_member_id = any(v_context_ids))
       or jsonb_typeof(v_command -> 'aktiv') is distinct from 'boolean' then
      raise exception 'Ein Aktiv-/Passiv-Befehl benötigt eine Person dieses Vertragsvorgangs und einen bestätigten Status.' using errcode = '23514';
    end if;
  end loop;

  foreach v_relation_id in array p_pachtverhaeltnis_ids loop
    select r.* into v_relation from public.pachtverhaeltnis as r where r.id = v_relation_id;
    select min(f.wirksam_ab) into v_successor_start from public.pachtvertragsfassung as f
      where f.pachtverhaeltnis_id = v_relation_id and f.wirksam_ab > p_vertragsdatum;
    -- A removed party loses only the selected relation, not another parcel or
    -- the state at an already existing successor. Split a spanning period.
    for v_current in select b.* from public.pachtverhaeltnis_beteiligung as b
      where b.pachtverhaeltnis_id = v_relation_id and b.gueltig_ab <= p_vertragsdatum
        and (b.gueltig_bis is null or b.gueltig_bis >= p_vertragsdatum)
        and not (b.mitglied_id = any(p_vertragspartei_mitglied_ids)) for update loop
      if v_current.gueltig_ab = p_vertragsdatum then
        raise exception 'Eine abweichende Beteiligung beginnt bereits am selben Vertragsdatum.' using errcode = '23514';
      end if;
      update public.pachtverhaeltnis_beteiligung as b set gueltig_bis = p_vertragsdatum - 1 where b.id = v_current.id;
      if v_successor_start is not null and (v_current.gueltig_bis is null or v_current.gueltig_bis >= v_successor_start) then
        insert into public.pachtverhaeltnis_beteiligung(pachtverhaeltnis_id, mitglied_id, rolle, gueltig_ab, gueltig_bis, created_by)
        values (v_relation_id, v_current.mitglied_id, v_current.rolle, v_successor_start, v_current.gueltig_bis, v_current.created_by);
      end if;
    end loop;
    foreach v_party_id in array p_vertragspartei_mitglied_ids loop
      if not exists (select 1 from public.pachtverhaeltnis_beteiligung as b
        where b.pachtverhaeltnis_id = v_relation_id and b.mitglied_id = v_party_id
          and b.gueltig_ab <= p_vertragsdatum and (b.gueltig_bis is null or b.gueltig_bis >= p_vertragsdatum)) then
        select min(b.gueltig_ab) into v_next_party_start from public.pachtverhaeltnis_beteiligung as b
          where b.pachtverhaeltnis_id = v_relation_id and b.mitglied_id = v_party_id and b.gueltig_ab > p_vertragsdatum;
        v_party_end := least(v_successor_start - 1, v_next_party_start - 1, v_relation.beendet_am);
        insert into public.pachtverhaeltnis_beteiligung(pachtverhaeltnis_id, mitglied_id, rolle, gueltig_ab, gueltig_bis, created_by)
        values (v_relation_id, v_party_id, case when v_party_id = v_document.mitglied_id then 'hauptpaechter' else 'mitpaechter' end,
          p_vertragsdatum, v_party_end, auth.uid());
      end if;
    end loop;
  end loop;

  -- The shared foundation still owns classification, version links, numbering,
  -- the audited membership-since correction and the return contract.
  return query select f.* from public.register_historischen_pachtvertrag_g6a(
    p_dokument_id, p_dokumenttyp_code, p_vertragsdatum, p_unterzeichnet, p_pachtverhaeltnis_ids,
    p_vorherige_enddaten, p_mitglied_seit_korrektur_mitglied_id, p_mitglied_seit_korrektur_datum) as f;

  for v_command in select cmd.value from jsonb_array_elements(p_mitgliedschaftsenden) as cmd(value) loop
    v_command_member_id := (v_command ->> 'mitglied_id')::bigint;
    v_end_date := (v_command ->> 'mitglied_ende')::date;
    select m.* into v_member from public.mitglied as m where m.id = v_command_member_id;
    if v_end_date < v_member.mitglied_seit or (v_member.mitglied_ende is not null and v_member.mitglied_ende <> v_end_date) then
      raise exception 'Das bestätigte Mitgliedschaftsende widerspricht dem vorhandenen Mitgliedschaftszeitraum.' using errcode = '23514';
    end if;
    if exists (select 1 from public.pachtverhaeltnis_beteiligung as b
      join public.pachtverhaeltnis as r on r.id = b.pachtverhaeltnis_id
      where b.mitglied_id = v_command_member_id
        and least(coalesce(b.gueltig_bis, 'infinity'::date), coalesce(r.beendet_am, 'infinity'::date)) > v_end_date
        and b.gueltig_ab <= coalesce(r.beendet_am, 'infinity'::date)) then
      raise exception 'Mitgliedschaftsende ist wegen weiterer wirksamer Pachtbeteiligung nicht zulässig.' using errcode = '23514';
    end if;
    update public.mitglied as m set mitglied_ende = v_end_date where m.id = v_command_member_id;
  end loop;
  for v_command in select cmd.value from jsonb_array_elements(p_aktiv_status_aenderungen) as cmd(value) loop
    v_command_member_id := (v_command ->> 'mitglied_id')::bigint;
    v_active := (v_command ->> 'aktiv')::boolean;
    select exists (select 1 from public.pachtverhaeltnis_beteiligung as b
      join public.pachtverhaeltnis as r on r.id = b.pachtverhaeltnis_id
      where b.mitglied_id = v_command_member_id and b.gueltig_ab <= v_today
        and (b.gueltig_bis is null or b.gueltig_bis >= v_today)
        and (r.beendet_am is null or r.beendet_am >= v_today)) into v_has_effective_lease;
    if v_active is distinct from v_has_effective_lease
       or (v_active and exists (select 1 from public.mitglied as m
         where m.id = v_command_member_id and m.mitglied_ende <= v_today)) then
      raise exception 'Der bestätigte Aktiv-/Passiv-Status widerspricht dem aktuellen Pachtbestand oder Mitgliedschaftsende.' using errcode = '23514';
    end if;
    update public.mitglied as m set aktiv = v_active where m.id = v_command_member_id;
  end loop;
end;
$$;
revoke all on function public.register_historischen_pachtvertrag_g11b1(bigint,text,date,boolean,bigint[],jsonb,bigint,date,bigint[],jsonb,jsonb) from public,anon,authenticated;
grant execute on function public.register_historischen_pachtvertrag_g11b1(bigint,text,date,boolean,bigint[],jsonb,bigint,date,bigint[],jsonb,jsonb) to service_role;

-- Existing callers remain compatible for documents outside the operation workflow.
create or replace function public.register_historischen_pachtvertrag(
 p_dokument_id bigint,p_dokumenttyp_code text,p_vertragsdatum date,p_unterzeichnet boolean,p_pachtverhaeltnis_ids bigint[],
 p_vorherige_enddaten jsonb default '{}',p_mitglied_seit_korrektur_mitglied_id bigint default null,p_mitglied_seit_korrektur_datum date default null,
 p_vertragspartei_mitglied_ids bigint[] default null,p_mitgliedschaftsenden jsonb default '[]',p_aktiv_status_aenderungen jsonb default '[]')
returns table(dokument_id bigint,dokumenttyp_id bigint,pachtverhaeltnis_id bigint,pachtvertragsfassung_id bigint,historische_vertragsnummer integer)
language plpgsql security definer set search_path=public,pg_temp as $$
begin
 if exists(select 1 from public.historical_contract_operation o where o.document_id=p_dokument_id) then
 raise exception 'Dieser Vertrag benötigt seine ursprüngliche Vorgangs-ID.' using errcode='22023'; end if;
 return query select * from public.register_historischen_pachtvertrag_g11b1(p_dokument_id,p_dokumenttyp_code,p_vertragsdatum,p_unterzeichnet,p_pachtverhaeltnis_ids,p_vorherige_enddaten,p_mitglied_seit_korrektur_mitglied_id,p_mitglied_seit_korrektur_datum,p_vertragspartei_mitglied_ids,p_mitgliedschaftsenden,p_aktiv_status_aenderungen);
end; $$;

create or replace function public.register_historischen_pachtvertrag(
 p_operation_id uuid,p_dokument_id bigint,p_dokumenttyp_code text,p_vertragsdatum date,p_unterzeichnet boolean,p_pachtverhaeltnis_ids bigint[],
 p_vorherige_enddaten jsonb default '{}',p_mitglied_seit_korrektur_mitglied_id bigint default null,p_mitglied_seit_korrektur_datum date default null,
 p_vertragspartei_mitglied_ids bigint[] default null,p_mitgliedschaftsenden jsonb default '[]',p_aktiv_status_aenderungen jsonb default '[]')
returns table(dokument_id bigint,dokumenttyp_id bigint,pachtverhaeltnis_id bigint,pachtvertragsfassung_id bigint,historische_vertragsnummer integer)
language plpgsql security definer set search_path=public,pg_temp as $$
#variable_conflict error
declare v_op public.historical_contract_operation%rowtype; v_request jsonb; v_result jsonb;
begin
 if not coalesce(public.can_register_historical_contracts(),false) then raise exception 'Altvertragsberechtigung fehlt.' using errcode='42501'; end if;
 select o.* into v_op from public.historical_contract_operation o where o.operation_id=p_operation_id for update;
 if not found or v_op.created_by is distinct from auth.uid() or not public.can_access_historical_contract_member(v_op.mitglied_id)
 then raise exception 'Vorgang nicht freigegeben.' using errcode='42501'; end if;
 if not public.can_access_historical_contract_document(p_dokument_id,false) then raise exception 'Vertragsdokument nicht freigegeben.' using errcode='42501'; end if;
 if v_op.document_id is distinct from p_dokument_id or v_op.contract_type is distinct from p_dokumenttyp_code then
 raise exception 'Vorgangs-ID passt nicht zum Vertragsdokument.' using errcode='22023'; end if;
 -- jsonb equality is the fingerprint: no hash collision and independent of object-key order.
 -- Set-valued arrays are canonicalized, command arrays retain order (duplicate commands remain invalid).
 v_request:=jsonb_build_object('document',p_dokument_id,'type',p_dokumenttyp_code,'date',p_vertragsdatum,'signed',p_unterzeichnet,
 'leases',array(select x from unnest(p_pachtverhaeltnis_ids) x order by x),'previous',p_vorherige_enddaten,
 'correction_member',p_mitglied_seit_korrektur_mitglied_id,'correction_date',p_mitglied_seit_korrektur_datum,
 'parties',array(select x from unnest(p_vertragspartei_mitglied_ids) x order by x),'ends',p_mitgliedschaftsenden,'activity',p_aktiv_status_aenderungen);
 if v_op.registration_request is not null then
  if v_op.registration_request is distinct from v_request then raise exception 'Vorgangs-ID wurde bereits mit anderen Vertragsdaten verwendet.' using errcode='22023'; end if;
  v_result:=v_op.registration_result;
 else
  select jsonb_agg(to_jsonb(r) order by r.pachtverhaeltnis_id) into v_result from public.register_historischen_pachtvertrag_g11b1(
   p_dokument_id,p_dokumenttyp_code,p_vertragsdatum,p_unterzeichnet,p_pachtverhaeltnis_ids,p_vorherige_enddaten,
   p_mitglied_seit_korrektur_mitglied_id,p_mitglied_seit_korrektur_datum,p_vertragspartei_mitglied_ids,p_mitgliedschaftsenden,p_aktiv_status_aenderungen) r;
  if v_result is null then raise exception 'Keine Vertragsfassung bestätigt.'; end if;
  update public.historical_contract_operation o set registration_request=v_request,registration_result=v_result where o.operation_id=p_operation_id;
 end if;
 return query select r.* from jsonb_to_recordset(v_result) as r(dokument_id bigint,dokumenttyp_id bigint,pachtverhaeltnis_id bigint,pachtvertragsfassung_id bigint,historische_vertragsnummer integer);
end; $$;
revoke all on function public.register_historischen_pachtvertrag(uuid,bigint,text,date,boolean,bigint[],jsonb,bigint,date,bigint[],jsonb,jsonb) from public,anon;
grant execute on function public.register_historischen_pachtvertrag(uuid,bigint,text,date,boolean,bigint[],jsonb,bigint,date,bigint[],jsonb,jsonb) to authenticated;

-- Short backend-only lease serializes PDF writes across Edge instances. A crash is
-- resumable after expiry; the authenticated PDF marker also protects the write/status gap.
create table if not exists public.historical_contract_stamp_lease(document_id bigint primary key references public.dokument(id),token uuid not null,expires_at timestamptz not null,stamp_marker text not null default (gen_random_uuid()::text || gen_random_uuid()::text));
alter table public.historical_contract_stamp_lease enable row level security;
revoke all on public.historical_contract_stamp_lease from public,anon,authenticated;
grant all on public.historical_contract_stamp_lease to service_role;
create or replace function public.claim_historical_contract_stamp(p_document_id bigint,p_token uuid)
returns boolean language plpgsql security definer set search_path=public,pg_temp as $$
begin
 if auth.role() is distinct from 'service_role' then raise exception 'Backend required' using errcode='42501'; end if;
 insert into public.historical_contract_stamp_lease as l(document_id,token,expires_at) values(p_document_id,p_token,now()+interval '5 minutes')
 on conflict(document_id) do update set token=excluded.token,expires_at=excluded.expires_at where l.expires_at<now();
 return found;
end; $$;
create or replace function public.release_historical_contract_stamp(p_document_id bigint,p_token uuid)
returns void language plpgsql security definer set search_path=public,pg_temp as $$
begin
 if auth.role() is distinct from 'service_role' then raise exception 'Backend required' using errcode='42501'; end if;
 update public.historical_contract_stamp_lease l set expires_at='-infinity'::timestamptz where l.document_id=p_document_id and l.token=p_token;
end; $$;
revoke all on function public.claim_historical_contract_stamp(bigint,uuid),public.release_historical_contract_stamp(bigint,uuid) from public,anon,authenticated;
grant execute on function public.claim_historical_contract_stamp(bigint,uuid),public.release_historical_contract_stamp(bigint,uuid) to service_role;

-- Explicit read for resuming an unknown upload result; no table SELECT grant.
create or replace function public.get_historical_contract_operation(p_operation_id uuid)
returns jsonb language plpgsql stable security definer set search_path=public,pg_temp as $$
declare v_op public.historical_contract_operation%rowtype;
begin
 if not coalesce(public.can_register_historical_contracts(),false) then raise exception 'Altvertragsberechtigung fehlt.' using errcode='42501'; end if;
 select o.* into v_op from public.historical_contract_operation o where o.operation_id=p_operation_id;
 if not found then return null; end if;
 if v_op.created_by is distinct from auth.uid() or not public.can_access_historical_contract_member(v_op.mitglied_id) then raise exception 'Vorgang nicht freigegeben.' using errcode='42501'; end if;
 return jsonb_build_object('documentId',v_op.document_id,'registered',v_op.registration_result is not null);
end; $$;
revoke all on function public.get_historical_contract_operation(uuid) from public,anon;
grant execute on function public.get_historical_contract_operation(uuid) to authenticated;

-- A permanent, backend-owned random marker survives signing-key rotation and
-- lease expiry. It is exposed in the PDF only after the first completed write.
create or replace function public.get_historical_contract_stamp_marker(p_document_id bigint)
returns text language plpgsql stable security definer set search_path=public,pg_temp as $$
declare v_marker text;
begin
 if auth.role() is distinct from 'service_role' then raise exception 'Backend required' using errcode='42501'; end if;
 select l.stamp_marker into strict v_marker from public.historical_contract_stamp_lease l where l.document_id=p_document_id;
 return v_marker;
end; $$;
revoke all on function public.get_historical_contract_stamp_marker(bigint) from public,anon,authenticated;
grant execute on function public.get_historical_contract_stamp_marker(bigint) to service_role;
