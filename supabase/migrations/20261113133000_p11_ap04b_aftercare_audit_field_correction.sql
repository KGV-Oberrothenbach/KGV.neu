-- AP-04.2: Die Grundtabelle verwendet bewusst die bestehenden Beschlussfelder.
-- Die Abschlussfunktionen werden hier nachgelagert darauf ausgerichtet.

create or replace function public.beende_pacht_nachsorge(p_nachsorge_id bigint,p_art text,p_begruendung text,p_nachfolger_pachtverhaeltnis_id bigint default null)
returns boolean language plpgsql security definer set search_path=public,pg_temp as $$
declare n public.pacht_nachsorge%rowtype; caller uuid:=auth.uid(); reason text:=nullif(btrim(p_begruendung),''); successor public.pachtverhaeltnis%rowtype;
begin
 if caller is null or not public.has_effective_permission(524288) then raise exception 'Keine Berechtigung für Nachsorgeentscheidungen.' using errcode='42501'; end if;
 if p_art not in ('ordentlich','entlassung','nachpaechteruebernahme') or reason is null then raise exception 'Art und Begründung sind erforderlich.' using errcode='22023'; end if;
 select * into n from public.pacht_nachsorge where id=p_nachsorge_id for update;
 if not found then raise exception 'Nachsorge wurde nicht gefunden.' using errcode='P0002'; end if;
 if n.beendet_am is not null then return true; end if;
 if p_art='ordentlich' and public.kgv_local_now()::date<n.endet_am then raise exception 'Ordentliche Beendigung ist erst zum Nachsorgeende zulässig.' using errcode='23514'; end if;
 if p_art='nachpaechteruebernahme' then
   if p_nachfolger_pachtverhaeltnis_id is null then raise exception 'Nachfolgepachtverhältnis erforderlich.' using errcode='22023'; end if;
   select * into successor from public.pachtverhaeltnis where id=p_nachfolger_pachtverhaeltnis_id for key share;
   if not found or successor.parzelle_id<>n.parzelle_id or successor.beendet_am is not null or not exists(select 1 from public.pachtverhaeltnis_beteiligung b join public.mitglied m on m.id=b.mitglied_id where b.pachtverhaeltnis_id=successor.id and b.rolle='hauptpaechter' and b.gueltig_ab<=public.kgv_local_now()::date and (b.gueltig_bis is null or b.gueltig_bis>=public.kgv_local_now()::date) and m.aktiv and m.aufnahme_status='aufgenommen') then
     raise exception 'Es liegt keine tatsächliche Nachpächterübernahme vor.' using errcode='23514';
   end if;
 end if;
 update public.pacht_nachsorge set beendet_am=least(public.kgv_local_today(),endet_am),beendigungsgrund=p_art,beschlossen_von=caller,beschlossen_am=now() where id=n.id;
 insert into public.pacht_nachsorge_ereignis(nachsorge_id,art,begruendung,akteur_user_id,nachfolger_pachtverhaeltnis_id) values(n.id,p_art,reason,caller,p_nachfolger_pachtverhaeltnis_id);
 return true;
end $$;

create or replace function public.complete_pacht_nachsorge_on_successor()
returns trigger language plpgsql security definer set search_path=public,pg_temp as $$
declare v_parzelle_id bigint; v_akteur_user_id uuid;
begin
 if new.rolle <> 'hauptpaechter' or new.gueltig_ab > public.kgv_local_now()::date or (new.gueltig_bis is not null and new.gueltig_bis < public.kgv_local_now()::date) then return new; end if;
 select pv.parzelle_id,coalesce(auth.uid(),pv.created_by,new.created_by) into v_parzelle_id,v_akteur_user_id from public.pachtverhaeltnis pv where pv.id=new.pachtverhaeltnis_id and pv.beendet_am is null;
 if v_parzelle_id is null or v_akteur_user_id is null then return new; end if;
 with beendet as (
   update public.pacht_nachsorge n set beendet_am=least(public.kgv_local_now()::date,n.endet_am),beendigungsgrund='nachpaechteruebernahme',beschlossen_von=v_akteur_user_id,beschlossen_am=now()
    where n.parzelle_id=v_parzelle_id and n.beendet_am is null and n.beginnt_am<=public.kgv_local_now()::date
      and exists(select 1 from public.pachtverhaeltnis alt where alt.id=n.pachtverhaeltnis_id and alt.beendet_am is not null)
    returning n.id
 )
 insert into public.pacht_nachsorge_ereignis(nachsorge_id,art,begruendung,akteur_user_id,nachfolger_pachtverhaeltnis_id)
 select id,'nachpaechteruebernahme','Automatische Beendigung durch wirksame Nachpächterübernahme.',v_akteur_user_id,new.pachtverhaeltnis_id from beendet;
 return new;
end $$;
