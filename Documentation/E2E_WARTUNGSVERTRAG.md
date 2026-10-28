# Phase 10 – E2E Wartungsvertrag

## Ergebnis

Der Wartungsvertragsprozess verwendet den gemeinsamen Prepared-Workflow. Vertragspartner und Wartungsvertragsdefinition werden revisionsfest gebunden, die 90xxx wird beim Übergang `zur_signatur` reserviert, Pflichtsignaturen werden persistent gespeichert und der Abschluss erfolgt serverseitig über `complete_prepared_wartungsvertrag`.

Zwei fachlich verschiedene Folgevorgänge bleiben strikt getrennt:

- Eine normale Verlängerung erzeugt eine anschließende Laufzeitzuordnung desselben `wartungsvertrag_id`. Sie behält dieselbe 90xxx und benötigt nur eine Verlängerungsmitteilung, keine neue Signaturstrecke.
- Eine grundlegende Änderung wird ausschließlich durch die ausdrückliche Benutzeraktion gestartet. Sie erzeugt eine neue, zunächst inaktive Vertragsdefinition, einen neuen Prepared-Vorgang und beim Signaturstart eine neue 90xxx.

Phase 10 schließt drei E2E-Lücken:

1. Der Verlängerungs-Retry nach einem Reload verwendete den Hauptmitgliedskontext statt der tatsächlichen Vertragspartei. Das scheiterte für Nebenmitglieder an der unveränderlichen Parteienbindung.
2. Ein erneuter Finalize-Aufruf einer bereits abgeschlossenen Verlängerungsmitteilung war nicht idempotent.
3. Ein abgebrochener Versuch einer grundlegenden Änderung blieb fachlich als vorbereitet markiert und blockierte einen später bewusst neu gestarteten Versuch.

## Tatsächlicher Neuvertragsablauf

1. Eine Wartungsvertragsdefinition und eine Zuordnung werden für den Hauptmitgliedskontext und die ausdrücklich ausgewählte Vertragspartei angelegt. Die Zuordnung dient zugleich als stabiler Anker des Dokumentworkflows.
2. `ensure_wartungsvertrag_dokument_workflow` und `ensure_wartungsvertrag_prepared_contract` erzeugen beziehungsweise liefern idempotent den Prepared-Vorgang.
3. Der Entwurf friert Vertragspartei, Vertragsdefinition, Laufzeit, Leistungsbeschreibung, Bereich, Arbeitsstundenwirkung und Bemerkung ein.
4. Die Vorschau wird ohne Vertragsnummer erzeugt. Es gibt keinen technischen Fallback.
5. `start_prepared_contract_signature` setzt den Vorgang auf `zur_signatur`, friert die Revision ein und reserviert über `fachnummer_reservierung` eine 90xxx für die fachliche `wartungsvertrag_id`.
6. Persistente Signaturpositionen werden für die tatsächliche Vertragspartei sowie `vorstand_1` und `vorstand_2` erzeugt.
7. Nach vollständigen Signaturen erzeugt der Generator genau ein finales Dokument pro Vorgang/Revision.
8. `complete_prepared_wartungsvertrag` bindet Dokument, Zuordnung und Vorgang und setzt den Dokumentworkflow auf abgeschlossen. Bei einer grundlegenden Änderung erzeugt dieser transaktionale Schritt erst die neue Zuordnung und beendet die alte Laufzeit ohne Überlappung.
9. Das Finaldokument friert den Versand-Snapshot ein. `prepare_contract_dispatch` verwendet den normalen Vertragsversand und ist für den initialen Versand idempotent.

Eine bereits vorhandene Zuordnungszeile vor dem Erstabschluss ist der technische Workflow-Anker. Fachlich dokumentiert abgeschlossen ist der Wartungsvertrag erst mit `kommunikationsvorgang.status = erledigt`, Finaldokument und `wartungsvertrag_dokument_workflow.status = abgeschlossen`. Eine grundlegende Änderung besitzt vor diesem Abschluss ausdrücklich noch keine neue Zuordnung.

## Datenmodell und zentrale Bausteine

Zentrale Tabellen:

- `wartungsvertraege`: fachliche Vertragsdefinition; die 90xxx gehört zu dieser ID
- `wartungsvertrag_zuordnungen`: Laufzeit, Hauptmitgliedskontext und tatsächliche Vertragspartei
- `wartungsvertrag_dokument_workflow`: Dokument-/Signaturstatus je Zuordnung
- `wartungsvertrag_folgeentscheidung`: Verlängerung oder Nichtverlängerung
- `wartungsvertrag_grundlegende_aenderung`: explizite Provenance eines neuen Vertragsversuchs
- `kommunikationsvorgang`: Prepared-Status, Revision, Entwurf und Finaldokument
- `prepared_contract_signatur`: persistente Signaturpositionen
- `fachnummer_reservierung`: einzige Quelle der 90xxx
- Versand-Snapshot- und `versandinstanz`-Tabellen für den revisionsfesten Versand

Zentrale RPCs und Services:

- `ensure_wartungsvertrag_prepared_contract`
- `start_prepared_contract_signature`
- `complete_prepared_wartungsvertrag`
- `verlaengere_wartungsvertrag`
- `complete_wartungsvertrag_verlaengerungsmitteilung`
- `entscheide_wartungsvertrag_nichtverlaengerung`
- `start_wartungsvertrag_grundlegende_aenderung`
- `prepare_contract_dispatch`
- Web: `MaintenanceContractSignatureFlow`, `MaintenanceContinuationPanel`, `MaintenanceMaterialChangePanel`
- MAUI: `PreparedMaintenanceContractSigningService`, `WartungsvertragFortfuehrungPage` und Infrastructure-RPCs

## 90xxx und sichtbare Identitäten

Die 90xxx wird ausschließlich beim Statuswechsel nach `zur_signatur` reserviert. Source of Truth ist `fachnummer_reservierung` mit `quelle_art = wartungsvertrag` und `quelle_id = wartungsvertrag_id`. Vorschauen bleiben leer, Retry desselben Vorgangs beziehungsweise derselben Vertragsdefinition liefert dieselbe Nummer und abgebrochene Nummern werden nicht freigegeben.

Sichtbare Mitgliedsnummern stammen aus `mitglied.mitgliedsnummer`. Der Generator verwendet weder `mitglied.id` noch `wartungsvertrag_id`, Kommunikationsvorgangs-ID, Dokument-ID oder UUID als sichtbaren Ersatz. Titel und Dateiname enthalten die fachliche 90xxx.

## Vertragspartei und Signaturen

Der Hauptmitgliedskontext und die tatsächliche Vertragspartei sind getrennte Felder. Vertragspartei kann das Hauptmitglied oder dessen zulässiges Nebenmitglied sein. `kommunikationsvorgang.mitglied_id`, Dokumente, Versand und die Signaturposition `mitglied` werden an `vertragspartei_mitglied_id` gebunden. Das Hauptmitglied wird bei einem Nebenmitglied nicht automatisch zusätzlich unterschriftspflichtig.

Erforderlich sind die Signatur der tatsächlichen Vertragspartei und zwei Vereinsunterschriften. Vereinsunterzeichner werden aus dem eingefrorenen Funktions-/Vertretungssnapshot bestimmt; Namen, Freitextämter oder hartcodierte Personen entscheiden nicht. Fehlende Pflichtsignaturen sperren die Finalisierung. Retry erzeugt aufgrund der Positionsschlüssel keine Duplikate.

## PDF und Templates

Der Generator akzeptiert die versionierten Field-Specs v1 und v2 mit Renderer `top-left/v1`. Nur v2 besitzt das sichtbare Feld `vertragsnummer`.

Lokale, gegen B4 geprüfte v2-Artefakte:

- Wartungsvertrag PDF: `7fe8ee60173e9634f1f299f24e1e42f8c64e6bc5efd285e5b07d8f0ce027a050`
- Wartungsvertrag Field-Spec: `13ce4cd971cd48a4f75bb5f08f325d335ffb6805789d531e320ae775abefb755`
- Verlängerungsmitteilung PDF: `7f1ea293d872fd5762b4a5c24210b5cc4cd0d1ad843463c1b15785d798e281b2`
- Verlängerungsmitteilung Field-Spec: `b13024a353b10415482ad03887e8cf0e88b2be2cb660d42ac149fe4c32e1b865`

B4 registriert diese Artefakte bewusst nur als inaktive Drafts. Phase 10 lädt oder aktiviert keine Vorlage. Zur Laufzeit wird ausschließlich eine bereits veröffentlichte, kompatible Katalogfassung verwendet.

## Normale Verlängerung

`verlaengere_wartungsvertrag` bestimmt den Folgebeginn serverseitig als Tag nach `gueltig_bis`, übernimmt dieselbe `wartungsvertrag_id`, denselben Hauptmitgliedskontext und dieselbe tatsächliche Vertragspartei. Die Fachnummer bleibt daher unverändert. Die Folgeentscheidung und Folgezuordnung werden bei identischen Wiederholungen wiederverwendet.

Die Verlängerungsmitteilung ist ein `dokument_erzeugung`-Vorgang ohne Signaturpositionen. Sie nennt `Wartungsvertrag Nr. 90xxx`, Laufzeit und Vertragspartei. Der Abschluss ist nun auch nach bereits erfolgreicher Erzeugung idempotent. Ein Reload-Retry verwendet `vertragspartei_mitglied_id`, sodass Nebenmitglieder korrekt unterstützt bleiben.

## Nichtverlängerung

Die ausdrückliche Entscheidung `nicht_verlaengern` erzeugt weder Folgezuordnung noch Wartungsvertrag, Signaturvorgang oder 90xxx. Die bestehende Zuordnung bleibt bis zu ihrem `gueltig_bis` gültig und endet danach. Es wird keine Anschlusslaufzeit errechnet. Eine erlaubte Rücknahme bleibt vollständig auditiert.

## Grundlegende Änderung

Es gibt keine Änderungsheuristik. Unterschiede bei Preis, Leistung, Laufzeit, Partei oder anderen Feldern starten niemals automatisch einen neuen Vertrag. Nur `Neuen Wartungsvertrag wegen grundlegender Änderung erstellen` ruft den entsprechenden RPC auf.

Der RPC kopiert Eingabedaten als Arbeitserleichterung in eine neue inaktive Vertragsdefinition und erzeugt einen Prepared-Vorgang ohne Zuordnung. Der alte Vertrag bleibt unverändert. Erst nach neuen Signaturen führt `complete_prepared_wartungsvertrag` transaktional folgende Schritte aus:

- neue Zuordnung anlegen,
- alte Zuordnung auf den Vortag des neuen Beginns begrenzen,
- neue Vertragsdefinition aktivieren,
- Finaldokument und Dokumentworkflow binden,
- Provenance als erledigt markieren.

Abbruch markiert den Versuch als `abgebrochen`; eine bereits reservierte 90xxx bleibt verbraucht und der Altvertrag bleibt bestehen. Ein später ausdrücklich neu gestarteter Versuch erzeugt einen neuen inaktiven Vertrag. Ein partieller eindeutiger Index erlaubt höchstens einen vorbereiteten Versuch je Vorgänger und bewahrt abgebrochene Versuche historisch.

## Versand, Resume und Retry

Neu- und Änderungsverträge verwenden nach erfolgreichem Abschluss den normalen Prepared-Vertragsversand. Verlängerungen verwenden den separaten Mitteilungsweg. Beide erzeugen revisionsfeste Empfänger-/Dokumentsnapshots; die Vertragspartei wird nicht geraten.

Wiederaufnehmbar sind Prepared, Vorschau, `zur_signatur`, Teilunterschriften, vollständige Signaturen vor Abschluss, abgeschlossene Finalisierung vor Versand, Versandfehler, offene Verlängerungsmitteilungen und aktive grundlegende Änderungen. Nummer, Signaturpositionen, Dokument, Zuordnung und initiale Versandinstanz bleiben beim Retry stabil.

## Web, MAUI und Berechtigungen

Web bietet Signatur, Verlängerung, Nichtverlängerung und die ausdrückliche grundlegende Änderung an. MAUI besitzt die gemeinsame Signatur-/Fortführungslogik und denselben Backendzustand; es wurde keine künstliche UI-Parität ergänzt.

Vorbereitung und grundlegende Änderung verwenden die vorhandene Admin-/Vorstandsprüfung. Verlängerung und Nichtverlängerung verwenden den Funktionszweck `wartungsvertrag_entscheidung` einschließlich Vertretung. Vereinsunterschriften verwenden `vertragsunterzeichnung`; Versand verwendet `kann_vertragsversand_ausfuehren`. Finalisierungs-RPCs bleiben `service_role` vorbehalten.

## Automatisierte Absicherung und bekannte Altprobleme

`npm run test:maintenance-contract-e2e` deckt die Szenarien A–I ab. Zusätzlich bestehen 25 vorhandene Wartungsvertrags-Vertragstests und 10 fokussierte .NET-Tests. Der Web-Produktionsbuild und gezieltes ESLint sind erfolgreich.

Der bekannte MAUI-Vollbuild bleibt mit 48 fachfremden Altfehlern in anderen Seiten/Integrationen rot; in den geänderten Wartungsvertrags-, Core- und Infrastructure-Dateien verbleibt kein Buildfehler. Bestehende ImageSharp-Advisories, Nullability-Warnungen und die bekannten allgemeinen Testfixtures sind nicht Teil von Phase 10.

## Migration und Betrieb

`20261028130000_phase10_wartungsvertrag_e2e_haertung.sql` enthält den Vertragspartei-Detail-RPC, idempotente Verlängerungsmitteilungen und die abbruchsichere Historie grundlegender Änderungen. Sie wurde nur versioniert, nicht ausgeführt. Es wurden keine Edge Functions deployt, keine Templates hochgeladen oder aktiviert, keine Produktionsdaten geändert und kein Release erzeugt.

**Phase 10 – E2E Wartungsvertrag = 100 %.**
