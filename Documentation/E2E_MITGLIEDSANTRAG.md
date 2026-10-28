# Phase 8 – E2E Mitgliedsantrag

## Tatsächlicher Workflow

1. Ein Hauptmitglied wird als Antragsteller mit `aufnahme_status=antrag_offen`, `aktiv=false` und ohne `mitglied_seit` angelegt. Ein abgelehnter Antragsteller kann mit denselben Stammdaten einen neuen Vorgang beginnen; die neue serverseitige Insert-Guard setzt ihn dabei atomar wieder auf `antrag_offen`. Aufgenommene Mitglieder und Nebenmitglieder sind ausgeschlossen.
2. Web lädt Stammdaten, Saisonbeitrag, optionales Nebenmitglied und bei Minderjährigen den Vertreterkontext. Ein Vertreter kann ein vorhandenes Mitglied sein oder als manueller Snapshot erfasst werden.
3. Vor jeder Vorschau wird ausschließlich der Satzungsnachweis eingefroren. Digital gebundene Fassung und dokumentierte Papierübergabe sind zulässig. Die Vorschau wird ohne Fachnummer erzeugt und als `vertrag_signatur/mitgliedsantrag` in `vorbereitet` geparkt.
4. `start_prepared_contract_signing` friert Revision, berechtigte Vereinsunterzeichner und Signaturpositionen ein. Beim Zustandswechsel nach `zur_signatur` reserviert `trg_reserve_vertragsnummer` über `reserve_fachnummer` genau eine 10xxx in `fachnummer_reservierung` für den Kommunikationsvorgang.
5. Persistente Signaturen werden im privaten Storage mit SHA-256 gespeichert. Der Generator verwendet für Field-Spec v4 und die aktive v5 denselben persistenten Signaturpfad, prüft Vollständigkeit und Hashes und übernimmt die reservierte `Antrags-Nr.`. Ein Retry derselben Revision verwendet das vorhandene Finaldokument.
6. Nach der letzten Prüfung ruft `kgv-complete-prepared-membership-application` die RPC `start_mitgliedsaufnahme_vorstandsbearbeitung` auf. Nur bestehende Admin-/Vorstandsberechtigung darf die Bearbeitung starten; die Stimmberechtigten werden nach Vereinsfunktion-Verwendungszweck `mitgliedsaufnahme_abstimmung` eingefroren.
7. Jede Snapshot-Person stimmt Ja oder Nein. Eine Nein-Begründung ist optional und intern. Identische Wiederholung gibt dieselbe Stimme zurück; eine geänderte Stimme wird versioniert. Alle Berechtigten sehen Stimmen und Quellen.
8. Stimmen entscheiden nichts automatisch. Bei vollständigem Ja darf eine Person mit Vereinsfunktion-Verwendungszweck `mitgliedsaufnahme_entscheidung` den Weg `einstimmig` verwenden. Sonst bleibt der Weg `vorstandssitzung` mit Sitzungsdatum. Nur diese Funktion entscheidet Annahme oder Ablehnung.
9. Annahme setzt den vorhandenen Antragsteller auf `aufgenommen`, `aktiv=true` und das beschlossene Eintrittsdatum. Ein vorhandener Vertreter wird verknüpft und nicht dupliziert. Ein manueller Vertreter wird erst in derselben Abschluss-Transaktion angelegt und verknüpft. Ablehnung setzt `abgelehnt`, behält Vorgang, Dokument, Stimmen und 10xxx und übernimmt keine interne Stimmbegründung in ein externes Schreiben.
10. Bei Annahme entsteht genau ein retrybarer `dokument_erzeugung/aufnahmebestaetigung`-Vorgang. Das eigene PDF verwendet dieselbe 10xxx des Antrags, die kanonische `mitglied.mitgliedsnummer`, den Antragsteller als Empfänger und wird mit Provenance abgelegt. Danach kann über den bestehenden Vertragsversand ein idempotenter Erstversand vorbereitet, gesendet und bei Fehler fortgesetzt werden.

## Zustände und Resume

Der Kommunikationsvorgang durchläuft `vorbereitet → zur_signatur → letzte_pruefung → erledigt`; Abbruch bleibt der vorhandene kontrollierte Nebenpfad. Entwurf, Satzungs-Snapshot, Signaturrevision, einzelne Signaturen, Finaldokument, Vorstandssnapshot, versionierte Stimmen, Entscheidung, Aufnahmebestätigung und Versandinstanzen liegen persistent vor. Web und MAUI laden diese Zustände wieder, statt einen neuen Vorgang zu beginnen. Die Web-Vorstandsansicht zeigt zusätzlich Erzeugungs- und Versandstatus der Aufnahmebestätigung. MAUI unterstützt Antrag, persistente Signatur und Vorstandsbearbeitung; die Erzeugung und der Versand der Aufnahmebestätigung bleiben bewusst Web-Funktionen. Beide Oberflächen verwenden dieselben RPCs und Dateninvarianten.

## Signaturplan

- Volljährig: `member.application`, `member.privacy`, `vorstand_1`, `vorstand_2`.
- Minderjährig: zusätzlich `gesetzliche_vertretung.antrag` und `gesetzliche_vertretung.datenschutz`.
- Optionales Nebenmitglied bei Volljährigen: `nebenmitglied.antrag` und `nebenmitglied.datenschutz` statt Vertreterpositionen.
- Alle angelegten Positionen sind Pflichtpositionen. Zwei Vereinsunterzeichner werden aus dem eingefrorenen Vereinsfunktions-Snapshot gewählt; Namens- oder Amtsbezeichnungsheuristiken werden nicht verwendet.

## Nummern, PDFs und Dokumente

- Antragsnummer: 10000–19999, Source of Truth `fachnummer_reservierung`, Reservierung erst beim ersten Übergang nach `zur_signatur`.
- Mitgliedsnummer: ausschließlich `mitglied.mitgliedsnummer`; keine Berechnung aus der technischen ID.
- Aktiver Mitgliedsantrag: Template `mitgliedsantrag/v5`, Field-Spec `mitgliedsantrag-field-spec/v5`, Renderer `top-left/v1`.
- Aufnahmebestätigung: Template `aufnahmebestaetigung/v1`, Field-Spec `aufnahmebestaetigung-field-spec/v1`.
- Lokale SHA-256-Werte stimmen mit den Katalogmigrationen überein: Mitgliedsantrag PDF `9a543f…230b`, Spec `0aeee9…0ff`; Aufnahmebestätigung PDF `743375…bb35`, Spec `034143…eb0e`.
- Dateinamen des Mitgliedsantrags und der Aufnahmebestätigung verwenden die Fachnummer. Die Aufnahmebestätigung reserviert keine weitere Nummer.
- Der Mitgliedsantragsversand enthält das finale Antrags-PDF plus ausschließlich die Satzung, soweit digital; Papierübergabe bleibt als Nachweis im Snapshot. Die Aufnahmebestätigung hat einen eigenen Dokument-/Empfänger-Snapshot und keine zusätzlichen Regelwerke.

## Idempotenz und Fehlerkonsistenz

- Erneutes Starten der Signatur reserviert keine zweite 10xxx.
- Entwurf und aktiver Vorgang werden wiederaufgenommen; nach Ablehnung entsteht bewusst ein neuer Vorgang mit neuer 10xxx.
- Finalisierung derselben eingefrorenen Revision verwendet dasselbe Dokument.
- Die Vorstandsbearbeitung ist je Antrag eindeutig; identische Stimmen werden wiederverwendet, Änderungen versioniert.
- Eine zweite Abschlussentscheidung wird abgewiesen. Mitglied, Vertreterverknüpfung und Entscheidung werden in einer Transaktion geändert.
- Aufnahmebestätigungsprozess und -dokument werden anhand des ursprünglichen Antragvorgangs wiederverwendet.
- Der initiale Versand ist je Vorgang/Revision eindeutig und wird bei Doppelklick wiederverwendet. Fehlgeschlagene Empfänger können in derselben Versandinstanz erneut beansprucht werden; ein bewusster Neuversand ist erst nach Erfolg möglich.
- Fehlende Pflichtdaten, Vertreter, Signaturen, Berechtigung, Dokument-Provenance, E-Mail-Einwilligung oder sichere Dateiablage stoppen den jeweiligen Schritt, ohne den vorherigen Fachzustand zurückzusetzen.

## Automatisierte Abdeckung

`npm run test:membership-application-e2e` führt einen zustandsbasierten Integrationstest aus und verwendet produktive Signaturplan-, Lifecycle-, Entscheidungsweg- und v5-Persistenz-Policies. Abgedeckt sind Volljähriger, Minderjähriger mit vorhandenem Vertreter, Minderjähriger mit neuem Vertreter, Ablehnung, Neuantrag nach Ablehnung, Pflichtsignaturen, 10xxx-Stabilität, keine automatische Entscheidung, Berechtigungsfehler, Resume-Snapshot sowie Retry von Start, PDF/Finalisierung, Stimme, Entscheidung, Aufnahmebestätigung und Versand.

Ergänzend bleiben die vorhandenen .NET-Tests für Mapping, Signaturplan, persistentes Signieren, Recovery und Resume maßgeblich. Fachfremde Altfehler im Gesamttestbestand und bekannte fachfremde MAUI-Compilerdiagnosen gehören nicht zu Phase 8.

## Geänderte Lücken und Betriebshinweis

Behoben wurden die v5-Finalisierung über persistente Signaturen, der Neuantrag nach Ablehnung, die serverseitige Startberechtigung der Vorstandsbearbeitung, technische ID-Fallbacks in der betroffenen Vorstandsliste sowie Dokument-/Empfänger-/Retry-Versandstatus der Aufnahmebestätigung. Es bleibt keine fachlich relevante Lücke im beschriebenen Mitgliedsantragsprozess offen.

Die Migration `20261028110000_phase8_mitgliedsantrag_e2e_haertung.sql` ist nur versioniert. In diesem Auftrag wurden weder Migrationen ausgeführt noch Edge Functions deployt, Templates hochgeladen/aktiviert, Produktionsdaten geändert oder Releases erzeugt.

**Phase 8 – E2E Mitgliedsantrag = 100 %**
