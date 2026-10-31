-- ALT-3.2: Neutrale, relationale Metadaten für allgemeine Dokumente.
-- Bestandsdokumente bleiben bewusst unklassifiziert.

alter table public.dokument
    add column if not exists dokumenttyp_id bigint,
    add column if not exists dokumentdatum date,
    add column if not exists unterzeichnet boolean;
do $$
begin
    if not exists (
        select 1
        from pg_constraint
        where conrelid = 'public.dokument'::regclass
          and conname = 'dokument_dokumenttyp_id_fkey'
    ) then
        alter table public.dokument
            add constraint dokument_dokumenttyp_id_fkey
            foreign key (dokumenttyp_id)
            references public.dokumenttyp(id)
            on delete restrict;
    end if;
end;
$$;
create index if not exists ix_dokument_dokumenttyp_id_aktiv
    on public.dokument (dokumenttyp_id)
    where archiviert_at is null
      and dokumenttyp_id is not null;
comment on column public.dokument.dokumenttyp_id is
    'Optionaler allgemeiner Dokumenttyp. Bestehende Dokumente bleiben ohne rückwirkende Typisierung NULL.';
comment on column public.dokument.dokumentdatum is
    'Optionales fachliches Datum des Dokuments; weder Uploadzeitpunkt noch bei Pachtverträgen das maßgebliche Vertragsdatum.';
comment on column public.dokument.unterzeichnet is
    'Expliziter Signaturstatus: TRUE unterzeichnet, FALSE nicht unterzeichnet, NULL unbekannt oder nicht klassifiziert.';
