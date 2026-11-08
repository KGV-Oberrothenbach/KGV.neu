-- KGV: G3.4 – Hauptzähler in den bestehenden Zähler-Validierungstriggern
-- Voraussetzung: die Hauptzähler-Migrationen bis einschließlich
-- 20261104230000 sind angewendet. Die Trigger selbst bleiben unverändert
-- installiert; nur ihre Funktionskörper werden erweitert.

begin;

create or replace function public.trg_zaehler_validate_medium_allowed()
returns trigger
language plpgsql
as $$
declare
  v_hat_wasser boolean;
  v_hat_strom boolean;
begin
  -- Hauptzähler gehören zu einer Messstelle, nicht zu einer Parzelle. Der
  -- Hauptzählerpfad wird daher ausdrücklich über die kanonische Messstelle
  -- validiert und nicht allein wegen einer NULL-parzelle_id übersprungen.
  if new.ist_hauptzaehler then
    if new.parzelle_id is not null then
      raise exception 'Ein Hauptzähler darf keiner Parzelle zugeordnet sein.' using errcode = '23514';
    end if;

    if new.medium is null or new.medium::text not in ('strom', 'wasser') then
      raise exception 'Das Medium des Hauptzählers ist ungültig.' using errcode = '23514';
    end if;

    if new.anlagenbereich is null or new.anlagenbereich not in ('Nord', 'Süd') then
      raise exception 'Der Anlagenbereich des Hauptzählers ist ungültig.' using errcode = '23514';
    end if;

    if not exists (
      select 1
      from public.hauptzaehler_messstelle h
      where h.medium = new.medium::text
        and h.anlagenbereich = new.anlagenbereich
    ) then
      raise exception 'Für Hauptzähler-Medium % und Anlagenbereich % fehlt die Messstelle.', new.medium, new.anlagenbereich
        using errcode = '23514';
    end if;

    return new;
  end if;

  -- Parzellenzähler: unveränderte bisherige Medium-Prüfung.
  select p.hat_wasser, p.hat_strom
    into v_hat_wasser, v_hat_strom
  from public.parzelle p
  where p.id = new.parzelle_id;

  if new.medium = 'wasser' and coalesce(v_hat_wasser, false) = false then
    raise exception 'Parzelle % ist nicht als wasserführend markiert.', new.parzelle_id;
  end if;

  if new.medium = 'strom' and coalesce(v_hat_strom, false) = false then
    raise exception 'Parzelle % ist nicht als stromführend markiert.', new.parzelle_id;
  end if;

  return new;
end;
$$;

create or replace function public.trg_zaehler_validate_parzelle_rfid()
returns trigger
language plpgsql
as $$
declare
  v_rfid text;
begin
  -- Die RFID eines Hauptzählers liegt ausschließlich an der Messstelle. Die
  -- Parzellen-RFID-Prüfung ist deshalb für den ausdrücklich markierten
  -- Hauptzähler nicht anwendbar; die Messstelle wurde im Medium-Trigger
  -- geprüft und die CHECK-Constraints bleiben zusätzlich wirksam.
  if new.ist_hauptzaehler then
    if new.parzelle_id is not null then
      raise exception 'Ein Hauptzähler darf keiner Parzelle zugeordnet sein.' using errcode = '23514';
    end if;
    return new;
  end if;

  -- Parzellenzähler: unveränderte bisherige RFID-Prüfung.
  if new.medium = 'wasser' then
    select p.rfid_wasser into v_rfid
    from public.parzelle p
    where p.id = new.parzelle_id;
  else
    select p.rfid_strom into v_rfid
    from public.parzelle p
    where p.id = new.parzelle_id;
  end if;

  if v_rfid is null or btrim(v_rfid) = '' then
    raise exception 'Für Parzelle % ist keine RFID für Medium % hinterlegt.', new.parzelle_id, new.medium;
  end if;

  return new;
end;
$$;

commit;
