-- Zählerbauart, gesetzeskonforme Eichfristen und Status-Synchronisierung.
-- Diese Migration ist noch nicht ausgerollt und ersetzt daher die vorherige Fassung
-- unter demselben Dateinamen.
--
-- Fachlogik Eichfristen:
--   Wasser: 6 Jahre, Ende der Eichfrist am 31.12. des Zieljahres.
--   Strom mechanisch (Ferraris/Induktionsmesswerk): 16 Jahre, Ende am 31.12.
--   Strom elektronisch: 8 Jahre, Ende am 31.12.
--
-- Fachlogik Status:
--   ausgebaut_am IS NULL     -> status = 'aktiv'
--   ausgebaut_am IS NOT NULL -> status = 'ausgebaut'
--
-- Bestehende Zähler werden zunächst als 'mechanisch' übernommen.
-- Bekannte elektronische Altzähler können anschließend gezielt per UPDATE
-- auf 'elektronisch' gesetzt werden; eichfaellig_am wird dabei automatisch
-- neu berechnet.

alter table public.zaehler
    add column if not exists bauart text;

update public.zaehler
set bauart = 'mechanisch'
where bauart is null;

alter table public.zaehler
    alter column bauart set default 'mechanisch';

alter table public.zaehler
    alter column bauart set not null;

alter table public.zaehler
    drop constraint if exists ck_zaehler_bauart;

alter table public.zaehler
    add constraint ck_zaehler_bauart
    check (bauart in ('mechanisch', 'elektronisch'));


-- Zentrale Eichfristberechnung mit Bauart.
-- Bei Eichfristen >= 1 Jahr endet die Frist mit Ablauf des Kalenderjahres.
create or replace function public.calc_eichfaellig_am(
    p_medium public.zaehler_medium,
    p_eichdatum date,
    p_bauart text
)
returns date
language plpgsql
immutable
as $$
declare
    v_zieljahr integer;
begin
    if p_eichdatum is null then
        return null;
    end if;

    case p_medium
        when 'wasser' then
            v_zieljahr := extract(year from p_eichdatum)::integer + 6;

        when 'strom' then
            case coalesce(nullif(btrim(p_bauart), ''), 'mechanisch')
                when 'mechanisch' then
                    v_zieljahr := extract(year from p_eichdatum)::integer + 16;

                when 'elektronisch' then
                    v_zieljahr := extract(year from p_eichdatum)::integer + 8;

                else
                    raise exception 'Ungültige Zählerbauart: %', p_bauart
                        using errcode = '23514';
            end case;

        else
            return null;
    end case;

    return make_date(v_zieljahr, 12, 31);
end;
$$;


-- Bestehende Aufrufer der bisherigen 2-Parameter-Funktion bleiben kompatibel.
-- Ohne explizite Bauart gilt für Strom aus Kompatibilitätsgründen 'mechanisch'.
create or replace function public.calc_eichfaellig_am(
    p_medium public.zaehler_medium,
    p_eichdatum date
)
returns date
language sql
immutable
as $$
    select public.calc_eichfaellig_am(
        p_medium,
        p_eichdatum,
        'mechanisch'
    );
$$;


-- Bestehende Triggerfunktion für Eichfälligkeit auf die neue zentrale Berechnung umstellen.
create or replace function public.trg_zaehler_set_eichfaellig()
returns trigger
language plpgsql
as $$
begin
    new.eichfaellig_am := public.calc_eichfaellig_am(
        new.medium,
        new.eichdatum,
        new.bauart
    );

    return new;
end;
$$;


-- Sicherstellen, dass Änderungen an der Bauart die Eichfälligkeit neu berechnen.
drop trigger if exists trg_zaehler_bauart_set_eichfaellig
on public.zaehler;

create trigger trg_zaehler_bauart_set_eichfaellig
before update of bauart
on public.zaehler
for each row
execute function public.trg_zaehler_set_eichfaellig();


-- Status und Ausbaudatum zentral konsistent halten.
create or replace function public.trg_zaehler_sync_status()
returns trigger
language plpgsql
as $$
begin
    if new.ausgebaut_am is not null then
        new.status := 'ausgebaut'::public.zaehler_status;
    else
        new.status := 'aktiv'::public.zaehler_status;
    end if;

    return new;
end;
$$;

drop trigger if exists trg_zaehler_sync_status
on public.zaehler;

create trigger trg_zaehler_sync_status
before insert or update of ausgebaut_am, status
on public.zaehler
for each row
execute function public.trg_zaehler_sync_status();


-- Bestehende inkonsistente Statuswerte bereinigen.
update public.zaehler
set status = case
    when ausgebaut_am is not null
        then 'ausgebaut'::public.zaehler_status
    else 'aktiv'::public.zaehler_status
end;


-- Altbestand nach Einführung der Bauart mit der neuen Eichfristlogik neu berechnen.
update public.zaehler
set eichfaellig_am = public.calc_eichfaellig_am(
    medium,
    eichdatum,
    bauart
);


comment on column public.zaehler.bauart is
'Zählerbauart: mechanisch oder elektronisch. Wasser: 6 Jahre bis 31.12. des Zieljahres; Strom mechanisch: 16 Jahre bis 31.12.; Strom elektronisch: 8 Jahre bis 31.12.';

comment on function public.calc_eichfaellig_am(public.zaehler_medium, date, text) is
'Berechnet das Ende der Eichfrist zum 31.12. des Zieljahres: Wasser 6 Jahre, Strom mechanisch 16 Jahre, Strom elektronisch 8 Jahre.';

comment on function public.trg_zaehler_sync_status() is
'Hält public.zaehler.status automatisch konsistent zu ausgebaut_am.';
