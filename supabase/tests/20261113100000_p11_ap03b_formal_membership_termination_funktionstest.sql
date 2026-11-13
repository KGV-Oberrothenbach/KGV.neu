-- P11 AP-03.2: authenticated, synthetic and fully rollbackable acceptance.
begin;

create temporary table p11_ap03b_ids (granted uuid, denied uuid, revoked uuid, main_id bigint, secondary_id bigint, lease_id bigint) on commit drop;
insert into public.mitglied(name, vorname, aktiv, aufnahme_status, lockedbyuserid, lockat) values
 ('P11 AP03B Main','Test',true,'aufgenommen','00000000-0000-0000-0000-000000000321',now() at time zone 'UTC'),
 ('P11 AP03B Secondary','Test',true,'aufgenommen',null,null),
 ('P11 AP03B Denied','Test',true,'aufgenommen','00000000-0000-0000-0000-000000000322',now() at time zone 'UTC'),
 ('P11 AP03B Revoked','Test',true,'aufgenommen','00000000-0000-0000-0000-000000000323',now() at time zone 'UTC');
update public.mitglied set hauptmitglied_id=(select id from public.mitglied where name='P11 AP03B Main') where name='P11 AP03B Secondary';
insert into public.app_user(user_id, role, permission_grants, permission_revocations) values
 ('00000000-0000-0000-0000-000000000321','user',4,0),
 ('00000000-0000-0000-0000-000000000322','user',0,0),
 ('00000000-0000-0000-0000-000000000323','admin',0,4);
insert into p11_ap03b_ids select '00000000-0000-0000-0000-000000000321','00000000-0000-0000-0000-000000000322','00000000-0000-0000-0000-000000000323',
 (select id from public.mitglied where name='P11 AP03B Main'),(select id from public.mitglied where name='P11 AP03B Secondary'),null;
grant select on p11_ap03b_ids to authenticated;

set local role authenticated;
do $$ declare v p11_ap03b_ids%rowtype; r record; begin
 select * into v from p11_ap03b_ids;
 perform set_config('request.jwt.claim.sub',v.granted::text,true);
 select * into r from public.end_membership(v.main_id,'promote_secondary','schriftliche Kündigung liegt vor');
 if not r.success then raise exception 'AP03.2 authorized formal termination failed'; end if;
 perform set_config('kgv.formal_membership_termination','0',true);
 begin update public.mitglied set mitglied_ende=current_date where id=v.secondary_id; raise exception 'AP03.2 direct PATCH unexpectedly succeeded'; exception when sqlstate '42501' then null; end;
 perform set_config('request.jwt.claim.sub',v.denied::text,true);
 begin perform public.end_membership(v.main_id,null,'unberechtigt'); raise exception 'AP03.2 denied caller unexpectedly succeeded'; exception when sqlstate '42501' then null; end;
 perform set_config('request.jwt.claim.sub',v.revoked::text,true);
 begin perform public.end_membership(v.main_id,null,'entzogen'); raise exception 'AP03.2 revoked admin unexpectedly succeeded'; exception when sqlstate '42501' then null; end;
end $$;
reset role;
do $$ declare v p11_ap03b_ids%rowtype; begin
 select * into v from p11_ap03b_ids;
 if not exists(select 1 from public.mitgliedschaftsbeendigung_audit where mitglied_id=v.main_id and begruendung='schriftliche Kündigung liegt vor' and akteur_user_id=v.granted)
    or not exists(select 1 from public.mitglied where id=v.main_id and mitglied_ende=public.kgv_local_now()::date and aktiv=false)
    or not exists(select 1 from public.mitglied where id=v.secondary_id and hauptmitglied_id is null and mitglied_ende is null and aktiv=true) then
   raise exception 'AP03.2 audit or formal membership state was not persisted';
 end if;
end $$;
rollback;
