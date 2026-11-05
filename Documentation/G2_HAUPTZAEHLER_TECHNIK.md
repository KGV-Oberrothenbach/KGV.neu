# G2 – technische Vorbereitung Hauptzähler

G2 erweitert ausschließlich Datenverträge und Zugriffsschichten. Es verändert weder eine JEA-Oberfläche noch Export-, Verbrauchs- oder Umlagelogik.

## Modelle

`ZaehlerRecord`, die Strom-/Wasser-Varianten sowie deren Insert-Records führen `ParzelleId` nullable und transportieren zusätzlich `IstHauptzaehler` sowie `Anlagenbereich`. `HauptzaehlerMessstelleRecord` bildet `hauptzaehler_messstelle` mit Medium, Anlagenbereich und RFID ab.

## Wählbare aktive Zähler

Der neue Vertrag `SelectableActiveMeter` / `SelectableActiveMeterQuery` trennt zwei Modi:

- Normalmodus: aktive, nicht ausgebaute Nicht-Hauptzähler einer konkreten Parzelle.
- Hauptzählermodus: aktive, nicht ausgebaute Hauptzähler ohne Parzelle.

MAUI stellt dies über `ISupabaseService.GetSelectableActiveMetersAsync` bereit. Web stellt den äquivalenten, noch nicht angebundenen Repository-Zugriff `listSelectableActiveMeters` bereit. Beide transportieren ID, Medium, Zählernummer, optionale Parzelle, Hauptzählerflag, Anlagenbereich und eine abgeleitete Anzeigeinformation.

## Hauptmessstelle und RFID

Web und MAUI können `hauptzaehler_messstelle` lesen und über deren RFID-UID auflösen. MAUI kann anschließend mittels Medium plus Anlagenbereich den aktuell aktiven physischen Hauptzähler abfragen. Die vorhandene Parzellen-RFID-Logik wird bewusst nicht verändert.

## Bewusst offen für G3

- Einbindung des Auswahlvertrags in Web- und MAUI-JEA.
- Hauptzähler-UI, Checkbox und Wegfall des Medium-Pickers.
- RFID-Scan-Workflow für Hauptmessstellen.
- Zählerwechsel, Jahresverbrauch, Export und Umlage für Hauptzähler.
