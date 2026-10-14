# Zentraler PDF Template Katalog

Der Katalog `pdf_template_catalog` und der private Storage-Bucket `pdf-templates` trennen Vorlagen vollständig von fertigen Dokumenten im Google-Drive-/`dokument`-Pfad.

## Mitgliedsantrag v1 bereitstellen

1. Die unveränderte PDF nach `templates/mitgliedsantrag/v1/template.pdf` und die zugehörige Feldspec nach `templates/mitgliedsantrag/v1/field-spec.json` in den privaten Bucket laden.
2. Für beide Dateien SHA-256-Prüfsummen bilden.
3. Einen Katalogeintrag mit `template_key = mitgliedsantrag`, `version = v1`, `field_spec_version = mitgliedsantrag-field-spec/v1`, `renderer_version = pdfsharpcore-ma/v1`, `status = published` und `aktiv = true` anlegen.

Eine veröffentlichte Version ist durch einen Datenbank-Trigger unveränderlich. Für eine Anpassung wird immer ein neuer Pfad und eine neue Version angelegt; erst danach darf die bisher aktive Version deaktiviert werden.

`SupabasePdfTemplateProvider` lädt nur veröffentlichte aktive Versionen und prüft Renderer-Version, Feldspec-Version und beide Prüfsummen. Der Mitgliedsantrag-Renderer vergleicht außerdem die vollständige Feldgeometrie mit der unterstützten v1-Spezifikation und bricht bei Abweichungen verständlich ab. Damit bleiben die Signaturkoordinaten unverändert abgesichert.

Der eingebettete Mitgliedsantrag bleibt ausschließlich für isolierte lokale Factory-Tests als explizit kommentierter Kompatibilitätsfall erhalten. Die produktiven MA-Pfade in `SupabaseService` verwenden ausschließlich den zentralen Provider und fallen bei einer fehlenden, manipulierten oder inkompatiblen Version nicht stillschweigend zurück.
