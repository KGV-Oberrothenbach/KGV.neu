-- NB-1. Commit this migration; do not apply it without separate approval.
alter table public.mitglied add column if not exists mitgliedsnummer bigint generated always as (id + 1000) stored;
create unique index if not exists ux_mitglied_mitgliedsnummer on public.mitglied(mitgliedsnummer);
create table if not exists public.fachnummer_reservierung (
  is_demo boolean not null, nummer integer not null, art text not null check (art in ('mitgliedsantrag','pachtvertrag','historischer_kombivertrag','wartungsvertrag')),
  quelle_art text not null check (quelle_art in ('kommunikationsvorgang','historisches_dokument')),
  quelle_id bigint not null, reserviert_am timestamptz not null default now(), reserviert_von uuid default auth.uid(),
  primary key (is_demo, nummer), unique(is_demo, quelle_art, quelle_id),
  check ((art='mitgliedsantrag' and nummer between 10000 and 19999) or (art='pachtvertrag' and nummer between 20000 and 29999) or (art='historischer_kombivertrag' and nummer between 80000 and 89999) or (art='wartungsvertrag' and nummer between 90000 and 99999))
);
create sequence if not exists public.fachnummer_mitgliedsantrag_seq minvalue 10000 maxvalue 19999 start 10000 no cycle;
create sequence if not exists public.fachnummer_pachtvertrag_seq minvalue 20000 maxvalue 29999 start 20000 no cycle;
create sequence if not exists public.fachnummer_historisch_seq minvalue 80000 maxvalue 89999 start 80000 no cycle;
create sequence if not exists public.fachnummer_wartungsvertrag_seq minvalue 90000 maxvalue 99999 start 90000 no cycle;
create sequence if not exists public.fachnummer_mitgliedsantrag_demo_seq minvalue 10000 maxvalue 19999 start 10000 no cycle;
create sequence if not exists public.fachnummer_pachtvertrag_demo_seq minvalue 20000 maxvalue 29999 start 20000 no cycle;
create sequence if not exists public.fachnummer_historisch_demo_seq minvalue 80000 maxvalue 89999 start 80000 no cycle;
create sequence if not exists public.fachnummer_wartungsvertrag_demo_seq minvalue 90000 maxvalue 99999 start 90000 no cycle;
create or replace function public.reserve_fachnummer(p_art text,p_quelle_art text,p_quelle_id bigint,p_is_demo boolean) returns integer language plpgsql security definer set search_path=public,pg_temp as $$ declare n integer; s regclass; begin
 if not public.is_admin_or_vorstand() and auth.role()<>'service_role' then raise exception 'Keine Berechtigung für Nummernvergabe' using errcode='42501'; end if;
 select nummer into n from public.fachnummer_reservierung where is_demo=p_is_demo and quelle_art=p_quelle_art and quelle_id=p_quelle_id; if n is not null then return n; end if;
 s:=case when p_is_demo then case p_art when 'mitgliedsantrag' then 'public.fachnummer_mitgliedsantrag_demo_seq'::regclass when 'pachtvertrag' then 'public.fachnummer_pachtvertrag_demo_seq'::regclass when 'historischer_kombivertrag' then 'public.fachnummer_historisch_demo_seq'::regclass when 'wartungsvertrag' then 'public.fachnummer_wartungsvertrag_demo_seq'::regclass end else case p_art when 'mitgliedsantrag' then 'public.fachnummer_mitgliedsantrag_seq'::regclass when 'pachtvertrag' then 'public.fachnummer_pachtvertrag_seq'::regclass when 'historischer_kombivertrag' then 'public.fachnummer_historisch_seq'::regclass when 'wartungsvertrag' then 'public.fachnummer_wartungsvertrag_seq'::regclass end end; if s is null then raise exception 'Unbekannte Nummernart' using errcode='22023'; end if;
 n:=nextval(s); insert into public.fachnummer_reservierung(is_demo,nummer,art,quelle_art,quelle_id) values(p_is_demo,n,p_art,p_quelle_art,p_quelle_id) returning nummer into n; return n; end $$;
create or replace function public.trg_reserve_vertragsnummer() returns trigger language plpgsql security definer set search_path=public,pg_temp as $$ declare a text; demo boolean; begin
 if new.status='zur_signatur' and old.status is distinct from 'zur_signatur' then a:=case new.dokumenttyp when 'mitgliedsantrag' then 'mitgliedsantrag' when 'pachtvertrag' then 'pachtvertrag' when 'wartungsvertrag' then 'wartungsvertrag' end; if a is not null then select coalesce(is_demo,false) into demo from public.mitglied where id=new.mitglied_id; perform public.reserve_fachnummer(a,'kommunikationsvorgang',new.id,demo); end if; end if; return new; end $$;
drop trigger if exists trg_reserve_vertragsnummer on public.kommunikationsvorgang;
create trigger trg_reserve_vertragsnummer before update of status on public.kommunikationsvorgang for each row execute function public.trg_reserve_vertragsnummer();
revoke all on table public.fachnummer_reservierung from public, anon, authenticated;
grant select on table public.fachnummer_reservierung to authenticated, service_role;
revoke all on function public.reserve_fachnummer(text,text,bigint,boolean) from public, anon;
grant execute on function public.reserve_fachnummer(text,text,bigint,boolean) to authenticated, service_role;
