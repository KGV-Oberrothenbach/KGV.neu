-- ALT-3.4: Ausschließlich relationale Nachweisstatus für Mitgliedsantrag,
-- Pachtvertrag und Wartungsvertrag. Dateiname, Titel und Storage-Pfad sind
-- bewusst kein Bestandteil dieser Auswertung.

-- Die Wirkungen sind reine Katalogmetadaten. Lesbarkeit für authentifizierte
-- Nutzer ist nötig, damit SECURITY INVOKER-Views den Nachweis des jeweils
-- ohnehin lesbaren eigenen Dokuments korrekt einordnen können.
drop policy if exists dokumenttyp_wirkung_read_admin_vorstand on public.dokumenttyp_wirkung;
create policy dokumenttyp_wirkung_read_authenticated
on public.dokumenttyp_wirkung
for select to authenticated
using (true);

create or replace view public.v_mitglied_mitgliedsantrag_nachweisstatus
with (security_invoker = true) as
select
    m.id as mitglied_id,
    case
        when n.dokument_id is null then 'kein_nachweis'
        when n.unterzeichnet then 'unterschrieben'
        else 'nur_unsigniert'
    end as nachweisstatus,
    n.dokumentdatum,
    n.dokument_id
from public.mitglied m
left join lateral (
    select
        d.id as dokument_id,
        d.unterzeichnet,
        d.dokumentdatum
    from public.dokument d
    join public.dokumenttyp_wirkung w
      on w.dokumenttyp_id = d.dokumenttyp_id
     and w.wirkung = 'mitgliedsantrag_nachweis'
    where d.mitglied_id = m.id
      and d.archiviert_at is null
      and d.dokumenttyp_id is not null
      and d.unterzeichnet in (true, false)
    order by
        case when d.unterzeichnet then 0 else 1 end,
        d.dokumentdatum desc nulls last,
        d.id desc
    limit 1
) n on true;

create or replace view public.v_pachtverhaeltnis_pachtvertrag_nachweisstatus
with (security_invoker = true) as
select
    pv.id as pachtverhaeltnis_id,
    case
        when n.dokument_id is null then 'kein_nachweis'
        when n.unterzeichnet then 'unterschrieben'
        else 'nur_unsigniert'
    end as nachweisstatus,
    n.vertragsdatum,
    n.dokument_id,
    n.pachtvertragsfassung_id
from public.pachtverhaeltnis pv
left join lateral (
    select
        d.id as dokument_id,
        d.unterzeichnet,
        f.vertragsdatum,
        f.id as pachtvertragsfassung_id
    from public.pachtvertragsfassung f
    join public.dokument d
      on d.id = f.dokument_id
    join public.dokumenttyp_wirkung w
      on w.dokumenttyp_id = d.dokumenttyp_id
     and w.wirkung = 'pachtvertrag_nachweis'
    where f.pachtverhaeltnis_id = pv.id
      and d.archiviert_at is null
      and d.dokumenttyp_id is not null
      and d.unterzeichnet in (true, false)
    order by
        case when d.unterzeichnet then 0 else 1 end,
        f.vertragsdatum desc nulls last,
        f.id desc
    limit 1
) n on true;

create or replace view public.v_wartungsvertrag_zuordnung_nachweisstatus
with (security_invoker = true) as
select
    wz.id as wartungsvertrag_zuordnung_id,
    case
        when n.dokument_id is null then 'kein_nachweis'
        when n.unterzeichnet then 'unterschrieben'
        else 'nur_unsigniert'
    end as nachweisstatus,
    n.dokumentdatum,
    n.dokument_id
from public.wartungsvertrag_zuordnungen wz
left join lateral (
    select
        d.id as dokument_id,
        d.unterzeichnet,
        d.dokumentdatum
    from public.dokument_wartungsvertrag_zuordnung dwz
    join public.dokument d
      on d.id = dwz.dokument_id
    join public.dokumenttyp_wirkung w
      on w.dokumenttyp_id = d.dokumenttyp_id
     and w.wirkung = 'wartungsvertrag_nachweis'
    where dwz.wartungsvertrag_zuordnung_id = wz.id
      and d.archiviert_at is null
      and d.dokumenttyp_id is not null
      and d.unterzeichnet in (true, false)
    order by
        case when d.unterzeichnet then 0 else 1 end,
        d.dokumentdatum desc nulls last,
        d.id desc
    limit 1
) n on true
where public.is_admin_or_vorstand();

revoke all on table public.v_mitglied_mitgliedsantrag_nachweisstatus,
    public.v_pachtverhaeltnis_pachtvertrag_nachweisstatus,
    public.v_wartungsvertrag_zuordnung_nachweisstatus
from public, anon;
grant select on table public.v_mitglied_mitgliedsantrag_nachweisstatus,
    public.v_pachtverhaeltnis_pachtvertrag_nachweisstatus,
    public.v_wartungsvertrag_zuordnung_nachweisstatus
to authenticated;

comment on view public.v_mitglied_mitgliedsantrag_nachweisstatus is
    'Relationaler Mitgliedsantrag-Nachweisstatus je Mitglied. Unterzeichnete Nachweise haben Vorrang; danach entscheidet dokumentdatum und Dokument-ID.';
comment on view public.v_pachtverhaeltnis_pachtvertrag_nachweisstatus is
    'Relationaler Pachtvertrags-Nachweisstatus je Pachtverhältnis. Maßgeblich ist pachtvertragsfassung.vertragsdatum.';
comment on view public.v_wartungsvertrag_zuordnung_nachweisstatus is
    'Relationaler Wartungsvertrags-Nachweisstatus je Zuordnung. Maßgeblich ist dokument.dokumentdatum.';
