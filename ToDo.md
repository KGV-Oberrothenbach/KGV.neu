# ToDo – KGV-Software

Letzte Aktualisierung: 2026-10-17 (offene WEB-Arbeitsstunden- und Wartungsvertragsthemen konsolidiert)

Diese Datei enthält ausschließlich offene, aktuelle Aufgaben. Erledigte Punkte, historische Abgleiche und frühere Zielbildtexte werden nicht fortgeführt.

## Pflege der ToDo

- Neue Punkte immer dem passenden Themenbereich zuordnen.
- Nach jedem erfolgreich abgeschlossenen Auftrag gruppieren, committen und auf den aktuellen Arbeitsbranch pushen.
- Keine erledigten Punkte in dieser Liste führen; bei Abschluss entfernen oder in die zugehörige Dokumentation bzw. Release-Notiz übernehmen.
- Punkte unter „manuell eingetragene Punkte“ fachlich eindeutig einem passenden Themenbereich zuordnen und anschließend den Sammelbereich nur für nicht eindeutig zuordenbare Punkte verwenden.

## WEB App

### Kopfbereich und Markenauftritt

- [ ] Das bisherige „K“-Logo durch das echte KGV-Logo ersetzen.

### Kommunikation

- [ ] Mailversand direkt aus der WEB-App ermöglichen.

### Kleine UI- und Kosmetikfehler

- [ ] Kleine sichtbare UI-Fehler (Beschriftungen, Abstände, Darstellung und ähnliche Auffälligkeiten) gesammelt aufnehmen und priorisiert bereinigen.
- [ ] Nur funktional relevante WEB-Warnungen und Restfehler nachziehen; keine unnötigen Großumbauten.

### Verträge und Dokumente

- [ ] Pachtvertrag fachlich weiter ausarbeiten und finalisieren.
- [ ] Im Pachtvertrag separate, zusätzlich bearbeitbare Felder vorsehen.

### Wartungsverträge

- [ ] WEB-Wartungsvertragsübersicht vereinfachen: Startansicht nur `Titel | Belegung | Arbeitsstundenwirkung`.
- [ ] Klick auf einen Wartungsvertrag öffnet denselben Detail-View wie beim Bearbeiten, zunächst ohne Bearbeitungsfunktion.
- [ ] Im Detail-View Mitglieder einem Wartungsvertrag zuordnen oder abwählen können.
- [ ] Wartungsvertrag optional auf eine Saison befristen können.
- [ ] Signaturpfad einführen: Für jedes zugeordnete Mitglied ein eigenes unterschreibbares Dokument erzeugen, auch bei demselben Wartungsvertrag.

### Arbeitsstunden

- [ ] **Priorität hoch – Fehler:** In „Arbeitsstunden freigeben“ erscheint trotz berechtigtem Prüf-/Freigabe-Kontext „Nur eigene offene Arbeitsstunden dürfen bearbeitet werden.“ Prüfen, warum der Prüf-/Freigabe-View offenbar den falschen Besitz-/Berechtigungskontext verwendet.
- [ ] Arbeitsstunden-Prüfprozess im Realbetrieb weiter absichern.

## Technische Qualität und Release

- [ ] Funktional relevante technische Restwarnungen nachziehen.
- [ ] Google-Play-Test- und Release-Reife prüfen und fehlende Anforderungen ergänzen.
- [ ] Aktuelle Release-Skripte `AWR.bat` und `A-WR.bat` mit realen Releases und Versionsflüssen verifizieren.
- [ ] Produktiv- und Testreife durch reale Installations-, Update- und Geräteprüfungen erhöhen.

## Später / Neuentwicklung

- [ ] Jahresabschluss in MAUI umsetzen (Details: `Documentation/MAUI_JAHRESABSCHLUSS_UMSETZUNGSPLAN.md`).
