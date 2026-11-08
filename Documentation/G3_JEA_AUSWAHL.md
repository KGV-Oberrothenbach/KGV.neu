# G3 – vereinheitlichte JEA-Zählerauswahl

Die Web-JEA verwendet jetzt Parzelle → aktiver Zähler; das separate Medium-Feld entfällt nur dort. Im Hauptzählermodus wird die Parzellenauswahl zurückgesetzt und ausgeblendet. Die Auswahl verwendet den G2-Vertrag und zeigt normale Zähler als `Strom – Nummer` bzw. `Wasser – Nummer`, Hauptzähler als `Hauptzähler Strom Nord` usw.

MAUI verwendet im NFC-Ersatzweg ebenfalls Parzelle → aktiver Zähler und bietet den Hauptzähler-Schalter. Die Auswahl ruft `GetSelectableActiveMetersAsync` auf. Ein unbekannter JEA-RFID-Tag wird gegen `hauptzaehler_messstelle` geprüft und kann den aktiven Hauptzähler der zugehörigen Kombination wählen.

Die Web-Speicherprüfung lässt aktive Hauptzähler ausschließlich für direkte JEA-Erfassung zu und überspringt dabei bewusst Parzellen- und Belegungsprüfung. Normale Zähler bleiben unverändert validiert.

Offen für G4: eine vollständige, gemeinsame RFID-Auflösungsrepräsentation ohne technische Pseudo-Parzelle in MAUI sowie die produktive UI-Validierung auf Geräten.
