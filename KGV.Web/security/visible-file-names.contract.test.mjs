import assert from "node:assert/strict";
import test from "node:test";
import { readFile } from "node:fs/promises";

const root = new URL("../../", import.meta.url);
const read = (path) => readFile(new URL(path, root), "utf8");

test("PendingPhotos behalten die UUID nur im internen Pfad", async () => {
  const source = await read("KGV.Maui/Services/PendingPhotos/PendingPhotoService.cs");
  assert.match(source, /GetPendingFilePath\(\$"\{id:N\}_\{fileName\}"\)/);
  assert.match(source, /FileName = fileName/);
  assert.doesNotMatch(source, /FileName = Path\.GetFileName\(filePath\)/);
});

test("PDF-Share verwendet einen fachlichen Namen, der UUID-Cache bleibt intern", async () => {
  const source = await read("KGV.Maui/Pages/PdfViewerPage.cs");
  assert.match(source, /kgv-pdf-\{Guid\.NewGuid\(\):N\}\.pdf/);
  assert.match(source, /BuildPdf\(originalDateiname, title\)/);
  assert.match(source, /new ShareFile\(sharePath, "application\/pdf"\)/);
  assert.doesNotMatch(source, /new ShareFile\(_cachePath, "application\/pdf"\)/);
});

test("historische Uploads und Vereinsunterlagen behalten den gewählten Namen", async () => {
  const [historical, club] = await Promise.all([
    read("KGV.Web/features/documents/HistoricalDocumentUploadDialog.tsx"),
    read("KGV.Web/features/club-documents/ClubDocumentsManagement.tsx"),
  ]);

  assert.match(historical, /uploadDocumentForOwner\([^,]+, [^,]+, memberDocumentOwner\(memberId\), file, title\)/);
  assert.match(club, /dateiname: draft\.file\.name/);
  assert.doesNotMatch(historical, /SichtbarerDateiname|automatische Umbenennung/i);
  assert.doesNotMatch(club, /SichtbarerDateiname|automatische Umbenennung/i);
});
