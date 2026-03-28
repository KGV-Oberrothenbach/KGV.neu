# Changelog

Alle nennenswerten Änderungen am KGV Release Manager werden hier dokumentiert.

## [0.1.7-internal] - 2026-03-28
### Hinzugefügt
- direkte WPF-Produktversion in `KGV.Wpf/KGV.Wpf.csproj`
- getrennte lokale WPF-/Android-Historien für Release-Zusammenfassungen und Versionsanker

### Geändert
- Produktversionen werden jetzt ausschließlich direkt aus `KGV.Wpf/KGV.Wpf.csproj` und `KGV.Maui/KGV.Maui.csproj` gelesen
- Zielversion wird jetzt erst aus der tatsächlichen Release-Auswahl für WPF und/oder Android abgeleitet
- Versionsschreibung aktualisiert nur noch die ausgewählten Produkte statt immer beide Pfade mitzuschreiben
- ReleaseManager-Oberfläche zeigt aktuelle WPF- und Android-Versionen sowie getrennte Historienpfade separat an

### Behoben
- Abhängigkeit der Versionslogik von `AssemblyInfo.cs`, Android-Manifest und sonstigen Nebenpfaden entfernt
- Versionsdrift zwischen WPF und Android erzwingt keinen falschen gemeinsamen Wert mehr beim Laden

## [0.1.6-internal] - 2026-03-28
### Hinzugefügt
- Laufzeitoption für Android-Signing mit `Key-Passwort = Keystore-Passwort` direkt im Signierungsdialog

### Geändert
- Android-Signing-Passwörter laufen jetzt nur noch über temporäre Prozess-Umgebungsvariablen statt als Klartext in der `dotnet publish`-Commandline
- Android-Buildbefehle erzwingen jetzt eine einzelne Artefaktausgabe und können den konfigurierten `ApplicationId`-/Package-Name sauber in den Build übernehmen
- fehlgeschlagene Android-Buildmeldungen schwärzen Laufzeitpasswörter aus UI- und Statusausgaben

### Behoben
- bisher hätten Android-Signing-Passwörter im Fehlerfall über Prozessausgaben oder über die Build-Commandline sichtbar werden können

## [0.1.5-internal] - 2026-03-28
### Hinzugefügt
- reales Inno-Setup-Skript unter `KGV.Wpf/Installer/KGV.Wpf.iss` als stabile Grundlage für den WPF-Installerpfad

### Geändert
- Inno-Setup-Aufruf übergibt jetzt die Zielversion als `AppVersion`-Define an das Skript, damit Installer-Metadaten und Dateiname auf demselben Release-Stand basieren

### Behoben
- bisher fehlte im Quellrepo ein belastbares `*.iss`-Skript für den WPF-Releasepfad

## [0.1.4-internal] - 2026-03-28
### Hinzugefügt
- lokale versionierte Release-Notiz-Historie als JSON-Speicher für Version, WPF-Text, Android-/Play-Store-Text und Rohimport
- echte Log-Auswertung seit dem letzten gespeicherten Release-Anker mit Filter auf ReleaseManager-interne Änderungen

### Geändert
- `MainWindow` zeigt jetzt letzten gespeicherten Release-Stand, Vorschau der ermittelten Änderungen, kopierfertigen Exporttext und einen klaren Importbereich für ChatGPT-Zusammenfassungen
- Exporttext enthält jetzt Zielversion, ausgewerteten Logbereich, Rohzusammenfassung und einen strikten ChatGPT-Prompt für `WPF / Download` sowie `Android / Play Store`
- Import von Zusammenfassungen prüft jetzt die benötigten Abschnitte, speichert sie lokal versioniert und stellt sie für spätere Releases wieder bereit

### Behoben
- bisherige Exportfunktion nutzte nur den neuesten Logabschnitt und hatte keinen belastbaren Release-Anker
- bisheriger Zusammenfassungsimport speicherte keine versionierte WPF-/Android-Releasebasis

## [0.1.3-internal] - 2026-03-28
### Hinzugefügt
- echte Release-Ausführung im `KGV.ReleaseManager` mit Dry Run, Versionsschreibung, Prozessausführung, Artefakt-Suche und Rollback-Grundlage
- neue Statusmodelle für Prozessausführung, Versionsbackups und Release-Ergebnisse
- Laufzeitdialog für Android-Signierungs-Passwörter ohne persistente Klartextspeicherung

### Geändert
- `MainWindow` ergänzt um Settings für `ISCC.exe` und `Keystore-Alias` sowie einen klaren Release-Bereich mit Zielauswahl, Dry Run und echtem Release-Start
- `BuildCommandService` erzeugt jetzt reale `dotnet`- und Inno-Setup-Befehle für WPF-, APK- und AAB-Erzeugung
- Versionsschreibung nutzt jetzt reale Produktdateien im konfigurierten `KGV.neu`-Pfad und sichert den Originalzustand für Rollback
- Android-Builds verwenden jetzt signierte Release-Befehle mit Laufzeitpasswörtern statt Platzhalterlogik
- bestätigte lokale Standardpfade für `KGV.neu`, `KGV-WPF`, Release-Root, APK und AAB werden jetzt beim ersten Start bzw. bei leeren Settings automatisch vorbelegt
- der bestätigte lokale Pfad zu `ISCC.exe` wird jetzt ebenfalls automatisch vorbelegt, wenn die Datei real vorhanden ist
- erzeugte Artefakte werden jetzt zusätzlich in das lokale `KGV-WPF`-Repo bzw. in die konfigurierten APK-/AAB-Ausgabeordner kopiert

### Behoben
- fehlende echte Release-Verarbeitung hinter dem Button `Release starten`
- fehlende Rollback-Reaktion bei Fehlern nach bereits geschriebener Version
- fehlende robuste Fehlertexte bei ungültigen Tool-, Keystore- oder Artefaktpfaden

## [0.1.2-internal] - 2026-03-28
### Geändert
- interne Entwicklungsänderung: Versionslogik liest den konfigurierten KGV.neu-Pfad jetzt robust aus `KGV.Wpf`- und `KGV.Maui`-Projektdateien sowie Android-relevanten Stellen
- interne Entwicklungsänderung: Zielversion wird jetzt aus einem auswählbaren Versionssprung `Patch` / `Minor` / `Major` vorgeschlagen
- interne Entwicklungsänderung: `MainWindow` zeigt jetzt die erkannten Versionsstände, den Status der primären Logquelle und den Status des Veröffentlichungsordners an
- interne Entwicklungsänderung: Export-Prompt verwendet jetzt die erkannte Logquelle aus dem konfigurierten Quellrepo statt eines lokalen Platzhalterpfads

### Hinzugefügt
- Statusmodelle für Versionserkennung, Logquelle und Veröffentlichungsordner
- nicht-destruktive Vorbereitung eines Versionsordners mit den Unterordnern `WPF`, `Android/APK`, `Android/AAB` und `Dokumentation`
- Warnlogik für Versionsdrift zwischen WPF und Android

### Behoben
- interne Entwicklungsänderung: fehlende oder nicht lesbare Versionsquellen führen jetzt zu verständlichen Statusmeldungen statt zu impliziten Platzhalterwerten
- interne Entwicklungsänderung: vorhandene Veröffentlichungsordner werden jetzt sauber als bereits vorhanden behandelt statt still neu interpretiert zu werden

## [0.1.1-internal] - 2026-03-28
### Geändert
- interne Entwicklungsänderung: Einstellungsmodell um Store-Link ergänzt und Grund-UI in `MainWindow` in die Bereiche `Projektpfade`, `Android / Play Store` und `Veröffentlichung` gegliedert
- interne Entwicklungsänderung: Einstellungen werden jetzt beim Start automatisch geladen und lokal per JSON robust gespeichert

### Behoben
- interne Entwicklungsänderung: Laden beschädigter Settings-Dateien startet jetzt mit Defaultwerten statt die App scheitern zu lassen
- interne Entwicklungsänderung: grundlegende Pfad- und URL-Validierung mit verständlichen Rückmeldungen ergänzt

## [0.1.0] - 2026-03-28
### Hinzugefügt
- initiale Projektzusammenfassung aus den vier vorhandenen Threads
- Projektordner mit WPF-Scaffold für den KGV Release Manager
- erste Modelle für Einstellungen, Releaseplan und Zielauswahl
- erste Services für Versionierung, Settings, Log-Export und Releaseordner
- PowerShell-Skriptvorlagen für WPF- und Android-Releaseabläufe
- ausführlicher Fortschrittslog als Startdokument

### Geändert
- bisher nur dokumentarisch vorhandene Anforderungen in ein konkretes Projektgerüst überführt

### Behoben
- fehlender echter Projektordner im ersten Artefaktstand

### Entfernt
- nichts
