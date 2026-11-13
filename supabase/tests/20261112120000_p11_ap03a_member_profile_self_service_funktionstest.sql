-- P11 AP-03.1: local authenticated self-service and effective administration.
begin;
insert into auth.users(id,aud,role,email,encrypted_password,email_confirmed_at,raw_app_meta_data,raw_user_meta_data,created_at,updated_at) values
 ('00000000-0000-0000-0000-00000000b311','authenticated','authenticated','p11-b31-own@example.test','x',now(),'{}','{}',now(),now()),
 ('00000000-0000-0000-0000-00000000b312','authenticated','authenticated','p11-b31-other@example.test','x',now(),'{}','{}',now(),now()),
 ('00000000-0000-0000-0000-00000000b313','authenticated','authenticated','p11-b31-manager@example.test','x',now(),'{}','{}',now(),now()),
 ('00000000-0000-0000-0000-00000000b314','authenticated','authenticated','p11-b31-revoked@example.test','x',now(),'{}','{}',now(),now());
insert into public.mitglied(name,vorname,aktiv,aufnahme_status,auth_user_id,email,adresse,plz,ort) values
 ('P11 B31 Own','Test',true,'aufgenommen','00000000-0000-0000-0000-00000000b311','p11-b31-own@example.test','Altweg 1','00000','Altort'),
 ('P11 B31 Other','Test',true,'aufgenommen','00000000-0000-0000-0000-00000000b312','p11-b31-other@example.test','Fremdweg 1','11111','Fremdort'),
 ('P11 B31 Manager','Test',true,'aufgenommen','00000000-0000-0000-0000-00000000b313','p11-b31-manager@example.test','Verwaltungsweg 1','22222','Verwaltungsort'),
 ('P11 B31 Revoked','Test',true,'aufgenommen','00000000-0000-0000-0000-00000000b314','p11-b31-revoked@example.test','Entzugsweg 1','33333','Entzugsort');
create temporary table p11_b31_ids(own_id bigint,other_id bigint) on commit drop;
insert into p11_b31_ids select (select id from public.mitglied where auth_user_id='00000000-0000-0000-0000-00000000b311'),(select id from public.mitglied where auth_user_id='00000000-0000-0000-0000-00000000b312');
insert into public.app_user(user_id,mitglied_id,role,permission_grants,permission_revocations)
select '00000000-0000-0000-0000-00000000b311'::uuid,own_id,'user',0,0 from p11_b31_ids
union all select '00000000-0000-0000-0000-00000000b312'::uuid,other_id,'user',0,0 from p11_b31_ids
union all select '00000000-0000-0000-0000-00000000b313'::uuid,null,'user',4,0
union all select '00000000-0000-0000-0000-00000000b314'::uuid,null,'admin',0,4
on conflict(user_id) do update set mitglied_id=excluded.mitglied_id,role=excluded.role,permission_grants=excluded.permission_grants,permission_revocations=excluded.permission_revocations;
grant select on p11_b31_ids to authenticated;
set local role authenticated;
do $$ declare v p11_b31_ids%rowtype; r public.mitglied; begin
 select * into v from p11_b31_ids; perform set_config('request.jwt.claim.role','authenticated',true); perform set_config('request.jwt.claim.sub','00000000-0000-0000-0000-00000000b311',true);
 select * into r from public.update_own_member_contact('Neuweg 2','99999','Neuort','0123','0456');
 if r.id<>v.own_id or r.adresse<>'Neuweg 2' or r.telefon<>'0123' then raise exception 'P11 B31 own contact update failed'; end if;
 begin update public.mitglied set aufnahme_status='antrag_offen' where id=v.own_id; if found then raise exception 'P11 B31 self status patch accepted'; end if; exception when sqlstate '42501' then null; end;
 begin update public.mitglied set adresse='Fremdpatch' where id=v.other_id; if found then raise exception 'P11 B31 foreign patch accepted'; end if; exception when sqlstate '42501' then null; end;
 perform set_config('request.jwt.claim.sub','00000000-0000-0000-0000-00000000b313',true);
 update public.mitglied set adresse='Verwaltet' where id=v.other_id;
 if not found then raise exception 'P11 B31 individually granted manager denied'; end if;
 perform set_config('request.jwt.claim.sub','00000000-0000-0000-0000-00000000b314',true);
 begin update public.mitglied set adresse='Entzug' where id=v.other_id; if found then raise exception 'P11 B31 revoked admin retained edit rights'; end if; exception when sqlstate '42501' then null; end;
end $$;
reset role;
do $$ declare v p11_b31_ids%rowtype; begin select * into v from p11_b31_ids; if (select adresse from public.mitglied where id=v.other_id) <> 'Verwaltet' then raise exception 'P11 B31 denied update partially changed data'; end if; end $$;
rollback;
