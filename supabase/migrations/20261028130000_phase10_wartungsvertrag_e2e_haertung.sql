-- Phase 10: retry-safe extension notices and explicit contract-party detail.
-- Source only. This migration is deliberately not executed by this change.

drop function if exists public.get_wartungsvertrag_folgeentscheidung_detail(bigint);
create function public.get_wartungsvertrag_folgeentscheidung_detail(p_vorgaenger_zuordnung_id bigint)
returns table(
  vorgaenger_zuordnung_id bigint,
  hauptmitglied_id bigint,
  vertragspartei_mitglied_id bigint,
  wartungsvertrag_id bigint,
  gueltig_ab date,
  gueltig_bis date,
  folgeentscheidung_id bigint,
  entscheidung text,
  entscheidungsdatum date,
  vorstandsbeschlussdatum date,
  initiator text,
  begruendung text,
  folgezuordnung_id bigint,
  folge_wartungsvertrag_id bigint,
  folge_wartungsvertrag_titel text,
  folge_gueltig_ab date,
  folge_gueltig_bis date,
  kommunikationsvorgang_id bigint,
  workflow_id bigint,
  workflow_art text,
  workflow_status text,
  dokument_id bigint,
  zurueckgenommen_am timestamptz,
  ruecknahmebegruendung text,
  can_decide boolean,
  can_withdraw_nichtverlaengerung boolean
)
language sql
stable
security definer
set search_path=public,pg_temp as $$
  select z.id,z.hauptmitglied_id,z.vertragspartei_mitglied_id,z.wartungsvertrag_id,
    z.gueltig_ab,z.gueltig_bis,e.id,e.entscheidung,e.entscheidungsdatum,
    e.vorstandsbeschlussdatum,e.initiator,e.begruendung,e.folgezuordnung_id,
    f.wartungsvertrag_id,fw.titel,f.gueltig_ab,f.gueltig_bis,
    e.kommunikationsvorgang_id,wd.id,wd.workflow_art,wd.status,wd.dokument_id,
    e.zurueckgenommen_am,e.ruecknahmebegruendung,
    (public.ist_wartungsvertrag_entscheidung_berechtigt()
      and z.gueltig_bis>current_date
      and (e.id is null or e.zurueckgenommen_am is not null)),
    (public.ist_wartungsvertrag_entscheidung_berechtigt()
      and e.entscheidung='nicht_verlaengern'
      and e.zurueckgenommen_am is null
      and z.gueltig_bis>current_date)
  from public.wartungsvertrag_zuordnungen z
  left join public.wartungsvertrag_folgeentscheidung e
    on e.vorgaenger_zuordnung_id=z.id
  left join public.wartungsvertrag_zuordnungen f on f.id=e.folgezuordnung_id
  left join public.wartungsvertraege fw on fw.id=f.wartungsvertrag_id
  left join public.wartungsvertrag_dokument_workflow wd on wd.folgeentscheidung_id=e.id
  where z.id=p_vorgaenger_zuordnung_id;
$$;
revoke all on function public.get_wartungsvertrag_folgeentscheidung_detail(bigint)
  from public,anon;
grant execute on function public.get_wartungsvertrag_folgeentscheidung_detail(bigint)
  to authenticated,service_role;
create or replace function public.complete_wartungsvertrag_verlaengerungsmitteilung(
  p_kommunikationsvorgang_id bigint,
  p_dokument_id bigint
)
returns void
language plpgsql
security definer
set search_path=public,pg_temp as $$
declare
  v_vorgang public.kommunikationsvorgang%rowtype;
  v_dokument public.dokument%rowtype;
begin
  if auth.role() is distinct from 'service_role' then
    raise exception 'Der Abschluss der Verlängerungsmitteilung ist nur serverseitig zulässig.'
      using errcode='42501';
  end if;

  select * into v_vorgang
  from public.kommunikationsvorgang
  where id=p_kommunikationsvorgang_id
  for update;
  if not found
     or v_vorgang.vorgangstyp<>'dokument_erzeugung'
     or v_vorgang.dokumenttyp<>'wartungsvertrag_verlaengerung' then
    raise exception 'Verlängerungsmitteilung ist nicht abschließbar.' using errcode='23514';
  end if;

  select * into v_dokument
  from public.dokument
  where id=p_dokument_id
    and source_kommunikationsvorgang_id=v_vorgang.id
    and source_revision=v_vorgang.revision
    and archiviert_at is null
  for key share;
  if not found or v_dokument.mitglied_id is distinct from v_vorgang.mitglied_id then
    raise exception 'Das Dokument passt nicht zur Verlängerungsmitteilung.' using errcode='23514';
  end if;

  if v_vorgang.status='erledigt' then
    if v_vorgang.final_dokument_id is distinct from p_dokument_id then
      raise exception 'Die abgeschlossene Verlängerungsmitteilung verweist auf ein anderes Dokument.'
        using errcode='23514';
    end if;
    return;
  end if;
  if v_vorgang.status<>'vorbereitet' then
    raise exception 'Verlängerungsmitteilung ist nicht abschließbar.' using errcode='40001';
  end if;

  update public.kommunikationsvorgang
  set final_dokument_id=p_dokument_id,status='erledigt'
  where id=v_vorgang.id;
  update public.wartungsvertrag_dokument_workflow
  set dokument_id=p_dokument_id,status='abgeschlossen',fehlermeldung=null
  where kommunikationsvorgang_id=v_vorgang.id
    and workflow_art='verlaengerungsmitteilung';
end;
$$;
revoke all on function public.complete_wartungsvertrag_verlaengerungsmitteilung(bigint,bigint)
  from public,anon,authenticated;
grant execute on function public.complete_wartungsvertrag_verlaengerungsmitteilung(bigint,bigint)
  to service_role;
create or replace function public.sync_wartungsvertrag_grundlegende_aenderung_abbruch()
returns trigger
language plpgsql
security definer
set search_path=public,pg_temp as $$
begin
  if new.dokumenttyp='wartungsvertrag'
     and new.status='abgebrochen'
     and old.status is distinct from 'abgebrochen' then
    update public.wartungsvertrag_grundlegende_aenderung
    set status='abgebrochen'
    where kommunikationsvorgang_id=new.id
      and status='vorbereitet';
  end if;
  return new;
end;
$$;
drop trigger if exists trg_sync_wartungsvertrag_grundlegende_aenderung_abbruch
  on public.kommunikationsvorgang;
create trigger trg_sync_wartungsvertrag_grundlegende_aenderung_abbruch
after update of status on public.kommunikationsvorgang
for each row execute function public.sync_wartungsvertrag_grundlegende_aenderung_abbruch();
revoke all on function public.sync_wartungsvertrag_grundlegende_aenderung_abbruch()
  from public,anon,authenticated;
alter table public.wartungsvertrag_grundlegende_aenderung
  drop constraint if exists wartungsvertrag_grundlegende_aenderung_vorgaenger_zuordnung_id_key;
create unique index if not exists ux_wvga_aktiver_versuch_je_vorgaenger
  on public.wartungsvertrag_grundlegende_aenderung(vorgaenger_zuordnung_id)
  where status='vorbereitet';
create or replace function public.start_wartungsvertrag_grundlegende_aenderung(
  p_vorgaenger_zuordnung_id bigint,
  p_titel text,
  p_beschreibung text,
  p_bereich text,
  p_max_aktive_zuordnungen integer,
  p_befreit_von_pflichtstunden boolean,
  p_arbeitsstunden_gutschrift numeric,
  p_gueltig_ab date,
  p_bemerkung text default null
)
returns table (kommunikationsvorgang_id bigint, wartungsvertrag_id bigint, status text)
language plpgsql
security definer
set search_path=public,pg_temp as $$
declare
  v_old public.wartungsvertrag_zuordnungen%rowtype;
  v_old_contract public.wartungsvertraege%rowtype;
  v_change public.wartungsvertrag_grundlegende_aenderung%rowtype;
  v_new_contract public.wartungsvertraege%rowtype;
  v_communication public.kommunikationsvorgang%rowtype;
begin
  if auth.uid() is null or not public.is_admin_or_vorstand() then
    raise exception 'Nur Admin oder Vorstand darf eine grundlegende Wartungsvertragsänderung starten.'
      using errcode='42501';
  end if;

  select * into v_old
  from public.wartungsvertrag_zuordnungen
  where id=p_vorgaenger_zuordnung_id
  for update;
  if not found or (v_old.gueltig_bis is not null and v_old.gueltig_bis<current_date) then
    raise exception 'Die Ausgangszuordnung ist nicht mehr aktuell.' using errcode='23514';
  end if;
  if v_old.vertragspartei_mitglied_id is null then
    raise exception 'Die tatsächliche Vertragspartei des Altvertrags ist ungeklärt; es wird nicht geraten.'
      using errcode='23514';
  end if;
  if exists (
    select 1 from public.wartungsvertrag_zuordnungen successor
    where successor.vorgaenger_zuordnung_id=v_old.id
  ) then
    raise exception 'Für die Ausgangszuordnung besteht bereits eine Folgezuordnung.'
      using errcode='23514';
  end if;

  select * into v_change
  from public.wartungsvertrag_grundlegende_aenderung
  where vorgaenger_zuordnung_id=v_old.id
    and status='vorbereitet'
  for update;
  if found then
    select * into v_communication
    from public.kommunikationsvorgang
    where id=v_change.kommunikationsvorgang_id;
    if not found then
      raise exception 'Der vorgemerkte Signaturvorgang fehlt.' using errcode='23503';
    end if;
    return query select v_communication.id,v_change.wartungsvertrag_id,v_communication.status;
    return;
  end if;

  if nullif(btrim(p_titel),'') is null
     or p_max_aktive_zuordnungen<1
     or p_arbeitsstunden_gutschrift<0
     or (p_befreit_von_pflichtstunden and p_arbeitsstunden_gutschrift>0)
     or p_gueltig_ab is null
     or p_gueltig_ab<current_date
     or p_gueltig_ab<=v_old.gueltig_ab then
    raise exception 'Die Vertragsdaten oder der neue Gültigkeitsbeginn sind ungültig.'
      using errcode='22023';
  end if;

  select * into v_old_contract
  from public.wartungsvertraege
  where id=v_old.wartungsvertrag_id
  for key share;
  if not found then
    raise exception 'Die Vertragsdefinition des Altvertrags fehlt.' using errcode='23503';
  end if;

  insert into public.wartungsvertraege(
    titel,beschreibung,bereich,max_aktive_zuordnungen,
    befreit_von_pflichtstunden,arbeitsstunden_gutschrift,aktiv,bemerkung,is_demo
  ) values (
    btrim(p_titel),nullif(btrim(p_beschreibung),''),nullif(btrim(p_bereich),''),
    p_max_aktive_zuordnungen,p_befreit_von_pflichtstunden,
    p_arbeitsstunden_gutschrift,false,nullif(btrim(p_bemerkung),''),v_old_contract.is_demo
  ) returning * into v_new_contract;

  insert into public.wartungsvertrag_grundlegende_aenderung(
    vorgaenger_zuordnung_id,wartungsvertrag_id
  ) values (v_old.id,v_new_contract.id)
  returning * into v_change;

  insert into public.kommunikationsvorgang(
    vorgangstyp,dokumenttyp,status,mitglied_id,parzelle_id,
    wartungsvertrag_id,entwurf_daten,revision,zugewiesen_an
  ) values (
    'vertrag_signatur','wartungsvertrag','vorbereitet',
    v_old.vertragspartei_mitglied_id,null,v_new_contract.id,
    jsonb_build_object(
      'schema_version','kgv-prepared-contract/v1',
      'document_type','wartungsvertrag',
      'memberId',v_old.vertragspartei_mitglied_id,
      'wartungsvertrag_id',v_new_contract.id,
      'wartungsvertrag_zuordnung_id',null,
      'vorgaenger_zuordnung_id',v_old.id,
      'gueltig_ab',p_gueltig_ab::text,
      'gueltig_bis',null,
      'wartungsvertrag_titel',v_new_contract.titel,
      'wartungsvertrag_beschreibung',v_new_contract.beschreibung,
      'wartungsvertrag_bereich',v_new_contract.bereich,
      'befreit_von_pflichtstunden',v_new_contract.befreit_von_pflichtstunden,
      'arbeitsstunden_gutschrift',v_new_contract.arbeitsstunden_gutschrift,
      'bemerkung',v_new_contract.bemerkung
    ),1,auth.uid()
  ) returning * into v_communication;

  update public.wartungsvertrag_grundlegende_aenderung
  set kommunikationsvorgang_id=v_communication.id
  where id=v_change.id;
  return query select v_communication.id,v_new_contract.id,v_communication.status;
end;
$$;
revoke all on function public.start_wartungsvertrag_grundlegende_aenderung(bigint,text,text,text,integer,boolean,numeric,date,text)
  from public,anon;
grant execute on function public.start_wartungsvertrag_grundlegende_aenderung(bigint,text,text,text,integer,boolean,numeric,date,text)
  to authenticated,service_role;
