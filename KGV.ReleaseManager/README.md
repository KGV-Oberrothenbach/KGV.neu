# KGV Release Manager

Lokales Windows-Werkzeug für die Veröffentlichung der KGV-Software.

## Ziel
Der Release Manager steuert den Veröffentlichungsablauf für das Quellprojekt `KGV.neu`:
- Version automatisch hochzählen
- WPF-Setup erzeugen
- Android-Artefakte als signierte APK und signierte AAB erzeugen
- pro Version einen Veröffentlichungsordner anlegen
- bei Fehlern Versionsanpassungen in Projektdateien zurücksetzen
- Release-Texte aus dem Fortschrittslog ableiten, exportieren und wieder importieren
- nach erfolgreichem Echt-Release einen Release-Marker in das Fortschrittslog schreiben
- Änderungen im Quellrepo und im WPF-Zielrepo committen und pushen

## Enthalten
- WPF-Projektgerüst
- erste Modell- und Serviceklassen
- PowerShell-Skriptvorlagen
- Dokumentation, Changelog und ausführlicher Fortschrittslog

## Hinweis
Das Projekt bleibt bewusst schlank, aber der reale WPF-Release-Flow ist jetzt weiter verdrahtet:
- Versionen werden direkt aus den Projektdateien neu eingelesen
- Release-Texte nutzen den Delta-Bereich seit dem letzten `[RELEASE_MARKER]`
- nach erfolgreichem Echt-Release werden Marker sowie Commit/Push im Quellrepo und im WPF-Zielrepo ausgeführt

Externe Toolpfade wie `git`, `dotnet`, Inno Setup oder Android-Signing bleiben trotzdem weiter von der lokalen Umgebung abhängig und sollten im Praxisbetrieb gezielt validiert werden.
