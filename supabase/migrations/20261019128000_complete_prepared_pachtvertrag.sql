-- PV-APP2a: Atomarer, serverseitiger Abschluss eines vorbereiteten Pachtvertrags.
create or replace function public.complete_prepared_pachtvertrag(
  p_kommunikationsvorgang_id bigint,
  p_expected_revision integer,
  p_created_by uuid
)
returns table(pachtverhaeltnis_id bigint, pachtvertragsfassung_id bigint, bereits_abgeschlossen boolean)
language plpgsql security definer set search_path = public, pg_temp
as $$
declare
  v_vorgang public.kommunikationsvorgang%rowtype;
  v_final_dokument public.dokument%rowtype;
  v_aktive_verhaeltnisse integer;
  v_pachtverhaeltnis_id bigint;
  v_vorgaenger_id bigint;
  v_letzte_fassungen integer;
  v_start_text text;
  v_effective_date_text text;
  v_pachtbeginn date;
  v_vertragsdatum date;
  v_wirksam_ab date;
  v_include_mitpaechter boolean;
  v_mitpaechter_id bigint;
  v_aktive_mitpaechter integer;
  v_aktiver_mitpaechter_id bigint;
  v_fassung_id bigint;
begin
  if auth.role() is distinct from 'service_role' then
    raise exception 'Der Pachtvertragsabschluss ist nur serverseitig zulässig' using errcode = '42501';
  end if;
  if p_kommunikationsvorgang_id is null or p_expected_revision is null or p_created_by is null then
    raise exception 'Kommunikationsvorgang, Revision und auslösender Benutzer sind erforderlich' using errcode = '22023';
  end if;
  if not exists (
    select 1 from public.app_user au join public.mitglied m on m.id = au.mitglied_id and m.auth_user_id = au.user_id
    where au.user_id = p_created_by and au.mitglied_id is not null and au.mitglied_id > 0 and au.role in ('admin', 'vorstand')
  ) then
    raise exception 'Der auslösende Benutzer ist nicht als Admin oder Vorstand berechtigt' using errcode = '42501';
  end if;

  select * into v_vorgang from public.kommunikationsvorgang where id = p_kommunikationsvorgang_id for update;
  if not found then raise exception 'Unbekannter Kommunikationsvorgang: %', p_kommunikationsvorgang_id using errcode = 'P0002'; end if;
  if v_vorgang.vorgangstyp <> 'vertrag_signatur' or v_vorgang.dokumenttyp <> 'pachtvertrag' then
    raise exception 'Der Vorgang ist kein vorbereiteter Pachtvertragsvorgang' using errcode = '23514';
  end if;
  if v_vorgang.revision <> p_expected_revision then raise exception 'Der Vertragsvorgang wurde zwischenzeitlich geändert' using errcode = '40001'; end if;
  if v_vorgang.zugewiesen_an is distinct from p_created_by then raise exception 'Der Vertragsvorgang ist nicht dem auslösenden Benutzer zugewiesen' using errcode = '42501'; end if;
  if v_vorgang.final_dokument_id is null then
    raise exception 'Der Pachtvertragsvorgang enthält kein Finaldokument' using errcode = '23514';
  end if;
  select * into v_final_dokument from public.dokument
  where id = v_vorgang.final_dokument_id and archiviert_at is null
  for key share;
  if not found then
    raise exception 'Das Finaldokument existiert nicht oder ist archiviert' using errcode = '23514';
  end if;
  if v_final_dokument.source_kommunikationsvorgang_id is distinct from v_vorgang.id
     or v_final_dokument.source_revision is distinct from v_vorgang.revision then
    raise exception 'Das Finaldokument gehört nicht zur eingefrorenen Revision dieses Kommunikationsvorgangs' using errcode = '23514';
  end if;
  if v_vorgang.status = 'erledigt' then
    select f.pachtverhaeltnis_id, f.id into v_pachtverhaeltnis_id, v_fassung_id from public.pachtvertragsfassung f where f.dokument_id = v_vorgang.final_dokument_id;
    if found then return query select v_pachtverhaeltnis_id, v_fassung_id, true; end if;
    raise exception 'Der Vorgang ist bereits erledigt, besitzt aber keine registrierte Vertragsfassung' using errcode = '23514';
  end if;
  if v_vorgang.status <> 'letzte_pruefung' or v_vorgang.mitglied_id is null or v_vorgang.parzelle_id is null then
    raise exception 'Der Pachtvertragsvorgang ist nicht vollständig zur letzten Prüfung bereit' using errcode = '23514';
  end if;
  if v_vorgang.signatur_gestartet_am is null or v_vorgang.signatur_revision is null
     or v_vorgang.signatur_revision <> v_vorgang.revision
     or v_vorgang.signatur_revision <> p_expected_revision then
    raise exception 'Der Pachtvertragsvorgang besitzt keinen gültigen eingefrorenen Signaturstand' using errcode = '23514';
  end if;

  v_start_text := v_vorgang.entwurf_daten #>> '{draft,startDate}';
  if v_start_text is null or v_start_text !~ '^\d{4}-\d{2}-\d{2}$' then raise exception 'Im vorbereiteten Pachtvertrag fehlt ein gültiger Pachtbeginn' using errcode = '23514'; end if;
  v_pachtbeginn := v_start_text::date;
  v_vertragsdatum := (v_final_dokument.created_at at time zone 'Europe/Berlin')::date;
  v_include_mitpaechter := coalesce((v_vorgang.entwurf_daten #>> '{draft,includeSecondaryMember}')::boolean, false);
  if v_include_mitpaechter then
    if coalesce(v_vorgang.entwurf_daten #>> '{draft,secondaryMemberId}', '') !~ '^\d+$' then
      raise exception 'Der vorbereitete Erstvertrag enthält keine konkrete Mitpächter-ID' using errcode = '23514';
    end if;
    v_mitpaechter_id := (v_vorgang.entwurf_daten #>> '{draft,secondaryMemberId}')::bigint;
  end if;

  perform pg_advisory_xact_lock(hashtextextended('pachtverhaeltnis:parzelle:' || v_vorgang.parzelle_id::text, 0));
  select count(*), min(id) into v_aktive_verhaeltnisse, v_pachtverhaeltnis_id
  from public.pachtverhaeltnis where parzelle_id = v_vorgang.parzelle_id and beendet_am is null;
  if v_aktive_verhaeltnisse > 1 then raise exception 'Inkonsistenter Bestand: mehrere aktive Pachtverhältnisse für die Parzelle' using errcode = '23514'; end if;

  if v_aktive_verhaeltnisse = 0 then
    select pachtverhaeltnis_id, pachtvertragsfassung_id into v_pachtverhaeltnis_id, v_fassung_id
    from public.register_pachtverhaeltnis_erstvertrag(v_vorgang.parzelle_id, v_vorgang.mitglied_id, v_pachtbeginn,
      v_vorgang.final_dokument_id, v_vertragsdatum, v_pachtbeginn, p_created_by, v_mitpaechter_id);
  else
    select count(*), min(mitglied_id) into v_aktive_mitpaechter, v_aktiver_mitpaechter_id
    from public.pachtverhaeltnis_beteiligung
    where pachtverhaeltnis_id = v_pachtverhaeltnis_id and rolle = 'mitpaechter' and gueltig_bis is null;
    if v_include_mitpaechter and (v_aktive_mitpaechter <> 1 or v_aktiver_mitpaechter_id <> v_mitpaechter_id) then
      raise exception 'Beteiligungsänderungen bei einer Neufassung sind noch nicht unterstützt' using errcode = '23514';
    end if;
    if not v_include_mitpaechter and v_aktive_mitpaechter <> 0 then
      raise exception 'Das Entfernen eines Mitpächters bei einer Neufassung ist noch nicht unterstützt' using errcode = '23514';
    end if;
    v_effective_date_text := v_vorgang.entwurf_daten #>> '{draft,effectiveDate}';
    if v_effective_date_text is null or v_effective_date_text !~ '^\d{4}-\d{2}-\d{2}$' then
      raise exception 'Im vorbereiteten Pachtvertrag fehlt das Wirksamkeitsdatum der Neufassung' using errcode = '23514';
    end if;
    v_wirksam_ab := v_effective_date_text::date;
    select count(*), min(f.id) into v_letzte_fassungen, v_vorgaenger_id
    from public.pachtvertragsfassung f
    where f.pachtverhaeltnis_id = v_pachtverhaeltnis_id
      and not exists (select 1 from public.pachtvertragsfassung n where n.vorgaenger_fassung_id = f.id);
    if v_letzte_fassungen = 0 then raise exception 'Für das aktive Pachtverhältnis existiert keine aktuelle Vertragsfassung' using errcode = '23514'; end if;
    if v_letzte_fassungen <> 1 then raise exception 'Für das aktive Pachtverhältnis ist keine eindeutige aktuelle Vertragsfassung bestimmbar' using errcode = '23514'; end if;
    select pachtvertragsfassung_id into v_fassung_id from public.register_pachtvertrags_neufassung(
      v_pachtverhaeltnis_id, v_vorgaenger_id, v_vorgang.final_dokument_id, v_vertragsdatum, v_wirksam_ab, p_created_by);
  end if;

  update public.kommunikationsvorgang set status = 'erledigt', bearbeitet_von = p_created_by
  where id = v_vorgang.id and status = 'letzte_pruefung' and revision = p_expected_revision
    and signatur_revision = p_expected_revision and final_dokument_id = v_vorgang.final_dokument_id
    and zugewiesen_an = p_created_by;
  if not found then raise exception 'Der Vertragsvorgang konnte nicht abgeschlossen werden' using errcode = '40001'; end if;
  return query select v_pachtverhaeltnis_id, v_fassung_id, false;
end;
$$;
revoke all on function public.complete_prepared_pachtvertrag(bigint, integer, uuid) from public, anon, authenticated, service_role;
grant execute on function public.complete_prepared_pachtvertrag(bigint, integer, uuid) to service_role;
