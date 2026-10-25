-- WV-4: the principal remains the relationship/capacity context.  The new
-- column is the actual contractual party; existing NULL values are deliberately
-- not backfilled because the former normalization lost that information.
alter table public.wartungsvertrag_zuordnungen add column if not exists vertragspartei_mitglied_id bigint references public.mitglied(id) on delete restrict;
comment on column public.wartungsvertrag_zuordnungen.vertragspartei_mitglied_id is 'Tatsächliche Vertragspartei. NULL bei Altbestand bedeutet unbekannt, nicht Hauptmitglied.';
create index if not exists ix_wvz_vertragspartei on public.wartungsvertrag_zuordnungen(vertragspartei_mitglied_id);

create or replace function public.validate_wartungsvertrag_vertragspartei() returns trigger language plpgsql security definer set search_path=public,pg_temp as $$
declare p public.mitglied%rowtype;
begin
 if new.vertragspartei_mitglied_id is null then
   if tg_op='INSERT' then raise exception 'Für neue Wartungsverträge ist die tatsächliche Vertragspartei erforderlich' using errcode='23514'; end if;
   return new;
 end if;
 select * into p from public.mitglied where id=new.vertragspartei_mitglied_id;
 if not found or (p.id<>new.hauptmitglied_id and p.hauptmitglied_id is distinct from new.hauptmitglied_id) then raise exception 'Die Wartungsvertragspartei muss das Hauptmitglied oder dessen Nebenmitglied sein' using errcode='23514'; end if;
 return new;
end $$;
drop trigger if exists trg_validate_wartungsvertrag_vertragspartei on public.wartungsvertrag_zuordnungen;
create trigger trg_validate_wartungsvertrag_vertragspartei before insert or update of hauptmitglied_id,vertragspartei_mitglied_id on public.wartungsvertrag_zuordnungen for each row execute function public.validate_wartungsvertrag_vertragspartei();

create or replace function public.validate_wartungsvertrag_folgezuordnung() returns trigger language plpgsql security definer set search_path=public,pg_temp as $$
declare v public.wartungsvertrag_zuordnungen%rowtype;
begin
 if new.vorgaenger_zuordnung_id is null then return new; end if;
 select * into v from public.wartungsvertrag_zuordnungen where id=new.vorgaenger_zuordnung_id;
 if not found or v.gueltig_bis is null then raise exception 'Die Vorgängerzuordnung muss ein festes Vertragsende besitzen.' using errcode='23514'; end if;
 if new.hauptmitglied_id is distinct from v.hauptmitglied_id or new.vertragspartei_mitglied_id is distinct from v.vertragspartei_mitglied_id then raise exception 'Eine Folgezuordnung muss dieselbe Vertragspartei und denselben Hauptmitgliedskontext besitzen.' using errcode='23514'; end if;
 if new.gueltig_ab<>v.gueltig_bis+1 then raise exception 'Eine Folgezuordnung muss am Tag nach dem Vertragsende beginnen.' using errcode='23514'; end if; return new;
end $$;
drop trigger if exists trg_validate_wartungsvertrag_folgezuordnung on public.wartungsvertrag_zuordnungen;
create trigger trg_validate_wartungsvertrag_folgezuordnung before insert or update of vorgaenger_zuordnung_id,hauptmitglied_id,vertragspartei_mitglied_id,gueltig_ab on public.wartungsvertrag_zuordnungen for each row execute function public.validate_wartungsvertrag_folgezuordnung();

create or replace function public.validate_wartungsvertrag_kommunikationsvorgang_link() returns trigger language plpgsql security definer set search_path=public,pg_temp as $$
declare z public.wartungsvertrag_zuordnungen%rowtype;
begin
 if new.wartungsvertrag_zuordnung_id is null then return new; end if;
 select * into z from public.wartungsvertrag_zuordnungen where id=new.wartungsvertrag_zuordnung_id;
 if not found or z.vertragspartei_mitglied_id is null or new.mitglied_id is distinct from z.vertragspartei_mitglied_id or new.parzelle_id is not null then raise exception 'Der Wartungsvertragsvorgang passt nicht zur tatsächlichen Vertragspartei.' using errcode='23514'; end if;
 if (new.vorgangstyp='vertrag_signatur' and new.dokumenttyp='wartungsvertrag' and z.vorgaenger_zuordnung_id is null) or (new.vorgangstyp='dokument_erzeugung' and new.dokumenttyp='wartungsvertrag_verlaengerung' and z.vorgaenger_zuordnung_id is not null) then return new; end if;
 raise exception 'Vorgangsart und Wartungsvertragszuordnung passen nicht zusammen.' using errcode='23514';
end $$;

-- Replaces the former normalized insertion for newly created first contracts.
create or replace function public.ensure_wartungsvertrag_prepared_contract(p_zuordnung_id bigint) returns public.kommunikationsvorgang language plpgsql security definer set search_path=public,pg_temp as $$
declare z public.wartungsvertrag_zuordnungen%rowtype; w public.wartungsvertrag_dokument_workflow%rowtype; c public.kommunikationsvorgang%rowtype;
begin
 if not public.is_admin_or_vorstand() or auth.uid() is null then raise exception 'Nur administrative Vertragsbearbeitung darf Wartungsverträge vorbereiten.' using errcode='42501'; end if;
 select * into z from public.wartungsvertrag_zuordnungen where id=p_zuordnung_id; if not found or z.vertragspartei_mitglied_id is null then raise exception 'Die tatsächliche Vertragspartei des Wartungsvertrags ist nicht bestimmt.' using errcode='23514'; end if;
 perform public.ensure_wartungsvertrag_dokument_workflow(p_zuordnung_id); select * into w from public.wartungsvertrag_dokument_workflow where wartungsvertrag_zuordnung_id=p_zuordnung_id for update;
 if w.kommunikationsvorgang_id is not null then select * into c from public.kommunikationsvorgang where id=w.kommunikationsvorgang_id; return c; end if;
 insert into public.kommunikationsvorgang(vorgangstyp,dokumenttyp,status,mitglied_id,wartungsvertrag_zuordnung_id,entwurf_daten,revision,zugewiesen_an) values('vertrag_signatur','wartungsvertrag','vorbereitet',z.vertragspartei_mitglied_id,z.id,jsonb_build_object('schema_version','kgv-prepared-contract/v1','document_type','wartungsvertrag','memberId',z.vertragspartei_mitglied_id,'wartungsvertrag_zuordnung_id',z.id,'hauptmitglied_id',z.hauptmitglied_id,'vertragspartei_mitglied_id',z.vertragspartei_mitglied_id),1,auth.uid()) returning * into c;
 update public.wartungsvertrag_dokument_workflow set kommunikationsvorgang_id=c.id,status='dokument_ausstehend' where id=w.id; return c;
end $$;

-- Paper evidence is captured in the prepared draft before signature/finalization;
-- after signatur start the existing immutable-draft guard deliberately prevents edits.
create or replace function public.dokumentiere_prepared_contract_papieruebergabe(p_kommunikationsvorgang_id bigint,p_dokumenttyp text,p_fassung text,p_uebergabe_am timestamptz,p_bemerkung text default null) returns public.kommunikationsvorgang language plpgsql security definer set search_path=public,pg_temp as $$
declare v public.kommunikationsvorgang%rowtype; a jsonb; required text[]; f_id bigint;
begin
 if not public.is_admin_or_vorstand() or auth.uid() is null then raise exception 'Keine Berechtigung für Papierübergabe' using errcode='42501'; end if;
 select * into v from public.kommunikationsvorgang where id=p_kommunikationsvorgang_id for update;
 required:=case v.dokumenttyp when 'mitgliedsantrag' then array['satzung'] when 'pachtvertrag' then array['kleingartenordnung','beitragsordnung','arbeitsordnung'] else array[]::text[] end;
 if v.status<>'vorbereitet' or not p_dokumenttyp=any(required) or nullif(btrim(p_fassung),'') is null or p_uebergabe_am is null then raise exception 'Papierübergabe kann nur vor Signatur für eine Pflichtanlage dokumentiert werden' using errcode='23514'; end if;
 select id into f_id from public.vereinsdokument_fassung where dokumenttyp_code=p_dokumenttyp and fassung=btrim(p_fassung); if f_id is null then raise exception 'Die angegebene Papierfassung ist nicht als Vereinsdokumentfassung bekannt' using errcode='23514'; end if;
 select jsonb_build_object('dokumenttyp',p_dokumenttyp,'vereinsdokument_fassung_id',f_id,'fassung',p_fassung,'bereitstellungsart','papier','bereitgestellt_am',p_uebergabe_am,'bemerkung',nullif(btrim(p_bemerkung),'')) into a;
 update public.kommunikationsvorgang set entwurf_daten=jsonb_set(v.entwurf_daten,'{vereinsunterlagenNachweis}',coalesce((select jsonb_agg(case when x->>'dokumenttyp'=p_dokumenttyp then a else x end) from jsonb_array_elements(coalesce(v.entwurf_daten->'vereinsunterlagenNachweis','[]')) x),'[]'::jsonb),true),revision=v.revision+1 where id=v.id returning * into v; return v;
end $$;
revoke all on function public.dokumentiere_prepared_contract_papieruebergabe(bigint,text,text,timestamptz,text) from public,anon;
grant execute on function public.dokumentiere_prepared_contract_papieruebergabe(bigint,text,text,timestamptz,text) to authenticated,service_role;
