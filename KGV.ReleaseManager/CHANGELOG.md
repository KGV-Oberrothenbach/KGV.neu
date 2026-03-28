# Changelog

Alle nennenswerten Änderungen am KGV Release Manager werden hier dokumentiert.

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
