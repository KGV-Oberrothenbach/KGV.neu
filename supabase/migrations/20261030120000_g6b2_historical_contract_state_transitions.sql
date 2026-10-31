-- G6b2: completes the G6a transaction with contract parties and explicitly
-- confirmed membership/status consequences. No automatic membership end exists.

do $$
begin
  if to_regprocedure('public.register_historischen_pachtvertrag(bigint,text,date,boolean,bigint[],jsonb,bigint,date)') is not null
     and to_regprocedure('public.register_historischen_pachtvertrag_g6a(bigint,text,date,boolean,bigint[],jsonb,bigint,date)') is null then
    execute 'alter function public.register_historischen_pachtvertrag(bigint, text, date, boolean, bigint[], jsonb, bigint, date) rename to register_historischen_pachtvertrag_g6a';
  end if;
end;
$$;
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
declare
  v_party_id bigint;
  v_relation_id bigint;
  v_end jsonb;
  v_status jsonb;
  v_next_start date;
  v_current record;
begin
  if not public.can_register_historical_contracts() then
    raise exception 'Für die Nacherfassung historischer Verträge fehlt die Berechtigung.' using errcode = '42501';
  end if;
  if coalesce(array_length(p_vertragspartei_mitglied_ids, 1), 0) = 0
     or exists (select 1 from unnest(p_vertragspartei_mitglied_ids) id group by id having count(*) > 1) then
    raise exception 'Mindestens eine vorhandene Vertragspartei ist erforderlich.' using errcode = '22023';
  end if;
  if not exists (select 1 from unnest(p_vertragspartei_mitglied_ids) id where id = (select mitglied_id from public.dokument where id = p_dokument_id)) then
    raise exception 'Das Dokumentmitglied muss Vertragspartei sein.' using errcode = '23514';
  end if;
  if exists (select 1 from unnest(p_vertragspartei_mitglied_ids) id where not exists (select 1 from public.mitglied m where m.id = id)) then
    raise exception 'Eine Vertragspartei ist kein vorhandenes Mitglied.' using errcode = 'P0002';
  end if;

  -- Only the selected parcel relations receive a new party state.
  foreach v_relation_id in array p_pachtverhaeltnis_ids loop
    for v_current in select * from public.pachtverhaeltnis_beteiligung b
      where b.pachtverhaeltnis_id = v_relation_id and b.gueltig_ab <= p_vertragsdatum
        and (b.gueltig_bis is null or b.gueltig_bis >= p_vertragsdatum) for update loop
      if not (v_current.mitglied_id = any(p_vertragspartei_mitglied_ids)) then
        update public.pachtverhaeltnis_beteiligung set gueltig_bis = p_vertragsdatum - 1
          where id = v_current.id and (gueltig_bis is null or gueltig_bis >= p_vertragsdatum);
      end if;
    end loop;
    foreach v_party_id in array p_vertragspartei_mitglied_ids loop
      if not exists (select 1 from public.pachtverhaeltnis_beteiligung b where b.pachtverhaeltnis_id = v_relation_id and b.mitglied_id = v_party_id and b.gueltig_ab <= p_vertragsdatum and (b.gueltig_bis is null or b.gueltig_bis >= p_vertragsdatum)) then
        select min(b.gueltig_ab) into v_next_start from public.pachtverhaeltnis_beteiligung b
          where b.pachtverhaeltnis_id = v_relation_id and b.mitglied_id = v_party_id and b.gueltig_ab > p_vertragsdatum;
        insert into public.pachtverhaeltnis_beteiligung(pachtverhaeltnis_id, mitglied_id, rolle, gueltig_ab, gueltig_bis, created_by)
        values (v_relation_id, v_party_id,
          case when v_party_id = (select mitglied_id from public.dokument where id = p_dokument_id) then 'hauptpaechter' else 'mitpaechter' end,
          p_vertragsdatum, case when v_next_start is null then null else v_next_start - 1 end, auth.uid());
      end if;
    end loop;
  end loop;

  -- The caller must explicitly opt in to each membership end; no party removal implies one.
  for v_end in select value from jsonb_array_elements(coalesce(p_mitgliedschaftsenden, '[]'::jsonb)) loop
    if coalesce((v_end ->> 'mitglied_id')::bigint, 0) = any(p_vertragspartei_mitglied_ids) then
      raise exception 'Eine weiterhin beteiligte Vertragspartei darf nicht als Mitglied beendet werden.' using errcode = '22023';
    end if;
    update public.mitglied set mitglied_ende = (v_end ->> 'mitglied_ende')::date
      where id = (v_end ->> 'mitglied_id')::bigint;
    if not found then raise exception 'Mitgliedschaftsende verweist auf unbekanntes Mitglied.' using errcode = 'P0002'; end if;
  end loop;

  -- Status changes are intentional commands, never inferred from a party change.
  for v_status in select value from jsonb_array_elements(coalesce(p_aktiv_status_aenderungen, '[]'::jsonb)) loop
    update public.mitglied set aktiv = (v_status ->> 'aktiv')::boolean
      where id = (v_status ->> 'mitglied_id')::bigint;
    if not found then raise exception 'Statusänderung verweist auf unbekanntes Mitglied.' using errcode = 'P0002'; end if;
  end loop;

  return query select * from public.register_historischen_pachtvertrag_g6a(
    p_dokument_id, p_dokumenttyp_code, p_vertragsdatum, p_unterzeichnet, p_pachtverhaeltnis_ids,
    p_vorherige_enddaten, p_mitglied_seit_korrektur_mitglied_id, p_mitglied_seit_korrektur_datum);
end;
$$;
revoke all on function public.register_historischen_pachtvertrag_g6a(bigint, text, date, boolean, bigint[], jsonb, bigint, date) from public, anon, authenticated;
grant execute on function public.register_historischen_pachtvertrag_g6a(bigint, text, date, boolean, bigint[], jsonb, bigint, date) to service_role;
revoke all on function public.register_historischen_pachtvertrag(bigint, text, date, boolean, bigint[], jsonb, bigint, date, bigint[], jsonb, jsonb) from public, anon;
grant execute on function public.register_historischen_pachtvertrag(bigint, text, date, boolean, bigint[], jsonb, bigint, date, bigint[], jsonb, jsonb) to authenticated, service_role;
comment on function public.register_historischen_pachtvertrag(bigint, text, date, boolean, bigint[], jsonb, bigint, date, bigint[], jsonb, jsonb) is
  'G6b2 historical contract registration: atomic party timeline, explicit membership ends and confirmed activity state changes.';
