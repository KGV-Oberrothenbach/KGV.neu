-- Datenschutzexport: entspricht der aktiven Mitgliederliste.
-- Ein Mitglied ist aktiv, wenn das explizite Aktiv-Flag gesetzt und ein
-- gegebenes Mitgliedsende noch nicht vergangen ist.
create function public.rpc_export_datenschutz_aktive_mitglieder()
returns table (
    garten_nr text,
    name text,
    email text,
    email_info boolean,
    email_rechnung boolean,
    whatsapp boolean
)
language sql
stable
security invoker
as $$
    with aktive_gaerten as (
        select
            pb.mitglied_id,
            string_agg(distinct p.garten_nr, ', ' order by p.garten_nr) as garten_nr
        from public.parzellen_belegung pb
        join public.parzelle p on p.id = pb.parzelle_id
        where (pb.von_datum is null or pb.von_datum <= current_date)
          and (pb.bis_datum is null or pb.bis_datum >= current_date)
          and nullif(btrim(p.garten_nr), '') is not null
        group by pb.mitglied_id
    )
    select
        coalesce(g.garten_nr, '') as garten_nr,
        concat_ws(' ', m.vorname, m.name) as name,
        coalesce(m.email, '') as email,
        coalesce(m.email_info_einwilligung, false) as email_info,
        coalesce(m.email_rechnung_einwilligung, false) as email_rechnung,
        coalesce(m.whatsapp_einwilligung, false) as whatsapp
    from public.mitglied m
    left join aktive_gaerten g on g.mitglied_id = m.id
    where m.aktiv = true
      and (m.mitglied_ende is null or m.mitglied_ende >= current_date)
      and not coalesce(m.is_demo, false)
    order by m.name, m.vorname, m.email;
$$;

grant execute on function public.rpc_export_datenschutz_aktive_mitglieder() to authenticated;

insert into public.app_export_definition
    (export_key, titel, beschreibung, quelle_typ, quelle_name, aktiv, standard_sortierung, standard_ausgabe, erlaubt_csv, erlaubt_pdf)
values
    ('datenschutz_aktive_mitglieder', 'Datenschutz – aktive Mitglieder',
     'Kontakt- und Einwilligungsübersicht aller aktiven Mitglieder.',
     'rpc', 'rpc_export_datenschutz_aktive_mitglieder', true, 'name', 'csv', true, true)
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

insert into public.app_export_column_definition
    (export_key, column_key, label_kurz, label_lang, sortierung, standard_sichtbar, ist_sortierspalte)
values
    ('datenschutz_aktive_mitglieder', 'garten_nr', 'Garten Nr.', 'Garten Nr.', 10, true, true),
    ('datenschutz_aktive_mitglieder', 'name', 'Name', 'Name', 20, true, false),
    ('datenschutz_aktive_mitglieder', 'email', 'E-Mail', 'E-Mail', 30, true, false),
    ('datenschutz_aktive_mitglieder', 'email_info', 'E-Mail-Info', 'E-Mail-Info', 40, true, false),
    ('datenschutz_aktive_mitglieder', 'email_rechnung', 'E-Mail-Rechnung', 'E-Mail-Rechnung', 50, true, false),
    ('datenschutz_aktive_mitglieder', 'whatsapp', 'WhatsApp', 'WhatsApp', 60, true, false)
on conflict (export_key, column_key) do update set
    label_kurz = excluded.label_kurz,
    label_lang = excluded.label_lang,
    sortierung = excluded.sortierung,
    standard_sichtbar = excluded.standard_sichtbar,
    ist_sortierspalte = excluded.ist_sortierspalte;
