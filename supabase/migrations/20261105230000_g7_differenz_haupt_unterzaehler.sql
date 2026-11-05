-- G7: G6-Fehlervertrag normalisieren und Differenz ausschließlich serverseitig ableiten.
begin;

alter function public.rpc_jahresverbrauch_haupt_und_unterzaehler(integer)
  rename to rpc_jahresverbrauch_haupt_und_unterzaehler_g6_basis;

create function public.rpc_jahresverbrauch_haupt_und_unterzaehler(p_jahr integer)
returns table (
  jahr integer, medium text, anlagenbereich text,
  hauptzaehler_verbrauch numeric, unterzaehler_verbrauch numeric,
  hauptzaehler_berechenbar boolean, unterzaehler_berechenbar boolean,
  hauptzaehler_fehlercode text, hauptzaehler_fehlertext text,
  unterzaehler_fehlercode text, unterzaehler_fehlertext text
)
language plpgsql stable security definer set search_path to 'public', 'pg_temp'
as $$
begin
  if not public.has_effective_permission(32) then raise exception 'Für die Verbrauchsauswertung fehlt CanReadMeters.' using errcode='42501'; end if;
  return query select b.jahr,b.medium,b.anlagenbereich,b.hauptzaehler_verbrauch,b.unterzaehler_verbrauch,
    b.hauptzaehler_berechenbar,b.unterzaehler_berechenbar,
    case when b.hauptzaehler_berechenbar then null else b.hauptzaehler_fehlercode end,
    case when b.hauptzaehler_berechenbar then null else b.hauptzaehler_fehlertext end,
    case when b.unterzaehler_berechenbar then null else b.unterzaehler_fehlercode end,
    case when b.unterzaehler_berechenbar then null else b.unterzaehler_fehlertext end
  from public.rpc_jahresverbrauch_haupt_und_unterzaehler_g6_basis(p_jahr) b;
end;
$$;

create function public.rpc_jahresverbrauch_haupt_unter_differenz(p_jahr integer)
returns table (
  jahr integer, medium text, anlagenbereich text, einheit text,
  hauptzaehler_verbrauch numeric, unterzaehler_verbrauch numeric, differenz numeric,
  berechenbar boolean, pruefbeduerftig boolean, fehlercode text, fehlertext text
)
language plpgsql stable security definer set search_path to 'public', 'pg_temp'
as $$
begin
  if not public.has_effective_permission(32) then raise exception 'Für die Verbrauchsauswertung fehlt CanReadMeters.' using errcode='42501'; end if;
  return query select r.jahr,r.medium,r.anlagenbereich,
    case when r.medium='strom' then 'kWh' else 'm³' end,
    r.hauptzaehler_verbrauch,r.unterzaehler_verbrauch,
    case when r.hauptzaehler_berechenbar and r.unterzaehler_berechenbar then r.hauptzaehler_verbrauch-r.unterzaehler_verbrauch end,
    r.hauptzaehler_berechenbar and r.unterzaehler_berechenbar,
    r.hauptzaehler_berechenbar and r.unterzaehler_berechenbar and r.hauptzaehler_verbrauch-r.unterzaehler_verbrauch < 0,
    case when r.hauptzaehler_berechenbar and r.unterzaehler_berechenbar then null else concat_ws(';',r.hauptzaehler_fehlercode,r.unterzaehler_fehlercode) end,
    case when r.hauptzaehler_berechenbar and r.unterzaehler_berechenbar then null else concat_ws(' ',r.hauptzaehler_fehlertext,r.unterzaehler_fehlertext) end
  from public.rpc_jahresverbrauch_haupt_und_unterzaehler(p_jahr) r;
end;
$$;

revoke all on function public.rpc_jahresverbrauch_haupt_und_unterzaehler(integer) from public;
revoke all on function public.rpc_jahresverbrauch_haupt_unter_differenz(integer) from public;
grant execute on function public.rpc_jahresverbrauch_haupt_und_unterzaehler(integer) to authenticated;
grant execute on function public.rpc_jahresverbrauch_haupt_unter_differenz(integer) to authenticated;
commit;
