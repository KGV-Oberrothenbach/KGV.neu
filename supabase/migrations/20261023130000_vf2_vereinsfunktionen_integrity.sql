-- VF-2: preserve the identity and history of club-function assignments.
-- This migration deliberately does not alter existing assignments or legacy data.

do $$
begin
  if exists (
    select 1
      from public.vereinsfunktion_verwendung
     group by vereinsfunktion_id, verwendungszweck
    having count(*) > 1
  ) then
    raise exception 'Doppelte Vereinsfunktionsverwendungen müssen vor VF-2 bereinigt werden';
  end if;
end;
$$;

alter table public.vereinsfunktion_verwendung
  add constraint uq_vereinsfunktion_verwendung_funktion_zweck
  unique (vereinsfunktion_id, verwendungszweck);

create or replace function public.prevent_vereinsfunktion_besetzung_identity_change()
returns trigger
language plpgsql
set search_path = public, pg_temp
as $$
begin
  if new.vereinsfunktion_id is distinct from old.vereinsfunktion_id
     or new.mitglied_id is distinct from old.mitglied_id
     or new.gueltig_ab is distinct from old.gueltig_ab then
    raise exception 'Funktion, Mitglied und Beginn einer Besetzung sind historisch und dürfen nicht geändert werden. Bitte die Besetzung beenden und eine neue anlegen.'
      using errcode = '23514';
  end if;
  return new;
end;
$$;

drop trigger if exists trg_vereinsfunktion_besetzung_identity_immutable
  on public.vereinsfunktion_besetzung;
create trigger trg_vereinsfunktion_besetzung_identity_immutable
before update of vereinsfunktion_id, mitglied_id, gueltig_ab
on public.vereinsfunktion_besetzung
for each row execute function public.prevent_vereinsfunktion_besetzung_identity_change();

create or replace function public.prevent_vereinsfunktion_schluessel_change()
returns trigger
language plpgsql
set search_path = public, pg_temp
as $$
begin
  if new.schluessel is distinct from old.schluessel then
    raise exception 'Der technische Schlüssel einer Vereinsfunktion ist nach der Anlage unveränderlich.'
      using errcode = '23514';
  end if;
  return new;
end;
$$;

drop trigger if exists trg_vereinsfunktion_schluessel_immutable
  on public.vereinsfunktion;
create trigger trg_vereinsfunktion_schluessel_immutable
before update of schluessel
on public.vereinsfunktion
for each row execute function public.prevent_vereinsfunktion_schluessel_change();

-- The table is retained solely as a migration archive. New data must use
-- vereinsfunktion, vereinsfunktion_besetzung and vereinsfunktion_verwendung.
create or replace function public.prevent_legacy_impressum_funktion_slot_write()
returns trigger
language plpgsql
set search_path = public, pg_temp
as $$
begin
  raise exception 'impressum_funktion_slot ist ein schreibgeschütztes Legacy-Archiv. Vereinsfunktionen bitte im neuen Modell pflegen.'
    using errcode = '55000';
end;
$$;

drop trigger if exists trg_impressum_funktion_slot_legacy_read_only
  on public.impressum_funktion_slot;
create trigger trg_impressum_funktion_slot_legacy_read_only
before insert or update or delete
on public.impressum_funktion_slot
for each row execute function public.prevent_legacy_impressum_funktion_slot_write();
