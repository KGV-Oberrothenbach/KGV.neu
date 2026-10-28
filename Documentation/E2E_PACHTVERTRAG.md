# Phase 9 – E2E Pachtvertrag

## Ergebnis

Der produktive Pachtvertragsweg ist ein durchgängiger Prepared-Workflow. Web und MAUI erzeugen keinen separaten fachlichen Abschluss: Beide arbeiten mit `kommunikationsvorgang`, Revision, persistierten Signaturpositionen, der 20xxx-Reservierung beim Übergang `zur_signatur`, dem Finaldokument und `complete_prepared_pachtvertrag`.

Phase 9 schließt drei gefundene Lücken:

1. Pächter 2 war in Web und PDF-Generator künstlich auf das verknüpfte Nebenmitglied begrenzt. Nun ist jedes andere aktive, aufgenommene Mitglied zulässig; ein verknüpftes Nebenmitglied bleibt die bequeme Vorauswahl.
2. Der Mitgliedsstatus der Pächter wurde vor dem serverseitigen Abschluss nicht nochmals verbindlich geprüft. Die neue Migration prüft Haupt- und Mitpächter am Abschluss und schützt zusätzlich neue Beteiligungen per Trigger.
3. Ein wiederholter Start desselben Ersatzvertrags konnte an der Eindeutigkeit scheitern. Der Wrapper gibt nun den bereits aktiven, demselben Benutzer zugewiesenen Prepared-Vorgang zurück.

## Tatsächlicher Ablauf

1. Aus dem Mitgliedskontext werden Hauptpächter, zulässige Parzellen, Saison, Altvertragsangaben und gegebenenfalls Pächter 2 geladen.
2. Pächter 1 und 2 müssen `aktiv = true` und `aufnahme_status = aufgenommen` erfüllen. Pächter 2 muss von Pächter 1 verschieden sein. Es wird kein neuer Personen- oder Mitgliedsstamm erzeugt.
3. Die Oberfläche erfasst Vertragsbeginn, historischen Papier-Altvertrag, Übergabe, weitere Vereinbarungen und gegebenenfalls das Wirksamkeitsdatum einer Neufassung.
4. Die Vorschau verwendet die aktive versionierte Pachtvertragsvorlage v9. Vor `zur_signatur` ist keine Vertragsnummer erforderlich und es wird keine technische Ersatznummer angezeigt.
5. Der Prepared-Vorgang wird in `kommunikationsvorgang` gespeichert. Vereinsunterlagen, Vereinsunterzeichner und Vertragsdaten werden revisionsbezogen eingefroren.
6. Beim Übergang `zur_signatur` reserviert `fachnummer_reservierung` genau eine 20xxx für den Kommunikationsvorgang. Wiederholungen lesen dieselbe Reservierung.
7. Persistente Signaturpositionen werden je Revision angelegt: `lease.member`, optional `lease.second_party`, `vorstand_1` und `vorstand_2`. Die Finalisierung bleibt gesperrt, solange eine Pflichtposition fehlt.
8. Der Generator erstellt das Final-PDF mit der reservierten Vertragsnummer. Titel und Dateiname sind fachnummernbasiert; Mitglieds- und Parzellennummern stammen aus den fachlichen Feldern, nicht aus technischen IDs.
9. `complete_prepared_pachtvertrag` registriert atomar die Pachtvertragsfassung. Beim Erstvertrag entsteht genau ein `pachtverhaeltnis` mit Beteiligungen; bei einer Neufassung oder einem Ersatzvertrag wird das bestehende Verhältnis weiterverwendet.
10. Das Finaldokument friert den Versand-Snapshot ein. `prepare_contract_dispatch` erzeugt den normalen Prepared-Versand einschließlich Vertragsparteien und Pflichtunterlagen idempotent.

## Datenmodell und beteiligte Bausteine

Zentrale Tabellen sind:

- `kommunikationsvorgang` für Prepared-Status, Revision, Entwurf, Finaldokument und Ersatzbezug
- `fachnummer_reservierung` als einzige Quelle der 20xxx
- `kommunikationsvorgang_signatur` für persistente Pflichtsignaturen
- `dokument` für Vorschau-/Finaldokumente
- `pachtverhaeltnis`, `pachtverhaeltnis_beteiligung` und `pachtvertragsfassung` für das fachliche Vertragsmodell
- `parzellen_belegung` für die bereits zuvor bestehende operative Mitglied-Parzellen-Zuordnung
- `vertragsversand_vertragspartei_snapshot`, `vertragsversand_anlage_snapshot`, `versandinstanz` und Empfänger-/Dokument-Snapshots für den Versand

Wesentliche RPCs sind der Übergang des Kommunikationsvorgangs nach `zur_signatur`, `complete_prepared_pachtvertrag`, `prepare_pachtvertrag_ersatz`, `ensure_vertragsversand_snapshot` und `prepare_contract_dispatch`.

Web verwendet insbesondere `LeaseContractFlow`, `lease-service`, `prepared-contract-service`, `prepared-contract-signature-service`, `member-club-document-snapshot-service` und den Generator `kgv-generate-contract`. MAUI verwendet `PachtvertragFlowHelper`, den gemeinsamen Prepared-Mapper/-Parser und dieselben serverseitigen Zustände/RPCs.

## Nummern, PDF und sichtbare Identitäten

- Die Vertragsnummer wird ausschließlich beim bestehenden Übergang `zur_signatur` reserviert.
- Vorher bleibt das Feld leer; es gibt weder technische ID noch sichtbaren Ersatzwert.
- Retry desselben Vorgangs behält dieselbe Nummer. Nach Abbruch bleibt eine reservierte Nummer verbraucht.
- Die aktive Vorlage ist Pachtvertrag v9. Katalog-PDF-SHA: `0fa618ce6405e570c7a1de1588ad31df89029e7daf89ee2259d3106874277940`; Field-Spec-SHA: `1b9833e769812c9949b6fbc888507398aad2afa49ac6b481ec9f1805dfa52032`.
- Das Feld `vertragsnummer` stellt die 20xxx sichtbar dar. Pächteranzeigen verwenden `mitglied.mitgliedsnummer`; Parzellenanzeigen verwenden die fachliche Garten-/Parzellennummer.
- Titel und Dateiname basieren auf der Fachnummer und enthalten keine Dokument-ID, Vorgangs-ID, UUID oder Pachtverhältnis-ID.

## Pächter und Minderjährige

Pächter 1 ist ein vorhandenes aktives, aufgenommenes Mitglied. Pächter 2 kann ein verknüpftes Nebenmitglied oder jedes andere aktive, aufgenommene Haupt-/Vollmitglied sein. Nichtmitglieder, inaktive Mitglieder, offene Anträge und dieselbe Person als beide Pächter werden abgewiesen.

Die bestehende Minderjährigenregel bleibt unverändert: Der minderjährige Hauptpächter ist Vertragspartei; der im signierten Mitgliedsantrag hinterlegte aktive gesetzliche Vertreter wird wiederverwendet und übernimmt die zweite Vertragsparteien-Signatur. Es wird kein Vertreter doppelt angelegt und kein zusätzlicher Pächter 2 aufgenommen.

## Vereinsunterzeichnung und Berechtigungen

Die zwei Vereinsunterschriften werden aus dem eingefrorenen Funktions-/Vertretungssnapshot bestimmt. Namen, frei formulierte Amtsbezeichnungen oder hartcodierte Vorsitzenden-Texte entscheiden nicht über die Berechtigung. Änderungen an Funktionen während eines laufenden Vorgangs verändern den eingefrorenen Stand nicht.

Vorbereitung, Ersatzstart und Bedienung bleiben auf die bestehenden Admin-/Vorstandsrollen und Capabilities begrenzt. Der Ersatz-RPC prüft `auth.uid()`, Zuweisung und Rolle. Der Abschluss-RPC ist ausschließlich für `service_role` ausführbar. Versand verwendet `kann_vertragsversand_ausfuehren()`.

## Wirksamkeit, Verhältnis und Belegung

Vor dem erfolgreichen Abschluss erzeugen Vorbereitung, Vorschau, `zur_signatur`, Teilunterschriften, PDF-Fehler oder Abbruch kein neues `pachtverhaeltnis` und keine Pachtvertragsfassung. `complete_prepared_pachtvertrag` ist die atomare Wirksamkeitsgrenze.

Beim ersten Abschluss entstehen genau ein Verhältnis und die dazugehörigen Beteiligungen. Wiederholte Finalisierung liefert den bestehenden Abschluss. Ersatzfassungen verwenden dasselbe Verhältnis.

`parzellen_belegung` ist die operative, bereits vor dem Vertragsabschluss bestehende Zuordnung, über die eine zulässige Parzelle ausgewählt wird. Sie ist nicht mit der rechts-/vertragsbezogenen Beteiligung in `pachtverhaeltnis_beteiligung` gleichzusetzen. Der Abschluss dupliziert die operative Belegung nicht; er erzeugt die maßgebliche Vertragsbeteiligung.

## Kosten

Die vorhandene Saison liefert `pacht_pro_qm`. Daraus berechnet die bestehende Logik die Jahrespacht und bei einem neuen Vertrag die monatsanteilige Pacht für das laufende Jahr. Eine separate saisonale Quelle für eine Nebenkostenvorauszahlung existiert derzeit nicht; deshalb wird in Phase 9 kein Betrag erfunden und keine neue Forderung persistiert. Nebenkosten bleiben in der bestehenden Jahresabrechnungsarchitektur. Eine Neufassung erzeugt weder neue Mitgliedschaft noch Aufnahmegebühr oder ein zweites Pachtverhältnis.

## Pflichtunterlagen und Papiernachweis

Für den Pachtvertrag sind exakt folgende Dokumenttypen erforderlich:

- `kleingartenordnung`
- `beitragsordnung`
- `arbeitsordnung`

Die Satzung wird nicht zusätzlich erzwungen. Ein gebundener digitaler Nachweis oder eine dokumentierte Papierübergabe erfüllt die jeweilige Pflicht. Fehlt beides für nur einen Pflichttyp, ist der Snapshot unvollständig und der Versand-/Abschlussweg wird nicht als vollständig behandelt.

## Versand

Das Finaldokument und die Pflichtunterlagen werden revisionsbezogen eingefroren. Vertragsparteien werden aus dem Vorgang beziehungsweise den Signatur-/Vertragsdaten übernommen. Der initiale Aufruf von `prepare_contract_dispatch` ist für denselben Vorgang und dieselbe Revision idempotent; ein ausdrücklich erneuter Versand referenziert einen erfolgreichen Vorgänger. Versandfehler ändern weder Vertragsnummer noch Pachtverhältnis, Vertragsfassung oder Maßgeblichkeit.

## Ersatzvertrag und Maßgeblichkeit

`Korrektur-/Ersatzvertrag erstellen` verwendet `prepare_pachtvertrag_ersatz` und niemals den Altkompatibilitätspfad `signExisting`. `has_previous_contract` und `previous_contract_date` beschreiben nur historischen Papierkontext und bilden keine technische Ersatzkette.

Bei A → B bleibt A maßgeblich, solange B vorbereitet, zur Signatur, teilweise signiert, fehlgeschlagen oder abgebrochen ist. Erst der erfolgreiche transaktionale Abschluss von B setzt B maßgeblich und A `nicht_mehr_massgeblich_am`. Für A → B → C referenziert B unmittelbar A und C unmittelbar B; die Kette wird nicht auf A abgeflacht. Alle Fassungen und Fachnummern bleiben erhalten.

Ein wiederholter Ersatzstart gibt den bereits aktiven, demselben Benutzer zugewiesenen Vorgang zurück. Nach Abbruch kann ein neuer Vorgang entstehen; eine zuvor reservierte 20xxx bleibt verbraucht und der Vorgänger bleibt maßgeblich.

## Resume und Retry

Die Persistenz in `kommunikationsvorgang`, Revision, Signaturpositionen, Finaldokument, Vertragsfassung und Versandinstanz deckt folgende Wiederaufnahmepunkte ab: begonnene Vertragsdaten, Vorschau/Prepared, `zur_signatur`, Teilunterschriften, vollständige Signaturen vor Finalisierung, abgeschlossene Finalisierung vor Versand, fehlgeschlagener Versand sowie begonnene oder teilweise signierte Ersatzverträge.

Idempotente Schlüssel und RPC-Prüfungen verhindern beim Retry eine zweite 20xxx, zusätzliche Signaturpositionen, ein zweites Final-PDF, eine zweite Vertragsfassung, ein zweites Verhältnis, doppelte Beteiligungen, einen zweiten initialen Versand und einen doppelten Maßgeblichkeitswechsel.

## Web und MAUI

Web bietet die Auswahl aller aktiven aufgenommenen Mitglieder als Pächter 2 an und wählt ein vorhandenes Nebenmitglied standardmäßig vor. MAUI bietet bei einer Neuanlage weiterhin bewusst das verknüpfte Nebenmitglied an; bei Wiederaufnahme lädt es nun die im Prepared-Vorgang gespeicherte Mitglieds-ID auch dann, wenn es sich um ein vollwertiges Hauptmitglied handelt. Es wurde keine künstliche UI-Parität geschaffen. Beide Clients verwenden dieselben Backendregeln, Nummern, Prepared-Zustände, Finalisierung und Ersatzkette.

## Automatisierte Absicherung

`npm run test:lease-contract-e2e` prüft:

- A: Neuvertrag mit einem Pächter, 20xxx-Retry, Pflichtsignaturen, genau ein Verhältnis/eine Beteiligung, Dokumentnachweis, Versand-Retry und anteilige Pacht
- B/C/D/E: zwei Pächter, Nebenmitglied, vollwertiges Mitglied und Abweisung eines Nichtmitglieds
- F/G: A → B und A → B → C mit unmittelbarer Kette und genau einem Verhältnis
- H/I: Abbruch mit verbrauchter Nummer, fortbestehende Maßgeblichkeit und idempotenter Ersatz-Retry
- digitale Pflichtunterlagen, Papierersatz und fehlender Nachweis
- den versionierten Serververtrag für Pächterstatus, Selbstduplikat-Sperre und benutzergebundenen Ersatz-Retry

Zusätzlich war der Web-Produktionsbuild erfolgreich. Die fokussierte .NET-Auswahl kompiliert Core und Infrastructure; 21 Tests laufen erfolgreich. 15 bekannte, blockfremde Tests bleiben durch eine versiegelte `DispatchProxy`-Testklasse beziehungsweise eine nicht verfügbare kanonische PDF-Testvorlage rot. Der MAUI-Vollbuild bleibt wegen 50 bekannten Fehlern in fachfremden Seiten/Altintegration rot; nach Korrektur der drei pachtvertragbezogenen API-Driftstellen meldet er keinen Fehler mehr in `PachtvertragFlowHelper.cs`. Gezieltes ESLint meldet in unveränderten Altzeilen von `LeaseContractFlow.tsx` eine bestehende Hook-Regelverletzung und drei bestehende Warnungen. Die vorhandenen ImageSharp-Advisories und Nullability-Warnungen sind ebenfalls nicht Teil dieser Phase.

## Migration und Betrieb

`20261028120000_phase9_pachtvertrag_e2e_haertung.sql` enthält die serverseitige Statusprüfung und das Ersatz-Retry-Hardening. Die Migration wurde nur versioniert, nicht ausgeführt. Es wurden keine Edge Functions deployt, keine Templates hochgeladen oder aktiviert, keine Produktionsdaten geändert und kein Release erzeugt.

**Phase 9 – E2E Pachtvertrag = 100 %.**
