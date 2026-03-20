# KGV – rekonstruierter Projektordner (Best Effort)

Dieser Ordner wurde aus drei Quellen aufgebaut:

1. der alten/flachen `KGV.zip` als Quellbasis,
2. den neueren WPF/Core/Infrastructure-Artefakten (`.dll`, `.pdb`, `.deps.json`, `.runtimeconfig.json`),
3. den Android-Artefakten (`.aab`, `.apk`) als spätere Referenz für MAUI.

## Was hier drin ist

- `KGV.Core`, `KGV.Infrastructure`, `KGV.Wpf`, `KGV.Maui`, `KGV.Tests`: bestmöglich sortierte Projektordner
- `_RecoveredArtifacts/Wpf`: neuere Build-Artefakte inkl. PDBs
- `_RecoveredArtifacts/Android`: AAB/APK
- `_Recovery/PdbDocumentLists`: Dateinamen, die in den neueren PDBs gefunden wurden
- `_Recovery/MissingFromCurrentPdb`: Dateien, die laut PDB im neueren Stand existierten, aber in der alten ZIP nicht mehr enthalten waren
- `_Recovery/OriginalMetadata`: Zusatzinfos, originale flache Projektdateien und Android-Inhaltsübersicht

## Wichtiger Stand

- Die PDBs enthalten einen SourceLink auf:
  - Repo: `KGV-Oberrothenbach/KGV-WPF`
  - Commit: `5f195fbe4607bc18a20c0391f7a0a7a8b29aeecd`
- Die WPF-Artefakte gehören zum Stand `0.2.7` (aus `KGV.Wpf.deps.json` / Build-Artefakten).
- Die alte ZIP ist älter als dieser Buildstand. Deshalb ist dieser Ordner **kein perfekter letzter Live-Stand**, sondern eine **kombinierte Rettungsbasis**.

## Ehrliche Einschätzung

- `KGV.Core`, `KGV.Infrastructure` und `KGV.Wpf` sind hier deutlich besser rekonstruierbar als `KGV.Maui`.
- Für MAUI fehlen direkte Quelltexte des neueren Standes; AAB/APK sind deshalb separat abgelegt.
- Einige Dateien wurden nur als **Hinweislisten** rekonstruiert, nicht als echter Quelltext.
- Die Projektdateien (`.csproj`) wurden für diese rekonstruierte Ordnerstruktur **vereinfacht/normalisiert**. Sie sind eine Arbeitsbasis, nicht garantiert 1:1 der Originalzustand.

## Empfehlung

1. Diesen Ordner lokal entpacken.
2. Zuerst `KGV.slnx` in Visual Studio öffnen.
3. Danach die Dateien aus `_Recovery/MissingFromCurrentPdb` mit den Artefakten unter `_RecoveredArtifacts/Wpf` gezielt nachziehen/dekompilieren.
4. Für MAUI bei Bedarf separat aus AAB/APK dekompilieren.

## Kurzbilanz

- Gemappte Dateien aus der alten ZIP: 152
- Fehlende Dateien laut neuerem PDB-Stand:
  - KGV.Core: 32
  - KGV.Infrastructure: 5
  - KGV.Wpf: 64
