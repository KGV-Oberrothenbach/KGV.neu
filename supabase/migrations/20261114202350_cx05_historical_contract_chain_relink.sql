-- CX-05: inserting a historical version between two existing versions must
-- temporarily release the predecessor's unique successor edge before the new
-- version is inserted. The transaction keeps the chain atomic to observers.
create or replace function public.guard_pachtvertragsfassung_historie()
returns trigger
language plpgsql
security definer
set search_path = public, pg_temp
as $$
begin
  if tg_op = 'UPDATE'
     and current_setting('kgv.historical_contract_chain_relink', true) = 'on'
     and coalesce(public.can_register_historical_contracts(), false)
     and (
       (to_jsonb(new) - 'vorgaenger_fassung_id') = (to_jsonb(old) - 'vorgaenger_fassung_id')
       or (to_jsonb(new) - 'gueltig_bis') = (to_jsonb(old) - 'gueltig_bis')
     ) then
    return new;
  end if;
  if tg_op = 'UPDATE'
     and old.nicht_mehr_massgeblich_am is null
     and new.nicht_mehr_massgeblich_am is not null
     and (to_jsonb(new) - 'nicht_mehr_massgeblich_am') = (to_jsonb(old) - 'nicht_mehr_massgeblich_am')
     and exists (
       select 1 from public.pachtvertragsfassung successor
       where successor.vorgaenger_fassung_id = old.id
     ) then
    return new;
  end if;
  raise exception 'Gespeicherte Vertragsfassungen sind unveränderliche historische Originale.' using errcode = '55000';
end;
$$;

revoke all on function public.guard_pachtvertragsfassung_historie() from public, anon, authenticated;

create or replace function public.register_historischen_pachtvertrag_g6a(
  p_dokument_id bigint,
  p_dokumenttyp_code text,
  p_vertragsdatum date,
  p_unterzeichnet boolean,
  p_pachtverhaeltnis_ids bigint[],
  p_vorherige_enddaten jsonb default '{}'::jsonb,
  p_mitglied_seit_korrektur_mitglied_id bigint default null,
  p_mitglied_seit_korrektur_datum date default null)
returns table(
  dokument_id bigint,
  dokumenttyp_id bigint,
  pachtverhaeltnis_id bigint,
  pachtvertragsfassung_id bigint,
  historische_vertragsnummer integer)
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
      v_vorgaenger_id := null;
      v_nachfolger_id := null;
      v_nachfolger_beginn := null;
      select f.id into v_vorgaenger_id from public.pachtvertragsfassung as f
        where f.pachtverhaeltnis_id = v_relation_id and f.wirksam_ab < p_vertragsdatum
        order by f.wirksam_ab desc, f.id desc limit 1 for update;
      select f.id, f.wirksam_ab into v_nachfolger_id, v_nachfolger_beginn from public.pachtvertragsfassung as f
        where f.pachtverhaeltnis_id = v_relation_id and f.wirksam_ab > p_vertragsdatum
        order by f.wirksam_ab, f.id limit 1 for update;

      if v_vorgaenger_id is not null then
        v_vorgaenger_ende := coalesce(nullif(p_vorherige_enddaten ->> v_relation_id::text, '')::date, p_vertragsdatum - 1);
        if v_vorgaenger_ende >= p_vertragsdatum then
          raise exception 'Das Vorgängerende muss vor dem neuen Vertragsbeginn liegen.' using errcode = '23514';
        end if;
      end if;

      perform set_config('kgv.historical_contract_chain_relink', 'on', true);
      if v_nachfolger_id is not null then
        update public.pachtvertragsfassung as f set vorgaenger_fassung_id = null where f.id = v_nachfolger_id;
      end if;

      insert into public.pachtvertragsfassung as f(pachtverhaeltnis_id, dokument_id, fassungstyp, vertragsdatum, wirksam_ab, gueltig_bis, vorgaenger_fassung_id)
      values (v_relation_id, p_dokument_id, 'import', p_vertragsdatum, p_vertragsdatum,
        case when v_nachfolger_beginn is null then null else v_nachfolger_beginn - 1 end, v_vorgaenger_id)
      returning f.id into v_fassung_id;

      if v_vorgaenger_id is not null then
        update public.pachtvertragsfassung as f set gueltig_bis = v_vorgaenger_ende where f.id = v_vorgaenger_id;
      end if;
      if v_nachfolger_id is not null then
        update public.pachtvertragsfassung as f set vorgaenger_fassung_id = v_fassung_id where f.id = v_nachfolger_id;
      end if;
      perform set_config('kgv.historical_contract_chain_relink', 'off', true);
    end if;
  end loop;

  v_nummer := public.reserve_fachnummer('historischer_kombivertrag', 'historisches_dokument', p_dokument_id);
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

revoke all on function public.register_historischen_pachtvertrag_g6a(bigint,text,date,boolean,bigint[],jsonb,bigint,date)
  from public, anon, authenticated;
grant execute on function public.register_historischen_pachtvertrag_g6a(bigint,text,date,boolean,bigint[],jsonb,bigint,date)
  to service_role;

-- G11b1 previously created missing party periods before G6a checked the
-- document member's period at the requested contract date. Preserve the
-- established implementation as a private core and validate the unchanged
-- timeline before any party mutation can make the check pass retroactively.
do $$
begin
  if to_regprocedure('public.register_historischen_pachtvertrag_g11b1_cx05_base(bigint,text,date,boolean,bigint[],jsonb,bigint,date,bigint[],jsonb,jsonb)') is null then
    alter function public.register_historischen_pachtvertrag_g11b1(bigint,text,date,boolean,bigint[],jsonb,bigint,date,bigint[],jsonb,jsonb)
      rename to register_historischen_pachtvertrag_g11b1_cx05_base;
  end if;
end;
$$;

revoke all on function public.register_historischen_pachtvertrag_g11b1_cx05_base(bigint,text,date,boolean,bigint[],jsonb,bigint,date,bigint[],jsonb,jsonb)
  from public, anon, authenticated, service_role;

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
  p_aktiv_status_aenderungen jsonb default '[]'::jsonb)
returns table(
  dokument_id bigint,
  dokumenttyp_id bigint,
  pachtverhaeltnis_id bigint,
  pachtvertragsfassung_id bigint,
  historische_vertragsnummer integer)
language plpgsql
security definer
set search_path = public, pg_temp
as $$
declare
  v_dokument_mitglied_id bigint;
  v_relation_id bigint;
begin
  if p_vertragsdatum is null or not isfinite(p_vertragsdatum) then
    raise exception 'Ein endliches Vertragsdatum ist erforderlich.' using errcode = '22023';
  end if;
  if coalesce(cardinality(p_pachtverhaeltnis_ids), 0) = 0
     or exists (select 1 from unnest(p_pachtverhaeltnis_ids) ids(id) where ids.id is null or ids.id <= 0)
     or exists (select 1 from unnest(p_pachtverhaeltnis_ids) ids(id) group by ids.id having count(*) > 1) then
    raise exception 'Eindeutige Pachtbeziehungen sind erforderlich.' using errcode = '22023';
  end if;

  select d.mitglied_id into v_dokument_mitglied_id
  from public.dokument d
  where d.id = p_dokument_id
  for update;

  if not found or v_dokument_mitglied_id is null then
    raise exception 'Das Vertragsdokument benötigt ein vorhandenes Mitglied.' using errcode = 'P0002';
  end if;

  foreach v_relation_id in array p_pachtverhaeltnis_ids loop
    if not exists (
      select 1
      from public.pachtverhaeltnis_beteiligung b
      where b.pachtverhaeltnis_id = v_relation_id
        and b.mitglied_id = v_dokument_mitglied_id
        and b.gueltig_ab <= p_vertragsdatum
        and (b.gueltig_bis is null or b.gueltig_bis >= p_vertragsdatum)
    ) then
      raise exception 'Das Dokumentmitglied war am Vertragsdatum nicht an Pachtverhältnis % beteiligt.', v_relation_id
        using errcode = '23514';
    end if;
  end loop;

  return query
  select result.*
  from public.register_historischen_pachtvertrag_g11b1_cx05_base(
    p_dokument_id,
    p_dokumenttyp_code,
    p_vertragsdatum,
    p_unterzeichnet,
    p_pachtverhaeltnis_ids,
    p_vorherige_enddaten,
    p_mitglied_seit_korrektur_mitglied_id,
    p_mitglied_seit_korrektur_datum,
    p_vertragspartei_mitglied_ids,
    p_mitgliedschaftsenden,
    p_aktiv_status_aenderungen
  ) result;
end;
$$;

revoke all on function public.register_historischen_pachtvertrag_g11b1(bigint,text,date,boolean,bigint[],jsonb,bigint,date,bigint[],jsonb,jsonb)
  from public, anon, authenticated;
grant execute on function public.register_historischen_pachtvertrag_g11b1(bigint,text,date,boolean,bigint[],jsonb,bigint,date,bigint[],jsonb,jsonb)
  to service_role;
