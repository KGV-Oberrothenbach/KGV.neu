-- G3.5: Hauptzählerwechsel-RPC – Enum/Text-Vergleich reparieren.
-- Die Messstelle speichert medium als text, zaehler.medium als zaehler_medium.
-- Bereits angewendete Migrationen werden nicht verändert.

begin;

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
        where h.medium = v_old.medium::text
          and h.anlagenbereich = p_anlagenbereich
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

commit;
