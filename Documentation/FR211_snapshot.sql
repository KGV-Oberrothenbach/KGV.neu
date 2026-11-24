-- No personal values are returned. Row counts + order-independent row hashes.
SELECT 'FR211_FINGERPRINT:' || md5(jsonb_build_object(
 'mitglieder',(SELECT jsonb_build_array(count(*),md5(coalesce(string_agg(md5(to_jsonb(t)::text),'' ORDER BY md5(to_jsonb(t)::text)),''))) FROM public.mitglied t),
 'parzellen',(SELECT jsonb_build_array(count(*),md5(coalesce(string_agg(md5(to_jsonb(t)::text),'' ORDER BY md5(to_jsonb(t)::text)),''))) FROM public.parzelle t),
 'belegungen',(SELECT jsonb_build_array(count(*),md5(coalesce(string_agg(md5(to_jsonb(t)::text),'' ORDER BY md5(to_jsonb(t)::text)),''))) FROM public.parzellen_belegung t),
 'zaehler',(SELECT jsonb_build_array(count(*),md5(coalesce(string_agg(md5(to_jsonb(t)::text),'' ORDER BY md5(to_jsonb(t)::text)),''))) FROM public.zaehler t),
 'ablesungen',(SELECT jsonb_build_array(count(*),md5(coalesce(string_agg(md5(to_jsonb(t)::text),'' ORDER BY md5(to_jsonb(t)::text)),''))) FROM public.zaehler_ablesung t),
 'stunden',(SELECT jsonb_build_array(count(*),md5(coalesce(string_agg(md5(to_jsonb(t)::text),'' ORDER BY md5(to_jsonb(t)::text)),''))) FROM public.arbeitsstunde t),
 'rechte',(SELECT jsonb_build_array(count(*),md5(coalesce(string_agg(md5(to_jsonb(t)::text),'' ORDER BY md5(to_jsonb(t)::text)),''))) FROM public.app_user_fachrecht_override t),
 'exportdefinitionen',(SELECT jsonb_build_array(count(*),md5(coalesce(string_agg(md5(to_jsonb(t)::text),'' ORDER BY md5(to_jsonb(t)::text)),''))) FROM public.app_export_definition t),
 'exportspalten',(SELECT jsonb_build_array(count(*),md5(coalesce(string_agg(md5(to_jsonb(t)::text),'' ORDER BY md5(to_jsonb(t)::text)),''))) FROM public.app_export_column_definition t),
 'exportfilter',(SELECT jsonb_build_array(count(*),md5(coalesce(string_agg(md5(to_jsonb(t)::text),'' ORDER BY md5(to_jsonb(t)::text)),''))) FROM public.app_export_filter_definition t)
)::text) AS fingerprint;
