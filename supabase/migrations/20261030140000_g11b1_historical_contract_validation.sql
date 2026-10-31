-- G11b1 forward migration: replace effective functions without rewriting applied migrations.

create or replace function public.register_historischen_pachtvertrag_g6a(
  p_dokument_id bigint,
  p_dokumenttyp_code text,
  p_vertragsdatum date,
  p_unterzeichnet boolean,
  p_pachtverhaeltnis_ids bigint[],
  p_vorherige_enddaten jsonb default '{}'::jsonb,
  p_mitglied_seit_korrektur_mitglied_id bigint default null,
  p_mitglied_seit_korrektur_datum date default null
)
returns table (
  dokument_id bigint,
  dokumenttyp_id bigint,
  pachtverhaeltnis_id bigint,
  pachtvertragsfassung_id bigint,
  historische_vertragsnummer integer
)
language plpgsql
security definer
set search_path = public, pg_temp
as $$
#variable_conflict error
declare
  v_dokument public.dokument%rowtype;
  v_dokumenttyp_id bigint;
  v_hat_ma_nachweis boolean;
  v_hat_pv_nachweis boolean;
  v_relation_id bigint;
  v_fassung_id bigint;
  v_vorgaenger_id bigint;
  v_nachfolger_id bigint;
  v_vorgaenger_ende date;
  v_nachfolger_beginn date;
  v_nummer integer;
  v_is_demo boolean;
  v_old_mitglied_seit date;
begin
  if not public.can_register_historical_contracts() then
    raise exception 'Für die Nacherfassung historischer Verträge fehlt die Berechtigung.' using errcode = '42501';
  end if;
  if p_vertragsdatum is null or p_unterzeichnet is null then
    raise exception 'Vertragsdatum und Signaturstatus sind erforderlich.' using errcode = '22023';
  end if;
  if coalesce(array_length(p_pachtverhaeltnis_ids, 1), 0) = 0
     or exists (select 1 from unnest(p_pachtverhaeltnis_ids) id group by id having count(*) > 1) then
    raise exception 'Mindestens eine eindeutige Pachtbeziehung ist erforderlich.' using errcode = '22023';
  end if;

  select d.* into v_dokument from public.dokument as d where d.id = p_dokument_id for update;
  if not found or v_dokument.archiviert_at is not null then
    raise exception 'Das Dokument existiert nicht oder ist bereits archiviert.' using errcode = 'P0002';
  end if;
  if v_dokument.source_kommunikationsvorgang_id is not null or v_dokument.source_wartungsvertrag_zuordnung_id is not null then
    raise exception 'Prepared-Dokumente dürfen nicht als historische Verträge registriert werden.' using errcode = '23514';
  end if;
  if v_dokument.mitglied_id is null then
    raise exception 'Ein historischer Vertrag benötigt ein zugeordnetes bestehendes Mitglied.' using errcode = '22023';
  end if;

  select dt.id,
      exists(select 1 from public.dokumenttyp_wirkung w where w.dokumenttyp_id = dt.id and w.wirkung = 'mitgliedsantrag_nachweis'),
      exists(select 1 from public.dokumenttyp_wirkung w where w.dokumenttyp_id = dt.id and w.wirkung = 'pachtvertrag_nachweis')
  into v_dokumenttyp_id, v_hat_ma_nachweis, v_hat_pv_nachweis
  from public.dokumenttyp dt where dt.code = p_dokumenttyp_code and dt.aktiv;
  if not found or p_dokumenttyp_code not in ('historischer_ma_pv_erstvertrag', 'historischer_pv_folgevertrag')
     or not v_hat_pv_nachweis then
    raise exception 'Der Dokumenttyp ist kein zulässiger historischer Pachtvertrag.' using errcode = '22023';
  end if;

  if v_dokument.dokumenttyp_id is not null
     and (v_dokument.dokumenttyp_id is distinct from v_dokumenttyp_id
       or v_dokument.dokumentdatum is distinct from p_vertragsdatum
       or v_dokument.unterzeichnet is distinct from p_unterzeichnet) then
    raise exception 'Das Dokument ist bereits abweichend klassifiziert.' using errcode = '23514';
  end if;

  foreach v_relation_id in array p_pachtverhaeltnis_ids loop
    if not exists (select 1 from public.pachtverhaeltnis as r where r.id = v_relation_id) then
      raise exception 'Unbekanntes Pachtverhältnis: %', v_relation_id using errcode = 'P0002';
    end if;
    if not exists (
      select 1 from public.pachtverhaeltnis_beteiligung b
      where b.pachtverhaeltnis_id = v_relation_id and b.mitglied_id = v_dokument.mitglied_id
        and b.gueltig_ab <= p_vertragsdatum and (b.gueltig_bis is null or b.gueltig_bis >= p_vertragsdatum)
    ) then
      raise exception 'Das Dokumentmitglied war am Vertragsdatum nicht an Pachtverhältnis % beteiligt.', v_relation_id using errcode = '23514';
    end if;
  end loop;

  update public.dokument as d set dokumenttyp_id = v_dokumenttyp_id, dokumentdatum = p_vertragsdatum,
    unterzeichnet = p_unterzeichnet, updated_at = now() where d.id = p_dokument_id;

  foreach v_relation_id in array p_pachtverhaeltnis_ids loop
    select f.id into v_fassung_id from public.pachtvertragsfassung as f
      where f.dokument_id = p_dokument_id and f.pachtverhaeltnis_id = v_relation_id for update;

    if found then
      if not exists (select 1 from public.pachtvertragsfassung f where f.id = v_fassung_id and f.fassungstyp = 'import' and f.wirksam_ab = p_vertragsdatum) then
        raise exception 'Das Dokument ist für Pachtverhältnis % bereits abweichend registriert.', v_relation_id using errcode = '23514';
      end if;
    else
      select f.id into v_vorgaenger_id from public.pachtvertragsfassung as f
        where f.pachtverhaeltnis_id = v_relation_id and f.wirksam_ab < p_vertragsdatum
        order by f.wirksam_ab desc, f.id desc limit 1 for update;
      select f.id, f.wirksam_ab into v_nachfolger_id, v_nachfolger_beginn from public.pachtvertragsfassung as f
        where f.pachtverhaeltnis_id = v_relation_id and f.wirksam_ab > p_vertragsdatum
        order by f.wirksam_ab, f.id limit 1 for update;

      insert into public.pachtvertragsfassung as f(pachtverhaeltnis_id, dokument_id, fassungstyp, vertragsdatum, wirksam_ab, gueltig_bis, vorgaenger_fassung_id)
      values (v_relation_id, p_dokument_id, 'import', p_vertragsdatum, p_vertragsdatum,
        case when v_nachfolger_beginn is null then null else v_nachfolger_beginn - 1 end, v_vorgaenger_id)
      returning f.id into v_fassung_id;

      if v_vorgaenger_id is not null then
        v_vorgaenger_ende := coalesce(nullif(p_vorherige_enddaten ->> v_relation_id::text, '')::date, p_vertragsdatum - 1);
        if v_vorgaenger_ende >= p_vertragsdatum then
          raise exception 'Das Vorgängerende muss vor dem neuen Vertragsbeginn liegen.' using errcode = '23514';
        end if;
        update public.pachtvertragsfassung as f set gueltig_bis = v_vorgaenger_ende where f.id = v_vorgaenger_id;
      end if;
      if v_nachfolger_id is not null then
        update public.pachtvertragsfassung as f set vorgaenger_fassung_id = v_fassung_id where f.id = v_nachfolger_id;
      end if;
    end if;

  end loop;

  -- Uses the established 80xxx reservation source and only broadens its permission gate.
  select coalesce(m.is_demo, false) into v_is_demo from public.mitglied as m where m.id = v_dokument.mitglied_id;
  v_nummer := public.reserve_fachnummer('historischer_kombivertrag', 'historisches_dokument', p_dokument_id, v_is_demo);
  update public.dokument as d set historische_vertragsnummer = v_nummer,
    titel = regexp_replace(coalesce(nullif(btrim(d.titel), ''), 'Historischer Vertrag'), '\s*·\s*Vertrags-Nr\.\s*\d{5}\s*$', '', 'i') || ' · Vertrags-Nr. ' || v_nummer,
    updated_at = now() where d.id = p_dokument_id;

  if p_mitglied_seit_korrektur_mitglied_id is not null or p_mitglied_seit_korrektur_datum is not null then
    if not v_hat_ma_nachweis or p_mitglied_seit_korrektur_mitglied_id is null or p_mitglied_seit_korrektur_datum is null then
      raise exception 'Mitglied-seit-Korrekturen sind nur bewusst beim MA+PV-Erstvertrag zulässig.' using errcode = '22023';
    end if;
    if not exists (select 1 from public.pachtverhaeltnis_beteiligung as b
      where b.pachtverhaeltnis_id = any(p_pachtverhaeltnis_ids)
        and b.mitglied_id = p_mitglied_seit_korrektur_mitglied_id
        and b.gueltig_ab <= p_vertragsdatum and (b.gueltig_bis is null or b.gueltig_bis >= p_vertragsdatum)) then
      raise exception 'Die Mitglied-seit-Korrektur gehört nicht zu den Vertragsparteien.' using errcode = '23514';
    end if;
    select m.mitglied_seit into v_old_mitglied_seit from public.mitglied as m where m.id = p_mitglied_seit_korrektur_mitglied_id for update;
    if not found then raise exception 'Mitglied für die Korrektur nicht gefunden.' using errcode = 'P0002'; end if;
    update public.mitglied as m set mitglied_seit = p_mitglied_seit_korrektur_datum where m.id = p_mitglied_seit_korrektur_mitglied_id;
    if not exists (select 1 from public.mitglied_historische_korrektur as k
      where k.mitglied_id = p_mitglied_seit_korrektur_mitglied_id
        and k.dokument_id = p_dokument_id and k.feld = 'mitglied_seit') then
      insert into public.mitglied_historische_korrektur(mitglied_id, dokument_id, feld, bisheriger_wert, korrigierter_wert, korrigiert_von)
      values (p_mitglied_seit_korrektur_mitglied_id, p_dokument_id, 'mitglied_seit', v_old_mitglied_seit, p_mitglied_seit_korrektur_datum, auth.uid());
    end if;
  end if;

  return query select p_dokument_id, v_dokumenttyp_id, f.pachtverhaeltnis_id, f.id, v_nummer
    from public.pachtvertragsfassung f where f.dokument_id = p_dokument_id order by f.pachtverhaeltnis_id;
end;
$$;
-- G11b1: keep the public RPC signature; validate commands before applying them.
create or replace function public.register_historischen_pachtvertrag(
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
revoke all on function public.register_historischen_pachtvertrag_g6a(bigint, text, date, boolean, bigint[], jsonb, bigint, date) from public, anon, authenticated;
grant execute on function public.register_historischen_pachtvertrag_g6a(bigint, text, date, boolean, bigint[], jsonb, bigint, date) to service_role;
revoke all on function public.register_historischen_pachtvertrag(bigint, text, date, boolean, bigint[], jsonb, bigint, date, bigint[], jsonb, jsonb) from public, anon;
grant execute on function public.register_historischen_pachtvertrag(bigint, text, date, boolean, bigint[], jsonb, bigint, date, bigint[], jsonb, jsonb) to authenticated, service_role;
