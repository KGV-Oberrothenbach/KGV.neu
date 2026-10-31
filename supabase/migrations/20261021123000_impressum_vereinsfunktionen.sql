-- Structured, purpose-specific presentation metadata for the new function model.
-- No legacy slot name is interpreted here.

alter table public.vereinsfunktion_verwendung
  add column if not exists impressum_gruppe text,
  add column if not exists impressum_gruppe_bezeichnung text,
  add column if not exists impressum_gruppen_sort_order smallint,
  add column if not exists impressum_ist_hauptkontakt boolean,
  add column if not exists impressum_zeige_ladungsfaehige_anschrift boolean;
update public.vereinsfunktion_verwendung
set impressum_gruppe = coalesce(nullif(btrim(impressum_gruppe), ''), 'weitere'),
    impressum_gruppe_bezeichnung = coalesce(nullif(btrim(impressum_gruppe_bezeichnung), ''), 'Weitere Funktionen'),
    impressum_gruppen_sort_order = coalesce(impressum_gruppen_sort_order, 10),
    impressum_ist_hauptkontakt = coalesce(impressum_ist_hauptkontakt, false),
    impressum_zeige_ladungsfaehige_anschrift = coalesce(impressum_zeige_ladungsfaehige_anschrift, false)
where impressum_gruppe is null
   or btrim(impressum_gruppe) = ''
   or impressum_gruppe_bezeichnung is null
   or btrim(impressum_gruppe_bezeichnung) = ''
   or impressum_gruppen_sort_order is null
   or impressum_ist_hauptkontakt is null
   or impressum_zeige_ladungsfaehige_anschrift is null;
alter table public.vereinsfunktion_verwendung
  alter column impressum_gruppe set default 'weitere',
  alter column impressum_gruppe_bezeichnung set default 'Weitere Funktionen',
  alter column impressum_gruppen_sort_order set default 10,
  alter column impressum_ist_hauptkontakt set default false,
  alter column impressum_zeige_ladungsfaehige_anschrift set default false,
  alter column impressum_gruppe set not null,
  alter column impressum_gruppe_bezeichnung set not null,
  alter column impressum_gruppen_sort_order set not null,
  alter column impressum_ist_hauptkontakt set not null,
  alter column impressum_zeige_ladungsfaehige_anschrift set not null;
alter table public.vereinsfunktion_verwendung
  add constraint ck_vereinsfunktion_verwendung_impressum_gruppe_nonempty
    check (verwendungszweck <> 'impressum' or btrim(impressum_gruppe) <> ''),
  add constraint ck_vereinsfunktion_verwendung_impressum_gruppe_bezeichnung_nonempty
    check (verwendungszweck <> 'impressum' or btrim(impressum_gruppe_bezeichnung) <> ''),
  add constraint ck_vereinsfunktion_verwendung_impressum_metadaten_vollstaendig
    check (verwendungszweck <> 'impressum' or (
      impressum_gruppe is not null
      and impressum_gruppe_bezeichnung is not null
      and impressum_gruppen_sort_order is not null
      and impressum_ist_hauptkontakt is not null
      and impressum_zeige_ladungsfaehige_anschrift is not null
    )),
  add constraint ck_vereinsfunktion_verwendung_impressum_anschrift_hauptkontakt
    check (not coalesce(impressum_zeige_ladungsfaehige_anschrift, false)
      or coalesce(impressum_ist_hauptkontakt, false));
create unique index ux_vereinsfunktion_verwendung_impressum_hauptkontakt
  on public.vereinsfunktion_verwendung (verwendungszweck)
  where verwendungszweck = 'impressum' and impressum_ist_hauptkontakt;
-- This deliberately returns only the contact fields required by the Impressum.
-- It is SECURITY DEFINER solely to project selected member fields; callers never
-- receive direct member-table access, history, representations or other purposes.
create or replace function public.impressum_kontakte()
returns table (
  bezeichnung text,
  besetzung_id bigint,
  gruppen_schluessel text,
  gruppen_bezeichnung text,
  gruppen_sort_order smallint,
  ist_hauptkontakt boolean,
  zeige_ladungsfaehige_anschrift boolean,
  vorname text,
  nachname text,
  handy text,
  adresse text,
  plz text,
  ort text
)
language sql stable security definer
set search_path = public, pg_temp
as $$
  select f.bezeichnung, b.id,
         v.impressum_gruppe, v.impressum_gruppe_bezeichnung, v.impressum_gruppen_sort_order,
         v.impressum_ist_hauptkontakt, v.impressum_zeige_ladungsfaehige_anschrift,
         m.vorname, m.name, m.handy,
         case when v.impressum_zeige_ladungsfaehige_anschrift then m.adresse end,
         case when v.impressum_zeige_ladungsfaehige_anschrift then m.plz end,
         case when v.impressum_zeige_ladungsfaehige_anschrift then m.ort end
    from public.vereinsfunktion f
    join public.vereinsfunktion_verwendung v on v.vereinsfunktion_id = f.id
    join public.vereinsfunktion_besetzung b on b.vereinsfunktion_id = f.id
    join public.mitglied m on m.id = b.mitglied_id
   where auth.role() in ('authenticated', 'service_role')
     and f.aktiv
     and v.verwendungszweck = 'impressum'
     and b.gueltig_ab <= current_date
     and (b.gueltig_bis is null or b.gueltig_bis >= current_date)
   order by v.impressum_ist_hauptkontakt desc, v.impressum_gruppen_sort_order,
            v.sort_order, f.schluessel, b.sort_order, b.id;
$$;
revoke all on function public.impressum_kontakte() from public;
grant execute on function public.impressum_kontakte() to authenticated, service_role;
