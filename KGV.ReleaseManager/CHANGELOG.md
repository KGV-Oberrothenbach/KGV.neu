# Changelog

Alle nennenswerten Änderungen am KGV Release Manager werden hier dokumentiert.

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
