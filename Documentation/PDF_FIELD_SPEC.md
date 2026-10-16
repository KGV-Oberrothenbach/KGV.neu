# PDF Field-Spec

Neue Specs verwenden `schema_version: "kgv-pdf-field-spec/v1"`. Das technische Format ist unabhängig von der dokumentbezogenen Katalogversion `field_spec_version`.

```json
{"schema_version":"kgv-pdf-field-spec/v1","coordinate_system":"top-left-points","settings":{"coordinateOrigin":"topLeft"},"fields":[{"name":"mitglied_name","type":"text","page":1,"rect":{"x":28.3,"y":154,"w":134.3,"h":9.5}}]}
```

Koordinaten sind PDF-Points mit Ursprung links oben; `x` wächst nach rechts, `y` nach unten, und Seiten sind 1-basiert. Zulässig sind ausschließlich `text`, `multiline`, `checkbox` und `signature`. Namen sind nicht leer und innerhalb der Spec eindeutig; Rechtecke haben `x/y >= 0` und `w/h > 0`.

Specs ohne `schema_version` bleiben als Legacy lesbar. `text` mit `flags.multiline: true` wird zu `multiline` normalisiert. Unbekannte explizite Schema-Versionen werden abgelehnt. Layout- oder Geometrieänderungen veröffentlichter Templates erfordern weiterhin eine neue Dokumentversion.

Die Field-Spec beschreibt nur Technik. Dokumentbezogene `requiredFields` gehören in den Descriptor; ob Nutzende einen Wert eingeben müssen, bleibt Fach-/UI-Validierung.

## Renderer-Vorbereitung (D3a)

`PdfTemplateRenderer` bündelt das technische Öffnen, Annotation-Entfernen und Zeichnen normalisierter `text`, `multiline` und `checkbox`-Felder. `signature` wird bewusst erkannt und übersprungen. Mitgliedsantrag und Pachtvertrag verwenden bis D3b weiter ihre bewährten spezifischen Renderpfade: Sie unterscheiden sich derzeit bei Schriftgrößen, Small-Text-Logik, Umbruch/Clipping und MA-spezifischen Beschriftungen. FieldMap bleibt dokumentfachlich, der Renderer kennt nur Feldname, Typ, Rechteck und Wert.

## D3a2: Renderprofile

`PdfTemplateRenderOptions` bildet die tatsächlich vorhandenen technischen Unterschiede ab: Schriftgrößen, Small-Font-Regel, Single-Line-Offset, Text- und Multiline-Padding sowie Checkbox-Glyphenposition. Die Profile `Mitgliedsantrag` und `Pachtvertrag` reproduzieren die bisherigen Fontgrößen und Regeln; sie liegen außerhalb des Renderers, der keine Dokumentschlüssel kennt. Beide Factories bleiben bis D3b1/D3b2 unverändert auf dem Referenzpfad.

Der bestätigte Migrationsplan lautet: (1) Renderer/Profile testen, (2) MA migrieren, (3) MA manuell abnehmen, (4) PV-Anlagen als Text-`X` explizit kompatibel behandeln, (5) PV migrieren. Der aktuelle PV-Anlagenpfad darf nicht versehentlich zu einer gerahmten Checkbox werden.
