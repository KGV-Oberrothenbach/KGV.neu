# KGV – aktive Entwicklungsbasis

Dieses Repository ist die aktuelle Arbeitsbasis für den Wiederaufbau und die Weiterentwicklung der KGV-Software.

## Schnellüberblick

### Aktive Projekte
- `KGV.Core` – gemeinsame Modelle, Interfaces, Sicherheits-/Rechtebasis
- `KGV.Infrastructure` – Supabase-, Auth- und Infrastruktur-Anbindung
- `KGV.Wpf` – Windows-Desktop-App auf WPF
- `KGV.Maui` – mobile App auf .NET MAUI

### Archiv- und Referenzbereiche
- `_Archiv` – gebündelter Archivbereich für nicht mehr aktive Root-Bereiche
  - `_Archiv/_Recovery` – rekonstruierte Referenzlisten, PDB-Spuren und Recovery-Metadaten; nicht produktiv
  - `_Archiv/_RecoveredArtifacts` – wiedergefundene Build-/App-Artefakte als Referenz; nicht produktiv
  - `_Archiv/KGV.Tests` – ausdrücklich archiviertes Testprojekt; derzeit nicht Teil der aktiven Lösung/CI
- `README_RETTUNG.md` – Herkunft und Recovery-Kontext des Bestands; nicht der primäre Einstieg für die Entwicklung

## Empfohlener Einstieg
1. `KGV.slnx` in Visual Studio öffnen.
2. Zuerst die Root-Dokumente lesen:
   - `README.md`
   - `ARCHITECTURE.md`
   - `Documentation/DEVELOPMENT.md`
   - `DEV_LOG.md`
3. Danach lokal mindestens diese Builds prüfen:
   - `dotnet build KGV.Wpf/KGV.Wpf.csproj -c Debug`
   - `dotnet build KGV.Maui/KGV.Maui.csproj -c Debug`

## Aktueller Stand
- WPF und MAUI sind beide Teil der aktiven Arbeitsbasis.
- `KGV.Core` und `KGV.Infrastructure` werden von beiden UI-Projekten mitgetragen.
- Archiv- und Recovery-Material bleibt bewusst im Repository, ist aber nur Referenz für Wiederaufbau und Abgleich.
- Nicht alle ursprünglich vorhandenen Funktionen sind bereits belastbar rekonstruiert; offene Stellen werden im `DEV_LOG.md` dokumentiert.

## Wichtige Hinweise
- Im Auth-/Supabase-Kontext wird mit `sb_publishable` bzw. Publishable Key gearbeitet; alte `anon key`-Annahmen sollen nicht wieder eingeführt werden.
- Demo-/Play-Store-Testdaten dürfen fachliche Auswertungen nicht verfälschen.
- Mobile und Desktop sollen fachlich zusammen gedacht werden; WPF und MAUI gelten beide als aktive Zielanwendungen.

## Dokumente
- `ARCHITECTURE.md` – reale Projektarchitektur und Rollen der Projekte
- `DECISIONS.md` – aktuelle technische Leitentscheidungen
- `Documentation/DEVELOPMENT.md` – pragmatischer Build-/Entwicklungsleitfaden
- `DEV_LOG.md` – laufendes Entwicklungslog
- `Documentation/RELEASE_NOTES_HISTORY.md` – historisierte Release-Texte
- `_Archiv/README.md` – Einordnung der archivierten Root-Bereiche
