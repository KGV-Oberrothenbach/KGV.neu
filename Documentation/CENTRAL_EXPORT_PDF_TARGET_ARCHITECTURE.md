# Sollarchitektur: zentraler Tabellen-PDF-Export

## Geltungsbereich

Die zentrale Funktion rendert die Exporte `mitgliederliste` und
`arbeitsstunden_uebersicht` aus den jeweiligen JSON-RPC-Quellen. Sie übernimmt
keine Datenaufbereitung aus der Datenbank und verändert keine Daten.

## Datenvertrag

Jeder Export besitzt einen expliziten Vertrag mit:

- erwarteter RPC-Quelle;
- zulässigen technischen Spalten;
- fachlich erforderlichen Spalten;
- optionalen Spalten;
- fester Standardanzeigereihenfolge;
- erlaubten Request-Parametern.

Die angeforderten sichtbaren Spalten müssen im Vertrag liegen. Die RPC-Antwort
wird auf diese Spalten projiziert. Zusätzliche technische Felder in der Antwort
sind bei `arbeitsstunden_uebersicht` zulässig und werden ignoriert; sie sind
kein Quellenfehler. Eine falsche RPC-Quelle oder fehlende erforderliche Felder
bleibt ein harter Fehler.

## Textgrenze

Vor jeder Breitenmessung und vor jedem `pdf-lib`-Textaufruf wird Text zentral
normalisiert. Druckbare Zeichen – einschließlich deutscher Umlaute und ß –
bleiben unverändert. CR/LF/CRLF werden in mehrzeiligen Zellen zu logischen
Zeilen; in einzeiligen Texten zu Leerzeichen. Tab, NUL und sonstige
Steuerzeichen werden zu Leerzeichen. Kein Rohwert wird an `pdf-lib` gereicht.

## Renderer

Der Renderer arbeitet mit einer normalisierten Tabellenmatrix. Für jede Zelle
werden umbrochene Zeilen und daraus die dynamische Zeilenhöhe bestimmt. Eine
Seite besteht aus Kopfbereich, wiederholtem Tabellenkopf, Datenbereich und
Footer mit Seitenzahl. Vor dem Zeichnen einer Datenzeile wird geprüft, ob sie
noch in den Datenbereich passt; andernfalls wird eine neue Seite mit erneutem
Tabellenkopf begonnen. Mehrzeilige Blöcke wie Adresse und Kontakt bleiben als
mehrere Textzeilen erhalten.

## Fehlerdiagnose

Ein Rendererfehler enthält intern Export-Schlüssel, technische Spalte,
Datensatzindex, eine nichtpersonenbezogene ID-Referenz, Exception-Typ und
Exception-Text. Vollständige Personen- oder Kontaktinhalte werden nicht
protokolliert.

## Testbarkeit

Normalisierung, Vertragsprüfung und Seitenlayout sind getrennt testbar. Die
Integrationstests bauen vollständige PDFs für mehrseitige Mitglieder- und
Arbeitsstunden-Tabellen und prüfen eine erfolgreiche PDF-Signatur sowie die
Seitenanzahl.

## Vergleich mit der bisherigen Function

Die bisherige Function enthielt bereits die Katalog-/Prüfsummenprüfung,
Autorisierung und den Template-Hintergrund. Diese Teile bleiben erhalten. Die
Tabellenzeichnung war jedoch als kompakte Schleife in der Function eingebettet;
die Arbeitsstunden-Validierung behandelte jedes zusätzliche RPC-Feld als Fehler.

Der neue Stand trennt den Datenvertrag in `export-data-contract.ts`, die
Textgrenze in `pdf-text-normalization.mjs` und das umbruch-/seitenfähige Layout
in `pdf-table-layout.mjs`. Der Renderer übernimmt diese Bausteine, zeichnet
weiterhin Tabellenkopf auf jeder Folgeseite und hat zusätzlich einen echten
Seiten-Footer. Die aktive `export-allgemein/v3`-Vorlage, Katalogzugriff und
Prüfsummenlogik wurden nicht ersetzt.

Der WinAnsi-Fehler `0x000a` entstand, weil mehrzeilige Blockwerte bis zur
WinAnsi-Schriftfunktion gelangen konnten. Die zentrale Textgrenze normalisiert
alle Steuerzeichen vor Breitenmessung und Textausgabe. Der Spalten-Mismatch
entstand, weil die alte Arbeitsstundenprüfung unbekannte Antwortfelder als
Quellenfehler wertete; nun werden nur ausgewählte, erlaubte Spalten gerendert,
während Zusatzeigenschaften ignoriert werden.
