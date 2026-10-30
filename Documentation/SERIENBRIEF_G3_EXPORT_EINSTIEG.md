# Serienbrief G3 – Export-Einstieg

Erster aktivierter Referenzexport ist `zaehler_eichfaelligkeit`. Seine RPC liefert
`mitglied_id`, `parzelle_id` und den technischen Empfängerstatus. Bei einem
aktiven Pachtverhältnis mit Beteiligung ist ausschließlich dessen aktiver
`hauptpaechter` Empfänger. Nur ohne diesen neuen Pachtkontext wird exakt eine
aktuelle `parzellen_belegung` verwendet. Fehlende oder mehrere Legacy-Belegungen
bleiben als `nicht_ermittelbar` beziehungsweise `mehrdeutig` sichtbar und
erhalten keine `mitglied_id`.

Freigegeben sind Vorname, Nachname, Parzelle, Zählerart, Zählernummer,
Eichdatum, Eichfälligkeit und Fälligkeitsstatus; technische IDs und Status sind
keine Vorlagenfelder. `ExportCenter` nutzt weiterhin denselben vollständigen
`executeExport`-Weg und bildet daraus den typisierten Serienbriefkontext. Die
500-Zeilen-Grenze gilt nur für die normale Tabellenanzeige.

G3 enthält noch keinen Editor, keine Auswahlpersistenz, freien Felder,
Briefanrede, Vorschau, PDF-, Archiv- oder Versandlogik. Diese folgen ab G4.
