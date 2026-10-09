# G7.1 – Arbeitsstunden: technische Grundlage

## Geänderte und neue Dateien

- `KGV.Web/features/work-hours/work-hours-types.ts`: gemeinsame Verträge für Arbeitsstunden, Verlauf und Pflichtstundenübersicht.
- `KGV.Web/repositories/work-hours/work-hours-repository.ts`: saisonbezogenes Lesen von Arbeitsstunden, Verlauf und der zentralen Pflichtstunden-View.
- `KGV.Web/services/work-hours/work-hours-service.ts`: lesender UI-Service.
- `KGV.Web/app/page.tsx`: lesende Arbeitsstundenpfade verwenden Service und Repository; die Anzeige der Pflichtstunden bezieht ihre Werte ausschließlich aus `v_pflichtstunden_uebersicht`.
- `supabase/migrations/20261008120000_g7_work_hours_foundation.sql`: User-INSERT-Policy und Versionierung des vorhandenen Prüfverlaufs.

## Fachliche Regeln

Ein eigener User-Eintrag wird als `offen` und nicht freigegeben gespeichert. `genehmigt_von` und `genehmigt_am` bleiben dabei leer. Die zentrale View zählt ausschließlich freigegebene Stunden als geleistete Stunden; offene Einträge reduzieren die offizielle Reststundenanzeige nicht.

Die spätere administrative Erfassung erfolgt über die effektive Berechtigung `ManageWorkHours` und wird direkt genehmigt gespeichert. Sie ist kein Bestandteil von G7.1.

## Bekannte Backend-Abweichungen

Die aktuelle RLS für Verwaltungszugriffe auf `arbeitsstunde` ist weiterhin rollenbasiert (`is_productive_admin_or_vorstand()`) und wertet `ManageWorkHours` noch nicht aus.

`arbeitsstunde_pruefverlauf` wird mit dem vorgefundenen Übergangszustand versioniert: `ON DELETE CASCADE` kann keinen dauerhaften Löschverlauf bewahren und SELECT/INSERT für alle authentifizierten Benutzer ist für ein Audit zu weit gefasst.

## Bewusst verschoben

- **G7.2:** Bearbeiten und Löschen eigener offener Einträge sowie die zugehörige RLS.
- **G7.3:** administrative Erfassungsmaske und Umstellung der MAUI-Auto-Freigabe auf `ManageWorkHours`.
- **G7.4:** atomare Review-RPC, Freigeben/Ablehnen/Korrigieren/Löschen, Audit-Härtung, FK-Umbau und DELETE-Locking.
- **G7.5:** weitergehende Darstellung von Fehlbetrag und Pflichtstunden.

## G7.2 – Eigene offene Arbeitsstunden

Ein normaler User darf eigene Arbeitsstunden mit Datum, Stunden und Art der Arbeit erfassen und ausschließlich eigene offene, nicht freigegebene Einträge in genau diesen drei Feldern bearbeiten. Genehmigte und abgelehnte Einträge bleiben lesbar. Es gibt keine User-Löschfunktion.

Die UPDATE-RLS prüft den alten und neuen fachlichen Zustand. Ein zusätzlicher Trigger schützt unveränderliche Felder und erlaubt die Pflege der Lock-Felder nur über den vorhandenen Browser-Edit-Lock. Administrative Erfassung sowie Prüf- und Löschworkflow bleiben G7.3 beziehungsweise G7.4 vorbehalten.
