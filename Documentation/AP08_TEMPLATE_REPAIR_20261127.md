# AP08 Vertragsvorlagen – Integrationsstand

Basis: fc228b46bd9159fb606110bd3708d1039920c0bb. Kein WPF, keine produktiven Änderungen.

- Pachtvertrag v10: Pflichtfelder, beide Flächen, Pachtbetrag und Fälligkeit, getrennte Rollen. Historische v9 bleibt unterstützt. Die zwei optionalen Felder Lagebeschreibung und Flächenbasis werden mangels verifizierter strukturierter Datenquelle nicht erfunden.
- Wartungsvertrag und Verlängerung v3: neue unveränderliche Vorlagen, keine v2-Overlays auf v3. Alte Versionen bleiben unterstützt.
- Mitgliedsantrag v6: ausschließlich Quellasset (noch inkompatibel, nicht veröffentlichen). Laufende Vorgänge 8, 9, 10 haben keine eingefrorene Vorlagenversion; 9 und 10 bereits je vier Signaturen. Der separate Antrag für Nebenmitglieder und Vertreterauflösung sind ein eigener notwendiger Workflow-Umbau. Keine alte Signatur übertragen oder überschreiben.

## Freigabestufen

Dieses Paket ändert nur den Reparaturworktree, nicht DemoDB, Storage oder Edge-Deployment. Vor Veröffentlichung müssen echte Renderer-Ausgaben und CI geprüft, laufende Vorgänge/Versionen abgeglichen, die kompatiblen Clients bereitgestellt und neue Storage-Pfade auf Nichtvorhandensein geprüft werden. Keine bestehende Vorlage überschreiben. Version v6 bleibt inaktiv, bis der gesamte Workflow kompatibel ist. Produktionsveröffentlichung ausschließlich im späteren freigegebenen 1-Klick-Paket.

## Weitere Befunde und Freigabegrenzen

43 unsichtbare Textausgaben im Pachtvertragsasset und eine im Mitgliedsantragsasset entfernt. Jede Entfernung wurde mit unverändertem Seitenrender bei 72 und 144 dpi kontrolliert; anschließend erneuter Vergleich der gespeicherten Dateien. Die visuelle Vertragsfassung ist gleich geblieben, aber die Textsuche liefert nicht mehr die überdeckten alten Pachtparteibezeichnungen.

Die Pachtvertrag-Fassung der ausgehändigten Kleingartenordnung und die Anlage-/Unterlagennachweise müssen noch in der realen Dokumenterzeugung überprüft werden. Eine fehlende strukturierte Datenquelle darf nicht durch erfundene Angaben ersetzt werden. Die v6-Integration des einzelnen Mitgliedsantrags, die explizite Vorlagenversionsbindung laufender Signaturvorgänge sowie gegebenenfalls mehrere gesetzliche Vertreter bleiben offen. Dieses Paket ist ein getesteter Vorbereitungsschritt, keine AP08-Gesamtfreigabe.
