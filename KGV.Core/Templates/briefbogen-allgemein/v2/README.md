# Allgemeiner Briefbogen v2

Diese Version übernimmt das freigegebene Briefkopf-Layout mit Wappen, Trennlinien
und den drei Fußzeilenüberschriften. Sie enthält bewusst keine festen Vereins-,
Empfänger- oder Beispieldaten.

Bei der Dokumenterzeugung werden ausschließlich die Felder der `field-spec.json`
aus dem für den Vorgang gespeicherten Vereinskonfigurationssnapshot und den
jeweiligen Empfängerdaten gefüllt:

- `absender_name`, `absender_anschrift`
- `empfaenger_block`, `datum`, `betreff`, `anrede`, `brieftext`, `abschluss`
- `fusszeile_verein`, `fusszeile_bank`, `fusszeile_vorstand`

Die v1 bleibt unverändert als bisheriges Freigabemuster erhalten. Eine
Katalogveröffentlichung, ein Storage-Upload oder eine Aktivierung sind nicht
Bestandteil dieser lokalen Vorlage.
