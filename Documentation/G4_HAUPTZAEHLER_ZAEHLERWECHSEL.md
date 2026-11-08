# G4 – Hauptzähler-Zählerwechsel und MAUI-Hauptzählerkontext

## Ergebnis

Hauptzähler werden mit einem eigenen, atomaren Datenbank-RPC gewechselt. Der
bisherige parzellenbezogene RPC `execute_meter_change` und dessen Prüfungen
bleiben unverändert. Der Wechsel erzeugt in einer Transaktion die
Ausbauablesung, beendet den alten Hauptzähler, legt den neuen Hauptzähler ohne
Parzelle an und erzeugt dessen Einbauablesung.

Die neue Migration `20261103090000_hauptzaehler_meter_change.sql` ergänzt
zusätzlich den Partial-Unique-Index
`ux_zaehler_aktiver_hauptzaehler_messstelle`. Er sichert, dass es je
`medium` und `anlagenbereich` höchstens einen aktiven Hauptzähler gibt.

## Web

Die bestehende Wechselmaske enthält eine Option `Hauptzähler`. In diesem Modus
werden nur zum Ausbaudatum aktive Hauptzähler angeboten. Beim Ausführen sendet
die Maske `istHauptzaehler` und `anlagenbereich` an den separaten RPC; eine
Parzellen-ID wird nicht übertragen.

Ein RFID-Scan versucht unverändert zunächst die bestehende
Parzellen-RFID-Auflösung. Falls diese keinen aktiven Zähler liefert, wird die
bereits vorhandene Hauptzähler-Messstelle nur gelesen und der aktive
Hauptzähler zu Medium und Bereich gesucht. Es wird dabei kein Tag angelegt,
geändert oder einer Parzelle zugeordnet.

## MAUI

Der RFID-Workflow behandelt einen unbekannten Parzellen-Tag zuerst als
potenzielle Hauptzähler-Messstelle. Nur wenn Messstelle und aktiver
Hauptzähler vorhanden sind, wird ein Wechselkontext mit
`ParzelleId = null`, `IstHauptzaehler = true` und `Anlagenbereich` erzeugt.
Andernfalls bleibt der bisherige Flow `RFID hinzufügen` zuständig.

Die Ausbau-Seite übergibt diesen Kontext an
`ExecuteMeterChangeAsync`. Die Infrastruktur wählt anhand von
`IstHauptzaehler` den Hauptzähler-RPC. Die Seite zeigt statt einer erfundenen
Parzelle die Hauptzähler-Messstelle an. Der alte Einbauflow bleibt bewusst
parzellenbezogen; ein Hauptzähler ohne aktiven Vorgänger wird dort mit einer
klaren Meldung abgewiesen, statt eine Ersatz-Parzelle anzulegen.

## Historie, Eichung und Bauart

Der neue RPC nutzt dieselben Ausbau-/Einbauablesungsarten, Datums- und
Endstandprüfungen wie der bestehende Wechsel. `eichdatum` wird beim neuen
Zähler gesetzt. `bauart` folgt dem bestehenden Wechselvertrag und dem
Spaltenstandard `mechanisch`; der vorhandene Datenbank-Trigger berechnet die
Eichfälligkeit. Es wurde keine abweichende Bauartlogik für Hauptzähler
eingeführt.

## Geänderte Dateien

- `supabase/migrations/20261103090000_hauptzaehler_meter_change.sql`
- `KGV.Core/Models/MeterChangeExecutionRequest.cs`
- `KGV.Core/Models/RfidScanContextRecord.cs`
- `KGV.Infrastructure/Services/SupabaseService.cs`
- `KGV.Web/repositories/meters/meter-change-repository.ts`
- `KGV.Web/app/page.tsx`
- `KGV.Maui/Pages/ZaehlerwechselPage.cs`
- `KGV.Maui/Pages/ZaehlerwechselAusbauPage.cs`
- `KGV.Maui/Pages/ZaehlerwechselEinbauPage.cs`

## Verifikation

- `npm run build` in `KGV.Web`: erfolgreich.
- `dotnet build KGV.Core/KGV.Core.csproj --no-restore -v:q`: erfolgreich
  (bestehende ImageSharp-Sicherheitswarnungen).
- `dotnet build KGV.Infrastructure/KGV.Infrastructure.csproj --no-restore -v:q`:
  erfolgreich (bestehende ImageSharp-Sicherheitswarnungen).
- MAUI-Build: Die G4-Änderungen kompilieren. Es verbleiben zwei unabhängige,
  bereits vorhandene Fehler in `KGV.Maui/Pages/MeterChangeRequestPages.cs`
  (Zeilen 162 und 166: `Keyboard` ist nicht auflösbar), plus bestehende
  Paketwarnungen.

## Übergabe an G5

G5 kann auf der eindeutigen aktiven Hauptzähler-Messstelle und der lückenlosen
Ausbau-/Einbauhistorie aufsetzen. Noch nicht Teil von G4 sind
Jahresverbrauchsaggregation, JEA-Export, Differenzrechnung und Umlage.
Falls ein administrativer Ersteinbau für eine Messstelle ohne aktiven
Hauptzähler benötigt wird, braucht er einen expliziten, separaten Workflow;
der bestehende parzellengebundene Einbauflow ist dafür absichtlich nicht
umgedeutet worden.
