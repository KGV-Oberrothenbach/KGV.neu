-- Historical repair: 20260924230000 already copies these season values from
-- the 2025 template, but the original column-foundation migration was absent.

alter table public.saison
  add column if not exists pacht_pro_qm numeric(10,5),
  add column if not exists mitgliedsbeitrag numeric(10,2),
  add column if not exists mitgliedsbeitrag_nebenmitglied numeric(10,2) not null default 0.00,
  add column if not exists aufnahmegebuehr numeric(10,2) not null default 10.00,
  add column if not exists gebuehr_bauantrag numeric(10,2) not null default 0.00,
  add column if not exists is_demo boolean default false;

update public.saison
set pacht_pro_qm = 0
where pacht_pro_qm is null;

alter table public.saison
  alter column pacht_pro_qm set not null;
