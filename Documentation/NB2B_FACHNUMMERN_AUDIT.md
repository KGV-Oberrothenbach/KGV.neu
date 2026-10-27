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

Offener technischer Befund: `reserve_fachnummer` führt „lesen, nextval, insert“ ohne Konfliktbehandlung aus. Gleichzeitige erste Reservierungen für dieselbe Quelle können eine Unique-Verletzung auslösen und eine Sequenznummer zusätzlich verbrauchen. Es entsteht keine doppelte Zuweisung, aber der Retry ist in diesem Parallelfall nicht robust.

| Nummernart | Workflow | Reservierung | Anzeige | Retry | Korrektur/Verlängerung | Bewertung | Änderung nötig |
|---|---|---|---|---|---|---|---|
| 10xxx | Mitgliedsantrag | Trigger bei `zur_signatur`, Quelle `kommunikationsvorgang` | PDF, Aufnahmebestätigung und Dateiname | gleiche Quellreservierung wird gelesen | neuer Vorgang erhält neue Quelle | OK | Nein |
| 20xxx | Pachtvertrag | Trigger bei `zur_signatur`, Quelle `kommunikationsvorgang` | Dateiname; nicht im PDF-Feld/Titel | vorhandenes Finaldokument wird je Revision wiederverwendet | Korrekturbezug nicht modelliert | FEHLER | Ja |
| 80xxx | historischer Kombivertrag | keine Anbindung gefunden | keine Anzeige | Altimport ist fachlich idempotent, aber ohne Nummer | nicht anwendbar | FEHLT | Ja |
| 90xxx | Wartungsvertrag | Trigger bei `zur_signatur`, Quelle `kommunikationsvorgang` | Dateiname; nicht im PDF-Feld/Titel | vorhandenes Finaldokument wird wiederverwendet | Verlängerung erzeugt bewusst keine neue Reservierung | FEHLER | Ja |

## Mitgliedsantrag 10xxx

Der Prepared-Contract-Workflow legt den Vorgang zunächst vorbereitet an und startet ihn serverseitig über `start_prepared_contract_signature`; der Wechsel zu `zur_signatur` löst die Reservierung aus. `KGV.Infrastructure.Services.SupabaseService.LoadMitgliedsantragAntragsnummerAsync` liest danach explizit die 10xxx-Reservierung des konkreten Kommunikationsvorgangs und lehnt eine fehlende oder ungültige Reservierung ab.

`MitgliedsantragDokumentFactory` schreibt `Antrags-Nr. 10xxx` in das PDF und verwendet sie im Fachdateinamen. Der Edge-Renderer `kgv-generate-contract` liest dieselbe Reservierung für das v5-Template und für die Aufnahmebestätigung. Die Aufnahmebestätigung referenziert den ursprünglichen Mitgliedsantragsvorgang und erhält daher dieselbe Nummer.

Vorschauen vor `zur_signatur` bleiben ohne Antragsnummer. Finalisierungs- und Dokument-Retries verwenden `source_kommunikationsvorgang_id` plus Revision und lesen statt neu zu reservieren. Eine Ablehnung verbraucht die bereits reservierte Nummer; ein späterer neuer Antrag derselben Person erzeugt einen neuen Vorgang und damit eine neue 10xxx-Nummer. Kein aktiver sichtbarer Fallback leitet die Nummer aus einer technischen ID ab.

**Bewertung: OK.**

## Pachtvertrag 20xxx

Der Prepared-Pachtvertrag geht über denselben Statuswechsel zu `zur_signatur`; damit ist die 20xxx-Reservierung vorhanden. `kgv-generate-contract` lädt die bestehende Reservierung in `loadExistingFachnummer` und erzeugt den sichtbaren Dateinamen `Pachtvertrag-20xxx_…pdf`. Vor einer Reservierung bleibt die Vorschau ohne 20xxx und verwendet nur vorhandene Fachwerte.

Die PDF-Feldspezifikationen v3–v8 enthalten jedoch kein Feld `vertragsnummer`; auch der Dokumenttitel bleibt `Pachtvertrag (signiert)`. Die Vertragsnummer ist damit weder im PDF noch zuverlässig in Dokumentenliste/Vorgangsansicht sichtbar. Die Finalisierung ist je Kommunikationsvorgang und Revision idempotent.

Zusätzlicher Fehler im aktiven Alt-Signierpfad `sign-existing`: `Pachtvertrag-signiert-${source_document_id}.pdf` gibt eine technische Dokument-ID sichtbar aus und erstellt keine 20xxx-Reservierung. Dieser Pfad ist nicht als Korrekturworkflow modelliert.

**Bewertung: FEHLER.**

## Historischer Kombivertrag 80xxx

Der Altimport (`HistoricalDocumentImportPage`, `HistoricalDocumentUploadDialog`, `register_historisches_dokument`) übernimmt einen vom Nutzer gewählten Scan und ordnet Typ, Mitglied, Parzelle und Wirkungen atomar zu. Die Registrierungsfunktion prüft Wiederholungen auf denselben vollständigen Fachzustand; sie erzeugt keine zweite Pachtvertragsfassung oder Wartungszuordnung.

Es gibt aber keine UI-, Service- oder SQL-Anbindung an `reserve_fachnummer('historischer_kombivertrag', 'historisches_dokument', …)`. 80xxx kann deshalb weder ausdrücklich zugewiesen noch im Stempel, Titel, Metadaten, Downloadnamen oder Versand dargestellt werden. Die vorgegebene manuelle Zuweisungsaktion ist somit noch nicht implementiert, nicht etwa automatisch zu ersetzen.

**Bewertung: FEHLT.**

## Wartungsvertrag 90xxx

`ensure_wartungsvertrag_prepared_contract` erstellt einen vorbereiteten Signaturvorgang; `MaintenanceContractSignatureFlow` startet ihn über `start_prepared_contract_signature`. Der Trigger reserviert dadurch eine 90xxx-Nummer. Der Edge-Renderer liest sie für den Dateinamen `Wartungsvertrag-90xxx_…pdf`; Wiederholungen finden das bereits vorhandene Finaldokument über Vorgang, Revision und Zuordnung.

Das Wartungsvertragstemplate v1 besitzt kein Vertragsnummernfeld. Titel, Dokumentmetadaten und die sichtbaren Workflowansichten zeigen die 90xxx-Nummer nicht. Die Verlängerungsmitteilung arbeitet bewusst mit einem eigenen Dokumentvorgang und erhält keine neue Reservierung; sie zeigt aber auch nicht die Nummer des fortgeführten Wartungsvertrags.

Eine normale Verlängerung behält technisch dieselbe `wartungsvertrag_id`, wie die Fortführungsfunktionen verlangen. Ob eine grundlegende Änderung einen neuen Wartungsvertrag und damit eine neue 90xxx anlegt, ist im aktuellen Code nicht als fachlicher Entscheidungs- oder Nachfolgepfad erkennbar.

**Bewertung: FEHLER; grundlegende Änderung: ENTSCHEIDUNG ERFORDERLICH.**

## Korrektur-/Ersatzverträge

Für den Pachtvertrag gibt es nur `has_previous_contract` und `previous_contract_date`. Das PDF kann einen Hinweis auf eine bisherige Urkunde ausgeben, persistiert jedoch weder einen unmittelbaren Vorgänger noch eine Kette. Der Alt-Signierpfad bewahrt die unsignierte Quelle, erzeugt aber keinen neuen Prepared-Vorgang und keine neue 20xxx-Nummer.

Die verbindliche Regel „neuer Korrektur-/Ersatzvertrag mit neuer 20xxx, Vorgängerreferenz und weiterhin sichtbarem Altvertrag“ wird folglich noch nicht erfüllt.

**Bewertung: FEHLT.**

## Retry / Idempotenz

- Die Quelleneindeutigkeit macht wiederholte sequenzielle `reserve_fachnummer`-Aufrufe idempotent.
- Antrags-, Pacht- und Wartungsfinalisierung suchen vorhandene Dokumente nach Kommunikationsvorgang und Revision und verwenden sie wieder.
- Altimportregistrierung ist bei identischem Fachzustand idempotent.
- Die unter „Fundament“ genannte Parallelrace ist offen; sie kann keine Nummer doppelt vergeben, aber einen ersten Aufruf mit Konflikt scheitern lassen und eine Nummer zusätzlich verbrauchen.

**Bewertung: FEHLER (nur parallele Erstreservierung).**

## Demo / Produktion

Die acht Sequenzen sind je Nummernart strikt getrennt. Für Kommunikationsvorgänge bestimmt der Trigger die Wahl über `mitglied.is_demo`; die Reservierung enthält das gleiche Flag im Primär- und Unique-Schlüssel. Für den noch fehlenden historischen Reservierungsaufruf muss das Demo-Flag aus dem historischen Dokument bzw. dessen Mitglied fachlich festgelegt und explizit übergeben werden.

**Bewertung: OK für 10xxx/20xxx/90xxx; FEHLT für 80xxx.**

## Sichtbare technische IDs

| Fundstelle | Befund | Bewertung |
|---|---|---|
| `kgv-generate-contract/signExisting` | Dateiname `Pachtvertrag-signiert-<dokument_id>.pdf` | FEHLER |
| `kgv-generate-contract` Pachtwerte | Fallback `parzelle_nr = #<parzelle.id>` im PDF | FEHLER |
| `MaintenanceContinuationPanel.tsx` | „Bestehender Wartungsvertrag #<wartungsvertrag_id>“ | FEHLER |
| `maintenance-assignment-service.ts`, `AssignContractsToMember.tsx` | Fallback `Wartungsvertrag #<wartungsvertrag_id>` | FEHLER |

Die übrigen geprüften IDs in FKs, RPC-Parametern, Navigation und Provenance bleiben intern zulässig. Insbesondere wird keine 10xxx-, 20xxx-, 80xxx- oder 90xxx-Nummer aus einer technischen ID berechnet.

## Offene Fachentscheidungen

1. **Grundlegende Wartungsvertragsänderung:** Die Abgrenzung zur Verlängerung und der Auslöser für einen neuen Wartungsvertrag mit neuer 90xxx-Nummer fehlen.
2. **Historischer Kombivertrag:** Der Auftrag fordert eine explizite manuelle 80xxx-Zuweisung. Zu entscheiden bleibt nur, an welcher Prüf-/Freigabestufe diese Aktion verbindlich erfolgt und wie das Demo-Flag bei Altmaterial bestimmt wird.

## Vorgeschlagene Umsetzungsblöcke

1. **NB-2B-B2 – Reservierungsrobustheit und 80xxx:** Konfliktsichere `reserve_fachnummer`, manuelle Altimport-Zuweisung, Wiederverwendung und Anzeige von 80xxx.
2. **NB-2B-B3 – Pachtvertrag 20xxx und Ersatzkette:** PDF-/Metadatenfelder, Dokumentlisten, fachlicher Alt-Signierpfad sowie persistierte Vorgängerreferenz für Korrektur-/Ersatzverträge.
3. **NB-2B-B4 – Wartungsvertrag 90xxx:** Sichtbarkeit in PDF, Metadaten und Verlängerungsmitteilung; danach die Entscheidung und Umsetzung für grundlegende Änderungen.
4. **NB-2B-B5 – sichtbare technische Fallbacks:** Parzellen- und Wartungsvertrags-IDs durch neutrale/fachliche Angaben ersetzen.

## Abschluss

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

**NB-2B noch NICHT = 100 %.**
