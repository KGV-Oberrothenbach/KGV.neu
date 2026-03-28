# KGV Fortschritt ausführlich

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
