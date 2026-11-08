-- Ausschließlich nach manueller G7.3-Abnahme auf DemoDB ausführen.
begin;
delete from public.zaehler_ablesung where zaehler_id in (select id from public.zaehler where zaehlernummer like 'CX-G7.3-%');
delete from public.zaehler where zaehlernummer like 'CX-G7.3-%';
delete from public.parzelle where garten_nr like 'CX-G7.3-%';
commit;
