# G7.2 Endabnahme-Checkliste

Automatisch geprüft wird ausschließlich DemoDB `ymxhszxfutjnljiuciyj`.
Physische RFID-/NFC-Tags, eine angemeldete Browser-Sitzung und ein Android-Gerät
stehen dieser technischen Abnahme nicht zur Verfügung; diese Fälle sind bewusst
als manuell ausstehend markiert.

## G7.3-Demo-Testdaten (Jahr 2026)

Das Setup liegt in `supabase/tests/manual/20261107_g7_3_demo_setup.sql`, das
Cleanup in `supabase/tests/manual/20261107_g7_3_demo_cleanup.sql`. Alle Daten
sind mit `CX-G7.3-` markiert. Die RFID-Werte `CX-G7.3-NO-PHYSICAL-*` sind nur
Constraint-Platzhalter und **keine** erwarteten physischen Tags. Eigene Tags
erst während der Abnahme über die UI zuordnen.

| Bereich | Hauptzähler | Unterzähler | Erwartete Differenz | Testkennungen |
|---|---:|---:|---:|---|
| Strom Nord | 50 kWh | 30 kWh | +20 kWh | `CX-G7.3-H-SN`, `CX-G7.3-U-SN` |
| Strom Süd | 20 kWh | 30 kWh | -10 kWh, Prüfbedürftig | `CX-G7.3-H-SS`, `CX-G7.3-U-SS` |
| Wasser Nord | 20 m³ | 20 m³ | 0 m³ | `CX-G7.3-H-WN`, `CX-G7.3-U-WN` |
| Wasser Süd | fehlt | 10 m³ | Nicht berechenbar | `CX-G7.3-H-WS`, `CX-G7.3-U-WS` |

Browser-Reihenfolge: Demo anmelden → **Jahresablesung 2026** öffnen →
Hauptzähler über die manuelle Auswahl prüfen → Strom Nord für einen Wechsel
verwenden → Jahresauswertung öffnen → JAE-XLSX exportieren. Anschließend auf
Android dieselbe Anmeldung, manuelle Auswahl, später eigene NFC-/RFID-Tags,
JEA und historische Auswahl prüfen. Nach Abschluss ausschließlich das genannte
Cleanup-Skript gegen DemoDB ausführen.

| Nr. | Oberfläche | Ausgangssituation | Handlung | Erwartetes Ergebnis | Tatsächliches Ergebnis | Status |
|---|---|---|---|---|---|---|
| 01 | Web | Demo-Anmeldung, Parzellen-RFID Strom | RFID scannen | Parzelle, Strom und aktiver Zähler | Manuelle Prüfung ausstehend | nicht getestet |
| 02 | Web | Demo-Anmeldung, Hauptzähler-RFID Nord/Süd | RFID scannen | Messstelle, Medium, Bereich, aktiver Zähler | Manuelle Prüfung ausstehend | nicht getestet |
| 03 | Web | Unbekannte/leere RFID | RFID eingeben | Verständlicher unbekannt/leer-Status | Manuelle Prüfung ausstehend | nicht getestet |
| 04 | Web | Hauptzähler ohne aktiven Zähler | RFID scannen | Messstelle ohne künstliche Parzelle | Manuelle Prüfung ausstehend | nicht getestet |
| 05 | Web | JEA-Ansicht DemoDB | Parzellen- und Hauptzähler manuell wählen, JEA speichern | Nummer, Medium, Datum, Duplikat- und Plausibilitätsregeln korrekt | Manuelle Prüfung ausstehend | nicht getestet |
| 06 | Web | Aktiver Hauptzähler | Wechsel mit Ausbau/Einbau ausführen | Historie, RFID-Messstelle und zeitliche Auswahl korrekt | Manuelle Prüfung ausstehend | nicht getestet |
| 07 | Web | Jahresauswertung mit vier Kombinationen | Saison wechseln | Haupt/Unter/Differenz, Einheit, Fehlerstatus aktualisieren | Manuelle Prüfung ausstehend | nicht getestet |
| 08 | Web | Negative/Null/fehlende Differenz auf DemoDB | Auswertung öffnen | Prüfbedürftig, 0 bzw. Nicht berechenbar sichtbar | Automatischer RPC-/XLSX-Test erfolgreich; UI manuell offen | nicht getestet |
| 09 | MAUI | Android mit DemoDB und NFC/RFID | Parzellen- und Hauptzähler-Tag scannen | Gemeinsamer Resolver, Nord/Süd und aktiver Zähler korrekt | Manuelle Geräteprüfung ausstehend | nicht getestet |
| 10 | MAUI | Android mit DemoDB | JEA, manuelle Auswahl und Hauptzählerwechsel | Speicherung, Historie, Fehler-/Berechtigungsverhalten korrekt | Manuelle Geräteprüfung ausstehend | nicht getestet |
