-- A published version owns exactly one deterministic PDF and Field-Spec path.
-- This is additive: the existing catalog has already established the same path
-- convention, while this constraint makes it impossible to bypass in the future.
alter table public.pdf_template_catalog
    add constraint ck_pdf_template_catalog_versioned_paths
    check (
        storage_path = 'templates/' || template_key || '/' || version || '/template.pdf'
        and field_spec_path = 'templates/' || template_key || '/' || version || '/field-spec.json'
    );
