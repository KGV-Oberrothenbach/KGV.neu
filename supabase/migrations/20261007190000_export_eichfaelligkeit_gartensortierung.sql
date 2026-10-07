-- Die Eichfälligkeit bleibt fachlich vollständig durch v_zaehler_eichstatus
-- bestimmt. Diese Migration korrigiert ausschließlich die Standardreihenfolge
-- des Exports auf die natürliche Gartennummern-Sortierung.
update public.app_export_definition
set standard_sortierung = 'garten_nr'
where export_key = 'zaehler_eichfaelligkeit';

create or replace function public.rpc_export_zaehler_eichfaelligkeit(p_medium text default null)
returns table (
    parzelle text,
    medium text,
    zaehler_id bigint,
    zaehlernummer text,
    rfid text,
    zaehler_status text,
    eingebaut_am date,
    eichdatum date,
    eichfaellig_am date,
    tage_bis_faellig integer,
    faelligkeitsstatus text
)
language sql
stable
security invoker
as $$
    select
        concat_ws(' ', e.anlage, e.garten_nr) as parzelle,
        case e.medium::text when 'strom' then 'Strom' when 'wasser' then 'Wasser' else e.medium::text end as medium,
        e.id as zaehler_id,
        e.zaehlernummer,
        case e.medium::text when 'wasser' then p.rfid_wasser else p.rfid_strom end as rfid,
        case e.status::text when 'aktiv' then 'Aktiv' else e.status::text end as zaehler_status,
        e.eingebaut_am,
        e.eichdatum,
        e.eichfaellig_am,
        e.tage_bis_faellig,
        case e.eichstatus
            when 'ueberfaellig' then 'Überfällig'
            when 'bald_faellig' then 'Bald fällig'
            else e.eichstatus
        end as faelligkeitsstatus
    from public.v_zaehler_eichstatus e
    join public.parzelle p on p.id = e.parzelle_id
    where e.eichstatus in ('ueberfaellig', 'bald_faellig')
      and (nullif(btrim(p_medium), '') is null or e.medium::text = lower(btrim(p_medium)))
    order by
        case when nullif(btrim(e.garten_nr), '') is null then 1 else 0 end,
        substring(e.garten_nr from '^[[:space:]]*([0-9]+)')::integer nulls last,
        e.garten_nr,
        e.anlage,
        e.medium,
        e.zaehlernummer,
        e.id;
$$;

grant execute on function public.rpc_export_zaehler_eichfaelligkeit(text) to authenticated;
