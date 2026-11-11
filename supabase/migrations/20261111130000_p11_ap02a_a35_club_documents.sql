-- P11 AP-02A / A3.5: club-document administration uses effective permissions.
drop policy if exists dokumenttyp_write_admin on public.dokumenttyp;
create policy dokumenttyp_write_effective on public.dokumenttyp for all to authenticated
  using (public.has_effective_permission(134217728))
  with check (public.has_effective_permission(134217728));

create or replace function public.create_vereinsdokument_fassung(p_dokumenttyp_code text,p_fassung text,p_gueltig_ab date,p_storage_path text,p_dateiname text,p_sha256 text)
returns public.vereinsdokument_fassung language plpgsql security definer set search_path=public,pg_temp as $$
declare v_latest public.vereinsdokument_fassung; v_result public.vereinsdokument_fassung; v_has_latest boolean;
begin
 if auth.role()<>'service_role' and (auth.uid() is null or not public.has_effective_permission(268435456)) then raise exception 'Keine Berechtigung für Vereinsdokumente' using errcode='42501'; end if;
 if not exists(select 1 from public.vereinsdokument_typ where code=p_dokumenttyp_code) then raise exception 'Unbekannter Vereinsdokumenttyp: %',p_dokumenttyp_code using errcode='23503'; end if;
 if p_gueltig_ab is null or length(btrim(p_fassung))=0 or length(btrim(p_storage_path))=0 or length(btrim(p_dateiname))=0 or length(btrim(p_sha256))=0 or p_sha256 !~ '^[0-9a-f]{64}$' or p_storage_path not like 'vereinsunterlagen/'||p_dokumenttyp_code||'/%' then raise exception 'Ungültige Dokumentfassung' using errcode='23514'; end if;
 perform pg_advisory_xact_lock(hashtextextended(p_dokumenttyp_code,0)); perform 1 from public.vereinsdokument_fassung where dokumenttyp_code=p_dokumenttyp_code for update;
 select * into v_latest from public.vereinsdokument_fassung where dokumenttyp_code=p_dokumenttyp_code order by gueltig_ab desc limit 1; v_has_latest:=found;
 if v_has_latest and p_gueltig_ab<=v_latest.gueltig_ab then raise exception 'Eine Nachfolgefassung muss nach der zeitlich letzten Fassung beginnen' using errcode='23514'; end if;
 if v_has_latest and v_latest.gueltig_bis is null then update public.vereinsdokument_fassung set gueltig_bis=p_gueltig_ab-1 where id=v_latest.id; end if;
 insert into public.vereinsdokument_fassung(dokumenttyp_code,fassung,gueltig_ab,storage_path,dateiname,sha256) values(p_dokumenttyp_code,p_fassung,p_gueltig_ab,p_storage_path,p_dateiname,p_sha256) returning * into v_result; return v_result;
end $$;

create or replace function public.create_mitglied_vereinsdokument_bereitstellungen_papier(p_mitglied_id bigint,p_vereinsdokument_fassung_ids bigint[],p_bereitgestellt_am timestamptz)
returns setof public.mitglied_vereinsdokument_bereitstellung language plpgsql security definer set search_path=public,pg_temp as $$
declare v_count integer; v_actor uuid;
begin
 if auth.role()<>'authenticated' or auth.uid() is null or not public.has_effective_permission(268435456) then raise exception 'Keine Berechtigung für Vereinsdokumente' using errcode='42501'; end if; v_actor:=auth.uid();
 if p_mitglied_id is null or p_bereitgestellt_am is null or coalesce(array_length(p_vereinsdokument_fassung_ids,1),0)=0 or p_bereitgestellt_am>now() then raise exception 'Ungültige Papierübergabe' using errcode='22023'; end if;
 if not exists(select 1 from public.mitglied where id=p_mitglied_id) then raise exception 'Unbekanntes Mitglied: %',p_mitglied_id using errcode='P0002'; end if;
 if cardinality(p_vereinsdokument_fassung_ids)<>cardinality(array(select distinct unnest(p_vereinsdokument_fassung_ids))) then raise exception 'Jede Dokumentfassung darf nur einmal übergeben werden' using errcode='22023'; end if;
 select count(*) into v_count from public.vereinsdokument_fassung where id=any(p_vereinsdokument_fassung_ids); if v_count<>cardinality(p_vereinsdokument_fassung_ids) then raise exception 'Mindestens eine Dokumentfassung ist unbekannt' using errcode='23503'; end if;
 return query insert into public.mitglied_vereinsdokument_bereitstellung(mitglied_id,vereinsdokument_fassung_id,bereitstellungsart,bereitgestellt_am,bereitgestellt_von) select p_mitglied_id,id,'papier',p_bereitgestellt_am,v_actor from unnest(p_vereinsdokument_fassung_ids) requested(id) returning *;
end $$;

create or replace function public.create_mitglied_vereinsdokument_bereitstellungen_digital(p_mitglied_id bigint,p_vereinsdokument_fassung_ids bigint[],p_bereitgestellt_von uuid)
returns setof public.mitglied_vereinsdokument_bereitstellung language plpgsql security definer set search_path=public,pg_temp as $$
declare v_count integer;
begin
 if auth.role()<>'service_role' then raise exception 'Die digitale Bereitstellung ist nur serverseitig zulässig' using errcode='42501'; end if;
 if p_mitglied_id is null or p_bereitgestellt_von is null or coalesce(array_length(p_vereinsdokument_fassung_ids,1),0)=0 then raise exception 'Mitglied, Fassungen und auslösender Benutzer sind erforderlich' using errcode='22023'; end if;
 if not exists(select 1 from public.app_user au join public.mitglied m on m.id=au.mitglied_id and m.auth_user_id=au.user_id where au.user_id=p_bereitgestellt_von and (((public.permission_role_base(au.role)|au.permission_grants)&~au.permission_revocations)&268435456)=268435456) then raise exception 'Der auslösende Benutzer ist nicht berechtigt' using errcode='42501'; end if;
 if not exists(select 1 from public.mitglied where id=p_mitglied_id) then raise exception 'Unbekanntes Mitglied: %',p_mitglied_id using errcode='P0002'; end if;
 if cardinality(p_vereinsdokument_fassung_ids)<>cardinality(array(select distinct unnest(p_vereinsdokument_fassung_ids))) then raise exception 'Jede Dokumentfassung darf nur einmal übergeben werden' using errcode='22023'; end if;
 select count(*) into v_count from public.vereinsdokument_fassung where id=any(p_vereinsdokument_fassung_ids); if v_count<>cardinality(p_vereinsdokument_fassung_ids) then raise exception 'Mindestens eine Dokumentfassung ist unbekannt' using errcode='23503'; end if;
 return query insert into public.mitglied_vereinsdokument_bereitstellung(mitglied_id,vereinsdokument_fassung_id,bereitstellungsart,bereitgestellt_am,bereitgestellt_von) select p_mitglied_id,id,'digital',now(),p_bereitgestellt_von from unnest(p_vereinsdokument_fassung_ids) requested(id) returning *;
end $$;

create or replace function public.save_dokumenttyp(p_dokumenttyp_id bigint,p_bezeichnung text,p_aktiv boolean,p_sortierung integer,p_wirkungen text[] default '{}'::text[])
returns table(id bigint,bezeichnung text,aktiv boolean,sortierung integer) language plpgsql security definer set search_path=public,pg_temp as $$
declare v_id bigint; v_wirkungen text[]:=coalesce(p_wirkungen,'{}'::text[]);
begin
 if auth.role()<>'service_role' and (auth.uid() is null or not public.has_effective_permission(134217728)) then raise exception 'Keine Berechtigung für Dokumenttypen' using errcode='42501'; end if;
 if nullif(btrim(p_bezeichnung),'') is null then raise exception 'Eine Bezeichnung ist erforderlich.'; end if;
 if exists(select 1 from unnest(v_wirkungen) requested(wirkung) where requested.wirkung not in ('mitgliedsantrag_nachweis','pachtvertrag_nachweis','wartungsvertrag_nachweis','pachtbezug_erforderlich','wartungsvertragsbezug_erforderlich')) then raise exception 'Eine Dokumenttyp-Wirkung ist ungültig.'; end if;
 if (select count(*) from unnest(v_wirkungen))<>(select count(distinct wirkung) from unnest(v_wirkungen) item(wirkung)) then raise exception 'Eine Dokumenttyp-Wirkung wurde mehrfach angegeben.'; end if;
 if p_dokumenttyp_id is null then v_id:=nextval('public.dokumenttyp_id_seq'); insert into public.dokumenttyp(id,code,bezeichnung,aktiv,sortierung) values(v_id,format('dokumenttyp_%s',v_id),btrim(p_bezeichnung),coalesce(p_aktiv,true),p_sortierung); else update public.dokumenttyp dt set bezeichnung=btrim(p_bezeichnung),aktiv=coalesce(p_aktiv,true),sortierung=p_sortierung where dt.id=p_dokumenttyp_id returning dt.id into v_id; if not found then raise exception 'Der Dokumenttyp existiert nicht.'; end if; end if;
 delete from public.dokumenttyp_wirkung where dokumenttyp_id=v_id; insert into public.dokumenttyp_wirkung(dokumenttyp_id,wirkung) select v_id,item.wirkung from unnest(v_wirkungen) item(wirkung);
 return query select dt.id,dt.bezeichnung,dt.aktiv,dt.sortierung from public.dokumenttyp dt where dt.id=v_id;
end $$;
