-- KGV: G2.1 – kanonische Hauptzähler-Bereiche als sichere Vorwärtsmigration.
-- Voraussetzung: 20261102224500_hauptzaehler_basis wurde bereits angewendet.
-- Falls vorhanden, wird 20261103090000_hauptzaehler_meter_change durch diese
-- Migration fachlich konsolidiert. Keine Tabelle und keine Spalte wird erzeugt.
-- Diese Datei nicht auf einer DB ohne die genannte Basismigration ausführen.

begin;

do $$
begin
    if to_regclass('public.hauptzaehler_messstelle') is null then
        raise exception 'Voraussetzung fehlt: public.hauptzaehler_messstelle existiert nicht.';
    end if;

    if not exists (
        select 1
        from information_schema.columns
        where table_schema = 'public'
          and table_name = 'zaehler'
          and column_name in ('ist_hauptzaehler', 'anlagenbereich')
        group by table_schema, table_name
        having count(*) = 2
    ) then
        raise exception 'Voraussetzung fehlt: die Hauptzähler-Spalten in public.zaehler existieren nicht.';
    end if;
end;
$$;

-- Alte technische Werte entfernen, ohne Daten oder Historie zu verlieren.
alter table public.zaehler
    drop constraint if exists zaehler_hauptzaehler_parzelle_bereich_chk,
    drop constraint if exists zaehler_hauptzaehler_zuordnung_chk;
alter table public.hauptzaehler_messstelle
    drop constraint if exists hauptzaehler_messstelle_bereich_chk,
    drop constraint if exists hauptzaehler_messstelle_anlagenbereich_chk,
    drop constraint if exists hauptzaehler_messstelle_anlagenbereich_check;

-- Vor der Normalisierung dürfen keine Werte auf dieselbe kanonische
-- Kombination zusammenfallen. Die Migration bricht dann ohne Datenverlust ab,
-- statt RFID-Zuordnungen, Zähler oder Historie zusammenzuführen.
do $$
begin
    if exists (
        select 1
        from public.hauptzaehler_messstelle
        group by medium, case lower(btrim(anlagenbereich))
            when 'nord' then 'Nord'
            when 'sued' then 'Süd'
            when 'süd' then 'Süd'
            else anlagenbereich
        end
        having count(*) > 1
    ) then
        raise exception 'Die Normalisierung würde doppelte Hauptzähler-Messstellen erzeugen.';
    end if;

    if exists (
        select 1
        from public.zaehler
        where ist_hauptzaehler = true
          and status = 'aktiv'
          and ausgebaut_am is null
        group by medium, case lower(btrim(anlagenbereich))
            when 'nord' then 'Nord'
            when 'sued' then 'Süd'
            when 'süd' then 'Süd'
            else anlagenbereich
        end
        having count(*) > 1
    ) then
        raise exception 'Die Normalisierung würde doppelte aktive Hauptzähler je Medium und Bereich erzeugen.';
    end if;
end;
$$;

update public.hauptzaehler_messstelle
set anlagenbereich = case lower(btrim(anlagenbereich))
    when 'nord' then 'Nord'
    when 'sued' then 'Süd'
    when 'süd' then 'Süd'
    else anlagenbereich
end
where lower(btrim(anlagenbereich)) in ('nord', 'sued', 'süd');

update public.zaehler
set anlagenbereich = case lower(btrim(anlagenbereich))
    when 'nord' then 'Nord'
    when 'sued' then 'Süd'
    when 'süd' then 'Süd'
    else anlagenbereich
end
where ist_hauptzaehler = true
  and lower(btrim(anlagenbereich)) in ('nord', 'sued', 'süd');

do $$
declare
    invalid_messstellen integer;
    invalid_zaehler integer;
    missing_kombinationen integer;
begin
    select count(*) into invalid_messstellen
    from public.hauptzaehler_messstelle
    where medium not in ('strom', 'wasser')
       or anlagenbereich not in ('Nord', 'Süd');
    if invalid_messstellen > 0 then
        raise exception 'Hauptzähler-Messstellen enthalten % ungültige Medium-/Bereichswerte.', invalid_messstellen;
    end if;

    select count(*) into missing_kombinationen
    from (
        values ('strom'::text, 'Nord'::text), ('strom'::text, 'Süd'::text),
               ('wasser'::text, 'Nord'::text), ('wasser'::text, 'Süd'::text)
    ) expected(medium, anlagenbereich)
    left join public.hauptzaehler_messstelle h
      on h.medium = expected.medium and h.anlagenbereich = expected.anlagenbereich
    where h.id is null;
    if missing_kombinationen > 0 then
        raise exception 'Es fehlen % der vier verbindlichen Hauptzähler-Messstellen.', missing_kombinationen;
    end if;

    select count(*) into invalid_zaehler
    from public.zaehler
    where (ist_hauptzaehler = true and (parzelle_id is not null or anlagenbereich not in ('Nord', 'Süd')))
       or (ist_hauptzaehler = false and (parzelle_id is null or anlagenbereich is not null));
    if invalid_zaehler > 0 then
        raise exception 'public.zaehler enthält % Einträge, die nicht der Hauptzähler-Zuordnung entsprechen.', invalid_zaehler;
    end if;
end;
$$;

alter table public.zaehler
    add constraint zaehler_hauptzaehler_zuordnung_chk
    check (
        (ist_hauptzaehler = false and parzelle_id is not null and anlagenbereich is null)
        or
        (ist_hauptzaehler = true and parzelle_id is null and anlagenbereich in ('Nord', 'Süd'))
    );
alter table public.hauptzaehler_messstelle
    add constraint hauptzaehler_messstelle_anlagenbereich_check
    check (anlagenbereich in ('Nord', 'Süd'));

-- Genau ein kanonischer Index; ausgebaute Zähler bleiben historisch möglich.
drop index if exists public.ux_zaehler_aktiver_hauptzaehler_medium_bereich;
drop index if exists public.ux_zaehler_aktiver_hauptzaehler_messstelle;
create unique index ux_zaehler_aktiver_hauptzaehler_messstelle
    on public.zaehler (medium, anlagenbereich)
    where ist_hauptzaehler = true and status = 'aktiv' and ausgebaut_am is null;

-- Ersetzt die noch technische nord/sued-Variante. Der Wechsel bleibt atomar;
-- RFID verbleibt an der Messstelle und wird nicht berührt.
create or replace function public.execute_hauptzaehler_meter_change(
    p_alter_zaehler_id bigint, p_medium public.zaehler_medium, p_anlagenbereich text,
    p_ausbaudatum date, p_endstand numeric, p_ausbau_foto_pfad text,
    p_neue_zaehlernummer text, p_eichdatum date, p_einbaudatum date,
    p_anfangsstand numeric, p_einbau_foto_pfad text
)
returns table(alter_zaehler_id bigint, neuer_zaehler_id bigint)
language plpgsql security definer set search_path to 'public', 'pg_temp'
as $$
declare
    v_old public.zaehler%rowtype;
    v_new public.zaehler%rowtype;
    v_last_stand numeric;
begin
    if not public.has_effective_permission(64) then
        raise exception 'Für Zählerwechsel besteht keine Berechtigung.' using errcode = '42501';
    end if;
    if p_anlagenbereich not in ('Nord', 'Süd') or p_ausbaudatum is null or p_einbaudatum is null or p_eichdatum is null
       or p_endstand is null or p_endstand < 0 or p_anfangsstand is null or p_anfangsstand < 0
       or nullif(trim(p_neue_zaehlernummer), '') is null or p_einbaudatum < p_ausbaudatum then
        raise exception 'Die Hauptzähler-Wechselangaben sind ungültig.' using errcode = '23514';
    end if;
    select * into v_old from public.zaehler where id = p_alter_zaehler_id for update;
    if v_old.id is null or not v_old.ist_hauptzaehler or v_old.parzelle_id is not null
       or v_old.medium <> p_medium or v_old.anlagenbereich <> p_anlagenbereich
       or v_old.status <> 'aktiv' or v_old.ausgebaut_am is not null then
        raise exception 'Der alte Zähler ist nicht der aktive Hauptzähler der Messstelle.' using errcode = '23514';
    end if;
    if not exists (
        select 1 from public.hauptzaehler_messstelle h
        where h.medium = v_old.medium and h.anlagenbereich = p_anlagenbereich
    ) then
        raise exception 'Die Hauptzähler-Messstelle für Medium und Anlagenbereich fehlt.' using errcode = 'P0002';
    end if;
    select a.stand into v_last_stand from public.zaehler_ablesung a
    where a.zaehler_id = v_old.id order by a.ablesedatum desc, a.id desc limit 1;
    if v_last_stand is not null and p_endstand < v_last_stand then
        raise exception 'Der Endstand darf nicht kleiner als der letzte bekannte Stand sein.' using errcode = '23514';
    end if;
    insert into public.zaehler_ablesung(zaehler_id, ablesedatum, stand, art, foto_pfad, freigegeben, pruefstatus)
    values(v_old.id, p_ausbaudatum, p_endstand, 'ausbau', nullif(trim(p_ausbau_foto_pfad), ''), true, 'freigegeben');
    update public.zaehler set ausgebaut_am = p_ausbaudatum, status = 'ausgebaut' where id = v_old.id;
    insert into public.zaehler(parzelle_id, ist_hauptzaehler, medium, anlagenbereich, zaehlernummer, eichdatum, eingebaut_am, status)
    values(null, true, p_medium, p_anlagenbereich, trim(p_neue_zaehlernummer), p_eichdatum, p_einbaudatum, 'aktiv')
    returning * into v_new;
    insert into public.zaehler_ablesung(zaehler_id, ablesedatum, stand, art, foto_pfad, freigegeben, pruefstatus)
    values(v_new.id, p_einbaudatum, p_anfangsstand, 'einbau', nullif(trim(p_einbau_foto_pfad), ''), true, 'freigegeben');
    return query select v_old.id, v_new.id;
end;
$$;
grant execute on function public.execute_hauptzaehler_meter_change(bigint, public.zaehler_medium, text, date, numeric, text, text, date, date, numeric, text) to authenticated;

-- RFID-UIDs an Messstellen nur für die bestehende Zähler-Leseberechtigung.
alter table public.hauptzaehler_messstelle enable row level security;
revoke all on table public.hauptzaehler_messstelle from anon;
revoke insert, update, delete on table public.hauptzaehler_messstelle from authenticated;
grant select on table public.hauptzaehler_messstelle to authenticated;
drop policy if exists hauptzaehler_messstelle_select_can_read_meters on public.hauptzaehler_messstelle;
create policy hauptzaehler_messstelle_select_can_read_meters
    on public.hauptzaehler_messstelle for select to authenticated
    using (public.has_effective_permission(32));

comment on column public.zaehler.anlagenbereich is
    'Nur bei Hauptzählern: Nord oder Süd (identisch zu parzelle."Anlage").';
comment on column public.hauptzaehler_messstelle.anlagenbereich is
    'Nord oder Süd; identisch zu zaehler.anlagenbereich und parzelle."Anlage".';

commit;
