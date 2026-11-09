begin;

-- D2 compatibility contract: callers may still pass the historical Boolean
-- until D3/D4, but a project always reserves from one project-local namespace.
create or replace function public.reserve_fachnummer(
  p_art text,
  p_quelle_art text,
  p_quelle_id bigint,
  p_is_demo boolean
) returns integer
language plpgsql
security definer
set search_path = public, pg_temp
as $$
declare
  v_nummer integer;
  v_bestehende_art text;
  v_sequenz regclass;
begin
  if p_art = 'historischer_kombivertrag' and
     (p_quelle_art is distinct from 'historisches_dokument'
      or not public.is_registered_historical_contract_document(p_quelle_id)
      or (auth.role() is distinct from 'service_role'
          and not public.can_access_historical_contract_document(p_quelle_id, true))) then
    raise exception 'Historische Nummern benötigen einen berechtigten registrierten Vertragskontext.'
      using errcode = '42501';
  end if;

  if not public.is_admin_or_vorstand() and auth.role() <> 'service_role'
     and not (p_art = 'historischer_kombivertrag' and public.can_register_historical_contracts()) then
    raise exception 'Keine Berechtigung für Nummernvergabe' using errcode = '42501';
  end if;

  perform pg_advisory_xact_lock(
    hashtextextended(concat_ws(':', 'project', p_quelle_art, p_quelle_id::text), 0)
  );

  select nummer, art
    into v_nummer, v_bestehende_art
    from public.fachnummer_reservierung
   where is_demo = false
     and quelle_art = p_quelle_art
     and quelle_id = p_quelle_id;

  if found then
    if v_bestehende_art <> p_art then
      raise exception 'Die Quelle besitzt bereits eine Reservierung der Art % und kann nicht als % verwendet werden.',
        v_bestehende_art, p_art using errcode = '22023';
    end if;
    return v_nummer;
  end if;

  v_sequenz := case p_art
    when 'mitgliedsantrag' then 'public.fachnummer_mitgliedsantrag_seq'::regclass
    when 'pachtvertrag' then 'public.fachnummer_pachtvertrag_seq'::regclass
    when 'historischer_kombivertrag' then 'public.fachnummer_historisch_seq'::regclass
    when 'wartungsvertrag' then 'public.fachnummer_wartungsvertrag_seq'::regclass
  end;
  if v_sequenz is null then
    raise exception 'Unbekannte Nummernart' using errcode = '22023';
  end if;

  v_nummer := nextval(v_sequenz);
  insert into public.fachnummer_reservierung(is_demo, nummer, art, quelle_art, quelle_id)
  values (false, v_nummer, p_art, p_quelle_art, p_quelle_id);
  return v_nummer;
end;
$$;

create or replace function public.trg_reserve_vertragsnummer()
returns trigger
language plpgsql
security definer
set search_path = public, pg_temp
as $$
declare
  v_art text;
  v_quelle_art text;
  v_quelle_id bigint;
begin
  if new.status <> 'zur_signatur' or old.status is not distinct from 'zur_signatur' then
    return new;
  end if;

  v_art := case new.dokumenttyp
    when 'mitgliedsantrag' then 'mitgliedsantrag'
    when 'pachtvertrag' then 'pachtvertrag'
    when 'wartungsvertrag' then 'wartungsvertrag'
  end;
  if v_art is null then
    return new;
  end if;

  if v_art = 'wartungsvertrag' then
    if new.wartungsvertrag_id is null then
      raise exception 'Dem Wartungsvertrags-Signaturvorgang fehlt die fachliche Vertragszuordnung.'
        using errcode = '23514';
    end if;
    perform 1 from public.wartungsvertraege where id = new.wartungsvertrag_id;
    if not found then
      raise exception 'Der Wartungsvertrag für die Nummernreservierung wurde nicht gefunden.'
        using errcode = '23503';
    end if;
    v_quelle_art := 'wartungsvertrag';
    v_quelle_id := new.wartungsvertrag_id;
  else
    v_quelle_art := 'kommunikationsvorgang';
    v_quelle_id := new.id;
  end if;

  perform public.reserve_fachnummer(v_art, v_quelle_art, v_quelle_id, false);
  return new;
end;
$$;

create or replace function public.reserve_historische_vertragsnummer(p_dokument_id bigint)
returns table(nummer integer, titel text, mime_type text, pdf_bereits_gestempelt boolean)
language plpgsql
security definer
set search_path = public, pg_temp
as $$
declare
  v_dokument public.dokument%rowtype;
  v_nummer integer;
  v_titel text;
begin
  if not public.is_registered_historical_contract_document(p_dokument_id)
     or (auth.role() is distinct from 'service_role'
         and not public.can_access_historical_contract_document(p_dokument_id, true)) then
    raise exception 'Nummer und Stempelstatus benötigen einen berechtigten registrierten Vertragskontext.'
      using errcode = '42501';
  end if;
  if not public.can_register_historical_contracts() then
    raise exception 'Für die Nacherfassung historischer Verträge fehlt die Berechtigung.'
      using errcode = '42501';
  end if;

  select * into v_dokument from public.dokument where id = p_dokument_id for update;
  if not found or v_dokument.archiviert_at is not null then
    raise exception 'Das historische Dokument existiert nicht oder ist archiviert.' using errcode = 'P0002';
  end if;
  if v_dokument.mitglied_id is null then
    raise exception 'Ohne ausgewähltes Mitglied darf keine historische Vertragsnummer reserviert werden.'
      using errcode = '22023';
  end if;
  perform 1 from public.mitglied where id = v_dokument.mitglied_id;
  if not found then
    raise exception 'Das ausgewählte Mitglied existiert nicht.' using errcode = 'P0002';
  end if;

  v_nummer := public.reserve_fachnummer(
    'historischer_kombivertrag', 'historisches_dokument', p_dokument_id, false
  );
  v_titel := regexp_replace(
    coalesce(nullif(btrim(v_dokument.titel), ''), 'Historischer Vertrag'),
    '\s*·\s*Vertrags-Nr\.\s*\d{5}\s*$', '', 'i'
  ) || ' · Vertrags-Nr. ' || v_nummer;

  update public.dokument
     set historische_vertragsnummer = v_nummer,
         titel = v_titel,
         updated_at = now()
   where id = p_dokument_id;

  return query
  select v_nummer, v_titel, v_dokument.mime_type,
         v_dokument.historische_vertragsnummer_pdf_gestempelt_am is not null;
end;
$$;

commit;
