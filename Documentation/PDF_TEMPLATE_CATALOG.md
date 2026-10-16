# Zentraler PDF Template Katalog

Der Katalog `pdf_template_catalog` und der private Storage-Bucket `pdf-templates` trennen Vorlagen vollständig von fertigen Dokumenten im Google-Drive-/`dokument`-Pfad.

## Einheitlicher technischer Ladeweg (D1)

`IPdfTemplateProvider` beziehungsweise `SupabasePdfTemplateProvider` ist der einzige produktive .NET-Ladeweg für katalogbasierte PDF-Vorlagen. Er lädt die aktive veröffentlichte Katalogversion sowie die beiden privaten Storage-Objekte, prüft Content-Type und SHA-256-Prüfsummen und parst die grundlegende Field-Spec einschließlich des `topLeft`-Koordinatenursprungs. Die Edge-Function `kgv-generate-contract` verwendet für Mitgliedsantrag und Pachtvertrag denselben generischen `loadPublishedPdfTemplate`-Loader mit denselben Stages: `catalog-load`, `pdf-download`, `field-spec-download`, `checksum`, `field-spec-parse` und `compatibility-check`.

Die dokumentbezogenen Descriptors bzw. Aufrufer bestimmen ausschließlich erwartete Field-Spec-/Renderer-Versionen und erforderliche Felder. Vollständige Feldgeometrie, Fachlogik, Signaturablauf und Rendering bleiben bewusst in den jeweiligen Dokumentfactories beziehungsweise Erzeugungspfaden. Eingebettete Vorlagen bleiben nur als expliziter lokaler Test-Kompatibilitätsfallback vorhanden; ein Produktionsfehler im Katalog löst keinen Fallback aus.

## Mitgliedsantrag v1 bereitstellen

1. Die unveränderte PDF nach `templates/mitgliedsantrag/v1/template.pdf` und die zugehörige Feldspec nach `templates/mitgliedsantrag/v1/field-spec.json` in den privaten Bucket laden.
2. Für beide Dateien SHA-256-Prüfsummen bilden.
3. Einen Katalogeintrag mit `template_key = mitgliedsantrag`, `version = v1`, `field_spec_version = mitgliedsantrag-field-spec/v1`, `renderer_version = pdfsharpcore-ma/v1`, `status = published` und `aktiv = true` anlegen.

Eine veröffentlichte Version ist durch einen Datenbank-Trigger unveränderlich. PDF und Field-Spec müssen exakt unter `templates/<template_key>/<version>/template.pdf` beziehungsweise `field-spec.json` liegen; Datenbank und Provider prüfen diese Bindung. Für eine Anpassung wird immer ein neuer Pfad und eine neue Version angelegt; erst danach darf die bisher aktive Version deaktiviert werden.

`SupabasePdfTemplateProvider` lädt nur veröffentlichte aktive Versionen und prüft Renderer-Version, Feldspec-Version und beide Prüfsummen. Der Mitgliedsantrag-Renderer vergleicht außerdem die vollständige Feldgeometrie mit der unterstützten v1-Spezifikation und bricht bei Abweichungen verständlich ab. Damit bleiben die Signaturkoordinaten unverändert abgesichert.

Die aktive Katalogauflösung wird bei jedem Dokument erneut abgefragt, damit ein Aktivwechsel unmittelbar wirksam wird. Bereits vollständig geprüfte Assets werden dagegen pro Prozess anhand von Schlüssel, Version, beiden SHA-256-Prüfsummen sowie Field-Spec- und Renderer-Version geteilt gecacht. Fehlgeschlagene Downloads oder Validierungen werden nicht gecacht.

Der eingebettete Mitgliedsantrag bleibt ausschließlich für isolierte lokale Factory-Tests als explizit kommentierter Kompatibilitätsfall erhalten. Die produktiven MA-Pfade in `SupabaseService` verwenden ausschließlich den zentralen Provider und fallen bei einer fehlenden, manipulierten oder inkompatiblen Version nicht stillschweigend zurück.

## Pachtvertrag v1 bereitstellen

Der Pachtvertrag verwendet dieselbe Katalog- und Prüflogik wie der Mitgliedsantrag. Die lokale, hochzuladende Referenz liegt unter `KGV.Core/Templates/pachtvertrag/v1/`:

| Wert | Festlegung |
| --- | --- |
| `template_key` | `pachtvertrag` |
| Version | `v1` |
| PDF-Pfad | `templates/pachtvertrag/v1/template.pdf` |
| Field-Spec-Pfad | `templates/pachtvertrag/v1/field-spec.json` |
| `field_spec_version` | `pachtvertrag-field-spec/v1` |
| `renderer_version` | `top-left/v1` |
| PDF SHA-256 | `d532c57f9c7efc0164bbb96d0972d80fb5bb7d1880878cd0ef81e7f1c6f93a8c` |
| Field-Spec SHA-256 | `10be3daff4f55c1c6691117187f576604c6500676b556fd76622d335492796fd` |

Die Field-Spec ist rendererneutral in Punkten mit Ursprung oben links und einbasierter Seitennummerierung definiert. PdfSharpCore nutzt diese Werte direkt; pdf-lib rechnet `y` gegen die Seitenhöhe um. Die Geometrie stammt unverändert aus dem bisherigen MAUI-Produktivpfad `PachtvertragDokumentFactory`.

1. `template.pdf` und `field-spec.json` in den privaten Bucket `pdf-templates` an den oben genannten Pfaden hochladen.
2. Den folgenden Katalogeintrag ausführen. Vorher sicherstellen, dass kein anderer aktiver veröffentlichter `pachtvertrag`-Eintrag vorhanden ist.

```sql
insert into public.pdf_template_catalog
  (template_key, version, storage_bucket, storage_path, content_type, checksum,
   field_spec_path, field_spec_checksum, field_spec_version, renderer_version, status, aktiv)
values
  ('pachtvertrag', 'v1', 'pdf-templates', 'templates/pachtvertrag/v1/template.pdf', 'application/pdf',
   'd532c57f9c7efc0164bbb96d0972d80fb5bb7d1880878cd0ef81e7f1c6f93a8c',
   'templates/pachtvertrag/v1/field-spec.json',
   '10be3daff4f55c1c6691117187f576604c6500676b556fd76622d335492796fd',
   'pachtvertrag-field-spec/v1', 'top-left/v1', 'published', true);
```

MAUI lädt im Produktivpfad ausschließlich die veröffentlichte Katalogversion und behält die eingebettete PDF nur als lokalen Testfallback. Die Web-Edge-Function lädt dieselbe PDF und Field-Spec direkt aus dem privaten Bucket; sie prüft Katalogstatus, Versionen, Prüfsummen und `topLeft`-Koordinatenursprung. Beide Pfade bewahren die bisherige Reihenfolge der digitalen Signaturen (Pächter/in 1, gegebenenfalls zweite Partei bzw. gesetzliche/r Vertreter/in, Vorstand/Verpächter) als nachfolgende Signaturseite. Fertige PDFs bleiben im Google-Drive-/`dokument`-Pfad.

## Pachtvertrag v2 bereitstellen

`v1` bleibt unverändert. `v2` entfernt Gemeinschaftsleistungen und die nicht fachlich definierte Kosten-/Erstattungsposition. Sie verwendet die bestehende Vertreterauflösung und zeichnet alle Signaturen direkt auf Seite 3.

| Wert | Festlegung |
| --- | --- |
| `template_key` | `pachtvertrag` |
| Version | `v2` |
| PDF-Pfad | `templates/pachtvertrag/v2/template.pdf` |
| Field-Spec-Pfad | `templates/pachtvertrag/v2/field-spec.json` |
| `field_spec_version` | `pachtvertrag-field-spec/v2` |
| `renderer_version` | `top-left/v1` |
| PDF SHA-256 | `bf97836eda617fc31bac5fee7ee2ddda7b47c60f0b29d8398776be8261db693e` |
| Field-Spec SHA-256 | `b669d7eee42809079f827fc76f1d03e1cc9bbc2570870e8963de278c99d4330f` |

Nach dem Upload beider Dateien wird v1 deaktiviert und v2 veröffentlicht:

```sql
update public.pdf_template_catalog
set aktiv = false
where template_key = 'pachtvertrag' and aktiv = true;

insert into public.pdf_template_catalog
  (template_key, version, storage_bucket, storage_path, content_type, checksum,
   field_spec_path, field_spec_checksum, field_spec_version, renderer_version, status, aktiv)
values
  ('pachtvertrag', 'v2', 'pdf-templates', 'templates/pachtvertrag/v2/template.pdf', 'application/pdf',
   'bf97836eda617fc31bac5fee7ee2ddda7b47c60f0b29d8398776be8261db693e',
   'templates/pachtvertrag/v2/field-spec.json',
   'b669d7eee42809079f827fc76f1d03e1cc9bbc2570870e8963de278c99d4330f',
   'pachtvertrag-field-spec/v2', 'top-left/v1', 'published', true);
```
