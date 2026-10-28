# Phase 13 – MAUI / Android technische Bereinigung

## Ausgangslage

Ausgangsstand war Commit `55dad742d4312f45207a3082a78b474073d33a4c` auf `main` bei sauberem Arbeitsbaum. Der Android-Debug-Build von `KGV.Maui/KGV.Maui.csproj` für `net10.0-android` reproduzierte exakt 40 Compilerfehler.

Die historischen Stellen `ZaehlerwechselEinbauPage`, `AblesungErfassenPage` und `ArbeitseinsaetzeEditorPage` waren nicht Teil der aktuellen Compilerdiagnosen und wurden nicht geändert.

## Fehlergruppen

| Gruppe | Fehlercodes | Baseline-Anzahl | Ursache | Betroffene Dateien |
| --- | --- | ---: | --- | --- |
| MAUI-Typauflösung | `CS0103`, `CS0246` | 29 | Nach dem MAUI-/NET-10-Update wurden Typen wie `Thickness`, `LineBreakMode`, `TextAlignment`, `GridLength`, `DevicePlatform` und `MediaPicker` nicht mehr über die vorhandenen Usings aufgelöst. | `AuslaufendeWartungsvertraegePage.cs`, `HistoricalDocumentImportPage.cs`, `MemberClubDocumentsSection.cs`, `MembershipBoardReviewPage.cs`, `VereinsunterlagenPage.cs` |
| Binding-API | `CS7036` | 3 | `SetBinding` benötigt die Ziel-`BindableProperty`; die alten Kurzaufrufe passten nicht mehr zur aktuellen API. | `MembershipBoardReviewPage.cs` |
| gemeinsame Service-/Helper-Signaturen | `CS7036`, `CS1503` | 5 | Der inzwischen erforderliche Dokumenttyp fehlte bei Snapshot-, Nachweis- und Service-Aufrufen. | `MemberDetailPage.cs`, `PreparedMembershipApplicationPreviewFlow.cs` |
| nullable `DatePicker.Date` | `CS1503` | 1 | `DatePicker.Date` ist nullable und wurde direkt an einen nicht-nullbaren Parameter übergeben. | `HistoricalDocumentImportPage.cs` |
| Android-Typmehrdeutigkeit | `CS0104` | 2 | `Android.Graphics.Path` und `System.IO.Path` waren im Android-Build mehrdeutig. | `PdfViewerPage.cs` |

Nach Behebung der 40 Baseline-Diagnosen wurde noch ein zuvor verdeckter Aufruf von `VereinsunterlagenNachweisHelper.Equivalent` mit derselben Signaturdrift sichtbar und ebenfalls ausschließlich durch Übergabe von `PreparedContractConstants.MembershipApplication` angepasst.

## Fortschritt

```text
Baseline: 40 Fehler
nach den fünf Baseline-Gruppen: 1 zuvor verdeckter Folgefehler
nach dem letzten Aufruf derselben Service-Signaturgruppe: 0 Fehler
```

## Technische Korrekturen

- Fehlende MAUI-Namespaces wurden explizit ergänzt; UI, Layout und Fachregeln blieben unverändert.
- Die drei Bindings verwenden nun `Label.TextProperty` als Zielproperty.
- Mitgliedsantrags-Snapshots verwenden die bereits bestehende Konstante `PreparedContractConstants.MembershipApplication`; es wurde keine zweite MAUI-Fachlogik eingeführt.
- Das historische Dokumentdatum wird auf einen vorhandenen Wert geprüft. Bei fehlendem Datum erscheint eine Validierungsmeldung; es wird kein stiller Default eingeführt.
- Die beiden Dateipfad-Aufrufe sind explizit als `System.IO.Path` qualifiziert.
- XAML, Eventhandler, Manifest, Paketname und Versionswerte mussten nicht verändert werden.

## Builds und Android-Artefakte

- Debug: `dotnet build KGV.Maui/KGV.Maui.csproj -c Debug --no-restore -p:UseSharedCompilation=false` erfolgreich, 0 Fehler.
- Release: `dotnet build KGV.Maui/KGV.Maui.csproj -c Release --no-restore -p:UseSharedCompilation=false` erfolgreich, 0 Fehler.
- Core und Infrastructure wurden in beiden Builds erfolgreich mitgebaut.
- AAB: `KGV.Maui/bin/Release/net10.0-android/de.kgv.oberrothenbach-Signed.aab`, technisch erzeugt; enthält `base/dex/classes.dex` und `base/dex/classes2.dex`.
- APK: `KGV.Maui/bin/Release/net10.0-android/de.kgv.oberrothenbach-Signed.apk`, technisch erzeugt; enthält `classes.dex` und `classes2.dex`.
- Es traten keine DEX-, Assembly- oder Packagingfehler auf.
- Es wurde kein Artefakt verteilt, hochgeladen oder veröffentlicht.

## Linking, Trimming, AOT und Obfuscation

Die vorhandene Release-Konfiguration wurde nicht verändert:

- `AndroidLinkMode=None`
- `PublishTrimmed=false`
- `RunAOTCompilation=false`
- `AndroidEnableProfiledAot=false`

Damit waren keine Keep-Regeln für Reflection, JSON, Supabase, PDF oder Biometrie erforderlich. Es ist keine eigene Obfuscation-/ProGuard-Konfiguration aktiv. Ein neues Obfuscation-System wurde bewusst nicht eingeführt.

## Paket, Version und Permissions

- Paketname unverändert: `de.kgv.oberrothenbach`.
- Version unverändert: `0.8.1` / Version Code `203`.
- Im zusammengeführten Release-Manifest: `INTERNET`, `ACCESS_NETWORK_STATE`, `CAMERA`, `USE_BIOMETRIC`, `NFC` sowie die vom Android-Build app-spezifisch erzeugte `DYNAMIC_RECEIVER_NOT_EXPORTED_PERMISSION`.
- NFC bleibt als nicht erforderliches Hardwaremerkmal (`required=false`) markiert.
- Durch Phase 13 wurde keine Permission hinzugefügt.

## Regressionstests

- 36/36 fokussierte .NET-Tests bestanden: Biometrie-Session-Scope, Mitgliedsantrag-Resume, Wartungsvertrag-Signing/Resume-nahe Abläufe, Signaturfortschritt, Vereinskonfigurations- und Vereinsunterlagen-Snapshots sowie Supabase-Dokumentladen.
- 36/37 zusammen ausgeführte Node-Contract-Fälle bestanden: MAUI-Zugangsprüfung, Workflow Resume/Retry einschließlich Mitgliedsantrag/Pachtvertrag/Wartungsvertrag, Vereinsfunktionen und Vereinsunterlagen-Snapshots.
- Der einzelne Fehler ist der bereits veraltete Web-Quelltexttest `document-proxy.contract.test.mjs`, der in `MemberDetailPage` noch den alten Ausdruck `DownloadDokumentContentAsync(document)` erwartet. Die produktive MAUI-Korrektur aus einer abgeschlossenen Phase wurde nicht zurückgedreht und Web wurde in Phase 13 nicht verändert.
- Ein kompletter Lauf von `KGV.Tests` zeigt weitere vorbestehende, nicht durch Phase 13 verursachte Altfehler in PDF-Template-Erwartungen, Demo-Filtern und einem versiegelten `DispatchProxy` des Lease-Resume-Testaufbaus. Die Phase-13-relevanten Tests wurden deshalb zusätzlich separat und erfolgreich ausgeführt.
- Passwortlogin, gültiger KGV-Zugang, Biometrieaktivierung/-login, Session-Löschung beim Vereinswechsel sowie Demo-/Produktiv-Scope sind durch `BiometricSessionScopeTests` und `maui-valid-access.contract.test.mjs` regressionsgeprüft.

## Verbleibende Warnungen

Die Builds enthalten weiterhin bereits bekannte Warnungen, insbesondere:

- `NU1902`/`NU1903` für `SixLabors.ImageSharp` 2.1.13,
- bestehende Nullable-Warnungen,
- obsolete MAUI-Dialog-APIs,
- Nullable-Signaturen der beiden Value Converter.

Diese Warnungen blockieren den Build nicht und werden gemäß Auftrag nicht mit den Compilerfehlern vermischt. Ihre Generalbereinigung gehört in Phase 14.

Es wurden keine Migration, kein Deployment, kein Test-/Produktivrelease und keine sonstige Releaseaktion ausgeführt.

**Phase 13 – MAUI / Android technische Bereinigung = 100 %.**
