-- RFID is a metadata-backed view.  The central PDF service intentionally accepts
-- only RPC exports, so do not offer a PDF action that the shared backend rejects.
update public.app_export_definition
set erlaubt_csv = true,
    erlaubt_pdf = false
where export_key = 'rfid_status'
  and quelle_typ = 'view';
