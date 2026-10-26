-- NB-2A-D3: Correct future extension snapshots without rewriting applied WV-6 history.
-- This migration is source-only until a separately approved database deployment.
create or replace function public.verlaengere_wartungsvertrag(
  p_vorgaenger_zuordnung_id bigint,
  p_neues_gueltig_bis date,
  p_vorstandsbeschlussdatum date,
  p_bemerkung text default null
)
returns table(folgeentscheidung_id bigint,folgezuordnung_id bigint,kommunikationsvorgang_id bigint,workflow_id bigint,vertragspartei_mitglied_id bigint)
language plpgsql security definer set search_path=public,pg_temp as $$
declare v_alt public.wartungsvertrag_zuordnungen%rowtype; v_neu public.wartungsvertrag_zuordnungen%rowtype; v_dec public.wartungsvertrag_folgeentscheidung%rowtype; v_vorgang public.kommunikationsvorgang%rowtype; v_workflow public.wartungsvertrag_dokument_workflow%rowtype; v_member public.mitglied%rowtype; v_contract public.wartungsvertraege%rowtype;
begin
  if auth.uid() is null or not public.ist_wartungsvertrag_entscheidung_berechtigt() then raise exception 'Keine fachliche Berechtigung zur Wartungsvertragsentscheidung.' using errcode='42501'; end if;
  if p_vorstandsbeschlussdatum is null then raise exception 'Vorstandsbeschlussdatum ist erforderlich.' using errcode='22023'; end if;
  select * into v_alt from public.wartungsvertrag_zuordnungen where id=p_vorgaenger_zuordnung_id for update;
  if not found or v_alt.gueltig_bis is null then raise exception 'Die Zuordnung ist nicht regulär verlängerbar.' using errcode='23514'; end if;
  if v_alt.vertragspartei_mitglied_id is null then raise exception 'Die tatsächliche Vertragspartei des Altvertrags ist ungeklärt; keine Verlängerung möglich.' using errcode='23514'; end if;
  select * into v_dec from public.wartungsvertrag_folgeentscheidung where vorgaenger_zuordnung_id=v_alt.id and zurueckgenommen_am is null for update;
  if found then
    if v_dec.entscheidung='verlaengern' and v_dec.folgezuordnung_id is not null then
      select * into v_neu from public.wartungsvertrag_zuordnungen where id=v_dec.folgezuordnung_id;
      if v_neu.wartungsvertrag_id=v_alt.wartungsvertrag_id and v_neu.gueltig_bis=p_neues_gueltig_bis and v_dec.vorstandsbeschlussdatum=p_vorstandsbeschlussdatum then
        select * into v_workflow from public.wartungsvertrag_dokument_workflow where folgeentscheidung_id=v_dec.id;
        return query select v_dec.id,v_neu.id,v_dec.kommunikationsvorgang_id,v_workflow.id,v_neu.vertragspartei_mitglied_id; return;
      end if;
    end if;
    raise exception 'Für diese Laufzeit besteht bereits eine wirksame Folgeentscheidung.' using errcode='23514';
  end if;
  if exists(select 1 from public.wartungsvertrag_zuordnungen where vorgaenger_zuordnung_id=v_alt.id) then raise exception 'Für diese Laufzeit besteht bereits eine Folgezuordnung.' using errcode='23514'; end if;
  if p_neues_gueltig_bis < v_alt.gueltig_bis+1 then raise exception 'Das neue Vertragsende liegt vor dem serverseitig bestimmten Beginn.' using errcode='22023'; end if;
  select * into v_contract from public.wartungsvertraege where id=v_alt.wartungsvertrag_id for key share;
  if not found then raise exception 'Der Wartungsvertrag der Vorgängerzuordnung fehlt.' using errcode='23514'; end if;
  insert into public.wartungsvertrag_zuordnungen(wartungsvertrag_id,hauptmitglied_id,vertragspartei_mitglied_id,gueltig_ab,gueltig_bis,bemerkung,vorgaenger_zuordnung_id)
  values(v_alt.wartungsvertrag_id,v_alt.hauptmitglied_id,v_alt.vertragspartei_mitglied_id,v_alt.gueltig_bis+1,p_neues_gueltig_bis,nullif(btrim(p_bemerkung),''),v_alt.id) returning * into v_neu;
  insert into public.wartungsvertrag_folgeentscheidung(vorgaenger_zuordnung_id,entscheidung,entscheidungsdatum,entschieden_von_auth_user_id,vorstandsbeschlussdatum,folgezuordnung_id)
  values(v_alt.id,'verlaengern',p_vorstandsbeschlussdatum,auth.uid(),p_vorstandsbeschlussdatum,v_neu.id) returning * into v_dec;
  select * into v_member from public.mitglied where id=v_alt.vertragspartei_mitglied_id;
  if not found or v_member.mitgliedsnummer is null or v_member.mitgliedsnummer <= 0 then raise exception 'Für die Wartungsvertragsverlängerung fehlt eine gültige Mitgliedsnummer.' using errcode='23514'; end if;
  insert into public.kommunikationsvorgang(vorgangstyp,dokumenttyp,status,mitglied_id,wartungsvertrag_zuordnung_id,entwurf_daten,revision,zugewiesen_an)
  values('dokument_erzeugung','wartungsvertrag_verlaengerung','vorbereitet',v_alt.vertragspartei_mitglied_id,v_neu.id,jsonb_build_object('schema_version','kgv-wartungsvertrag-verlaengerung/v1','template_key','wartungsvertrag-verlaengerung','template_version','v1','vorgaenger_zuordnung_id',v_alt.id,'folgezuordnung_id',v_neu.id,'folgeentscheidung_id',v_dec.id,'mitglied_id',v_alt.vertragspartei_mitglied_id,'hauptmitglied_id',v_alt.hauptmitglied_id,'mitglied_name',concat_ws(' ',v_member.vorname,v_member.name),'mitgliedsnummer',v_member.mitgliedsnummer,'wartungsvertrag_id',v_contract.id,'wartungsvertrag_titel',v_contract.titel,'wartungsvertrag_beschreibung',v_contract.beschreibung,'arbeitsstundenwirkung',case when v_contract.befreit_von_pflichtstunden then 'Befreiung von Pflichtstunden' else 'Keine Befreiung von Pflichtstunden' end,'vorgaenger_gueltig_bis',v_alt.gueltig_bis::text,'gueltig_ab',v_neu.gueltig_ab::text,'gueltig_bis',v_neu.gueltig_bis::text,'vorstandsbeschlussdatum',p_vorstandsbeschlussdatum::text,'bemerkung',p_bemerkung),1,auth.uid()) returning * into v_vorgang;
  update public.wartungsvertrag_folgeentscheidung set kommunikationsvorgang_id=v_vorgang.id where id=v_dec.id;
  update public.wartungsvertrag_dokument_workflow set workflow_art='verlaengerungsmitteilung',folgeentscheidung_id=v_dec.id,kommunikationsvorgang_id=v_vorgang.id,status='dokument_ausstehend',fehlermeldung=null where wartungsvertrag_zuordnung_id=v_neu.id returning * into v_workflow;
  return query select v_dec.id,v_neu.id,v_vorgang.id,v_workflow.id,v_neu.vertragspartei_mitglied_id;
end $$;

revoke all on function public.verlaengere_wartungsvertrag(bigint,date,date,text) from public,anon;
grant execute on function public.verlaengere_wartungsvertrag(bigint,date,date,text) to authenticated,service_role;
