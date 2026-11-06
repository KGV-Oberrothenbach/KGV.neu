# G7.2 Endabnahme-Checkliste

Automatisch geprüft wird ausschließlich DemoDB `ymxhszxfutjnljiuciyj`.
Physische RFID-/NFC-Tags, eine angemeldete Browser-Sitzung und ein Android-Gerät
stehen dieser technischen Abnahme nicht zur Verfügung; diese Fälle sind bewusst
als manuell ausstehend markiert.

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
