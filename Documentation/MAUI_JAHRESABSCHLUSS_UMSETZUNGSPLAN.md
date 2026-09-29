# MAUI – Umsetzungsplan Jahresabschluss

**Stand:** 27. September 2026  
**Status:** Phase 0 bis Phase 5 vorbereitet; produktive Fachfreigabe steht aus

## Ziel

Der Jahresabschluss wird als revisionsfähiger, unveränderlicher Snapshot pro `Saison` umgesetzt. Laufende Daten wie Belegungen, Zähler, Ablesungen, Arbeitsstunden und Rechnungen bleiben unverändert. Die MAUI-App führt berechtigte Nutzer durch Erfassen, Berechnen, Prüfen, Abschließen und Exportieren.

`Saison → Quellen erfassen → berechnen → prüfen → abschließen → Snapshot/Export`

Ein Abschluss ist nie ein Datenumzug und überschreibt keine historische Ablesung.

## Fachliche Leitplanken

- Genau ein `jahresabschluss` je `saison_id`; der Zeitraum ist immer 01.01.–31.12.
- Wasserablesungen Ende Oktober bedeuten November/Dezember = 0, nicht ein verkürztes Jahr.
- Strom außerhalb der Saison wird nach bestätigter Vereinsregel vernachlässigt.
- Zähler- und Pächterwechsel werden zeitlich segmentiert und mit Berechnungsweg gespeichert.
- Kostenart beschreibt **was** bezahlt wurde; Umlageart beschreibt **wie** verteilt wird.
- U2 wird ausschließlich nach m² auf tatsächlich verpachtete Flächen umgelegt. Vereinsgärten, Leerstand und nicht verpachtete Teilflächen erhalten keinen U2-Anteil.
- Bei Pächterwechsel mit Nachpächter wird die Pacht zeitanteilig zwischen den Belegungen berechnet. Ohne Nachpächter endet der Pachtvertrag grundsätzlich zum Jahresende.
- Beim Tod eines alleinigen Kleingartenpächters endet der Pachtvertrag gemäß § 12 Abs. 1 BKleingG mit Ablauf des auf den Todesmonat folgenden Kalendermonats – nicht nach einer dreimonatigen Sonderkündigungsfrist. Bei gemeinschaftlichem Vertrag mit Ehegatte/Lebenspartner läuft er mit der überlebenden Person fort; diese kann binnen eines Monats erklären, ihn nicht fortsetzen zu wollen (§ 12 Abs. 2 BKleingG).
- Die Pacht wird auch beim Todesfall bis zum gesetzlichen Vertragsende zeitanteilig berechnet. Mitgliedsbeiträge werden weder bei Kündigung noch bei Tod zeitanteilig berechnet, gekürzt oder zwischen Personen aufgeteilt.
- Umlagen werden bei Pächterwechsel und Todesfall anhand der tatsächlichen Belegungszeiträume zeitanteilig aufgeteilt.
- Dies gilt für U1, U2, U-S und U-W: U1 wird nach Belegungstagen, U2 nach Fläche × Belegungstage und U-S/U-W nach Belegungstagen ausschließlich für Parzellen mit dem jeweiligen Anschluss verteilt.
- Ein Pächterwechsel setzt tatsächliche Strom- und Wasserablesungen voraus. Verbrauch (V-S/V-W) wird bei Wechseln ausschließlich aus diesen Ablesungen ermittelt und niemals zeitanteilig geschätzt.
- Berechnungen erfolgen intern mit voller Dezimalgenauigkeit. Erst die endgültigen Rechnungspositionen werden auf zwei Cent gerundet; eine verbleibende Centdifferenz wird nachvollziehbar der letzten Position derselben Umlage zugeordnet.
- Vorauszahlungen können mit Datum, Betrag, Mitglied und optionaler Parzelle erfasst und im Abschluss verrechnet werden. Das Buchen von Zahlungseingängen ist in der ersten Version optional; ohne Zahlungsbuchung bleibt die vollständige Abschlussforderung offen, der Abschluss ist dennoch möglich.
- Fehler nach einem endgültigen Abschluss werden über einen separaten Korrekturabschluss mit referenzierten Differenzpositionen behandelt. Der ursprüngliche Abschluss bleibt unverändert.
- Fehlende oder rückläufige Zählerstände blockieren den Abschluss. Auffällige, technisch aber mögliche Verbräuche erzeugen eine prüfpflichtige Warnung, die berechtigt begründet und bestätigt werden muss; die hinterlegten Ablesefotos sind der Nachweis.
- Für jede Ablesung ist ein Foto verpflichtend, unabhängig davon, ob es sich um eine Zwischen-, Wechsel- oder Jahresablesung handelt.
- Der Rest einer Rechnung ist abgeleitet: `gesamtbetrag - Summe(zuordnungen)`; er wird nicht redundant gespeichert.
- Nach `abgeschlossen` dürfen Snapshots nur lesbar sein. Korrekturen erfolgen über einen später festzulegenden Storno-/Korrekturprozess, nie durch Änderung des Snapshots.

## Datenmodell und Migrationen

### 1. Stammdaten

Neue Tabellen und Verwaltung:

- `kostenart(id, bezeichnung, beschreibung, aktiv)`
- `umlageart(id, kuerzel, bezeichnung, verteilung, aktiv)`

Initiale Umlagearten:

| Kürzel | Verteilung |
| --- | --- |
| U1 | pro abrechnungsrelevantem Mitglied |
| U2 | nach abrechnungsrelevanter Fläche |
| U-S | Fixkosten auf Parzellen mit Stromanschluss |
| U-W | Fixkosten auf Parzellen mit Wasseranschluss |
| V-S | nach Stromverbrauch |
| V-W | nach Wasserverbrauch |
| EINZEL | gezielt an Parzelle oder Mitglied |
| KEINE | Vereinskosten ohne Weiterverteilung |

### 2. Erfasste Rechnungen

- `jahresabschluss_rechnung`: `saison_id`, Lieferant, Rechnungsnummer, Rechnungsdatum, Leistungszeitraum, Gesamtbetrag, Bemerkung, Erfassungsmetadaten.
- `jahresabschluss_rechnung_zuordnung`: Rechnung, Kostenart, Umlageart, Betrag, optionale Parzelle, optionales Mitglied, Bemerkung.

Regeln: Summe der Zuordnungen darf den Rechnungsbetrag nicht überschreiten; bei `EINZEL` ist mindestens eine Parzelle oder ein Mitglied Pflicht; die Anwendung zeigt den Restbetrag live.

### 3. Abschluss-Snapshot

- `jahresabschluss`: `saison_id` (eindeutig), Status (`in_bearbeitung`, `berechnet`, `geprueft`, `abgeschlossen`), `berechnung_version`, Erstell-/Abschlussmetadaten, Bemerkung.
- `jahresabschluss_position`: Abschluss, Parzelle, Mitglied, Positionstyp, Menge, Einheit, Preis, Betrag, Bemerkung sowie die verwendeten Werte aus der Saison als Snapshot.
- `jahresabschluss_verbrauch`: Ergebnisposition, Parzelle, Mitglied, Zähler, Verbrauchsart, Zeitraum, Start-/Endstand, Verbrauch, Grund (`normal`, `zaehlerwechsel`, `paechterwechsel`, kombiniert).
- `jahresabschluss_vorauszahlung`: Datum, Betrag, Mitglied, optionale Parzelle und Verwendungszweck. Die Erfassung ist optional und keine Voraussetzung für den Jahresabschluss.
- `jahresabschluss_korrektur`: Referenz auf den abgeschlossenen Ursprungsabschluss, eigene Abschlussmetadaten und begründete Differenzpositionen; keine Änderung des Ursprungssnapshots.

Alle Tabellen erhalten Fremdschlüssel, sinnvolle Indizes, Zeitstempel sowie RLS-Regeln. Der bestehende Datenbestand ist aktuell ein Einvereins-Modell und enthält kein `verein_id`; eine Mandantenzuordnung wird deshalb nicht künstlich ergänzt. Schreibrechte für Rechnungen, Berechnung und Abschluss liegen bei Vorstand/Admin; Mitglieder sehen ausschließlich ihre eigenen Abrechnungsdaten.

## Umsetzungsreihenfolge

### Phase 0 – Fachregeln verbindlich machen

Vor dem Rechenkern folgende Fälle mit Beispielwerten und erwarteten Centbeträgen festlegen:

1. **Festgelegt:** U2 wird ausschließlich auf tatsächlich verpachtete Flächen nach m² umgelegt; Vereinsgärten, Leerstand und nicht verpachtete Teilflächen bleiben außerhalb des U2-Verteilerschlüssels.
2. **Festgelegt:** Bei Pächterwechsel mit Nachpächter wird Pacht zeitanteilig verteilt; ohne Nachpächter endet der Pachtvertrag grundsätzlich zum Jahresende. Beim Tod eines alleinigen Pächters endet der Vertrag gesetzlich mit Ablauf des Folgemonats (§ 12 Abs. 1 BKleingG); bei gemeinsamem Vertrag mit Ehegatte/Lebenspartner gilt die gesetzliche Fortsetzung und Optionsfrist (§ 12 Abs. 2 BKleingG). Die Pacht wird bis zum rechtlichen Vertragsende zeitanteilig berechnet. Mitgliedsbeiträge werden weder bei Kündigung noch bei Tod zeitanteilig berechnet oder aufgeteilt. U1, U2, U-S und U-W werden zeitanteilig verteilt: U1 nach Belegungstagen, U2 nach Fläche × Belegungstage und U-S/U-W nur für angeschlossene Parzellen nach Belegungstagen. Ein Pächterwechsel setzt echte Strom- und Wasserablesungen voraus; V-S und V-W werden daraus und nie aus einer zeitanteiligen Schätzung ermittelt. Ein Beendigungsgrund einschließlich `tod` wird an `parzellen_belegung` ergänzt; aktuell speichert das Modell nur Beginn und Ende.
3. **Festgelegt:** Intern wird ohne kaufmännische Zwischenrundungen gerechnet. Erst endgültige Rechnungspositionen werden auf zwei Cent gerundet; eine Centdifferenz geht dokumentiert an die letzte Position derselben Umlage.
4. **Festgelegt:** Vorauszahlungen werden mit Datum, Betrag, Mitglied und optionaler Parzelle erfasst und von der Endforderung abgezogen; ein negativer Saldo ist Guthaben. Zahlungsbuchungen bleiben zunächst optional und sind keine Abschlussvoraussetzung.
5. **Festgelegt:** Ein Fehler nach Abschluss erzeugt einen separaten Korrekturabschluss mit eindeutig referenzierten Differenzpositionen. Der Ursprungssnapshot bleibt unverändert.
6. **Festgelegt:** Fehlende oder rückläufige Zählerstände blockieren den Abschluss. Auffällige, aber technisch mögliche Werte sind begründungspflichtige Warnungen; Ablesefotos werden als Nachweis in der Prüfung angezeigt.

Ergebnis: versionierte fachliche Testfall-Tabelle; ohne diese Regeln darf der Abschluss nicht produktiv gesperrt werden.

### Phase 1 – Datenbank, Core und Infrastruktur

**Status: umgesetzt im Arbeitsbereich; Supabase-Migration noch bereitzustellen.**

1. Supabase-Migrationen, Constraints, RLS und Seed der Umlagearten anlegen.
2. Records/DTOs, Status- und Verteilungs-Enums sowie Requests in `KGV.Core` ergänzen.
3. `ISupabaseService` um Lesepfade für Stammdaten, Rechnungen, Abschlussübersicht und Snapshots erweitern.
4. Einen dedizierten `IJahresabschlussService` in Core und die Supabase-Implementierung in Infrastructure einführen.
5. Berechnung und Abschluss als transaktionale/RPC-gestützte Operation vorbereiten: Die numerische Berechnung folgt in Phase 3; Berechnung erstellt nur ersetzbare Entwürfe, der endgültige Abschluss validiert und sperrt atomar.
6. Berechnungslogik nicht in Pages oder ViewModels duplizieren.

### Phase 2 – Rechnungserfassung und Stammdaten in MAUI

**Status: umgesetzt im Arbeitsbereich.**

1. Admin-Menü um „Jahresabschluss“ ergänzen; Saison-Kontext aus dem vorhandenen Arbeitskontext übernehmen.
2. Seiten für Kostenarten und Umlagearten mit Aktivstatus anlegen.
3. Rechnungsübersicht je Saison sowie Editor für Kopf und beliebig viele Zuordnungszeilen bereitstellen.
4. Für `EINZEL` eine Suche nach Gartennummer, Parzelle oder Mitglied statt interner IDs anbieten.
5. Restbetrag, ungültige Zuordnungen und nicht vollständig verteilte Rechnungen unmittelbar anzeigen.
6. Alle Schreibvorgänge anhand der bestehenden Berechtigungslogik absichern.

### Phase 3 – Berechnung und Prüfoberfläche

**Status: Prüfoberfläche umgesetzt; die verbindliche Snapshot-Berechnung folgt mit Phase 4.**

1. Übersichtsseite mit Abschlussstatus und Aktionen „Vorbereiten“, „Neu berechnen“, „Prüfen“ und „Abschließen“ bauen.
2. Prüfbericht mit Vollständigkeit der Ablesungen, Zählerwechseln, Pächterwechseln, fehlenden Daten, auffälligen Verbräuchen und nicht verteilten Rechnungen anzeigen.
3. Detailansicht einer Position zeigt sämtliche Verbrauchssegmente samt Zähler, Zeitspanne, Pächter und Ständen.
4. Blockierende Fehler von bloßen Warnungen trennen; Abschluss nur bei fehlerfreiem Prüfbericht und expliziter Bestätigung erlauben.
5. Jede Berechnung mit `berechnung_version` und den Eingabedaten nachvollziehbar protokollieren.

### Phase 4 – Abschluss, Auswertung und Export

**Status: endgültiger Abschluss und Snapshot-Sperre umgesetzt; Exporte folgen als Ausbau der bestehenden Exportansicht.**

1. Abschlussdialog mit Zusammenfassung, Anzahl Positionen, Gesamtsummen und endgültiger Sperr-Warnung implementieren.
2. Nach erfolgreichem Abschluss nur Snapshot-Daten für Ansicht und Export verwenden.
3. Abrechnung pro Mitglied/Pächter, Parzellenübersicht und Umlagen-/Verbrauchsübersicht als mobile Ansicht bereitstellen.
4. Bestehende Exportinfrastruktur für CSV/Excel/PDF aus den Snapshots erweitern; keine Neuberechnung im Export.
5. Download-/Anzeigerechte wie den Abschluss selbst schützen.

### Phase 5 – Qualität und Freigabe

**Status: Build und bestehender automatisierter Testbestand erfolgreich; fachliche Freigabe-Checkliste angelegt. Der Test mit anonymisierten Echtdaten und die Vorstandsabnahme stehen noch aus.**

1. Service- und Datenbanktests für alle fachlichen Testfälle aus Phase 0.
2. Integrationsfälle: Zählerwechsel, Pächterwechsel, kombinierter Wechsel, `EINZEL`, unvollständige Rechnung, leerstehende Vereinsparzelle, fehlende Ablesung und Abschluss-Sperre.
3. MAUI-Realtest auf Android und mindestens einem großen Bildschirm; Navigation, Saisonwechsel, Offline-/Fehleranzeige und Rollen testen.
4. Testabschluss mit anonymisierten Echtdaten durch Vorstand prüfen und die Ergebnisse gegen die bestehende Abrechnung vergleichen.
5. Erst danach Berechtigung zum endgültigen Abschluss in der produktiven Saison freischalten.

## Abnahmekriterien

- Keine operative Quelldaten werden beim Berechnen oder Abschließen verändert.
- Jeder Abschluss ist pro Saison eindeutig, vollständig nachvollziehbar und nach Abschluss unveränderlich.
- Summe der Rechnungszuordnungen, Umlagen und Ergebnispositionen ist in jeder Ansicht nachvollziehbar.
- Ein Verbrauchs-/Pächterwechsel lässt sich anhand gespeicherter Segmente erklären.
- Nicht berechtigte Nutzer können weder Rechnungen verändern noch einen Abschluss durchführen oder fremde Abrechnungen einsehen.
- PDF/CSV/Excel und MAUI-Ansicht enthalten identische Snapshotwerte.

## Ergebnis Phase 0

Die fachlichen Berechnungs-, Rundungs-, Vorauszahlungs-, Korrektur- und Ableseregeln sind festgelegt. Phase 1 und Phase 2 sind im Arbeitsbereich umgesetzt. Als nächster Umsetzungsschritt folgt Phase 3: Berechnung und Prüfoberfläche.
