# Architektur

## Zweck dieses Dokuments

Dieses Dokument beschreibt den aktuellen Repository- und Architekturstand des KGV-Projekts.

## Aktive Projektstruktur

- `KGV.Core`
  - gemeinsame .NET-Modelle, Interfaces, Regeln und Sicherheitsgrundlagen
- `KGV.Infrastructure`
  - technische Infrastruktur, Authentifizierung und Supabase-Anbindung
- `KGV.Maui`
  - aktive mobile Anwendung auf .NET MAUI
- `KGV.Web`
  - aktive Browser-Anwendung
- `KGV.Tests`
  - automatisierte Tests für Core-, Infrastructure- und Workflowlogik
- `supabase`
  - Migrationen, Datenbank- und serverseitige Bestandteile

## Architekturprinzipien

- Fachlogik möglichst zentral und unabhängig von der Oberfläche halten.
- MAUI und Web fachlich konsistent entwickeln.
- Supabase ist das gemeinsame Backend.
- UI-spezifische Logik bleibt in der jeweiligen Anwendung.
- Bestehende gemeinsame Verträge und Serverregeln sollen nicht in einzelnen Clients dupliziert werden.
- Kleine, überprüfbare und buildfähige Änderungen bevorzugen.

## MAUI

Die mobile Anwendung nutzt `KGV.Core` und `KGV.Infrastructure` als gemeinsame .NET-Basis.

Aktives Ziel ist insbesondere Android auf .NET 10.

## Web

`KGV.Web` bildet die Browser-Oberfläche ab.

Die Web-Anwendung soll dieselben fachlichen Serververträge, Statusmodelle und Supabase-Regeln verwenden wie die mobile Anwendung, auch wenn die technische Clientstruktur unterschiedlich ist.

## Datenfluss

MAUI:

`KGV.Maui -> KGV.Core / KGV.Infrastructure -> Supabase`

Web:

`KGV.Web -> Web-Services / Supabase-API -> Supabase`

## Tests

`KGV.Tests` ist das aktive Testprojekt.

Tests sollen insbesondere gemeinsame Geschäftslogik, Repository-Verträge, Parser, Mapper und Workflowregeln absichern.

## Archiv

`_Archiv` enthält historische Recovery- und Referenzdaten.

Archivmaterial gehört nicht zur aktiven Produktstruktur und darf keine aktive Build- oder Projektabhängigkeit erzeugen.
