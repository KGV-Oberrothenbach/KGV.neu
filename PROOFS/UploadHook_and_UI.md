Upload Hook & UI Design

Viewer UI:
- Button: "Speichern & Hochladen" (visible wenn LocalDocument exists and not uploaded)
- Button Handler:
  1. Read bytes from LocalDocumentService.GetLocalPath(FileName)
  2. Call ISupabaseService.CreateSignedMitgliedsantragDokumentAsync or a generic Upload API with FileContent
  3. On success: update DocumentInfo.StoragePath with returned storage reference and mark LocalDocumentStatus.IsUploaded = true
  4. Update MemberDetailPage UI
  5. Optionally archive or delete local file after successful upload (policy)

MemberDetailPage UI changes:
- Replace "Mitgliedsantrag als PDF" with context-aware label:
  - If local exists: "Dokument öffnen" (open Viewer)
  - If not: "Mitgliedsantrag als PDF" (create & persist)
- Show small status line under the button: "Lokal vorhanden, noch nicht hochgeladen" or "Hochgeladen".

Concurrency & Safety:
- Disable Upload button while upload in progress
- Use optimistic locking on upload: if server returns conflict, show dialog to user with options (overwrite/abort)

Server Contract:
- Upload API should accept FileContent and return Document record with StoragePath and DriveFileId etc.
- Ensure server validates upload caller permissions (only allowed roles can set final storage refs).
