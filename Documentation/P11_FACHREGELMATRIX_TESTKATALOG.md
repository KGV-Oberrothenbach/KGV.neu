# P11 – Fachregelmatrix und Testfallkatalog

Stand: 08.11.2026  
Geltung: fachlicher Soll-Zustand nach `P11_ARBEITSABLAUFPLAN.md`, Abschnitt 2. Dieses Dokument ist die verbindliche Referenz für alle folgenden CX-Aufträge.

## Lesart

- **Serververtrag** bezeichnet den später verbindlichen RPC-, RLS-, Trigger- oder Edge-Function-Vertrag. Die hier genannten Verträge werden mit AP-00 festgelegt, nicht implementiert.
- **Positiv**, **Negativ** und **Historie** sind verpflichtende, automatisierbare Testfälle. Ihre IDs werden in den Folgeaufträgen referenziert.
- Web und MAUI dürfen unterschiedliche Oberflächen haben, müssen aber denselben Serververtrag und dieselbe effektive Rechteentscheidung verwenden.

| Regel-ID | Fachregel und Verantwortlichkeit | Maßgeblicher Serververtrag | Web-/MAUI-Auswirkung | Folgepakete |
| --- | --- | --- | --- | --- |
| P11-R01 | Fachvorgänge prüfen ausschließlich effektive Rechte (Rollenbasis plus Gewährungen minus Entzüge); Rolle ist nur Vorlage. Verantwortlich: Rechteverwaltung. | Effektive-Rechte-Auflösung vor jedem Lese-, Schreib-, Freigabe- und Verwaltungsvorgang. | Nur erlaubte Ziele und Aktionen anzeigen; UI ersetzt nie die Serverprüfung. | AP-02A, AP-09 |
| P11-R02 | Rollen- und Rechteverwaltung ist ein eigenes Recht; Schutz geschützter Konten verhindert nur vollständige Selbstsperrung. Verantwortlich: Rechteverwaltung. | `CanManageRoles` und Sperrschutz. | Verwaltungszugang nur bei Fachrecht. | AP-02A |
| P11-R03 | Aktive Mitglieder dürfen nur eigene Anschrift, Telefon und private E-Mail ändern; E-Mail synchronisiert vorhandene Anmeldeadresse. Verantwortlich: Mitgliedsverwaltung. | Begrenzte Eigendaten-Operation mit Auth-Abgleich. | Eigenes Profil, kein generisches Stammdaten-Update. | AP-03 |
| P11-R04 | Mitgliedschaft endet nur durch Kündigung oder formellen Prozess. Verantwortlich: Vorstand. | Status-Fachoperation mit Begründung und Audit. | Kein automatisches Beenden anzeigen oder auslösen. | AP-03 |
| P11-R05 | Ehemalige Mitglieder erhalten eigene Unterlagenhistorie bis 31.12. des Folgejahres. Verantwortlich: Datenschutz. | Zeitgebundene Historienprojektion. | Historienansicht nur bis Fristende. | AP-02B, AP-03 |
| P11-R06 | Pacht setzt aktive Mitgliedschaft voraus; Ende der letzten Pacht setzt passiv, ohne App-Zugang oder Mitgliedschaft zu beenden. Passive führen keinen Pachtvertrag. Verantwortlich: Pachtverwaltung. | Transaktionaler Status-/Pachtvertrag. | Status und Pachtaktionen getrennt darstellen. | AP-03, AP-04 |
| P11-R07 | Nebenmitglieder enden nicht automatisch; beim Ausscheiden des Hauptpächters endet der alte Vertrag, Fortführung erfolgt als neuer Vertrag mit Hauptpächter. Verantwortlich: Pachtverwaltung. | Vertragsende und Neuvertrag, keine Übernahme. | Keine Vertragsübernahmeaktion anbieten. | AP-04 |
| P11-R08 | Neue Verträge schließen grundsätzlich nahtlos an; Nachsorge hat Enddatum, endet bei Nachpächter früher und verlangt Übergabeprotokoll spätestens zum Saisonende. Verantwortlich: Pachtverwaltung. | Nicht überlappende Belegung, Nachsorge- und Übergabeprozess. | Prozessstatus, Fristen und offene Übergabe sichtbar. | AP-04 |
| P11-R09 | Beendete Pacht-, Belegungs- und Vertragsdaten werden nur durch begründete, protokollierte Korrektur ergänzt. Verantwortlich: Fachverwaltung. | Unveränderlichkeit mit Korrekturereignis. | Keine stille Bearbeitung abgeschlossener Daten. | AP-04, AP-05 |
| P11-R10 | Ex-Pächter sehen nur eigene Daten und Unterlagen aus eigener Pachtzeit, höchstens bis Nachpächterübernahme. Verantwortlich: Datenschutz. | Pachtzeit- und Nachpächtergrenze. | Keine aktuellen Parzellendaten für Ex-Pächter. | AP-02B, AP-04 |
| P11-R11 | Laufende Änderungen an Fläche oder Anschlüssen erfordern vollständig unterschriebene Vertragsänderung. Verantwortlich: Pachtverwaltung. | Signierte Vertragsänderung vor Wirksamkeit. | Nicht direkt editierbar bei laufender Pacht. | AP-04, AP-05 |
| P11-R12 | Nur vollständige Mitgliedsanträge werden bearbeitet; Vereinsunterschriften gehören zum Antrag. Verantwortlich: Vorstand. | Vollständigkeits- und Signaturprüfung. | Unvollständige Anträge nicht zur Bearbeitung freigeben. | AP-03, AP-05 |
| P11-R13 | Bildsignaturen bleiben erhalten, ersetzen aber keine gesetzliche Schriftform; Pachtkündigung bleibt Originalprozess. Verantwortlich: Dokumentverwaltung. | Dokumenttyp- und Signaturgrenze. | Keine Software-Kündigung als wirksam ausgeben. | AP-05 |
| P11-R14 | Jede Ablesung sowie Ein-/Ausbau beim Wechsel verlangt ein Foto. Verantwortlich: Zählerverwaltung. | Atomarer Foto- und Ablese-/Wechselvertrag. | Ohne Nachweis keine Freigabe. | AP-02B, AP-06 |
| P11-R15 | Verbrauch wird bei Pächter-/Zählerwechsel zeitlich segmentiert; Leerstand und Vereinsgarten werden klassifiziert, nicht finanziell verteilt. Verantwortlich: Verbrauchsermittlung. | Revisionsfähige Verbrauchssegmente. | Segmentnachweise, keine Kostenverteilung. | AP-07 |
| P11-R16 | Pflichtstundensoll gilt nur für Hauptmitglieder. Anmeldung zu Einsätzen ist für alle Mitglieder möglich; Stunden entstehen manuell oder nach Prüfung, Saison folgt Arbeitsdatum. Verantwortlich: Arbeitsdienst. | Stunden-/Anmeldevertrag mit Datums-Saison. | Einsatz ist keine automatische Stundenbuchung. | AP-08 |
| P11-R17 | Wartungsverträge wirken für das Hauptmitglied über die gesamte betreffende Saison. Verantwortlich: Arbeitsdienst. | Saison-Snapshot der Wartungswirkung. | Jahreswirkung nachvollziehbar darstellen. | AP-08 |
| P11-R18 | Serienbrief und Dokumenttypenverwaltung bleiben Web-Verwaltung; JAE-XLSX wird zentral erzeugt und in beiden Clients bereitgestellt. Verantwortlich: Plattform. | Zentraler Exportvertrag. | Bewusste Plattformgrenzen sichtbar machen. | AP-07, AP-09 |

## Testfallkatalog

| Test-ID | Regel | Fall | Vorbedingung / Aktion | Erwartetes Ergebnis |
| --- | --- | --- | --- | --- |
| P11-T01-P | R01 | Positiv | `user` mit einzelner Zählergewährung erfasst Ablesung. | Server akzeptiert; Web und MAUI bieten erlaubten Einstieg. |
| P11-T01-N | R01 | Negativ | `admin` mit entzogenem Zählerrecht ruft denselben Serververtrag direkt auf. | Serverseitige Ablehnung. |
| P11-T01-H | R01 | Historie | Rollenwechsel nach individueller Gewährung/Entzug. | Individuelle Abweichung bleibt wirksam und nachvollziehbar. |
| P11-T02-P | R02 | Positiv | Berechtigter verwaltet Rollen eines anderen Kontos. | Änderung wird akzeptiert und protokolliert. |
| P11-T02-N | R02 | Negativ | Konto ohne `CanManageRoles` versucht Rechteänderung. | Serverseitige Ablehnung. |
| P11-T02-H | R02 | Historie | Geschütztes Konto verliert fast alle Rechte. | Vollständige Sperre wird verhindert; kein zusätzliches Fachrecht entsteht. |
| P11-T03-P | R03 | Positiv | Aktives Mitglied ändert eigene Telefon-/Adressdaten. | Nur erlaubte Felder ändern sich. |
| P11-T03-N | R03 | Negativ | Mitglied versucht Status oder Pachtbeziehung zu ändern. | Serverseitige Ablehnung. |
| P11-T03-H | R03 | Historie | Mitglied mit App-Zugang ändert E-Mail. | Auth-Anmeldeadresse folgt dem bestätigten Prozess. |
| P11-T04-P | R04/R05 | Positiv | Formeller Beendigungsprozess wird durchgeführt. | Mitgliedschaft endet nachvollziehbar; Historienfrist wird gesetzt. |
| P11-T04-N | R04 | Negativ | Hintergrundprozess versucht automatisches Mitgliedschaftsende. | Übergang wird abgewiesen. |
| P11-T04-H | R05 | Historie | Ehemaliges Mitglied öffnet am 31.12. Folgejahr eigene Unterlage. | Zugriff möglich; am Folgetag gesperrt. |
| P11-T05-P | R06/R07 | Positiv | Letzte Pacht endet, bisheriges Haupt-/Nebenverhältnis bleibt historisch. | Mitglied wird passiv; keine automatische Nebenmitgliedlöschung. |
| P11-T05-N | R06 | Negativ | Passives Mitglied soll neuen Pachtvertrag erhalten. | Server lehnt ab. |
| P11-T05-H | R07 | Historie | Hauptpächter scheidet aus, bisheriger Mitpächter führt fort. | Alter Vertrag endet; neuer Vertrag mit Hauptpächter entsteht. |
| P11-T06-P | R08 | Positiv | Nahtloser Pächterwechsel mit Nachsorge und Nachpächter. | Belegung überschneidet nicht; Nachsorge endet, Übergabeprotokoll entsteht. |
| P11-T06-N | R08 | Negativ | Parallelbelegung derselben Parzelle wird angelegt. | Server lehnt ab. |
| P11-T06-H | R08 | Historie | Kein Nachpächter bis Saisonende. | Nachsorge-Enddatum und fälliges Übergabeprotokoll bleiben nachweisbar. |
| P11-T07-P | R09/R11 | Positiv | Vollständig unterschriebene Vertragsänderung passt Anschluss an. | Änderung wird wirksam und protokolliert. |
| P11-T07-N | R09/R11 | Negativ | Abgeschlossene Pacht oder laufender Anschluss wird direkt geändert. | Server lehnt ab. |
| P11-T07-H | R09 | Historie | Begründete Korrektur zu abgeschlossenem Vertrag. | Original bleibt erhalten, Korrekturereignis zeigt Akteur/Zeit/Grund. |
| P11-T08-P | R10 | Positiv | Ex-Pächter öffnet eigene historische Unterlage vor Nachpächterübernahme. | Zugriff im erlaubten Zeitfenster. |
| P11-T08-N | R10 | Negativ | Ex-Pächter liest aktuelle Parzellendaten oder fremde Unterlage. | Serverseitige Ablehnung. |
| P11-T08-H | R10 | Historie | Nachpächter übernimmt Parzelle. | Zugriff des Ex-Pächters endet danach. |
| P11-T09-P | R12/R13 | Positiv | Vollständiger Mitgliedsantrag mit Vereinsunterschriften wird bearbeitet; Bildsignatur für zulässiges Dokument. | Bearbeitung bzw. Dokumentprozess möglich. |
| P11-T09-N | R12/R13 | Negativ | Unvollständiger Antrag oder bildsignierte Pachtkündigung wird als wirksam behandelt. | Prozess wird gesperrt bzw. als Originalprozess gekennzeichnet. |
| P11-T09-H | R13 | Historie | Signiertes Dokument wird später angezeigt. | Gespeicherte Signaturart und rechtliche Grenze bleiben sichtbar. |
| P11-T10-P | R14 | Positiv | Ablesung und Zählerwechsel mit erforderlichen Fotos. | Atomar gespeichert und freigabefähig. |
| P11-T10-N | R14 | Negativ | Ablesung oder Ein-/Ausbau ohne Foto. | Serverseitige Ablehnung. |
| P11-T10-H | R14 | Historie | Alter Zähler wird ausgebaut, neuer eingebaut. | Fotos bleiben an beiden historischen Vorgängen nachweisbar. |
| P11-T11-P | R15 | Positiv | Pächter- oder Zählerwechsel im Jahr. | Getrennte, nachvollziehbare Verbrauchssegmente. |
| P11-T11-N | R15 | Negativ | Finanzielle Umlage aus Verbrauchssegmenten auslösen. | Nicht verfügbar bzw. gesperrt. |
| P11-T11-H | R15 | Historie | Leerstand oder Vereinsgarten über mehrere Segmente. | Klassifikation bleibt revisionsfähig, ohne Kostenverteilung. |
| P11-T12-P | R16/R17 | Positiv | Hauptmitglied erhält manuell bestätigte Stunde für tatsächliches Arbeitsdatum; Wartungsvertrag im Saisonjahr. | Saison und volles Wartungsjahr korrekt berücksichtigt. |
| P11-T12-N | R16 | Negativ | Arbeitseinsatz soll automatisch Stunden erzeugen oder Nebenmitglied Pflichtsoll erhalten. | Keine Stundenbuchung bzw. kein Pflichtsoll. |
| P11-T12-H | R16/R17 | Historie | Nachbuchung über Jahresgrenze oder Vertrag endet im Jahr. | Ursprüngliches Arbeitsdatum und Saisonwirkung bleiben nachvollziehbar. |
| P11-T13-P | R18 | Positiv | Berechtigter ruft JAE-XLSX in Web und MAUI ab. | Beide Clients nutzen denselben Serververtrag. |
| P11-T13-N | R18 | Negativ | MAUI oder unberechtigtes Konto öffnet Serienbrief/Dokumenttypenverwaltung. | Kein Einstieg bzw. serverseitige Ablehnung. |
| P11-T13-H | R18 | Historie | Export wird nach mehreren Verbrauchssegmenten erneut aufgerufen. | Vollständige Segmentdarstellung über zentralen Vertrag. |

## Produktgrenze Finanzabrechnung

Kostenverteilung, Umlagen, Vorauszahlungen, Salden, Nachforderungen und Erstattungen liegen außerhalb des aktuellen Umfangs. Der finanzielle Jahresabschluss darf nicht produktiv verwendet werden, solange er Verbrauchskosten verteilen kann. AP-07 muss vorhandene Einstiege vor einer Freigabe sperren oder unmissverständlich als nicht verfügbar kennzeichnen.

Der aktuelle Dokumenten- und Quelltextbefund führt als prüfbedürftigen Einstieg die Navigation `jahresabschluss` in `KGV.Web/features/navigation/NavigationConfig.ts`. Diese Feststellung ist eine Kennzeichnung für AP-07/AP-09 und keine Änderung dieses Arbeitspakets.

## Abnahme von AP-00

AP-00 ist erfüllt, wenn jeder spätere CX-Auftrag mindestens eine Regel-ID und konkrete Test-IDs aus diesem Katalog nennt. Die Testfälle sind vor der jeweiligen Umsetzung als automatisierte Positiv-, Negativ-, Parallelitäts- und Historienprüfungen zu konkretisieren. Dieses Dokument führt keine Migration, keine Client- oder Serverimplementierung aus.
