# NB-2B-A1 – Audit fachlicher Vorgangs- und Vertragsnummern

Stand: 27.10.2026  
Geprüfter Code-Stand: `57ca3b82acdfc4e5702255e49098c580a26cac97`

## Umfang und Methode

Dieser Audit prüft die vier vorgesehenen aktiven Nummernworkflows im Repository: Mitgliedsantrag, Pachtvertrag, historischer Kombivertrag und Wartungsvertrag. Er umfasst SQL-Fundament, aktive Web-/MAUI- und Edge-Pfade, PDF-Felder, Dokumentmetadaten, Dateinamen und Wiederholungen. Die Prüfung ist statisch; die Migration `20261025130000_nb1_fachnummern_fundament.sql` wurde nicht ausgeführt und es wurde keine Datenbank abgefragt oder verändert.

Bewertungen: `OK`, `FEHLT`, `FEHLER`, `NICHT ERFORDERLICH`, `ENTSCHEIDUNG ERFORDERLICH` und `NICHT AKTIV`.

## Fundament

Fundstelle: `supabase/migrations/20261025130000_nb1_fachnummern_fundament.sql`.

- `fachnummer_reservierung` erzwingt den Primärschlüssel `(is_demo, nummer)` und die eindeutige Quellbindung `(is_demo, quelle_art, quelle_id)`.
- Die Bereiche sind korrekt per Check abgesichert: 10xxx Antrag, 20xxx Pacht, 80xxx historischer Kombivertrag und 90xxx Wartung.
- Es gibt je Bereich eine Produktiv- und Demo-Sequenz, jeweils mit `NO CYCLE`. Ein einmal per `nextval` gezogener Wert wird daher nicht wiederverwendet.
- `reserve_fachnummer` prüft Admin/Vorstand oder `service_role`, liefert für dieselbe Quelle die bestehende Reservierung und wählt die Demo-Sequenz anhand von `p_is_demo`.
- `trg_reserve_vertragsnummer` reserviert beim ersten Statuswechsel eines `kommunikationsvorgang` nach `zur_signatur` für Antrag, Pacht und Wartung. Das Demo-Flag stammt aus `mitglied.is_demo`.
- Tabelle und Funktion sind für `anon` gesperrt; `authenticated` darf lesen und die Funktion ausführen.

**B2-Status:** Eine neue, nicht ausgeführte Migration ersetzt die Funktion mit einer quellbezogenen Transaktionssperre. Gleichzeitige erste Reservierungen derselben Quelle erhalten dieselbe Reservierung; für unterschiedliche Quellen besteht keine globale Sperre. Eine vorhandene Quelle mit abweichender `art` wird mit einer fachlichen Fehlermeldung abgelehnt.

| Nummernart | Workflow | Reservierung | Anzeige | Retry | Korrektur/Verlängerung | Bewertung | Änderung nötig |
|---|---|---|---|---|---|---|---|
| 10xxx | Mitgliedsantrag | Trigger bei `zur_signatur`, Quelle `kommunikationsvorgang` | PDF, Aufnahmebestätigung und Dateiname | gleiche Quellreservierung wird gelesen | neuer Vorgang erhält neue Quelle | OK | Nein |
| 20xxx | Pachtvertrag | Trigger bei `zur_signatur`, Quelle `kommunikationsvorgang` | PDF, Titel, Dateiname und Vertragsfassungsansicht | vorhandenes Finaldokument wird je Revision wiederverwendet | echte unmittelbare Ersatzkette | OK | Nein |
| 80xxx | historischer Kombivertrag | ausdrückliche Aktion nach fachlicher Zuordnung, Quelle `historisches_dokument`/`dokument.id` | Titel, Metadaten, Ansicht und PDF-Stempel | dieselbe Quellreservierung | nicht anwendbar | OK | Nein |
| 90xxx | Wartungsvertrag | Trigger bei `zur_signatur`, kanonische Quelle `wartungsvertrag_id` | versioniertes v2-PDF, Titel, Dateiname, Mitglieds- und Workflowansichten | vorhandene Reservierung und Finaldokument werden wiederverwendet | normale Verlängerung behält dieselbe 90xxx; grundlegende Änderung erhält eine neue | OK (B4, Repository-Stand) | Nein |

## Mitgliedsantrag 10xxx

Der Prepared-Contract-Workflow legt den Vorgang zunächst vorbereitet an und startet ihn serverseitig über `start_prepared_contract_signature`; der Wechsel zu `zur_signatur` löst die Reservierung aus. `KGV.Infrastructure.Services.SupabaseService.LoadMitgliedsantragAntragsnummerAsync` liest danach explizit die 10xxx-Reservierung des konkreten Kommunikationsvorgangs und lehnt eine fehlende oder ungültige Reservierung ab.

`MitgliedsantragDokumentFactory` schreibt `Antrags-Nr. 10xxx` in das PDF und verwendet sie im Fachdateinamen. Der Edge-Renderer `kgv-generate-contract` liest dieselbe Reservierung für das v5-Template und für die Aufnahmebestätigung. Die Aufnahmebestätigung referenziert den ursprünglichen Mitgliedsantragsvorgang und erhält daher dieselbe Nummer.

Vorschauen vor `zur_signatur` bleiben ohne Antragsnummer. Finalisierungs- und Dokument-Retries verwenden `source_kommunikationsvorgang_id` plus Revision und lesen statt neu zu reservieren. Eine Ablehnung verbraucht die bereits reservierte Nummer; ein späterer neuer Antrag derselben Person erzeugt einen neuen Vorgang und damit eine neue 10xxx-Nummer. Kein aktiver sichtbarer Fallback leitet die Nummer aus einer technischen ID ab.

**Bewertung: OK.**

## Pachtvertrag 20xxx

Der Prepared-Pachtvertrag geht über denselben Statuswechsel zu `zur_signatur`; damit ist die 20xxx-Reservierung vorhanden. `kgv-generate-contract` lädt die bestehende Reservierung in `loadExistingFachnummer` und erzeugt den sichtbaren Dateinamen `Pachtvertrag-20xxx_…pdf`. Vor einer Reservierung bleibt die Vorschau ohne 20xxx und verwendet nur vorhandene Fachwerte.

Das versionierte v9-Template ergänzt `vertragsnummer` und zeigt nach der Reservierung `Vertrags-Nr. 20xxx`; eine Vorschau davor bleibt bewusst leer. Edge-Generator, Titel und Dateiname lesen ausschließlich die Reservierung des Kommunikationsvorgangs. Die Vertragsfassungsansicht löst dieselbe Nummer über Dokument-Provenance und `fachnummer_reservierung` auf.

Der Altpfad `signExisting` bleibt aus Kompatibilitätsgründen bestehen, ist aber nicht mehr der Korrekturpfad. Web und MAUI starten Korrekturen ausschließlich als neuen Prepared-Vorgang; dadurch entstehen neue Signaturen, eine neue 20xxx und der normale Finalisierungs-/Versandpfad.

**Bewertung: OK.**

## Historischer Kombivertrag 80xxx

Der Altimport (`HistoricalDocumentImportPage`, `HistoricalDocumentUploadDialog`, `register_historisches_dokument`) übernimmt einen vom Nutzer gewählten Scan und ordnet Typ, Mitglied, Parzelle und Wirkungen atomar zu. Die Registrierungsfunktion prüft Wiederholungen auf denselben vollständigen Fachzustand; sie erzeugt keine zweite Pachtvertragsfassung oder Wartungszuordnung.

**B2 umgesetzt:** Nach der fachlichen Zuordnung zeigen Web und MAUI ausdrücklich `Nummer zuweisen`. Erst diese bewusste Aktion ruft die gemeinsame Backend-Aktion auf. Die stabile Quelle ist die hochgeladene `dokument.id`, als `quelle_art = historisches_dokument`; beim Öffnen, der Scan-, Mitglieds- oder Parzellenauswahl wird keine Nummer reserviert. Wiederholungen verwenden dieselbe Reservierung.

Das Backend leitet `is_demo` ausschließlich aus `dokument.mitglied_id -> mitglied.is_demo` ab. Es führt die 80xxx sichtbar in Titel und Metadaten. PDFs werden serverseitig mit `Vertrags-Nr. 80xxx` gestempelt; JPG/PNG/WebP und andere Bildscans bleiben grafisch unverändert und führen die Nummer über Titel und Metadaten. Der Originaldateiname bleibt erhalten; eine technische ID wird nicht als Vertragsnummer verwendet.

**Bewertung: OK.**

## Wartungsvertrag 90xxx

**B4-Abschluss im Repository-Stand:** `fachnummer_reservierung` ist die einzige Quelle. Die neue Migration macht die Nummer über `v_wartungsvertrag_fachnummer` pro `wartungsvertrag_id` kanonisch; der Statuswechsel eines Prepared-Vorgangs nach `zur_signatur` reserviert die Nummer für genau diese Vertragsdefinition. Vorher bleibt die Vorschau ohne Nummer. Bei finaler Erzeugung verwenden PDF-Feld, Titel, Dateiname und Metadaten dieselbe 90xxx. Die v1-Artefakte bleiben unverändert; Wartungsvertrag und Verlängerungsmitteilung erhalten versionierte v2-Artefakte mit `vertragsnummer`.

Eine normale Verlängerung behält dieselbe `wartungsvertrag_id` und dieselbe 90xxx. Ihre Mitteilung referenziert „Wartungsvertrag Nr. 90xxx“ und erzeugt keine eigene Nummer. Wiederholungen verwenden die vorhandene Reservierung und das bereits gespeicherte Dokument.

Eine grundlegende Änderung wird ausschließlich durch die ausdrückliche Aktion „Neuen Wartungsvertrag wegen grundlegender Änderung erstellen“ gestartet; es gibt keinen Feldvergleich oder eine automatische Heuristik. Der Server legt einen neuen inaktiven Wartungsvertrag mit technischer Vorgängerzuordnung und einen Prepared-Signaturvorgang an, aber noch keine neue Nummer oder neue Zuordnung. Erst der normale Signaturstart reserviert die neue 90xxx. Erst der erfolgreiche Abschluss nach vollständigen Unterschriften und finalem Dokument aktiviert den neuen Vertrag, legt die neue Zuordnung an und beendet nötigenfalls die alte Laufzeit. Vorbereitung, Abbruch, Teilunterschrift und Dokumentfehler verändern den Altvertrag nicht. Eindeutige Vorgängerbindung, wiederverwendeter Vorgang/dieselbe Reservierung und idempotenter Abschluss verhindern Dubletten.

Der Wartungsvertrags-Signaturflow und die Web-/MAUI-Mitglieds- und Fortführungsansichten verwenden die Vertragsnummer statt technischer Wartungsvertrags-IDs. Die gefundenen Wartungsvertrags-Fallbacks in Fortführungs-, Mitglieds- und Zuweisungsansichten sind beseitigt. Mit B5 wurden auch die allgemeinen sichtbaren Vertrags-, Dokument-, Kommunikations-, Ablesungs- und Parzellen-ID-Fallbacks entfernt.

Die beiden neuen Templatekatalogeinträge sind ausdrücklich `draft` und inaktiv. Die Migrationen und lokalen Artefakte sind vorbereitet; sie wurden nicht angewendet, hochgeladen oder aktiviert. Deshalb ist die PDF-Nummerndarstellung im lokalen Generator-/Field-Spec-Vertrag geprüft, nicht in einem produktiv aktivierten Template.

**Bewertung: OK (B4 im Repository; operative Aktivierung bleibt separat).**

## Korrektur-/Ersatzverträge

Der Korrekturstart bindet die aktuelle `pachtvertragsfassung` als unmittelbaren Vorgänger am neuen Prepared-Vorgang. Beim Abschluss verwendet die bestehende Registrierung diese Fassung über `vorgaenger_fassung_id`; das bestehende Pachtverhältnis und alle Dokumente bleiben erhalten. Erst in derselben erfolgreichen Abschluss-Transaktion erhält der Vorgänger `nicht_mehr_massgeblich_am`.

Damit bleiben vorbereitete, teilweise signierte, fehlgeschlagene oder abgebrochene Ersatzvorgänge ohne Wirkung auf die bisherige Maßgeblichkeit. A → B → C ist rekonstruierbar, weil C an B gebunden wird. Finalisierungs-Retries verwenden Vorgang/Revision, vorhandenes Dokument, Vertragsfassung und 20xxx erneut. Der normale Versand setzt auf dem abgeschlossenen neuen Prepared-Vorgang auf und versendet nicht automatisch den Vorgänger.

**Bewertung: OK.**

## Retry / Idempotenz

- Die Quelleneindeutigkeit macht wiederholte sequenzielle `reserve_fachnummer`-Aufrufe idempotent.
- Antrags-, Pacht- und Wartungsfinalisierung suchen vorhandene Dokumente nach Kommunikationsvorgang und Revision und verwenden sie wieder.
- Altimportregistrierung ist bei identischem Fachzustand idempotent.
- Die B2-Sperre macht auch parallele Erstreservierungen derselben Quelle idempotent. Ein technischer Sequenzsprung bleibt zulässig, eine doppelte Reservierung oder ein sichtbarer Unique-Fehler nicht.

**Bewertung: OK.**

## Demo / Produktion

Die acht Sequenzen sind je Nummernart strikt getrennt. Für Kommunikationsvorgänge bestimmt der Trigger die Wahl über `mitglied.is_demo`; die Reservierung enthält das gleiche Flag im Primär- und Unique-Schlüssel. Für den historischen Kombivertrag liest die gemeinsame Backend-Aktion das Flag ausschließlich über das ausgewählte Mitglied des historischen Dokuments.

**Bewertung: OK.**

## Sichtbare technische IDs

| Fundstelle | Befund | Bewertung |
|---|---|---|
| `kgv-generate-contract/signExisting` | neutraler sichtbarer Dateiname `Pachtvertrag-signiert.pdf`; technische Dokument-ID nur intern | in B5 behoben |
| `kgv-generate-contract` und `.NET PachtvertragDokumentFactory` | Gartennummer im PDF, sonst neutral `nicht verfügbar`; kein Fallback auf `parzelle.id` | in B5 behoben |
| `MaintenanceContinuationPanel.tsx` | zuvor „Bestehender Wartungsvertrag #<wartungsvertrag_id>“ | in B4 behoben; verwendet 90xxx |
| `maintenance-assignment-service.ts`, `AssignContractsToMember.tsx` | zuvor Fallback `Wartungsvertrag #<wartungsvertrag_id>` | in B4 behoben; verwendet 90xxx bzw. neutrale Bezeichnung |
| Web-Parzellen-, Zähler- und Protokollansichten | zuvor sichtbare `#<parzelle_id>`-Fallbacks und eine explizite Parzellen-ID | in B5 behoben; Gartennummer oder neutraler Text |
| MAUI-Parzellen-, Dokument- und Pachtvertragsansichten | zuvor sichtbare ID-Zeilen und `#<parzelle_id>`-Fallbacks | in B5 behoben; Gartennummer oder neutraler Text |
| Kommunikationszentrum, Vertragsversand und Finalprüfung | zuvor Vorgangs-, Revisions-, Dokument- oder Vorgänger-ID sichtbar | in B5 behoben; fachlicher Status bzw. neutraler Verfügbarkeitstext |
| historischer Upload und offene Ablesefotos | zuvor Dokument- bzw. Ablesungs-ID sichtbar | in B5 behoben; neutraler Status-/Kontexttext |

Die übrigen geprüften IDs in FKs, RPC-Parametern, Navigation und Provenance bleiben intern zulässig. Insbesondere wird keine 10xxx-, 20xxx-, 80xxx- oder 90xxx-Nummer aus einer technischen ID berechnet.

## Offene Fachentscheidungen

1. **Grundlegende Wartungsvertragsänderung:** Durch NB-2B-B4 umgesetzt; kein Feldvergleich und keine automatische Heuristik.

## Vorgeschlagene Umsetzungsblöcke

1. **NB-2B-B2 – Reservierungsrobustheit und 80xxx:** Konfliktsichere `reserve_fachnummer`, manuelle Altimport-Zuweisung, Wiederverwendung und Anzeige von 80xxx.
2. **NB-2B-B3 – Pachtvertrag 20xxx und Ersatzkette:** PDF-/Metadatenfelder, Dokumentlisten, fachlicher Alt-Signierpfad sowie persistierte Vorgängerreferenz für Korrektur-/Ersatzverträge.
3. **NB-2B-B4 – Wartungsvertrag 90xxx:** Sichtbarkeit in PDF, Metadaten und Verlängerungsmitteilung; danach die Entscheidung und Umsetzung für grundlegende Änderungen.
4. **NB-2B-B5 – sichtbare technische Fallbacks:** abgeschlossen; Parzellen-, Vertrags-, Dokument-, Kommunikations- und Ablesungs-IDs wurden durch fachliche Nummern oder neutrale Angaben ersetzt.

## Abschluss – Ausgangsbefund NB-2B-A1

Die folgende Zählung dokumentiert den historischen A1-Ausgangsbefund vor B2–B4:

| Kennzahl | Anzahl |
|---|---:|
| geprüfte aktive Nummernworkflows | 4 |
| OK | 1 |
| FEHLT | 2 |
| FEHLER | 3 |
| ENTSCHEIDUNG ERFORDERLICH | 1 |
| technische-ID-Fallbacks | 4 |
| Reservierungsprobleme | 1 |
| Retry-/Idempotenzprobleme | 1 |
| Korrektur-/Verlängerungsprobleme | 2 |

**NB-2B-A1 Fachnummern-Audit = 100 %.**

## NB-2B-B2 Abschluss

- Reservierungsrobustheit: **OK**
- 80xxx historischer Kombivertrag: **OK**
- Demo/Produktion 80xxx: **OK**
- Gemeinsame Backendlogik für Web und MAUI: **OK**
- Migration erstellt, aber nicht ausgeführt.

**NB-2B-B2 = 100 %.**

## NB-2B-B3 Abschluss

- Pachtvertrag 20xxx in PDF, Titel, Dateiname und Vertragsfassungsansicht: **OK**
- `fachnummer_reservierung` bleibt einzige Nummernquelle: **OK**
- unmittelbare Vorgängerrelation und Ketten A → B → C: **OK**
- atomarer Maßgeblichkeitswechsel erst nach erfolgreichem Abschluss: **OK**
- Prepared-Workflow, neue Signaturen, Retry und normaler Versand: **OK**
- Web-/MAUI-Aktion „Korrektur-/Ersatzvertrag erstellen“: **OK**
- `signExisting` nur noch Altkompatibilität, nicht Korrekturworkflow: **OK**
- Migrationen und v9-Katalogeintrag erstellt, aber nicht ausgeführt oder aktiviert.

**NB-2B-B3 = 100 %.**

## NB-2B-B4 Abschluss

- 90xxx-Quelle je Wartungsvertrag, Nummernreservierung erst bei `zur_signatur`, Vorschau vorher ohne Nummer: **OK**
- Wartungsvertrag-PDF, Titel, Dateiname und Metadaten verwenden dieselbe reservierte 90xxx: **OK im lokalen Generator-/v2-Vertrag; v2 bleibt draft/inaktiv**
- Normale Verlängerung behält Vertrags-ID und 90xxx; Verlängerungsmitteilung referenziert die bestehende Nummer und erzeugt keine eigene: **OK**
- Ausdrücklicher grundlegender Änderungsstart, neuer inaktiver Vertrag, Prepared-Signaturen und neue 90xxx: **OK**
- Altvertrag bleibt bis zum vollständigen signierten Dokumentabschluss unverändert; Abschluss und Wiederholung sind idempotent: **OK**
- Keine automatische Feldvergleichsheuristik; Web und MAUI nutzen denselben Prepared-/Backendpfad: **OK**
- Die gefundenen Wartungsvertrags-ID-Fallbacks in `MaintenanceContinuationPanel.tsx`, `maintenance-assignment-service.ts` und `AssignContractsToMember.tsx` sind durch 90xxx bzw. neutrale Texte ersetzt; die übrigen allgemeinen Vertrags-/Parzellen-Fallbacks wurden anschließend in B5 geschlossen.
- Die Generator-Finalisierung prüft die Mitgliedsunterschrift gegen die Vertragspartei des signierten Vorgangs und unterstützt so den Grundänderungs-Prepared-Vertrag ohne aktive Zuordnung.
- B4-Tests: **57 bestanden** (46 Web-/Generator- und 11 .NET-Tests); der Workflow-Validator unterscheidet Erstvertrag und Verlängerungsmitteilung anhand ihrer Vorgangsart. Web-, Core- und Infrastructure-Build erfolgreich. Der MAUI-Build scheitert an 53 unterschiedlichen vorhandenen Compilerfehlern (106 Meldungen über zwei Target Frameworks), darunter zwei unveränderte `LineBreakMode`-Verwendungen in der Fortführungsseite; keine B4-spezifischen Compilerfehler.
- Schema- und Templatekatalogmigrationen sowie zwei v2-Artefaktpaare erstellt; **keine Migration angewendet**, keine Templates hochgeladen oder aktiviert.

**NB-2B-B4 = 100 %.**

## NB-2B-B5 Abschluss

- `signExisting` erzeugt den neutralen sichtbaren Dateinamen `Pachtvertrag-signiert.pdf`; die technische Quelldokument-ID bleibt ausschließlich intern.
- Pachtvertrag-PDFs aus Edge Function und .NET verwenden die Gartennummer oder den neutralen Text `nicht verfügbar`, niemals die Parzellen-ID.
- Web- und MAUI-Parzellenansichten, Zählerfilter, Protokollhistorie und Dokumentkontexte verwenden Gartennummern bzw. neutrale Fallbacks; explizite technische Parzellen-ID-Zeilen wurden entfernt.
- Kommunikationszentrum, Vertragsversand, historische Uploads, Foto-Uploads und Finalprüfung zeigen keine technischen Vorgangs-, Revisions-, Dokument-, Ablesungs- oder Vorgänger-IDs mehr.
- Die B4-Wartungsvertragsansichten bleiben fachnummernbasiert (`90xxx`) und fallen andernfalls neutral zurück.
- UUIDs bleiben nur in internen Request-, Storage-, Cache-, Pending- und Client-Schlüsseln. Sichtbare Dateinamen werden davon getrennt fachlich/original benannt.
- Schlussaudit über Web, MAUI, Core, Infrastructure und Edge Functions: **0 Treffer** für die bekannten sichtbaren Hash-/ID-Fallbackmuster (`#${...}`, `Mitglied #`, `Parzelle #`, `Garten #`, `Dokument #`, `Wartungsvertrag #`, `Vorgangs-ID`, `Parzellen-ID`, `Finaldokument #`, `Ablesung #`, sichtbare `ID`-Detailzeilen sowie die beiden PDF-Fallbacks).
- Tests: **30/30 bestanden** (16 B5-Vertragstests, 3 bestehende sichtbare-Dateinamen-Tests und 11 bestehende B2–B4-Fachnummerntests). Der ältere vollständige .NET-Testbestand bleibt mit 354 bestandenen und 39 fachfremden Altfehlern inkonsistent.
- Builds: Web, Core und Infrastructure erfolgreich. MAUI reproduziert die bereits aus B4 bekannten **53 fachfremden Compilerfehler**; keine geänderte B5-Stelle ist in der Fehlerliste enthalten.
- Keine Migration notwendig oder angelegt; keine Migration ausgeführt. Kein Template aktiviert. E2E gemäß Vorgabe nicht gestartet.

**NB-2B-B5 = 100 %.**

**NB-2B = 100 %.**

**Nummerierungssystem / Phase 7 = 100 %.**
