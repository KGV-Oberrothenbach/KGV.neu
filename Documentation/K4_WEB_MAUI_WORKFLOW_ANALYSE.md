# K4a – WEB/MAUI Vertragsworkflow: Analyse und Zielvertrag

Stand: 2026-10-17. Diese Analyse beschreibt den Zustand nach K3. Sie führt keine Produktivlogik ein.

## K4b-Status

K4b ergänzt die gemeinsame MAUI-Datenbasis: Kommunikationsvorgang- und
`kgv-prepared-contract/v1`-DTOs, strengen Parser sowie ein RLS-gebundenes
Repository mit Revision, Zuweisung und Signaturstart/-abschluss. Es wurden keine
MAUI-Seiten, Generatoren, Signaturdaten oder Datenbankmigrationen geändert.

Die Repository-HTTP-Tests verwenden einen lokalen `HttpMessageHandler` und
prüfen Load, Create, Claim/Release, Revisionsupdate, Signaturstart und Abschluss
gegen den PostgREST-Vertrag. KGV.Core und KGV.Infrastructure sind kompilierbar.
Das vorherige Workload-Problem wurde mit `dotnet workload restore` behoben
(Workload-Satz 10.0.402, Android 36.1.118, MAUI 10.0.110); der Android-Debug-
Build ist erfolgreich. K4b ist vollständig validiert. K4 insgesamt bleibt offen;
K4c ist der nächste Teil.

## 1. Ist-Zustand WEB

K3 verwendet `kommunikationsvorgang` als serverseitigen Source of Truth. Der strukturierte Draft ist `kgv-prepared-contract/v1`:

| Dokument | Persistierter Draft |
| --- | --- |
| Mitgliedsantrag | `memberId`, `startDate` (ISO `yyyy-MM-dd`), `memberFee`, `representative` (oder `null`), `includeSecondaryMember` |
| Pachtvertrag | `memberId` plus `draft`: `startDate`, `parcelId`, `hasPreviousContract`, `previousContractDate`, `includeSecondaryMember`, `handoverDate`, `fixedUntil`, `gardenRulesVersion`, `gardenRulesHandedOverAt`, `additionalAgreements`, `attachment1`–`attachment4`, `clubSignatory` |

`prepared-contract-service.ts` lädt und validiert den Draft, speichert ausschließlich bei inhaltlicher Änderung per erwarteter `revision`, startet die Signatur nur bei `vorbereitet`/eigener Zuweisung und schließt nur eine eingefrorene eigene Revision ab. Der K1-Trigger setzt bei `zur_signatur` Serverzeit und `signatur_revision`; der K3b-Guard sperrt Entwürfe nach Beginn.

Nicht persistiert werden Signaturen, Preview-PDFs, öffentliche Preview-URLs oder Storage-Pfade. Preview und Finalize verwenden in WEB `kgv-generate-contract`; die Preview ist eine nicht persistierte Edge-Function-Antwort.

## 2. Ist-Zustand MAUI

Der Mitgliedsantrag startet in `MemberDetailPage` über `MitgliedsantragDialogPage`, dann `BuildMitgliedsantragPreviewAsync`, `MitgliedsantragPreviewPage`, `SignatureFlowHelper`/`VertragsSignaturPage` und abschließend `CreateSignedMitgliedsantragDokumentAsync`.

Der Pachtvertrag startet in `MemberParzellenDetailPage` über `PachtvertragFlowHelper`, `PachtvertragDialogPage`, `BuildPachtvertragPreviewAsync`, `PachtvertragPreviewPage`, dieselbe Signaturseite und `CreateSignedPachtvertragDokumentAsync`.

MAUI verwendet dafür aktuell `SupabaseService` mit lokalen `MitgliedsantragDokumentFactory`-/`PachtvertragDokumentFactory`-Pfaden, nicht `kgv-generate-contract`. Die Preview-Seiten schreiben eine PDF-Kopie nach `FileSystem.AppDataDirectory/documents/persistent`, damit die Signaturseite sie öffnen kann. Die Signatur (`DigitalSignatureCapture` mit Strokes) bleibt nach der Analyse nur im lokalen Flow-State und wird an die Finalisierung übergeben; sie wird nicht in Preferences, SecureStorage oder einem Vertragsentwurf abgelegt. SecureStorage wird für Auth-Tokens verwendet, nicht für Signaturen.

## 3. Feldmapping

### Mitgliedsantrag

| WEB-v1 | MAUI-Quelle | Bewertung |
| --- | --- | --- |
| `memberId` | `MitgliedsantragDokumentRequest.MitgliedId` | identisch |
| `startDate` | `BeginnDatum` | `DateTime` nach ISO-Datum konvertieren |
| `memberFee` | `Mitgliedsbeitrag` | `decimal` nach JSON-Zahl |
| `representative` | Flags, Mitglied-ID und `GesetzlicherVertreterSnapshot` | Konverter erforderlich: `existing`/`manual` und Adressabweichung abbilden |
| `includeSecondaryMember` | `NebenmitgliedEinbeziehen` | identisch |

Die in MAUI vorhandenen Status-, Minderjährigkeits-, Snapshot- und Gebührenhilfswerte gehören nicht in den v1-Draft, sofern sie serverseitig erneut aufgelöst werden.

### Pachtvertrag

| WEB-v1 | MAUI-Quelle | Bewertung |
| --- | --- | --- |
| `startDate`, `parcelId` | `Vertragsbeginn`, `ParzelleId` | ISO-Datum/ID-Konvertierung |
| `hasPreviousContract` | nur `AltvertragDatum` plus Dialogentscheidung | explizites Boolean im MAUI-Modell ergänzen |
| `previousContractDate` | `AltvertragDatum` | Konvertierung `DateTime?` ↔ ISO/null |
| `includeSecondaryMember` | `IncludeSecondaryMember` | kompatibel, Nullable im MAUI normalisieren |
| `handoverDate`, `fixedUntil`, Gartenordnung, Vereinbarungen, Anlagen 1–4, Unterzeichner | im MAUI-Request vorhanden | UI-Eingaben fehlen derzeit überwiegend |

`kgv-prepared-contract/v1` ist deshalb ohne Versionswechsel wiederverwendbar. Persistierte Datumswerte bleiben überall ISO-Daten (`yyyy-MM-dd`); MAUI konvertiert an der UI-Grenze zu/von `DateTime.Date`.

## 4. Auth, Zuweisung und Concurrency

MAUI besitzt über `AuthService`/`UserContextState.CurrentUserId` sowie `UserContextService.current_app_user_context()` dieselbe Supabase-Identität und Rollenauflösung wie WEB. K4 muss daraus ohne Sonderregel `zugewiesen_an = auth.uid()` verwenden: eigene Vorgänge öffnen, unzugeordnete atomar claimen, fremde nur lesen und eigene gestartete Vorgänge fortsetzen.

MAUI verwendet heute keine Vertragsrevision. K4 übernimmt die WEB-Regeln unverändert: Laden liefert `revision`; ein inhaltlich anderer Draft wird per erwarteter Revision aktualisiert und übernimmt `revision + 1`; null aktualisierte Zeilen sind ein Konflikt. Kein Force-Update.

Der gegenwärtige Vertragsflow benötigt Netzwerk für Daten, Preview und Dokumentablage. Es existiert keine fachliche Offline-Synchronisierung; K4 führt kein lokales Prepared-Contract-Repository ein.

## 5. Gemeinsamer Zielworkflow

Beide Plattformen verwenden denselben Vorgang und denselben zentralen Generator:

```text
WEB oder MAUI → vorbereitet (v1-Draft) → optional bearbeiten/Preview
              → zur_signatur (Server friert Revision ein)
              → flüchtige Signaturen → generator finalize → erledigt
```

Ein vorbereitetes Dokument ist auf beiden Plattformen editierbar, sofern es dem angemeldeten Benutzer zugewiesen ist. Nach `zur_signatur` ist der Draft auf beiden Plattformen gesperrt; eine Wiederaufnahme startet lokal ohne frühere Teil-Signaturen. MAUI darf keine zweite fachliche PDF-Erzeugung für MA/PV behalten: K4 leitet Preview und Finalize auf `kgv-generate-contract` um.

## 6. Risiken und Teilaufgaben

1. **K4b:** Gemeinsame MAUI-Prepared-Contract-DTOs, REST-Repository für `kommunikationsvorgang`, Claim/Load/optimistic Update und v1-Konverter.
2. **K4c:** Mitgliedsantrag in MAUI auf Parken/Resume und den zentralen Generator umstellen.
3. **K4d:** Pachtvertrag-Editor um alle v1-Felder ergänzen und auf Parken/Resume/zentralen Generator umstellen.
4. **K4e:** MAUI-Signaturstart, eingefrorene Revision, Abschluss und flüchtige Signaturen mit dem gemeinsamen Vorgang verbinden; lokale persistente Preview-Kopien für diesen Vertragsflow entfernen.
5. **K4f:** WEB↔MAUI-Cross-Platform-Tests, Konflikte, Zuweisung, Signatur-Freeze und Abschluss testen.

Es ist keine neue Datenbankmigration für K4a erkennbar: Tabelle, RLS, Trigger, Active-Contract-Indizes und Edit-Guard sind bereits vorhanden. Eine Migration wird erst erforderlich, falls K4b bei der realen MAUI-REST-Anbindung eine serverseitig nicht ausdrückbare Schutzlücke nachweist.
