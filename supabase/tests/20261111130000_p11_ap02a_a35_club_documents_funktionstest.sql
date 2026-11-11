-- P11 A3.5: synthetic local PostgreSQL test; all fixture data is rolled back.
begin;
insert into auth.users(id,aud,role,email,encrypted_password,email_confirmed_at,raw_app_meta_data,raw_user_meta_data,created_at,updated_at) values
 ('00000000-0000-0000-0000-000000000f51','authenticated','authenticated','p11-a35-grant@example.test','x',now(),'{}','{}',now(),now()),
 ('00000000-0000-0000-0000-000000000f52','authenticated','authenticated','p11-a35-denied@example.test','x',now(),'{}','{}',now(),now()),
 ('00000000-0000-0000-0000-000000000f53','authenticated','authenticated','p11-a35-revoked@example.test','x',now(),'{}','{}',now(),now()),
 ('00000000-0000-0000-0000-000000000f54','authenticated','authenticated','p11-a35-type@example.test','x',now(),'{}','{}',now(),now());
insert into public.mitglied(name,vorname,aktiv,aufnahme_status) values('P11 A35 Mitglied','Test',true,'aufgenommen') returning id as member_id \gset
insert into public.app_user(user_id,role,permission_grants,permission_revocations) values
 ('00000000-0000-0000-0000-000000000f51','user',268435456,0),('00000000-0000-0000-0000-000000000f52','user',0,0),('00000000-0000-0000-0000-000000000f53','admin',0,268435456),('00000000-0000-0000-0000-000000000f54','user',134217728,0);
create temporary table p11_a35(id bigint,member_id bigint) on commit drop; insert into p11_a35(member_id) values (:'member_id'); grant select,insert,update on p11_a35 to authenticated;
set local role authenticated; select set_config('request.jwt.claim.sub','00000000-0000-0000-0000-000000000f51',true); select set_config('request.jwt.claim.role','authenticated',true);
do $$ declare x public.vereinsdokument_fassung; begin x:=public.create_vereinsdokument_fassung('satzung','P11-A35',current_date+1000,'vereinsunterlagen/satzung/a.pdf','a.pdf',repeat('a',64)); update p11_a35 set id=x.id; perform public.create_mitglied_vereinsdokument_bereitstellungen_papier((select member_id from p11_a35),array[x.id],now()-interval '1 minute'); end $$;
select set_config('request.jwt.claim.sub','00000000-0000-0000-0000-000000000f52',true); do $$ begin perform public.create_vereinsdokument_fassung('satzung','P11-A35-x',current_date+1001,'vereinsunterlagen/satzung/b.pdf','b.pdf',repeat('b',64)); raise exception 'missing right succeeded'; exception when sqlstate '42501' then null; end $$;
select set_config('request.jwt.claim.sub','00000000-0000-0000-0000-000000000f53',true); do $$ begin perform public.create_mitglied_vereinsdokument_bereitstellungen_papier((select member_id from p11_a35),array[(select id from p11_a35)],now()); raise exception 'revoked admin succeeded'; exception when sqlstate '42501' then null; end $$;
select set_config('request.jwt.claim.sub','00000000-0000-0000-0000-000000000f54',true); do $$ begin perform public.save_dokumenttyp(null,'P11 A35 Typ',true,1,array[]::text[]); end $$;
reset role;
do $$ begin if (select count(*) from public.mitglied_vereinsdokument_bereitstellung where mitglied_id=(select member_id from p11_a35))<>1 then raise exception 'delivery missing'; end if; end $$;
rollback;
