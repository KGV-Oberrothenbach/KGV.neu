-- G14.6: structured JAE classification belongs to G12, never to the XLSX client.
alter table public.parzelle add column if not exists ist_vereinsgarten boolean not null default false;
alter function public.rpc_jahresverbrauch_segmente(integer) rename to rpc_jahresverbrauch_segmente_g12_basis;
create function public.rpc_jahresverbrauch_segmente(p_jahr integer)
returns table (
    jahr integer, parzelle_id bigint, parzelle text, mitglied_id bigint, pachtbezug text,
    medium text, zaehler_id bigint, zaehlernummer text, von_datum date, bis_datum date,
    start_ablesung_id bigint, end_ablesung_id bigint, startstand numeric, endstand numeric,
    verbrauch numeric, grund text, berechenbar boolean, fehlercode text, fehlertext text,
    bezug_typ text, ist_vereinsgarten boolean, umlagefaehig boolean
)
language sql stable security invoker as $$
select b.jahr, b.parzelle_id, b.parzelle, b.mitglied_id,
       case when p.ist_vereinsgarten then 'Verein' else b.pachtbezug end,
       b.medium, b.zaehler_id, b.zaehlernummer, b.von_datum, b.bis_datum,
       b.start_ablesung_id, b.end_ablesung_id, b.startstand, b.endstand, b.verbrauch,
       b.grund, b.berechenbar, b.fehlercode, b.fehlertext,
       case when p.ist_vereinsgarten then 'verein' when b.mitglied_id is null then 'leerstand' else 'paechter' end,
       p.ist_vereinsgarten,
       true
from public.rpc_jahresverbrauch_segmente_g12_basis(p_jahr) b
join public.parzelle p on p.id = b.parzelle_id;
$$;
grant execute on function public.rpc_jahresverbrauch_segmente(integer) to authenticated;
