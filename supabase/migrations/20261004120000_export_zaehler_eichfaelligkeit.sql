-- Eichfälligkeit: Die Klassifizierung (überfällig / innerhalb von 180 Tagen)
-- kommt ausschließlich aus v_zaehler_eichstatus. So bleibt der Export mit der
-- Ablesungs-/Zähleransicht fachlich identisch.
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
        case e.eichstatus when 'ueberfaellig' then 0 when 'bald_faellig' then 1 else 2 end,
        e.eichfaellig_am,
        e.anlage,
        e.garten_nr,
        e.medium,
        e.zaehlernummer;
$$;

grant execute on function public.rpc_export_zaehler_eichfaelligkeit(text) to authenticated;

insert into public.app_export_definition
    (export_key, titel, beschreibung, quelle_typ, quelle_name, aktiv, standard_sortierung, standard_ausgabe, erlaubt_csv, erlaubt_pdf)
values
    ('zaehler_eichfaelligkeit', 'Zähler – Eichfälligkeit',
     'Überfällige und innerhalb von 180 Tagen fällige aktive Zähler gemäß zentralem Eichstatus.',
     'rpc', 'rpc_export_zaehler_eichfaelligkeit', true, 'eichfaellig_am', 'csv', true, true)
on conflict (export_key) do update set
    titel = excluded.titel,
    beschreibung = excluded.beschreibung,
    quelle_typ = excluded.quelle_typ,
    quelle_name = excluded.quelle_name,
    aktiv = excluded.aktiv,
    standard_sortierung = excluded.standard_sortierung,
    standard_ausgabe = excluded.standard_ausgabe,
    erlaubt_csv = excluded.erlaubt_csv,
    erlaubt_pdf = excluded.erlaubt_pdf;

delete from public.app_export_filter_definition where export_key = 'zaehler_eichfaelligkeit';
insert into public.app_export_filter_definition (export_key, filter_key, label, typ, optionen_json, pflicht, sortierung) values
    ('zaehler_eichfaelligkeit', 'p_medium', 'Zählerart', 'select',
     '[{"label":"Strom","value":"strom"},{"label":"Wasser","value":"wasser"}]'::jsonb, false, 10);

delete from public.app_export_column_definition where export_key = 'zaehler_eichfaelligkeit';
insert into public.app_export_column_definition
    (export_key, column_key, label_kurz, label_lang, sortierung, standard_sichtbar, ist_sortierspalte) values
    ('zaehler_eichfaelligkeit', 'parzelle', 'Parzelle', 'Parzelle', 10, true, true),
    ('zaehler_eichfaelligkeit', 'medium', 'Zählerart', 'Zählerart', 20, true, false),
    ('zaehler_eichfaelligkeit', 'zaehlernummer', 'Zählernummer', 'Zählernummer', 30, true, false),
    ('zaehler_eichfaelligkeit', 'rfid', 'RFID', 'RFID-Tag', 40, true, false),
    ('zaehler_eichfaelligkeit', 'zaehler_status', 'Status', 'Aktueller Zählerstatus', 50, true, false),
    ('zaehler_eichfaelligkeit', 'eingebaut_am', 'Einbau', 'Eingebaut am', 60, true, false),
    ('zaehler_eichfaelligkeit', 'eichdatum', 'Eichdatum', 'Eichdatum', 70, true, false),
    ('zaehler_eichfaelligkeit', 'eichfaellig_am', 'Eichfällig', 'Eichfällig am', 80, true, true),
    ('zaehler_eichfaelligkeit', 'tage_bis_faellig', 'Tage', 'Tage bis zur Eichfälligkeit', 90, true, false),
    ('zaehler_eichfaelligkeit', 'faelligkeitsstatus', 'Fälligkeitsstatus', 'Fälligkeitsstatus', 100, true, false),
    ('zaehler_eichfaelligkeit', 'zaehler_id', 'Zähler-ID', 'Technische Zähler-ID', 110, false, false);
