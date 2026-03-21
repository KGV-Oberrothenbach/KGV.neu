# KGV – aktuelle Architekturübersicht

## Zweck dieses Dokuments

Dieses Dokument beschreibt den realen aktuellen Repository-Stand. Es ist keine historische Soll-Architektur und keine Ein-Projekt-WPF-Beschreibung mehr.

## Aktive Projektstruktur

- `KGV.Core`
  - gemeinsame Modelle, DTOs, Interfaces und Sicherheits-/Rechtegrundlagen
- `KGV.Infrastructure`
  - konkrete technische Anbindung, insbesondere Auth- und Supabase-Zugriffe
- `KGV.Wpf`
  - aktive Windows-Desktop-Anwendung auf WPF
- `KGV.Maui`
  - aktive mobile Anwendung auf .NET MAUI

## Nicht-produktive Referenzbereiche

- `_Archiv/_Recovery`
  - PDB-Dokumentlisten, Recovery-Metadaten, Missing-Listen und weitere Wiederaufbauhilfen
- `_Archiv/_RecoveredArtifacts`
  - wiedergefundene WPF-/Android-Artefakte
- `_Archiv/KGV.Tests`
  - auf Wunsch archiviertes Testprojekt; nicht Teil der aktiven Root-Basis, Lösung oder CI

Diese Bereiche sind Archiv-/Referenzmaterial und nicht Teil der produktiven Laufzeitbasis.

## Architekturrichtung

- Mehrprojektstruktur statt Monolith
- Trennung zwischen fachlichen Verträgen (`KGV.Core`) und technischer Infrastruktur (`KGV.Infrastructure`)
- UI-spezifische Umsetzung getrennt für Desktop (`KGV.Wpf`) und mobil (`KGV.Maui`)
- WPF und MAUI werden parallel gedacht; mobile Wege sind nicht nur Nebenprodukt
- Recovery-Material wird als Quelle für Rekonstruktion genutzt, aber nicht als aktive Produktstruktur dargestellt

## Technologiestand

- .NET 8 für zentrale Bibliotheken und WPF
- .NET 9 / .NET MAUI für die mobile App
- CommunityToolkit.Mvvm in der bestehenden MVVM-Basis
- Microsoft.Extensions.DependencyInjection / Configuration
- Supabase .NET SDK

## Grober Datenfluss

`UI (WPF/MAUI) -> ViewModel -> Interface aus KGV.Core -> Implementierung in KGV.Infrastructure -> Supabase`

## Rolle der UI-Projekte

### `KGV.Wpf`
- derzeit die belastbarere Desktop-Arbeitsoberfläche
- enthält weiterhin den größten Teil der rekonstruierbaren Verwaltungsoberfläche

### `KGV.Maui`
- aktive mobile Arbeitsoberfläche
- wird bewusst parallel mitgeführt und lokal mitgebaut
- enthält weiterhin rekonstruierte bzw. schrittweise angeglichene Pfade; nicht jede Funktion ist bereits auf dem gleichen Reifegrad wie in WPF

## Tests

- `KGV.Tests` wurde auf Wunsch in `_Archiv/KGV.Tests` verschoben.
- Die aktive Root-Basis und der aktuelle CI-Lauf bauen deshalb nur noch `KGV.Core`, `KGV.Infrastructure`, `KGV.Wpf` und `KGV.Maui`.
- Fehlende oder künftig wieder aktivierte Tests sollen weiterhin offen benannt werden.

## Offene Unsicherheiten

- Nicht alle ursprünglich vorhandenen fachlichen Bereiche sind bereits vollständig rekonstruiert.
- Teile der heutigen Basis stammen aus kontrolliertem Wiederaufbau und nicht aus einem vollständig erhaltenen Original-Repository.
- Recovery-Listen und Artefakte bleiben deshalb relevant, sind aber bewusst vom aktiven Code getrennt.

## Praktischer Einstieg

- `README.md` als zentraler Einstieg
- `Documentation/DEVELOPMENT.md` für lokalen Build und Entwicklungsablauf
- `DEV_LOG.md` für den tatsächlichen Arbeitsfortschritt
