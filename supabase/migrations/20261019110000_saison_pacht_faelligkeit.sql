-- Die Pachtfälligkeit ist eine saisonbezogene fachliche Einstellung.
alter table public.saison
  add column if not exists pacht_faellig_am date;
update public.saison
set pacht_faellig_am = make_date(jahr, 3, 31)
where pacht_faellig_am is null;
alter table public.saison
  alter column pacht_faellig_am set not null;
comment on column public.saison.pacht_faellig_am is
  'Fälligkeit der laufenden Jahrespacht für diese Saison.';
