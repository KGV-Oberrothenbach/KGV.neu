# P11 Reparaturwelle 02 – aktueller technischer Abnahmestand

**Stand:** 19.11.2026. **Geltung:** Web, MAUI, Core, Infrastructure, Supabase. WPF ausdrücklich ausgeschlossen.

## Verifizierter Git-Zustand
- Repository `KGV-Oberrothenbach/KGV.neu`
- Branch `fix/p11-reparaturwelle-02`
- HEAD `3fc99b96da60d411186a9be02370591647df33d5`
- Der neue Regressionstest aus diesem Paket wurde noch **nicht** nach GitHub gepusht. GitHub-Connector-Schreibzugriff verweigert mit 403; lokaler Push erforderlich.

## Datenbankzustand
- Demo `ymxhszxfutjnljiuciyj`: 247 Migrationen, letzte `20261119012000`
- ProduktivDB `itjcabiibuodkxayhvjq`: 247 Migrationen, letzte `20261119012000`
- Keine SQL-Migration und keine produktive Vereinsdatenänderung durch Reparaturwelle 02.

## Edge-Functions-Demo
Alle 13 kanonischen Entrypoint-Dateien wurden mit GitHub verglichen und entsprechen `fix/p11-reparaturwelle-02`. Alle 13 `verify_jwt`-Werte entsprechen der versionierten `supabase/config.toml`.

| Edge Function | Demo-Version | VDB-Version | Demo-Entrypoint mit Repo identisch |
|---|---:|---:|---|
| kgv-complete-prepared-lease-contract | 3 | 1 | ja |
| kgv-complete-prepared-membership-application | 2 | 2 | ja |
| kgv-delete-user | 10 | 25 | ja |
| kgv-discard-prepared-contract | 2 | 1 | ja |
| kgv-generate-contract | 8 | 3 | ja |
| kgv-generate-export-pdf | 9 | 8 | ja |
| kgv-invite-user | 17 | 39 | ja |
| kgv-request-first-login-otp | 1 | 7 | ja |
| kgv-send-club-documents | 5 | 1 | ja |
| kgv-send-contract-documents | 2 | 1 | ja |
| kgv-upload-document | 15 | 9 | ja |
| kgv-upload-photo | 11 | 20 | ja |
| upload-prepared-contract-signature | 2 | 2 | ja |

Der bisherige Demo-Slug `clever-task` ist weiterhin registriert (Version 12), führt aber keinen OTP-Versand mehr aus; er antwortet mit HTTP 410. Der kanonische OTP-Endpunkt ist in Demo Version 1 aktiv.

## Korrigierte Abweichungen
- **Vereinsdokumentversand**: Deployment prüft nun `CanManageClubDocuments` statt bloßer Rollenetiketten.
- **Pachtvertragsabschluss / Vertragsverwerfung**: effektive Rechte vor dem Dienstrollenaufruf; beim Verwerfen kombinierte Rechte.
- **Dokument-Upload**: direkter DELETE-Pfad zur Drive-Dateilöschung entfernt; differenzierte Lese-/Schreib-/Archivberechtigung und keine veralteten Demospalten.
- **Signatur-Upload**: Identität, effektive Rechte, Signaturposition und Unterzeichnerbezug nach Repository.
- **Vertragsversand**: zusätzliche verifizierte Benutzeridentität vor dem Claim.
- **OTP**: korrekter Demo-Slug; altes Alias stillgelegt.
- **Einladungen/PDF-Erzeugung/Export**: Demo auf Repository-Fassungen gebracht.

## Tests und Status
- Vom Nutzer auf Commit `3fc99b96` lokal ausgeführt: 2/2 gezielte Tests, 116/116 Supabase-/Node-Tests, 425/425 .NET-Tests, Web-Produktionsbuild erfolgreich, MAUI-Android-Build erfolgreich.
- Der zusätzliche Regressionstest in diesem Paket ist **noch nicht im Repository gelaufen** und wird durch das beiliegende PowerShell-Skript ausgeführt.
- Demo-Deployments wurden auf Status `ACTIVE`, Version, JWT-Konfiguration und Entrypoint-Gleichheit geprüft. Das ersetzt **keinen** authentifizierten HTTP-End-to-End-Test.
- GitHub Actions: Für Commit `3fc99b96` wurde **kein** aktueller CI-Lauf gefunden. Ursache (z.B. Repository-Einstellungen) unbekannt.
- Ein isolierter VDB-Backup-Restore wurde in dieser Welle nicht durchgeführt.
- Lesende ACL-Inventur: 21 Trigger- und 28 Nicht-Trigger-`SECURITY DEFINER`-Funktionen haben `EXECUTE` für `anon`. Eine direkte Ausnutzbarkeit ist damit **nicht** nachgewiesen; eine Einzelfunktionsklassifizierung/Negativprüfung bleibt offen.

## Release-Sperren (nicht als abgeschlossen melden)
1. Authentifizierte HTTP-E2E- und Negativtests in Demo für Vereinsdokumentversand, Vertragsabschluss/-verwerfung, Dokument- und Signatur-Uploads.
2. Erfolgreicher aktueller GitHub-Actions-Lauf für Reparaturwelle 02. `push`-Trigger ist im Branch hinterlegt, tatsächlich wurde kein Lauf nachgewiesen.
3. Produktivangleichung der betroffenen Edge-Functions erst **nach** vollständiger Demo-Abnahme, gesonderter Freigabe und aktueller Backup-/Restore-Prüfung.
4. Berechtigungsaudit der direkt ausführbaren sicherheitsprivilegierten RPCs und sonstige nach P11 offene Fachbefunde.
5. Legacy-Demo-Slug `clever-task` kann nach Kompatibilitätsbestätigung aus dem Dashboard gelöscht werden; derzeit absichtlich mit 410 stillgelegt.

**Gesamtbewertung:** Demonstrationsseitiger Quellcode-Abgleich abgeschlossen; P11 Reparaturwelle 02 **nicht vollständig abgenommen / kein Go-live**. Keine unbewiesenen Testergebnisse als „bestanden“ verbuchen.
