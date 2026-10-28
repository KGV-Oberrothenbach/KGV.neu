# Phase 14 – technische Warnungen

## Ausgangslage

Ausgangsstand: 9a2e7f392b4c85ca5617b33a59f02f9824f14ccb, HEAD entspricht origin/main, sauberer Arbeitsbaum.

Ein vollständiger Android-Release-Rebuild ergab 0 Fehler und 161 Warnungen:

- 25 wiederholt ausgegebene NU1902/NU1903-Sicherheitswarnungen für ImageSharp,
- 136 Nullable-/obsolete-API-Warnungen aus eigenem Code.

Nach den risikoarmen Korrekturen ergab der vollständige Build 0 Fehler und 140 Warnungen. Ein anschließender inkrementeller Release-Packaging-Build meldete nur noch die 25 ImageSharp-Paketwarnungen.

## A – behoben

- document-proxy.contract.test.mjs erwartet den aktuellen produktiven MAUI-Ablauf: Nur DokumentePage lädt an dieser Stelle direkt über DownloadDokumentContentAsync(document); die anderen geprüften Flows dürfen weiterhin keine direkte Dokument-URL auflösen.
- Die relevante Node-Suite ist damit vollständig grün: 37/37.
- Drei obsolete DisplayAlert-Aufrufe verwenden nun DisplayAlertAsync.
- Beide Board-Review-Converter entsprechen der nullable IValueConverter-Signatur.
- Initialisierungsreihenfolge im Wartungsvertrag-Editor korrigiert, ohne Verhalten zu ändern.
- Vereinskonfiguration verwendet eine echte nullable Zustandsangabe mit Validierung statt einer falschen Non-Nullable-Deklaration.
- Lokale Nullability in Mitgliedsanzeige, PDF-Signaturquelle und Parzellenprotokoll wurde durch Flow-Checks bzw. bereits validierte lokale Werte geklärt.

## ImageSharp

KGV.Core referenziert ImageSharp 2.1.13 direkt. NuGet meldet dazu zwei moderate und drei hohe Security-Advisories (NU1902/NU1903).

Die aktuellen stabilen Linien 4.1.3 und 3.2.0 wurden jeweils per Restore und Test-Build geprüft. Beide stoppen den Build ohne SixLaborsLicenseKey bzw. Lizenzdatei. Da keine Lizenz vorliegt und der Auftrag keine größere Paket-/Lizenzmigration erlaubt, wurde der Paketstand wieder auf 2.1.13 gesetzt. Bildverarbeitungslogik wurde nicht verändert.

Das Update ist damit kein technisch risikoloser Paketwechsel, sondern ein eigener Lizenz-/Migrationspunkt.

## B – bewusst verbleibend

- Historische Nullability-Drift in KGV.Infrastructure/Services/SupabaseService.cs (überwiegend CS8603, außerdem einzelne CS8602, CS8604, CS8629). Eine sichere Bereinigung erfordert die systematische Anpassung zahlreicher Supabase-Response-/Mapping-Signaturen.
- Sechs Warnstellen in KGV.Infrastructure/Authentication/AuthService.cs. Sie betreffen externe Auth-/Postgrest-Rückgaben und sollten gemeinsam mit den Serviceverträgen gehärtet werden.
- Zwei Warnstellen in KGV.Core/Utilities/ExportPdfBuilder.cs; ihre Korrektur erfordert eine konsistente Nullable-Signatur für den gesamten Exportpfad und wurde nicht mit punktuellen Nullunterdrückungen kaschiert.

## C – extern bzw. lizenzabhängig

- ImageSharp 2.1.13: bekannte Advisories; sichere aktuelle Paketlinien verlangen in dieser Buildumgebung eine Six-Labors-Lizenz.
- Wiederholte Ausgabe derselben Paketwarnungen entsteht durch die Referenzkette Core zu Infrastructure/MAUI und mehrere Android-Buildziele.

## Regression

- Core und Infrastructure: erfolgreich über die MAUI-Builds sowie die fokussierte Test-Suite gebaut.
- MAUI Debug: erfolgreich, 0 Fehler.
- MAUI Release: erfolgreich, 0 Fehler.
- APK: de.kgv.oberrothenbach-Signed.apk technisch neu erzeugt.
- AAB: de.kgv.oberrothenbach-Signed.aab technisch neu erzeugt.
- Node-Contracts: 37/37 bestanden.
- Fokussierte .NET-Tests: 36/36 bestanden.
- Biometrie-/Zugangscontract bleibt grün; Passwortlogin, gültiger Zugang, Biometrieangebot und Root-Wechsel wurden nicht verändert.

Keine Versionsänderung, Migration, Datenbankänderung, Deployment-, Template-Aktivierungs- oder Releaseaktion wurde durchgeführt.

**Phase 14 – technische Warnungen = 100 %.**
