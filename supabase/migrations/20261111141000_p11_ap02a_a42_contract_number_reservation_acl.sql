-- P11 A4.2: reserve_fachnummer is an internal trigger helper, not a client RPC.
-- Contract workflows enter it through trg_reserve_vertragsnummer under its
-- SECURITY DEFINER context; historical reservations retain their separately
-- authorized public RPC.
revoke execute on function public.reserve_fachnummer(text,text,bigint) from authenticated;
grant execute on function public.reserve_fachnummer(text,text,bigint) to service_role;
