# Zentraler PDF-Export

`supabase/functions/kgv-generate-export-pdf` ist der serverseitige Einstiegspunkt für PDF-Exporte. MAUI und Web verwenden dafür denselben generischen Export-Request.

## Vertrag

Ein authentifizierter `POST` an die Function verwendet diesen exportunabhängigen Contract:

```json
{
  "export_key": "...",
  "format": "pdf",
  "parameters": {
    "aktiv_filter": "aktiv",
    "sortierung": "name"
  },
  "columns": ["mitgliedsnr", "name", "email"],
  "orientation": "auto"
}
```

`export_key`, `format`, `parameters`, `columns` und `orientation` sind für alle künftigen Standardexporte gleich. Neue Exporte sollen künftig ausschließlich über Supabase-Metadaten und Datenquellen ergänzt werden können; die Clients benötigen dafür keinen exportspezifischen Request oder Download-Service. Derzeit unterstützt der Server ausschließlich `mitgliederliste` und `format = pdf`. `orientation` akzeptiert `auto` (Standard), `portrait` und `landscape`; die endgültige automatische Ausrichtung entscheidet künftig serverseitig.

Für die Mitgliederliste unterstützt `aktiv_filter` `alle`, `aktiv` (Standard) und `inaktiv`. `sortierung` unterstützt `name` (Standard), `mitgliedsnr` und `garten`. Die Berechtigung wird über `rpc_execute_export` und damit `assert_export_access` geprüft; nur Admin/Vorstand erhalten Daten.

Die Funktion liefert `application/pdf` mit einem datierten Dateinamen. Die Mitgliederliste ist A4 quer, wiederholt Titel und Tabellenkopf auf jeder Seite und fasst Adresse bzw. Telefon/Mobil jeweils mehrzeilig in einer Zelle zusammen. Bemerkungen folgen gegebenenfalls als kompakte Zusatzzeile, damit die Tabelle lesbar bleibt. Kennzeichen sind ausschließlich `X` oder leer.

Die Daten kommen aus `rpc_export_mitgliederliste(aktiv_filter, sortierung)`: nicht-Demo-Mitglieder, fachlicher Aktivstatus inklusive `mitglied_ende`, aktuelle `parzellen_belegung`, Einwilligungen und ein boolescher App/Auth-Zugang. UUIDs werden nicht ausgegeben.

## Vorlagen-Fallback

Optional kann eine aktive veröffentlichte Katalogvorlage mit `template_key = export_mitgliederliste` hinterlegt werden. Sie wird als Hintergrund verwendet, falls sie lesbar ist. Fehlt sie oder ist sie ungültig, erzeugt die Function ohne Fehler ein normales weißes A4-PDF.

## Datenbankänderung

`20261015100000_cx1_mitgliederliste_pdf_export.sql` ergänzt die parameterisierte Mitgliederlisten-RPC und reicht deren Filter im bestehenden sicheren Export-Gateway weiter. Die bisherige parameterlose RPC bleibt als kompatible Hülle erhalten.
