# Kommunikations- und Dokumentvorgangs-Zentrale

## Arbeitsplan

| Block | Inhalt | Stand |
| --- | --- | --- |
| K1 | Datenmodell und serverseitige Statuslogik | Erledigt |
| K2 | WEB-Kommunikationsmenü | Erledigt |
| K3 | MA/PV vorbereiten, parken, bearbeiten und signieren | Erledigt |
| K4 | Gemeinsamer WEB-/MAUI-Workflow | Offen |
| K5 | Mail-/Druckentscheidung | Offen |
| K6 | Mailversand | Offen |
| K7 | Druck und Archivierung | Offen |
| K8 | Weitere Dokumenttypen | Offen |

### K4-Teilstatus

| Teil | Stand |
| --- | --- |
| K4a | ✅ |
| K4b | ✅ |
| K4c | ✅ |
| K4d | ✅ |
| K4e | Offen |
| K4f | Offen |

K4 insgesamt bleibt **offen**. K4d schließt den MAUI-Pachtvertrag bis zum
Status `vorbereitet` ab: Mapper, Resume/Claim, vollständiger Editor,
semantischer Unsaved-Guard, zentrale flüchtige `kgv-generate-contract`-Preview
und revisionssicheres Create/Update sind umgesetzt. Beide MAUI-Einstiege
verwenden denselben Prepared-Flow. Lokale Pachtvertrag-Signatur- und
Finalize-Schritte gehören nicht mehr zu diesem Flow; sie folgen erst in K4e.

## K1-Entscheidungen

`public.kommunikationsvorgang` ist die zentrale, vereinsbezogene Vorgangstabelle. Es gibt bewusst kein `verein_id`, weil die Vereinsauswahl außerhalb der einzelnen Vereinsdatenbank erfolgt.

Der Entwurf bleibt als JSONB inklusive Revision speicherbar. Wird eine Signatur gestartet, speichert der Trigger die aktuelle Revision und sperrt die signaturrelevanten Entwurfsfelder. Statuswechsel werden ebenfalls im Trigger geprüft.

Ein Storage-Bucket wird in K1 noch nicht angelegt. Die Tabellenfelder für temporäre Vorschauen und finale Dateien sind vorhanden, aber einen privaten Bucket und dessen Policies legt K3/K5 zusammen mit dem ersten tatsächlichen Erzeugungs- und Zugriffsworkflow fest. So bleibt die Storage-Policy nicht ohne nutzbaren, geprüften Zugriffspfad zurück.

## K2 WEB-Arbeitsoberfläche

Der Menüpunkt **Kommunikation** ist ausschließlich für Admin und Vorstand sichtbar. Die gemeinsame Arbeitszentrale enthält die Ansichten **Zur Signatur**, **Mailausgang**, **Druckausgabe** und **Erledigt**. Jede Ansicht hat die Arbeitsfilter **Meine**, **Unzugeordnet** und **Alle**; standardmäßig ist Unzugeordnet aktiv.

Die Liste lädt die fachlichen Verknüpfungen zu Mitglied und Parzelle gemeinsam mit dem Vorgang. Admin und Vorstand können einen unzugeordneten Vorgang atomar übernehmen oder ihre eigene Zuweisung freigeben. Die Detailansicht ist bewusst nur lesend; weder Storage-Pfade noch öffentliche URLs werden angezeigt.

## K3 Vertragsentwürfe und Signaturen

Mitgliedsanträge und Pachtverträge werden als versionierter Entwurf vorbereitet. Bis zum Signaturstart kann ausschließlich die zugewiesene Person den Entwurf revisionssicher bearbeiten. Der Start friert die aktuelle Revision serverseitig ein; Signaturen bleiben ausschließlich im flüchtigen Client-State. Wird eine gestartete Signatur erneut geöffnet, beginnt ihre Reihenfolge ohne zuvor erfasste Teil-Signaturen von vorn. Das finale Dokument wird vor dem Statuswechsel des Kommunikationsvorgangs nach `erledigt` erzeugt.

Falls die Dokumenterzeugung erfolgreich war, der anschließende Abschluss des Kommunikationsvorgangs jedoch fehlschlägt, kann innerhalb derselben Sitzung ausschließlich der Workflow-Abschluss erneut versucht werden; ein zweites Finalize wird nicht ausgelöst. Nach einem vollständigen Neuöffnen gibt es dafür bewusst keine automatische Recovery-Heuristik: Aus dem vorhandenen Dokumentstatus ist nicht sicher ableitbar, ob genau dieser Kommunikationsvorgang bereits finalisiert wurde.
