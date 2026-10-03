create or replace function public.rpc_export_datenschutz_aktive_mitglieder()
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
    ),
    basis as (
        select
            m.id,
            m.hauptmitglied_id,
            coalesce(m.hauptmitglied_id, m.id) as gruppen_id,
            coalesce(eigener_garten.garten_nr, '') as garten_nr,
            coalesce(gruppen_garten.garten_nr, '') as gruppen_garten_nr,
            concat_ws(' ', m.vorname, m.name) as name,
            coalesce(m.email, '') as email,
            coalesce(m.email_info_einwilligung, false) as email_info,
            coalesce(m.email_rechnung_einwilligung, false) as email_rechnung,
            coalesce(m.whatsapp_einwilligung, false) as whatsapp
        from public.mitglied m
        left join aktive_gaerten eigener_garten
            on eigener_garten.mitglied_id = m.id
        left join aktive_gaerten gruppen_garten
            on gruppen_garten.mitglied_id = coalesce(m.hauptmitglied_id, m.id)
        where m.aktiv = true
          and (m.mitglied_ende is null or m.mitglied_ende >= current_date)
          and not coalesce(m.is_demo, false)
    )
    select
        b.garten_nr,
        b.name,
        b.email,
        b.email_info,
        b.email_rechnung,
        b.whatsapp
    from basis b
    order by
        case when nullif(b.gruppen_garten_nr, '') is null then 1 else 0 end,
        substring(b.gruppen_garten_nr from '^[[:space:]]*([0-9]+)')::integer nulls last,
        b.gruppen_garten_nr,
        b.gruppen_id,
        case when b.hauptmitglied_id is null then 0 else 1 end,
        b.name,
        b.email;
$$;
