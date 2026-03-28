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

## Enthalten
- WPF-Projektgerüst
- erste Modell- und Serviceklassen
- PowerShell-Skriptvorlagen
- Dokumentation, Changelog und ausführlicher Fortschrittslog

## Hinweis
Das ist ein sauberer Projektstart / Scaffold. Externe Tools wie `git`, `dotnet`, `msbuild`, Inno Setup oder Android-Signing sind bewusst noch nicht vollständig verdrahtet, sondern als vorbereitete Stellen mit TODO-Markierungen angelegt.
