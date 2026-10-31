# Serienbrief – Abschluss

## Ziel

Die Serienbrieffunktion überführt das vollständige Ergebnis des explizit freigegebenen Exports `zaehler_eichfaelligkeit` in individualisierte Schreiben. Konfliktdatensätze bleiben sichtbar und werden nie als Empfänger geraten.

## Ablauf

`Export → Filter → Serienbrief → Empfängerauswahl → Vorlage → Freifelder → Vorschau → Renderjob → PDF → Archiv → Mail/Druck → Status`

Der Einstieg verwendet die vollständige Exporttreffermenge, nicht die sichtbare Exportvorschau. Nur Admin und Vorstand erreichen Export und Serienbriefe.

## Architektur

- `KGV.Web/services/export/serienbrief-export-service.ts`: validiert den Exportdatenvertrag und baut den Serienbriefkontext.
- `KGV.Web/features/serienbrief`: Auswahl, Editor, Freifelder, Platzhalter und Vorschau.
- `KGV.Web/services/serienbrief`: Vorlagenkompatibilität, Renderjobs, Routing und Statuszusammenfassung.
- `KGV.Core/Utilities/SerienbriefPdfFactory.cs`: Briefbogen-PDFs mit eindeutigen Dateinamen.
- `KGV.Core/Utilities/SerienbriefArchivService.cs`: Dokumentarchiv mit Mitglied und optionaler Parzelle.

## Datenmodell

Die Metadaten geben Serienbriefe und fachliche Exportfelder ausdrücklich frei. Vorlagen speichern Betreff, Rich-Text, Exportfeldabhängigkeiten und typisierte Freifelder. Technische IDs bleiben außerhalb der Vorlagenfelder.

## Empfängerregel

Der Export löst Empfänger streng in dieser Reihenfolge auf:

1. Aktives neues Pachtverhältnis: aktiver Hauptpächter.
2. Nur ohne neues Pachtverhältnis: genau eine aktuelle Legacy-Belegung.
3. Kein oder mehrdeutiger Empfänger: Konflikt mit `mitglied_id = NULL`.

Konflikte (`nicht_ermittelbar`, `mehrdeutig`) bleiben im Einstieg sichtbar und sind nicht auswählbar.

## Vorlageneditor und Vorschau

Der Editor unterstützt Betreff, fest vorgegebene Briefanrede, Rich-Text (Fett, Kursiv, Liste), Exportfelder und Freifelder. Platzhalter werden vor dem Speichern normalisiert, damit Rich-Text keine fragmentierten Platzhalter hinterlässt. Freifeldwerte gelten für den Lauf; beim Rückweg aus der Vorschau bleibt die Vorlage erhalten. Die Vorschau löst pro Datensatz Empfänger, Betreff, Text und Freifelder auf und erlaubt das Blättern.

## PDF und Archiv

Der PDF-Renderer erwartet den Version-2-Briefbogen mit Anschrift, Betreff, Anrede und Brieftext. Dateinamen werden bereinigt und innerhalb eines Laufs mit Suffixen eindeutig gehalten. Erfolgreiche PDFs werden mit Mitglied und – soweit vorhanden – Parzelle archiviert; doppelte Row-Keys werden nicht doppelt abgelegt.

## Routing und Status

Eine gültige Mailadresse mit Informations- oder Rechnungseinwilligung führt zum Mailausgang. Fehlende, ungültige oder nicht freigegebene Mailadressen führen zur Druckausgabe. Ein technischer Mailfehler bleibt ein Routingfehler und wird nicht automatisch zu Druck. Der Standardmailtext stammt aus `public.vertragsversand_mailtext_snapshot()` der Vertragsversand-Migration. Status zählt PDF, Archiv, Mail, Druck und Fehler getrennt und speichert Fehlerphase sowie letzten Erfolg.

## Rechte

Admin und Vorstand dürfen Export und Serienbrief verwenden sowie Vorlagen verwalten. Normale Benutzer erhalten keinen Serienbriefeinstieg; die Vorlagenmigration schützt die Daten zusätzlich per RLS.

## Abschlussprüfung

Der Vertragstest `serienbrief-workflow.test.ts` bildet den Zählerbrief mit Betreff `Erforderlicher Austausch Ihres {{zaehlerart}}`, Zählernummer, Eichfälligkeit und dem Datumsfreifeld `Frist` ab. Er prüft zwei Briefe desselben Mitglieds, die identische Auflösung von Vorschau und Renderjob, Konfliktisolierung sowie Mail-/Druckentscheidungen. Ergänzende Tests decken Platzhalter, Freifelder, Vorlagenkompatibilität, Empfängerauflösung, Status und Rechte ab.

Die drei Serienbriefmigrationen wurden statisch auf Reihenfolge, Abhängigkeiten, RLS, Grants und Funktionsnamen geprüft. Eine lokale Supabase-Ausführung und eine visuelle PDF-Prüfung waren in dieser Umgebung nicht möglich, weil Docker nicht verfügbar ist; sie wurden nicht vorgetäuscht.

## Tests/Builds

Der finale Serienbrief-Vertragstestlauf umfasst 20 TypeScript-Tests sowie die G2–G7-Vertragstests und die angrenzenden Capability-, Kommunikations- und Dokumenttests. Alle dabei relevanten Serienbrief-, Export-, Kommunikations- und Dokumenttests sind erfolgreich. `test:navigation-capabilities` schlägt unabhängig von Serienbrief in `KGV.Maui/AdminShell.xaml.cs` fehl, weil der vorhandene Test den nicht vorhandenen Aufruf `CanRegisterHistoricalContracts(managementContext)` erwartet; keine Serienbriefdatei ist daran beteiligt oder wurde angepasst.

`dotnet build` für Core und Infrastructure sowie `npm run build` für Web sind erfolgreich. Es bleiben bestehende Warnungen zu ImageSharp, Nullbarkeit in Core/Infrastructure und großen Web-Bundles.

## Bekannte Grenzen

- Es gibt keine eigene persistierte Serienbrief-Laufhistorie.
- Der tatsächliche Versand erfolgt weiterhin im vorhandenen Mailausgang, der physische Druck in der Druckausgabe.
- Die vorhandene Migration `supabase/migrations/20261031152500_fix_meter_reading_art_validation.sql` gehört nicht zu Serienbrief und wurde nicht verändert.
