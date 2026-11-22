-- FR2-04: attach an existing identical preliminary assignment to a newly
-- registered first lease instead of inserting an overlapping second assignment.
-- An assignment may be promoted only when parcel, member and the entire
-- [start,end] date interval are exactly identical and it has no lease FK.
-- Otherwise the unchanged exclusion constraint must still reject overlap.
-- No tables, RLS policies, data records or security grants are modified here.
DO $fr204_occupancy$
DECLARE
  v_function regprocedure := 'public.sync_pachtverhaeltnis_belegung()'::regprocedure;
  v_definition text;
  v_old text := $old$  insert into public.parzellen_belegung(parzelle_id,mitglied_id,von_datum,bis_datum,pachtverhaeltnis_id)$old$;
  v_new text := $new$  -- FR2-04: preserve and bind a byte-for-byte matching preliminary assignment.
  -- Existing lease-bound rows keep the original upsert/update behavior.
  if not exists (
    select 1 from public.parzellen_belegung b
    where b.pachtverhaeltnis_id = v_relation.id
  ) then
    update public.parzellen_belegung b
       set pachtverhaeltnis_id = v_relation.id
     where b.parzelle_id = v_relation.parzelle_id
       and b.mitglied_id = new.mitglied_id
       and b.von_datum = new.gueltig_ab
       and b.bis_datum is not distinct from new.gueltig_bis
       and b.pachtverhaeltnis_id is null;
    if found then return new; end if;
  end if;
  insert into public.parzellen_belegung(parzelle_id,mitglied_id,von_datum,bis_datum,pachtverhaeltnis_id)$new$;
BEGIN
  v_definition := pg_get_functiondef(v_function);
  IF v_definition IS NULL
    OR (length(v_definition) - length(replace(v_definition, v_old, ''))) <> length(v_old)
    OR position(v_new IN v_definition) > 0 THEN
    RAISE EXCEPTION 'FR2-04: sync function baseline differs; aborting without changes.'
      USING ERRCODE = '23514';
  END IF;
  EXECUTE replace(v_definition, v_old, v_new);
  IF position(v_new IN pg_get_functiondef(v_function)) = 0 THEN
    RAISE EXCEPTION 'FR2-04: preliminary occupancy promotion not present after migration.'
      USING ERRCODE = '23514';
  END IF;
END
$fr204_occupancy$;
