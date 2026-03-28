# Projektzusammenfassung KGV Release Manager

## Ausgangslage aus den vier Threads
Aus den bisherigen Threads ergibt sich ein eigenständiges lokales Windows-Tool, das Releases für das Projekt `KGV.neu` vorbereitet und ausführt. Der Fokus liegt auf einer reproduzierbaren, halbautomatisierten Veröffentlichung für WPF und Android.

## Fachliche Kernelemente
- Quellprojekt: `https://github.com/KGV-Oberrothenbach/KGV.neu`
- WPF-Zielrepo für veröffentlichte Setup-Dateien: `https://github.com/KGV-Oberrothenbach/KGV-WPF`
- automatische Versionserhöhung vor dem Release
- Erzeugung einer WPF-`Setup.exe`
- Erzeugung einer signierten Android-`APK` für interne Tests
- Erzeugung einer signierten Android-`AAB` für den manuellen Play-Store-Upload
- pro Version eigener Veröffentlichungsordner mit allen Artefakten
- bei Fehlschlag Versionsnummern in den Projektdateien zurücksetzen
- Einstellungen für lokale Pfade und Zielorte im Einstellungsmenü
- Logquelle ist `KGV_Fortschritt_ausfuehrlich.md`
- Änderungen seit dem letzten Release sollen exportierbar sein
- Export soll zusätzlich einen Prompt für eine ChatGPT-Versionszusammenfassung enthalten
- erzeugte Release-Zusammenfassung soll wieder importierbar sein
- Texte sollen anschließend für WPF-Veröffentlichung und Play Store nutzbar sein

## Geplante Projektstruktur
- WPF-Desktopanwendung als lokale Release-Oberfläche
- Einstellungsmodell für Pfade, Signing und Zielorte
- Versionsservice für Lesen, Erhöhen und Rollback
- Build-/Git-Service für Skript- und Tool-Aufrufe
- Log-/Release-Notes-Service für Export und Reimport
- Veröffentlichungsordner pro Version
- Dokumentationsbereich mit Changelog und ausführlichem Log

## Empfohlene nächste Ausbaustufen
1. Settings-Seite mit persistentem Speichern
2. echte Versionsanalyse im Quellrepo
3. WPF-Setup-Build per Inno Setup / MSBuild
4. Android-Build mit signierter APK und AAB
5. Rollback-Logik bei Fehlern
6. Export/Import von Release-Notizen
7. Git-Abschluss und optional GitHub-Release
