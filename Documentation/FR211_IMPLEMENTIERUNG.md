# FR2-11 – Auswertungen und Export

Basis: FR2-10, `d945f914eecd5cc10c6df1abe894b65759c9d45b`, DemoDB `ymxhszxfutjnljiuciyj` mit 290 Migrationen bis `20261124205315`.

## Umsetzung

- Sechs bestehende Exportdefinitionen bleiben erhalten. Der öffentliche Gateway arbeitet als SECURITY INVOKER und setzt Auswertungs- und Exportfachrecht sowie die Leserechte der jeweiligen Datenbereiche voraus.
- Fest vorgegebene Quellen, Parameter und Ausgabespalten; Metadaten dürfen den Umfang nicht erweitern. Technische Serienbriefzuordnungen nur mit zusätzlichem Serienbriefrecht. Öffentliche Roh-RPCs und alter View-Gateway werden gesperrt; die zwei Options-RPCs erhalten Rechteprüfungen.
- Die vorhandenen Quellabfragen werden anhand überprüfter Quelltext-Hashes in ein nicht von PostgREST exponiertes Schema kopiert. Voraussetzung: dieses Schema darf auch künftig nicht in der Data API freigegeben werden. Auf SQL-Ebene benötigt authenticated zur Invoker-Ausführung USAGE/EXECUTE im privaten Schema; die Benutzeranmeldung über die normale Data API ist kein direkter SQL-Zugang.
- JAE-XLSX erhält einen eigenen Export-RPC. Die operativen Lesefunktionen bleiben erhalten; die Berechnungskette wird als Invoker ausgeführt. Legitim lesbare Daten können grundsätzlich clientseitig gespeichert werden.
- Web und MAUI: Fachrechtsprüfungen, CSV-Formelschutz, erlaubte Optionsquellen, keine direkte Legacy-View-Ausführung. MAUI entfernt personenbezogene Export-Diagnostik und verwendet eindeutige Cache-Dateinamen. PDF-Edge reduziert Fehlerdetails und korrigiert Feldverträge.
- Schutzlimits: 8 KiB Parameter, 1000 Zeilen pro gewöhnlichem Export (kein stilles Abschneiden durch die übliche PostgREST-Grenze), 16 KiB je Ergebniszeile; maximal 5000 Elemente je JAE-Array. Bei Überschreitung Fehler statt Teilresultat. Größere Datenmengen benötigen einen späteren paginierten Exportvertrag.

## Nachweise und Einschränkungen

Die Tests im Paket sind zunächst Kandidaten. Der lokale Abschlusslauf prüft die exakten Änderungen, Regressionstests, Builds, SQL im Rollback, DemoDB-Sicherungen, Dry Run, installierten Katalog, erneut SQL im Rollback und unveränderte geprüfte Bestandsdaten. Der Bericht dokumentiert die tatsächlich erreichten Gates. Die funktionalen SQL-Prüfungen arbeiten mit einem vorhandenen nicht geschützten Demo-Konto und transaktional temporär gesetzten Rechten. Keine Rechteänderung bleibt dadurch dauerhaft bestehen. Das ersetzt keine authentifizierten HTTP- oder UI-E2E-Tests. Die vollständige fachliche Objektprüfung mehrerer Identitäten, sämtliche alternativen Statistikpfade und die FR2-Gesamtabnahme bleiben offen. Kein WPF, keine ProduktivDB, kein Merge nach main.
