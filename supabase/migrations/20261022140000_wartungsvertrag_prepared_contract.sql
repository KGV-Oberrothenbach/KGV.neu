-- Regular maintenance contracts reuse the existing assignment/document workflow and the shared prepared-signature table.
create or replace function public.ensure_wartungsvertrag_prepared_contract(p_zuordnung_id bigint)
returns public.kommunikationsvorgang
language plpgsql security definer set search_path = public, pg_temp
as $$
declare v_assignment public.wartungsvertrag_zuordnungen%rowtype; v_workflow public.wartungsvertrag_dokument_workflow%rowtype; v_contract public.kommunikationsvorgang%rowtype;
begin
  if not public.is_admin_or_vorstand() or auth.uid() is null then raise exception 'Nur administrative Vertragsbearbeitung darf Wartungsverträge vorbereiten.' using errcode='42501'; end if;
  select * into v_assignment from public.wartungsvertrag_zuordnungen where id=p_zuordnung_id;
  if not found then raise exception 'Die Wartungsvertragszuordnung wurde nicht gefunden.' using errcode='P0002'; end if;
  perform public.ensure_wartungsvertrag_dokument_workflow(p_zuordnung_id);
  select * into v_workflow from public.wartungsvertrag_dokument_workflow where wartungsvertrag_zuordnung_id=p_zuordnung_id for update;
  if v_workflow.kommunikationsvorgang_id is not null then
    select * into v_contract from public.kommunikationsvorgang where id=v_workflow.kommunikationsvorgang_id;
    if not found then raise exception 'Der verknüpfte Wartungsvertragsvorgang fehlt.' using errcode='23503'; end if;
    return v_contract;
  end if;
  insert into public.kommunikationsvorgang(vorgangstyp,dokumenttyp,status,mitglied_id,parzelle_id,wartungsvertrag_zuordnung_id,entwurf_daten,revision,zugewiesen_an)
  values('vertrag_signatur','wartungsvertrag','vorbereitet',v_assignment.hauptmitglied_id,null,v_assignment.id,
    jsonb_build_object('schema_version','kgv-prepared-contract/v1','document_type','wartungsvertrag','memberId',v_assignment.hauptmitglied_id,'wartungsvertrag_zuordnung_id',v_assignment.id,'wartungsvertrag_id',v_assignment.wartungsvertrag_id,'gueltig_ab',v_assignment.gueltig_ab::text,'gueltig_bis',case when v_assignment.gueltig_bis is null then null else v_assignment.gueltig_bis::text end),1,auth.uid()) returning * into v_contract;
  update public.wartungsvertrag_dokument_workflow set kommunikationsvorgang_id=v_contract.id,status='dokument_ausstehend',fehlermeldung=null where id=v_workflow.id;
  return v_contract;
end $$;
create or replace function public.sync_wartungsvertrag_prepared_contract_workflow()
returns trigger language plpgsql security definer set search_path = public, pg_temp as $$
begin
  if new.dokumenttyp='wartungsvertrag' and new.wartungsvertrag_zuordnung_id is not null then
    update public.wartungsvertrag_dokument_workflow set
      status=case when new.status='zur_signatur' then 'signatur_vorgang' when new.status='erledigt' then 'abgeschlossen' else status end,
      dokument_id=case when new.status='erledigt' then new.final_dokument_id else dokument_id end,
      fehlermeldung=case when new.status in ('zur_signatur','erledigt') then null else fehlermeldung end
    where wartungsvertrag_zuordnung_id=new.wartungsvertrag_zuordnung_id;
  end if;
  return new;
end $$;
drop trigger if exists trg_sync_wartungsvertrag_prepared_contract_workflow on public.kommunikationsvorgang;
create trigger trg_sync_wartungsvertrag_prepared_contract_workflow after update of status,final_dokument_id on public.kommunikationsvorgang for each row execute function public.sync_wartungsvertrag_prepared_contract_workflow();
revoke all on function public.ensure_wartungsvertrag_prepared_contract(bigint) from public,anon;
grant execute on function public.ensure_wartungsvertrag_prepared_contract(bigint) to authenticated,service_role;
