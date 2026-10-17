# Entwicklung und lokaler Build

## Empfohlener Start
- Repository über `KGV.slnx` in Visual Studio öffnen.
- Für CLI-Arbeit vom Repository-Root aus arbeiten.
- Die aktive Basis besteht aus `KGV.Core`, `KGV.Infrastructure`, `KGV.Maui` und `KGV.Web`.

## Rolle der Projekte
- `KGV.Core` – gemeinsame Verträge, Modelle und Sicherheitsgrundlagen
- `KGV.Infrastructure` – konkrete Infrastruktur- und Supabase-Anbindung
- `KGV.Maui` – mobile Arbeitsoberfläche; wird lokal mitgeprüft
- `KGV.Web` – Browser-Arbeitsoberfläche

## Lokale Voraussetzungen
- .NET SDK gemäß `global.json` (`9.0.310`)
- .NET MAUI-Workloads für lokale MAUI-Builds
- Android-SDK/Plattformen passend zur lokal installierten MAUI-Umgebung
- `appsettings.json` im Root mit Supabase-Konfiguration auf Basis des Publishable Keys

## Lokal sinnvoll zu prüfende Builds
```powershell
 dotnet build KGV.Maui/KGV.Maui.csproj -c Debug
```

## Tests
- `KGV.Tests` wurde auf Wunsch nach `_Archiv/KGV.Tests` verschoben und ist nicht mehr Teil der aktiven Lösung oder des aktiven CI-Laufs.
- Fehlende Testabdeckung der aktiven Basis soll weiterhin offen benannt und nicht durch Annahmen kaschiert werden.

## Recovery-Material
- `_Archiv/_Recovery` und `_Archiv/_RecoveredArtifacts` sind Referenzbereiche.
- Diese Verzeichnisse dürfen zur Rekonstruktion genutzt werden, sind aber nicht Teil der produktiven Laufzeitbasis.
- Änderungen an aktiver Doku oder aktivem Code sollen sich nicht auf Recovery-Hinweise als primären Einstieg stützen.

## Arbeitsprinzip für neue Blöcke
- Kleine, buildfähige Änderungen
- MAUI und Web fachlich parallel mitdenken
- Root-Dokumentation aktuell halten
- Offene Unsicherheiten direkt im `DEV_LOG.md` oder in der betroffenen Doku benennen
