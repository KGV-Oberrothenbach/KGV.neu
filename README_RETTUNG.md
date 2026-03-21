# README_RETTUNG – Herkunft und Recovery-Kontext

Diese Datei beschreibt die Herkunft des heutigen Repository-Bestands.

## Wichtig

- Für die tägliche Entwicklung ist `README.md` der zentrale Einstieg.
- Diese Datei ist kein primärer Entwicklungsleitfaden, sondern dokumentiert, warum im Repository zusätzlich Archiv- und Referenzbereiche existieren.

## Herkunft des Bestands

Der heutige Arbeitsstand wurde ursprünglich aus mehreren Quellen zusammengeführt:

1. einer älteren flachen `KGV.zip` als Quellbasis,
2. neueren WPF-/Core-/Infrastructure-Artefakten (`.dll`, `.pdb`, `.deps.json`, `.runtimeconfig.json`),
3. Android-Artefakten (`.aab`, `.apk`) als Referenz für die spätere MAUI-Rekonstruktion.

## Was heute aktiv ist

- `KGV.Core`
- `KGV.Infrastructure`
- `KGV.Wpf`
- `KGV.Maui`

Diese Projekte bilden die aktuelle aktive Entwicklungsbasis.

## Was Referenz bleibt

- `_Archiv/_Recovery` – Listen, Metadaten und Wiederaufbauhinweise
- `_Archiv/_RecoveredArtifacts` – wiedergefundene Artefakte
- `_Archiv/KGV.Tests` – auf Wunsch archiviertes Testprojekt

Diese Bereiche bleiben bewusst erhalten, sind aber nicht die produktive Quellbasis im Root.

## Weiterhin relevante Recovery-Fakten

- Die PDBs verweisen per SourceLink auf das frühere Repo `KGV-Oberrothenbach/KGV-WPF`.
- Der rekonstruierte Stand war nie ein perfekter letzter Live-Stand, sondern eine kombinierte Rettungsbasis.
- `KGV.Core`, `KGV.Infrastructure` und `KGV.Wpf` waren belastbarer rekonstruierbar als `KGV.Maui`.

## Aktuelle Nutzung dieser Datei

Diese Datei dient nur noch dazu,

- den Recovery-Ursprung nachvollziehbar zu halten,
- die Archivbereiche unter `_Archiv` im Repo einzuordnen,
- und Missverständnisse zwischen aktiver Basis und Recovery-Referenzen zu vermeiden.
