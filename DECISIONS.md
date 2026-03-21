# Architektur- und Repository-Entscheidungen

## 2026-02-18 – Neustart

Projekt bewusst neu aufgebaut, um inkonsistente Altstrukturen nicht ungeprüft weiterzuschleppen.

## MVVM-Basis

`CommunityToolkit.Mvvm` bleibt die Basis für die bestehende MVVM-Struktur.

Gründe:
- wenig Boilerplate
- klare ViewModel-Struktur
- bereits in der aktiven Codebasis verankert

## Navigation in WPF

WPF bleibt bei `ContentControl` plus ViewModel-Switching statt Frame-basierter Navigation.

Gründe:
- bestehende aktive Basis arbeitet bereits so
- MVVM-konformer als eine spätere Mischstruktur

## Mehrprojektstruktur statt Ein-Projekt-Darstellung

Die aktive Arbeitsbasis wird ausdrücklich als Mehrprojektstruktur geführt:

- `KGV.Core`
- `KGV.Infrastructure`
- `KGV.Wpf`
- `KGV.Maui`
- `KGV.Tests`

Grund:
- entspricht dem realen Stand des Repositories
- trennt fachliche Verträge, Infrastruktur, Desktop, mobil und Tests sauberer

## Recovery bleibt Referenz, nicht Produktbasis

`_Recovery` und `_RecoveredArtifacts` bleiben im Repository, werden aber dokumentarisch als Archiv-/Referenzbereiche behandelt.

Gründe:
- Wiederaufbau ist noch nicht in allen Bereichen abgeschlossen
- Recovery-Material ist weiterhin nützlich
- aktive Quellbasis und Referenzmaterial müssen klar getrennt sein

## Zentrale Einstiegspunkte

Der primäre Einstieg für Entwicklung liegt bei:

- `README.md`
- `ARCHITECTURE.md`
- `Documentation/DEVELOPMENT.md`
- `DEV_LOG.md`

Grund:
- Root und aktive Dokumentation sollen schneller verständlich sein als Recovery-Hinweise

## Supabase-/Auth-Kontext

Im aktiven Stand wird im Konfigurationskontext mit `sb_publishable` bzw. Publishable Key gearbeitet.

Grund:
- entspricht der aktuellen Umstellung im Projekt
- alte `anon key`-Annahmen sollen nicht wieder als Standard dokumentiert oder eingeführt werden

## SDK-Stand

Das Repository orientiert sich am in `global.json` fixierten SDK-Stand.

Aktuell:
- SDK `9.0.310`

Grund:
- passt zum aktuellen MAUI-/Workspace-Stand
- kann weiterhin auch die .NET-8-Projekte im Repository bauen