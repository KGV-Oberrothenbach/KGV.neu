-- P11 A4.1.  The existing maintenance workspace uses CanEditAllMembers (4).
-- Contract definitions remain readable only to their assigned main member or
-- to a user with that effective maintenance-management capability.
alter table public.wartungsvertraege enable row level security;

drop policy if exists wartungsvertraege_read_effective_or_own on public.wartungsvertraege;
drop policy if exists wartungsvertraege_write_effective on public.wartungsvertraege;
create policy wartungsvertraege_read_effective_or_own on public.wartungsvertraege for select to authenticated
  using (public.has_effective_permission(4) or exists (select 1 from public.wartungsvertrag_zuordnungen z where z.wartungsvertrag_id=wartungsvertraege.id and z.hauptmitglied_id=public.current_mitglied_id()));
create policy wartungsvertraege_write_effective on public.wartungsvertraege for all to authenticated
  using (public.has_effective_permission(4)) with check (public.has_effective_permission(4));

drop policy if exists wartungsvertrag_zuordnungen_admin_full on public.wartungsvertrag_zuordnungen;
drop policy if exists wartungsvertrag_zuordnungen_select_own_or_admin on public.wartungsvertrag_zuordnungen;
drop policy if exists wartungsvertrag_zuordnungen_select_own_or_manage_effective on public.wartungsvertrag_zuordnungen;
drop policy if exists wartungsvertrag_zuordnungen_insert_effective on public.wartungsvertrag_zuordnungen;
drop policy if exists wartungsvertrag_zuordnungen_update_effective on public.wartungsvertrag_zuordnungen;
create policy wartungsvertrag_zuordnungen_select_own_or_manage_effective on public.wartungsvertrag_zuordnungen for select to authenticated
  using (public.has_effective_permission(4) or hauptmitglied_id=public.current_mitglied_id());
create policy wartungsvertrag_zuordnungen_insert_effective on public.wartungsvertrag_zuordnungen for insert to authenticated
  with check (public.has_effective_permission(4));
create policy wartungsvertrag_zuordnungen_update_effective on public.wartungsvertrag_zuordnungen for update to authenticated
  using (public.has_effective_permission(4)) with check (public.has_effective_permission(4));

drop policy if exists wartungsvertrag_dokument_workflow_admin on public.wartungsvertrag_dokument_workflow;
drop policy if exists wartungsvertrag_dokument_workflow_manage_effective on public.wartungsvertrag_dokument_workflow;
create policy wartungsvertrag_dokument_workflow_manage_effective on public.wartungsvertrag_dokument_workflow for all to authenticated
  using (public.has_effective_permission(4)) with check (public.has_effective_permission(4));
drop policy if exists wartungsvertrag_grundlegende_aenderung_admin on public.wartungsvertrag_grundlegende_aenderung;
drop policy if exists wartungsvertrag_grundlegende_aenderung_manage_effective on public.wartungsvertrag_grundlegende_aenderung;
create policy wartungsvertrag_grundlegende_aenderung_manage_effective on public.wartungsvertrag_grundlegende_aenderung for select to authenticated
  using (public.has_effective_permission(4));

-- This trigger must inspect member and assignment rows even when the caller
-- received the management capability individually and therefore has no broad
-- member-table RLS access.  It remains an integrity-only trigger.
alter function public.validate_wartungsvertrag_zuordnung() security definer;
alter function public.validate_wartungsvertrag_zuordnung() set search_path=public,pg_temp;

create or replace function public.ensure_wartungsvertrag_dokument_workflow(p_zuordnung_id bigint) returns setof public.wartungsvertrag_dokument_workflow language plpgsql security definer set search_path=public,pg_temp as $$
declare v public.wartungsvertrag_zuordnungen%rowtype;
begin
  if auth.uid() is null or not public.has_effective_permission(4) then raise exception 'CanEditAllMembers required for maintenance contract documents.' using errcode='42501'; end if;
  select * into v from public.wartungsvertrag_zuordnungen where id=p_zuordnung_id;
  if not found then raise exception 'Die Wartungsvertragszuordnung wurde nicht gefunden.' using errcode='P0002'; end if;
  insert into public.wartungsvertrag_dokument_workflow(wartungsvertrag_zuordnung_id,status,fehlermeldung) values(v.id,'dokument_ausstehend','Zuordnung gespeichert, Wartungsvertragsdokument noch nicht erzeugt: Es fehlt eine freigegebene Wartungsvertragsvorlage.') on conflict(wartungsvertrag_zuordnung_id) do nothing;
  return query select * from public.wartungsvertrag_dokument_workflow where wartungsvertrag_zuordnung_id=v.id;
end $$;

create or replace function public.ensure_wartungsvertrag_prepared_contract(p_zuordnung_id bigint) returns public.kommunikationsvorgang language plpgsql security definer set search_path=public,pg_temp as $$
declare a public.wartungsvertrag_zuordnungen%rowtype; w public.wartungsvertrag_dokument_workflow%rowtype; c public.kommunikationsvorgang%rowtype;
begin
  if auth.uid() is null or not public.has_effective_permission(4) then raise exception 'CanEditAllMembers required for maintenance contract preparation.' using errcode='42501'; end if;
  select * into a from public.wartungsvertrag_zuordnungen where id=p_zuordnung_id; if not found then raise exception 'Die Wartungsvertragszuordnung wurde nicht gefunden.' using errcode='P0002'; end if;
  perform public.ensure_wartungsvertrag_dokument_workflow(p_zuordnung_id); select * into w from public.wartungsvertrag_dokument_workflow where wartungsvertrag_zuordnung_id=p_zuordnung_id for update;
  if w.kommunikationsvorgang_id is not null then select * into c from public.kommunikationsvorgang where id=w.kommunikationsvorgang_id; if not found then raise exception 'Der verknüpfte Wartungsvertragsvorgang fehlt.' using errcode='23503'; end if; return c; end if;
  insert into public.kommunikationsvorgang(vorgangstyp,dokumenttyp,status,mitglied_id,parzelle_id,wartungsvertrag_zuordnung_id,wartungsvertrag_id,entwurf_daten,revision,zugewiesen_an) values('vertrag_signatur','wartungsvertrag','vorbereitet',a.vertragspartei_mitglied_id,null,a.id,a.wartungsvertrag_id,jsonb_build_object('schema_version','kgv-prepared-contract/v1','document_type','wartungsvertrag','memberId',a.vertragspartei_mitglied_id,'wartungsvertrag_zuordnung_id',a.id,'wartungsvertrag_id',a.wartungsvertrag_id,'gueltig_ab',a.gueltig_ab::text,'gueltig_bis',a.gueltig_bis::text,'wartungsvertrag_titel',(select titel from public.wartungsvertraege where id=a.wartungsvertrag_id),'wartungsvertrag_beschreibung',(select beschreibung from public.wartungsvertraege where id=a.wartungsvertrag_id),'wartungsvertrag_bereich',(select bereich from public.wartungsvertraege where id=a.wartungsvertrag_id),'befreit_von_pflichtstunden',(select befreit_von_pflichtstunden from public.wartungsvertraege where id=a.wartungsvertrag_id),'arbeitsstunden_gutschrift',(select arbeitsstunden_gutschrift from public.wartungsvertraege where id=a.wartungsvertrag_id),'bemerkung',coalesce(a.bemerkung,(select bemerkung from public.wartungsvertraege where id=a.wartungsvertrag_id))),1,auth.uid()) returning * into c;
  update public.wartungsvertrag_dokument_workflow set kommunikationsvorgang_id=c.id,status='dokument_ausstehend',fehlermeldung=null where id=w.id; return c;
end $$;

create or replace function public.start_wartungsvertrag_grundlegende_aenderung(p_vorgaenger_zuordnung_id bigint,p_titel text,p_beschreibung text,p_bereich text,p_max_aktive_zuordnungen integer,p_befreit_von_pflichtstunden boolean,p_arbeitsstunden_gutschrift numeric,p_gueltig_ab date,p_bemerkung text default null) returns table(kommunikationsvorgang_id bigint,wartungsvertrag_id bigint,status text) language plpgsql security definer set search_path=public,pg_temp as $$
declare o public.wartungsvertrag_zuordnungen%rowtype; oc public.wartungsvertraege%rowtype; ch public.wartungsvertrag_grundlegende_aenderung%rowtype; nc public.wartungsvertraege%rowtype; kv public.kommunikationsvorgang%rowtype;
begin
  if auth.uid() is null or not public.has_effective_permission(4) then raise exception 'CanEditAllMembers required for a material maintenance contract change.' using errcode='42501'; end if;
  select * into o from public.wartungsvertrag_zuordnungen where id=p_vorgaenger_zuordnung_id for update; if not found or (o.gueltig_bis is not null and o.gueltig_bis<current_date) then raise exception 'Die Ausgangszuordnung ist nicht mehr aktuell.' using errcode='23514'; end if; if o.vertragspartei_mitglied_id is null then raise exception 'Die tatsächliche Vertragspartei des Altvertrags ist ungeklärt; es wird nicht geraten.' using errcode='23514'; end if; if exists(select 1 from public.wartungsvertrag_zuordnungen s where s.vorgaenger_zuordnung_id=o.id) then raise exception 'Für die Ausgangszuordnung besteht bereits eine Folgezuordnung.' using errcode='23514'; end if;
  select * into ch from public.wartungsvertrag_grundlegende_aenderung where vorgaenger_zuordnung_id=o.id and status='vorbereitet' for update; if found then select * into kv from public.kommunikationsvorgang where id=ch.kommunikationsvorgang_id; if not found then raise exception 'Der vorgemerkte Signaturvorgang fehlt.' using errcode='23503'; end if; return query select kv.id,ch.wartungsvertrag_id,kv.status; return; end if;
  if nullif(btrim(p_titel),'') is null or p_max_aktive_zuordnungen<1 or p_arbeitsstunden_gutschrift<0 or (p_befreit_von_pflichtstunden and p_arbeitsstunden_gutschrift>0) or p_gueltig_ab is null or p_gueltig_ab<current_date or p_gueltig_ab<=o.gueltig_ab then raise exception 'Die Vertragsdaten oder der neue Gültigkeitsbeginn sind ungültig.' using errcode='22023'; end if;
  select * into oc from public.wartungsvertraege where id=o.wartungsvertrag_id for key share; if not found then raise exception 'Die Vertragsdefinition des Altvertrags fehlt.' using errcode='23503'; end if;
  insert into public.wartungsvertraege(titel,beschreibung,bereich,max_aktive_zuordnungen,befreit_von_pflichtstunden,arbeitsstunden_gutschrift,aktiv,bemerkung,is_demo) values(btrim(p_titel),nullif(btrim(p_beschreibung),''),nullif(btrim(p_bereich),''),p_max_aktive_zuordnungen,p_befreit_von_pflichtstunden,p_arbeitsstunden_gutschrift,false,nullif(btrim(p_bemerkung),''),oc.is_demo) returning * into nc;
  insert into public.wartungsvertrag_grundlegende_aenderung(vorgaenger_zuordnung_id,wartungsvertrag_id) values(o.id,nc.id) returning * into ch;
  insert into public.kommunikationsvorgang(vorgangstyp,dokumenttyp,status,mitglied_id,parzelle_id,wartungsvertrag_id,entwurf_daten,revision,zugewiesen_an) values('vertrag_signatur','wartungsvertrag','vorbereitet',o.vertragspartei_mitglied_id,null,nc.id,jsonb_build_object('schema_version','kgv-prepared-contract/v1','document_type','wartungsvertrag','memberId',o.vertragspartei_mitglied_id,'wartungsvertrag_id',nc.id,'wartungsvertrag_zuordnung_id',null,'vorgaenger_zuordnung_id',o.id,'gueltig_ab',p_gueltig_ab::text,'gueltig_bis',null,'wartungsvertrag_titel',nc.titel,'wartungsvertrag_beschreibung',nc.beschreibung,'wartungsvertrag_bereich',nc.bereich,'befreit_von_pflichtstunden',nc.befreit_von_pflichtstunden,'arbeitsstunden_gutschrift',nc.arbeitsstunden_gutschrift,'bemerkung',nc.bemerkung),1,auth.uid()) returning * into kv;
  update public.wartungsvertrag_grundlegende_aenderung set kommunikationsvorgang_id=kv.id where id=ch.id; return query select kv.id,nc.id,kv.status;
end $$;

create or replace function public.guard_wartungsvertrag_history() returns trigger language plpgsql set search_path=public,pg_temp as $$
begin
  if exists(select 1 from public.wartungsvertrag_zuordnungen z where z.wartungsvertrag_id=old.id) and row(new.titel,new.beschreibung,new.bereich,new.max_aktive_zuordnungen,new.befreit_von_pflichtstunden,new.arbeitsstunden_gutschrift,new.aktiv,new.bemerkung) is distinct from row(old.titel,old.beschreibung,old.bereich,old.max_aktive_zuordnungen,old.befreit_von_pflichtstunden,old.arbeitsstunden_gutschrift,old.aktiv,old.bemerkung) then raise exception 'Wartungsverträge mit Zuordnungen sind historisch und dürfen nicht direkt geändert werden; eine grundlegende Änderung erzeugt eine neue Fassung.' using errcode='55000'; end if;
  return new;
end $$;
drop trigger if exists trg_guard_wartungsvertrag_history on public.wartungsvertraege;
create trigger trg_guard_wartungsvertrag_history before update on public.wartungsvertraege for each row execute function public.guard_wartungsvertrag_history();
create or replace function public.guard_wartungsvertrag_zuordnung_history() returns trigger language plpgsql set search_path=public,pg_temp as $$
begin if row(new.wartungsvertrag_id,new.hauptmitglied_id,new.vertragspartei_mitglied_id,new.gueltig_ab,new.vorgaenger_zuordnung_id) is distinct from row(old.wartungsvertrag_id,old.hauptmitglied_id,old.vertragspartei_mitglied_id,old.gueltig_ab,old.vorgaenger_zuordnung_id) then raise exception 'Historische Wartungsvertragspartei, Beginn und Vertragsbezug sind unveränderlich.' using errcode='55000'; end if; return new; end $$;
drop trigger if exists trg_guard_wartungsvertrag_zuordnung_history on public.wartungsvertrag_zuordnungen;
create trigger trg_guard_wartungsvertrag_zuordnung_history before update on public.wartungsvertrag_zuordnungen for each row execute function public.guard_wartungsvertrag_zuordnung_history();

revoke all on function public.ensure_wartungsvertrag_dokument_workflow(bigint),public.ensure_wartungsvertrag_prepared_contract(bigint),public.start_wartungsvertrag_grundlegende_aenderung(bigint,text,text,text,integer,boolean,numeric,date,text) from public,anon;
grant execute on function public.ensure_wartungsvertrag_dokument_workflow(bigint),public.ensure_wartungsvertrag_prepared_contract(bigint),public.start_wartungsvertrag_grundlegende_aenderung(bigint,text,text,text,integer,boolean,numeric,date,text) to authenticated,service_role;
