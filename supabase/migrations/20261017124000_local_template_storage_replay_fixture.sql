-- Local fresh-replay fixture for historical catalog migrations.
-- The original template binaries are versioned under KGV.Core/Templates but were
-- uploaded to Storage outside the historical SQL chain. Catalog migrations only
-- require their storage metadata rows. This fixture is intentionally inert when
-- the real objects already exist.

insert into storage.objects (bucket_id, name, metadata)
select 'pdf-templates', path, jsonb_build_object('local_replay_fixture', true)
from (values
  ('templates/export-zaehler-eichfaelligkeit-hoch/v1/template.pdf'),
  ('templates/export-zaehler-eichfaelligkeit-hoch/v1/field-spec.json'),
  ('templates/export-zaehler-eichfaelligkeit-quer/v1/template.pdf'),
  ('templates/export-zaehler-eichfaelligkeit-quer/v1/field-spec.json'),
  ('templates/briefbogen-allgemein/v1/template.pdf'),
  ('templates/briefbogen-allgemein/v1/field-spec.json'),
  ('templates/export-allgemein/v1/template.pdf'),
  ('templates/export-allgemein/v1/field-spec.json'),
  ('templates/mitgliedsantrag/v4/template.pdf'),
  ('templates/mitgliedsantrag/v4/field-spec.json'),
  ('templates/pachtvertrag/v4/template.pdf'),
  ('templates/pachtvertrag/v4/field-spec.json'),
  ('templates/wartungsvertrag/v1/template.pdf'),
  ('templates/wartungsvertrag/v1/field-spec.json'),
  ('templates/wartungsvertrag-verlaengerung/v1/template.pdf'),
  ('templates/wartungsvertrag-verlaengerung/v1/field-spec.json'),
  ('templates/export-allgemein/v2/template.pdf'),
  ('templates/export-allgemein/v2/field-spec.json'),
  ('templates/pachtvertrag/v5/template.pdf'),
  ('templates/pachtvertrag/v5/field-spec.json'),
  ('templates/pachtvertrag/v6/template.pdf'),
  ('templates/pachtvertrag/v6/field-spec.json'),
  ('templates/pachtvertrag/v7/template.pdf'),
  ('templates/pachtvertrag/v7/field-spec.json'),
  ('templates/pachtvertrag/v8/template.pdf'),
  ('templates/pachtvertrag/v8/field-spec.json'),
  ('templates/mitgliedsantrag/v5/template.pdf'),
  ('templates/mitgliedsantrag/v5/field-spec.json'),
  ('templates/aufnahmebestaetigung/v1/template.pdf'),
  ('templates/aufnahmebestaetigung/v1/field-spec.json'),
  ('templates/pachtvertrag/v9/template.pdf'),
  ('templates/pachtvertrag/v9/field-spec.json'),
  ('templates/wartungsvertrag/v2/template.pdf'),
  ('templates/wartungsvertrag/v2/field-spec.json'),
  ('templates/wartungsvertrag-verlaengerung/v2/template.pdf'),
  ('templates/wartungsvertrag-verlaengerung/v2/field-spec.json'),
  ('templates/export-allgemein/v3/template.pdf'),
  ('templates/export-allgemein/v3/field-spec.json')
) as fixture(path)
where not exists (
  select 1
  from storage.objects existing
  where existing.bucket_id = 'pdf-templates'
    and existing.name = fixture.path
);
