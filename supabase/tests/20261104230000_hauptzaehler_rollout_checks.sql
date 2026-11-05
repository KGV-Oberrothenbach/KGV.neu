-- G2.1: Nur lesende Prüfungen NACH dem Rollout von 20261104230000.

select column_name, is_nullable, column_default
from information_schema.columns
where table_schema = 'public' and table_name = 'zaehler'
  and column_name in ('parzelle_id', 'ist_hauptzaehler', 'anlagenbereich')
order by column_name;

select conrelid::regclass as table_name, conname, pg_get_constraintdef(oid) as definition
from pg_constraint
where conrelid in ('public.zaehler'::regclass, 'public.hauptzaehler_messstelle'::regclass)
  and contype = 'c'
order by conrelid::regclass::text, conname;

select medium, anlagenbereich, count(*) as aktive_zaehler
from public.zaehler
where ist_hauptzaehler = true and status = 'aktiv' and ausgebaut_am is null
group by medium, anlagenbereich having count(*) > 1;

-- Auch bei abweichender Schreibweise dürfen keine Kollisionskandidaten
-- verbleiben; beide Abfragen müssen nach der Migration leer sein.
select medium, lower(btrim(anlagenbereich)) as normalisierter_bereich, count(*) as messstellen
from public.hauptzaehler_messstelle
group by medium, lower(btrim(anlagenbereich))
having count(*) > 1;

select medium, lower(btrim(anlagenbereich)) as normalisierter_bereich, count(*) as aktive_zaehler
from public.zaehler
where ist_hauptzaehler = true and status = 'aktiv' and ausgebaut_am is null
group by medium, lower(btrim(anlagenbereich))
having count(*) > 1;

select medium, anlagenbereich, rfid_tag_uid
from public.hauptzaehler_messstelle order by medium, anlagenbereich;

select expected.medium, expected.anlagenbereich
from (values ('strom'::text, 'Nord'::text), ('strom'::text, 'Süd'::text),
             ('wasser'::text, 'Nord'::text), ('wasser'::text, 'Süd'::text))
    as expected(medium, anlagenbereich)
left join public.hauptzaehler_messstelle h
  on h.medium = expected.medium and h.anlagenbereich = expected.anlagenbereich
where h.id is null;

select z.id, z.zaehlernummer, z.medium, z.anlagenbereich, z.status
from public.zaehler z
left join public.hauptzaehler_messstelle h
  on h.medium = z.medium and h.anlagenbereich = z.anlagenbereich
where z.ist_hauptzaehler = true and h.id is null;

select z.medium, z.anlagenbereich, z.status, count(*) as zaehler_anzahl,
       count(a.id) as ablesungen
from public.zaehler z left join public.ablesung a on a.zaehler_id = z.id
where z.ist_hauptzaehler = true
group by z.medium, z.anlagenbereich, z.status
order by z.medium, z.anlagenbereich, z.status;

select indexname, indexdef from pg_indexes
where schemaname = 'public' and tablename = 'zaehler'
  and indexname = 'ux_zaehler_aktiver_hauptzaehler_messstelle';
select to_regprocedure('public.execute_hauptzaehler_meter_change(bigint,public.zaehler_medium,text,date,numeric,text,text,date,date,numeric,text)') as hauptzaehlerwechsel_rpc;
select policyname, cmd, roles, qual from pg_policies
where schemaname = 'public' and tablename = 'hauptzaehler_messstelle';
