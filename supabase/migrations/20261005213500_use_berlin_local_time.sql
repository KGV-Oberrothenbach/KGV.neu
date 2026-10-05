CREATE OR REPLACE FUNCTION public.trg_arbeitseinsatz_anmeldung_validate()
RETURNS trigger
LANGUAGE plpgsql
AS $$
declare
  v_aktiv boolean;
  v_max_teilnehmer integer;
  v_angemeldet_count integer;
  v_anmeldung_bis timestamp without time zone;
  v_datum date;
begin
  select a.aktiv, a.max_teilnehmer, a.anmeldung_bis, a.datum
	into v_aktiv, v_max_teilnehmer, v_anmeldung_bis, v_datum
  from public.arbeitseinsatz a
  where a.id = new.arbeitseinsatz_id;

  if v_aktiv is distinct from true then
	raise exception 'Anmeldung nicht möglich: Arbeitseinsatz ist nicht aktiv.';
  end if;

  if new.status = 'angemeldet' then
	if v_anmeldung_bis is not null and public.kgv_local_now() > v_anmeldung_bis then
	  raise exception 'Anmeldung nicht möglich: Anmeldeschluss ist erreicht.';
	end if;

	if v_anmeldung_bis is null and public.kgv_local_now()::date > v_datum then
	  raise exception 'Anmeldung nicht möglich: Arbeitseinsatz liegt in der Vergangenheit.';
	end if;

	if v_max_teilnehmer is not null then
	  select count(*)
		into v_angemeldet_count
	  from public.arbeitseinsatz_anmeldung x
	  where x.arbeitseinsatz_id = new.arbeitseinsatz_id
		and x.status = 'angemeldet'
		and (tg_op = 'INSERT' or x.id <> new.id);

	  if v_angemeldet_count >= v_max_teilnehmer then
		raise exception 'Anmeldung nicht möglich: maximale Teilnehmerzahl erreicht.';
	  end if;
	end if;
  end if;

  return new;
end;
$$;

CREATE OR REPLACE VIEW public.v_startseite_arbeitseinsatz
WITH (security_invoker = 'true') AS
select a.id,
	a.titel,
	a.beschreibung,
	a.datum,
	a.start_uhrzeit,
	a.end_uhrzeit,
	a.treffpunkt,
	a.max_teilnehmer,
	a.stunden_wert,
	a.sichtbar_ab,
	a.sichtbar_bis,
	a.anmeldung_bis,
	(coalesce(sum(
		case
			when aa.status = 'angemeldet'::public.arbeitseinsatz_anmeldung_status then 1
			else 0
		end), 0::bigint))::integer as angemeldet_count,
	case
		when a.max_teilnehmer is null then null::integer
		else greatest((a.max_teilnehmer - coalesce(sum(
			case
				when aa.status = 'angemeldet'::public.arbeitseinsatz_anmeldung_status then 1
				else 0
			end), 0::bigint)), 0::bigint)::integer
	end as freie_plaetze
from public.arbeitseinsatz a
left join public.arbeitseinsatz_anmeldung aa
	on aa.arbeitseinsatz_id = a.id
where a.aktiv = true
  and (a.sichtbar_ab is null or a.sichtbar_ab <= public.kgv_local_now())
  and (a.sichtbar_bis is null or a.sichtbar_bis >= public.kgv_local_now())
group by a.id, a.titel, a.beschreibung, a.datum, a.start_uhrzeit, a.end_uhrzeit,
	a.treffpunkt, a.max_teilnehmer, a.stunden_wert, a.sichtbar_ab, a.sichtbar_bis,
	a.anmeldung_bis
order by a.datum, a.start_uhrzeit nulls first, a.id;

CREATE OR REPLACE VIEW public.v_startseite_bekanntmachungen
WITH (security_invoker = 'true') AS
select id, titel, inhalt_html, sichtbar_ab, sichtbar_bis, sort_order, created_at
from public.bekanntmachung b
where aktiv = true
  and (sichtbar_ab is null or sichtbar_ab <= public.kgv_local_now())
  and (sichtbar_bis is null or sichtbar_bis >= public.kgv_local_now())
order by sort_order, created_at desc, id desc;

CREATE OR REPLACE VIEW public.v_startseite_termine
WITH (security_invoker = 'true') AS
select id, titel, beschreibung, datum, start_uhrzeit, end_uhrzeit, sichtbar_ab, sichtbar_bis
from public.termin t
where aktiv = true
  and (sichtbar_ab is null or sichtbar_ab <= public.kgv_local_now())
  and (sichtbar_bis is null or sichtbar_bis >= public.kgv_local_now())
order by datum, start_uhrzeit nulls first, id;

DROP POLICY IF EXISTS arbeitseinsatz_anmeldung_insert_own_open ON public.arbeitseinsatz_anmeldung;
CREATE POLICY arbeitseinsatz_anmeldung_insert_own_open ON public.arbeitseinsatz_anmeldung
FOR INSERT TO authenticated
WITH CHECK (
	public.is_productive_admin_or_vorstand()
	OR (
		public.current_mitglied_id() IS NOT NULL
		AND mitglied_id = public.current_mitglied_id()
		AND (
			NOT public.is_demo_or_reviewer()
			OR public.is_demo_member_arbeitseinsatz_scope(mitglied_id, arbeitseinsatz_id)
		)
		AND EXISTS (
			SELECT 1
			FROM public.arbeitseinsatz a
			WHERE a.id = arbeitseinsatz_anmeldung.arbeitseinsatz_id
			  AND a.aktiv = true
			  AND (a.sichtbar_ab IS NULL OR a.sichtbar_ab <= public.kgv_local_now())
			  AND (a.sichtbar_bis IS NULL OR a.sichtbar_bis >= public.kgv_local_now())
			  AND (a.anmeldung_bis IS NULL OR a.anmeldung_bis >= public.kgv_local_now())
		)
	)
);

DROP POLICY IF EXISTS arbeitseinsatz_anmeldung_update_own_open ON public.arbeitseinsatz_anmeldung;
CREATE POLICY arbeitseinsatz_anmeldung_update_own_open ON public.arbeitseinsatz_anmeldung
FOR UPDATE TO authenticated
USING (
	public.is_productive_admin_or_vorstand()
	OR (
		mitglied_id = public.current_mitglied_id()
		AND (
			NOT public.is_demo_or_reviewer()
			OR public.is_demo_member_arbeitseinsatz_scope(mitglied_id, arbeitseinsatz_id)
		)
	)
)
WITH CHECK (
	public.is_productive_admin_or_vorstand()
	OR (
		public.current_mitglied_id() IS NOT NULL
		AND mitglied_id = public.current_mitglied_id()
		AND (
			NOT public.is_demo_or_reviewer()
			OR public.is_demo_member_arbeitseinsatz_scope(mitglied_id, arbeitseinsatz_id)
		)
		AND EXISTS (
			SELECT 1
			FROM public.arbeitseinsatz a
			WHERE a.id = arbeitseinsatz_anmeldung.arbeitseinsatz_id
			  AND a.aktiv = true
			  AND (a.sichtbar_ab IS NULL OR a.sichtbar_ab <= public.kgv_local_now())
			  AND (a.sichtbar_bis IS NULL OR a.sichtbar_bis >= public.kgv_local_now())
			  AND (a.anmeldung_bis IS NULL OR a.anmeldung_bis >= public.kgv_local_now())
		)
	)
);

DROP POLICY IF EXISTS arbeitseinsatz_select_visible_authenticated ON public.arbeitseinsatz;
CREATE POLICY arbeitseinsatz_select_visible_authenticated ON public.arbeitseinsatz
FOR SELECT TO authenticated
USING (
	public.is_productive_admin_or_vorstand()
	OR (
		aktiv = true
		AND (sichtbar_ab IS NULL OR sichtbar_ab <= public.kgv_local_now())
		AND (sichtbar_bis IS NULL OR sichtbar_bis >= public.kgv_local_now())
		AND (
			(public.is_demo_or_reviewer() AND coalesce(is_demo, false) = true)
			OR ((NOT public.is_demo_or_reviewer()) AND coalesce(is_demo, false) = false)
		)
	)
);

DROP POLICY IF EXISTS bekanntmachung_select_visible_authenticated ON public.bekanntmachung;
CREATE POLICY bekanntmachung_select_visible_authenticated ON public.bekanntmachung
FOR SELECT TO authenticated
USING (
	public.is_productive_admin_or_vorstand()
	OR (
		aktiv = true
		AND (sichtbar_ab IS NULL OR sichtbar_ab <= public.kgv_local_now())
		AND (sichtbar_bis IS NULL OR sichtbar_bis >= public.kgv_local_now())
		AND (
			(public.is_demo_or_reviewer() AND coalesce(is_demo, false) = true)
			OR ((NOT public.is_demo_or_reviewer()) AND coalesce(is_demo, false) = false)
		)
	)
);

DROP POLICY IF EXISTS termin_select_visible_authenticated ON public.termin;
CREATE POLICY termin_select_visible_authenticated ON public.termin
FOR SELECT TO authenticated
USING (
	public.is_productive_admin_or_vorstand()
	OR (
		aktiv = true
		AND (sichtbar_ab IS NULL OR sichtbar_ab <= public.kgv_local_now())
		AND (sichtbar_bis IS NULL OR sichtbar_bis >= public.kgv_local_now())
		AND (
			(public.is_demo_or_reviewer() AND coalesce(is_demo, false) = true)
			OR ((NOT public.is_demo_or_reviewer()) AND coalesce(is_demo, false) = false)
		)
	)
);
