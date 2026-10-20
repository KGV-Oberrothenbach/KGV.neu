-- Follow-up for installations where 20261020110000 was already applied.
-- Reassert the discard function without clearing final_dokument_id or provenance.
create or replace function public.discard_prepared_contract(p_kommunikationsvorgang_id bigint, p_expected_revision integer, p_discarded_by uuid)
returns table(id bigint, status text, final_dokument_id bigint)
language plpgsql security definer set search_path = public, pg_temp as $$
declare v_vorgang public.kommunikationsvorgang%rowtype;
begin
  if auth.role() is distinct from 'service_role' then raise exception 'Das Verwerfen ist nur serverseitig zulässig' using errcode = '42501'; end if;
  if not exists (select 1 from public.app_user where user_id=p_discarded_by and role in ('admin','vorstand')) then raise exception 'Keine Berechtigung zum Verwerfen' using errcode='42501'; end if;
  select * into v_vorgang from public.kommunikationsvorgang where id=p_kommunikationsvorgang_id for update;
  if not found then raise exception 'Unbekannter Kommunikationsvorgang' using errcode='P0002'; end if;
  if v_vorgang.vorgangstyp <> 'vertrag_signatur' or v_vorgang.dokumenttyp not in ('mitgliedsantrag','pachtvertrag') then raise exception 'Der Vorgang ist kein Vertragsvorgang' using errcode='23514'; end if;
  if v_vorgang.status='verworfen' then return query select v_vorgang.id,v_vorgang.status,v_vorgang.final_dokument_id; return; end if;
  if v_vorgang.revision<>p_expected_revision or v_vorgang.zugewiesen_an is distinct from p_discarded_by then raise exception 'Der Vorgang wurde zwischenzeitlich geändert oder ist nicht zugewiesen' using errcode='40001'; end if;
  if v_vorgang.status not in ('vorbereitet','zur_signatur','letzte_pruefung') then raise exception 'Abgeschlossene Vertragsvorgänge können nicht verworfen werden' using errcode='23514'; end if;
  update public.dokument set archiviert_at=now(),archiviert_by=p_discarded_by,archiviert_begruendung='Verworfener Vertragsvorgang' where source_kommunikationsvorgang_id=v_vorgang.id and archiviert_at is null;
  update public.kommunikationsvorgang set status='verworfen',final_dokument_id=null,bearbeitet_von=p_discarded_by where id=v_vorgang.id and revision=p_expected_revision and status in ('vorbereitet','zur_signatur','letzte_pruefung') and zugewiesen_an=p_discarded_by;
  if not found then raise exception 'Der Vertragsvorgang wurde zwischenzeitlich geändert' using errcode='40001'; end if;
  return query select v_vorgang.id,'verworfen'::text,v_vorgang.final_dokument_id;
end; $$;

-- The workflow guard normally makes final_dokument_id immutable. Clearing it is allowed
-- only together with the server-owned discard transition; the document provenance remains.
create or replace function public.guard_kommunikationsvorgang_update()
returns trigger language plpgsql security definer set search_path = public, pg_temp as $$
begin
  if tg_op = 'INSERT' then
    if new.vorgangstyp = 'vertrag_signatur' and new.status = 'vorbereitet' and new.zugewiesen_an is distinct from auth.uid() then raise exception 'Prepared contract must be assigned to its creating user' using errcode='42501'; end if;
    if auth.uid() is not null then new.erstellt_von:=auth.uid(); new.bearbeitet_von:=auth.uid(); end if;
    if new.signatur_gestartet_am is not null then new.signatur_gestartet_am:=now(); new.signatur_revision:=new.revision; elsif new.signatur_revision is not null then raise exception 'signatur_revision requires signatur_gestartet_am' using errcode='23514'; end if; return new;
  end if;
  if old.vorgangstyp='vertrag_signatur' and old.status='vorbereitet' and new.entwurf_daten is distinct from old.entwurf_daten and (old.zugewiesen_an is distinct from auth.uid() or old.signatur_gestartet_am is not null or old.signatur_revision is not null or new.status<>'vorbereitet' or new.mitglied_id is distinct from old.mitglied_id or new.parzelle_id is distinct from old.parzelle_id or new.dokumenttyp is distinct from old.dokumenttyp) then raise exception 'Prepared contract is no longer editable' using errcode='55000'; end if;
  if old.signatur_gestartet_am is not null and new.status<>'verworfen' and (new.signatur_gestartet_am is distinct from old.signatur_gestartet_am or new.signatur_revision is distinct from old.signatur_revision or new.entwurf_daten is distinct from old.entwurf_daten or new.revision is distinct from old.revision or new.dokumenttyp is distinct from old.dokumenttyp or new.mitglied_id is distinct from old.mitglied_id or new.parzelle_id is distinct from old.parzelle_id) then raise exception 'Signed communication workflow revision is immutable' using errcode='55000'; end if;
  if old.signatur_gestartet_am is null and new.signatur_gestartet_am is not null then new.signatur_gestartet_am:=now(); new.signatur_revision:=new.revision; elsif new.signatur_revision is not null and new.signatur_gestartet_am is null then raise exception 'signatur_revision requires signatur_gestartet_am' using errcode='23514'; end if;
  if new.final_dokument_id is distinct from old.final_dokument_id and not ((old.status='zur_signatur' and new.status='letzte_pruefung' and old.final_dokument_id is null and new.final_dokument_id is not null) or (old.status in ('vorbereitet','zur_signatur','letzte_pruefung') and new.status='verworfen' and new.final_dokument_id is null)) then raise exception 'Final document may only be set when entering letzte_pruefung or cleared when discarding' using errcode='55000'; end if;
  if new.status is distinct from old.status and not ((old.status='entwurf' and new.status in ('vorbereitet','abgebrochen')) or (old.status='vorbereitet' and new.status in ('entwurf','zur_signatur','abgebrochen','verworfen')) or (old.status='zur_signatur' and new.status in ('vorbereitet','mail_bereit','druck_bereit','letzte_pruefung','abgebrochen','verworfen')) or (old.status='letzte_pruefung' and new.status in ('erledigt','verworfen')) or (old.status='mail_bereit' and new.status in ('wird_versendet','abgebrochen')) or (old.status='wird_versendet' and new.status in ('erledigt','fehler')) or (old.status='druck_bereit' and new.status in ('erledigt','fehler','abgebrochen')) or (old.status='fehler' and new.status in ('mail_bereit','druck_bereit','abgebrochen'))) then raise exception 'Invalid communication workflow status transition: % -> %',old.status,new.status using errcode='23514'; end if;
  if old.status='zur_signatur' and new.status='vorbereitet' and old.signatur_gestartet_am is not null then raise exception 'A started signature workflow cannot return to vorbereitet' using errcode='55000'; end if;
  new.bearbeitet_am:=now(); if auth.uid() is not null then new.bearbeitet_von:=auth.uid(); end if; return new;
end; $$;
