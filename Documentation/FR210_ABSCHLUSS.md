# FR2-10 - Vereinsverwaltung unter Fachrechte 2.0

Stand: 24.11.2026

## Ergebnis

Codeumstellung, lokale Tests, Builds und DemoDB-Migration
erfolgreich abgeschlossen.

## Ausgangsstand

- Arbeitsbranch: work/fr2-10-vereinsverwaltung-20261124
- Basiscommit: 77357c8209c08b9de5b980bfc6401f49dfcbf198
- DemoDB: ymxhszxfutjnljiuciyj
- Migration: 20261124205315_fr2_10_vereinsverwaltung.sql
- SHA256: B7A924C8C556D503CC8A787C8CE33668BAF7C21042555EF0A875A6EB4433C3FD

## Fachrechte

- termin.verwalten
- bekanntmachung.verwalten
- vereinsfunktion.verwalten
- verein.konfiguration
- saison.verwalten

## Lokale Nachweise

- Web-Contract-Tests: 126/126
- .NET-Tests: 458/458
- Keine fehlgeschlagenen oder uebersprungenen Tests
- Core Release: PASS
- Infrastructure Release: PASS
- Web Build: PASS
- MAUI Android Release: PASS
- SixLabors-Lizenz vorhanden und Git-ignoriert
- Git Diff Check: PASS

## Datenbanknachweise

- DemoDB vor Migration: 289 Migrationen
- Rollen-, Schema- und Datensicherung erstellt
- Sicherungsdateien mit SHA256 kontrolliert
- SQL-Vorpruefung mit Rollback: PASS
- Dry Run: ausschliesslich FR2-10
- DemoDB nach Migration: 290 Migrationen
- Bestandsdaten der acht betroffenen Tabellen unveraendert
- 71 vorhandene Datensaetze erhalten
- RLS und Tabellenrechte: PASS

## Unabhaengige Katalogpruefung

Direkte Abfragen der installierten DemoDB nach Migration:

- 15 neue FR210-Policies vorhanden
- Vier FR210-Schutztrigger aktiv
- Fachrechtspraedikate kontrolliert
- Lesezugriff fuer sichtbare Termine und Bekanntmachungen erhalten
- Impressumsbezogene Leser-Policies erhalten
- Keine direkten EXECUTE-Rechte auf interne Schutzfunktionen
  fuer anon und authenticated
- Saisonidentitaet und vergangene Saisons durch Trigger geschuetzt
- Berechnete, gepruefte und abgeschlossene Jahresabschluesse
  durch Saison-Schutztrigger beruecksichtigt
- Historische Vereinsfunktionen, Besetzungen und Vertretungen
  gegen physisches Loeschen geschuetzt

## Offene Nachweise

- Authentifizierte HTTP-E2E gesammelt nach FR2-11
- Fachliche positive und negative Laufzeittests
- Unabhaengige Persistenzpruefungen der HTTP-Schreiboperationen
- Konkurrenzfall bei gleichzeitig neu angelegtem Jahresabschluss
- UI-E2E und Deployment
- Wiederherstellungstest der SQL-Sicherung nicht ausgefuehrt
- Compilerwarnungen und Web-Bundle-Warnung vorhanden

## Abgrenzung

- ProduktivDB unveraendert
- WPF nicht bearbeitet
- Keine Produktionsfreigabe
- Kein automatischer Merge nach main