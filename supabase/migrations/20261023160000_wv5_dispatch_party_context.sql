-- WV-5: enrich the already frozen dispatch party snapshot only from the
-- assignment's explicit relational context. Never enumerate current secondary members.
create or replace function public.sync_wv_dispatch_party_context() returns trigger language plpgsql security definer set search_path=public,pg_temp as $$
declare z public.wartungsvertrag_zuordnungen%rowtype; p text; h text; rev integer;
begin
 if new.dokumenttyp not in ('wartungsvertrag','wartungsvertrag_verlaengerung') or new.final_dokument_id is null then return new; end if;
 select * into z from public.wartungsvertrag_zuordnungen where id=new.wartungsvertrag_zuordnung_id;
 if not found then return new; end if;
 rev:=coalesce(new.signatur_revision,new.revision);
 if z.vertragspartei_mitglied_id is null then return new; end if;
 select coalesce(nullif(btrim(concat_ws(' ',vorname,name)),''),'Mitglied #'||id) into p from public.mitglied where id=z.vertragspartei_mitglied_id;
 if z.vertragspartei_mitglied_id=z.hauptmitglied_id then
   update public.vertragsversand_vertragspartei_snapshot set rolle='hauptmitglied',mitglied_id=z.vertragspartei_mitglied_id,name_snapshot=p,quelle='wartungsvertrag_zuordnung' where kommunikationsvorgang_id=new.id and revision=rev and rolle='hauptmitglied';
 else
   update public.vertragsversand_vertragspartei_snapshot set rolle='nebenmitglied',mitglied_id=z.vertragspartei_mitglied_id,name_snapshot=p,quelle='wartungsvertrag_zuordnung' where kommunikationsvorgang_id=new.id and revision=rev and rolle='hauptmitglied';
   select coalesce(nullif(btrim(concat_ws(' ',vorname,name)),''),'Mitglied #'||id) into h from public.mitglied where id=z.hauptmitglied_id;
   insert into public.vertragsversand_vertragspartei_snapshot(kommunikationsvorgang_id,revision,rolle,mitglied_id,name_snapshot,quelle) values(new.id,rev,'hauptmitglied',z.hauptmitglied_id,h,'wartungsvertrag_zuordnung') on conflict(kommunikationsvorgang_id,revision,rolle) do nothing;
 end if;
 return new;
end $$;
drop trigger if exists trg_sync_wv_dispatch_party_context on public.kommunikationsvorgang;
create trigger trg_sync_wv_dispatch_party_context after update of final_dokument_id on public.kommunikationsvorgang for each row execute function public.sync_wv_dispatch_party_context();

create or replace function public.guard_wv_dispatch_party_known() returns trigger language plpgsql security definer set search_path=public,pg_temp as $$
begin
 if exists(select 1 from public.kommunikationsvorgang v join public.wartungsvertrag_zuordnungen z on z.id=v.wartungsvertrag_zuordnung_id where v.id=new.kommunikationsvorgang_id and v.dokumenttyp in ('wartungsvertrag','wartungsvertrag_verlaengerung') and z.vertragspartei_mitglied_id is null) then raise exception 'Wartungsvertrag-Altbestand: Die tatsächliche Vertragspartei ist unbekannt. Bitte vor dem Versand fachlich klären; es wird nicht geraten.' using errcode='23514'; end if;
 return new;
end $$;
drop trigger if exists trg_guard_wv_dispatch_party_known on public.versandinstanz;
create trigger trg_guard_wv_dispatch_party_known before insert on public.versandinstanz for each row execute function public.guard_wv_dispatch_party_known();
