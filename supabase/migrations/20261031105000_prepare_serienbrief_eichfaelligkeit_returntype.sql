-- Kompatibilitätsmigration:
-- Die Serienbriefmigration 20261031110000 erweitert den RETURNS-TABLE-Vertrag.
-- PostgreSQL erlaubt eine Änderung dieses Rückgabetyps nicht via CREATE OR REPLACE.

drop function if exists public.rpc_export_zaehler_eichfaelligkeit_json(jsonb);
drop function if exists public.rpc_export_zaehler_eichfaelligkeit(text);
