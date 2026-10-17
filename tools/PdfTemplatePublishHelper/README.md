# PDF-Template Publish Helper

Dieses Werkzeug ist ein lokaler Dry Run. Es validiert PDF und Field-Spec, berechnet SHA-256-Werte und gibt ein Markdown-Manifest mit Storage- und SQL-Befehlen aus. Es enthält keinen Supabase-Client, keine Datenbankverbindung und keine Remote-Schreibfunktion.

```powershell
dotnet run --project tools/PdfTemplatePublishHelper -- `
  --template-key pachtvertrag --version 3 `
  --pdf <lokale-template.pdf> --field-spec <lokale-field-spec.json> `
  --field-spec-version pachtvertrag-field-spec/v3 --renderer-version top-left/v1 `
  --expected-field-spec-version pachtvertrag-field-spec/v3 --expected-renderer-version top-left/v1 `
  --manifest .\publish-manifest.md
```

Die beiden `expected`-Angaben sind der vom aufrufenden Dokumentpfad erwartete Vertrag. Sie verhindern, dass versehentlich ein anderer Field-Spec- oder Renderer-Vertrag vorbereitet wird.

## Sichere Veröffentlichung

1. Immer eine neue positive Versionsnummer wählen. Nie eine veröffentlichte Version oder ihren Storage-Pfad überschreiben.
2. Helper ausführen und Manifest einschließlich Hashes, Zielpfade und SQL prüfen.
3. Zuerst DDB wählen: `supabase link --project-ref ymxhszxfutjnljiuciyj`.
4. Vor jedem im Manifest ausgegebenen `storage cp` im Dashboard oder mit `supabase storage ls` prüfen, dass der Zielpfad noch nicht existiert; die Upload-Befehle nicht mit einem Overwrite-Parameter ergänzen.
5. Beide Uploads ausführen, anschließend die Manifest-Transaktion im DDB-SQL-Editor ausführen.
6. Mitgliedsantrag und Pachtvertrag praktisch abnehmen.
7. Erst danach den identischen, neu generierten Ablauf nach `supabase link --project-ref itjcabiibuodkxayhvjq` für VDB koordinieren.

Die Manifest-Transaktion deaktiviert zuerst die alte aktive Version und fügt anschließend eine neue veröffentlichte aktive Version ein. Das hält den Unique-Index für genau eine aktive veröffentlichte Version ein. Bereits veröffentlichte Zeilen und Objekte werden nicht verändert.
