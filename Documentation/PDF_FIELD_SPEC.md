# PDF Field-Spec

Neue Specs verwenden `schema_version: "kgv-pdf-field-spec/v1"`. Das technische Format ist unabhängig von der dokumentbezogenen Katalogversion `field_spec_version`.

```json
{"schema_version":"kgv-pdf-field-spec/v1","coordinate_system":"top-left-points","settings":{"coordinateOrigin":"topLeft"},"fields":[{"name":"mitglied_name","type":"text","page":1,"rect":{"x":28.3,"y":154,"w":134.3,"h":9.5}}]}
```

Koordinaten sind PDF-Points mit Ursprung links oben; `x` wächst nach rechts, `y` nach unten, und Seiten sind 1-basiert. Zulässig sind ausschließlich `text`, `multiline`, `checkbox` und `signature`. Namen sind nicht leer und innerhalb der Spec eindeutig; Rechtecke haben `x/y >= 0` und `w/h > 0`.

Specs ohne `schema_version` bleiben als Legacy lesbar. `text` mit `flags.multiline: true` wird zu `multiline` normalisiert. Unbekannte explizite Schema-Versionen werden abgelehnt. Layout- oder Geometrieänderungen veröffentlichter Templates erfordern weiterhin eine neue Dokumentversion.

Die Field-Spec beschreibt nur Technik. Dokumentbezogene `requiredFields` gehören in den Descriptor; ob Nutzende einen Wert eingeben müssen, bleibt Fach-/UI-Validierung.
