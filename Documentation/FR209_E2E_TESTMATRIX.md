# FR2-09 – Authentifizierte HTTP-E2E-Testmatrix (SPÄTER AUSFÜHREN)

**Status:** reine Testplanung, kein einziger der hier aufgeführten Fälle gilt als ausgeführt oder erfolgreich. Diese Matrix wird in der gemeinsamen Abschlussphase aller FR2-Pakete mit den zwei per Windows-DPAPI vorhandenen Demo-Testkonten sowie ggf. zusätzlichen synthetischen Identitäten durchgeführt. ProduktivDB verboten. E-Mails ausschließlich an kontrollierte Testempfänger oder sicheren SMTP-Testtransport. Bei jedem Fall wirksame individuelle Entzüge und abweichende Objektbindung nachweisen.

| ID | Szenario | Erwartung | Unabhängiger Nachweis |
|---|---|---|---|
| 01 | Kein/ungültiger JWT: Vorgang, Versand-RPC, Versand-Edge | 401/403; kein Datensatz-/SMTP-Schreibzugriff | SQL-Zählung und Transportjournal |
| 02 | Nur `vorgang.lesen`, eigener zugewiesener Vorgang | lesen erlaubt | Datensatz-ID und Version stabil |
| 03 | Nur `vorgang.lesen`, fremder Vorgang | keine fremden Daten | negative REST-/RPC-Anfragen |
| 04 | `vorgang.lesen` ohne `vorgang.bearbeiten` | kein INSERT/UPDATE/Übernehmen/Löschen | vorher/nachher SQL |
| 05 | Nur `vorgang.bearbeiten`: eigener Vorgang | zulässige Bearbeitung | Revision und Status |
| 06 | Nur `vorgang.bearbeiten`: fremde ID | keine Mutation | SQL und SQLSTATE |
| 07 | `vereinsdaten.alle_lesen` ohne Bearbeitungs-/Versandrecht | global lesen, nicht schreiben/versenden | REST/RPC 403, keine SMTP-Ausgabe |
| 08 | Vorhandene Hauptrolle, Zusatzrolle, Vereinsfunktion, individuelle Gewährung | serverberechnetes effektives Fachrecht | Fachrechts-RPC vor Request |
| 09 | Individueller Entzug nach Ausstellung JWT | erneute Aktion verweigert | unterschiedliche JWT-Zeitpunkte, RPC und DB |
| 10 | Unzugeordneten Vorgang übernehmen | atomar genau ein Gewinner | getrennte Benutzer/Sessions, SQL-Zuordnung |
| 11 | Zeitgleiche Übernahme durch 2 Benutzer | einer erfolgreich, einer 55000 / Konflikt | zwei getrennte SQL-Sitzungen; Endzuordnung |
| 12 | Fremdzugeordneten Vorgang übernehmen oder freigeben | verweigert | SQLSTATE 42501/55000, unverändert |
| 13 | Bereits `erledigt`, `abgebrochen`, `verworfen`: nachträgliche Mutation | unveränderlich | alle auditrelevanten Spalten unverändert |
| 14 | Client fälscht `versendet_am`, `versendet_von`, `fehlertext`, `gedruckt_von` | 42501, kein Audit-Fake | SQL-Diff |
| 15 | Druckabschluss mit/ohne `druck.verwalten` | nur erlaubte eigene Druckaufträge | Status-/Zeitstempelvergleich |
| 16 | Fremder Mitglied-/Parzellenbezug bei Vorgangserstellung | verboten; auch mit gültigem allgemeinem Recht | Objekt-Scope-/RLS-/Triggernachweis |
| 17 | Nur `vertrag.vorbereiten` vs. nur `vertrag.versenden` | Vorbereitung und Versand unabhängig | Prepared-Workflow, Dispatch-RPC |
| 18 | Versandberechtigter Benutzer mit fremder `versandinstanz_id` | Claim/Versand verweigert | keine SMTP- und DB-Mutation |
| 19 | Reines Dokumentleserecht mit manipulierten Anhang-IDs | keine fremden Anhänge versenden | versandfertige, vom Server validierte Snapshot-Liste |
| 20 | Snapshot lesen mit `vorgang.lesen` ohne `dokument.lesen` | keine sensiblen Dateiverweise | RLS für Dokument-/Anlagensnapshot |
| 21 | Versandhistorie übergreifend nur `vereinsdaten.alle_lesen` | lesen, nicht senden | SQL-RLS, Schreibverweigerung |
| 22 | Direkte REST-Mutation an `versandinstanz` und Kindern | verboten, ebenso TRUNCATE | ACL und authentifizierter HTTP-/SQL-Test |
| 23 | Versand-Edge ohne/mit abgelaufenem oder widerrufenem Recht | verweigert vor SMTP | Testtransport und SQL |
| 24 | Club-Dokumentversand `vereinsunterlagen.verwalten` ohne passende Versandrechte | keine implizite Versandfreigabe | keine SMTP-Ausgabe |
| 25 | Club-Dokumentversand: fremdes Mitglied/fremde Fassung | verweigert | Objekt-/Empfänger-/Dateihashkontrolle |
| 26 | Vertrag: aktuelle vs. eingefrorene Vertragsparteien | korrekte unveränderliche Snapshots | SQL-Fassungen vor/nach Versand |
| 27 | Exakt identischer initialer Versand-Retry | keine neue Versandinstanz, keine Doppelmail | idempotency key, SMTP-Log |
| 28 | SMTP-Akzeptanz, danach DB-Schreibfehler | **kein blindes erneutes Senden** | transaktionaler Recovery-/Quarantänenachweis |
| 29 | >15 min Versand-Lock/Timeout mit laufendem Provider | konkurrierende Mailer dürfen keine Doppelmail senden | 2 echte Sessions, SMTP-Ledger |
| 30 | Teilfehler 2+ Empfänger | Erfolg und Fehler getrennt, nur fehlgeschlagene korrekt erneut | Status/Attempt/SMTP-Ledger |
| 31 | Erneutversand nach abgeschlossenem Versand | neuer kontrollierter Snapshot, alter unverändert | IDs, Hashes, Revisionsverknüpfungen |
| 32 | Snapshots nach Personen-/Email-/Dateiänderung | unverändert | SQL-Diff, Datei-SHA256 |
| 33 | Serienbrief nur `serienbrief.erstellen`, kein `vorgang.bearbeiten` | keine implizite Vorgangs-Schreib-/Versandberechtigung | Web/DB/Edge getrennt |
| 34 | Serienbrief mit mehrdeutiger Empfängerauflösung | Konflikt, nicht auswählbar | Empfängerliste und Sendjournal |
| 35 | Serienbrief Vorlagen lesen/bearbeiten mit individuellen Entzügen | `vorlage.verwalten` unabhängig | Vorlagen-RLS, Editor-UI |
| 36 | MA+PV historischer Erstvertrag mit mehreren Parzellen | gemeinsamer Eintrittsvorgang und korrekte Dokument-Provenienz | Vertrags-/Mitglieds-/Parzellen-IDs, PDF-Hash |
| 37 | PV für vorhandenes Mitglied mit weiterer Parzelle | nur historischer PV, kein zweiter MA | Mitglieder-/Vertragszählung |
| 38 | Vertragssignatur/Prepared/Recovery mit Unterbrechung | bestehender Vertrag/Snapshot/Unterschrift bleibt korrekt | PDF, Signatur-SHA256, letzte Revision |
| 39 | Parallel Archivierung und Versand desselben Dokuments | Versand nur konsistent gebundene Version | SQL-Locks + Datei-SHA256 |
| 40 | MAUI und Web identischer Vorgang | gleiche Berechtigungen und Status | 2 reale UI-E2E-Läufe |

**Nachweisformat pro Test:** UTC, Demo-Projekt-ID, geprüfter Git-Commit, Migrationsliste, Akteur-Pseudonym, Fachrechte/Entzüge, Objekt-IDs und Beziehungen, HTTP-Methode/URL ohne Geheimnisse, Soll-/Ist-Code, SQLSTATE, SQL-Vorher/Nachher, SMTP-Testjournal, Dateityp/SHA-256, Persistenz, Aufbewahrungs-/Bereinigungsentscheidung und Ergebnis.

**Sperre:** Keine behauptete Produktionsreife, solange die echten positiven und negativen HTTP-E2E- sowie SMTP-Konkurrenztests nicht erfolgreich durchgeführt sind.
