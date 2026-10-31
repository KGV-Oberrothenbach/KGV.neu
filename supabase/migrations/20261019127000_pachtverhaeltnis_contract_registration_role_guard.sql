-- PV-DB1e: auth.role() may be NULL in SQL-editor sessions; reject it explicitly.

create or replace function public.register_pachtverhaeltnis_erstvertrag(
  p_parzelle_id bigint, p_hauptpaechter_mitglied_id bigint, p_urspruenglicher_pachtbeginn date,
  p_dokument_id bigint, p_vertragsdatum date, p_wirksam_ab date, p_created_by uuid,
  p_mitpaechter_mitglied_id bigint default null
)
returns table(pachtverhaeltnis_id bigint, pachtvertragsfassung_id bigint)
language plpgsql security definer set search_path = public, pg_temp
as $$
declare
  v_hauptpaechter public.mitglied%rowtype;
  v_mitpaechter public.mitglied%rowtype;
  v_dokument public.dokument%rowtype;
  v_pachtverhaeltnis_id bigint;
  v_pachtvertragsfassung_id bigint;
begin
  if auth.role() is distinct from 'service_role' then
    raise exception 'Die Vertragsregistrierung ist nur serverseitig zulässig' using errcode = '42501';
  end if;
  if p_parzelle_id is null or p_hauptpaechter_mitglied_id is null
     or p_urspruenglicher_pachtbeginn is null or p_dokument_id is null
     or p_vertragsdatum is null or p_wirksam_ab is null or p_created_by is null then
    raise exception 'Parzelle, Hauptpächter, Pachtbeginn, Dokument, Vertragsdaten und auslösender Benutzer sind erforderlich' using errcode = '22023';
  end if;
  if p_wirksam_ab <> p_urspruenglicher_pachtbeginn then
    raise exception 'Beim Erstvertrag muss wirksam_ab dem ursprünglichen Pachtbeginn entsprechen' using errcode = '23514';
  end if;
  if not exists (
    select 1 from public.app_user au
    join public.mitglied m on m.id = au.mitglied_id and m.auth_user_id = au.user_id
    where au.user_id = p_created_by and au.mitglied_id is not null and au.mitglied_id > 0
      and au.role in ('admin', 'vorstand')
  ) then
    raise exception 'Der auslösende Benutzer ist nicht als Admin oder Vorstand berechtigt' using errcode = '42501';
  end if;
  select * into v_hauptpaechter from public.mitglied where id = p_hauptpaechter_mitglied_id;
  if not found then raise exception 'Unbekannter Hauptpächter: %', p_hauptpaechter_mitglied_id using errcode = 'P0002'; end if;
  if p_mitpaechter_mitglied_id is not null then
    if p_mitpaechter_mitglied_id = p_hauptpaechter_mitglied_id then
      raise exception 'Hauptpächter und Mitpächter müssen verschieden sein' using errcode = '23514';
    end if;
    select * into v_mitpaechter from public.mitglied where id = p_mitpaechter_mitglied_id;
    if not found then raise exception 'Unbekannter Mitpächter: %', p_mitpaechter_mitglied_id using errcode = 'P0002'; end if;
    if v_hauptpaechter.geburtsdatum is not null
       and (v_hauptpaechter.geburtsdatum + interval '18 years')::date > p_urspruenglicher_pachtbeginn then
      raise exception 'Minderjährige Hauptpächter dürfen keinen Mitpächter haben' using errcode = '23514';
    end if;
  end if;
  perform 1 from public.parzelle where id = p_parzelle_id for key share;
  if not found then raise exception 'Unbekannte Parzelle: %', p_parzelle_id using errcode = 'P0002'; end if;
  perform pg_advisory_xact_lock(hashtextextended('pachtverhaeltnis:parzelle:' || p_parzelle_id::text, 0));
  if exists (select 1 from public.pachtverhaeltnis where parzelle_id = p_parzelle_id and beendet_am is null for update) then
    raise exception 'Für die Parzelle besteht bereits ein aktives Pachtverhältnis' using errcode = '23505';
  end if;
  select * into v_dokument from public.dokument where id = p_dokument_id and archiviert_at is null for update;
  if not found then raise exception 'Unbekanntes oder archiviertes Vertragsdokument: %', p_dokument_id using errcode = 'P0002'; end if;
  if v_dokument.parzelle_id is distinct from p_parzelle_id then
    raise exception 'Das Vertragsdokument gehört nicht zur angegebenen Parzelle' using errcode = '23514';
  end if;
  if v_dokument.mitglied_id is not null and v_dokument.mitglied_id is distinct from p_hauptpaechter_mitglied_id then
    raise exception 'Das Vertragsdokument gehört nicht zum Hauptpächter' using errcode = '23514';
  end if;
  if exists (select 1 from public.pachtvertragsfassung where dokument_id = p_dokument_id) then
    raise exception 'Das Vertragsdokument ist bereits einer Vertragsfassung zugeordnet' using errcode = '23505';
  end if;
  if not exists (
    select 1 from public.kommunikationsvorgang kv
    where kv.final_dokument_id = p_dokument_id and kv.vorgangstyp = 'vertrag_signatur'
      and kv.dokumenttyp = 'pachtvertrag' and kv.parzelle_id = p_parzelle_id
      and kv.mitglied_id = p_hauptpaechter_mitglied_id and kv.status in ('letzte_pruefung', 'erledigt')
  ) then
    raise exception 'Das Dokument ist kein finales Pachtvertragsdokument des angegebenen Vertragsvorgangs' using errcode = '23514';
  end if;
  insert into public.pachtverhaeltnis (parzelle_id, urspruenglicher_pachtbeginn, created_by)
  values (p_parzelle_id, p_urspruenglicher_pachtbeginn, p_created_by) returning id into v_pachtverhaeltnis_id;
  insert into public.pachtverhaeltnis_beteiligung (pachtverhaeltnis_id, mitglied_id, rolle, gueltig_ab, created_by)
  values (v_pachtverhaeltnis_id, p_hauptpaechter_mitglied_id, 'hauptpaechter', p_urspruenglicher_pachtbeginn, p_created_by);
  if p_mitpaechter_mitglied_id is not null then
    insert into public.pachtverhaeltnis_beteiligung (pachtverhaeltnis_id, mitglied_id, rolle, gueltig_ab, created_by)
    values (v_pachtverhaeltnis_id, p_mitpaechter_mitglied_id, 'mitpaechter', p_urspruenglicher_pachtbeginn, p_created_by);
  end if;
  insert into public.pachtvertragsfassung
    (pachtverhaeltnis_id, dokument_id, fassungstyp, vertragsdatum, wirksam_ab, vorheriges_vertragsdatum, vorgaenger_fassung_id, created_by)
  values (v_pachtverhaeltnis_id, p_dokument_id, 'erstvertrag', p_vertragsdatum, p_wirksam_ab, null, null, p_created_by)
  returning id into v_pachtvertragsfassung_id;
  return query select v_pachtverhaeltnis_id, v_pachtvertragsfassung_id;
end;
$$;
create or replace function public.register_pachtvertrags_neufassung(
  p_pachtverhaeltnis_id bigint, p_vorgaenger_fassung_id bigint, p_dokument_id bigint,
  p_vertragsdatum date, p_wirksam_ab date, p_created_by uuid
)
returns table(pachtvertragsfassung_id bigint)
language plpgsql security definer set search_path = public, pg_temp
as $$
declare
  v_pachtverhaeltnis public.pachtverhaeltnis%rowtype;
  v_vorgaenger public.pachtvertragsfassung%rowtype;
  v_dokument public.dokument%rowtype;
  v_hauptpaechter_mitglied_id bigint;
  v_aktive_hauptpaechter_anzahl integer;
  v_pachtvertragsfassung_id bigint;
begin
  if auth.role() is distinct from 'service_role' then
    raise exception 'Die Vertragsregistrierung ist nur serverseitig zulässig' using errcode = '42501';
  end if;
  if p_pachtverhaeltnis_id is null or p_vorgaenger_fassung_id is null
     or p_dokument_id is null or p_vertragsdatum is null or p_wirksam_ab is null or p_created_by is null then
    raise exception 'Pachtverhältnis, Vorgängerfassung, Dokument, Vertragsdaten und auslösender Benutzer sind erforderlich' using errcode = '22023';
  end if;
  if not exists (
    select 1 from public.app_user au
    join public.mitglied m on m.id = au.mitglied_id and m.auth_user_id = au.user_id
    where au.user_id = p_created_by and au.mitglied_id is not null and au.mitglied_id > 0
      and au.role in ('admin', 'vorstand')
  ) then
    raise exception 'Der auslösende Benutzer ist nicht als Admin oder Vorstand berechtigt' using errcode = '42501';
  end if;
  select * into v_pachtverhaeltnis from public.pachtverhaeltnis
  where id = p_pachtverhaeltnis_id and beendet_am is null for update;
  if not found then raise exception 'Unbekanntes oder bereits beendetes Pachtverhältnis: %', p_pachtverhaeltnis_id using errcode = 'P0002'; end if;
  if v_pachtverhaeltnis.urspruenglicher_pachtbeginn is not null
     and p_wirksam_ab < v_pachtverhaeltnis.urspruenglicher_pachtbeginn then
    raise exception 'wirksam_ab darf nicht vor dem ursprünglichen Pachtbeginn liegen' using errcode = '23514';
  end if;
  select count(*), min(mitglied_id) into v_aktive_hauptpaechter_anzahl, v_hauptpaechter_mitglied_id
  from public.pachtverhaeltnis_beteiligung
  where pachtverhaeltnis_id = p_pachtverhaeltnis_id and rolle = 'hauptpaechter' and gueltig_bis is null;
  if v_aktive_hauptpaechter_anzahl <> 1 then
    raise exception 'Das Pachtverhältnis muss genau einen aktiven Hauptpächter haben' using errcode = '23514';
  end if;
  select * into v_vorgaenger from public.pachtvertragsfassung where id = p_vorgaenger_fassung_id for key share;
  if not found then raise exception 'Unbekannte Vorgängerfassung: %', p_vorgaenger_fassung_id using errcode = 'P0002'; end if;
  if v_vorgaenger.pachtverhaeltnis_id <> p_pachtverhaeltnis_id then
    raise exception 'Die Vorgängerfassung gehört nicht zum angegebenen Pachtverhältnis' using errcode = '23514';
  end if;
  if v_vorgaenger.vertragsdatum is not null and p_vertragsdatum < v_vorgaenger.vertragsdatum then
    raise exception 'Das Vertragsdatum der Neufassung darf nicht vor dem Vertragsdatum der Vorgängerfassung liegen' using errcode = '23514';
  end if;
  select * into v_dokument from public.dokument where id = p_dokument_id and archiviert_at is null for update;
  if not found then raise exception 'Unbekanntes oder archiviertes Vertragsdokument: %', p_dokument_id using errcode = 'P0002'; end if;
  if v_dokument.parzelle_id is distinct from v_pachtverhaeltnis.parzelle_id then
    raise exception 'Das Vertragsdokument gehört nicht zur Parzelle des Pachtverhältnisses' using errcode = '23514';
  end if;
  if v_dokument.mitglied_id is not null and v_dokument.mitglied_id is distinct from v_hauptpaechter_mitglied_id then
    raise exception 'Das Vertragsdokument gehört nicht zum aktiven Hauptpächter' using errcode = '23514';
  end if;
  if exists (select 1 from public.pachtvertragsfassung where dokument_id = p_dokument_id) then
    raise exception 'Das Vertragsdokument ist bereits einer Vertragsfassung zugeordnet' using errcode = '23505';
  end if;
  if not exists (
    select 1 from public.kommunikationsvorgang kv
    where kv.final_dokument_id = p_dokument_id and kv.vorgangstyp = 'vertrag_signatur'
      and kv.dokumenttyp = 'pachtvertrag' and kv.parzelle_id = v_pachtverhaeltnis.parzelle_id
      and kv.mitglied_id = v_hauptpaechter_mitglied_id and kv.status in ('letzte_pruefung', 'erledigt')
  ) then
    raise exception 'Das Dokument ist kein finales Pachtvertragsdokument des angegebenen Vertragsvorgangs' using errcode = '23514';
  end if;
  if exists (select 1 from public.pachtvertragsfassung where vorgaenger_fassung_id = p_vorgaenger_fassung_id) then
    raise exception 'Vorgängerfassung besitzt bereits einen Nachfolger' using errcode = '23505';
  end if;
  insert into public.pachtvertragsfassung
    (pachtverhaeltnis_id, dokument_id, fassungstyp, vertragsdatum, wirksam_ab, vorheriges_vertragsdatum, vorgaenger_fassung_id, created_by)
  values (p_pachtverhaeltnis_id, p_dokument_id, 'neufassung', p_vertragsdatum, p_wirksam_ab, v_vorgaenger.vertragsdatum, p_vorgaenger_fassung_id, p_created_by)
  returning id into v_pachtvertragsfassung_id;
  return query select v_pachtvertragsfassung_id;
end;
$$;
revoke all on function public.register_pachtverhaeltnis_erstvertrag(bigint, bigint, date, bigint, date, date, uuid, bigint)
  from public, anon, authenticated, service_role;
grant execute on function public.register_pachtverhaeltnis_erstvertrag(bigint, bigint, date, bigint, date, date, uuid, bigint)
  to service_role;
revoke all on function public.register_pachtvertrags_neufassung(bigint, bigint, bigint, date, date, uuid)
  from public, anon, authenticated, service_role;
grant execute on function public.register_pachtvertrags_neufassung(bigint, bigint, bigint, date, date, uuid)
  to service_role;
