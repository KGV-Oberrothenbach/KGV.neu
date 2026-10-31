-- ALT-3.5: Atomare Admin-Verwaltung für allgemeine Dokumenttypen.
-- Aktiv steuert nur die Auswahl bei neuen Importen; die Nachweis-Views werten
-- weiterhin ausschließlich die Wirkungsrelation aus und behalten Alt-Dokumente bei.

create or replace function public.save_dokumenttyp(
    p_dokumenttyp_id bigint,
    p_bezeichnung text,
    p_aktiv boolean,
    p_sortierung integer,
    p_wirkungen text[] default '{}'::text[]
)
returns table (id bigint, bezeichnung text, aktiv boolean, sortierung integer)
language plpgsql
security definer
set search_path = public, pg_temp
as $$
declare
    v_id bigint;
    v_wirkungen text[] := coalesce(p_wirkungen, '{}'::text[]);
begin
    if not coalesce(public.is_admin(), false) then
        raise exception 'Nur Admin darf Dokumenttypen verwalten.';
    end if;

    if nullif(btrim(p_bezeichnung), '') is null then
        raise exception 'Eine Bezeichnung ist erforderlich.';
    end if;

    if exists (
        select 1
        from unnest(v_wirkungen) as requested(wirkung)
        where requested.wirkung not in (
            'mitgliedsantrag_nachweis',
            'pachtvertrag_nachweis',
            'wartungsvertrag_nachweis',
            'pachtbezug_erforderlich',
            'wartungsvertragsbezug_erforderlich'
        )
    ) then
        raise exception 'Eine Dokumenttyp-Wirkung ist ungültig.';
    end if;

    if (select count(*) from unnest(v_wirkungen)) <> (select count(distinct wirkung) from unnest(v_wirkungen) as item(wirkung)) then
        raise exception 'Eine Dokumenttyp-Wirkung wurde mehrfach angegeben.';
    end if;

    if p_dokumenttyp_id is null then
        v_id := nextval('public.dokumenttyp_id_seq');
        insert into public.dokumenttyp (id, code, bezeichnung, aktiv, sortierung)
        values (v_id, format('dokumenttyp_%s', v_id), btrim(p_bezeichnung), coalesce(p_aktiv, true), p_sortierung);
    else
        update public.dokumenttyp as dt
        set bezeichnung = btrim(p_bezeichnung), aktiv = coalesce(p_aktiv, true), sortierung = p_sortierung
        where dt.id = p_dokumenttyp_id
        returning dt.id into v_id;

        if not found then
            raise exception 'Der Dokumenttyp existiert nicht.';
        end if;
    end if;

    delete from public.dokumenttyp_wirkung where dokumenttyp_id = v_id;
    insert into public.dokumenttyp_wirkung (dokumenttyp_id, wirkung)
    select v_id, item.wirkung from unnest(v_wirkungen) as item(wirkung);

    return query
    select dt.id, dt.bezeichnung, dt.aktiv, dt.sortierung
    from public.dokumenttyp as dt
    where dt.id = v_id;
end;
$$;
revoke all on function public.save_dokumenttyp(bigint, text, boolean, integer, text[]) from public, anon, authenticated, service_role;
grant execute on function public.save_dokumenttyp(bigint, text, boolean, integer, text[]) to authenticated;
comment on function public.save_dokumenttyp(bigint, text, boolean, integer, text[]) is
    'Speichert einen allgemeinen Dokumenttyp und seine vollständige Wirkungsmenge atomar. Nur Admin.';
