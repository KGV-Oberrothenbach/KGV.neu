# KGV Fortschritt ausführlich

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
