# Persistente Einzelunterschriften für vorbereitete Verträge

Stand: 21.10.2026 · Phase B. Dieses Dokument definiert das Zielmodell für Mitgliedsantrag (MA), Pachtvertrag (PV) und Wartungsvertrag (WV). Es ist keine Migration und ändert weder bestehende PDFs noch den laufenden Workflow.

## B1 – Zielbild

`kommunikationsvorgang` bleibt der alleinige übergeordnete Prepared-Contract-Vorgang. Jede physische Unterschrift wird als eigener, unveränderlicher Datensatz für die beim Signaturstart eingefrorene Revision geführt.

```text
kommunikationsvorgang (id, signatur_revision)
    |
    +-- prepared_contract_signatur: mitglied.antrag
    +-- prepared_contract_signatur: mitglied.datenschutz
    +-- prepared_contract_signatur: ... optionale private Positionen
    +-- prepared_contract_signatur: vorstand_1
    +-- prepared_contract_signatur: vorstand_2
```

`sort_order` steuert ausschließlich Anzeige und Auswahl von „Speichern und Weiter“. Es ist keine Abhängigkeit: Jede noch offene Position darf nach Start der Signatur unabhängig von anderen Positionen unterschrieben werden.

Die Umstellung ersetzt weder `kommunikationsvorgang` für MA/PV noch `wartungsvertrag_dokument_workflow` für WV.

## B2 – Signaturpositionen je Dokumenttyp

| Dokument | Schlüssel | Pflicht | Personentyp | Sortierung | Besonderheit |
| --- | --- | --- | --- | ---: | --- |
| MA | `mitglied.antrag` | ja | Mitglied | 10 | Eigene Unterschrift für den Antrag. |
| MA | `mitglied.datenschutz` | ja | Mitglied | 20 | Eigene, nicht wiederverwendbare Datenschutzunterschrift. |
| MA | `gesetzliche_vertretung.antrag` | bei Minderjährigkeit | gesetzliche Vertretung | 30 | Entspricht der bestehenden PlanBuilder-Logik. |
| MA | `gesetzliche_vertretung.datenschutz` | bei Minderjährigkeit | gesetzliche Vertretung | 40 | Nicht mit der Antragssignatur zusammenfassen. |
| MA | `nebenmitglied.antrag` | bei vorgesehener zweiter Partei | Nebenmitglied | 30 | Alternative zur gesetzlichen Vertretung, gemäß bestehendem PlanBuilder. |
| MA | `nebenmitglied.datenschutz` | bei vorgesehener zweiter Partei | Nebenmitglied | 40 | Eigenständige Datenschutzunterschrift. |
| MA | `vorstand_1` | ja | Vereinsunterzeichner | 90 | Auswahl aus fachlich berechtigten Personen. |
| MA | `vorstand_2` | ja | Vereinsunterzeichner | 100 | Andere natürliche Person als `vorstand_1`. |
| PV | `paechter_1` | ja | Mitglied/Pächter | 10 | Bestehender erster Pächter. |
| PV | `gesetzliche_vertretung` | bei Minderjährigkeit | gesetzliche Vertretung | 20 | Der bisherige „zweite Partei“-Fall. |
| PV | `paechter_2` | bei einbezogenem Nebenmitglied | Nebenmitglied/Pächter | 20 | Alternative zur gesetzlichen Vertretung. |
| PV | `vorstand_1` | ja | Vereinsunterzeichner | 90 | Ersetzt die bisher einzige Board-Signatur. |
| PV | `vorstand_2` | ja | Vereinsunterzeichner | 100 | Eigenständige zweite Board-Signatur. |
| WV | `mitglied` | ja | Hauptmitglied | 10 | Aus der vorhandenen Zuordnung `wartungsvertrag_zuordnungen.hauptmitglied_id`; Nebenmitglieder werden dort bereits auf das Hauptmitglied aufgelöst. |
| WV | `vorstand_1` | ja | Vereinsunterzeichner | 90 | Fachlich wie MA/PV. |
| WV | `vorstand_2` | ja | Vereinsunterzeichner | 100 | Fachlich wie MA/PV. |

Für WV existiert noch keine separate private Signatur-PlanBuilder-Logik. Die einzige heute fachlich festgelegte private Vertragspartei ist das Hauptmitglied der Zuordnung. Weitere WV-Privatpositionen dürfen erst ergänzt werden, wenn eine fachliche WV-Regel und Datenquelle existiert.

## B3 – Vorgeschlagenes Persistenzmodell

### Tabelle `prepared_contract_signatur`

| Spalte | Typ | Regel/Zweck |
| --- | --- | --- |
| `id` | `bigint generated ... identity` | Primärschlüssel. |
| `kommunikationsvorgang_id` | `bigint not null` | FK auf `kommunikationsvorgang`, `on delete restrict`. |
| `signatur_revision` | `integer not null` | Muss der eingefrorenen Revision des Vorgangs entsprechen. |
| `signatur_schluessel` | `text not null` | Fachlicher stabiler Schlüssel aus B2. |
| `sort_order` | `smallint not null` | Nur Darstellung. |
| `pflichtig` | `boolean not null` | Eingefrorene Pflichtentscheidung des konkreten Vorgangs. |
| `person_typ` | `text not null` | Beispielsweise `mitglied`, `gesetzlicher_vertreter`, `nebenmitglied`, `vereinsunterzeichner`. |
| `status` | `text not null default 'offen'` | Ausschließlich `offen` oder `unterschrieben`. |
| `mitglied_id` | `bigint null` | Private Person bzw. tatsächlicher Vereinsunterzeichner, soweit Mitglied. |
| `person_name_snapshot` | `text not null` | Anzeigename zum Zeitpunkt der Einrichtung bzw. Bestätigung. |
| `erfasst_von_user_id` | `uuid null` | Authentifizierter erfassender Benutzer. |
| `unterschrieben_am` | `timestamptz null` | Zeitpunkt der erfolgreichen Speicherung. |
| `signatur_storage_bucket` | `text null` | Privater Storage-Bucket; nur bei `unterschrieben`. |
| `signatur_storage_path` | `text null` | Unveränderlicher Objektpfad; nur bei `unterschrieben`. |
| `signatur_sha256` | `text null` | Integritätsprüfung der gespeicherten PNG-Datei. |
| `vereinsfunktion_id` | `bigint null` | Nur Vereinsunterzeichner: fachliche Funktion. |
| `vereinsfunktion_bezeichnung_snapshot` | `text null` | Bezeichnung der Funktion bei Auswahl. |
| `vereinsfunktion_besetzung_id` | `bigint null` | Eigene Besetzung des tatsächlichen Unterzeichners. |
| `berechtigungsart` | `text null` | `direkt` oder `vertretung`; nur Vereinsunterzeichner. |
| `vertretung_id` | `bigint null` | Gültige `vereinsfunktion_vertretung`, falls benutzt. |
| `vertretene_besetzung_id` | `bigint null` | Besetzung, die vertreten wird; nie als eigene Besetzung des Unterzeichners ausgeben. |
| `erstellt_am`, `bearbeitet_am` | `timestamptz not null` | Audit-Zeitpunkte. |

Erforderliche Constraints/Trigger für Phase C:

- `unique (kommunikationsvorgang_id, signatur_revision, signatur_schluessel)`;
- `status = 'offen'` bedeutet: keine Bildreferenz, kein Hash und kein `unterschrieben_am`; `status = 'unterschrieben'` bedeutet: alle drei sind vorhanden;
- Vereins-Snapshot-Felder sind nur für `person_typ = 'vereinsunterzeichner'` zulässig und dort vollständig;
- ein Trigger validiert beim Insert, dass `signatur_revision = kommunikationsvorgang.signatur_revision`, der Vorgang `zur_signatur` ist und der Dokumenttyp zum Positionsplan passt;
- ein Trigger validiert beim Übergang nach `unterschrieben` die fachliche Berechtigung gegen die Auswahl-/Auflösungs-RPC zum Stichtag `unterschrieben_am::date`;
- ein Trigger verhindert Update oder Delete einer Zeile mit `status = 'unterschrieben'`. Eine spätere Korrektur braucht einen expliziten, separaten Fachvorgang; sie darf kein stilles Überschreiben sein.

Die zwei Vorstandssignaturen sind unterschiedliche Datensätze. Ein weiterer Trigger verhindert für dieselbe Vorgangs-ID und Signaturrevision, dass `vorstand_1` und `vorstand_2` dieselbe `mitglied_id` erhalten. Das setzt das fachliche Ziel „zwei verschiedene natürliche Personen“ serverseitig durch.

## B4 – Vereinsunterzeichner

### Bestehende Auswahlfelder – Befund

Es gibt noch kein wiederverwendbares Auswahlfeld für Vertragsunterzeichner auf Basis der Vereinsfunktionen:

- PV hat in `KGV.Web/features/contracts/LeaseContractFlow.tsx` ein freies Textfeld **„Vereinsunterzeichner“**. Es wird als `clubSignatory: string` im Draft gespeichert und über `ContractGenerationClient` lediglich als Anzeigename an das Feld `vereins_unterzeichner` des PDFs weitergereicht. Es speichert keine `mitglied_id`, keine Besetzungs-ID und keine Berechtigungsgrundlage.
- Das Web-Parzellenprotokoll in `KGV.Web/app/parcel-workspaces.tsx` hat eine Auswahl **„Vorstand 2“**. Sie speichert `vorstand2_mitglied_id` als `mitglied_id`, filtert die Auswahl aber mit `app_user.role in ('admin','vorstand')`. Sie ist ein anderer Dokumenttyp und fachlich für Vertragsunterzeichnung nicht verwendbar, kann jedoch später als UI-Muster dienen.
- MA sowie die MAUI-Vertragsabläufe haben keine entsprechende Vereinsunterzeichner-Auswahl.

Damit ist keine vorhandene Auswahlmechanik fachlich sinnvoll direkt weiterzuverwenden. Phase D soll das PV-Freitextfeld durch eine gemeinsame Auswahlkomponente für beide Vorstandpositionen ersetzen und nicht eine weitere parallele Auswahl schaffen.

### Berechtigung und Vertretung

Technische Rollen erlauben allenfalls die Bedienung der Oberfläche, nie die Vertragsunterschrift. Die Auswahlquelle ist eine neue, gezielte RPC, etwa `vertragsunterzeichner_fuer_stichtag(p_stichtag date)`. Die bestehende `vereinsfunktion_besetzungen_fuer_verwendungszweck` bleibt unverändert kompatibel, weil sie heute keine Vertretungen auflöst.

Die neue RPC liefert ausschließlich für `verwendungszweck = 'vertragsunterzeichnung'` pro auswählbarer natürlicher Person mindestens:

- `mitglied_id`, Namen-Snapshotquelle und eigene `vereinsfunktion_besetzung_id`;
- `vereinsfunktion_id`, Schlüssel und Bezeichnung;
- `berechtigungsart = direkt | vertretung`;
- bei Vertretung: `vertretung_id` und `vertretene_besetzung_id`;
- gültige Zeiträume und stabile Sortierung.

Direkt Berechtigte werden aus aktiver Funktion, Verwendung und Besetzung ermittelt. Vertretende Personen werden nur im gültigen Vertretungszeitraum angeboten und mit ihrer **eigenen** Besetzung plus der vertretenen Besetzung zurückgegeben. Die RPC muss gegebenenfalls mehrfach vorhandene Berechtigungsgründe derselben Person deterministisch ausgeben oder als getrennte Auswahloptionen ausweisen; die gespeicherte Auswahl enthält immer den konkreten Grund.

## B5 – Signaturdaten und Sicherheitsmodell

Empfohlen ist **Variante B: geschützter Supabase Storage mit DB-Referenz und Metadaten**.

PNG-Signaturen sind Binärdaten, können wiederholt abgerufen und erst beim Finalisieren gerendert werden. Storage vermeidet Base64-Aufblähung und große Datenbank-Backups, während die Tabelle transaktionsnahe Metadaten, Hash und fachliche Bindung hält. Der Objektpfad wird deterministisch und revisionsgebunden angelegt, beispielsweise `prepared-contract-signatures/{kommunikationsvorgang_id}/{signatur_revision}/{signatur_schluessel}/{uuid}.png`.

- Upload und Metadatenpersistenz erfolgen über eine serverseitige/RPC-Operation; der Client darf keinen fremden Pfad wählen.
- Bucket und Objekte sind privat. Lesen/Schreiben ist auf berechtigte Vertragsbearbeiter begrenzt; Downloads laufen über autorisierte bzw. signierte URLs.
- Das Objekt wird zuerst hochgeladen, dann die Datenbankzeile atomar auf `unterschrieben` gesetzt. Nicht referenzierte Uploads werden per kontrolliertem Cleanup entfernt; ein fehlgeschlagener PDF-Render löscht keine referenzierten Objekte.
- Bei einer rechtmäßigen späteren Korrektur wird die alte Signatur nicht überschrieben oder wiederverwendet; Aufbewahrung und Löschung folgen einem noch festzulegenden dokumentrechtlichen Löschkonzept.

## B6 – Revision und Unveränderlichkeit

Beim Übergang `vorbereitet` → `zur_signatur` setzt der bestehende Ablauf `signatur_revision = revision`. Genau zu diesem Zeitpunkt werden die Positionszeilen erzeugt. Entwurfänderungen bleiben aufgrund der vorhandenen Guards ausgeschlossen, solange Signaturstart und -revision gesetzt sind.

Alle Lese-, Schreib- und Finalisierungsabfragen filtern zwingend über `(kommunikationsvorgang_id, signatur_revision)`. Ein Vorgang der Revision 4 kann damit keine Zeile der Revision 3 verwenden. Bereits unterschriebene Zeilen sind append-only/unveränderlich; offene Positionen können nur einmal in `unterschrieben` übergehen.

„Speichern“ persistiert eine einzelne Zeile und beendet optional die Sitzung. „Speichern und Weiter“ führt exakt denselben Persistenzschritt aus und wählt danach lediglich eine andere offene Zeile. Ein Sitzungsabbruch verändert bereits persistierte Positionen nicht.

## B7 – Finalisierung und Retry

Der bestehende Finalisierungsweg bleibt fachlich erhalten. Der Finalizer lädt künftig alle Positionen des eingefrorenen Vorgangs, prüft, dass jede `pflichtig = true` Position `unterschrieben` ist, lädt die signierten PNGs aus Storage und befüllt die später definierten PDF-Felder.

Bei Render-, Upload- oder `MarkFinalDocumentReadyForReview`-Fehlern bleiben Positionen und Signaturbilder erhalten. Der bestehende Provenance-/Recovery-Mechanismus für das finale Dokument wird weiterverwendet. Erst nach erfolgreichem finalem Dokument wird der Vorgang wie bisher auf `letzte_pruefung` gesetzt.

## B8 – Migration alter MA/PV-Vorgänge

| Altzustand | Strategie |
| --- | --- |
| `vorbereitet` | Bei einem künftigen Signaturstart normale Positionsbildung nach dem neuen Modell. |
| `zur_signatur`, ohne `final_dokument_id` | Kontrolliert übernehmen: beim ersten Öffnen bzw. per idempotenter Backfill-Operation werden ausschließlich offene Positionszeilen für die eingefrorene `signatur_revision` angelegt. Keine Position wird als unterschrieben markiert, weil vorher keine Einzelsignaturen persistent existierten. |
| `letzte_pruefung` / `erledigt` mit finalem Dokument | Nicht migrieren. Das finale PDF bleibt unveränderlicher historischer Beleg. |
| Inkonsistente Altvorgänge | Nicht automatisch finalisieren; fachlich prüfen oder über den bestehenden Verwerfungsweg beenden. |

## B9 – WV-Integration

Beim WV bleibt `wartungsvertrag_dokument_workflow` die Zuordnungsklammer. Für jede `wartungsvertrag_zuordnung` erzeugt bzw. stellt `ensure_wartungsvertrag_dokument_workflow` den Workflow bereit. Die WV-Prepared-Contract-Erstellung legt anschließend genau einen `kommunikationsvorgang` mit `dokumenttyp = 'wartungsvertrag'` und `wartungsvertrag_zuordnung_id` an und schreibt dessen ID in `wartungsvertrag_dokument_workflow.kommunikationsvorgang_id`.

Danach gelten derselbe Signaturstart, dieselben Positionszeilen und dieselbe Finalisierungslogik wie für MA/PV. Das finale Dokument wird sowohl über die bestehende Vorgangs-Provenance als auch über `source_wartungsvertrag_zuordnung_id` mit der Zuordnung verbunden; der WV-Workflow erhält die Dokument-ID und den Status `abgeschlossen`. Bei Erzeugungs- oder Signaturfehlern verbleiben Workflow und einzelne gespeicherte Signaturen für Wiederaufnahme erhalten.

## B10 – Exakte DB-Aufgaben für Phase C

1. Neue Migration: Tabelle `prepared_contract_signatur`, Indizes, FKs, Status-/Snapshot-Checks und RLS.
2. Trigger bzw. serverseitige Funktionen für revisionsgebundene Positionsbildung, Einmal-Übergang nach `unterschrieben` und Unveränderlichkeit.
3. Neue RPC zur Vertragsunterzeichner-Auflösung einschließlich direkter Berechtigungen und gültiger Vertretungen; bestehende allgemeine Besetzungs-RPC nicht semantisch brechen.
4. Serverseitige Operation zum Speichern einer einzelnen Signatur inklusive Berechtigungsprüfung, Snapshotbildung, Storage-Pfad/Hash und Verbot derselben Person für `vorstand_1` und `vorstand_2`.
5. Private Storage-Bucket-/Policy-Migration für Signatur-PNGs und kontrollierte Leserechte.
6. Idempotente Backfill-/Ensure-Funktion für Positionszeilen eines bereits gestarteten MA/PV-Vorgangs ohne persistierte Einzelsignaturen.
7. WV-Anbindung: sichere Funktion/Trigger zum Setzen von `wartungsvertrag_dokument_workflow.kommunikationsvorgang_id` beim Anlegen des WV-Prepared-Contracts sowie konsistente Statusübergänge.
8. Finalisierungs-Guard als RPC oder Trigger: `letzte_pruefung` nur bei vollständigen Pflichtpositionen derselben eingefrorenen Revision.

Phase C implementiert nur diese Datenbank- und Zugriffsschicht. Änderungen an C#/TypeScript, PDF-Feldspezifikationen oder UI folgen danach separat.

## Phase-C-Konkretisierung

Die Umsetzung verwendet die Migration `20261021160000_prepared_contract_signatures.sql`, die Tabelle `prepared_contract_signatur`, den privaten Bucket `prepared-contract-signatures` und die RPCs `vereinsfunktion_vertragsunterzeichner`, `ensure_prepared_contract_signatur_position`, `speichere_prepared_contract_signatur` sowie `prepared_contract_signaturen_vollstaendig`.

Die tatsächlichen Spaltennamen folgen dem deutschen Datenbankschema (`pflicht`, `unterzeichner_typ`, `unterzeichnet_am`, `unterzeichnet_von_auth_user_id`). `unterzeichner_name_snapshot` ist bei einer offenen Position zulässig leer, weil die konkrete Vereinsunterzeichner-Auswahl erst beim Speichern erfolgt. Für eine unterschriebene Vereinsposition werden alle Funktions- und Vertretungs-Snapshots serverseitig aus der neuen Resolver-RPC erzeugt.

Der Bucket erlaubt administrativen, authentifizierten Anwendern ausschließlich das Lesen. Upload, Änderung und Löschung erfolgen später kontrolliert über serverseitige Service-Role-Komponenten; der Speichervorgang prüft zusätzlich die Existenz des referenzierten Storage-Objekts. Die Tabellen-RLS erlaubt administrativen Anwendern nur das Lesen; Positionen werden ausschließlich über die geprüften RPCs angelegt oder unterschrieben.

### Manuelle DB-Prüfschritte für Phase C

Das Repository enthält keine lokale pgTAP-/Migrations-Testumgebung. Vor einem Deployment sind deshalb in einer isolierten Testdatenbank mindestens diese Prüfungen auszuführen:

1. Dieselbe `ensure_prepared_contract_signatur_position`-Anfrage zweimal ausführen: genau eine Position muss bestehen bleiben.
2. Für denselben Vorgang/Revision denselben Schlüssel mit abweichendem Plan übergeben: die Anfrage muss scheitern; für eine andere Revision darf keine Position angelegt werden, solange diese nicht die eingefrorene Vorgangsrevision ist.
3. Je eine offene Position `mitglied.antrag` und `mitglied.datenschutz` für dieselbe Person speichern: beide müssen zulässig sein. Danach `vorstand_1` und `vorstand_2` mit derselben Person versuchen: die zweite Speicherung muss scheitern.
4. Eine unterschriebene Position direkt ändern oder löschen: beides muss scheitern.
5. Die Resolver-RPC mit direkter Berechtigung, gültiger Vertretung und abgelaufener Vertretung prüfen. Nur die ersten beiden Fälle dürfen erscheinen; eine technische App-Rolle ohne `vertragsunterzeichnung` darf keinen Datensatz erzeugen.
6. `prepared_contract_signaturen_vollstaendig` mit offener Pflichtposition, vollständig unterschriebenen Pflichtpositionen und offener optionaler Position prüfen: erwartete Ergebnisse `false`, `true`, `true`.
