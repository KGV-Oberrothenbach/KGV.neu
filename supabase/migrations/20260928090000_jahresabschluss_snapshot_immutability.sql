-- Ein endgültiger Jahresabschluss schützt auch seine Ergebnispositionen.
create or replace function public.prevent_final_jahresabschluss_position_change()
returns trigger
language plpgsql
as $$
declare
    v_abschluss_id bigint := coalesce(new.jahresabschluss_id, old.jahresabschluss_id);
begin
    if exists (select 1 from public.jahresabschluss where id = v_abschluss_id and status = 'abgeschlossen') then
        raise exception 'Positionen eines abgeschlossenen Jahresabschlusses sind unveränderlich.';
    end if;
    return coalesce(new, old);
end;
$$;

drop trigger if exists trg_prevent_final_jahresabschluss_position_change on public.jahresabschluss_position;
create trigger trg_prevent_final_jahresabschluss_position_change
before insert or update or delete on public.jahresabschluss_position
for each row execute function public.prevent_final_jahresabschluss_position_change();
