-- K4e.3c-2b-2a: Finalized contracts require an explicit final-document review.
alter table public.kommunikationsvorgang
  add column if not exists final_dokument_id bigint references public.dokument(id) on delete restrict;

alter table public.kommunikationsvorgang
  drop constraint if exists kommunikationsvorgang_status_check,
  add constraint kommunikationsvorgang_status_check check (status in (
    'entwurf', 'vorbereitet', 'zur_signatur', 'letzte_pruefung', 'mail_bereit', 'wird_versendet',
    'druck_bereit', 'erledigt', 'fehler', 'abgebrochen'
  )),
  add constraint kommunikationsvorgang_letzte_pruefung_dokument_chk check (
    status <> 'letzte_pruefung' or final_dokument_id is not null
  );

create index if not exists ix_kommunikationsvorgang_final_dokument_id
  on public.kommunikationsvorgang(final_dokument_id)
  where final_dokument_id is not null;

create or replace function public.guard_kommunikationsvorgang_update()
returns trigger
language plpgsql
security definer
set search_path = public, pg_temp
as $$
begin
  if tg_op = 'INSERT' then
    if new.vorgangstyp = 'vertrag_signatur' and new.status = 'vorbereitet'
       and new.zugewiesen_an is distinct from auth.uid() then
      raise exception 'Prepared contract must be assigned to its creating user' using errcode = '42501';
    end if;
    if auth.uid() is not null then new.erstellt_von := auth.uid(); new.bearbeitet_von := auth.uid(); end if;
    if new.signatur_gestartet_am is not null then new.signatur_gestartet_am := now(); new.signatur_revision := new.revision;
    elsif new.signatur_revision is not null then raise exception 'signatur_revision requires signatur_gestartet_am' using errcode = '23514'; end if;
    return new;
  end if;

  if old.vorgangstyp = 'vertrag_signatur' and old.status = 'vorbereitet' and new.entwurf_daten is distinct from old.entwurf_daten then
    if old.zugewiesen_an is distinct from auth.uid() or old.signatur_gestartet_am is not null or old.signatur_revision is not null
       or new.status <> 'vorbereitet' or new.mitglied_id is distinct from old.mitglied_id or new.parzelle_id is distinct from old.parzelle_id or new.dokumenttyp is distinct from old.dokumenttyp then
      raise exception 'Prepared contract is no longer editable' using errcode = '55000';
    end if;
  end if;

  if old.signatur_gestartet_am is not null then
    if new.signatur_gestartet_am is distinct from old.signatur_gestartet_am or new.signatur_revision is distinct from old.signatur_revision
       or new.entwurf_daten is distinct from old.entwurf_daten or new.revision is distinct from old.revision
       or new.dokumenttyp is distinct from old.dokumenttyp or new.mitglied_id is distinct from old.mitglied_id or new.parzelle_id is distinct from old.parzelle_id then
      raise exception 'Signed communication workflow revision is immutable' using errcode = '55000';
    end if;
  elsif new.signatur_gestartet_am is not null then
    new.signatur_gestartet_am := now(); new.signatur_revision := new.revision;
  elsif new.signatur_revision is not null then
    raise exception 'signatur_revision requires signatur_gestartet_am' using errcode = '23514';
  end if;

  if new.final_dokument_id is distinct from old.final_dokument_id
     and not (old.status = 'zur_signatur' and new.status = 'letzte_pruefung' and old.final_dokument_id is null and new.final_dokument_id is not null) then
    raise exception 'Final document may only be set when entering letzte_pruefung' using errcode = '55000';
  end if;

  if new.status is distinct from old.status and not (
    (old.status = 'entwurf' and new.status in ('vorbereitet', 'abgebrochen')) or
    (old.status = 'vorbereitet' and new.status in ('entwurf', 'zur_signatur', 'abgebrochen')) or
    (old.status = 'zur_signatur' and new.status in ('vorbereitet', 'mail_bereit', 'druck_bereit', 'letzte_pruefung', 'abgebrochen')) or
    (old.status = 'letzte_pruefung' and new.status = 'erledigt') or
    (old.status = 'mail_bereit' and new.status in ('wird_versendet', 'abgebrochen')) or
    (old.status = 'wird_versendet' and new.status in ('erledigt', 'fehler')) or
    (old.status = 'druck_bereit' and new.status in ('erledigt', 'fehler', 'abgebrochen')) or
    (old.status = 'fehler' and new.status in ('mail_bereit', 'druck_bereit', 'abgebrochen'))
  ) then raise exception 'Invalid communication workflow status transition: % -> %', old.status, new.status using errcode = '23514'; end if;

  if old.status = 'zur_signatur' and new.status = 'vorbereitet' and old.signatur_gestartet_am is not null then
    raise exception 'A started signature workflow cannot return to vorbereitet' using errcode = '55000';
  end if;
  new.bearbeitet_am := now(); if auth.uid() is not null then new.bearbeitet_von := auth.uid(); end if;
  return new;
end;
$$;
