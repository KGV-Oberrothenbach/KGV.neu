-- G7.3 DEMO-SETUP. Ausschließlich ymxhszxfutjnljiuciyj; Cleanup separat.
begin;
do $$
declare b bigint; sn bigint; ss bigint; wn bigint; ws bigint; usn bigint; uss bigint; uwn bigint; uws bigint;
begin
  if exists(select 1 from public.zaehler where zaehlernummer like 'CX-G7.3-%') then raise exception 'G7.3-Setup bereits vorhanden.'; end if;
  if exists(select 1 from public.zaehler where ist_hauptzaehler and status='aktiv' and ausgebaut_am is null) then raise exception 'Aktive Bestands-Hauptzähler werden nicht überlagert.'; end if;
  select coalesce(max(id),0)+1000000 into b from public.parzelle;
  insert into public.parzelle(id,garten_nr,"Anlage",hat_strom,hat_wasser,rfid_strom,rfid_wasser,aktiv,is_demo) values
  (b,'CX-G7.3-SN','Nord',true,false,'CX-G7.3-NO-PHYSICAL-SN',null,true,true),(b+1,'CX-G7.3-SS','Süd',true,false,'CX-G7.3-NO-PHYSICAL-SS',null,true,true),(b+2,'CX-G7.3-WN','Nord',false,true,null,'CX-G7.3-NO-PHYSICAL-WN',true,true),(b+3,'CX-G7.3-WS','Süd',false,true,null,'CX-G7.3-NO-PHYSICAL-WS',true,true);
  insert into public.zaehler(parzelle_id,ist_hauptzaehler,medium,anlagenbereich,zaehlernummer,eichdatum,eingebaut_am,status) values
  (null,true,'strom','Nord','CX-G7.3-H-SN',date '2025-01-01',date '2025-01-01','aktiv'),(null,true,'strom','Süd','CX-G7.3-H-SS',date '2025-01-01',date '2025-01-01','aktiv'),(null,true,'wasser','Nord','CX-G7.3-H-WN',date '2025-01-01',date '2025-01-01','aktiv'),(null,true,'wasser','Süd','CX-G7.3-H-WS',date '2025-01-01',date '2025-01-01','aktiv');
  select id into sn from public.zaehler where zaehlernummer='CX-G7.3-H-SN'; select id into ss from public.zaehler where zaehlernummer='CX-G7.3-H-SS'; select id into wn from public.zaehler where zaehlernummer='CX-G7.3-H-WN'; select id into ws from public.zaehler where zaehlernummer='CX-G7.3-H-WS';
  insert into public.zaehler(parzelle_id,medium,zaehlernummer,eichdatum,eingebaut_am,status) values (b,'strom','CX-G7.3-U-SN',date '2025-01-01',date '2025-01-01','aktiv'),(b+1,'strom','CX-G7.3-U-SS',date '2025-01-01',date '2025-01-01','aktiv'),(b+2,'wasser','CX-G7.3-U-WN',date '2025-01-01',date '2025-01-01','aktiv'),(b+3,'wasser','CX-G7.3-U-WS',date '2025-01-01',date '2025-01-01','aktiv');
  select id into usn from public.zaehler where zaehlernummer='CX-G7.3-U-SN'; select id into uss from public.zaehler where zaehlernummer='CX-G7.3-U-SS'; select id into uwn from public.zaehler where zaehlernummer='CX-G7.3-U-WN'; select id into uws from public.zaehler where zaehlernummer='CX-G7.3-U-WS';
  insert into public.zaehler_ablesung(zaehler_id,ablesedatum,stand,art,freigegeben,pruefstatus) values
  (sn,date '2025-12-31',100,'jea',true,'freigegeben'),(sn,date '2026-12-31',150,'jea',true,'freigegeben'),(ss,date '2025-12-31',100,'jea',true,'freigegeben'),(ss,date '2026-12-31',120,'jea',true,'freigegeben'),(wn,date '2025-12-31',100,'jea',true,'freigegeben'),(wn,date '2026-12-31',120,'jea',true,'freigegeben'),(ws,date '2025-12-31',100,'jea',true,'freigegeben'),(usn,date '2025-12-31',10,'jea',true,'freigegeben'),(usn,date '2026-12-31',40,'jea',true,'freigegeben'),(uss,date '2025-12-31',10,'jea',true,'freigegeben'),(uss,date '2026-12-31',40,'jea',true,'freigegeben'),(uwn,date '2025-12-31',10,'jea',true,'freigegeben'),(uwn,date '2026-12-31',30,'jea',true,'freigegeben'),(uws,date '2025-12-31',10,'jea',true,'freigegeben'),(uws,date '2026-12-31',20,'jea',true,'freigegeben');
end $$;
commit;
