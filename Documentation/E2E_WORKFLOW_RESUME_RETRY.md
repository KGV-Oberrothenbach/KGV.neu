# Phase 12 – Workflow-Härtung / Resume / Retry / Abschlusszustände

## Gemeinsame Workflowarchitektur

Mitgliedsantrag, Pachtvertrag und Wartungsvertrag verwenden denselben Prepared-Vertragskern:

1. Ein kommunikationsvorgang hält fachliche Quelle, Entwurf, Revision, Zuweisung und Status.
2. Der atomare Signaturstart friert Vereinsunterzeichner und Signaturpositionen für genau eine Revision ein.
3. Die Fachnummer wird beim ersten Übergang nach zur_signatur reserviert.
4. Persistente Signaturen referenzieren Vorgang, Signaturrevision und stabilen Signaturschlüssel.
5. Das Finaldokument ist durch source_kommunikationsvorgang_id plus source_revision eindeutig.
6. Der fachliche Abschluss läuft in einem workflow-spezifischen, sperrenden RPC.
7. Versandparteien, Pflichtanlagen und Empfänger werden als unveränderliche Snapshots gebunden.

Die Unterschiede liegen hinter diesem Kern: Der Mitgliedsantrag führt in die Vorstandsbearbeitung und gegebenenfalls Aufnahmebestätigung, der Pachtvertrag erzeugt Pachtverhältnis und Vertragsfassung, der Wartungsvertrag erzeugt beziehungsweise verändert Vertrag und Zuordnung. Ersatzfassungen binden ihren unmittelbaren Vorgänger; Verlängerungen behalten die bestehende Wartungsvertragsdefinition und 90xxx-Nummer.

## Zustände und Abschlussmarker

Prepared-Verträge durchlaufen vorbereitet, zur_signatur, letzte_pruefung und erledigt; ein bewusst aufgegebener Vorgang endet in abgebrochen. signatur_revision, signatur_gestartet_am, persistente Positionen und Snapshot-Personen markieren den begonnenen Signaturstand. final_dokument_id plus dessen Source-Provenance markieren die erfolgreiche Finalisierung. Zusätzliche Abschlussmarker sind:

- Mitgliedsantrag: Vorstandsbearbeitung, Aufnahmezustand und Aufnahmebestätigungsvorgang.
- Pachtvertrag: Pachtverhältnis und genau eine maßgebliche Vertragsfassung.
- Wartungsvertrag: Wartungsvertrag, zeitliche Zuordnung und gegebenenfalls Verlängerungsmitteilung.
- Versand: Versandinstanz, Empfängersnapshot, Versuch und Empfängerstatus.

Widersprüchliche oder abweichend wiederholte Aktionen werden abgelehnt. Der Client erzeugt keine Ersatzobjekte zur stillen Reparatur inkonsistenter Altdaten.

## Idempotenz- und Retry-Schlüssel

| Bereich | Stabiler Schlüssel |
| --- | --- |
| Prepared-Vorgang | fachliche Source-/Aktivitätsbedingungen des jeweiligen Prepare-RPC |
| Revision | kommunikationsvorgang.id plus revision |
| 10xxx/20xxx | Fachnummerquelle kommunikationsvorgang |
| 80xxx historisch | Fachnummerquelle historisches_dokument |
| 90xxx | Wartungsvertragsdefinition als kanonische Quelle |
| Signaturposition | Vorgang, Signaturrevision und Signaturschlüssel |
| Signaturbild | SHA-256-abgeleitete UUID im Storage-Pfad |
| Finaldokument | Source-Kommunikationsvorgang und Source-Revision |
| Aufnahmebestätigung | ursprünglicher Mitgliedsantragsvorgang |
| Ersatzvertrag | unmittelbare pachtvertragsfassung |
| Erstversand | Vorgang plus Signatur-/Entwurfsrevision |
| SMTP-Nachricht | Versand-Idempotenz-UUID plus Empfänger-Snapshot-ID |

Eine fachlich beabsichtigte Änderung darf weiterhin eine neue Revision beziehungsweise einen neuen Vorgang erzeugen. Ablehnung mit späterem Neuantrag, Abbruch mit späterem Ersatzversuch, grundlegende Wartungsvertragsänderung und ausdrücklich erneuter Versand bleiben eigenständige Aktionen.

## Retry, Parallelität und Crash-Fenster

start_prepared_contract_signature sperrt den Vorgang. Nach verloren gegangener Antwort liefert ein Retry den bereits erreichten Stand einschließlich gleicher Revision, Fachnummer, Positionen und Snapshots. Ein Signatur-Snapshot wird nicht neu berechnet. Aktuelle Aktionsberechtigungen werden vor dem Retry erneut geprüft.

Der Signatur-Upload ist inhaltsadressiert. Derselbe PNG-Inhalt verwendet denselben Pfad; ein bereits bindend gespeicherter identischer Aufruf liefert Erfolg. Abweichendes Bild, andere Person oder andere eingefrorene Berechtigungsquelle überschreibt keine Signatur.

Finaldokumente sind datenbankseitig pro Source und Revision eindeutig. Der zentrale Upload registriert Owner, Storage-/Drive-Referenz und Provenienz in demselben Dokument-Insert. Scheitert ein paralleler Insert am eindeutigen Source-Schlüssel, kompensiert der Upload seine gerade erzeugte Drive-Datei und der Generator lädt das vorhandene Dokument. Die Abschluss-RPCs sperren ihre Quellen und geben bei bereits exakt abgeschlossenem Zustand das bestehende Ergebnis zurück oder lehnen eine Abweichung eindeutig ab.

Die Aufnahmeentscheidung aktualisiert Mitglied, Antrag, Vorstandsbearbeitung und Aufnahmebestätigungs-Shell in einer Transaktion. Ein exakt gleicher, weiterhin autorisierter Retry wird als Erfolg behandelt; ein anderes Ergebnis oder andere Entscheidungsdaten werden abgelehnt. Stimmen waren bereits versionsfähig und geben beim identischen Retry die letzte Stimmen-ID zurück.

Der Ersatzvertragstart sperrt die konkrete Vorgängerfassung, bevor nach einem aktiven Nachfolger gesucht wird. Parallelaufrufe konvergieren dadurch auf B. Nach abgebrochen darf ein neuer Versuch entstehen; eine bereits erledigte Fassung bleibt Teil der fachlichen Kette.

Der Erstversand wird pro Vorgang/Revision wiederverwendet. Ein ausdrücklicher Neuversand bleibt ein eigener Versandauftrag; ein noch nicht erfolgreich abgeschlossener Neuversand wird bei technischem Retry wiederverwendet. Claims sind exklusiv und nach 15 Minuten als verwaiste Claims wieder beanspruchbar.

SMTP kann ohne transaktionalen Provider oder Message Queue kein mathematisches Exactly-once nach einer Annahme mit anschließendem Prozessabbruch garantieren. Die stabile Message-ID pro Versandinstanz/Empfänger, persistente Versuche, Empfängerstatus und erneute Claimbarkeit begrenzen und dokumentieren dieses Crash-Fenster. Es wurde bewusst keine neue Queue-Architektur eingeführt.

## Pflichtdokumente und Papiernachweise

Pflichtunterlagen werden beim Abschluss in vertragsversand_anlage_snapshot gebunden. Der eindeutige Sortierschlüssel pro Vorgang/Revision verhindert doppelte Snapshot-Anlagen. papier_ausgehaendigt ist ein persistenter Bindungsstatus ohne Storage-Objekt und überlebt Reloads. Ein späterer bewusster Digitalversand ist ein neuer fachlicher Bereitstellungsschritt, kein technischer Retry.

## Resume in Web und MAUI

Web und MAUI rekonstruieren fachliche Zustände aus kommunikationsvorgang, Signaturpositionen, Finaldokument-Provenance, Fachobjekten und Versandtabellen. Browser-Neustart, Navigation und Gerätewechsel benötigen keinen lokalen fachlichen Idempotenzzustand. UI-Sperren und Ladeindikatoren sind Komfortfunktionen, nicht die einzige Absicherung.

Bereits erreichte idempotente Zustände werden serverseitig als bestehendes Ergebnis geliefert. Echte Inkonsistenzen und abweichende Wiederholungen bleiben sichtbare Fehler. Die Clients dürfen daraus keine zusätzlichen Kommunikationsvorgänge, Dokumente oder Vertragsfassungen ableiten.

## Serverseitige Absicherungen

- Row Locks auf Kommunikationsvorgängen und konkreten Vorgängerfassungen.
- Unique Constraints für Fachnummerquelle, Signaturposition, Finaldokument-Provenance, Aufnahmebestätigungsquelle und Vertragsfassungsnachfolger.
- Atomare RPCs für Signaturstart, Finalisierung, Fachabschluss und Versandvorbereitung.
- Eingefrorene Vereinsfunktions-/Vertretungs- und Versand-Snapshots.
- Exakte Retry-Prüfung für Signatur, Aufnahmeentscheidung und Versand.
- Stabile Content-/Source-Schlüssel statt ausschließlich clientseitigem Zustand.

## Testabdeckung

workflow-resume-retry.contract.test.mjs prüft die systemübergreifenden Schutzverträge für parallele Nummernreservierung, doppelten Signaturstart, eindeutige Finaldokumente, Lost-response-Retry, Resume, parallelen Fachabschluss, Versand-Retry und unveränderte Snapshots. Die Phase-8- bis Phase-11-E2E-Suiten regressionsprüfen Mitgliedsantrag, Pachtvertrag, Wartungsvertrag sowie Vereinsfunktionen/Vertretung. Web-Produktionsbuild, Core, Infrastructure und MAUI werden zusätzlich gebaut; bekannte fachfremde MAUI-Altfehler bleiben außerhalb des Scopes.

**Phase 12 – Workflow-Härtung / Resume / Retry / Abschlusszustände = 100 %.**
