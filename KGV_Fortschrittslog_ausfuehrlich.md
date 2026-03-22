# KGV_Fortschrittslog_ausfuehrlich

---

## 2026-03-22 – Prompt 1/5: Home nur mit Admin-Bearbeiten-Einstiegen, separate Verwaltungsviews ohne Platzhalter vorbereitet

- Aktuellen WPF-/MAUI-Istzustand vor dem Umbau erneut geprüft: `HomeView`/`HomeViewModel` zeigten bereits nur noch Listen plus separate Detailviews, `NavigationService` und `MainWindowViewModel` waren ViewModel-first organisiert, und die bestätigten Lesepfade für Home liefen über `v_startseite_arbeitseinsatz`, `v_startseite_termine` und `v_startseite_bekanntmachungen`.
- Im aktiven Repo zugleich keine belastbar verifizierten Schreibmodelle oder bestehenden Create/Update-Servicepfade für Arbeitseinsätze, Termine und Bekanntmachungen gefunden; genau deshalb wurde in diesem Block bewusst keine neue Formularlogik geraten.
- Home fachlich weiter bereinigt: auf der Startseite gibt es für normale Nutzer weiterhin nur die bisherigen Übersichtslisten; für Admin/Vorstand kommt jetzt zusätzlich eine kleine Verwaltungssektion mit drei Bearbeiten-Einstiegen hinzu, aber keine Bearbeitung direkt auf Home.
- Drei echte separate WPF-Verwaltungsviews angelegt und in die bestehende Navigation eingebunden:
  - `ArbeitseinsaetzeVerwaltungView`
  - `TermineVerwaltungView`
  - `BekanntmachungenVerwaltungView`
- Jede Verwaltungsansicht nutzt bereits das Ziel-Layout mit linker Liste und rechter Editorfläche. Wenn weder ein vorhandener Datensatz geöffnet noch `Neu` ausgelöst wurde, bleibt rechts bewusst ohne Formularfelder. Ein Doppelklick auf einen Listeneintrag öffnet rechts den Editierzustand mit den vorhandenen belastbaren Lesedaten; `Neu` öffnet denselben strukturellen Zustand leer.
- Keine Platzhalterformulare aufgebaut: weil die echten Schreibfelder/Basistabellen im aktiven Repo noch nicht sicher genug ableitbar waren, zeigt der rechte Bereich aktuell nur den geöffneten Bearbeitungszustand plus die verifizierten Lese-/Schreibpfad-Hinweise statt geratener Eingabefelder.
- Reale Datenlisten statt Platzhalter verdrahtet: `ISupabaseService`/`SupabaseService` stellen die drei bestätigten Startseiten-Lesepfade jetzt auch direkt für Verwaltungslisten bereit, sodass die neuen Views auf belastbaren Daten basieren und nicht auf Home-spezifischen Zwischenobjekten.
- Rechte entlang des vorhandenen Pfads sauber eingehängt: die Verwaltungsviews erscheinen in WPF nur für Admin/Vorstand sowohl über Home als auch in der Hauptnavigation; MAUI bleibt in diesem Block unverändert und wird nicht mit neuen Platzhalterseiten aufgebläht, profitiert aber später vom gemeinsamen Lesepfad.
- Technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach dem Block erfolgreich.
