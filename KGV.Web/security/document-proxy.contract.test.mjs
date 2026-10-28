import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";

const edgeFunction = await readFile(new URL("../../supabase/functions/kgv-upload-document/index.ts", import.meta.url), "utf8");
const webRepository = await readFile(new URL("../repositories/documents/document-file-repository.ts", import.meta.url), "utf8");
const mauiDocuments = await readFile(new URL("../../KGV.Maui/Pages/DokumentePage.xaml.cs", import.meta.url), "utf8");
const mauiMemberDetail = await readFile(new URL("../../KGV.Maui/Pages/MemberDetailPage.cs", import.meta.url), "utf8");
const mauiContractFlow = await readFile(new URL("../../KGV.Maui/Pages/PachtvertragFlowHelper.cs", import.meta.url), "utf8");
const mauiDocumentStorage = await readFile(new URL("../../KGV.Maui/Services/Documents/DocumentStorage.cs", import.meta.url), "utf8");
const infrastructure = await readFile(new URL("../../KGV.Infrastructure/Services/SupabaseService.cs", import.meta.url), "utf8");

assert.match(webRepository, /action:\s*"download",\s*document_id:\s*document\.id/);
assert.match(webRepository, /response\.blob\(\)/);
assert.doesNotMatch(webRepository, /drive\.google\.com|webViewLink|web_view_link/i);

assert.match(mauiDocuments, /DownloadDokumentContentAsync\(document\)/);
for (const source of [mauiDocuments, mauiMemberDetail, mauiContractFlow]) {
  assert.doesNotMatch(source, /ResolveDokumentOpenUrlAsync\(document/);
}
assert.match(mauiDocumentStorage, /FileSystem\.CacheDirectory/);
assert.match(mauiDocumentStorage, /OpenFileRequest/);
assert.match(mauiDocuments, /OpenTemporaryFileAsync/);

const documentResolver = infrastructure.slice(
  infrastructure.indexOf("ResolveDokumentOpenUrlAsync"),
  infrastructure.indexOf("ResolveAblesungFotoOpenUrlAsync"),
);
assert.match(documentResolver, /DriveFileId[\s\S]{0,180}return null/);
assert.doesNotMatch(documentResolver, /BuildGoogleDriveFileViewUrl/);

assert.match(edgeFunction, /select\("id,mitglied_id,parzelle_id,drive_file_id,dateiname,mime_type,archiviert_at"\)/);
assert.match(edgeFunction, /if \(document\.archiviert_at\)\s*return null/);
assert.match(edgeFunction, /authenticateDocumentUser\(authHeader\)[\s\S]*mayReadDocument\(auth, documentId\)[\s\S]*errorResponse\(403, "FORBIDDEN"/);
const downloadStart = edgeFunction.indexOf('if (action !== "download")');
const downloadSection = edgeFunction.slice(downloadStart);
assert.ok(downloadSection.indexOf("mayReadDocument(auth, documentId)") < downloadSection.indexOf("getGoogleAccessToken()"));
assert.doesNotMatch(edgeFunction, /webViewLink|web_view_link/);
