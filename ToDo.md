# ToDo – KGV-Software

## Vereinsfunktionen

- Vereinsunterzeichner aus einer gültigen Funktionsbesetzung ableiten und Person plus Funktion als Vertrags-Snapshot speichern.

## Kommunikation und Dokumente

- Gemeinsamen WEB-/MAUI-Kommunikationsworkflow abschließen: verbleibende K4-Teile sowie Mail-/Druckentscheidung, Versand, Druck, Archivierung und weitere Dokumenttypen gemäß Documentation/KOMMUNIKATIONS_WORKFLOW.md.
- Mailversand direkt im Web über denselben Backendpfad wie MAUI fertigstellen: Empfängervorbelegung, Betreff/Text, mehrere Anhänge, Versand-/Fehlerstatus, Retry ohne doppelte Fachaktion und Versandnachweis.
- DEMO-Testempfänger und reale VDB-Empfänger im End-to-End-Betrieb prüfen.
- Druckworkflow und Archivierung im Kommunikationsvorgang abschließen.
- Wartungsvertrags-Dokumentworkflow fachlich vervollständigen: freigegebene Vorlage samt Platzhalter-/Datenquellen festlegen, Signaturparteien und Reihenfolge bestimmen, danach Prepared-Contract-Flow/Generator/Web-/MAUI-Signaturstrecke anschließen; PDF-Erzeugung, Vorschau, Signaturfreigabe, letzte Prüfung, Abschließen/Verwerfen und Archivierung bleiben offen. Die technische Zuordnungsbindung, Duplikatsicherung, Trigger/RPC, Batch-Teilfehler und manuelle Workflow-Vorbereitung sind implementiert; Migration noch nicht angewendet.

## Verträge

- Pachtvertrag fachlich finalisieren; zusätzliche bearbeitbare Vertragsfelder gegen den Bedarf prüfen.
- Mitgliedsantrag- und Pachtvertragsabläufe inklusive Vertreter-/Nebenmitgliedslogik, Vorschau, Signatur und End-to-End-Finalisierung weiter abgleichen.

## WEB

- Eigenen rechtegesteuerten Bereich Meine Daten / Profil umsetzen.
- Verbliebene funktionale Detailabweichungen zu MAUI bei Mitgliedsstammdaten, Nebenmitgliedern, Benutzerrechten, Arbeitsstunden, Dokumenten und Vereinskonfiguration gezielt abgleichen.
- Parzellenprotokoll nach Bereitstellung der Serverfunktion produktiv als PDF-/Ablage-End-to-End testen.
- Echtes KGV-Logo integrieren.
- Kleine sichtbare UI-Fehler und funktional relevante Web-Warnungen priorisiert bereinigen.

## Arbeitsstunden

- Arbeitsstunden-Prüfprozess im Realbetrieb testen und absichern, insbesondere Rechte, Sperren, Kommentare und Verlauf.
- Restliche MAUI-Abweichungen bei Pflichtstunden-, Altersregeln sowie Review-Filterung/Gruppierung schließen.

## Technische Qualität und Release

- AWR.bat mit realen Releases und Versionsflüssen verifizieren.
- Google-Play-Test-/Release-Reife sowie reale Installations-, Update- und Gerätetests durchführen.
- Funktional relevante technische Warnungen und Release-Restfehler nachziehen.
- Responsive Bedienung, Barrierefreiheit und Parallelbearbeitung mit realen Geräten/Browsern testen.

## Später / Neuentwicklung

- Jahresabschluss in MAUI umsetzen; Details: Documentation/MAUI_JAHRESABSCHLUSS_UMSETZUNGSPLAN.md.
