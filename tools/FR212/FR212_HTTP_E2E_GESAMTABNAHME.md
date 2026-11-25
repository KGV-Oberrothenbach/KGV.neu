# FR2-01 bis FR2-12: Gemeinsame authentifizierte HTTP-/UI-E2E-Abnahmematrix

**Stand:** Planungsdokument, keine ausgeführten Live-Tests. Nach FR2-12 technisch fertigstellen, danach zentral durchführen. Ein 401/403-Smoke, Contract-Test, Build oder erfolgreiche Migration ersetzt **keinen** authentifizierten positiven Fachablauf mit unabhängiger Persistenzkontrolle.

## Synthetische Testidentitäten

- **A:** geschütztes Admin-Konto (nur zur Prüfung, nicht verändern).
- **B:** separates berechtigtes, nicht geschütztes Administrations-Testkonto; Benutzerverwaltung und Rechteverwaltung nach Fachkatalog.
- **C:** Testmitglied ohne Managementrechte mit eigenem Mitglieds-/Parzellenbezug.
- **D:** Testmitglied mit *gezielt gewährtem* Fachrecht und definiertem Objektscope.
- **E:** Benutzer mit Haupt-/Zusatzrolle und gezielt individuellem **DENY**.
- **F:** Berechtigter Fachbearbeiter ohne `berechtigungen.verwalten`, jedoch mit `benutzer.verwalten`, soweit die Admin-Zusatzrollenanforderung dies zulässt (ggf. über Test-Override **nicht** künstlich an Systemgrant-Regeln vorbei).
- **G:** Benutzer ohne gültige Mitgliedsverknüpfung beziehungsweise ohne V2-Hauptrolle (lokales/isoliertes Fixture).
- **H:** zweites zeitgleich angemeldetes Fachbearbeiterkonto für Konkurrenztests.

Alle Identitäten in getrennten echten Sessions mit kurzlebigen JWTs aus den vorhandenen lokal DPAPI-geschützten Demo-Testdaten verwenden. Rollenzuordnungen und individuelle Entzüge vor und nach jeder Prüfung unabhängig kontrollieren. Vorher Testobjekte und IDs dokumentieren. Keine Passwörter/JWTs/SMTP-Secrets protokollieren.

## Fachliche Matrix

| Bereich | Authentifizierter positiver Sollfall | Negativer Test / Umgehung | Persistenz-/Folgekontrolle |
|---|---|---|---|
| **FR2-01 Mitglieder** | D mit `mitglied.lesen/bearbeiten` liest/bearbeitet erlaubtes Testmitglied; C eigene Daten | E mit DENY, C fremdes Mitglied, Direkt-REST gegen fremden Datensatz | Mitgliedszeile, erwartete Änderungen, keine Fremddaten |
| **FR2-02 Mitgliedschaft** | B entscheidet synthetische Mitgliedsaufnahme entsprechend Prozessberechtigung | Fehlendes `mitgliedschaft.entscheiden`, fremde Funktion, ungültiger Status/Revision | Mitgliedsantrag, Entscheidung, Audit, nachgelagerte Schritte |
| **FR2-03 Berechtigungsgrundlage** | B Haupt-/Zusatzrolle und Override, 55 effektive Fachrechte | D ohne Adminrolle, E mit individuellem DENY, manipuliertes JWT/user_metadata | Role/Override/Audit, exakte Differenz 55er-Snapshot |
| **FR2-04 Parzellen und Pacht** | Berechtigter bearbeitet synthetische Belegung und Pachtvertrag | C fremde Parzelle, DENY für `belegung.verwalten`, veraltete Revision | Belegung, Vertragsfassung, Datumsbereiche, keine Doppelpacht |
| **Historische Verträge** | Berechtigter erfasst MA+PV-Erstvertrag bzw. PV für existierendes Mitglied; Mehrparzellen | Falscher Vertragstyp, fremdes Mitglied, doppelte 80xxx-Nummer, fehlendes Recht | Original-PDF, Stempel, Vertragsnummer, Mitgliedsnummer, Dateihash, Beziehungen |
| **FR2-05 Zähler & Ablesung** | D legt berechtigte Ablesung an, B prüft/freigibt; legitimer Fotoupload | C fremder Zähler, falsche Saison, DENY, direkter Storage-Upload | Zähler-Historie, Prüfstatus, Bild-Storage-SHA256 und Zuordnung |
| **FR2-06 Finanzen** | Berechtigter erstellt Rechnung/Kostenart, bereitet und finalisiert JAE | C ohne Recht, `jahresabschluss.korrigieren` getrennt, erneut finalisieren, unbefugter REST-Schreibversuch | Rechnungs-/Buchungs-/Jahresabschlussdatensätze, keine Doppelbuchung |
| **FR2-07 Arbeitsstunden** | Einreichung + qualifizierte Freigabe und Arbeitseinsatzmeldung | E `arbeitsstunden.freigeben` entzogen, fremde Stunden, Rollenbit-Manipulation | Arbeitsstunden/Protokoll, Pflichtstundensaldo, Teilnehmerzuordnung |
| **FR2-08 Dokumente** | Fachberechtigter dokumentiert, archiviert nur mit `dokument.archivieren` | `dokument.lesen` ohne Archivrecht, fremder Storage-Pfad, manipulierte Metadaten | Fassung, Provenance, Storage-Objekt, SHA256, Archivflag |
| **FR2-09 Vorgänge/Kommunikation** | Vorbereitete Verträge freigeben, versenden, drucken, Status prüfen | `vorgang.lesen` ohne `vertrag.versenden`, ungültige Revision, zweiter Versand | Vorgangsprotokoll, Empfänger, Versandstatus, Mail-/Druckauftrag |
| **FR2-10 Vereinsverwaltung** | Berechtigter Saison, Vereinsfunktion, Vereinsunterlagen ändert | Nur globales Leserecht, DENY, falscher Verein/Saison | Versionsstand/Vereinskonfiguration, Historie, Audit |
| **FR2-11 Auswertung/Export** | `auswertung.lesen` zeigt erlaubten Bestand; `export.ausfuehren` generiert Datei | `vereinsdaten.alle_lesen` **ohne** Exportrecht, fehlender Objektscope, fremde Datensätze | Exportjobs, Dateiinhalte und SHA256, keine Extrazeilen |
| **FR2-12 Benutzer lesen** | `benutzer.lesen` zeigt Mitglied-/Kontostatus | Ohne Recht, gefälschte Identität, verwaiste Zuordnung, direktes RPC | Entspricht `mitglied` / `app_user` / `auth.users`, keine Enumeration |
| **FR2-12 Benutzer verwalten** | B lädt synthetisches Mitglied via Einladung/OTP und löscht ungeschütztes Testkonto | `berechtigungen.verwalten` ohne Benutzerrecht, Self-Delete, geschütztes Konto, Fremdverknüpfung | Auth-User, `app_user`, Hauptrolle, Mitgliedsbezug, Löschreservierung und Mailstatus |
| **FR2-12 Rechteverwaltung** | B ändert V2-Hauptrolle/Override mit Begründung, liest Vorschau | Benutzerrecht allein, selbstgewählte Admin-Zusatzrolle, letzte geschützte Adminrolle, DENY | Vorher-/Nachher-Snapshot, Audit, konkurrierende Schreibversuche |
| **FR2-12 Globales Leserecht** | Berechtigtes A liest den fachlich definierten Vereinsbestand | Unbefugtes INSERT/UPDATE/DELETE, kein Export/Versand/Archiv/Benutzerrecht | Keine geänderte Zeile; SELECT nur gemäß Scope |

## Vertrags-, Auth- und Integrationsquerschnitt

| Ablauf | Positiver Nachweis | Negativ-/Wiederholungsnachweis |
|---|---|---|
| Mitgliedsantrag und Erstlogin | Signierte Form, auth-Mitglied-Verknüpfung, aktive V2-Mitgliedsrolle | Doppelte Einladung, unbekannte/mehrdeutige Mailadresse, OTP-Fehler, Rate Limits |
| Pacht- und Wartungsverträge | Vorbereiten → Signatur → Abschluss → Archiv/Versand, Provenance | Falsche Revision, konkurrierender Abschluss, Wiederholung unmittelbar nach Teilschritt |
| Vereinsunterzeichner | Rollengerechte Zeichnung und Vertretungsbefugnis zum Datum | Signatur ohne Vertretung, falscher Unterzeichner, verlorene Berechtigung |
| Historische Kennzeichnung | Nummer 80xxx, gestempelte PDFs, historischer Vertragsbezug | Doppelte Nummer/Datei, falsche Mitglieder-/Parzellenverknüpfung |
| PDF / Storage / Google Drive | Authentifizierter Download, korrekter MIME-Type, Hash und vollständige Fassung | Fremder Bucket/Ordner, inkonsistente Metadaten, Storage Upsert ohne Berechtigung |
| E-Mail-Versand | Einmaliger Versand, belegbarer Empfänger, revisionsgebundener Vorgang | Falsches Versandrecht, SMTP-Teilausfall, Retry ohne zweiten Versand |
| Sitzungsänderungen | Rechteentzug in aktiver Session wirkt beim nächsten geschützten RPC/REST-Schreibzugriff | Altes JWT, User-Metadata-Manipulation, Userdeaktivierung, gelöschter Auth-Account |
| Sicherheitsgrenzen | Direktaufruf jeder RPC/REST/Edge mit echtem Benutzer-JWT | Anon, unzulässige service_role-Verwendung, 5xx statt erwarteter 403, fremder Scope |
| Rennen / Idempotenz | Zwei berechtigte zeitgleiche Vorgänge ergeben definierte Konsistenz | Doppelter POST, Timeout nach COMMIT, Null-Zeilen-UPDATE, falscher SQLSTATE-40001-Retry |
| UI Web / MAUI | Benutzer navigiert bis zur realen fachlichen Aktion, Ergebnis wird nachgeladen | Verstecktes Menü direkt aufgerufen, UI-Cache stale, fehlender Recht-Refresh |

## Mindestkriterien je Testfall

1. **Voraussetzung:** gültiger Demo-User, wirksame Fachrechte und konkreter Objektscope unabhängig aus der DB bestätigt.
2. **Negative Varianten zuerst:** fehlendes Recht, individueller Entzug, fremder Scope und veraltete Revision. Prüfe Methode, Ziel, Soll- und Ist-Status. Keine 5xx als erwartete Ablehnung akzeptieren.
3. **Positiv:** echte erlaubte HTTP-Schreiboperation mit echtem JWT, verwertbare Response-ID, betroffene Zeilen > 0 (oder fachlich korrekter no-op bei dokumentierter Idempotenz).
4. **Unabhängig:** DB-Zeilen, Audit, Signaturrevisionen, Storage-/Google-Drive-Metadaten, PDF-Hash und Mail-/Druckaufträge aus separater Lesekette prüfen.
5. **Wiederholung und Konkurrenz:** gleicher Request darf keinen duplizierten Zustand erzeugen; parallele Anfragen korrekt sperren oder im gültigen Endzustand schließen.
6. **Aufbewahrung:** sinnvolle synthetische Testobjekte als Demo-Bestand behalten; flüchtige Rollen-Overrides und Sicherheitszwischenzustände wiederherstellen und unabhängigen Nachweis dokumentieren.
7. **Protokoll:** Testergebnis, Branch-SHA, Migration, Edge-Version, Identität **nur als Testkennung**, Objekt-ID, Soll/Ist, Persistenz, Bereinigungsstatus; niemals Schlüssel/Token.

## Abnahmeentscheidung

Für jede Zeile `offen`, `Contract-Test bestanden`, `SQL/RLS geprüft`, `authentifiziert negativ+positiv`, `Persistenz unabhängig bestätigt`, `UI-E2E`, `vollständig abgenommen` getrennt erfassen. Gesamtabnahme nur, wenn alle kritischen Matrixfälle auf der tatsächlich deployten **Demo** grünen Nachweis haben. Produktivfreigabe bleibt gesondert.
