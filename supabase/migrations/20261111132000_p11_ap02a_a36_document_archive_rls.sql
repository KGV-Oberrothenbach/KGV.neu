-- P11 AP-02A / A3.6: the maintenance-document association is a document-management read.
drop policy if exists dokument_wartungsvertrag_zuordnung_read_admin_vorstand on public.dokument_wartungsvertrag_zuordnung;
create policy dokument_wartungsvertrag_zuordnung_read_effective
  on public.dokument_wartungsvertrag_zuordnung for select to authenticated
  using (public.has_effective_permission(16));
