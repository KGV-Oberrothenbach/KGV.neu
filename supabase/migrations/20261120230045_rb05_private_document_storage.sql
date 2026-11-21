-- RB05: Private document storage for new KGV contract documents.
-- No public bucket and deliberately no authenticated storage.objects grants.
INSERT INTO storage.buckets (id, name, public, file_size_limit, allowed_mime_types)
VALUES ('dokumente', 'dokumente', false, 20971520, NULL)
ON CONFLICT (id) DO NOTHING;

DO $rb05$
BEGIN
 IF NOT EXISTS (SELECT 1 FROM storage.buckets WHERE id='dokumente' AND public=false)
 THEN RAISE EXCEPTION 'RB05: private dokumente storage bucket is missing'; END IF;
END
$rb05$;
