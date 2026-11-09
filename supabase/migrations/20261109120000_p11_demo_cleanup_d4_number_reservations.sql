begin;

-- D4 preflight: the two former Demo namespaces may only be collapsed when
-- both future project-local keys are already conflict-free.  No row is
-- deleted, merged, renumbered or overwritten here.
create or replace function public.assert_fachnummer_reservierung_project_conflict_free()
returns void
language plpgsql
set search_path = public, pg_temp
as $$
declare
  v_conflicts text;
  v_foreign_keys text;
begin
  select string_agg(format('%s (%s Reservierungen)', nummer, anzahl), ', ' order by nummer)
    into v_conflicts
    from (
      select nummer, count(*) as anzahl
      from public.fachnummer_reservierung
      group by nummer
      having count(*) > 1
    ) conflicts;
  if v_conflicts is not null then
    raise exception 'D4-Konflikt: Fachnummern sind projektweit nicht eindeutig: %', v_conflicts
      using errcode = '23505',
            hint = 'Keine Reservierung wurde verändert. Konflikte müssen fachlich geprüft werden.';
  end if;

  select string_agg(format('%s:%s (%s Reservierungen)', quelle_art, quelle_id, anzahl), ', '
                    order by quelle_art, quelle_id)
    into v_conflicts
    from (
      select quelle_art, quelle_id, count(*) as anzahl
      from public.fachnummer_reservierung
      group by quelle_art, quelle_id
      having count(*) > 1
    ) conflicts;
  if v_conflicts is not null then
    raise exception 'D4-Konflikt: Quellbindungen sind projektweit nicht eindeutig: %', v_conflicts
      using errcode = '23505',
            hint = 'Keine Reservierung wurde verändert. Konflikte müssen fachlich geprüft werden.';
  end if;

  select string_agg(format('%s.%s (%s)', child_ns.nspname, child.relname, constraint_row.conname), ', '
                    order by child_ns.nspname, child.relname, constraint_row.conname)
    into v_foreign_keys
    from pg_constraint constraint_row
    join pg_class child on child.oid = constraint_row.conrelid
    join pg_namespace child_ns on child_ns.oid = child.relnamespace
   where constraint_row.contype = 'f'
     and constraint_row.confrelid = 'public.fachnummer_reservierung'::regclass;
  if v_foreign_keys is not null then
    raise exception 'D4 kann vorhandene Fremdschlüssel nicht automatisch umstellen: %', v_foreign_keys
      using errcode = '2BP01',
            hint = 'Die Fremdschlüssel müssen einzeln geprüft und ohne DROP CASCADE migriert werden.';
  end if;
end;
$$;

select public.assert_fachnummer_reservierung_project_conflict_free();

-- Build the future keys first.  A failure leaves the transaction and all
-- existing reservations untouched.
create unique index ux_fachnummer_reservierung_d4_nummer
  on public.fachnummer_reservierung(nummer);
create unique index ux_fachnummer_reservierung_d4_quelle
  on public.fachnummer_reservierung(quelle_art, quelle_id);

alter table public.fachnummer_reservierung
  drop constraint fachnummer_reservierung_pkey;
alter table public.fachnummer_reservierung
  add constraint fachnummer_reservierung_pkey
  primary key using index ux_fachnummer_reservierung_d4_nummer;

alter table public.fachnummer_reservierung
  drop constraint fachnummer_reservierung_is_demo_quelle_art_quelle_id_key;
alter table public.fachnummer_reservierung
  add constraint fachnummer_reservierung_quelle_key
  unique using index ux_fachnummer_reservierung_d4_quelle;

-- Imported/reserved values remain unchanged.  Never move a sequence
-- backwards: gaps from rolled-back allocations stay consumed, while an
-- imported maximum still advances the corresponding project-local sequence.
do $$
declare
  v_row record;
  v_last_value bigint;
  v_is_called boolean;
  v_max_nummer integer;
begin
  for v_row in
    select * from (values
      ('mitgliedsantrag'::text, 'public.fachnummer_mitgliedsantrag_seq'::regclass),
      ('pachtvertrag'::text, 'public.fachnummer_pachtvertrag_seq'::regclass),
      ('historischer_kombivertrag'::text, 'public.fachnummer_historisch_seq'::regclass),
      ('wartungsvertrag'::text, 'public.fachnummer_wartungsvertrag_seq'::regclass)
    ) as sequence_map(art, sequenz)
  loop
    execute format('select last_value, is_called from %s', v_row.sequenz)
      into v_last_value, v_is_called;
    select max(nummer) into v_max_nummer
    from public.fachnummer_reservierung
    where art = v_row.art;

    if v_max_nummer is not null
       and (not v_is_called or v_max_nummer > v_last_value) then
      perform setval(v_row.sequenz, v_max_nummer, true);
    end if;
  end loop;
end;
$$;

create or replace function public.reserve_fachnummer(
  p_art text,
  p_quelle_art text,
  p_quelle_id bigint
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
   where quelle_art = p_quelle_art
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

  perform public.reserve_fachnummer(v_art, v_quelle_art, v_quelle_id);
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
    'historischer_kombivertrag', 'historisches_dokument', p_dokument_id
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

create or replace view public.v_wartungsvertrag_fachnummer
with (security_invoker = true)
as
select
  w.id as wartungsvertrag_id,
  fr.nummer as vertragsnummer
from public.wartungsvertraege w
left join public.fachnummer_reservierung fr
  on fr.art = 'wartungsvertrag'
 and fr.quelle_art = 'wartungsvertrag'
 and fr.quelle_id = w.id;

revoke all on function public.reserve_fachnummer(text, text, bigint) from public, anon;
grant execute on function public.reserve_fachnummer(text, text, bigint) to authenticated, service_role;

revoke all on function public.reserve_fachnummer(text, text, bigint, boolean)
  from public, anon, authenticated, service_role;
drop function public.reserve_fachnummer(text, text, bigint, boolean);

grant select on public.v_wartungsvertrag_fachnummer to authenticated, service_role;

commit;
