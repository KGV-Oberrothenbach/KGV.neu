-- NB-2B-B2. Commit this migration; do not apply it without separate approval.
-- Keeps fachnummer reservations source-specific and gives historical documents
-- a stable 80xxx metadata field. The source is always dokument.id.

alter table public.dokument
  add column if not exists historische_vertragsnummer integer,
  add column if not exists historische_vertragsnummer_pdf_gestempelt_am timestamptz;
alter table public.dokument
  drop constraint if exists dokument_historische_vertragsnummer_check;
alter table public.dokument
  add constraint dokument_historische_vertragsnummer_check
  check (historische_vertragsnummer is null or historische_vertragsnummer between 80000 and 89999);
create or replace function public.reserve_fachnummer(
  p_art text,
  p_quelle_art text,
  p_quelle_id bigint,
  p_is_demo boolean
)
returns integer
language plpgsql
security definer
set search_path = public, pg_temp
as $$
declare
  v_nummer integer;
  v_bestehende_art text;
  v_sequenz regclass;
begin
  if not public.is_admin_or_vorstand() and auth.role() <> 'service_role' then
    raise exception 'Keine Berechtigung für Nummernvergabe' using errcode = '42501';
  end if;

  -- Source-specific, transaction-scoped lock: concurrent first calls for one
  -- source share a result without serialising unrelated number ranges.
  perform pg_advisory_xact_lock(hashtextextended(concat_ws(':', p_is_demo::text, p_quelle_art, p_quelle_id::text), 0));

  select nummer, art
  into v_nummer, v_bestehende_art
  from public.fachnummer_reservierung
  where is_demo = p_is_demo
    and quelle_art = p_quelle_art
    and quelle_id = p_quelle_id;

  if found then
    if v_bestehende_art <> p_art then
      raise exception 'Die Quelle besitzt bereits eine Reservierung der Art % und kann nicht als % verwendet werden.', v_bestehende_art, p_art using errcode = '22023';
    end if;
    return v_nummer;
  end if;

  v_sequenz := case when p_is_demo then
    case p_art
      when 'mitgliedsantrag' then 'public.fachnummer_mitgliedsantrag_demo_seq'::regclass
      when 'pachtvertrag' then 'public.fachnummer_pachtvertrag_demo_seq'::regclass
      when 'historischer_kombivertrag' then 'public.fachnummer_historisch_demo_seq'::regclass
      when 'wartungsvertrag' then 'public.fachnummer_wartungsvertrag_demo_seq'::regclass
    end
  else
    case p_art
      when 'mitgliedsantrag' then 'public.fachnummer_mitgliedsantrag_seq'::regclass
      when 'pachtvertrag' then 'public.fachnummer_pachtvertrag_seq'::regclass
      when 'historischer_kombivertrag' then 'public.fachnummer_historisch_seq'::regclass
      when 'wartungsvertrag' then 'public.fachnummer_wartungsvertrag_seq'::regclass
    end
  end;

  if v_sequenz is null then
    raise exception 'Unbekannte Nummernart' using errcode = '22023';
  end if;

  v_nummer := nextval(v_sequenz);
  insert into public.fachnummer_reservierung(is_demo, nummer, art, quelle_art, quelle_id)
  values (p_is_demo, v_nummer, p_art, p_quelle_art, p_quelle_id);
  return v_nummer;
end;
$$;
create or replace function public.reserve_historische_vertragsnummer(p_dokument_id bigint)
returns table (nummer integer, titel text, mime_type text, pdf_bereits_gestempelt boolean)
language plpgsql
security definer
set search_path = public, pg_temp
as $$
declare
  v_dokument public.dokument%rowtype;
  v_is_demo boolean;
  v_nummer integer;
  v_titel text;
begin
  if not public.is_admin_or_vorstand() and auth.role() <> 'service_role' then
    raise exception 'Keine Berechtigung für Nummernvergabe' using errcode = '42501';
  end if;

  select * into v_dokument from public.dokument where id = p_dokument_id for update;
  if not found or v_dokument.archiviert_at is not null then
    raise exception 'Das historische Dokument existiert nicht oder ist archiviert.';
  end if;
  if v_dokument.mitglied_id is null then
    raise exception 'Ohne eindeutig ausgewähltes Mitglied darf keine historische Vertragsnummer reserviert werden.' using errcode = '22023';
  end if;

  select coalesce(is_demo, false) into v_is_demo from public.mitglied where id = v_dokument.mitglied_id;
  if not found then
    raise exception 'Das ausgewählte Mitglied existiert nicht.';
  end if;

  v_nummer := public.reserve_fachnummer('historischer_kombivertrag', 'historisches_dokument', p_dokument_id, v_is_demo);
  v_titel := regexp_replace(coalesce(nullif(btrim(v_dokument.titel), ''), 'Historischer Vertrag'), '\s*·\s*Vertrags-Nr\.\s*\d{5}\s*$', '', 'i') || ' · Vertrags-Nr. ' || v_nummer;

  update public.dokument
  set historische_vertragsnummer = v_nummer,
      titel = v_titel,
      updated_at = now()
  where id = p_dokument_id;

  return query select v_nummer, v_titel, v_dokument.mime_type,
    v_dokument.historische_vertragsnummer_pdf_gestempelt_am is not null;
end;
$$;
create or replace function public.mark_historische_vertragsnummer_pdf_gestempelt(p_dokument_id bigint, p_nummer integer)
returns void
language plpgsql
security definer
set search_path = public, pg_temp
as $$
begin
  if not public.is_admin_or_vorstand() and auth.role() <> 'service_role' then
    raise exception 'Keine Berechtigung für Nummernvergabe' using errcode = '42501';
  end if;
  update public.dokument
  set historische_vertragsnummer_pdf_gestempelt_am = coalesce(historische_vertragsnummer_pdf_gestempelt_am, now()), updated_at = now()
  where id = p_dokument_id and historische_vertragsnummer = p_nummer;
  if not found then
    raise exception 'Die historische Vertragsnummer passt nicht zum Dokument.' using errcode = '22023';
  end if;
end;
$$;
revoke all on function public.reserve_historische_vertragsnummer(bigint) from public, anon;
grant execute on function public.reserve_historische_vertragsnummer(bigint) to authenticated, service_role;
revoke all on function public.mark_historische_vertragsnummer_pdf_gestempelt(bigint, integer) from public, anon;
grant execute on function public.mark_historische_vertragsnummer_pdf_gestempelt(bigint, integer) to authenticated, service_role;
