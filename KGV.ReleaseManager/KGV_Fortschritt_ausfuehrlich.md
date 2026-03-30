# KGV Fortschritt ausführlich

## Stand 2026-03-30 – Interner Härtungsblock: transaktionalen Echt-Release, Rollback und Abschlussstatus präzisiert

### Ziel dieses Schritts
Den bestehenden Release-Manager nach dem Preflight-Block so härten, dass der eigentliche Echt-Release intern nachvollziehbar schrittweise läuft, bei Fehlern sauber in einen Rollbackpfad geht und im UI einen ehrlichen Abschlussstatus zeigt.

### Geprüft
- echter Git-Stand gegen `origin/main`
- direkt betroffene Release-Manager-Dateien:
  - `Services/ReleaseExecutionService.cs`
  - `Services/ReleaseMarkerService.cs`
  - `Services/VersionService.cs`
  - `Services/ReleaseFolderService.cs`
  - `Services/ReleaseVersionFileService.cs`
  - `Services/GitCommandService.cs`
  - `ViewModels/MainViewModel.cs`
  - `MainWindow.xaml`
  - `MainWindow.xaml.cs`
  - `Models/ReleaseExecutionRequest.cs`
  - `Models/ReleaseExecutionResult.cs`

### Ehrlicher Istzustand vor Umsetzung
- der Release-Ablauf war bereits funktionsfähig, aber intern noch nicht als klare transaktionale Schrittkette sichtbar modelliert
- Versionserhöhung lief über `ReleaseVersionFileService.WriteTargetVersion(...)`
- Rollback schrieb Backupdateien zwar zurück, lieferte aber noch keinen differenzierten Erfolgszustand
- Marker wurde vor Git geschrieben, Commit und Push waren aber noch zusammengezogen; Teilfehler nach lokalem Commit wurden nicht mehr streng als transaktionaler Fehler ausgewertet
- im UI fehlte ein sichtbarer Abschlussstatus mit Schrittliste

### Umgesetzt
- neue Status-/Ergebnis-Modelle für:
  - Gesamtstatus
  - Schrittergebnisse
  - Restore-Ergebnis
- `ReleaseExecutionService` intern auf fachlich getrennte Schritte gezogen:
  - Ausgangsversionen lesen
  - Versionen erhöhen/schreiben
  - WPF-Artefakte bauen
  - Android-APK bauen
  - Android-AAB bauen
  - Veröffentlichungsordner befüllen
  - Marker schreiben
  - Commit ausführen
  - Push ausführen
  - Rollback
  - Abschluss
- `VersionService` wird jetzt auch im Echt-Release verwendet, damit die Ausgangsversionen explizit gelesen und protokolliert werden
- `ReleaseVersionFileService.RestoreBackups(...)` gibt jetzt ein Restore-Ergebnis mit Einzelmeldungen zurück
- lokales Git-Rollback ergänzt:
  - Ausgangs-HEADs werden vor Commit erfasst
  - lokale Commits können bei Fehlern vor erfolgreichem Push per `reset --hard` zurückgesetzt werden
- Push-Reihenfolge so angepasst, dass das markerführende Quellrepo zuletzt gepusht wird
- bereits erfolgte Pushes werden bei Fehlern nicht beschönigt, sondern führen jetzt sichtbar zu `rollback unvollständig`
- der bestehende Statusbereich zeigt jetzt zusätzlich den Release-Abschlussstatus und die Release-Schrittliste

### Ergebnis
- der Echt-Release ist jetzt intern deutlich nachvollziehbarer und fachlich sauberer bewertet
- bei Fehlern nach Versionsschreiben wird klar zwischen erfolgreichem und unvollständigem Rollback unterschieden
- Marker, Commit und Push zählen nur im Vollerfolg final als abgeschlossen

### Validierung
- `dotnet build KGV.ReleaseManager/KGV.ReleaseManager.csproj -c Debug -clp:ErrorsOnly` erfolgreich
- `dotnet build KGV.slnx -c Debug -clp:ErrorsOnly` erfolgreich

### Logische Prüfung
- Dry Run bleibt marker-/commit-/push-frei
- echte Fehler nach Versionsschreiben laufen in den Rollbackpfad
- Marker/Commit/Push werden nur im Vollerfolg final als `ja` bewertet
- bereits gepushte Zustände werden nicht als sauber rückgesetzt behauptet, sondern als `rollback unvollständig` ausgewiesen

## Stand 2026-03-29 – Interner Betriebsblock: sichtbaren Preflight-/Systemcheck vor Dry Run und Echt-Release ergänzt

### Ziel dieses Schritts
Den bestehenden Release-Manager-Funktionsblock minimal-invasiv um einen echten Systemcheck vor dem Release-Start erweitern, damit externe Tools, Repos, Pfade und Schlüsseldateien vor Dry Run und Echt-Release nachvollziehbar geprüft werden.

### Geprüft
- echter Git-Stand gegen `origin/main`
- direkt betroffene Release-Manager-Dateien:
  - `MainWindow.xaml`
  - `MainWindow.xaml.cs`
  - `ViewModels/MainViewModel.cs`
  - `Models/ReleaseExecutionRequest.cs`
  - `Models/ReleaseExecutionResult.cs`
  - `Services/ReleaseExecutionService.cs`
  - `Services/ReleaseMarkerService.cs`
  - `Services/SettingsService.cs`
  - `Services/GitCommandService.cs`
  - `Services/BuildCommandService.cs`
  - `Services/ProcessExecutionService.cs`
  - `Services/ReleaseArtifactService.cs`
  - `Services/ReleaseFolderService.cs`
  - `Services/VersionService.cs`

### Ehrlicher Istzustand vor Umsetzung
- vorhanden waren bereits Settings-Validierung, Request-Validierung, Versionslesung, Releaseordner-Vorbereitung sowie Marker-/Git-/Build-Abläufe
- ein sichtbarer, separater Systemcheck vor Dry Run/Echt-Release fehlte jedoch noch
- Pflichtprüfungen wie `Git`/`ISCC` aufrufbar, Repos initialisiert, Projektdateien lesbar, Ausgabepfade beschreibbar/erstellbar und Keystore vorhanden waren im UI noch nicht gesammelt sichtbar
- der bestehende Statusbereich rechts war der sinnvollste Ort für die Ergänzung; ein zweiter konkurrierender Bereich war nicht nötig

### Umgesetzt
- neue Preflight-Modelle und `ReleasePreflightService` ergänzt
- Pflichtprüfungen werden jetzt lesbar einzeln gesammelt und mit `OK` / `Warnung` / `Fehler` angezeigt
- der bestehende Statusbereich zeigt jetzt zusätzlich:
  - Gesamtaussage `bereit` / `eingeschränkt` / `nicht startbar`
  - Button `Systemcheck`
  - Ergebnisliste der einzelnen Pflichtchecks
- der Preflight prüft je nach ausgewähltem Releaseziel u. a.:
  - Quellrepo vorhanden
  - Git-Executable vorhanden und aufrufbar
  - Quellrepo/WPF-Zielrepo als Git-Repo initialisiert und mit `origin` lesbar
  - Projekt-/Versionsdateien lesbar
  - Release-Ausgabeordner beschreibbar
  - WPF-Zielrepo vorhanden
  - WPF-Setup-Skript lesbar
  - Inno Setup aufrufbar
  - Android-Projekt vorhanden
  - APK-/AAB-Ausgabepfade vorhanden oder erstellbar
  - Android-Keystore vorhanden
  - Android-Keystore-Alias gesetzt
- `GitCommandService` löst `git.exe` jetzt zusätzlich aus bekannten lokalen Installationspfaden auf, damit Systemcheck und späterer Git-Releasepfad nicht an einem fehlenden PATH-Eintrag scheitern
- `RunDryRelease` und `RunRelease` führen jetzt automatisch zuerst denselben Preflight aus
- bei Preflight-Fehlern wird vor dem eigentlichen Release sauber abgebrochen; Marker, Commit und Push bleiben in diesem Fall unangetastet

### Ergebnis
- der Release-Manager zeigt vor einem echten Release jetzt nachvollziehbar an, ob die Umgebung bereit ist
- Dry Run und Echt-Release teilen denselben vorgeschalteten Systemcheck
- ein echter Release kann nicht mehr halb anlaufen, wenn Pflichtpfade/Tools vorab bereits fehlen

### Validierung
- `dotnet build KGV.ReleaseManager/KGV.ReleaseManager.csproj -c Debug -clp:ErrorsOnly` erfolgreich
- `dotnet build KGV.slnx -c Debug -clp:ErrorsOnly` erfolgreich

### Logische Prüfung
- bei gültiger Umgebung kann der Systemcheck auf `bereit` gehen
- bei Pflichtfehlern blockiert der Release vor Passwortabfrage, Marker, Commit und Push
- Dry Run bleibt markerfrei und schreibfrei

## Stand 2026-03-29 – Interner Prüfblock: Release-Manager-End-to-End-Fluss gegengeprüft und Markeranzeige vereinheitlicht

### Ziel dieses Schritts
Den bereits auf `main` abgeschlossenen Release-Manager-Block nicht neu umbauen, sondern den fachlichen End-to-End-Fluss im bestehenden Produktpfad gegenprüfen und nur direkte Restlücken minimal schließen.

### Geprüft
- echter Git-Stand gegen `origin/main`
- direkt betroffene Release-Manager-Dateien:
  - `MainWindow.xaml.cs`
  - `ViewModels/MainViewModel.cs`
  - `Services/VersionService.cs`
  - `Services/LogExtractionService.cs`
  - `Services/ReleaseNotesAnalysisService.cs`
  - `Services/ReleaseNotesImportExportService.cs`
  - `Services/ReleaseNotesHistoryService.cs`
  - `Services/ReleaseExecutionService.cs`
  - `Services/ReleaseMarkerService.cs`

### End-to-End-Befund
- Versions-Refresh bleibt korrekt auf den echten Projektdateien `KGV.Wpf.csproj` und `KGV.Maui.csproj`
- Export bleibt markerbasiert; ohne Marker bleibt der Initialfall erhalten
- Import bleibt für WPF-/Android-Abschnitte funktionsfähig
- Dry Run bleibt schreibfrei; Echt-Release behält Marker-/Artefakt-/Git-Pfad
- reale Restlücke saß noch in der zentralen Release-Anzeige: dort wurde noch nicht zwingend derselbe Marker verwendet wie im Delta-Export und beim Releaseabschluss

### Umgesetzt
- `ReleaseMarkerService` um einen menschenlesbaren Statustext für den neuesten Release-Marker ergänzt
- `MainWindow.xaml.cs` verwendet für `LastKnownReleaseText` jetzt bevorzugt denselben Fortschrittslog-Marker wie der Delta-Export
- nur wenn kein Marker vorhanden ist, fällt die zentrale Anzeige weiter auf den bisherigen Historienanker zurück
- Dry-Run-Erfolgsmeldung in `ReleaseExecutionService` fachlich präzisiert: keine Marker, Commits oder Pushes

### Ergebnis
- zentrale UI-Anzeige, Delta-Export und Releaseabschluss verwenden jetzt denselben Marker-Anker
- Dry-Run-Rückmeldung ist im End-to-End-Fluss klarer
- kein neuer Fachumfang begonnen

### Validierung
- `dotnet build KGV.ReleaseManager/KGV.ReleaseManager.csproj -c Debug -clp:ErrorsOnly` erfolgreich

### Abgrenzung
- kein echter produktiver Release mit externen Tools/realen Zugangsdaten durchgeführt
- geprüft und korrigiert wurde nur der direkte bestehende Codepfad des Release Managers

## Stand 2026-03-29 – Interner Abschlusslauf: Release-Manager-Block final gegengeprüft und Vollvalidierung belastbar bestätigt

### Ziel dieses Schritts
Den bereits begonnenen Release-Manager-Block ohne neuen Fachumfang sauber abschließen, den letzten Marker-Status-Patch gegen den realen Stand gegenlesen, alle geforderten Builds belastbar einzeln bestätigen und erst danach den Git-Abschluss freigeben.

### Geprüft
- echter Git-Stand gegen `origin/main`:
  - `git fetch origin`
  - `git status -sb`
  - `git branch -vv`
  - Divergenzprüfung via `git log origin/main..HEAD` und `git log HEAD..origin/main`
- direkt betroffene Release-Manager-Dateien:
  - `KGV.ReleaseManager/MainWindow.xaml.cs`
  - `KGV.ReleaseManager/Services/ReleaseExecutionService.cs`
  - `KGV.ReleaseManager/Services/ReleaseNotesAnalysisService.cs`
  - `KGV.ReleaseManager/Services/LogExtractionService.cs`
  - `KGV.ReleaseManager/Services/ReleaseMarkerService.cs`
  - `KGV.ReleaseManager/Services/GitCommandService.cs`
  - `KGV.ReleaseManager/Models/ReleaseExecutionRequest.cs`
- zusätzlich geprüft:
  - bestehende Änderung in `.github/copilot-instructions.md`

### Ehrlicher Istzustand vor Abschluss
- der begonnene Release-Manager-Block lag weiterhin uncommittet vor
- `.github/copilot-instructions.md` war zusätzlich bereits geändert
- der kleine Nachschärfungs-Patch in `MainWindow.xaml.cs` für die zentrale markerbasierte Statusanzeige war im aktuellen Stand vorhanden
- aus der direkten Gegenprüfung ergaben sich keine neuen Compile-, Using- oder Namensfehler in den betroffenen Release-Manager-Dateien
- der in den Instructions genannte Git-Pfad unter `...Visual Studio\2022\...\git.exe` existierte lokal nicht; für die belastbare Git-Prüfung wurde daher der vorhandene Visual-Studio-Git-Pfad `C:\Program Files\Microsoft Visual Studio\18\Community\Common7\IDE\CommonExtensions\Microsoft\TeamFoundation\Team Explorer\Git\cmd\git.exe` verwendet

### Umgesetzt
- kein weiterer Produktcode geändert, weil im realen Istzustand keine direkte Restlücke mehr offen war
- nur die Abschlussdokumentation mit dem tatsächlich verifizierten Stand fortgeschrieben

### Ergebnis
- der Release-Manager-Block blieb fachlich unverändert erhalten:
  - Git Add/Commit/Push im Release-Flow
  - Release-Marker
  - markerbasierter Delta-Export
  - Versions-Refresh-Button
  - markerbasierte Statusanzeige im WPF-UI
- die Vollvalidierung wurde nun belastbar einzeln bestätigt
- die zusätzliche Änderung in `.github/copilot-instructions.md` wurde nur transparent geprüft, nicht fachlich neu umgebaut

### Validierung
- `dotnet build KGV.ReleaseManager/KGV.ReleaseManager.csproj -c Debug -clp:ErrorsOnly` erfolgreich
- `dotnet build KGV.Wpf/KGV.Wpf.csproj -c Debug -clp:ErrorsOnly` erfolgreich, vorhandene Warnungen bleiben außerhalb dieses Blocks
- `dotnet build KGV.Maui/KGV.Maui.csproj -c Debug -clp:ErrorsOnly` erfolgreich, vorhandene Warnungen in `HomeManagementPage.cs` bleiben außerhalb dieses Blocks
- `dotnet build KGV.slnx -c Debug -clp:ErrorsOnly` erfolgreich

## Stand 2026-03-29 – Interner Funktionsblock: Commit/Push, Release-Marker, Delta-Export und Versions-Refresh im WPF-Release-Flow ergänzt

### Ziel dieses Schritts
Den vorhandenen WPF-Release-Manager minimal-invasiv so erweitern, dass der echte Erfolgsablauf Commit/Push auslösen kann, ein maschinenlesbarer Release-Marker in das Fortschrittslog geschrieben wird, Exporttexte nur noch das Delta seit dem letzten Marker verwenden und die aktuellen Projektversionen manuell neu geladen werden können.

### Geprüft
- reale Git-/Release-Ablaufspfade:
  - `KGV.ReleaseManager/Services/GitCommandService.cs`
  - `KGV.ReleaseManager/Services/ReleaseExecutionService.cs`
  - `KGV.ReleaseManager/Services/ProcessExecutionService.cs`
  - `KGV.ReleaseManager/Services/ReleaseArtifactService.cs`
  - `KGV.ReleaseManager/Services/ReleaseVersionFileService.cs`
- reale Analyse-/Exportpfade:
  - `KGV.ReleaseManager/Services/LogExtractionService.cs`
  - `KGV.ReleaseManager/Services/ReleaseNotesAnalysisService.cs`
  - `KGV.ReleaseManager/Services/ReleaseNotesImportExportService.cs`
- reale UI-/Versionspfade:
  - `KGV.ReleaseManager/MainWindow.xaml`
  - `KGV.ReleaseManager/MainWindow.xaml.cs`
  - `KGV.ReleaseManager/Services/VersionService.cs`
  - `KGV.ReleaseManager/README.md`

### Ehrlicher Istzustand vor Umsetzung
- `GitCommandService` bot real nur `git status -sb`
- Commit/Push waren im erfolgreichen WPF-Release-Ablauf noch nicht verdrahtet
- der Exporttext nutzte noch den bisherigen Release-Historienanker statt eines echten maschinenlesbaren Release-Markers im Fortschrittslog
- ein expliziter Button zum erneuten Einlesen der aktuellen Versionen aus den Projektdateien fehlte in der WPF-Oberfläche

### Umgesetzt
- `GitCommandService` um echte Git-Kommandos ergänzt:
  - `status --porcelain`
  - `add -A`
  - `commit`
  - `push`
- einfache Standard-Commitnachrichten eingeführt:
  - `Release {version}: source release state`
  - `Release {version}: publish WPF setup artifacts`
- neuer `ReleaseMarkerService` ergänzt
- Markerformat fest definiert:
  - `- [RELEASE_MARKER] Version {version} erfolgreich erstellt am {yyyy-MM-dd HH:mm}`
- Marker-Schreiblogik:
  - nur bei erfolgreichem Echt-Release
  - nie bei Dry Run
  - nie bei Fehler/Rollback
  - Einfügen als letzter Punkt des aktuellen obersten Fortschrittsabschnitts statt blind am Dateiende
- `ReleaseExecutionService` erweitert:
  - schreibt Marker nach erfolgreicher Artefaktveröffentlichung
  - stößt danach Commit/Push für Quellrepo und WPF-Zielrepo an
  - Git-Fehler vor lokalem Commit laufen weiter in den Rollbackpfad
  - Git-Fehler nach lokalem Commit werden sichtbar gemeldet statt den Arbeitsbaum künstlich zurückzusetzen
- `LogExtractionService` extrahiert jetzt das Log-Delta seit dem letzten Release-Marker
- `ReleaseNotesAnalysisService` verwendet dieses Delta direkt für `ChangesPreview` und Clipboard-Export
- Initialfall ohne Marker bleibt sauber erhalten und verwendet den gesamten relevanten Logbereich
- `MainWindow` um den Button `Aktuelle Versionen neu einlesen` ergänzt
- der Button nutzt die bestehende `VersionService`-Leselogik, speichert nichts und löst keinen Release aus
- UI-Hinweis im Release-Bereich ergänzt, dass Marker sowie Commit/Push Teil des erfolgreichen Echt-Release-Flows sind
- `README.md` um Marker-Format, Delta-Export-Verhalten und Commit/Push-Einordnung ergänzt

### Ergebnis
- der WPF-Release-Flow kann jetzt reale Git-Schreiboperationen auslösen
- Markerbasierte Delta-Exporte sind für die nächste Versionszusammenfassung vorbereitet
- Versionen lassen sich manuell direkt aus den Projektdateien neu laden

### Validierung
- `dotnet build KGV.ReleaseManager/KGV.ReleaseManager.csproj -c Debug` erfolgreich
- `dotnet build KGV.Wpf/KGV.Wpf.csproj -c Debug` erfolgreich
- `dotnet build KGV.Maui/KGV.Maui.csproj -c Debug` erfolgreich, nur bereits bekannte Warnungen
- `dotnet build KGV.slnx -c Debug` erfolgreich

## Stand 2026-03-28 – Interner Korrekturblock: Android-Versionserkennung in `VersionService` für SDK-Style-`csproj` gehärtet

### Ziel dieses Schritts
Die direkte Android-Versionslesung aus `KGV.Maui/KGV.Maui.csproj` minimal korrigieren, damit `ApplicationDisplayVersion` und `ApplicationVersion` im Release Manager wieder zuverlässig erscheinen, ohne die Rückkehr zu Nebenpfaden.

### Geprüft
- reale Projektdateien:
  - `KGV.Maui/KGV.Maui.csproj`
  - `KGV.Wpf/KGV.Wpf.csproj`
- reale Leselogik:
  - `KGV.ReleaseManager/Services/VersionService.cs`

### Ehrlicher Istzustand vor Umsetzung
- `ApplicationDisplayVersion` war im MAUI-`csproj` korrekt gesetzt
- die Android-Version erschien im Release Manager trotzdem als nicht gefunden
- Ursache war real nicht das Fehlen der Version, sondern die Dictionary-basierte XML-Auslesung über alle Properties: im MAUI-`csproj` kommen mehrere Property-Namen in verschiedenen konditionalen `PropertyGroup`-Blöcken mehrfach vor, wodurch die Dictionary-Erzeugung abbricht und die Lesung leer zurückfällt

### Umgesetzt
- `GetPropertyValue(...)` in `VersionService` auf LocalName-basierte Iteration statt `ToDictionary(...)` umgestellt
- die Suche läuft weiter direkt auf der geladenen `csproj`-XML und bleibt robust gegen SDK-Style-/Namespace-Strukturen
- keine Fallbacks auf `AssemblyInfo`, Manifest oder sonstige Nebenpfade eingeführt

### Ergebnis
- `ApplicationDisplayVersion` kann wieder direkt aus `KGV.Maui.csproj` gelesen werden
- `ApplicationVersion` bleibt als Android-VersionCode weiter nutzbar
- die WPF-Versionslesung aus `KGV.Wpf.csproj` bleibt unverändert intakt

### Validierung
- `dotnet build KGV.ReleaseManager/KGV.ReleaseManager.csproj -c Debug` erfolgreich
- `dotnet build KGV.Wpf/KGV.Wpf.csproj -c Debug` erfolgreich
- `dotnet build KGV.Maui/KGV.Maui.csproj -c Debug` erfolgreich, nur bereits bekannte Warnungen
- `dotnet build KGV.slnx -c Debug` erfolgreich

## Stand 2026-03-28 – Interner Inbetriebnahmeblock: csproj-basierte Produktversionen und getrennte WPF-/Android-Historien eingeführt

### Ziel dieses Schritts
Den Release Manager so umstellen, dass Produktversionen ausschließlich direkt aus `KGV.Wpf/KGV.Wpf.csproj` und `KGV.Maui/KGV.Maui.csproj` gelesen/geschrieben werden und WPF/Android getrennte Verlaufsstände für spätere Einzel- oder Sammelreleases behalten.

### Geprüft
- reale Versionsquellen im Quellrepo:
  - `KGV.Wpf/KGV.Wpf.csproj`
  - `KGV.Wpf/AssemblyInfo.cs`
  - `KGV.Maui/KGV.Maui.csproj`
- bestehende ReleaseManager-Pfade:
  - `KGV.ReleaseManager/Services/VersionService.cs`
  - `KGV.ReleaseManager/Services/ReleaseVersionFileService.cs`
  - `KGV.ReleaseManager/Services/ReleaseExecutionService.cs`
  - `KGV.ReleaseManager/Services/ReleaseNotesAnalysisService.cs`
  - `KGV.ReleaseManager/Services/ReleaseNotesImportExportService.cs`
  - `KGV.ReleaseManager/ViewModels/MainViewModel.cs`
  - `KGV.ReleaseManager/MainWindow.xaml`
  - `KGV.ReleaseManager/MainWindow.xaml.cs`

### Ehrlicher Istzustand vor Umsetzung
- Android war bereits sauber direkt im MAUI-`csproj` versioniert
- WPF hatte noch keine eigene Produktversion direkt in der Projektdatei
- die Versionslogik nutzte noch Fallbacks auf `AssemblyInfo.cs` bzw. Android-Manifest und behandelte WPF bei Drift implizit als führend
- die lokale Release-Historie war bislang nur als gemeinsame Datei ausgeprägt und damit nicht sauber produktgetrennt nutzbar

### Umgesetzt
- `KGV.Wpf/KGV.Wpf.csproj` um `<Version>0.2.6</Version>` ergänzt
- `VersionService` liest WPF jetzt nur noch aus `Version`/projektbezogenen Versionseigenschaften in `KGV.Wpf.csproj`
- Android wird nur noch aus `ApplicationDisplayVersion` und `ApplicationVersion` in `KGV.Maui.csproj` gelesen
- alle Fallbacks auf `AssemblyInfo.cs`, Android-Manifest und sonstige Nebenpfade entfernt
- gemeinsame Zielversion wird erst aus der aktuell gewählten Release-Kombination abgeleitet; bei gemeinsamem Release mit unterschiedlichem Stand aus dem höheren Versionsstand statt aus WPF allein
- `ReleaseVersionFileService` schreibt nur noch in die ausgewählten `csproj`-Dateien und aktualisiert bei Einzelrelease nicht mehr automatisch das jeweils andere Produkt
- lokale Historienpfade getrennt aufgeteilt in:
  - `%LocalAppData%\KGV.ReleaseManager\release-notes-history-wpf.json`
  - `%LocalAppData%\KGV.ReleaseManager\release-notes-history-android.json`
- Oberfläche zeigt jetzt getrennte aktuelle Produktversionen und getrennte Historienstände/Pfade an
- Import von Release-Zusammenfassungen akzeptiert jetzt produktbezogen WPF-only, Android-only oder beide zusammen passend zur Auswahl

### Ergebnis
- der Release Manager hängt für Produktversionen nicht mehr an AssemblyInfo- oder Manifest-Nebenpfaden
- WPF besitzt jetzt eine eigene direkte Versionsquelle in der Projektdatei
- WPF- und Android-Verläufe können getrennt gespeichert und separat angezeigt werden
- Einzelreleases für nur WPF oder nur Android schreiben nur noch die jeweils nötigen Versionsdateien fort

### Validierung
- `dotnet build KGV.ReleaseManager/KGV.ReleaseManager.csproj -c Debug` erfolgreich
- `dotnet build KGV.Wpf/KGV.Wpf.csproj -c Debug` erfolgreich
- `dotnet build KGV.Maui/KGV.Maui.csproj -c Debug` erfolgreich, nur bereits bekannte Warnungen
- `dotnet build KGV.slnx -c Debug` erfolgreich

### Abgrenzung
- kein automatischer GitHub-/Store-Release ergänzt
- kein neuer Fachumfang außerhalb des Release Managers gestartet
- Release-/Rollback-Pfad strukturell beibehalten; End-to-End-Veröffentlichung wurde in diesem Block nicht mit produktiven Signatur-/Setup-Werkzeugen durchlaufen

## Stand 2026-03-28 – Interner Inbetriebnahmeblock: Android-Signing-Pfad mit Laufzeitpasswörtern produktionsreif gehärtet

### Ziel dieses Schritts
Nur innerhalb von `KGV.ReleaseManager/` den Android-Signing-Pfad so absichern, dass signierte APK- und AAB-Builds mit Laufzeitpasswörtern sauber vorbereitet, ohne Klartextspeicherung verwendet und mit verständlichen Fehlern validiert werden können.

### Geprüft
- vorhandene Android-Settings im Release Manager:
  - `AndroidKeystorePath`
  - `AndroidKeystoreAlias`
  - `AndroidPackageName`
  - `PlayTrackName`
- reale Android-Buildparameter im Quellrepo:
  - `KGV.Maui/KGV.Maui.csproj`
  - reale Release-/Debug-Property-Groups für `AndroidPackageFormat`, `AndroidKeyStore` und `AndroidCreatePackagePerAbi`
- vorhandener Laufzeitdialog `RuntimeSecretPromptService`
- bestehende Android-Buildverkabelung in `BuildCommandService` und `ReleaseExecutionService`

### Ehrlicher Istzustand vor Umsetzung
- Keystore-Pfad, Alias und Package Name waren bereits als Settings vorhanden
- Store- und Key-Passwort wurden bereits nur zur Laufzeit abgefragt und nicht persistiert
- die `dotnet publish`-Commandline trug die Signing-Passwörter aber noch direkt als MSBuild-Parameter
- Android-Fehlertexte hätten Laufzeitpasswörter im Fehlerfall potenziell in Statusausgaben sichtbar machen können
- der Komfortfall `Key-Passwort = Keystore-Passwort` war nur implizit über ein leeres Feld gelöst, noch nicht als klare Option

### Umgesetzt
- Android-Signierungsdialog ergänzt um die explizite Laufzeitoption `Key-Passwort = Keystore-Passwort`
- `AndroidSigningSecrets` erweitert, damit der aktuelle Laufmodus ohne Passwortinhalte nachvollziehbar bleibt
- Android-Buildkommandos verwenden jetzt echte temporäre Prozess-Umgebungsvariablen für Store-/Key-Passwort statt Klartext in der Commandline
- Android-Buildkommandos erzwingen jetzt `AndroidCreatePackagePerAbi=false`, damit für Release Manager-Läufe eine einzelne signierte APK bzw. AAB sauber gefunden werden kann
- konfigurierter `AndroidPackageName` wird jetzt produktiv als `ApplicationId`-Override in den Android-Buildpfad übernommen
- Android-Prozessmeldungen schwärzen Laufzeitpasswörter aus Status- und Fehlertexten
- Laufzeitpasswörter werden nach der Release-Ausführung im Requestobjekt direkt wieder geleert
- UI-Hinweis ergänzt, dass Keystore- und Key-Passwort nicht in den Settings gespeichert werden

### Ergebnis
- Android-Signing ist im Release Manager jetzt fachlich sauberer für signierte APK- und AAB-Läufe vorbereitet
- Laufzeitpasswörter werden weiterhin nicht gespeichert und zusätzlich nicht mehr über die Build-Commandline transportiert
- Fehler bei fehlendem Keystore oder fehlendem Alias bleiben verständlich, ohne Passwörter in Statusmeldungen zu leaken

### Validierung
- `dotnet build KGV.ReleaseManager/KGV.ReleaseManager.csproj -c Debug` erfolgreich
- `dotnet build KGV.Maui/KGV.Maui.csproj -c Debug` erfolgreich, nur bereits bekannte Warnungen
- `dotnet build KGV.slnx -c Debug` erfolgreich
- servicebasierte Validierung bestätigt:
  - Android-Buildcommand enthält keine Klartextpasswörter mehr
  - Laufzeitpasswörter liegen nur in temporären Prozess-Umgebungsvariablen
  - Settings-Datei speichert keine Passwortfelder
  - fehlender Keystore liefert verständliche Meldung
  - fehlender Alias liefert verständliche Meldung
  - redigierte Fehlertexte enthalten keine Laufzeitpasswörter

### Abgrenzung
- keine Änderungen an MAUI-Produktcode oder Android-App-Logik außerhalb des Release Managers
- keine Änderungen am WPF-Installerpfad außer unverändertem Parallelbetrieb
- kein echter End-to-End-Signbuild mit produktivem Keystore durchgeführt, weil sensible reale Daten in diesem Block nicht verwendet wurden

## Stand 2026-03-28 – Interner Inbetriebnahmeblock: reales Inno-Setup-Skript für den WPF-Installerpfad ergänzt

### Ziel dieses Schritts
Nur den WPF-Installerpfad so vorbereiten, dass im aktuellen Quellrepo ein reales, belastbares Inno-Setup-Skript vorhanden ist und der Release Manager dieses Skript mit der Zielversion kompilieren kann.

### Geprüft
- reale Skriptlage im Quellrepo:
  - keine vorhandene `*.iss`-Datei in `KGV.neu`
  - keine belastbaren Installer-/Setup-/Packaging-Reste mit nutzbarem Inno-Skript
- reale WPF-Ausgabe im Projekt:
  - `KGV.Wpf\bin\Debug\net8.0-windows\KGV.Wpf.exe`
  - Projekt `KGV.Wpf/KGV.Wpf.csproj`
- reale Zielstruktur im lokalen Repo `C:\Programmieren\Restore KGV\KGV-WPF`:
  - `KGV-Setup.exe`
  - `KGV-Setup-0.2.4.exe`
  - `KGV-Setup-0.2.5.exe`
  - `KGV-Setup-0.2.6.exe`
  - `releases.json`
  - `version.json`

### Ehrlicher Istzustand vor Umsetzung
- im Quellrepo existierte noch kein reales `.iss`-Skript
- der Release Manager konnte den Inno-Compiler bereits aufrufen, hatte aber noch keine belastbare Skriptgrundlage
- die lokale Zielrepo-Struktur zeigte bereits die reale Namenslogik `KGV-Setup-<Version>.exe` und `KGV-Setup.exe`

### Umgesetzt
- neues reales Inno-Setup-Skript unter `KGV.Wpf/Installer/KGV.Wpf.iss` ergänzt
- Skript basiert auf der realen WPF-Releaseausgabe `KGV.Wpf\bin\Release\net8.0-windows`
- Hauptstartdatei ist real `KGV.Wpf.exe`
- Installer-Metadaten und Zielstruktur sauber definiert:
  - App-Name `KGV`
  - Publisher `KGV Oberrothenbach`
  - Default-Installationsordner `{autopf}\KGV`
  - Startmenüeintrag `KGV`
  - optionales Desktop-Icon nur als Benutzeraufgabe
- `BuildCommandService` und `ReleaseExecutionService` minimal ergänzt, damit `ISCC.exe` die Zielversion als `AppVersion`-Define erhält

### Ergebnis
- im Quellrepo ist jetzt genau ein reales `.iss`-Skript vorhanden und durch den Release Manager eindeutig auffindbar
- die bestehende Namenslogik `KGV-Setup-<Version>.exe` bleibt mit dem lokalen Zielrepo konsistent
- der Release Manager braucht für diesen Block kein zusätzliches Skript-Setting, weil der Pfad durch genau ein Skript stabil erkennbar ist

### Validierung
- `dotnet build KGV.Wpf/KGV.Wpf.csproj -c Release` erfolgreich
- `dotnet build KGV.ReleaseManager/KGV.ReleaseManager.csproj -c Debug` erfolgreich
- `dotnet build KGV.slnx -c Debug` erfolgreich
- `ISCC.exe` konnte mit dem neuen Skript grundsätzlich aufgerufen werden
- echter Skript-Compile-Lauf mit realer WPF-Releaseausgabe erfolgreich

### Abgrenzung
- keine Änderungen an MAUI/Android
- kein GitHub Release
- kein automatischer Upload ins lokale WPF-Repo durch diesen Block allein
- keine unnötigen Umbauten am Release Manager über die minimale Version-Define-Übergabe hinaus

## Stand 2026-03-28 – Interner Entwicklungsblock: Log-Auswertung seit letztem Release, Exporttext und versionierter Import ergänzt

### Ziel dieses Schritts
Nur innerhalb von `KGV.ReleaseManager/` die textliche Release-Aufbereitung so ergänzen, dass reale Änderungen seit dem letzten Release aus den vorhandenen Logs ermittelt, für ChatGPT exportiert und die fertigen WPF-/Android-Texte lokal versioniert gespeichert werden können.

### Geprüft
- vorhandene Settings-Persistenz, Versionslogik, Zielversion, Veröffentlichungsordner, Logquelle und Release-Ablauf-Grundgerüst in `KGV.ReleaseManager`
- reale Loglage im konfigurierten Quellrepo nur lesend:
  - `KGV_Fortschrittslog_ausfuehrlich.md`
  - `DEV_LOG.md`
- weitere reale Changelog-/Release-History-Dateien im Quellrepo: keine zusätzlichen produktbezogenen Release-History-Dateien gefunden

### Ehrlicher Istzustand vor Umsetzung
- `ReleaseNotesImportExportService` konnte bisher nur einen einfachen Prompt aus dem neuesten Logabschnitt erzeugen
- ein belastbarer Release-Anker für "seit letztem Release" fehlte noch vollständig
- importierte ChatGPT-Zusammenfassungen wurden bisher nicht versioniert für WPF und Android gespeichert
- die bestehende Oberfläche zeigte noch keinen klaren Status des letzten gespeicherten Releases und keine Vorschau des ausgewerteten Logbereichs

### Umgesetzt
- neue lokale Release-Notiz-Historie unter `%LocalAppData%\KGV.ReleaseManager\release-notes-history.json` ergänzt
- neue Analyse des relevanten Logabschnitts seit dem letzten gespeicherten Release-Anker ergänzt
- primäre Logquelle bleibt `KGV_Fortschrittslog_ausfuehrlich.md`; bei Fehlen wird weiter sauber auf `DEV_LOG.md` zurückgefallen
- ReleaseManager-interne Änderungen werden bei der Exportbasis aus Endnutzer-Release-Notizen herausgefiltert
- wenn noch kein belastbarer letzter Release-Anker vorhanden ist, wird der neueste relevante Logabschnitt ausdrücklich als erster sinnvoller Startzustand vorgeschlagen statt stillschweigend angenommen
- der Exporttext enthält jetzt:
  - Zielversion
  - ausgewerteten Logbereich
  - Rohzusammenfassung der relevanten Änderungen
  - klaren ChatGPT-Prompt mit Zielstruktur für `WPF / Download` und `Android / Play Store`
- Zwischenablage-Kopie für den kopierfertigen Exporttext ergänzt
- Import von ChatGPT-Zusammenfassungen prüft jetzt mindestens die Abschnitte `## WPF / Download` und `## Android / Play Store`
- gespeicherte Einträge enthalten jetzt lokal versioniert:
  - Version
  - Datum/Zeit
  - Logquelle
  - Release-Anker
  - WPF-Release-Text
  - Android-/Play-Store-Text
  - Rohimport

### Ergebnis
- der Release Manager kann jetzt reale Änderungen seit dem letzten gespeicherten Release ermitteln oder verständlich melden, wenn kein belastbarer Anker vorhanden ist
- Exporttext ist kopierfertig und auf die gewünschte ChatGPT-Ausgabe zugeschnitten
- importierte Release-Zusammenfassungen werden lokal versioniert gespeichert und beim nächsten Start wieder als letzter bekannter Release-Stand berücksichtigt

### Validierung
- `dotnet restore KGV.ReleaseManager/KGV.ReleaseManager.csproj` erfolgreich
- `dotnet build KGV.ReleaseManager/KGV.ReleaseManager.csproj -c Debug` erfolgreich
- `dotnet build KGV.Maui/KGV.Maui.csproj -c Debug` erfolgreich, nur bereits bekannte Warnungen
- `dotnet build KGV.slnx -c Debug` erfolgreich
- Workspace-Build erfolgreich
- Starttest des WPF-Tools über `KGV.ReleaseManager.exe` erfolgreich
- servicebasierter Test bestätigt:
  - primäre Logquelle wird erkannt
  - ohne gespeicherten Release-Anker wird ein verständlicher Startzustand vorgeschlagen
  - Exporttext wird erzeugt
  - ein Testimport mit `WPF / Download` und `Android / Play Store` wird lokal gespeichert und wieder geladen

### Abgrenzung
- kein GitHub Release
- kein automatischer Play-Store-Upload
- keine Veröffentlichung nach außen
- keine Implementierungsänderungen außerhalb von `KGV.ReleaseManager/`

## Stand 2026-03-28 – Interner Entwicklungsblock: echte Release-Ausführung mit Versionsschreibung, Build, Artefaktkopie und Rollback-Grundlage verdrahtet

### Ziel dieses Schritts
Nur innerhalb von `KGV.ReleaseManager/` den ersten echten Release-Ablauf so ergänzen, dass die Zielversion in real vorhandene Produktdateien geschrieben, WPF-/Android-Builds gestartet, Artefakte in den Versionsordner übernommen und Versionsstände bei Fehlern zurückgesetzt werden können.

### Geprüft
- vorhandene Settings-Persistenz
- vorhandene Versionslogik, Zielversion, Veröffentlichungsordner und Logquelle in `KGV.ReleaseManager`
- reale Build-/Versionsstellen im konfigurierten Quellrepo nur lesend:
  - `KGV.Wpf/KGV.Wpf.csproj`
  - `KGV.Wpf/AssemblyInfo.cs`
  - `KGV.Maui/KGV.Maui.csproj`
  - `KGV.Maui/Platforms/Android/AndroidManifest.xml`
- vorhandene ReleaseManager-Services `BuildCommandService`, `VersionService`, `ReleaseFolderService`, `LogExtractionService`

### Ehrlicher Istzustand vor Umsetzung
- der Release Manager konnte bereits Settings speichern, Versionen erkennen und Versionsordner vorbereiten
- echte Release-Ausführung hinter `Release starten` war noch komplett Platzhalterlogik
- `BuildCommandService` erzeugte bisher nur ein einfaches `dotnet publish`-Scaffold
- es gab noch keine robuste Prozessausführung mit ExitCode-/StdOut-/StdErr-Auswertung
- es gab noch keine Versionssicherung und kein Rollback
- im aktuell gelesenen Produktstand ist für Versionsschreibung real vorhanden:
  - in `KGV.Maui/KGV.Maui.csproj` die Felder `ApplicationDisplayVersion` und `ApplicationVersion`
  - in `KGV.Wpf` aktuell keine explizite Produktversion in `csproj` oder `AssemblyInfo`, die in diesem Block sicher beschrieben werden müsste
- im aktuell gelesenen Repo wurde außerdem kein `.iss`-Skript für Inno Setup gefunden; die WPF-Setup-Erzeugung muss deshalb sauber fehlschlagen bzw. im Dry Run transparent melden, solange kein reales Skript vorhanden ist

### Umgesetzt
- `ReleaseManagerSettings` um `AndroidKeystoreAlias` ergänzt; Android-Passwörter bleiben weiterhin ungespeichert und werden zur Laufzeit abgefragt
- bestätigte lokale Standardpfade für `KGV.neu`, `KGV-WPF`, den Release-Root sowie die APK-/AAB-Zielordner werden jetzt automatisch als Startwerte vorbelegt; der bestätigte lokale Pfad zu `ISCC.exe` wird zusätzlich nur dann vorbelegt, wenn die Datei real vorhanden ist
- neue Modelle für Prozessausführung, Versionsbackup, Versionsschreibergebnis und Release-Ergebnis ergänzt
- `BuildCommandService` erzeugt jetzt reale Befehle für:
  - `dotnet build` für WPF
  - `dotnet publish` für Android APK
  - `dotnet publish` für Android AAB
  - Inno-Setup-Kompilierung über `ISCC.exe`
- `ProcessExecutionService` führt Prozesse robust aus und sammelt ExitCode, Standardausgabe und Fehlerausgabe
- `ReleaseVersionFileService` sichert Originaldateien, schreibt die Zielversion in real vorhandene Versionsfelder und kann bei Fehlern den Ursprungszustand zurückschreiben
- in `KGV.Maui/KGV.Maui.csproj` werden jetzt für Releases die real vorhandenen Felder `ApplicationDisplayVersion` und `ApplicationVersion` aktualisiert
- WPF-Versionen werden nur beschrieben, wenn im Zielrepo tatsächlich relevante Felder vorhanden sind; im aktuellen Stand ist das nicht der Fall
- `ReleaseArtifactService` erkennt Inno-Setup-Skripte, sucht erzeugte Artefakte, bestimmt die reale Zielstruktur im lokalen `KGV-WPF`-Repo und kopiert Artefakte in die vorbereiteten Zielordner
- `ReleaseExecutionService` orchestriert jetzt den Ablauf:
  1. Preflight / Dry Run
  2. Versionsordner vorbereiten
  3. Versionsdateien sichern
  4. Zielversion schreiben
  5. WPF-Build / Setup versuchen
  6. APK erzeugen
  7. AAB erzeugen
  8. Artefakte in den Versionsordner kopieren
  9. WPF-Setup zusätzlich in das lokale `KGV-WPF`-Repo kopieren, wenn die Zielstruktur eindeutig ist
  10. APK und AAB zusätzlich in die konfigurierten Ausgabeordner kopieren
  11. bei Fehlern Rollback der Versionsdateien
- `MainWindow` minimal erweitert um:
  - Setting für `ISCC.exe`
  - Setting für `Keystore-Alias`
  - klaren Release-Bereich mit Zielauswahl, Dry Run und echtem Release-Start
- Android-Signierungs-Passwörter werden über einen Laufzeitdialog abgefragt und nicht in die Settings-Datei geschrieben

### Ergebnis
- der Release Manager kann jetzt real einen Release-Ablauf starten statt nur Status-Placeholders zu schreiben
- fehlende Tools, fehlende Skripte, fehlende Keystore-Daten oder fehlende Artefakte führen zu verständlichen Fehlern
- wenn nach erfolgreicher Versionsschreibung ein späterer Schritt fehlschlägt, ist ein Rollback der geänderten Versionsdateien vorgesehen und implementiert
- Artefakte werden bei Erfolg in den Versionsordner unter `WPF`, `Android/APK` und `Android/AAB` kopiert
- bei eindeutiger Zielstruktur wird das WPF-Setup zusätzlich in das lokale Repo `C:\Programmieren\Restore KGV\KGV-WPF` kopiert; APK/AAB werden zusätzlich in die konfigurierten Ausgabeordner kopiert

### Validierung
- `dotnet restore KGV.ReleaseManager/KGV.ReleaseManager.csproj` erfolgreich
- `dotnet build KGV.ReleaseManager/KGV.ReleaseManager.csproj -c Debug` erfolgreich
- `dotnet build KGV.Maui/KGV.Maui.csproj -c Debug` erfolgreich, nur bereits bekannte Warnungen
- `dotnet build KGV.slnx -c Debug` erfolgreich
- Workspace-Build erfolgreich
- Starttest des WPF-Tools über `KGV.ReleaseManager.exe` erfolgreich (`APP_STARTED=True`)
- temporärer Settings-Service-Test bestätigt die vorbelegten Pfade für Quellrepo, WPF-Zielrepo und Release-Root (`DEFAULT_SOURCE=...03_Arbeitsstand`, `DEFAULT_WPF_TARGET=...KGV-WPF`, `DEFAULT_RELEASE_ROOT=...Releases\KGV`)
- lokaler Inno-Setup-Pfad wurde vom Benutzer bestätigt: `C:\Users\Braen\AppData\Local\Programs\Inno Setup 6\ISCC.exe`
- servicebasierter Dry Run mit realem Repo-Pfad liefert verständlichen Fehler bei fehlendem WPF-Setup-Tool (`DRYRUN_SUCCESS=False`, Meldung zu fehlendem `ISCC.exe`)
- servicebasierter echter Android-Release-Test mit absichtlich ungültigem Signaturartefakt schlägt wie erwartet fehl und setzt die vorher geschriebene MAUI-Version wieder zurück (`EXECUTE_SUCCESS=False`, `EXECUTE_ROLLEDBACK=True`, `MauiRestored=True`)
- lokal weiterhin nicht bis zur erfolgreichen WPF-Setup-Erzeugung testbar, weil im aktuellen Produktstand kein reales `.iss`-Skript gefunden wurde
- lokal weiterhin nicht bis zu erfolgreichen signierten APK-/AAB-Artefakten testbar, weil dafür ein echter Keystore und gültige Laufzeitpasswörter nötig sind
- **Zusätzliche Validierungsergebnisse:**
  - Alle relevanten Pakete wurden erfolgreich wiederhergestellt.
  - Der Build des Hauptprojekts `KGV.ReleaseManager` war erfolgreich.
  - Die gesamte Lösung konnte ohne Fehler gebaut werden.
  - Das gestartete WPF-Tool konnte erfolgreich ausgeführt werden.
  - Der Dry Run für den Release-Prozess zeigte fehlende Teile (wie das WPF-Setup-Tool) korrekt an.
  - Der Versuch, ein echtes Release für Android durchzuführen, schlug aufgrund eines ungültigen Signaturartefakts fehl, was zu einem rollback der Änderungen führte.

### Abgrenzung
- kein GitHub Release
- kein automatischer Upload zum Play Store
- keine Veröffentlichung von Release Notes nach außen
- keine Implementierungsänderungen außerhalb von `KGV.ReleaseManager/`
- keine spekulativen Produktversionsdateien außerhalb real vorhandener Felder angelegt

## Stand 2026-03-28 – Interner Entwicklungsblock: Versionslogik, Zielversion, Veröffentlichungsordner und Logquellen-Grundlage verdrahtet

### Ziel dieses Schritts
Das bestehende `KGV.ReleaseManager`-Grundgerüst nur innerhalb von `KGV.ReleaseManager/` so erweitern, dass aktuelle Versionsstände aus dem konfigurierten `KGV.neu`-Pfad gelesen, eine Zielversion vorgeschlagen, ein Veröffentlichungsordner bewusst vorbereitet und die primäre Logquelle robust erkannt werden kann.

### Geprüft
- vorhandenes Settings-Modell
- vorhandene Services `SettingsService`, `VersionService`, `ReleaseFolderService`, `LogExtractionService`
- vorhandenes `MainViewModel`
- aktuelle `MainWindow`-Struktur
- reale Versionsquellen im konfigurierten Quellrepo nur lesend:
  - `KGV.Wpf/KGV.Wpf.csproj`
  - `KGV.Wpf/AssemblyInfo.cs`
  - `KGV.Maui/KGV.Maui.csproj`
  - `KGV.Maui/Platforms/Android/AndroidManifest.xml`
- vorhandene ReleaseManager-Logs `CHANGELOG.md` und `KGV_Fortschritt_ausfuehrlich.md`

### Ehrlicher Istzustand vor Umsetzung
- das Projekt besaß bereits Platzhalter für `CurrentVersion`, `NextVersion`, Releaseordner und Logauszug
- eine echte Ermittlung der Version aus dem konfigurierten `KGV.neu`-Pfad war noch nicht verdrahtet
- die Zielversion konnte bislang nur als einfacher Patch-Inkrement-Platzhalter behandelt werden
- der Veröffentlichungsordner wurde zwar angelegt, behandelte vorhandene Ordner aber noch nicht transparent genug
- die Logquelle war noch auf einen lokalen Platzhalterpfad im Ausgabeverzeichnis gerichtet statt auf das echte Quellrepo
- im aktuell gelesenen Produktstand wurde real erkannt:
  - `KGV.Maui/KGV.Maui.csproj` enthält `ApplicationDisplayVersion 0.2.6` und `ApplicationVersion 14`
  - `KGV.Wpf/KGV.Wpf.csproj` und `KGV.Wpf/AssemblyInfo.cs` enthalten im aktuellen Stand keine explizite Produktversionsangabe
  - `AndroidManifest.xml` enthält aktuell keine eigene `versionName`

### Umgesetzt
- kleine Modelle für Versionserkennung, Logquellenstatus und Veröffentlichungsordnerstatus ergänzt
- `VersionService` liest jetzt den konfigurierten Quellpfad robust aus und prüft echte Versionsquellen in `KGV.Wpf` und `KGV.Maui`
- unterstützt werden jetzt die Versionssprünge:
  - `Patch`
  - `Minor`
  - `Major`
- bei Versionsdrift zwischen WPF und Android wird jetzt ausdrücklich gewarnt, ohne eine automatische Korrektur auszulösen
- wenn keine Version sauber ermittelt werden kann, bleibt die App stabil und zeigt eine verständliche Statusmeldung statt eines Absturzes
- `MainViewModel` hält jetzt den erkannten WPF-/Android-Stand, den Versionsstatus, die Warnung bei Drift, die vorgeschlagene Zielversion sowie den Status der Logquelle
- `MainWindow` minimal erweitert um die Bereiche:
  - `Version`
  - `Logquelle`
  - `Veröffentlichungsordner`
- `ReleaseFolderService` bereitet einen Versionsordner jetzt bewusst per Aktion vor und legt nur die Struktur an:
  - `<Version>/WPF`
  - `<Version>/Android/APK`
  - `<Version>/Android/AAB`
  - `<Version>/Dokumentation`
- vorhandene Versionsordner werden jetzt sauber als bereits vorhanden gemeldet; es gibt keine zerstörerische Logik
- `LogExtractionService` erkennt jetzt primär `KGV_Fortschrittslog_ausfuehrlich.md` im konfigurierten Repo und nutzt bei Bedarf nur lesend `DEV_LOG.md` als Fallback
- der bestehende Export-Prompt nutzt jetzt die erkannte Logquelle aus dem konfigurierten Quellrepo statt einen lokalen Platzhalterpfad

### Ergebnis
- der Release Manager kann jetzt aus dem konfigurierten `KGV.neu`-Pfad reale Versionsstände lesen oder verständlich melden, wenn keine saubere Quelle vorliegt
- eine Zielversion wird aus dem ausgewählten Versionssprung vorgeschlagen
- ein Veröffentlichungsordner kann bewusst vorbereitet werden, ohne Buildartefakte zu kopieren
- die primäre Logquelle im Quellrepo wird gefunden oder sauber als fehlend/nicht lesbar markiert

### Validierung
- dateibezogene Fehlerprüfung der geänderten `KGV.ReleaseManager`-Dateien blieb unauffällig
- Restore-/Build-/Solution-Validierung folgt im Abschlusslauf dieses Blocks
- eine echte GUI-Interaktion wurde in diesem Dokumentationsschritt noch nicht automatisiert durchgeklickt; die Runtime-Absicherung erfolgt hier codebasiert über robuste Pfad-, Datei- und Parse-Behandlung

### Abgrenzung
- keine echten Builds oder Release-Automation ergänzt
- keine Git-Automation ergänzt
- keine Inno-Setup-Ausführung ergänzt
- keine APK-/AAB-Erstellung ergänzt
- keine Signierung ergänzt
- keine Produktversionsdateien außerhalb des Release Managers beschrieben
- keine Änderungen außerhalb von `KGV.ReleaseManager/` umgesetzt

## Stand 2026-03-28 – Interner Entwicklungsblock: Settings-Modell, Laden/Speichern und Grund-UI verdrahtet

### Ziel dieses Schritts
Ein kleines, belastbares Fundament für die Konfiguration des `KGV.ReleaseManager` schaffen, ohne bereits Build-, Git-, Inno- oder Android-Signing-Automation fachlich zu verdrahten.

### Geprüft
- vorhandenes Settings-Modell
- vorhandenes MainViewModel
- vorhandener `SettingsService`
- aktuelle `MainWindow`-Struktur
- vorhandene Logdateien `CHANGELOG.md` und `KGV_Fortschritt_ausfuehrlich.md`

### Umgesetzt
- `ReleaseManagerSettings` um `StoreUrl` ergänzt und Normalisierung der Settings-Werte zentralisiert
- `SettingsService` auf robuste JSON-Persistenz mit verständlichen Rückmeldungen umgestellt
- beim Start werden gespeicherte Einstellungen automatisch geladen
- beschädigte oder fehlende Settings-Dateien führen nicht zum Absturz, sondern zu Defaultwerten mit sauberer Statusmeldung
- `MainViewModel` validiert jetzt Pflichtfelder und grundlegende Pfadangaben
- `MainWindow` fachlich in die Bereiche `Projektpfade`, `Android / Play Store` und `Veröffentlichung` gegliedert
- klarer Speichern-Button am Ende des Settings-Formulars

### Ergebnis
- Einstellungen können lokal gespeichert und beim Start wieder geladen werden
- Hauptpfade werden vor dem Speichern verständlich validiert
- Store-Felder sind vorbereitet, ohne schon Release-Automation auszulösen

### Abgrenzung
- keine echte Release-Automation
- keine Git-, Build-, Inno- oder Android-Signing-Logik verdrahtet
- keine Endnutzer-Release-Notes des KGV-Produkts verändert

## Stand 2026-03-28 – Interner Entwicklungscheck: Buildfähigkeit bestätigt

### Ziel dieses Schritts
Nur prüfen, ob der bereits eingefügte `KGV.ReleaseManager` im aktuellen Repo-Stand buildfähig ist, und nur bei echten Buildblockern minimal korrigieren.

### Geprüft
- Solution-Einbindung des Projekts
- `KGV.ReleaseManager.csproj` mit `TargetFramework net8.0-windows` und `UseWPF=true`
- `App.xaml` / `App.xaml.cs`
- `MainWindow.xaml` / `MainWindow.xaml.cs`
- Restore und Projekt-Build
- kompletter Solution-Build

### Ergebnis
- `KGV.ReleaseManager` war im aktuellen Stand sofort buildfähig.
- Es waren keine minimalen Codekorrekturen nötig.
- Das Projekt baut sauber als WPF-Desktopprojekt.
- Die gesamte Solution baut ebenfalls weiter erfolgreich.

### Abgrenzung
- Keine fachliche Release-Logik erweitert.
- Keine UI-Funktion ergänzt.
- Keine ReleaseManager-Änderung in Endnutzer-Release-Notes übernommen.

## Stand 2026-03-28

### Ausgangspunkt
Die bisherigen vier Threads zum KGV Release Manager wurden konsolidiert. Das Ziel ist ein lokales Windows-Werkzeug, das aus dem Projekt `KGV.neu` reproduzierbare Releases für WPF und Android vorbereitet.

### In diesem Stand angelegt
- Projektordner `KGV_ReleaseManager_Projekt`
- WPF-Projektgerüst `KGV.ReleaseManager`
- Solution-Datei
- Dokumentation und technische Startstruktur
- vorbereitete Modelle und Services
- Skriptvorlagen für Build- und Release-Abläufe
- ausführlicher Fortschrittslog als Startdokument

### Fachlich festgehalten
- Versionen sollen automatisch erhöht werden
- pro Version wird ein Veröffentlichungsordner erstellt
- WPF liefert eine Setup-Datei
- Android liefert eine signierte APK und eine signierte AAB
- Einstellungen sollen lokale Pfade und Zielorte speichern
- Release-Inhalte sollen aus dem Fortschrittslog exportiert werden
- bei Fehlschlag ist ein Versionsrollback vorgesehen

### Noch offen
- echte Ausführung externer Tools
- Parsing der Versionsnummern aus den realen KGV-Projektdateien
- echtes Git-Pushen in Zielrepos
- Android-Signing-Parameter und sichere Passwortbehandlung
- UI für Import/Export der Release-Notizen
- Fehlerbehandlung und Fortschrittsanzeige im Detail

### Nächster sinnvoller Block
- Settings-Datei und UI vollständig verdrahten
- echte Versionsquellen im KGV.neu-Repo anbinden
- Release-Ordner und Exporttexte testbar machen
