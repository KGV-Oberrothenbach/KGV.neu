-- G8.3: zentrale Datenbankabsicherung für Verwaltungswerte von Arbeitseinsätzen.
alter table public.arbeitseinsatz
  drop constraint if exists ck_arbeitseinsatz_max_teilnehmer_positive,
  drop constraint if exists ck_arbeitseinsatz_sichtbarkeit,
  drop constraint if exists ck_arbeitseinsatz_anmeldung_bis;

alter table public.arbeitseinsatz
  add constraint ck_arbeitseinsatz_max_teilnehmer_positive check (max_teilnehmer is null or max_teilnehmer >= 1),
  add constraint ck_arbeitseinsatz_sichtbarkeit check (sichtbar_ab is null or sichtbar_bis is null or sichtbar_bis >= sichtbar_ab),
  add constraint ck_arbeitseinsatz_anmeldung_bis check (anmeldung_bis is null or anmeldung_bis <= datum + coalesce(start_uhrzeit, time '23:59'));
