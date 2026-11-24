# FR2-10 – Authentifizierte HTTP-E2E-Testmatrix

**Status: geplant, nicht ausgeführt.** Die HTTP-E2E-Gesamtabnahme wird entsprechend der ausdrücklichen Entscheidung erst nach FR2-11 durchgeführt. Ausschließlich DemoDB mit gesicherten synthetischen Testidentitäten und unabhängiger Persistenzkontrolle. Die Matrix selbst ist kein Nachweis.

| ID | Fachbereich / Fall | Erwartung | Unabhängige Kontrolle |
|---|---|---|---|
| 10-01 | Kein/ungültiges JWT gegen Termine/Bekanntmachungen/Verwaltung | Schreibaufrufe verweigert | HTTP + DB-Diff |
| 10-02 | Mitglied liest aktiven sichtbaren Termin | erlaubt, keine Verwaltung nötig | REST/View + lokale Zeit |
| 10-03 | Mitglied liest nicht sichtbaren, deaktivierten oder abgelaufenen Termin | keine Daten | View + RLS |
| 10-04 | Termin bearbeiten mit nur `bekanntmachung.verwalten` | verweigert | effektive 55 Rechte + DB |
| 10-05 | Termin verwalten mit `termin.verwalten` | zulässiger INSERT, UPDATE und Deaktivierung | Datensatz, Revision, RLS |
| 10-06 | Termin-Fachrecht individuell entzogen | sofortige Ablehnung trotz Haupt-/Zusatzrolle | zweite JWT-Session, SQL |
| 10-07 | Termin-DELETE/TRUNCATE/TRIGGER/REFERENCES als authentifiziert | verweigert | ACL und SQLSTATE |
| 10-08 | Termin-Grenzwerte und Aktiv-Status | keine ungültigen Sichtbarkeitsdaten | Datenbank und Homepage |
| 10-09 | Mitglied liest aktive veröffentlichte Bekanntmachung | erlaubt, HTML bereinigt | API + DOMPurify/Core-Sanitizer |
| 10-10 | Mitglied liest Entwurf/nicht sichtbare Bekanntmachung | verweigert | RLS + View |
| 10-11 | Bekanntmachung mit nur `termin.verwalten` bearbeiten | verweigert | SQL/HTTP |
| 10-12 | Bekanntmachung mit gültigem `bekanntmachung.verwalten` speichern/archivieren/reaktivieren | erlaubt, kein Harddelete | Originaldaten + Status |
| 10-13 | `javascript:`, `data:`, Eventhandler in HTML-Anzeige | keine Script-Ausführung | echter Web-/MAUI-UI-Test |
| 10-14 | Vereinsfunktionen ohne `vereinsfunktion.verwalten` editieren | verweigert | RLS/ACL |
| 10-15 | Vereinsfunktion mit individuellem Grant ohne Adminrolle verwalten | erlaubt | Serverantwort + Schreibpersistenz |
| 10-16 | Historische Funktionsbesetzung physisch löschen | verweigert | DELETE/Trigger und Historie |
| 10-17 | Besetzung beenden und Nachfolge anlegen | Historie unverändert, neue Besetzung gültig | Amtszeit-/Relationen-SQL |
| 10-18 | Zeitgleiche Einzelbesetzung / Vertretung | genau eine zulässige Entscheidung | parallele Verbindungen, Intervall-Diff |
| 10-19 | Vereinsfunktion ohne expliziten Signatur-Verwendungszweck | kein Unterzeichnerrecht | Signaturstart + RPC |
| 10-20 | Funktion nach Vertragsunterzeichnung umbenennen | historischer Signatur-Snapshot bleibt bytegenau gleich | DB-Snapshot + PDF SHA-256 |
| 10-21 | Nur `saison.verwalten` (kein `verein.konfiguration`) | Saison bearbeitbar, Vereinskonfiguration nicht | Web/MAUI-Menü + REST |
| 10-22 | Nur `verein.konfiguration` (kein `saison.verwalten`) | Vereinskonfiguration bearbeitbar, Saison nicht | Web/MAUI-Menü + REST |
| 10-23 | Nur `vereinsdaten.alle_lesen` | Verein lesbar, kein Schreiben | REST RLS / Audit |
| 10-24 | Individueller Entzug von `verein.konfiguration` | kein Schreiben trotz alter Adminmaske | HTTP + DB |
| 10-25 | Aktive Vereinskonfiguration sichern / erneutes Senden derselben Revision | keine Duplikate/unbefugtes Update | DB + Versionsspur |
| 10-26 | Benutzer ohne Fachrecht versucht Saisonänderung | verweigert | ACL/RLS + vorher/nachher |
| 10-27 | Benutzer mit Saisonrecht ändert vergangenes Jahr | verweigert | Trigger und unveränderte Finanzwerte |
| 10-28 | Benutzer mit Saisonrecht ändert Saison mit berechnetem/abgeschlossenem JA | verweigert | JA/Saison Transaktion |
| 10-29 | Parallel Saisonänderung und Jahresabschluss | keine Inkonsistenz oder historische Überarbeitung | zwei DB-Sessions, Locks, JA-Persistenz |
| 10-30 | Neuer Saisonvorschlag, Retry und identische Wiederholung | keine Duplikate | eindeutige Jahr-/ID-Bindung |
| 10-31 | Direkter REST-DELETE/TRUNCATE für `saison`, `vereinskonfiguration`, Amtszeittabellen | verweigert | Privilegien, Trigger, Historie |
| 10-32 | Rückwirkung auf FR2-06-Finanzsperren | unverändert | Abschluss-/Korrekturlauf |
| 10-33 | MA+PV historisch mit mehreren Parzellen | zusammenhängender Vorgang bleibt korrekt | Mitglied/Vertrag/Parzellen/Dokument |
| 10-34 | PV historisch bei vorhandenem Mitglied | kein weiterer Mitgliedsantrag | Vorgang, Mitgliedszählung |
| 10-35 | Prepared MA/PV/WV mit Vereinsunterzeichnern | bestehende Signatursnapshots unverändert | PDF/SHA-256, Zeitbezüge |
| 10-36 | Zusätzliche Fachrechte/Funktionen mit individuellen Grants und Entzügen | keine implizite Querberechtigung | 55er-Snapshot und RLS |
| 10-37 | UI Web und MAUI alle fünf Rechte (je ein Recht, keine Rechte, Kombinationen) | dieselben Navigations- und Editiergrenzen | reale UI-Interaktionen |
| 10-38 | Dokumentenanhang/Bekanntmachungsdokument fremder Mitglieder | kein fremder Zugriff | RLS/Storage/Persistenz |

**Pflichtfelder je Ergebnis:** UTC-Zeit, Git-SHA, Demo-Projekt-ID, Migrationen, pseudonymisierter Akteur, effektive Fachrechte und Entzüge, Objekt-IDs, HTTP-Methode/Status, SQLSTATE, vorher/nachher-Datenfingerabdruck, Persistenz/Datei-SHA-256, Retry-/Parallelitätsvariante, Aufbewahrungsstatus. Keine Passwörter, JWTs oder Schlüssel protokollieren.
