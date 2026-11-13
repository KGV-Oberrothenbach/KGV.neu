-- P11 AP-03.3: the admission decision needs both the configured office and
-- effective CanCreateMitglied; all synthetic changes are rolled back.
begin;

create temporary table p11_ap03c_ids (granted_user uuid, revoked_user uuid) on commit drop;
insert into public.mitglied(name, vorname, aktiv, aufnahme_status) values
  ('P11 AP03C Granted decision','Test',true,'aufgenommen'),
  ('P11 AP03C Revoked decision','Test',true,'aufgenommen');
insert into public.app_user(user_id, mitglied_id, role, permission_grants, permission_revocations) values
  ('00000000-0000-0000-0000-000000000331',(select id from public.mitglied where name='P11 AP03C Granted decision'),'user',524288,0),
  ('00000000-0000-0000-0000-000000000332',(select id from public.mitglied where name='P11 AP03C Revoked decision'),'admin',0,524288);
insert into public.vereinsfunktion(schluessel,bezeichnung,aktiv,erlaubt_mehrfachbesetzung)
values ('p11-ap03c-entscheidung','P11 AP03C Entscheidung',true,true);
insert into public.vereinsfunktion_verwendung(vereinsfunktion_id,verwendungszweck,sort_order)
select id,'mitgliedsaufnahme_entscheidung',1 from public.vereinsfunktion where schluessel='p11-ap03c-entscheidung';
insert into public.vereinsfunktion_besetzung(vereinsfunktion_id,mitglied_id,gueltig_ab,sort_order)
select f.id,m.id,current_date,1 from public.vereinsfunktion f cross join public.mitglied m
where f.schluessel='p11-ap03c-entscheidung' and m.name in ('P11 AP03C Granted decision','P11 AP03C Revoked decision');
insert into p11_ap03c_ids values ('00000000-0000-0000-0000-000000000331','00000000-0000-0000-0000-000000000332');
grant select on p11_ap03c_ids to authenticated;

set local role authenticated;
do $$
declare v p11_ap03c_ids%rowtype;
begin
  select * into v from p11_ap03c_ids;
  perform set_config('request.jwt.claim.sub',v.granted_user::text,true);
  if not public.kann_mitgliedsaufnahme_entscheiden() then
    raise exception 'AP03.3 individual CanCreateMitglied grant was not accepted for decision';
  end if;
  perform set_config('request.jwt.claim.sub',v.revoked_user::text,true);
  if public.kann_mitgliedsaufnahme_entscheiden() then
    raise exception 'AP03.3 revoked administrator retained admission decision authority';
  end if;
end;
$$;
reset role;
rollback;
