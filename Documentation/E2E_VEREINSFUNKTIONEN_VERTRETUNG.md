# Phase 11 – E2E Vertretung / Vollmacht / Vereinsfunktionen

## Ergebnis und Architektur

Die bestehende Architektur bleibt unverändert maßgeblich: `vereinsfunktion` definiert eine stabile fachliche Funktion, `vereinsfunktion_besetzung` bindet sie zeitlich an ein Mitglied, `vereinsfunktion_vertretung` überträgt genau diese Besetzung zeitlich befristet und `vereinsfunktion_verwendung` ordnet konkrete Zwecke zu. Technische App-Rollen steuern Verwaltung und Navigation, ersetzen aber keine Funktion, Besetzung oder zweckgebundene Handlung.

Geschäftslogik verwendet IDs, den unveränderlichen technischen Schlüssel, Zeiträume und Verwendungszwecke. Sichtbare Funktionsbezeichnungen und Personennamen dienen nur Anzeige und Snapshot; es gibt keine Vorsitzenden-, Amts- oder Namensheuristik.

## Technische Schlüssel und Funktionen

`vereinsfunktion.schluessel` ist eindeutig und nach Anlage serverseitig unveränderlich. Die Bezeichnung darf geändert und die Funktion aktiviert oder deaktiviert werden. Bereits eingefrorene Vorgänge behalten ihre damalige Bezeichnung. `erlaubt_mehrfachbesetzung` entscheidet, ob Zeiträume mehrerer Besetzungen überlappen dürfen; die Umschaltung auf Einzelbesetzung wird bei vorhandener Überlappung serverseitig abgewiesen.

Eine Deaktivierung entfernt die Funktion aus neuen aktuellen Auflösungen. Sie löscht weder Besetzungen noch historische Snapshots. Die Legacy-Tabelle `impressum_funktion_slot` ist schreibgeschützt.

## Besetzungen und zeitliche Gültigkeit

Eine Besetzung besitzt `gueltig_ab` und optional `gueltig_bis`. Der Zustand ist am Stichtag:

- aktuell: Beginn erreicht, Ende noch nicht überschritten,
- zukünftig: Beginn liegt nach dem Stichtag,
- historisch: Ende liegt vor dem Stichtag.

Funktion, Mitglied und Beginn sind nach Anlage unveränderliche historische Identität. Zum Wechsel wird die alte Besetzung beendet und eine neue angelegt. Bei Einzelbesetzung sperrt ein serverseitiger, durch Zeilensperre serialisierter Trigger jede zeitliche Überschneidung; Mehrfachbesetzung liefert alle gültigen Personen in stabiler Ordnung und wählt keine zufällige erste Person.

## Vertretung

Eine Vertretung referenziert eine vertretene und eine vertretende vorhandene Besetzung. Der Zeitraum muss vollständig innerhalb beider Besetzungszeiträume liegen; Selbstvertretung ist verboten. Änderungen einer Besetzung dürfen eine vorhandene Vertretung nicht nachträglich ungültig machen.

Phase 11 schließt zwei Integritätslücken:

1. Vertretene Besetzung, vertretende Besetzung und Beginn einer bestehenden Vertretung werden unveränderlich. Zum Wechsel wird die Vertretung beendet und neu angelegt.
2. Für dieselbe vertretene Besetzung darf sich kein Vertretungszeitraum mit einem anderen überschneiden. Die Prüfung ist serverseitig und gegen konkurrierende Inserts serialisiert.

Resolver betrachten nur eine am Stichtag gültige Vertretung mit gültiger eigener Vertreterbesetzung und aktiver Vertreterfunktion. Es gibt keine rekursive oder transitive Vertretung. Ist die Vertreterquelle nicht mehr gültig, erhält sie kein Recht mit leerer Provenienz; die vertretene Besetzung bleibt nach der bestehenden direkten Fachregel maßgeblich.

## Verwendungszwecke und Berechtigungen

Produktiv vorhandene Zwecke sind:

- `impressum`
- `vertragsunterzeichnung`
- `vertragsversand`
- `mitgliedsaufnahme_abstimmung`
- `mitgliedsaufnahme_entscheidung`
- `wartungsvertrag_entscheidung`

Eine Verwendung ist je Funktion und Zweck eindeutig. `mitgliedsaufnahme_berechtigungen` löst direkte Besetzungen und genau eine gültige Vertretung auf und speichert die ausgeübte sowie vertretene Quelle. Phase 11 korrigiert dabei den Fall einer später deaktivierten Vertreterfunktion: Sie berechtigt den Vertreter nicht mehr und erzeugt keine unvollständige Provenienz.

Mitgliedsaufnahme-Entscheidungen verlangen `mitgliedsaufnahme_entscheidung`; eine technische Rolle `vorstand` oder `admin` genügt nicht. Wartungsvertrags-Folgeentscheidungen verlangen `wartungsvertrag_entscheidung`, einschließlich gültiger Vertretung. Vertragsunterzeichnungen verlangen `vertragsunterzeichnung`. Administrative Start- und Pflegeberechtigungen bleiben davon getrennt.

## Vertragsunterzeichnung und Snapshot

`start_prepared_contract_signature` löst die aktuell gültigen Zeichnungsquellen serverseitig auf. Eine aktive Vertretung ersetzt dabei genau die vertretene Zeichnungsbesetzung. Mindestens zwei verschiedene natürliche Personen sind erforderlich.

Person und sämtliche Berechtigungsquellen werden revisionsgebunden in `prepared_contract_signatur_snapshot_person` und `prepared_contract_signatur_snapshot_quelle` eingefroren. Gespeichert werden Funktion, sichtbare Bezeichnung, Besetzung und Zeitraum sowie bei Vertretung deren ID, Zeitraum und vertretene Quelle. Resume lädt ausschließlich diesen Snapshot. Das Speichern einer Signatur akzeptiert nur eine Person und Quelle aus demselben Snapshot; zwei Vorstandssignaturpositionen dürfen nicht dieselbe natürliche Person verwenden.

Eine spätere Umbenennung, Deaktivierung, Amtsbeendigung oder Änderung der Vertretung verändert den laufenden Pacht-, Wartungs- oder Mitgliedsantragsvorgang nicht. Ein neuer Vorgang verwendet den dann gültigen Stand. Positions- und Snapshot-Unique-Constraints verhindern Duplikate bei Retry.

## Mitgliedsaufnahme

Für `mitgliedsaufnahme_abstimmung` wird beim Start der Vorstandsbearbeitung ein eigener Personen-/Berechtigungssnapshot erzeugt. Stimmen bleiben an diesen historischen Snapshot gebunden. Die abschließende Entscheidung prüft dagegen aktuell `mitgliedsaufnahme_entscheidung`. Ein normales Vorstands- oder Administratorkonto ohne passende Funktion beziehungsweise Vertretung darf nicht entscheiden.

## Impressum

`impressum_kontakte()` projiziert ausschließlich aktive Funktionen mit Verwendung `impressum` und deren heute gültige Besetzungen. Gruppen, Reihenfolge, Hauptkontakt und Freigabe der ladungsfähigen Anschrift sind strukturierte Metadaten der Verwendung. Name, Funktion, Telefon und – nur bei Freigabe – Anschrift stammen aus der aktuellen Besetzung; keine Person ist hartcodiert. Vertretungen werden nicht automatisch ins Impressum übernommen. Historische Vertrags-PDFs verwenden ihre Snapshots und werden durch einen Impressumswechsel nicht neu berechnet.

## Web und MAUI

Web verwaltet Funktionen, Aktivstatus, Mehrfachbesetzung, aktuelle/zukünftige/historische Besetzungen, Vertretungen, Zwecke und Impressumsmetadaten. Phase 11 sperrt bei bestehenden Besetzungen und Vertretungen die historischen Identitätsfelder im Editor; das Ende bleibt bearbeitbar.

Die vorhandene MAUI-Seite verwendet dieselben Backendtabellen und Regeln. Es wurde keine neue parallele Verwaltung gebaut. Auch dort sind Identität und Beginn bestehender Besetzungen/Vertretungen nicht mehr editierbar. Prepared-Vertragsworkflows greifen in Web und MAUI auf denselben serverseitigen Snapshot zu.

Der MAUI-Vollbuild bleibt wegen 40 bekannten fachfremden Altfehlern in anderen Seiten rot. In der geänderten Vereinsfunktionsseite verbleibt kein Buildfehler; vorhandene ImageSharp-Advisories und Nullability-Warnungen sind nicht Teil von Phase 11.

## Retry, Resume und Fehlerfälle

Wiederholte Anlage derselben Verwendung scheitert am Unique-Constraint. Einzelbesetzungs- und Vertretungsüberschneidungen, Selbstvertretung, Zeiträume außerhalb der Besetzungen sowie historische Identitätsänderungen werden serverseitig abgewiesen. Snapshot-Personen, -Quellen und Signaturpositionen sind revisionsgebunden eindeutig.

Geprüft sind aktuelle, zukünftige, historische und mehrfache Besetzungen, gültige und abgelaufene Vertretung, Vertretungsüberschneidung, Zweckbindung, Admin-Abgrenzung, Snapshotstabilität, Funktionsumbenennung, deaktivierte Vertreterfunktion und die Regressionen der Phasen 8 bis 10.

## Migration und Betrieb

`20261028140000_phase11_vereinsfunktionen_vertretung_e2e_haertung.sql` enthält ausschließlich die Vertretungsintegrität und die zweckgebundene Mitgliedsaufnahme-Auflösung. Sie ist versioniert, aber nicht ausgeführt. Es wurden keine Edge Functions deployt, keine Produktionsdaten geändert, keine Templates aktiviert und kein Release erzeugt.

**Phase 11 – E2E Vertretung / Vollmacht / Vereinsfunktionen = 100 %.**
