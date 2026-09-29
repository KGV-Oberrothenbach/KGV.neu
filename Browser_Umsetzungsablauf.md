# Browser-Umsetzungsablauf

Stand: 28.09.2026

## Ziel und Bewertungsregel

Die MAUI-App ist die fachliche Referenz fuer Berechtigungen, Datenfelder,
Validierungen, Statuswechsel und Dokumentablaeufe. Die Browser-App soll jedoch
nicht die mobile Seitennavigation kopieren. Auf einem grossen Bildschirm gilt:

- Uebersicht und Detail duerfen gleichzeitig sichtbar sein.
- Listen werden als filter- und sortierbare Tabellen dargestellt.
- Ein ausgewaehlter Datensatz bleibt beim Wechsel in Unterbereiche als Kontext
  erhalten.
- Editoren stehen rechts neben der Liste oder in einem klar abgegrenzten
  Arbeitsbereich.
- Mehrstufige Fachablaeufe verwenden Reiter oder einen Schrittindikator.
- Unter 700 Pixeln faellt die Darstellung wieder auf einen mobilen
  Einzelseitenablauf zurueck.

Status:

- **Fertig**: fachlich gleichwertig und fuer den PC sinnvoll umgesetzt.
- **Teilweise**: Datenpfad oder Grundfunktion vorhanden, aber MAUI-Inhalt,
  Validierung oder Bedienung ist noch nicht vollstaendig.
- **Fehlt**: noch keine fachlich nutzbare Browserseite vorhanden.
- **Integriert**: mehrere mobile MAUI-Views werden am PC bewusst in einer
  gemeinsamen Master-Detail-Seite abgebildet.

## 1. Zugang, Verein und Anmeldung

| Nr. | MAUI-View | Browser-Entsprechung | Status | Fachlicher Stand und Anpassungsbedarf |
| --- | --- | --- | --- | --- |
| **1.1** | `VereinsauswahlPage` | `ClubSelection` | **Fertig** | Vereins-ID wird gegen das Vereinsregister geprueft und der Vereinskontext getrennt gespeichert. Beim Vereinswechsel werden Sitzung und Arbeitskontext geloescht. |
| **1.2** | `VereinsQrScannerPage` | `QrScanner` innerhalb der Vereinsauswahl | **Fertig** | QR-Erkennung ueber die Browserkamera ist vorhanden. Berechtigungs- und Fehlertexte muessen bei den abschliessenden Geraetetests noch mit iPhone/Safari geprueft werden. |
| **1.3** | `LoginPage` | Login-Bereich in `Home` | **Fertig im festgelegten Browserumfang** | E-Mail-/Passwort-Anmeldung, Rollenpruefung, Vereinswechsel und 15-Minuten-Logout sind vorhanden. Die Zugangsdaten koennen im Browser-Passwortmanager gespeichert und unter Windows per Windows Hello/PIN freigegeben werden. Eine eigene OTP-, Passwort-Neuvergabe- oder Passkey-Oberflaeche wird deshalb derzeit bewusst nicht nachgebaut; die Kontoverwaltung bleibt server-/vorstandsseitig. |

## 2. Startseite, Inhalte und Impressum

| Nr. | MAUI-View | Browser-Entsprechung | Status | Fachlicher Stand und Anpassungsbedarf |
| --- | --- | --- | --- | --- |
| **2.1** | `HomePage` | `HomeDashboard` | **Fertig** | Aufbau und Fachinhalt folgen jetzt der WPF-Startseite: separater Verwaltungszugang fuer Vorstand/Admin, `Meine Arbeitsstunden` mit Soll/Geleistet/Offen aus `v_pflichtstunden_uebersicht`, direkter Einstieg zur Arbeitsstundenerfassung sowie drei breite Spalten fuer Arbeitseinsaetze, Termine und Bekanntmachungen. Die Startseite bezieht sich immer ausschliesslich auf das Mitgliedskonto des angemeldeten Benutzers; ein in der Verwaltung ausgewaehltes Mitglied wird dort weder angezeigt noch fachlich verwendet. Die Anmeldung zu einem offenen Arbeitseinsatz ist direkt auf der Startseite moeglich. Die zugehoerigen Detailansichten werden getrennt unter 2.2 bewertet. |
| **2.2** | `HomeSectionDetailPage` | breites `HomeSectionDetail`-Dialogfeld ueber dem `HomeDashboard` | **Fertig als PC-Integration** | Arbeitseinsaetze, Termine und Bekanntmachungen oeffnen eine vollstaendige Detaildarstellung, waehrend die Startseitenuebersicht im Hintergrund erhalten bleibt. Enthalten sind lange Texte, Datum, Beginn/Ende, Sichtbarkeit, Treffpunkt, Stundenwert, Teilnehmerzahl und Anmeldefrist. Arbeitseinsaetze unterstuetzen An- und Abmeldung des angemeldeten Benutzers; Vorstand/Admin sehen die Teilnehmerliste und gelangen direkt in die Verwaltung. Vorheriger/naechster Eintrag, Schliessen per Escape und eine mobile Vollbilddarstellung sind umgesetzt. HTML-Inhalte werden aus Sicherheitsgruenden als vollstaendiger Klartext dargestellt. |
| **2.3** | `HomeManagementPage` | Schaltflaechen im `HomeDashboard` zu `WorkAssignmentsManagement` und `ContentManagement` | **Fertig als PC-Integration** | Die gemischte mobile Verwaltungsseite wird am PC bewusst durch drei breite Master-Detail-Arbeitsbereiche ersetzt. Die Startseite fuehrt getrennt zu Arbeitseinsaetzen, Terminen und Bekanntmachungen; jeder Bereich bietet Rueckkehr zur Startseite, Aktualisieren und Neuanlage. Der gemeinsame MAUI-/WPF-Feldumfang ist angebunden: Endzeiten, Sichtbarkeitszeitraeume, Anmeldeschluss, Teilnehmerbegrenzung, Stundenwert und Sortierung. Pflichtfelder sowie Zeit-, Sichtbarkeits-, Teilnehmer- und Sortierregeln werden vor dem Speichern geprueft. Die weitergehenden Besonderheiten der einzelnen Editoren werden in 2.4 bis 2.9 getrennt bewertet. |
| **2.4** | `ArbeitseinsaetzeManagementPage` | `WorkAssignmentsManagement` | **Fertig als PC-Integration** | Die chronologisch nach Datum, Startzeit, Endzeit und Titel geordnete Verwaltung steht als breite Master-Detail-Ansicht bereit. Die Tabelle zeigt Datum, Zeitspanne, Titel, Treffpunkt, Teilnehmerbegrenzung und Status; Neuanlage, Aktualisieren, Rueckkehr zur Startseite sowie Vor-/Zurueck-Navigation im Datenbestand sind integriert. Teilnehmerverwaltung, Kapazitaet, Anmeldeschluss, Absage und Loeschen liegen ohne mobilen Seitenwechsel direkt beim gewaehlten Einsatz. |
| **2.5** | `ArbeitseinsaetzeEditorPage` | rechter Editor in `WorkAssignmentsManagement` | **Fertig als PC-Integration** | Anlegen und Bearbeiten enthalten Titel, Beschreibung, Datum, Start-/Endzeit, Treffpunkt, optionale Teilnehmerbegrenzung, Stundenwert, Sichtbarkeitszeitraum, optionalen Anmeldeschluss und Aktivstatus. Pflichtfelder, Zeitfolge, Sichtbarkeit, Anmeldedatum, Stundenwert und Teilnehmerzahl werden geprueft. Neue Datensaetze starten wie in MAUI/WPF mit 10:00 bis 13:00 Uhr und passenden Sichtbarkeitswerten. `Speichern + naechste Schicht` erzeugt eine eigene Folgeschicht mit uebernommenen Inhalten und fortgeschriebenen Uhrzeiten. Absagen und endgueltiges Loeschen mit Bestaetigung sind umgesetzt; Schreibvorgaenge beachten die Bearbeitungssperre. |
| **2.6** | `TermineManagementPage` | `AppointmentManagement` | **Fertig als PC-Integration** | Termine werden chronologisch nach Datum, Startzeit, Endzeit und Titel in einer breiten Master-Detail-Ansicht verwaltet. Die Tabelle zeigt Datum, Zeitspanne, Titel, Sichtbarkeitszeitraum und Status. Neuanlage, Aktualisieren, Rueckkehr zur Startseite sowie Vor-/Zurueck-Navigation durch den Datenbestand sind ohne mobilen Seitenwechsel vorhanden. |
| **2.7** | `TermineEditorPage` | rechter Editor in `AppointmentManagement` | **Fertig als PC-Integration** | Der Editor enthaelt Titel, Beschreibung, Datum, Start-/Endzeit, Sichtbar ab/bis und Aktivstatus. Titel und Datum, Zeitfolge und Sichtbarkeitsfolge werden vor dem Speichern geprueft. Neue Termine beginnen wie in MAUI auf der aktuellen Minute, enden standardmaessig eine Stunde spaeter und bleiben bis 23:59 Uhr des Termintags sichtbar. Speichern, Deaktivieren, Reaktivieren ueber den Aktivstatus sowie endgueltiges Loeschen mit Bestaetigung sind umgesetzt und beachten die Bearbeitungssperre. |
| **2.8** | `BekanntmachungenManagementPage` | `AnnouncementManagement` | **Fertig als PC-Integration** | Bekanntmachungen werden nach Sortierreihenfolge, Sichtbarkeitsbeginn und Titel in einer breiten Master-Detail-Ansicht geordnet. Die Tabelle zeigt Sortierung, Titel, Inhaltsvorschau, Sichtbarkeitszeitraum und Status. Neuanlage, Aktualisieren, Rueckkehr zur Startseite und Vor-/Zurueck-Navigation sind integriert. |
| **2.9** | `BekanntmachungEditorPage` | rechter Editor in `AnnouncementManagement` | **Fertig als PC-Integration** | Der Editor enthaelt Titel, HTML-Inhalt, Sichtbar ab/bis, optionale ganzzahlige Sortierreihenfolge und Aktivstatus. Die MAUI-/WPF-Einfuegehilfen fuer Absatz, Ueberschrift, Fett, Link und Liste sowie eine Live-Vorschau sind vorhanden. Die Vorschau laeuft in einem abgeschotteten Browserrahmen mit gesperrten Skripten, Formularen und externen Ressourcen. Pflichtfelder, Sichtbarkeitsfolge und Sortierung werden geprueft; Deaktivieren, Reaktivieren ueber den Aktivstatus und endgueltiges Loeschen beachten die Bearbeitungssperre. |
| **2.10** | `ImpressumPage` | `ImprintPage` | **Fertig** | Vereinsname, Register, Verantwortliche, Anschrift und Vereins-E-Mail entsprechen MAUI/WPF. Weitere Vorstands- und Bauausschusskontakte werden aus `impressum_funktion_slot` und den zugeordneten Mitgliedsdaten geladen und nach Funktion gruppiert. Am PC steht der Vereinskopf links neben den Kontaktfunktionen; Datenschutz besitzt einen eigenen Abschnitt mit Dialog und Link zur offiziellen Erklaerung. Die Ansicht ist fuer Tablet- und Telefonbreiten responsiv. |

## 3. Ablesen, Zaehler und lokale Fotos

| Nr. | MAUI-View | Browser-Entsprechung | Status | Fachlicher Stand und Anpassungsbedarf |
| --- | --- | --- | --- | --- |
| **3.1** | `AblesenOverviewPage` | `MeterOverview` plus aufgeklappte Menuegruppe `Ablesen` | **Fertig als PC-Integration** | Erfassung, Kennzahlen, Zaehler- und Ablesehistorie, offene Pruefungen, Pruefverlauf und eine detaillierte Tabelle der innerhalb von zwoelf Monaten eichfaelligen Zaehler liegen in einem breiten Arbeitsbereich. Die lokale Einstellung `Fotos nur bei erkanntem WLAN direkt hochladen` sowie direkte Einstiege zu Foto-Warteschlange und berechtigtem Zaehlerwechsel sind vorhanden. Die eigenstaendige RFID-Einrichtung und der weitergehende Scanablauf werden getrennt unter 3.5 und 3.6 bewertet. |
| **3.2** | `AblesungErfassenPage` | `MeterReadingEntry` in `MeterOverview` | **Fertig als PC-Integration** | Normale Ablesung und Jahresendablesung sind integriert; JEA verwendet den 31.12. der gewaehlten Saison. Der Einstieg funktioniert optional per Web-NFC und immer manuell ueber Parzelle, Medium und aktiven Zaehler. Foto-Pflicht, letzter freigegebener Stand, ruecklaeufige Werte, Dubletten und die zentrale Freigabe fuer Nutzerablesungen werden geprueft. Berechtigte Pruefer speichern direkt freigegeben, andere Nutzer reichen zur Pruefung ein. Fotos werden je nach WLAN-Einstellung direkt hochgeladen oder sicher in der lokalen Warteschlange gehalten. |
| **3.3** | `AblesungenFreigabePage` | Reiter `Eingereichte Ablesungen` und `ReadingReview` in `MeterOverview` | **Fertig als PC-Integration** | Offene Ablesungen und Pruefdetail stehen gleichzeitig nebeneinander. Parzelle und sprechender Mitgliedsname werden zum Ablesedatum ermittelt. Freigeben, mit korrigiertem Datum/Stand freigeben, Ablehnen und aus dem Pruefprozess Entfernen sind mit Pflichtkommentar, Bestaetigung und Bearbeitungssperre umgesetzt. Die letzten Ablesungen desselben Zaehlers sowie ein abgeschlossener Pruefverlauf mit Pruefer und Kommentar sind direkt sichtbar. |
| **3.4** | `PendingPhotoUploadsPage` | `PendingPhotoUploads` | **Fertig mit Testbedarf** | Lokale Warteschlange, Upload und Entfernen sind vorhanden. Vereinswechsel, Browser-Neustart, Offline/Online und mehrere Tabs muessen noch als End-to-End-Test abgesichert werden. |
| **3.5** | `RfidEinrichtenPage` | keine Browserseite | **Fehlt** | Browsergerechter QR-/NFC-/manueller Ersatzweg, Parzellen- und Medienauswahl sowie Zuordnungspruefung fehlen. NFC darf nur als optionale Geraetefunktion behandelt werden. |
| **3.6** | `RfidScanWorkflowPage` | teilweise Scannerlogik in `MeterReadingEntry` | **Teilweise** | Der vollstaendige MAUI-Workflow fuer bekannten/unbekannten Tag, Medium und fachliche Weiterleitung fehlt. |
| **3.7** | `FaelligeZaehlerPage` | Kennzahl `In 12 Monaten eichfaellig` in `MeterOverview` | **Teilweise** | Die Anzahl ist sichtbar; eine filterbare Detailtabelle mit Parzelle, Medium, Zaehler, Eichfrist und direkter Wechselaktion fehlt. |
| **3.8** | `ZaehlerwechselPage` | `MeterChange` | **Teilweise / integriert** | Auswahl und gemeinsames Ausbau-/Einbauformular sind vorhanden. MAUI fuehrt mobil bewusst ueber Einzelschritte; am PC ist die Zusammenfassung sinnvoll. RFID-Einstieg, Fotos, Pruefhinweise und vollstaendige Historie fehlen. |
| **3.9** | `ZaehlerwechselAusbauPage` | linker Teil des Formulars `MeterChange` | **Teilweise / integriert** | Ausbau, Datum und Endstand sind vorhanden. Foto-/Plausibilitaetslogik und alle Statuspruefungen muessen abgeglichen werden. |
| **3.10** | `ZaehlerwechselEinbauPage` | rechter Teil des Formulars `MeterChange` | **Teilweise / integriert** | neue Nummer, Eichdatum und Einbau sind vorhanden. Vollstaendige MAUI-Validierungen, RFID-Zuordnung und Ergebnisanzeige fehlen teilweise. |

## 4. Parzellen und Gaerten

| Nr. | MAUI-View | Browser-Entsprechung | Status | Fachlicher Stand und Anpassungsbedarf |
| --- | --- | --- | --- | --- |
| **4.1** | `ParzellenPage` | `ParcelWorkspace` und `ParcelDetail` | **Fertig als PC-Integration** | Der Aufbau folgt der WPF-Parzellenverwaltung: durchsuch- und filterbare Tabelle mit Garten, Anlage, Status und sprechendem Mitglied links; dauerhaftes Detail rechts. Stammdaten, Flaeche mit Aenderungsbestaetigung, Anschluesse, RFID-Anzeige, aktive Zaehler, Belegung und Dokumente sind enthalten. Die MAUI-Fachrechte und die Browser-Bearbeitungssperren bleiben wirksam. |
| **4.2** | `ParzellenAblesungenPage` | Reiter `Ablesungen` in `ParcelDetail` | **Fertig als PC-Integration** | Strom- und Wasserablesungen erscheinen im gewaehlten Parzellenkontext als filterbare Historientabelle mit Medium, Zaehlernummer, Ableseart, Datum, Stand, Freigabestatus und Fotozugriff. Der grosse Bildschirm behaelt Parzellenliste und Detailkontext gleichzeitig bei. |
| **4.3** | `MemberGardensPage` | `MemberGardensWorkspace` unter `Gaerten des Mitglieds` | **Fertig als PC-Integration** | Aktive und historische Zuordnungen werden wie im WPF-Mitgliedsbereich mit Garten, Anlage, Von/Bis und Status angezeigt. Die historische Sicht ist zuschaltbar; die Auswahl oeffnet rechts unmittelbar das vollstaendige Parzellendetail. |
| **4.4** | `MemberGardenAssignPage` | Zuordnungsbereich in `MemberGardensWorkspace` | **Fertig als PC-Integration** | Nur aktuell freie, aktive Parzellen koennen mit Startdatum zugeordnet werden. Doppelbelegung wird vorab verhindert und serverseitig weiter abgesichert. Beendigungen erfolgen im Belegungsreiter mit Datum und fachlichem Grund. Der Dokument-/Pachtvertragsbereich des Mitglieds ist direkt erreichbar. |
| **4.5** | `MemberParzellenDetailPage` | `ParcelDetail` im mitgliedsbezogenen Master-Detail | **Fertig als PC-Integration** | Uebersicht, Anschluesse und aktive Zaehler, vollstaendige Ablesungshistorie, Parzellendokumente sowie aktive und historische Belegung liegen in vier Reitern. Damit werden die MAUI-Einzelseiten und die WPF-Detailgruppen auf dem PC in einem dauerhaften Detailbereich zusammengefuehrt. |
| **4.6** | `ParzellenProtokollePage` | `ParcelProtocolsWorkspace` | **Fertig mit Deployment-/End-to-End-Testbedarf** | Der PC-Ablauf verwendet drei breite Schritte `Grunddaten – Anlagen – Unterschriften/Abschluss`. Enthalten sind Protokollart, aktive Mitgliedsparzelle, zwei Vorstandsrollen, optionale Begleitperson, Anlass, Feststellungen, Vereinbarungen, letzte Strom-/Wasserablesungen, maximal zehn verkleinerte Fotoanlagen und vier Unterschriftsfelder. Entwuerfe werden in den Protokolltabellen gespeichert; die erweiterte Serverfunktion erzeugt eine PDF mit Fotoanlagen, legt sie beim Mitglied ab und schliesst den Entwurf ab. Nach Bereitstellung der geaenderten Supabase-Funktion ist der produktive PDF-/Drive-End-to-End-Test noch auszufuehren. |

## 5. Mitglieder, Stammdaten und Benutzerrechte

| Nr. | MAUI-View | Browser-Entsprechung | Status | Fachlicher Stand und Anpassungsbedarf |
| --- | --- | --- | --- | --- |
| **5.1** | `MemberSearchPage.xaml` / `MemberSearchPage.xaml.cs` | `MemberSearch` | **Fertig im festgelegten PC-Umfang** | Die Seite dient ausschliesslich Suche und Auswahl. Der gemeinsame Suchtext findet Nachname, Vorname, E-Mail, Mitgliedsnummer und aktuell zugeordnete Gartennummern; die Tabelle zeigt die Gartennummern aus den aktiven Parzellenbelegungen als eigene Spalte. In MAUI und Browser sind inaktive Mitglieder standardmaessig ausgeblendet und koennen mit `Inaktive Mitglieder anzeigen` zugeschaltet werden. Der fehlerhafte Demo-Direktfilter auf der nur indirekt abgegrenzten Belegungstabelle wurde entfernt. Nach Auswahl erscheint der Name nur innerhalb der aufgeklappten Menuegruppe ueber den eingerueckten Unterpunkten. Die Auswahl wird bewusst nicht im lokalen Langzeitspeicher abgelegt und ist bei der naechsten Anmeldung nicht mehr vorausgewaehlt. |
| **5.2** | `MemberDetailPage` | eigene Seite `MemberStammdatenPage` unter `mitglied-stammdaten` | **Teilweise** | Die Stammdatenuebersicht wurde vollstaendig aus der Suchseite entfernt und besitzt jetzt einen eigenen Menuebereich. Grundkontakt, Adresse, Mitgliedschaft, Bearbeitungssperre, Nebenmitglied und Beendigung sind dort vorhanden; auch eine Neuanlage wechselt direkt in diese Seite. Die SQL-Bearbeitungssperre verwendet eindeutig qualifizierte Ablauf- und Benutzerfelder, sodass vorhandene Mitglieder ohne mehrdeutige `expires_at`-Referenz bearbeitet werden koennen. Weitere MAUI-Felder und Abschnitte, Altersregel/Pflichtstundenlogik, Nutzeraktionen und Mitgliedsantrag-Aktionen bleiben fuer den fachlichen Detailabgleich offen. |
| **5.3** | `MeineDatenPage` | kein eigener Browserbereich | **Fehlt / teilweise ueber Mitgliedskontext** | Eigene Stammdaten, Wartungsvertrags-/Pflichtstundeninformationen und berechtigte Eigenbearbeitung muessen als eigener, rechtegesteuerter Bereich umgesetzt werden. |
| **5.4** | `MyProfilePage` | kein eigener Browserbereich | **Fehlt** | Profilanzeige und die von MAUI getrennten Konto-/Stammdateninformationen muessen mit `Meine Daten` fachlich zusammengefuehrt werden; keine doppelte PC-Seite anlegen. |
| **5.5** | `NebenmitgliedPage` | `SecondaryMemberPanel` innerhalb `MemberDetail`; Menueziel `mitglied-nebenmitglied` noch ohne eigene Seite | **Teilweise** | Anlage und Kontakt-/Adressbearbeitung sind vorhanden. Eigene PC-Seite, Geburtsdatum-Schalter, Beginn, WhatsApp, Rechte und alle MAUI-Validierungen muessen abgeglichen werden. |
| **5.6** | `AdminMenuPage` | `UserRightsAdministration(fixedMemberId)` | **Teilweise** | Rolle, Rechte, Einladung und Passwort-Reset sind vorhanden. Die MAUI-Einstellung fuer Nutzerablesungen, exakte Rollenbasis-/Override-Anzeige und alle kontobezogenen Statusfaelle fehlen teilweise. |
| **5.7** | `UserManagementPage` | Kontenaktionen in `UserRightsAdministration` | **Teilweise / integriert** | Mitgliedsgebundene Benutzerverwaltung ist vorhanden. MAUI-Aktionen, Rueckmeldungen und Sicherheitspruefungen muessen einzeln abgeglichen werden. |

## 6. Arbeitsstunden

| Nr. | MAUI-View | Browser-Entsprechung | Status | Fachlicher Stand und Anpassungsbedarf |
| --- | --- | --- | --- | --- |
| **6.1** | `MyArbeitsstundenPage` | `OwnWorkHours` | **Teilweise bis weitgehend fertig** | Soll, freigegeben, offen, Tabelle und Verlauf sind vorhanden. Wartungsvertrag/Altersbefreiung und die exakte MAUI-Berechnung des Pflichtstundenstatus muessen geprueft werden. |
| **6.2** | `ArbeitsstundenEditorPage` | Editor oberhalb der Tabelle in `OwnWorkHours` | **Teilweise / integriert** | Neu, Bearbeiten und Loeschen offener Eintraege sind vorhanden. Vollstaendige MAUI-Validierungen, Statushinweise und etwaige Zusatzfelder muessen abgeglichen werden. |
| **6.3** | `ArbeitsstundenReviewPage` | `WorkHoursOverview` | **Teilweise / integriert** | Offene Liste und Kennzahlen sind vorhanden. Statt technischer Mitglieds-ID muss der Name erscheinen; Filter, Sortierung und Gruppierung nach Mitglied fehlen. |
| **6.4** | `ArbeitsstundenReviewDetailPage` | rechter Pruefbereich in `WorkHoursOverview` | **Teilweise / integriert** | Freigeben, Korrigieren, Ablehnen, Kommentar und Verlauf sind vorhanden. Sperrverhalten, Pflichtkommentarregeln und alle MAUI-Pruefinformationen muessen abschliessend verglichen werden. |

## 7. Wartungsvertraege

| Nr. | MAUI-View | Browser-Entsprechung | Status | Fachlicher Stand und Anpassungsbedarf |
| --- | --- | --- | --- | --- |
| **7.1** | `WartungsvertraegePage` | `MaintenanceContracts` ohne Mitgliedsfilter | **Weitgehend fertig / integriert** | Tabelle, Kontingent und Auswahl sind vorhanden. Status-, Filter- und Aktualisierungsdarstellung mit MAUI abgleichen. |
| **7.2** | `WartungsvertragDetailPage` | rechter Detailbereich in `MaintenanceContracts` | **Weitgehend fertig / integriert** | Beschreibung, Belegung, freie Plaetze und Zuordnungen sind vorhanden. Read-only-/Edit-Rechte und alle Hinweistexte pruefen. |
| **7.3** | `WartungsvertragEditorPage` | Editorzustand in `MaintenanceContracts` | **Weitgehend fertig / integriert** | Anlegen und Bearbeiten der Kernfelder ist vorhanden. Vollstaendige Validierung und Statuswechsel mit MAUI abgleichen. |
| **7.4** | `WartungsvertragAssignMembersPage` | Zuordnungsformular in `MaintenanceContracts` | **Teilweise / integriert** | Zuordnen und Beenden sind vorhanden. Mehrfachauswahl, Verfuegbarkeitslogik und alle MAUI-Pruefungen muessen verglichen werden. |
| **7.5** | `MemberWartungsvertraegePage` | `MaintenanceContracts(memberId=...)` | **Weitgehend fertig / integriert** | Mitgliedsfilter und Zuordnung sind vorhanden. Darstellung von Haupt-/Nebenmitglied, Garten und Pflichtstundenbefreiung abschliessend pruefen. |

## 8. Dokumente, Vertraege, Vorschau und Signatur

| Nr. | MAUI-View | Browser-Entsprechung | Status | Fachlicher Stand und Anpassungsbedarf |
| --- | --- | --- | --- | --- |
| **8.1** | `DokumentePage` | `DocumentList` | **Teilweise** | Liste, Upload, Oeffnen, Vorschau, Archivieren und Vertragserzeugung sind vorhanden. Digitales Nachsignieren bestehender Vertraege, Archivpasswort, Dokumentauswahl und alle MAUI-Sicherheitsdialoge muessen abgeglichen werden. |
| **8.2** | `MitgliedsantragDialogPage` | `ContractComposer` mit Typ `mitgliedsantrag` | **Teilweise / integriert** | Beginn, Beitrag, Aufnahmegebuehr und Unterschriften sind vorhanden. Vollstaendige MAUI-Vorbelegung, Validierung und Vertreterlogik fehlen teilweise. |
| **8.3** | `MitgliedsantragPreviewPage` | PDF-Vorschau aus `ContractComposer` | **Teilweise / integriert** | Servergenerierte Vorschau ist vorhanden. Browseranzeige, Fehlerbehandlung und erneutes Bearbeiten muessen als durchgaengiger Ablauf getestet werden. |
| **8.4** | `PachtvertragDialogPage` | `ContractComposer` mit Typ `pachtvertrag` | **Teilweise / integriert** | Parzellenauswahl und Unterschriften sind vorhanden. Vollstaendige MAUI-Vertragsdaten, Nebenmitglied-/Vertreterlogik und Plausibilitaet fehlen teilweise. |
| **8.5** | `PachtvertragPreviewPage` | PDF-Vorschau aus `ContractComposer` | **Teilweise / integriert** | Vorschau vorhanden; Ruecksprung, Korrektur und finale Ablage als kompletter Ablauf testen. |
| **8.6** | `VertragsSignaturPage` | drei `SignaturePad`-Felder in `ContractComposer` | **Teilweise / integriert** | Browserunterschrift und sichere Ablage sind vorhanden. Rollen der Unterzeichner, Nachsignieren und alle MAUI-Pflichtpruefungen muessen verglichen werden. |
| **8.7** | `PdfViewerPage` | geschuetzte URL/PDF im Browser | **Fertig als Browserersatz** | Eigene Viewer-Seite ist am PC nicht erforderlich. Tokenlaufzeit, neuer Tab, Druck und Downloadrechte testen. |
| **8.8** | `ImageViewerPage` | `PhotoOpenButton` bzw. geschuetzte Bild-URL | **Fertig als Browserersatz** | Eigene Viewer-Seite ist nicht erforderlich; Zoom und grosse Bilder sollten noch browserseitig geprueft werden. |

## 9. Export und zentrale Verwaltung

| Nr. | MAUI-View | Browser-Entsprechung | Status | Fachlicher Stand und Anpassungsbedarf |
| --- | --- | --- | --- | --- |
| **9.1** | `ExportPage` | `ExportCenter` | **Teilweise bis weitgehend fertig** | Definitionen, dynamische Filter, breite Ergebnistabelle, CSV und Drucken/PDF sind vorhanden. MAUI-PDF-Layout, Dateinamen, Spaltenauswahl und grosse Datenmengen muessen abgeglichen werden. |
| **9.2** | `SaisonverwaltungPage` | `SeasonAdministration` | **Weitgehend fertig / integriert** | Saisonliste links, Editor rechts, Vorjahresvorschlag und Schreibschutz sind vorhanden. Exakte Validierungen und alle Saisonfelder pruefen. |
| **9.3** | `VereinskonfigurationPage` | `ClubConfigurationAdministration` | **Teilweise** | Felder und Speichern sind vorhanden. MAUI-Gruppierung, Aktualisieren, Standardtexte, Datenschutz und Dokumentmetadaten in PC-Reiter gliedern und vollstaendig vergleichen. |

## 10. Jahresabschluss

| Nr. | MAUI-View | Browser-Entsprechung | Status | Fachlicher Stand und Anpassungsbedarf |
| --- | --- | --- | --- | --- |
| **10.1** | `JahresabschlussPage` | Menueziel `jahresabschluss`, derzeit nur vorbereiteter Bereich | **Fehlt** | Saisonwahl, Rechnungsliste, Status, neue Rechnung, Stammdaten/Umlagen und Pruefung fehlen. PC-Ziel: Rechnungsliste links, ausgewaehlte Rechnung mittig, Status/Pruefung rechts. |
| **10.2** | `JahresabschlussStammdatenPage` | keine Browserseite | **Fehlt** | Kostenarten, Umlagearten und fachliche Stammdaten muessen als eigener Reiter im Jahresabschluss umgesetzt werden. |
| **10.3** | `JahresabschlussRechnungEditorPage` | keine Browserseite | **Fehlt** | Rechnungskopf, Betraege, Belege, Umlagezuordnung, Validierung und Speichern fehlen. |
| **10.4** | `JahresabschlussPruefungPage` | keine Browserseite | **Fehlt** | Vollstaendigkeitspruefung, Fehlerliste, Warnungen und Abschlussstatus fehlen. PC-Ziel: dauerhaft sichtbare Pruefliste mit direkter Navigation zum fehlerhaften Datensatz. |

## 11. Technische MAUI-Bausteine ohne eigene Browserseite

| Nr. | MAUI-Datei | Browser-Entsprechung | Bewertung |
| --- | --- | --- | --- |
| **11.1** | `ManagementOverviewPageBase` | gemeinsames Tabellen-/Master-Detail-Muster in `page.tsx` und `globals.css` | Kein eigenes Menueziel; das Muster soll spaeter in wiederverwendbare Browserkomponenten zerlegt werden. |
| **11.2** | `PachtvertragFlowHelper` | Ablauf in `ContractComposer` und Serverfunktion `kgv-generate-contract` | Fachlogik vergleichen; keine eigene Seite erforderlich. |
| **11.3** | `SignatureFlowHelper` | `SignaturePad`, Vorschau und Finalisieren in `ContractComposer` | Keine eigene Seite erforderlich; gemeinsame Validierungslogik sicherstellen. |

## 12. Querschnittlicher technischer Stand

Bereits vorhanden:

- Vereinsauswahl und getrennte Vereinskontexte
- Supabase-Anmeldung und Rollen-/Fachrechte
- zentrale Demo-/Echtdaten-Trennung
- serverseitige Bearbeitungssperren mit MAUI-/WPF-Kompatibilitaet
- automatischer Logout nach 15 Minuten Inaktivitaet
- lokale Foto-Warteschlange
- PC-Master-Detail-Grundmuster
- portables Windows-Testpaket

Noch querschnittlich zu pruefen oder zu vereinheitlichen:

- alle sichtbaren IDs durch Namen bzw. fachliche Bezeichnungen ersetzen
- einheitliche Lade-, Leer-, Fehler- und Erfolgsmeldungen
- Tabellenfilter, Sortierung, Seitengroesse und Tastaturbedienung
- Rechte nicht nur im Menue, sondern fuer jede Aktion pruefen
- Schreibsperren in jedem Editor erst beim Bearbeitungsbeginn aktivieren
- vollstaendige MAUI-Validierungen in gemeinsam testbare Funktionen uebernehmen
- responsive Rueckfallansicht fuer Surface, iPad und iPhone
- End-to-End-Tests mit zwei gleichzeitig angemeldeten Benutzern

## 13. Nummerierter weiterer Umsetzungsablauf

### 1. Detailabgleich festschreiben – abgeschlossen

- Jede MAUI-View ist einer Browserseite oder einem bewusst integrierten
  PC-Arbeitsbereich zugeordnet.
- Fehlende Funktionen und PC-Zieldarstellung sind in diesem Dokument erfasst.

### 2. Zugang und Anmeldung abschliessen — **abgeschlossen**

- E-Mail-/Passwort-Anmeldung und Rollenpruefung umgesetzt
- Vereinswechsel und getrennte Vereins-/Arbeitskontexte umgesetzt
- automatischer Sitzungsabschluss nach 15 Minuten Inaktivitaet umgesetzt
- schneller Login ueber den Browser-Passwortmanager; unter Windows kann der
  Browser die Freigabe mit Windows Hello/PIN schuetzen
- eigene OTP-, Passwort-Neuvergabe- und Passkey-Oberflaeche nach gemeinsamer
  Festlegung nicht Bestandteil des aktuellen Browserumfangs

### 3. Startseite und Impressum angleichen

- Startseiten-Detailinhalte und Pflichtstundeninformation vervollstaendigen
- Impressum mit Vereinskopf, Funktionen, Kontakten und Datenschutz umsetzen
- Verwaltungszugaenge auf der Startseite fachlich pruefen

### 4. Mitgliedsstammdaten als gemeinsames PC-Fundament fertigstellen

- vollstaendiges MAUI-Datenmodell anzeigen und bearbeiten
- Abschnitte `Grunddaten`, `Kontakt`, `Adresse`, `Mitgliedschaft`, `Admin`
- Untermenue `Stammdaten` an denselben Editor anbinden
- `Meine Daten`/`MyProfile` in einem rechtegesteuerten PC-Bereich zusammenfassen
- Namen statt Mitglieds-IDs in allen abhaengigen Ansichten

### 5. Jahresabschluss als priorisierten PC-Arbeitsbereich umsetzen

- Saison und Abschlussstatus
- Kosten-/Umlagearten
- Rechnungsliste und Rechnungserfassung
- Umlagezuordnung
- Vollstaendigkeitspruefung mit direkter Fehlernavigation
- spaeter PDF-/Export-/Abschlussfunktionen

### 6. Mitgliedsbezogene Unterseiten vervollstaendigen

- Nebenmitglied als eigene Unterseite
- Gaerten des Mitglieds und Parzellenzuordnung
- Admin-Menue einschliesslich Ablesungsfreigabe
- Arbeitsstundenregeln und Befreiungen

### 7. Dokumente, Vertraege und Protokolle abschliessen

- Dokumentliste und Archivierung vollstaendig angleichen
- Mitgliedsantrag- und Pachtvertragsablaeufe pruefen
- Nachsignieren und geschuetzte Archivierung
- Parzellenprotokolle mit Fotos, Unterschriften, Entwurf und PDF

### 8. Parzellenbereich fachlich vervollstaendigen

- alle MAUI-Stammdaten und Aktionen
- aktive/historische Belegungen mit Namen
- parzellenbezogene Ablesungen und Dokumente
- direkte Navigation zwischen Mitglied, Parzelle, Zaehler und Dokument

### 9. Ablesen und Zaehlerablaeufe vervollstaendigen

- RFID-/QR-/manueller Einstieg
- faellige Zaehler als Detailtabelle
- vollstaendige Erfassungs- und Freigaberegeln
- Zaehlerwechsel mit Fotos, Historie und Plausibilitaet
- Offline-/Online- und Mehrtabtests

### 10. Arbeitsstunden vollstaendig angleichen

- Namen, Filter und Gruppierung in der Freigabe
- Pflichtstunden-, Alters- und Wartungsvertragsregeln
- Editor- und Pruefvalidierungen
- Verlauf und Sperrtests mit zwei Bearbeitern

### 11. Startseiten-Verwaltung fuer Einsaetze, Termine und Bekanntmachungen

- gemeinsame PC-Integration aus `HomeManagementPage`: **abgeschlossen in 2.3**
- vollstaendige Detailanzeige: **abgeschlossen in 2.2**
- weitergehende Einzelfunktionen der PC-Master-Detail-Editoren und Vorschau: siehe 2.4 bis 2.9

### 12. Wartungsvertraege, Export und zentrale Verwaltung abschliessen

- restliche MAUI-Regeln und Rechte
- PDF-/CSV-Gleichstand im Export
- Vereinskonfiguration in PC-Abschnitte/Reiter gliedern
- Saison- und Konfigurationssperren testen

### 13. Responsive, Barrierefreiheit und Oberflaechenkonsistenz

- Desktop, Surface/Tablet und iPhone-Breiten
- Tastatur, Fokus, Beschriftungen und Kontraste
- einheitliche Tabellen, Detailbereiche, Editoren und Dialoge

### 14. Gemeinsame Abnahme gegen MAUI

- jede Tabellenzeile dieses Dokuments erneut pruefen
- Rollen `user`, `vorstand`, `admin` testen
- Demo- und Echtdaten testen
- gleichzeitige Bearbeitung testen
- portables Windows-Paket neu erstellen
- anschliessend HTTPS-Testveroeffentlichung fuer Apple-/Browser-Tester

## 14. Definition von „auf demselben Stand wie MAUI“

Eine Browserseite gilt erst als fertig, wenn:

1. alle fachlich relevanten MAUI-Felder vorhanden sind,
2. dieselben Rechte und Rollen gelten,
3. dieselben Pflichtfelder, Plausibilitaeten und Statuswechsel gelten,
4. vorhandene Dokument-, Foto- und Signaturablaeufe funktionieren,
5. Demo- und Echtdaten korrekt getrennt bleiben,
6. Bearbeitungssperren bei konkurrierender Bearbeitung greifen,
7. die PC-Darstellung den grossen Bildschirm sinnvoll nutzt,
8. Tablet und Smartphone weiterhin bedienbar bleiben und
9. mindestens ein erfolgreicher End-to-End-Test dokumentiert ist.
