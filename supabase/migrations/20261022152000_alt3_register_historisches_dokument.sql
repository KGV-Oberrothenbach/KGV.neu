-- ALT-3.3: Atomare, fachlich validierte Registrierung bereits hochgeladener Alt-Dokumente.
-- Der Ablauf leitet Wirkungen ausschließlich aus dokumenttyp_wirkung ab und verändert
-- weder Bestandsdokumente noch Prepared-Contract-Provenance automatisch.

create or replace function public.register_historisches_dokument(
    p_dokument_id bigint,
    p_dokumenttyp_id bigint,
    p_dokumentdatum date,
    p_unterzeichnet boolean,
    p_pachtverhaeltnis_id bigint default null,
    p_wartungsvertrag_zuordnung_id bigint default null
)
returns table (
    dokument_id bigint,
    dokumenttyp_id bigint,
    dokumentdatum date,
    unterzeichnet boolean,
    pachtvertragsfassung_id bigint,
    wartungsvertrag_zuordnung_id bigint
)
language plpgsql
security definer
set search_path = public, pg_temp
as $$
declare
    v_dokument public.dokument%rowtype;
    v_pachtverhaeltnis public.pachtverhaeltnis%rowtype;
    v_wartungszuordnung public.wartungsvertrag_zuordnungen%rowtype;
    v_pachtvertragsfassung public.pachtvertragsfassung%rowtype;
    v_hat_mitgliedsnachweis boolean := false;
    v_hat_pachtnachweis boolean := false;
    v_hat_pachtbezug boolean := false;
    v_hat_wartungsnachweis boolean := false;
    v_hat_wartungsbezug boolean := false;
    v_pacht_erforderlich boolean := false;
    v_wartung_erforderlich boolean := false;
    v_pachtvertragsfassung_anzahl bigint := 0;
    v_wartungsrelation_anzahl bigint := 0;
    v_bestehende_wartungszuordnung_id bigint;
    v_pachtvertragsfassung_id bigint;
begin
    if not coalesce(public.is_admin_or_vorstand(), false) then
        raise exception 'Nur Admin oder Vorstand darf historische Dokumente registrieren.';
    end if;

    if p_dokumentdatum is null then
        raise exception 'Ein Dokumentdatum ist erforderlich.';
    end if;

    if p_unterzeichnet is null then
        raise exception 'Der Signaturstatus muss explizit angegeben werden.';
    end if;

    select *
    into v_dokument
    from public.dokument
    where id = p_dokument_id
    for update;

    if not found then
        raise exception 'Das Dokument existiert nicht.';
    end if;

    if v_dokument.archiviert_at is not null then
        raise exception 'Archivierte Dokumente können nicht registriert werden.';
    end if;

    if v_dokument.source_kommunikationsvorgang_id is not null
       or v_dokument.source_wartungsvertrag_zuordnung_id is not null then
        raise exception 'Prepared-Dokumente mit vorhandener Provenance können nicht als Altimport registriert werden.';
    end if;

    perform 1
    from public.dokumenttyp
    where id = p_dokumenttyp_id
      and aktiv;

    if not found then
        raise exception 'Der Dokumenttyp existiert nicht oder ist inaktiv.';
    end if;

    select
        coalesce(bool_or(wirkung = 'mitgliedsantrag_nachweis'), false),
        coalesce(bool_or(wirkung = 'pachtvertrag_nachweis'), false),
        coalesce(bool_or(wirkung = 'pachtbezug_erforderlich'), false),
        coalesce(bool_or(wirkung = 'wartungsvertrag_nachweis'), false),
        coalesce(bool_or(wirkung = 'wartungsvertragsbezug_erforderlich'), false)
    into
        v_hat_mitgliedsnachweis,
        v_hat_pachtnachweis,
        v_hat_pachtbezug,
        v_hat_wartungsnachweis,
        v_hat_wartungsbezug
    from public.dokumenttyp_wirkung
    where dokumenttyp_id = p_dokumenttyp_id;

    v_pacht_erforderlich := v_hat_pachtnachweis or v_hat_pachtbezug;
    v_wartung_erforderlich := v_hat_wartungsnachweis or v_hat_wartungsbezug;

    if v_hat_mitgliedsnachweis and v_dokument.mitglied_id is null then
        raise exception 'Dieser Dokumenttyp erfordert ein dem Mitglied zugeordnetes Dokument.';
    end if;

    if v_pacht_erforderlich then
        if p_pachtverhaeltnis_id is null then
            raise exception 'Für diesen Dokumenttyp ist ein Pachtverhältnis erforderlich.';
        end if;

        if v_dokument.mitglied_id is null then
            raise exception 'Ein Pacht-Nachweis benötigt ein dem beteiligten Mitglied zugeordnetes Dokument.';
        end if;

        select *
        into v_pachtverhaeltnis
        from public.pachtverhaeltnis
        where id = p_pachtverhaeltnis_id;

        if not found then
            raise exception 'Das angegebene Pachtverhältnis existiert nicht.';
        end if;

        perform 1
        from public.pachtverhaeltnis_beteiligung
        where pachtverhaeltnis_id = v_pachtverhaeltnis.id
          and mitglied_id = v_dokument.mitglied_id
          and gueltig_ab <= p_dokumentdatum
          and (gueltig_bis is null or gueltig_bis >= p_dokumentdatum);

        if not found then
            raise exception 'Das Mitglied des Dokuments war am Dokumentdatum nicht am Pachtverhältnis beteiligt.';
        end if;
    elsif p_pachtverhaeltnis_id is not null then
        raise exception 'Der gewählte Dokumenttyp erfordert kein Pachtverhältnis.';
    end if;

    if v_wartung_erforderlich then
        if p_wartungsvertrag_zuordnung_id is null then
            raise exception 'Für diesen Dokumenttyp ist eine Wartungsvertragszuordnung erforderlich.';
        end if;

        if v_dokument.mitglied_id is null then
            raise exception 'Ein Wartungsvertrags-Nachweis benötigt ein dem Hauptmitglied zugeordnetes Dokument.';
        end if;

        select *
        into v_wartungszuordnung
        from public.wartungsvertrag_zuordnungen
        where id = p_wartungsvertrag_zuordnung_id;

        if not found then
            raise exception 'Die angegebene Wartungsvertragszuordnung existiert nicht.';
        end if;

        if v_wartungszuordnung.hauptmitglied_id <> v_dokument.mitglied_id then
            raise exception 'Das Dokument gehört nicht zum Hauptmitglied der Wartungsvertragszuordnung.';
        end if;

        if v_wartungszuordnung.gueltig_ab > p_dokumentdatum
           or (v_wartungszuordnung.gueltig_bis is not null
               and v_wartungszuordnung.gueltig_bis < p_dokumentdatum) then
            raise exception 'Die Wartungsvertragszuordnung war am Dokumentdatum nicht gültig.';
        end if;
    elsif p_wartungsvertrag_zuordnung_id is not null then
        raise exception 'Der gewählte Dokumenttyp erfordert keine Wartungsvertragszuordnung.';
    end if;

    select count(*), min(id)
    into v_pachtvertragsfassung_anzahl, v_pachtvertragsfassung_id
    from public.pachtvertragsfassung
    where dokument_id = p_dokument_id;

    if v_pachtvertragsfassung_anzahl > 0 then
        select *
        into v_pachtvertragsfassung
        from public.pachtvertragsfassung
        where dokument_id = p_dokument_id;
    end if;

    select count(*), min(wartungsvertrag_zuordnung_id)
    into v_wartungsrelation_anzahl, v_bestehende_wartungszuordnung_id
    from public.dokument_wartungsvertrag_zuordnung
    where dokument_id = p_dokument_id;

    -- Jede Wiederholung muss den bereits vollständigen, identischen Fachzustand treffen.
    if v_dokument.dokumenttyp_id is not null
       or v_dokument.dokumentdatum is not null
       or v_dokument.unterzeichnet is not null
       or v_pachtvertragsfassung_anzahl > 0
       or v_wartungsrelation_anzahl > 0 then
        if v_dokument.dokumenttyp_id is distinct from p_dokumenttyp_id
           or v_dokument.dokumentdatum is distinct from p_dokumentdatum
           or v_dokument.unterzeichnet is distinct from p_unterzeichnet
           or (v_pacht_erforderlich and (
                v_pachtvertragsfassung_anzahl <> 1
                or v_pachtvertragsfassung.pachtverhaeltnis_id is distinct from p_pachtverhaeltnis_id
                or v_pachtvertragsfassung.fassungstyp is distinct from 'import'
                or v_pachtvertragsfassung.vertragsdatum is distinct from p_dokumentdatum
           ))
           or (not v_pacht_erforderlich and v_pachtvertragsfassung_anzahl <> 0)
           or (v_wartung_erforderlich and (
                v_wartungsrelation_anzahl <> 1
                or v_bestehende_wartungszuordnung_id is distinct from p_wartungsvertrag_zuordnung_id
           ))
           or (not v_wartung_erforderlich and v_wartungsrelation_anzahl <> 0) then
            raise exception 'Das Dokument ist bereits abweichend oder unvollständig klassifiziert.';
        end if;

        return query
        select p_dokument_id, p_dokumenttyp_id, p_dokumentdatum, p_unterzeichnet,
               v_pachtvertragsfassung_id,
               case when v_wartung_erforderlich then p_wartungsvertrag_zuordnung_id else null end;
        return;
    end if;

    update public.dokument
    set dokumenttyp_id = p_dokumenttyp_id,
        dokumentdatum = p_dokumentdatum,
        unterzeichnet = p_unterzeichnet,
        updated_at = now()
    where id = p_dokument_id;

    if v_pacht_erforderlich then
        insert into public.pachtvertragsfassung (
            pachtverhaeltnis_id,
            dokument_id,
            fassungstyp,
            vertragsdatum
        )
        values (
            p_pachtverhaeltnis_id,
            p_dokument_id,
            'import',
            p_dokumentdatum
        )
        returning id into v_pachtvertragsfassung_id;
    end if;

    if v_wartung_erforderlich then
        insert into public.dokument_wartungsvertrag_zuordnung (
            dokument_id,
            wartungsvertrag_zuordnung_id
        )
        values (
            p_dokument_id,
            p_wartungsvertrag_zuordnung_id
        );
    end if;

    return query
    select p_dokument_id, p_dokumenttyp_id, p_dokumentdatum, p_unterzeichnet,
           v_pachtvertragsfassung_id,
           case when v_wartung_erforderlich then p_wartungsvertrag_zuordnung_id else null end;
end;
$$;

revoke all on function public.register_historisches_dokument(bigint, bigint, date, boolean, bigint, bigint)
    from public, anon, authenticated, service_role;
grant execute on function public.register_historisches_dokument(bigint, bigint, date, boolean, bigint, bigint)
    to authenticated;

comment on function public.register_historisches_dokument(bigint, bigint, date, boolean, bigint, bigint) is
    'Registriert ein bereits hochgeladenes historisches Dokument atomar nach den Wirkungen seines Dokumenttyps. Ausschließlich für Admin oder Vorstand.';
