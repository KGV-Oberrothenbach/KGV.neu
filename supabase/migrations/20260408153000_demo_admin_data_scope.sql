alter table public.termin
    add column if not exists is_demo boolean not null default false;

comment on column public.termin.is_demo is 'Kennzeichnet Demo-/Play-Store-/Test-Termine. Nicht im normalen produktiven Nutzerkontext anzeigen.';

alter table public.bekanntmachung
    add column if not exists is_demo boolean not null default false;

comment on column public.bekanntmachung.is_demo is 'Kennzeichnet Demo-/Play-Store-/Test-Bekanntmachungen. Nicht im normalen produktiven Nutzerkontext anzeigen.';

create or replace function public.is_demo_or_reviewer() returns boolean
    language sql stable security definer
    set search_path to 'public', 'pg_temp'
    as $$
    select public.is_demo_user()
        or public.is_playstore_reviewer();
$$;

comment on function public.is_demo_or_reviewer() is 'TRUE für Demo-/Play-Store-Reviewer-Konten, unabhängig von der fachlichen Rolle.';

create or replace function public.is_productive_admin_or_vorstand() returns boolean
    language sql stable security definer
    set search_path to 'public', 'pg_temp'
    as $$
    select public.is_admin_or_vorstand()
        and not public.is_demo_or_reviewer();
$$;

comment on function public.is_productive_admin_or_vorstand() is 'TRUE nur für echte produktive Admin-/Vorstandskonten; Demo-/Reviewer-Konten werden bewusst ausgeschlossen.';

create or replace function public.is_restricted_demo_admin_or_vorstand() returns boolean
    language sql stable security definer
    set search_path to 'public', 'pg_temp'
    as $$
    select public.is_admin_or_vorstand()
        and public.is_demo_or_reviewer();
$$;

comment on function public.is_restricted_demo_admin_or_vorstand() is 'TRUE für Demo-/Reviewer-Konten mit Rolle admin/vorstand; diese dürfen ausschließlich Demo-Scope sehen.';

create or replace function public.is_demo_mitglied_id(p_mitglied_id bigint) returns boolean
    language sql stable security definer
    set search_path to 'public', 'pg_temp'
    as $$
    select exists (
        select 1
        from public.mitglied m
        where m.id = p_mitglied_id
          and coalesce(m.is_demo, false) = true
    );
$$;

create or replace function public.is_demo_parzelle_id(p_parzelle_id bigint) returns boolean
    language sql stable security definer
    set search_path to 'public', 'pg_temp'
    as $$
    select exists (
        select 1
        from public.parzelle p
        where p.id = p_parzelle_id
          and coalesce(p.is_demo, false) = true
    );
$$;

create or replace function public.is_demo_zaehler_id(p_zaehler_id bigint) returns boolean
    language sql stable security definer
    set search_path to 'public', 'pg_temp'
    as $$
    select exists (
        select 1
        from public.zaehler z
        join public.parzelle p
          on p.id = z.parzelle_id
        where z.id = p_zaehler_id
          and coalesce(p.is_demo, false) = true
    );
$$;

create or replace function public.is_demo_app_user_row(p_mitglied_id bigint, p_is_demo_account boolean) returns boolean
    language sql stable security definer
    set search_path to 'public', 'pg_temp'
    as $$
    select coalesce(p_is_demo_account, false)
        or public.is_demo_mitglied_id(p_mitglied_id);
$$;

create or replace function public.is_demo_dokument_scope(p_mitglied_id bigint, p_parzelle_id bigint) returns boolean
    language sql stable security definer
    set search_path to 'public', 'pg_temp'
    as $$
    select (p_mitglied_id is not null or p_parzelle_id is not null)
        and (p_mitglied_id is null or public.is_demo_mitglied_id(p_mitglied_id))
        and (p_parzelle_id is null or public.is_demo_parzelle_id(p_parzelle_id));
$$;

create or replace function public.is_demo_member_parzelle_scope(p_mitglied_id bigint, p_parzelle_id bigint) returns boolean
    language sql stable security definer
    set search_path to 'public', 'pg_temp'
    as $$
    select p_mitglied_id is not null
        and p_parzelle_id is not null
        and public.is_demo_mitglied_id(p_mitglied_id)
        and public.is_demo_parzelle_id(p_parzelle_id);
$$;

create or replace function public.is_demo_member_arbeitseinsatz_scope(p_mitglied_id bigint, p_arbeitseinsatz_id bigint) returns boolean
    language sql stable security definer
    set search_path to 'public', 'pg_temp'
    as $$
    select p_mitglied_id is not null
        and p_arbeitseinsatz_id is not null
        and public.is_demo_mitglied_id(p_mitglied_id)
        and exists (
            select 1
            from public.arbeitseinsatz a
            where a.id = p_arbeitseinsatz_id
              and coalesce(a.is_demo, false) = true
        );
$$;

create or replace function public.is_demo_member_wartungsvertrag_scope(p_hauptmitglied_id bigint, p_wartungsvertrag_id bigint) returns boolean
    language sql stable security definer
    set search_path to 'public', 'pg_temp'
    as $$
    select p_hauptmitglied_id is not null
        and p_wartungsvertrag_id is not null
        and public.is_demo_mitglied_id(p_hauptmitglied_id)
        and exists (
            select 1
            from public.wartungsvertraege w
            where w.id = p_wartungsvertrag_id
              and coalesce(w.is_demo, false) = true
        );
$$;

create or replace function public.is_demo_impressum_slot_scope(p_mitglied_id bigint) returns boolean
    language sql stable security definer
    set search_path to 'public', 'pg_temp'
    as $$
    select p_mitglied_id is null
        or public.is_demo_mitglied_id(p_mitglied_id);
$$;

create or replace view public.v_pflichtstunden_uebersicht
with ("security_invoker" = 'true') as
 select s.id as saison_id,
    s.jahr as saison_jahr,
    m.id as hauptmitglied_id,
    m.name,
    m.vorname,
    x.regelgrund,
    x.ist_befreit,
    x.hat_wartungsvertrag,
    x.altersbefreit,
    x.eintritt_im_saisonjahr,
    x.eintritt_zweites_halbjahr,
    x.pflichtstunden_soll,
    x.geleistete_stunden,
    x.offene_stunden,
    x.euro_pro_fehlstunde,
    x.fehlbetrag
   from public.saison s
     cross join public.mitglied m
     cross join lateral public.fn_berechne_pflichtstunden_status(m.id, s.id::bigint) x(hauptmitglied_id, saison_id, saison_jahr, regelgrund, ist_befreit, hat_wartungsvertrag, altersbefreit, eintritt_im_saisonjahr, eintritt_zweites_halbjahr, pflichtstunden_soll, geleistete_stunden, offene_stunden, euro_pro_fehlstunde, fehlbetrag)
  where m.hauptmitglied_id is null;

create or replace view public.v_startseite_arbeitseinsatz
with ("security_invoker" = 'true') as
 select a.id,
    a.titel,
    a.beschreibung,
    a.datum,
    a.start_uhrzeit,
    a.end_uhrzeit,
    a.treffpunkt,
    a.max_teilnehmer,
    a.stunden_wert,
    a.sichtbar_ab,
    a.sichtbar_bis,
    a.anmeldung_bis,
    (coalesce(sum(
        case
            when aa.status = 'angemeldet'::public.arbeitseinsatz_anmeldung_status then 1
            else 0
        end), 0::bigint))::integer as angemeldet_count,
        case
            when a.max_teilnehmer is null then null::integer
            else (greatest((a.max_teilnehmer - coalesce(sum(
                case
                    when aa.status = 'angemeldet'::public.arbeitseinsatz_anmeldung_status then 1
                    else 0
                end), 0::bigint)), 0::bigint))::integer
        end as freie_plaetze
   from public.arbeitseinsatz a
     left join public.arbeitseinsatz_anmeldung aa
       on aa.arbeitseinsatz_id = a.id
  where a.aktiv = true
    and (a.sichtbar_ab is null or a.sichtbar_ab <= now()::timestamp without time zone)
    and (a.sichtbar_bis is null or a.sichtbar_bis >= now()::timestamp without time zone)
  group by a.id, a.titel, a.beschreibung, a.datum, a.start_uhrzeit, a.end_uhrzeit, a.treffpunkt, a.max_teilnehmer, a.stunden_wert, a.sichtbar_ab, a.sichtbar_bis, a.anmeldung_bis
  order by a.datum, a.start_uhrzeit nulls first, a.id;

drop policy if exists app_user_admin_full on public.app_user;
create policy app_user_admin_full on public.app_user
    to authenticated
    using (public.is_productive_admin_or_vorstand())
    with check (public.is_productive_admin_or_vorstand());

drop policy if exists app_user_select_own_or_admin on public.app_user;
create policy app_user_select_own_or_admin on public.app_user
    for select to authenticated
    using (
        public.is_productive_admin_or_vorstand()
        or (
            user_id = auth.uid()
            and (
                not public.is_demo_or_reviewer()
                or public.is_demo_app_user_row(mitglied_id, is_demo_account)
            )
        )
    );

drop policy if exists app_user_demo_admin_full on public.app_user;
create policy app_user_demo_admin_full on public.app_user
    to authenticated
    using (
        public.is_restricted_demo_admin_or_vorstand()
        and public.is_demo_app_user_row(mitglied_id, is_demo_account)
    )
    with check (
        public.is_restricted_demo_admin_or_vorstand()
        and public.is_demo_app_user_row(mitglied_id, is_demo_account)
    );

drop policy if exists arbeitseinsatz_admin_full on public.arbeitseinsatz;
create policy arbeitseinsatz_admin_full on public.arbeitseinsatz
    to authenticated
    using (public.is_productive_admin_or_vorstand())
    with check (public.is_productive_admin_or_vorstand());

drop policy if exists arbeitseinsatz_demo_admin_full on public.arbeitseinsatz;
create policy arbeitseinsatz_demo_admin_full on public.arbeitseinsatz
    to authenticated
    using (public.is_restricted_demo_admin_or_vorstand() and coalesce(is_demo, false) = true)
    with check (public.is_restricted_demo_admin_or_vorstand() and coalesce(is_demo, false) = true);

drop policy if exists arbeitseinsatz_select_visible_authenticated on public.arbeitseinsatz;
create policy arbeitseinsatz_select_visible_authenticated on public.arbeitseinsatz
    for select to authenticated
    using (
        public.is_productive_admin_or_vorstand()
        or (
            aktiv = true
            and (sichtbar_ab is null or sichtbar_ab <= now()::timestamp without time zone)
            and (sichtbar_bis is null or sichtbar_bis >= now()::timestamp without time zone)
            and (
                (public.is_demo_or_reviewer() and coalesce(is_demo, false) = true)
                or ((not public.is_demo_or_reviewer()) and coalesce(is_demo, false) = false)
            )
        )
    );

drop policy if exists arbeitseinsatz_anmeldung_admin_full on public.arbeitseinsatz_anmeldung;
create policy arbeitseinsatz_anmeldung_admin_full on public.arbeitseinsatz_anmeldung
    to authenticated
    using (public.is_productive_admin_or_vorstand())
    with check (public.is_productive_admin_or_vorstand());

drop policy if exists arbeitseinsatz_anmeldung_demo_admin_full on public.arbeitseinsatz_anmeldung;
create policy arbeitseinsatz_anmeldung_demo_admin_full on public.arbeitseinsatz_anmeldung
    to authenticated
    using (
        public.is_restricted_demo_admin_or_vorstand()
        and public.is_demo_member_arbeitseinsatz_scope(mitglied_id, arbeitseinsatz_id)
    )
    with check (
        public.is_restricted_demo_admin_or_vorstand()
        and public.is_demo_member_arbeitseinsatz_scope(mitglied_id, arbeitseinsatz_id)
    );

drop policy if exists arbeitseinsatz_anmeldung_delete_own_or_admin on public.arbeitseinsatz_anmeldung;
create policy arbeitseinsatz_anmeldung_delete_own_or_admin on public.arbeitseinsatz_anmeldung
    for delete to authenticated
    using (
        public.is_productive_admin_or_vorstand()
        or (
            mitglied_id = public.current_mitglied_id()
            and (
                not public.is_demo_or_reviewer()
                or public.is_demo_member_arbeitseinsatz_scope(mitglied_id, arbeitseinsatz_id)
            )
        )
    );

drop policy if exists arbeitseinsatz_anmeldung_insert_own_open on public.arbeitseinsatz_anmeldung;
create policy arbeitseinsatz_anmeldung_insert_own_open on public.arbeitseinsatz_anmeldung
    for insert to authenticated
    with check (
        public.is_productive_admin_or_vorstand()
        or (
            public.current_mitglied_id() is not null
            and mitglied_id = public.current_mitglied_id()
            and (
                not public.is_demo_or_reviewer()
                or public.is_demo_member_arbeitseinsatz_scope(mitglied_id, arbeitseinsatz_id)
            )
            and exists (
                select 1
                from public.arbeitseinsatz a
                where a.id = arbeitseinsatz_anmeldung.arbeitseinsatz_id
                  and a.aktiv = true
                  and (a.sichtbar_ab is null or a.sichtbar_ab <= now()::timestamp without time zone)
                  and (a.sichtbar_bis is null or a.sichtbar_bis >= now()::timestamp without time zone)
                  and (a.anmeldung_bis is null or a.anmeldung_bis >= now()::timestamp without time zone)
            )
        )
    );

drop policy if exists arbeitseinsatz_anmeldung_select_own_or_admin on public.arbeitseinsatz_anmeldung;
create policy arbeitseinsatz_anmeldung_select_own_or_admin on public.arbeitseinsatz_anmeldung
    for select to authenticated
    using (
        public.is_productive_admin_or_vorstand()
        or (
            mitglied_id = public.current_mitglied_id()
            and (
                not public.is_demo_or_reviewer()
                or public.is_demo_member_arbeitseinsatz_scope(mitglied_id, arbeitseinsatz_id)
            )
        )
    );

drop policy if exists arbeitseinsatz_anmeldung_update_own_open on public.arbeitseinsatz_anmeldung;
create policy arbeitseinsatz_anmeldung_update_own_open on public.arbeitseinsatz_anmeldung
    for update to authenticated
    using (
        public.is_productive_admin_or_vorstand()
        or (
            mitglied_id = public.current_mitglied_id()
            and (
                not public.is_demo_or_reviewer()
                or public.is_demo_member_arbeitseinsatz_scope(mitglied_id, arbeitseinsatz_id)
            )
        )
    )
    with check (
        public.is_productive_admin_or_vorstand()
        or (
            public.current_mitglied_id() is not null
            and mitglied_id = public.current_mitglied_id()
            and (
                not public.is_demo_or_reviewer()
                or public.is_demo_member_arbeitseinsatz_scope(mitglied_id, arbeitseinsatz_id)
            )
            and exists (
                select 1
                from public.arbeitseinsatz a
                where a.id = arbeitseinsatz_anmeldung.arbeitseinsatz_id
                  and a.aktiv = true
                  and (a.sichtbar_ab is null or a.sichtbar_ab <= now()::timestamp without time zone)
                  and (a.sichtbar_bis is null or a.sichtbar_bis >= now()::timestamp without time zone)
                  and (a.anmeldung_bis is null or a.anmeldung_bis >= now()::timestamp without time zone)
            )
        )
    );

drop policy if exists arbeitsstunde_admin_full on public.arbeitsstunde;
create policy arbeitsstunde_admin_full on public.arbeitsstunde
    to authenticated
    using (public.is_productive_admin_or_vorstand())
    with check (public.is_productive_admin_or_vorstand());

drop policy if exists arbeitsstunde_demo_admin_full on public.arbeitsstunde;
create policy arbeitsstunde_demo_admin_full on public.arbeitsstunde
    to authenticated
    using (public.is_restricted_demo_admin_or_vorstand() and public.is_demo_mitglied_id(mitglied_id))
    with check (public.is_restricted_demo_admin_or_vorstand() and public.is_demo_mitglied_id(mitglied_id));

drop policy if exists arbeitsstunde_select_own_or_admin on public.arbeitsstunde;
create policy arbeitsstunde_select_own_or_admin on public.arbeitsstunde
    for select to authenticated
    using (
        public.is_productive_admin_or_vorstand()
        or (
            mitglied_id = public.current_mitglied_id()
            and (
                not public.is_demo_or_reviewer()
                or public.is_demo_mitglied_id(mitglied_id)
            )
        )
    );

drop policy if exists bekanntmachung_admin_full on public.bekanntmachung;
create policy bekanntmachung_admin_full on public.bekanntmachung
    to authenticated
    using (public.is_productive_admin_or_vorstand())
    with check (public.is_productive_admin_or_vorstand());

drop policy if exists bekanntmachung_demo_admin_full on public.bekanntmachung;
create policy bekanntmachung_demo_admin_full on public.bekanntmachung
    to authenticated
    using (public.is_restricted_demo_admin_or_vorstand() and coalesce(is_demo, false) = true)
    with check (public.is_restricted_demo_admin_or_vorstand() and coalesce(is_demo, false) = true);

drop policy if exists bekanntmachung_select_visible_authenticated on public.bekanntmachung;
create policy bekanntmachung_select_visible_authenticated on public.bekanntmachung
    for select to authenticated
    using (
        public.is_productive_admin_or_vorstand()
        or (
            aktiv = true
            and (sichtbar_ab is null or sichtbar_ab <= now()::timestamp without time zone)
            and (sichtbar_bis is null or sichtbar_bis >= now()::timestamp without time zone)
            and (
                (public.is_demo_or_reviewer() and coalesce(is_demo, false) = true)
                or ((not public.is_demo_or_reviewer()) and coalesce(is_demo, false) = false)
            )
        )
    );

drop policy if exists dokument_select on public.dokument;
create policy dokument_select on public.dokument
    for select to authenticated
    using (
        public.is_productive_admin_or_vorstand()
        or (
            (
                mitglied_id = public.current_mitglied_id()
                or (
                    parzelle_id is not null
                    and exists (
                        select 1
                        from public.parzellen_belegung b
                        where b.parzelle_id = dokument.parzelle_id
                          and b.mitglied_id = public.current_mitglied_id()
                          and (b.von_datum is null or b.von_datum <= now()::date)
                          and (b.bis_datum is null or b.bis_datum >= now()::date)
                    )
                )
            )
            and (
                not public.is_demo_or_reviewer()
                or public.is_demo_dokument_scope(mitglied_id, parzelle_id)
            )
        )
    );

drop policy if exists dokument_write on public.dokument;
create policy dokument_write on public.dokument
    to authenticated
    using (public.is_productive_admin_or_vorstand())
    with check (public.is_productive_admin_or_vorstand());

drop policy if exists dokument_demo_admin_full on public.dokument;
create policy dokument_demo_admin_full on public.dokument
    to authenticated
    using (
        public.is_restricted_demo_admin_or_vorstand()
        and public.is_demo_dokument_scope(mitglied_id, parzelle_id)
    )
    with check (
        public.is_restricted_demo_admin_or_vorstand()
        and public.is_demo_dokument_scope(mitglied_id, parzelle_id)
    );

drop policy if exists impressum_funktion_slot_admin_full on public.impressum_funktion_slot;
create policy impressum_funktion_slot_admin_full on public.impressum_funktion_slot
    to authenticated
    using (public.is_productive_admin_or_vorstand())
    with check (public.is_productive_admin_or_vorstand());

drop policy if exists impressum_funktion_slot_demo_admin_full on public.impressum_funktion_slot;
create policy impressum_funktion_slot_demo_admin_full on public.impressum_funktion_slot
    to authenticated
    using (
        public.is_restricted_demo_admin_or_vorstand()
        and public.is_demo_impressum_slot_scope(mitglied_id)
    )
    with check (
        public.is_restricted_demo_admin_or_vorstand()
        and public.is_demo_impressum_slot_scope(mitglied_id)
    );

drop policy if exists impressum_funktion_slot_select_authenticated on public.impressum_funktion_slot;
create policy impressum_funktion_slot_select_authenticated on public.impressum_funktion_slot
    for select to authenticated
    using (
        public.is_productive_admin_or_vorstand()
        or not public.is_demo_or_reviewer()
        or public.is_demo_impressum_slot_scope(mitglied_id)
    );

drop policy if exists mitglied_admin_full on public.mitglied;
create policy mitglied_admin_full on public.mitglied
    to authenticated
    using (public.is_productive_admin_or_vorstand())
    with check (public.is_productive_admin_or_vorstand());

drop policy if exists mitglied_demo_admin_full on public.mitglied;
create policy mitglied_demo_admin_full on public.mitglied
    to authenticated
    using (public.is_restricted_demo_admin_or_vorstand() and coalesce(is_demo, false) = true)
    with check (public.is_restricted_demo_admin_or_vorstand() and coalesce(is_demo, false) = true);

drop policy if exists mitglied_select_own_or_admin on public.mitglied;
create policy mitglied_select_own_or_admin on public.mitglied
    for select to authenticated
    using (
        public.is_productive_admin_or_vorstand()
        or (
            (id = public.current_mitglied_id() or (auth_user_id is not null and auth_user_id = auth.uid()))
            and (
                not public.is_demo_or_reviewer()
                or coalesce(is_demo, false) = true
            )
        )
    );

drop policy if exists mitglied_saison_admin_full on public.mitglied_saison;
create policy mitglied_saison_admin_full on public.mitglied_saison
    to authenticated
    using (public.is_productive_admin_or_vorstand())
    with check (public.is_productive_admin_or_vorstand());

drop policy if exists mitglied_saison_demo_admin_full on public.mitglied_saison;
create policy mitglied_saison_demo_admin_full on public.mitglied_saison
    to authenticated
    using (public.is_restricted_demo_admin_or_vorstand() and public.is_demo_mitglied_id(mitglied_id))
    with check (public.is_restricted_demo_admin_or_vorstand() and public.is_demo_mitglied_id(mitglied_id));

drop policy if exists mitglied_saison_select_own_or_admin on public.mitglied_saison;
create policy mitglied_saison_select_own_or_admin on public.mitglied_saison
    for select to authenticated
    using (
        public.is_productive_admin_or_vorstand()
        or (
            mitglied_id = public.current_mitglied_id()
            and (
                not public.is_demo_or_reviewer()
                or public.is_demo_mitglied_id(mitglied_id)
            )
        )
    );

drop policy if exists parzelle_admin_full on public.parzelle;
create policy parzelle_admin_full on public.parzelle
    to authenticated
    using (public.is_productive_admin_or_vorstand())
    with check (public.is_productive_admin_or_vorstand());

drop policy if exists parzelle_demo_admin_full on public.parzelle;
create policy parzelle_demo_admin_full on public.parzelle
    to authenticated
    using (public.is_restricted_demo_admin_or_vorstand() and coalesce(is_demo, false) = true)
    with check (public.is_restricted_demo_admin_or_vorstand() and coalesce(is_demo, false) = true);

drop policy if exists parzelle_select_assigned_or_admin on public.parzelle;
create policy parzelle_select_assigned_or_admin on public.parzelle
    for select to authenticated
    using (
        public.is_productive_admin_or_vorstand()
        or (
            exists (
                select 1
                from public.parzellen_belegung pb
                where pb.parzelle_id = parzelle.id
                  and pb.mitglied_id = public.current_mitglied_id()
            )
            and (
                not public.is_demo_or_reviewer()
                or coalesce(parzelle.is_demo, false) = true
            )
        )
    );

drop policy if exists parzellen_belegung_admin_full on public.parzellen_belegung;
create policy parzellen_belegung_admin_full on public.parzellen_belegung
    to authenticated
    using (public.is_productive_admin_or_vorstand())
    with check (public.is_productive_admin_or_vorstand());

drop policy if exists parzellen_belegung_demo_admin_full on public.parzellen_belegung;
create policy parzellen_belegung_demo_admin_full on public.parzellen_belegung
    to authenticated
    using (
        public.is_restricted_demo_admin_or_vorstand()
        and public.is_demo_member_parzelle_scope(mitglied_id, parzelle_id)
    )
    with check (
        public.is_restricted_demo_admin_or_vorstand()
        and public.is_demo_member_parzelle_scope(mitglied_id, parzelle_id)
    );

drop policy if exists parzellen_belegung_select_own_or_admin on public.parzellen_belegung;
create policy parzellen_belegung_select_own_or_admin on public.parzellen_belegung
    for select to authenticated
    using (
        public.is_productive_admin_or_vorstand()
        or (
            mitglied_id = public.current_mitglied_id()
            and (
                not public.is_demo_or_reviewer()
                or public.is_demo_member_parzelle_scope(mitglied_id, parzelle_id)
            )
        )
    );

drop policy if exists termin_admin_full on public.termin;
create policy termin_admin_full on public.termin
    to authenticated
    using (public.is_productive_admin_or_vorstand())
    with check (public.is_productive_admin_or_vorstand());

drop policy if exists termin_demo_admin_full on public.termin;
create policy termin_demo_admin_full on public.termin
    to authenticated
    using (public.is_restricted_demo_admin_or_vorstand() and coalesce(is_demo, false) = true)
    with check (public.is_restricted_demo_admin_or_vorstand() and coalesce(is_demo, false) = true);

drop policy if exists termin_select_visible_authenticated on public.termin;
create policy termin_select_visible_authenticated on public.termin
    for select to authenticated
    using (
        public.is_productive_admin_or_vorstand()
        or (
            aktiv = true
            and (sichtbar_ab is null or sichtbar_ab <= now()::timestamp without time zone)
            and (sichtbar_bis is null or sichtbar_bis >= now()::timestamp without time zone)
            and (
                (public.is_demo_or_reviewer() and coalesce(is_demo, false) = true)
                or ((not public.is_demo_or_reviewer()) and coalesce(is_demo, false) = false)
            )
        )
    );

drop policy if exists wartungsvertraege_admin_full on public.wartungsvertraege;
create policy wartungsvertraege_admin_full on public.wartungsvertraege
    to authenticated
    using (public.is_productive_admin_or_vorstand())
    with check (public.is_productive_admin_or_vorstand());

drop policy if exists wartungsvertraege_demo_admin_full on public.wartungsvertraege;
create policy wartungsvertraege_demo_admin_full on public.wartungsvertraege
    to authenticated
    using (public.is_restricted_demo_admin_or_vorstand() and coalesce(is_demo, false) = true)
    with check (public.is_restricted_demo_admin_or_vorstand() and coalesce(is_demo, false) = true);

drop policy if exists wartungsvertraege_select_assigned_or_admin on public.wartungsvertraege;
create policy wartungsvertraege_select_assigned_or_admin on public.wartungsvertraege
    for select to authenticated
    using (
        public.is_productive_admin_or_vorstand()
        or (
            exists (
                select 1
                from public.wartungsvertrag_zuordnungen wz
                where wz.wartungsvertrag_id = wartungsvertraege.id
                  and wz.hauptmitglied_id = public.current_mitglied_id()
            )
            and (
                not public.is_demo_or_reviewer()
                or coalesce(wartungsvertraege.is_demo, false) = true
            )
        )
    );

drop policy if exists wartungsvertrag_zuordnungen_admin_full on public.wartungsvertrag_zuordnungen;
create policy wartungsvertrag_zuordnungen_admin_full on public.wartungsvertrag_zuordnungen
    to authenticated
    using (public.is_productive_admin_or_vorstand())
    with check (public.is_productive_admin_or_vorstand());

drop policy if exists wartungsvertrag_zuordnungen_demo_admin_full on public.wartungsvertrag_zuordnungen;
create policy wartungsvertrag_zuordnungen_demo_admin_full on public.wartungsvertrag_zuordnungen
    to authenticated
    using (
        public.is_restricted_demo_admin_or_vorstand()
        and public.is_demo_member_wartungsvertrag_scope(hauptmitglied_id, wartungsvertrag_id)
    )
    with check (
        public.is_restricted_demo_admin_or_vorstand()
        and public.is_demo_member_wartungsvertrag_scope(hauptmitglied_id, wartungsvertrag_id)
    );

drop policy if exists wartungsvertrag_zuordnungen_select_own_or_admin on public.wartungsvertrag_zuordnungen;
create policy wartungsvertrag_zuordnungen_select_own_or_admin on public.wartungsvertrag_zuordnungen
    for select to authenticated
    using (
        public.is_productive_admin_or_vorstand()
        or (
            hauptmitglied_id = public.current_mitglied_id()
            and (
                not public.is_demo_or_reviewer()
                or public.is_demo_member_wartungsvertrag_scope(hauptmitglied_id, wartungsvertrag_id)
            )
        )
    );

drop policy if exists zaehler_ablesung_admin_full on public.zaehler_ablesung;
create policy zaehler_ablesung_admin_full on public.zaehler_ablesung
    to authenticated
    using (public.is_productive_admin_or_vorstand())
    with check (public.is_productive_admin_or_vorstand());

drop policy if exists zaehler_ablesung_demo_admin_full on public.zaehler_ablesung;
create policy zaehler_ablesung_demo_admin_full on public.zaehler_ablesung
    to authenticated
    using (public.is_restricted_demo_admin_or_vorstand() and public.is_demo_zaehler_id(zaehler_id))
    with check (public.is_restricted_demo_admin_or_vorstand() and public.is_demo_zaehler_id(zaehler_id));

drop policy if exists zaehler_ablesung_select_assigned_or_admin on public.zaehler_ablesung;
create policy zaehler_ablesung_select_assigned_or_admin on public.zaehler_ablesung
    for select to authenticated
    using (
        public.is_productive_admin_or_vorstand()
        or (
            exists (
                select 1
                from public.zaehler z
                join public.parzellen_belegung pb
                  on pb.parzelle_id = z.parzelle_id
                where z.id = zaehler_ablesung.zaehler_id
                  and pb.mitglied_id = public.current_mitglied_id()
            )
            and (
                not public.is_demo_or_reviewer()
                or public.is_demo_zaehler_id(zaehler_id)
            )
        )
    );

drop policy if exists zaehler_admin_full on public.zaehler;
create policy zaehler_admin_full on public.zaehler
    to authenticated
    using (public.is_productive_admin_or_vorstand())
    with check (public.is_productive_admin_or_vorstand());

drop policy if exists zaehler_demo_admin_full on public.zaehler;
create policy zaehler_demo_admin_full on public.zaehler
    to authenticated
    using (public.is_restricted_demo_admin_or_vorstand() and public.is_demo_parzelle_id(parzelle_id))
    with check (public.is_restricted_demo_admin_or_vorstand() and public.is_demo_parzelle_id(parzelle_id));

drop policy if exists zaehler_select_assigned_or_admin on public.zaehler;
create policy zaehler_select_assigned_or_admin on public.zaehler
    for select to authenticated
    using (
        public.is_productive_admin_or_vorstand()
        or (
            exists (
                select 1
                from public.parzellen_belegung pb
                where pb.parzelle_id = zaehler.parzelle_id
                  and pb.mitglied_id = public.current_mitglied_id()
            )
            and (
                not public.is_demo_or_reviewer()
                or public.is_demo_parzelle_id(parzelle_id)
            )
        )
    );
