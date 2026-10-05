CREATE OR REPLACE FUNCTION public.kgv_local_now()
RETURNS timestamp without time zone
LANGUAGE sql
STABLE
AS $$
	SELECT now() AT TIME ZONE 'Europe/Berlin';
$$;
