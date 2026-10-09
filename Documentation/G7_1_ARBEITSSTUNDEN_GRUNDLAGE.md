# G7.1 – Arbeitsstunden: technische Grundlage

## Geänderte und neue Dateien

- `KGV.Web/features/work-hours/work-hours-types.ts`: gemeinsame Verträge für Arbeitsstunden, Verlauf und Pflichtstundenübersicht.
- `KGV.Web/repositories/work-hours/work-hours-repository.ts`: saisonbezogenes Lesen von Arbeitsstunden, Verlauf und der zentralen Pflichtstunden-View.
- `KGV.Web/services/work-hours/work-hours-service.ts`: lesender UI-Service.
- `KGV.Web/app/page.tsx`: lesende Arbeitsstundenpfade verwenden Service und Repository; die Anzeige der Pflichtstunden bezieht ihre Werte ausschließlich aus `v_pflichtstunden_uebersicht`.
- `supabase/migrations/20261008120000_g7_work_hours_foundation.sql`: User-INSERT-Policy und Versionierung des vorhandenen Prüfverlaufs.

## Fachliche Regeln

Ein eigener User-Eintrag wird als `offen` und nicht freigegeben gespeichert. `genehmigt_von` und `genehmigt_am` bleiben dabei leer. Die zentrale View zählt ausschließlich freigegebene Stunden als geleistete Stunden; offene Einträge reduzieren die offizielle Reststundenanzeige nicht.

Die administrative Erfassung erfolgt über die effektive Berechtigung `ManageWorkHours` und wird direkt genehmigt gespeichert.

## Bekannte Backend-Abweichungen

`arbeitsstunde_pruefverlauf` wird mit dem vorgefundenen Übergangszustand versioniert: `ON DELETE CASCADE` kann keinen dauerhaften Löschverlauf bewahren und SELECT/INSERT für alle authentifizierten Benutzer ist für ein Audit zu weit gefasst.

## Bewusst verschoben

- **G7.2:** Bearbeiten und Löschen eigener offener Einträge sowie die zugehörige RLS.
- **G7.4:** atomare Review-RPC, Freigeben/Ablehnen/Korrigieren/Löschen, Audit-Härtung, FK-Umbau und DELETE-Locking.
- **G7.5:** weitergehende Darstellung von Fehlbetrag und Pflichtstunden.

## G7.2 – Eigene offene Arbeitsstunden

Ein normaler User darf eigene Arbeitsstunden mit Datum, Stunden und Art der Arbeit erfassen und ausschließlich eigene offene, nicht freigegebene Einträge in genau diesen drei Feldern bearbeiten. Genehmigte und abgelehnte Einträge bleiben lesbar. Es gibt keine User-Löschfunktion.

Die UPDATE-RLS prüft den alten und neuen fachlichen Zustand. Ein zusätzlicher Trigger schützt unveränderliche Felder und erlaubt die Pflege der Lock-Felder nur über den vorhandenen Browser-Edit-Lock. Administrative Erfassung sowie Prüf- und Löschworkflow bleiben G7.3 beziehungsweise G7.4 vorbehalten.

## G7.3 – Administrative Arbeitsstundenerfassung

Die administrative Erfassung ist an die effektive Permission `ManageWorkHours` gebunden; `EditAllMembers` spielt in der Arbeitsstundenlogik keine Rolle. Der Server wertet dazu die zentrale Rollen-Grundmaske samt benutzerspezifischen Grants und Revocations aus. Diese Permission gewährt in G7.3 ausschließlich das Lesen sowie den administrativen INSERT. Für administrative Einträge setzt ein Trigger `status = genehmigt`, `freigegeben = true`, den aktuell verknüpften Bearbeiter als `genehmigt_von` sowie den aktuellen Zeitpunkt als `genehmigt_am`; vom Client übermittelte Genehmigerwerte werden nicht übernommen.

Web und MAUI verwenden dieselbe effektive Permission. Die administrative Erfassung funktioniert für den aktuell ausgewählten Haupt- ebenso wie Nebenmitglied-Kontext. Eigene Einträge ohne `ManageWorkHours` bleiben unverändert offen und nicht freigegeben. Die Pflichtstundenwerte stammen weiterhin ausschließlich aus `v_pflichtstunden_uebersicht`; der Prüfworkflow wird in G7.4 über die zentrale Review-RPC abgesichert.

## G7.4 – Prüfung, Audit und Concurrency

Offene Arbeitsstunden werden ausschließlich über die atomare RPC `review_arbeitsstunde` geprüft. Sie verlangt die effektive Permission `ManageWorkHours`, einen Pflichtkommentar und akzeptiert nur Freigeben, Ablehnen, Korrigieren oder Löschen. Die RPC sperrt den Datensatz mit `FOR UPDATE`, prüft den offenen Vorzustand sowie den Demo-/Reviewer-Scope erneut und schreibt Statusänderung und Audit in derselben Transaktion.

Prüfsnapshots liegen migrationssicher als `jsonb` vor. Der Fremdschlüssel mit `ON DELETE CASCADE` ist entfernt; daher bleibt ein Lösch-Audit mit vollständigem Vorher-Snapshot dauerhaft erhalten. Direkte Audit-INSERTs und freie Review-UPDATE/DELETE-Rechte sind gesperrt. Der Browser-Edit-Lock bleibt für die Web-UX bestehen; der bestehende globale MAUI-Review-Lock bleibt als UX-Koordination erhalten, die Datenintegrität beruht jedoch auf der RPC und der Datenbank-Zeilensperre.

## G7.5 – Pflichtstunden, Befreiung und Fehlbetrag darstellen

Web und MAUI stellen Sollstunden, freigegebene Stunden, offene Stunden, Euro je Fehlstunde und Fehlbetrag direkt aus `v_pflichtstunden_uebersicht` dar. Befreiung, Regelgrund, Wartungsvertrag, Altersbefreiung sowie Eintritt im Saisonjahr oder zweiten Halbjahr werden als von der View gelieferter Status erläutert; beide Clients berechnen weder Stunden noch Fehlbeträge selbst.

Die Arbeitsstundenübersicht liest für den gewählten Mitgliedskontext die View-Zeile über `mitglied_id`. Damit erhält ein Nebenmitglied seine eigene Zeile; der Home-Dashboard-Pfad behält seine separate Abfrage über `hauptmitglied_id` bei.
