-- G11b2: capability-scoped historical uploads and reads, no general document write grant.
alter table public.dokument add column if not exists historical_contract_type text
  check (historical_contract_type is null or historical_contract_type in ('historischer_ma_pv_erstvertrag','historischer_pv_folgevertrag'));
create or replace function public.guard_historical_contract_upload_context()
returns trigger language plpgsql security definer set search_path = public, pg_temp as $$
begin
  if tg_op = 'INSERT' then
    if new.historical_contract_type is not null and auth.role() is distinct from 'service_role' then
      raise exception 'Historischer Uploadkontext darf nur durch den geschützten Uploaddienst gesetzt werden.' using errcode = '42501';
    end if;
  elsif (new.historical_contract_type is distinct from old.historical_contract_type
      or (old.historical_contract_type is not null and
        (new.created_by is distinct from old.created_by or new.mitglied_id is distinct from old.mitglied_id
         or new.parzelle_id is distinct from old.parzelle_id or new.drive_file_id is distinct from old.drive_file_id
         or new.source_kommunikationsvorgang_id is distinct from old.source_kommunikationsvorgang_id
         or new.source_wartungsvertrag_zuordnung_id is distinct from old.source_wartungsvertrag_zuordnung_id)))
      and auth.role() is distinct from 'service_role' then
    raise exception 'Historischer Uploadkontext ist nicht durch den Client änderbar.' using errcode = '42501';
  end if;
  if new.historical_contract_type is not null and (new.mitglied_id is null or new.parzelle_id is not null
      or new.created_by is null or new.source_kommunikationsvorgang_id is not null
      or new.source_wartungsvertrag_zuordnung_id is not null) then
    raise exception 'Historischer Upload benötigt Mitglied, Urheber und einen eigenen Vertragskontext.' using errcode = '23514';
  end if;
  if new.historical_contract_type is not null and new.dokumenttyp_id is not null
     and not exists (select 1 from public.dokumenttyp as dt
       where dt.id = new.dokumenttyp_id and dt.code = new.historical_contract_type) then
    raise exception 'Dokumenttyp und historischer Uploadkontext stimmen nicht überein.' using errcode = '23514';
  end if;
  -- A caller must not turn an arbitrary foreign ordinary document into a
  -- readable historical document merely by invoking the registration RPC.
  if tg_op = 'UPDATE' and auth.role() is distinct from 'service_role'
     and exists (select 1 from public.dokumenttyp as dt where dt.id = new.dokumenttyp_id
       and dt.code in ('historischer_ma_pv_erstvertrag','historischer_pv_folgevertrag'))
     and not (coalesce(old.historical_contract_type = new.historical_contract_type
                       and old.historical_contract_type is not null and old.created_by = auth.uid(), false)
       or (public.is_registered_historical_contract_document(old.id) and old.dokumenttyp_id = new.dokumenttyp_id)) then
    raise exception 'Die Registrierung benötigt einen eigenen geschützten historischen Upload oder einen bestehenden historischen Vertrag.' using errcode = '42501';
  end if;
  return new;
end;
$$;
drop trigger if exists guard_historical_contract_upload_context on public.dokument;
create trigger guard_historical_contract_upload_context before insert or update on public.dokument
  for each row execute function public.guard_historical_contract_upload_context();
revoke all on function public.guard_historical_contract_upload_context() from public, anon, authenticated;
create or replace function public.can_access_historical_contract_member(p_mitglied_id bigint)
returns boolean language sql stable security definer set search_path = public, pg_temp as $$
  select coalesce(public.can_register_historical_contracts(), false) and exists (
    select 1 from public.mitglied as m cross join public.current_app_user_context() as c
    where m.id = p_mitglied_id and coalesce(m.is_demo, false) = c.is_demo_account
  );
$$;
create or replace function public.is_registered_historical_contract_document(p_dokument_id bigint)
returns boolean language sql stable security definer set search_path = public, pg_temp as $$
  select exists (select 1 from public.dokument as d join public.dokumenttyp as dt on dt.id = d.dokumenttyp_id
    where d.id = p_dokument_id and d.archiviert_at is null
      and d.source_kommunikationsvorgang_id is null and d.source_wartungsvertrag_zuordnung_id is null
      and dt.code in ('historischer_ma_pv_erstvertrag','historischer_pv_folgevertrag')
      and (d.historical_contract_type is null or d.historical_contract_type = dt.code)
      and exists (select 1 from public.pachtvertragsfassung as f
        where f.dokument_id = d.id and f.fassungstyp = 'import' and f.wirksam_ab = d.dokumentdatum));
$$;
revoke all on function public.is_registered_historical_contract_document(bigint) from public, anon, authenticated;
grant execute on function public.is_registered_historical_contract_document(bigint) to service_role;
create or replace function public.can_access_historical_contract_document(p_dokument_id bigint, p_require_registered boolean default false)
returns boolean language sql stable security definer set search_path = public, pg_temp as $$
  select coalesce(public.can_register_historical_contracts(), false) and exists (
    select 1 from public.dokument as d
    where d.id = p_dokument_id and d.archiviert_at is null
      and d.source_kommunikationsvorgang_id is null and d.source_wartungsvertrag_zuordnung_id is null
      and public.can_access_historical_contract_member(d.mitglied_id)
      and (
        (public.is_registered_historical_contract_document(d.id)
          and not exists (select 1 from public.pachtvertragsfassung as f join public.pachtverhaeltnis as r on r.id = f.pachtverhaeltnis_id
            join public.parzelle as p on p.id = r.parzelle_id
            where f.dokument_id = d.id and (not exists (select 1 from public.current_app_user_context() as c where coalesce(p.is_demo, false) = c.is_demo_account)
              or not exists (select 1 from public.pachtverhaeltnis_beteiligung as b where b.pachtverhaeltnis_id = f.pachtverhaeltnis_id
                and b.gueltig_ab <= f.wirksam_ab and (b.gueltig_bis is null or b.gueltig_bis >= f.wirksam_ab))
              or exists (select 1 from public.pachtverhaeltnis_beteiligung as b where b.pachtverhaeltnis_id = f.pachtverhaeltnis_id
                and b.gueltig_ab <= f.wirksam_ab and (b.gueltig_bis is null or b.gueltig_bis >= f.wirksam_ab)
                and not public.can_access_historical_contract_member(b.mitglied_id)))))
        or (not p_require_registered and d.dokumenttyp_id is null
          and d.historical_contract_type in ('historischer_ma_pv_erstvertrag','historischer_pv_folgevertrag')
          and d.created_by = auth.uid())
      )
  );
$$;
revoke all on function public.can_access_historical_contract_member(bigint) from public, anon;
grant execute on function public.can_access_historical_contract_member(bigint) to authenticated;
revoke all on function public.can_access_historical_contract_document(bigint, boolean) from public, anon;
grant execute on function public.can_access_historical_contract_document(bigint, boolean) to authenticated;
drop policy if exists dokument_historical_contract_select on public.dokument;
create policy dokument_historical_contract_select on public.dokument for select to authenticated
  using (public.can_access_historical_contract_document(id, false));
-- Minimal lookup projections for the workflow; no general member/parcel SELECT grants.
create or replace function public.get_historical_contract_members()
returns setof jsonb language plpgsql stable security definer set search_path = public, pg_temp as $$
begin
  if not coalesce(public.can_register_historical_contracts(), false) then
    raise exception 'Altvertragsberechtigung fehlt.' using errcode = '42501';
  end if;
  return query select jsonb_build_object('id',m.id,'mitgliedsnummer',m.mitgliedsnummer,'vorname',m.vorname,'name',m.name,'mitglied_seit',m.mitglied_seit,'aktiv',m.aktiv)
    from public.mitglied as m cross join public.current_app_user_context() as c
    where coalesce(m.is_demo, false) = c.is_demo_account order by m.name, m.vorname, m.id;
end;
$$;
create or replace function public.get_historical_contract_lease_relationships(p_mitglied_id bigint)
returns setof jsonb language plpgsql stable security definer set search_path = public, pg_temp as $$
begin
  if not public.can_access_historical_contract_member(p_mitglied_id) then
    raise exception 'Die Pachtbeziehungen liegen außerhalb des historischen Vertragsbereichs.' using errcode = '42501';
  end if;
  return query select jsonb_build_object('pachtverhaeltnisId',r.id,'parzelleId',r.parzelle_id,'gartenNr',p.garten_nr,'anlage',p."Anlage",'rolle',b.rolle,
      'gueltigAb',b.gueltig_ab,'gueltigBis',b.gueltig_bis,'urspruenglicherPachtbeginn',r.urspruenglicher_pachtbeginn,'beendetAm',r.beendet_am)
    from public.pachtverhaeltnis_beteiligung as b join public.pachtverhaeltnis as r on r.id = b.pachtverhaeltnis_id
      join public.parzelle as p on p.id = r.parzelle_id
    where b.mitglied_id = p_mitglied_id and coalesce(p.is_demo, false) = (select c.is_demo_account from public.current_app_user_context() as c)
    order by b.gueltig_ab desc, r.id;
end;
$$;
revoke all on function public.get_historical_contract_members() from public, anon;
grant execute on function public.get_historical_contract_members() to authenticated;
revoke all on function public.get_historical_contract_lease_relationships(bigint) from public, anon;
grant execute on function public.get_historical_contract_lease_relationships(bigint) to authenticated;
-- Retain the G7 projection, adding only the historical data scope check.
-- G7 shared read model. It is capability-gated just like the write RPC.
create or replace function public.get_historische_pachtvertragsvorschau(p_pachtverhaeltnis_ids bigint[], p_vertragsdatum date)
returns table(pachtverhaeltnis_id bigint, pachtvertragsfassung_id bigint, wirksam_ab date, gueltig_bis date, historische_vertragsnummer integer, vertragspartei_mitglied_ids bigint[], ist_vorgaenger boolean, ist_nachfolger boolean, vorgeschlagenes_vorgaengerende date)
language plpgsql stable security definer set search_path = public, pg_temp as $$
begin
  if not public.can_register_historical_contracts() then
    raise exception 'Für die Nacherfassung historischer Verträge fehlt die Berechtigung.' using errcode = '42501';
  end if;
  if exists (select 1 from public.pachtverhaeltnis as r
      where r.id = any(p_pachtverhaeltnis_ids) and
        (not exists (select 1 from public.pachtverhaeltnis_beteiligung as b where b.pachtverhaeltnis_id = r.id)
         or exists (select 1 from public.pachtverhaeltnis_beteiligung as b where b.pachtverhaeltnis_id = r.id
           and not public.can_access_historical_contract_member(b.mitglied_id))))
     or exists (select 1 from public.pachtverhaeltnis as r join public.parzelle as p on p.id = r.parzelle_id
       where r.id = any(p_pachtverhaeltnis_ids) and not exists (select 1 from public.current_app_user_context() as c where coalesce(p.is_demo, false) = c.is_demo_account))
     or exists (select 1 from unnest(p_pachtverhaeltnis_ids) as ids(id)
       where not exists (select 1 from public.pachtverhaeltnis as r where r.id = ids.id)) then
    raise exception 'Die Vorschau liegt außerhalb des historischen Vertragsbereichs.' using errcode = '42501';
  end if;
  return query
  with versions as (
    select f.*, d.historische_vertragsnummer
    from public.pachtvertragsfassung f join public.dokument d on d.id = f.dokument_id
    where f.pachtverhaeltnis_id = any(p_pachtverhaeltnis_ids)
  )
  select v.pachtverhaeltnis_id, v.id, v.wirksam_ab, v.gueltig_bis, v.historische_vertragsnummer,
    coalesce(array(select b.mitglied_id from public.pachtverhaeltnis_beteiligung b where b.pachtverhaeltnis_id = v.pachtverhaeltnis_id and b.gueltig_ab <= v.wirksam_ab and (b.gueltig_bis is null or b.gueltig_bis >= v.wirksam_ab) order by b.mitglied_id), '{}'::bigint[]),
    v.wirksam_ab = (select max(x.wirksam_ab) from public.pachtvertragsfassung x where x.pachtverhaeltnis_id = v.pachtverhaeltnis_id and x.wirksam_ab < p_vertragsdatum),
    v.wirksam_ab = (select min(x.wirksam_ab) from public.pachtvertragsfassung x where x.pachtverhaeltnis_id = v.pachtverhaeltnis_id and x.wirksam_ab > p_vertragsdatum),
    case when v.wirksam_ab = (select max(x.wirksam_ab) from public.pachtvertragsfassung x where x.pachtverhaeltnis_id = v.pachtverhaeltnis_id and x.wirksam_ab < p_vertragsdatum) then p_vertragsdatum - 1 end
  from versions v order by v.pachtverhaeltnis_id, v.wirksam_ab, v.id;
end;
$$;
revoke all on function public.get_historische_pachtvertragsvorschau(bigint[], date) from public, anon;
grant execute on function public.get_historische_pachtvertragsvorschau(bigint[], date) to authenticated, service_role;
-- Keep number/stamp RPCs under the same context boundary, including direct calls.
create or replace function public.reserve_fachnummer(p_art text, p_quelle_art text, p_quelle_id bigint, p_is_demo boolean)
returns integer language plpgsql security definer set search_path = public, pg_temp as $$
declare v_nummer integer; v_bestehende_art text; v_sequenz regclass;
begin
  if p_art = 'historischer_kombivertrag' and
     (p_quelle_art is distinct from 'historisches_dokument'
      or p_is_demo is distinct from (select coalesce(m.is_demo, false) from public.dokument as d join public.mitglied as m on m.id = d.mitglied_id where d.id = p_quelle_id)
      or not public.is_registered_historical_contract_document(p_quelle_id)
      or (auth.role() is distinct from 'service_role' and not public.can_access_historical_contract_document(p_quelle_id, true))) then
    raise exception 'Historische Nummern benötigen einen berechtigten registrierten Vertragskontext.' using errcode = '42501';
  end if;
  if not public.is_admin_or_vorstand() and auth.role() <> 'service_role'
     and not (p_art = 'historischer_kombivertrag' and public.can_register_historical_contracts()) then
    raise exception 'Keine Berechtigung für Nummernvergabe' using errcode = '42501';
  end if;
  perform pg_advisory_xact_lock(hashtextextended(concat_ws(':', p_is_demo::text, p_quelle_art, p_quelle_id::text), 0));
  select nummer, art into v_nummer, v_bestehende_art from public.fachnummer_reservierung where is_demo = p_is_demo and quelle_art = p_quelle_art and quelle_id = p_quelle_id;
  if found then
    if v_bestehende_art <> p_art then raise exception 'Die Quelle besitzt bereits eine Reservierung der Art % und kann nicht als % verwendet werden.', v_bestehende_art, p_art using errcode = '22023'; end if;
    return v_nummer;
  end if;
  v_sequenz := case when p_is_demo then case p_art when 'mitgliedsantrag' then 'public.fachnummer_mitgliedsantrag_demo_seq'::regclass when 'pachtvertrag' then 'public.fachnummer_pachtvertrag_demo_seq'::regclass when 'historischer_kombivertrag' then 'public.fachnummer_historisch_demo_seq'::regclass when 'wartungsvertrag' then 'public.fachnummer_wartungsvertrag_demo_seq'::regclass end else case p_art when 'mitgliedsantrag' then 'public.fachnummer_mitgliedsantrag_seq'::regclass when 'pachtvertrag' then 'public.fachnummer_pachtvertrag_seq'::regclass when 'historischer_kombivertrag' then 'public.fachnummer_historisch_seq'::regclass when 'wartungsvertrag' then 'public.fachnummer_wartungsvertrag_seq'::regclass end end;
  if v_sequenz is null then raise exception 'Unbekannte Nummernart' using errcode = '22023'; end if;
  v_nummer := nextval(v_sequenz);
  insert into public.fachnummer_reservierung(is_demo, nummer, art, quelle_art, quelle_id) values (p_is_demo, v_nummer, p_art, p_quelle_art, p_quelle_id);
  return v_nummer;
end;
$$;
-- Keep number/stamp RPCs under the same context boundary, including direct calls.
create or replace function public.reserve_historische_vertragsnummer(p_dokument_id bigint)
returns table (nummer integer, titel text, mime_type text, pdf_bereits_gestempelt boolean)
language plpgsql security definer set search_path = public, pg_temp as $$
declare v_dokument public.dokument%rowtype; v_is_demo boolean; v_nummer integer; v_titel text;
begin
  if not public.is_registered_historical_contract_document(p_dokument_id)
     or (auth.role() is distinct from 'service_role' and not public.can_access_historical_contract_document(p_dokument_id, true)) then
    raise exception 'Nummer und Stempelstatus benötigen einen berechtigten registrierten Vertragskontext.' using errcode = '42501';
  end if;
  if not public.can_register_historical_contracts() then
    raise exception 'Für die Nacherfassung historischer Verträge fehlt die Berechtigung.' using errcode = '42501';
  end if;
  select * into v_dokument from public.dokument where id = p_dokument_id for update;
  if not found or v_dokument.archiviert_at is not null then raise exception 'Das historische Dokument existiert nicht oder ist archiviert.' using errcode = 'P0002'; end if;
  if v_dokument.mitglied_id is null then raise exception 'Ohne ausgewähltes Mitglied darf keine historische Vertragsnummer reserviert werden.' using errcode = '22023'; end if;
  select coalesce(is_demo, false) into v_is_demo from public.mitglied where id = v_dokument.mitglied_id;
  if not found then raise exception 'Das ausgewählte Mitglied existiert nicht.' using errcode = 'P0002'; end if;
  v_nummer := public.reserve_fachnummer('historischer_kombivertrag', 'historisches_dokument', p_dokument_id, v_is_demo);
  v_titel := regexp_replace(coalesce(nullif(btrim(v_dokument.titel), ''), 'Historischer Vertrag'), '\s*·\s*Vertrags-Nr\.\s*\d{5}\s*$', '', 'i') || ' · Vertrags-Nr. ' || v_nummer;
  update public.dokument set historische_vertragsnummer = v_nummer, titel = v_titel, updated_at = now() where id = p_dokument_id;
  return query select v_nummer, v_titel, v_dokument.mime_type, v_dokument.historische_vertragsnummer_pdf_gestempelt_am is not null;
end;
$$;
-- Keep number/stamp RPCs under the same context boundary, including direct calls.
create or replace function public.mark_historische_vertragsnummer_pdf_gestempelt(p_dokument_id bigint, p_nummer integer)
returns void
language plpgsql security definer set search_path = public, pg_temp as $$
begin
  if not public.is_registered_historical_contract_document(p_dokument_id)
     or (auth.role() is distinct from 'service_role' and not public.can_access_historical_contract_document(p_dokument_id, true)) then
    raise exception 'Nummer und Stempelstatus benötigen einen berechtigten registrierten Vertragskontext.' using errcode = '42501';
  end if;
  if not public.can_register_historical_contracts() then
    raise exception 'Für die Nacherfassung historischer Verträge fehlt die Berechtigung.' using errcode = '42501';
  end if;
  update public.dokument set historische_vertragsnummer_pdf_gestempelt_am = coalesce(historische_vertragsnummer_pdf_gestempelt_am, now()), updated_at = now()
  where id = p_dokument_id and historische_vertragsnummer = p_nummer;
  if not found then raise exception 'Die historische Vertragsnummer passt nicht zum Dokument.' using errcode = '22023'; end if;
end;
$$;
-- Only the backend that wrote the stamped bytes may confirm that outcome.
revoke all on function public.mark_historische_vertragsnummer_pdf_gestempelt(bigint, integer) from public, anon, authenticated;
grant execute on function public.mark_historische_vertragsnummer_pdf_gestempelt(bigint, integer) to service_role;
create or replace function public.get_historical_contract_documents(p_mitglied_id bigint)
returns setof jsonb language plpgsql stable security definer set search_path = public, pg_temp as $$
begin
  if not public.can_access_historical_contract_member(p_mitglied_id) then
    raise exception 'Das Dokumentarchiv liegt außerhalb des historischen Vertragsbereichs.' using errcode = '42501';
  end if;
  return query select jsonb_build_object('documentId',d.id,'title',d.titel,'number',d.historische_vertragsnummer,'date',d.dokumentdatum,'mimeType',d.mime_type)
    from public.dokument as d
    where public.can_access_historical_contract_document(d.id, true)
      and (d.mitglied_id = p_mitglied_id or exists (
        select 1 from public.pachtvertragsfassung as f join public.pachtverhaeltnis_beteiligung as b on b.pachtverhaeltnis_id = f.pachtverhaeltnis_id
        where f.dokument_id = d.id and b.mitglied_id = p_mitglied_id and b.gueltig_ab <= f.wirksam_ab
          and (b.gueltig_bis is null or b.gueltig_bis >= f.wirksam_ab)))
    order by d.dokumentdatum desc, d.id desc;
end;
$$;
revoke all on function public.get_historical_contract_documents(bigint) from public, anon;
grant execute on function public.get_historical_contract_documents(bigint) to authenticated;
