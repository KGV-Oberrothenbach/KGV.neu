-- Repair environments that applied the original 20261011120000 definition.
do $$
declare
  v_definition text;
begin
  select pg_get_functiondef('public.rpc_jahresverbrauch_segmente(integer)'::regprocedure) into v_definition;
  v_definition := replace(v_definition,
    'lead(bp.datum) over w as bis_datum, lead(bp.ablesung_id) over w as end_' || 'ablesung_id,' || chr(10) ||
    '           lead(bp.stand) over w as end' || 'stand, lead(bp.fehlercode) over w as end_' || 'fehlercode,' || chr(10) ||
    '           lead(bp.art) over w as end_' || 'art',
    'lead(bp.datum) over w as bis_datum, lead(bp.ablesung_id) over w as segment_end_ablesung_id,' || chr(10) ||
    '           lead(bp.stand) over w as segment_endstand, lead(bp.fehlercode) over w as segment_end_fehlercode,' || chr(10) ||
    '           lead(bp.art) over w as segment_end_art');
  v_definition := replace(v_definition, 'r.ablesung_id as start_ablesung_id, r.end_ablesung_id, r.stand as startstand, r.endstand,',
    'r.ablesung_id as start_ablesung_id, r.segment_end_ablesung_id as end_ablesung_id, r.stand as startstand, r.segment_endstand as endstand,');
  v_definition := replace(v_definition, 'r.end_fehlercode', 'r.segment_end_fehlercode');
  v_definition := replace(v_definition, 'r.endstand', 'r.segment_endstand');
  v_definition := replace(v_definition, 'r.end_art', 'r.segment_end_art');
  execute v_definition;
end;
$$;

grant execute on function public.rpc_jahresverbrauch_segmente(integer) to authenticated;
