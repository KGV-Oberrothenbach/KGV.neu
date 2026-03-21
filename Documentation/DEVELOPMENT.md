# Entwicklung und lokaler Build

## Empfohlener Start
- Repository über `KGV.slnx` in Visual Studio öffnen.
- Für CLI-Arbeit vom Repository-Root aus arbeiten.
- Die aktive Basis besteht aus `KGV.Core`, `KGV.Infrastructure`, `KGV.Wpf`, `KGV.Maui` und `KGV.Tests`.

## Rolle der Projekte
- `KGV.Core` – gemeinsame Verträge, Modelle und Sicherheitsgrundlagen
- `KGV.Infrastructure` – konkrete Infrastruktur- und Supabase-Anbindung
- `KGV.Wpf` – derzeit zentrale Desktop-Arbeitsoberfläche
- `KGV.Maui` – mobile Arbeitsoberfläche; wird parallel mitgedacht und lokal mitgeprüft
- `KGV.Tests` – Testprojekt; aktuell eher schmal, aber Teil der aktiven Lösung

## Lokale Voraussetzungen
- .NET SDK gemäß `global.json` (`9.0.310`)
- Windows-Entwicklungsumgebung für WPF
- .NET MAUI-Workloads für lokale MAUI-Builds
- Android-SDK/Plattformen passend zur lokal installierten MAUI-Umgebung
- `appsettings.json` im Root mit Supabase-Konfiguration auf Basis des Publishable Keys

## Lokal sinnvoll zu prüfende Builds
```powershell
 dotnet build KGV.Wpf/KGV.Wpf.csproj -c Debug
 dotnet build KGV.Maui/KGV.Maui.csproj -c Debug
 dotnet build KGV.Tests/KGV.Tests.csproj -c Debug
```

## Tests
- `KGV.Tests` ist als aktives Testprojekt Teil des Repositories.
- Der aktuelle Wiederaufbau-Stand kann bedeuten, dass lokal noch keine oder nur wenige Tests entdeckt werden.
- Fehlende Testabdeckung soll offen benannt und nicht durch Annahmen kaschiert werden.

## Recovery-Material
- `_Recovery` und `_RecoveredArtifacts` sind Referenzbereiche.
- Diese Verzeichnisse dürfen zur Rekonstruktion genutzt werden, sind aber nicht Teil der produktiven Laufzeitbasis.
- Änderungen an aktiver Doku oder aktivem Code sollen sich nicht auf Recovery-Hinweise als primären Einstieg stützen.

## Arbeitsprinzip für neue Blöcke
- Kleine, buildfähige Änderungen
- WPF und MAUI parallel mitdenken
- Root-Dokumentation aktuell halten
- Offene Unsicherheiten direkt im `DEV_LOG.md` oder in der betroffenen Doku benennen
