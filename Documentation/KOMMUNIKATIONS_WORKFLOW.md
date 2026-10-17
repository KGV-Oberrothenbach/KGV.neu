# Kommunikations- und Dokumentvorgangs-Zentrale

## Arbeitsplan

| Block | Inhalt | Stand |
| --- | --- | --- |
| K1 | Datenmodell und serverseitige Statuslogik | Erledigt |
| K2 | WEB-Kommunikationsmenü | Offen |
| K3 | MA/PV vorbereiten und parken | Offen |
| K4 | Gemeinsamer WEB-/MAUI-Workflow | Offen |
| K5 | Mail-/Druckentscheidung | Offen |
| K6 | Mailversand | Offen |
| K7 | Druck und Archivierung | Offen |
| K8 | Weitere Dokumenttypen | Offen |

## K1-Entscheidungen

`public.kommunikationsvorgang` ist die zentrale, vereinsbezogene Vorgangstabelle. Es gibt bewusst kein `verein_id`, weil die Vereinsauswahl außerhalb der einzelnen Vereinsdatenbank erfolgt.

Der Entwurf bleibt als JSONB inklusive Revision speicherbar. Wird eine Signatur gestartet, speichert der Trigger die aktuelle Revision und sperrt die signaturrelevanten Entwurfsfelder. Statuswechsel werden ebenfalls im Trigger geprüft.

Ein Storage-Bucket wird in K1 noch nicht angelegt. Die Tabellenfelder für temporäre Vorschauen und finale Dateien sind vorhanden, aber einen privaten Bucket und dessen Policies legt K3/K5 zusammen mit dem ersten tatsächlichen Erzeugungs- und Zugriffsworkflow fest. So bleibt die Storage-Policy nicht ohne nutzbaren, geprüften Zugriffspfad zurück.
