-- G6: ausschließlich auf Demo/TestDB ausführen. Alle Testdaten werden zurückgerollt.
begin;

do $$
declare
  v_user uuid := gen_random_uuid(); v_member bigint; v_parcels bigint[]; v_parcel_base bigint;
  v_sn_a bigint; v_sn_b bigint; v_sn_c bigint; v_ss bigint; v_wn bigint; v_ws bigint;
  v_usn1 bigint; v_usn2 bigint; v_uss bigint; v_uwn bigint; v_uws bigint;
  v_value numeric; v_ok boolean; v_code text;
begin
  insert into auth.users(id,aud,role,email,encrypted_password,email_confirmed_at,raw_app_meta_data,raw_user_meta_data,created_at,updated_at)
  values(v_user,'authenticated','authenticated','g6-'||v_user::text||'@test.invalid','',now(),'{"provider":"email","providers":["email"]}','{}',now(),now());
  select coalesce(max(id),0)+1000000 into v_member from public.mitglied;
  insert into public.mitglied(id,name,vorname,auth_user_id,role,aufnahme_status)
  values(v_member,'G6 Test','Verbrauch',v_user,'user','aufgenommen');
  insert into public.app_user(user_id,mitglied_id,role,is_protected,permission_grants,permission_revocations)
  values(v_user,v_member,'user',false,96,0)
  on conflict(user_id) do update set mitglied_id=excluded.mitglied_id, permission_grants=excluded.permission_grants, permission_revocations=excluded.permission_revocations;
  perform set_config('request.jwt.claim.sub',v_user::text,true);
  perform set_config('request.jwt.claim.role','authenticated',true);

  if exists(select 1 from public.zaehler where ist_hauptzaehler and status='aktiv' and ausgebaut_am is null) then
    raise exception 'G6-Test verlangt DemoDB ohne aktive Bestands-Hauptzähler.';
  end if;
  select coalesce(max(id),0)+1000000 into v_parcel_base from public.parzelle;
  v_parcels := array[v_parcel_base,v_parcel_base+1,v_parcel_base+2,v_parcel_base+3,v_parcel_base+4];
  insert into public.parzelle(id,garten_nr,"Anlage",hat_strom,hat_wasser,rfid_strom,rfid_wasser,aktiv)
  values (v_parcels[1],'CX-G6-1','Nord',true,true,'CX-G6-S-1','CX-G6-W-1',true),
    (v_parcels[2],'CX-G6-2','Nord',true,true,'CX-G6-S-2','CX-G6-W-2',true),
    (v_parcels[3],'CX-G6-3','Süd',true,true,'CX-G6-S-3','CX-G6-W-3',true),
    (v_parcels[4],'CX-G6-4','Nord',true,true,'CX-G6-S-4','CX-G6-W-4',true),
    (v_parcels[5],'CX-G6-5','Süd',true,true,'CX-G6-S-5','CX-G6-W-5',true);

  insert into public.zaehler(parzelle_id,ist_hauptzaehler,medium,anlagenbereich,zaehlernummer,eichdatum,eingebaut_am,ausgebaut_am,status) values
    (null,true,'strom','Nord','CX-G6-H-SN-A',date '2025-01-01',date '2025-01-01',date '2026-04-01','ausgebaut'),
    (null,true,'strom','Nord','CX-G6-H-SN-B',date '2026-04-01',date '2026-04-01',date '2026-07-01','ausgebaut'),
    (null,true,'strom','Nord','CX-G6-H-SN-C',date '2026-07-01',date '2026-07-01',null,'aktiv'),
    (null,true,'strom','Süd','CX-G6-H-SS',date '2025-01-01',date '2025-01-01',null,'aktiv'),
    (null,true,'wasser','Nord','CX-G6-H-WN',date '2025-01-01',date '2025-01-01',null,'aktiv'),
    (null,true,'wasser','Süd','CX-G6-H-WS',date '2025-01-01',date '2025-01-01',null,'aktiv');
  select id into v_sn_a from public.zaehler where zaehlernummer='CX-G6-H-SN-A'; select id into v_sn_b from public.zaehler where zaehlernummer='CX-G6-H-SN-B'; select id into v_sn_c from public.zaehler where zaehlernummer='CX-G6-H-SN-C';
  select id into v_ss from public.zaehler where zaehlernummer='CX-G6-H-SS'; select id into v_wn from public.zaehler where zaehlernummer='CX-G6-H-WN'; select id into v_ws from public.zaehler where zaehlernummer='CX-G6-H-WS';
  insert into public.zaehler(parzelle_id,medium,zaehlernummer,eichdatum,eingebaut_am,status) values
    (v_parcels[1],'strom','CX-G6-U-SN-1',date '2025-01-01',date '2025-01-01','aktiv'),(v_parcels[2],'strom','CX-G6-U-SN-2',date '2026-05-01',date '2026-05-01','aktiv'),
    (v_parcels[3],'strom','CX-G6-U-SS',date '2025-01-01',date '2025-01-01','aktiv'),(v_parcels[4],'wasser','CX-G6-U-WN',date '2025-01-01',date '2025-01-01','aktiv'),(v_parcels[5],'wasser','CX-G6-U-WS',date '2025-01-01',date '2025-01-01','aktiv');
  select id into v_usn1 from public.zaehler where zaehlernummer='CX-G6-U-SN-1'; select id into v_usn2 from public.zaehler where zaehlernummer='CX-G6-U-SN-2'; select id into v_uss from public.zaehler where zaehlernummer='CX-G6-U-SS'; select id into v_uwn from public.zaehler where zaehlernummer='CX-G6-U-WN'; select id into v_uws from public.zaehler where zaehlernummer='CX-G6-U-WS';
  insert into public.zaehler_ablesung(zaehler_id,ablesedatum,stand,art,freigegeben,pruefstatus) values
    (v_sn_a,date '2025-12-31',100,'jea',true,'freigegeben'),(v_sn_a,date '2026-04-01',150,'ausbau',true,'freigegeben'),(v_sn_b,date '2026-04-01',10,'einbau',true,'freigegeben'),(v_sn_b,date '2026-07-01',50,'ausbau',true,'freigegeben'),(v_sn_c,date '2026-07-01',5,'einbau',true,'freigegeben'),(v_sn_c,date '2026-12-31',45,'jea',true,'freigegeben'),
    (v_ss,date '2025-12-31',100,'jea',true,'freigegeben'),(v_ss,date '2026-12-31',140,'jea',true,'freigegeben'),(v_wn,date '2025-12-31',100,'jea',true,'freigegeben'),(v_wn,date '2026-12-31',130,'jea',true,'freigegeben'),(v_ws,date '2025-12-31',100,'jea',true,'freigegeben'),(v_ws,date '2026-12-31',120,'jea',true,'freigegeben'),
    (v_usn1,date '2025-12-31',10,'jea',true,'freigegeben'),(v_usn1,date '2026-12-31',50,'jea',true,'freigegeben'),(v_usn2,date '2026-05-01',5,'einbau',true,'freigegeben'),(v_usn2,date '2026-12-31',20,'jea',true,'freigegeben'),(v_uss,date '2025-12-31',20,'jea',true,'freigegeben'),(v_uss,date '2026-12-31',45,'jea',true,'freigegeben'),(v_uwn,date '2025-12-31',10,'jea',true,'freigegeben'),(v_uwn,date '2026-12-31',30,'jea',true,'freigegeben'),(v_uws,date '2025-12-31',5,'jea',true,'freigegeben'),(v_uws,date '2026-12-31',15,'jea',true,'freigegeben');

  select hauptzaehler_verbrauch, hauptzaehler_berechenbar into v_value,v_ok from public.rpc_jahresverbrauch_haupt_und_unterzaehler(2026) where medium='strom' and anlagenbereich='Nord';
  if v_value<>130 or not v_ok then raise exception 'Strom Nord Hauptzählerwechsel falsch: %, %',v_value,v_ok; end if;
  select unterzaehler_verbrauch, unterzaehler_berechenbar, unterzaehler_fehlercode into v_value,v_ok,v_code from public.rpc_jahresverbrauch_haupt_und_unterzaehler(2026) where medium='strom' and anlagenbereich='Nord';
  if v_value<>55 or not v_ok then raise exception 'Strom Nord Unterzähler-Summe falsch: %, %, %',v_value,v_ok,v_code; end if;
  if (select count(*) from public.rpc_jahresverbrauch_haupt_und_unterzaehler(2026) where hauptzaehler_berechenbar and unterzaehler_berechenbar) <> 4 then raise exception 'Nicht alle vier Kombinationen sind berechenbar.'; end if;
  select differenz, berechenbar into v_value,v_ok from public.rpc_jahresverbrauch_haupt_unter_differenz(2026) where medium='strom' and anlagenbereich='Nord';
  if v_value<>75 or not v_ok then raise exception 'G7-Differenz Strom Nord falsch: %, %',v_value,v_ok; end if;

  -- Negative Differenz, fehlende JEA, nicht freigegebene und fehlende Ausbauablesung bleiben sichtbar.
  update public.zaehler_ablesung set stand=115 where zaehler_id=v_wn and art='jea' and ablesedatum::date=date '2026-12-31';
  select pruefbeduerftig into v_ok from public.rpc_jahresverbrauch_haupt_unter_differenz(2026) where medium='wasser' and anlagenbereich='Nord';
  if not v_ok then raise exception 'Negative G7-Differenz wurde nicht als prüfbedürftig markiert.'; end if;
  update public.zaehler_ablesung set stand=90 where zaehler_id=v_wn and art='jea' and ablesedatum::date=date '2026-12-31';
  select fehlercode into v_code from public.rpc_hauptzaehler_jahresverbrauch_segmente(2026) where zaehler_id=v_wn;
  if v_code <> 'NEGATIVER_VERBRAUCH' then raise exception 'Negative Differenz wurde nicht erkannt: %',v_code; end if;
  update public.zaehler_ablesung set freigegeben=false,pruefstatus='eingereicht' where zaehler_id=v_ws and art='jea' and ablesedatum::date=date '2026-12-31';
  select fehlercode into v_code from public.rpc_hauptzaehler_jahresverbrauch_segmente(2026) where zaehler_id=v_ws;
  if v_code <> 'ABLESUNG_NICHT_FREIGEGEBEN' then raise exception 'Nicht freigegebene Ablesung wurde nicht erkannt: %',v_code; end if;
  delete from public.zaehler_ablesung where zaehler_id=v_ss and art='jea' and ablesedatum::date=date '2026-12-31';
  select fehlercode into v_code from public.rpc_hauptzaehler_jahresverbrauch_segmente(2026) where zaehler_id=v_ss;
  if v_code <> 'ENDABLESUNG_FEHLT' then raise exception 'Fehlende Jahresablesung wurde nicht erkannt: %',v_code; end if;
  delete from public.zaehler_ablesung where zaehler_id=v_sn_b and art='ausbau';
  select fehlercode into v_code from public.rpc_hauptzaehler_jahresverbrauch_segmente(2026) where zaehler_id=v_sn_b;
  if v_code <> 'ENDABLESUNG_FEHLT' then raise exception 'Fehlende Ausbauablesung wurde nicht erkannt: %',v_code; end if;
end;
$$;
rollback;
