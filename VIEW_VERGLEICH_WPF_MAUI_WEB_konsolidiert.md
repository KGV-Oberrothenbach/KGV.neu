# View-Vergleich WPF – MAUI – Web

## Grundregeln

- **WPF** ist die Referenz für Darstellung, Seitenaufbau und Bedienkonzept. Das bedeutet keine 1:1-Kopie; dokumentierte webtypische Verbesserungen dürfen bewusst abweichen.
- **MAUI** ist die Standardreferenz für aktuelle Fachlogik, Berechtigungen, Datenflüsse und Geschäftsregeln.
- **Datenbankregeln, RPCs/Trigger und der gemeinsame `KGV.Core` haben Vorrang**, wenn sie eine Fachregel eindeutig zentral festlegen oder wenn eine Gruppe ausdrücklich feststellt, dass der aktuelle MAUI-Stand noch vorläufig bzw. widersprüchlich ist. Das betrifft insbesondere G12 Jahresabschluss.
- **Web** wird gegen diese Referenzen geprüft; eine in dieser Analyse ausdrücklich festgelegte Zielregel gilt anschließend gruppenübergreifend.
- Die bestehende große `KGV.Web/app/page.tsx` ist nur Übergangsstruktur.
- Neue oder überarbeitete Web-Funktionen werden fachlich modularisiert.
- Nummerierungen bleiben stabil.
- Neue Punkte werden innerhalb ihrer Fachgruppe ergänzt.
- Ziel ist keine 1:1-Dateikopie von MAUI, sondern eine **webgerechte fachliche Trennung entlang derselben Verantwortlichkeiten**.

## Verbindliche Web-Architektur

```text
UI / Pages / Components
        ↓
Fachservice
        ↓
Repository / Datenzugriff
        ↓
Supabase / HTTP / Storage
```

### UI darf

- Daten anzeigen
- Formulare verwalten
- Benutzeraktionen behandeln
- Loading- und Fehlerzustände anzeigen
- Navigation auslösen
- Fachservices aufrufen

### Fachservice darf

- Geschäftsregeln anwenden
- mehrere Datenquellen kombinieren
- fachliche Berechtigungen berücksichtigen
- Workflows koordinieren
- Daten für die UI aufbereiten

### Repository darf

- Supabase-Tabellen abfragen
- RPCs aufrufen
- HTTP-/Auth-Endpunkte ansprechen
- INSERT/PATCH/DELETE ausführen
- Datenquellen-DTOs liefern

### Repository darf nicht

- Dialoge anzeigen
- React-State verändern
- Navigation auslösen
- UI-Texte oder UI-Zustände steuern

Direkte Datenbankzugriffe wie

```ts
readSupabase<Member>(session, "mitglied", ...)
```

sollen langfristig **nicht in React-Komponenten** stehen.

Stattdessen verwendet die UI beispielsweise:

```ts
searchMembers(...)
getMember(...)
saveMember(...)
```

Der Fachservice ruft wiederum das passende Repository auf.

## Gruppenübergreifende Zuständigkeiten

Damit die Zielarchitektur nicht selbst wieder doppelte Fachlogik erzeugt, gelten zusätzlich:

- **Rollen und Permission-Bitmasken gehören zentral zu G13.** Fachbereichsspezifische Access-Services dürfen daraus Fähigkeiten ableiten, aber keine eigene Rollenbasis oder eigene Bitmasken definieren. `navigation-service.ts` aus G2 konsumiert diese zentrale Rechteauswertung. **Explizit dokumentierte Rollenregeln** bleiben davon unberührt, z. B. Archivieren und Vereins-/Saisonkonfiguration nur Admin, Jahresabschluss Admin/Vorstand, Export bis zu einem eigenen Exportrecht nur Admin/Vorstand und automatische Freigabe neu erfasster Arbeitsstunden nur bei Admin/Vorstand.
- **Session/Auth gehört zu G1.** G15 beschreibt nur die browserspezifische technische Umsetzung derselben `services/auth/session-service.ts`; es gibt keinen zweiten `browser-session-service.ts`.
- **Saison-Datenzugriff gehört zu `services/seasons` / `repositories/seasons` aus G12** und wird von G2 für den Saisonpicker wiederverwendet.
- **Die Startseite G2 ist eine Aggregationsschicht.** Sie besitzt keine zweite Implementierung für Arbeitseinsätze, Termine, Bekanntmachungen oder Pflichtstunden, sondern verwendet die Fachservices/-repositories aus G8, G9 und G7.
- **Generische Dokumentablage gehört zu G10.** G4 erzeugt und signiert Verträge, delegiert das Speichern/Öffnen fertiger Dokumente aber an die gemeinsame Dokument-Infrastruktur aus G10.
- **Parzellenbeziehungen gehören zu G5.** Mitgliedsseiten aus G3 dürfen diese Funktionen darstellen, verwenden dafür aber den Parzellenservice statt eine zweite Mitglieds-Implementierung aufzubauen. Entsprechendes gilt für Wartungsverträge (G11), Rollen/Rechte (G13) und Dokumente (G10).
- **Browser-Scanner werden zweistufig getrennt:** technische Gerätezugriffe wie `NfcScanner.tsx` gehören zu G15; fachliche Wrapper wie `features/meters/rfid/RfidScanner.tsx` gehören zu G6.
- **Admin-Kontoverwaltung G13 und Self-Service-Auth G1 bleiben getrennte Fachaufgaben.** Beide dürfen dieselbe technische Auth-Infrastruktur verwenden.

---

# Fachgruppen

| Gruppe | Fachbereich | Enthaltene Seiten / Funktionen |
|---|---|---|
| **G1** | Anmeldung & Vereinsauswahl | Vereinsauswahl, QR-Auswahl, Login, Erstlogin/OTP, Passwort vergessen, Passwort setzen, Vereinswechsel, Session |
| **G2** | Startseite & Navigation | Hauptnavigation, Startseite, Startseiten-Details, Impressum, Saisonkontext |
| **G3** | Mitglieder | Mitgliedersuche, Mitglieddetails, Stammdaten, neues Mitglied, Nebenmitglied, Mitgliedskontext |
| **G4** | Mitgliedsantrag & Verträge | Mitgliedsantrag, Vorschau, Pachtvertrag, Signatur |
| **G5** | Parzellen | Parzellenverwaltung, Zuordnung, Gärten eines Mitglieds, Parzellendetails |
| **G6** | Zähler & Ablesungen | Ablesen, Ablesung erfassen/freigeben, RFID, Zählerwechsel, fällige Zähler |
| **G7** | Arbeitsstunden | Eigene Arbeitsstunden, Erfassung, Prüfung/Freigabe |
| **G8** | Arbeitseinsätze | Verwaltung, Editor, Anmeldung, Teilnahme |
| **G9** | Termine & Bekanntmachungen | Termine, Termin-Editor, Bekanntmachungen, Bekanntmachungs-Editor |
| **G10** | Dokumente | Mitgliedsdokumente, Parzellendokumente, PDF/Bildanzeige, Upload |
| **G11** | Wartungsverträge | Übersicht, Detail, Editor, Mitgliederzuordnung |
| **G12** | Saison & Jahresabschluss | Saisonverwaltung, Jahresabschluss, Stammdaten, Prüfung, Rechnungseditor |
| **G13** | Benutzer, Rollen & Rechte | Benutzerverwaltung, Rollen, Admin-Menü |
| **G14** | Vereinskonfiguration & Exporte | Vereinskonfiguration, Export |
| **G15** | Browser-/Systemfunktionen | PWA/Offline, Pending Uploads, Edit-Locks, technische Hilfsfunktionen |

---

# G1 – Anmeldung & Vereinsauswahl

## 1.1 Vereinsauswahl

### WPF
Keine aktuelle gleichwertige Mehrvereins-Auswahl.

### MAUI
`VereinsauswahlPage.cs`

- Vereins-ID eingeben
- `resolve_vereinscode`
- Vereinskontext speichern
- QR-Code scannen
- ungültige/inaktive Vereine behandeln
- anschließend Login gegen Vereinsdatenbank

### Web aktuell
`ClubSelection` liegt noch in `KGV.Web/app/page.tsx`.

Vorhanden:
- Vereins-ID
- `resolveClub()`
- QR-Scanner
- Fehlermeldungen
- Speichern des Vereinskontextes

### Bewertung
✅ Fachlicher Grundablauf entspricht MAUI weitgehend.

### Ziel
```text
features/auth/
  ClubSelection.tsx

services/auth/
  club-service.ts

repositories/auth/
  club-repository.ts
```

UI:
```ts
clubService.resolveClub(code)
```

Repository kennt den konkreten RPC-Aufruf `resolve_vereinscode`.

## 1.2 Vereins-QR-Scanner

### MAUI
`VereinsQrScannerPage`

### Web aktuell
`QrScanner` direkt in `page.tsx`.

Nutzt:
- Browserkamera
- `BarcodeDetector`
- Fallback bei inkompatiblen Browsern

### Bewertung
✅ Webgerechte Umsetzung bereits gut.

### Offen
Scanner aus `page.tsx` lösen.

### Ziel
```text
components/scanner/
  QrScanner.tsx
```

Möglichst generisch, damit die Komponente später auch von anderen QR-/Barcode-Funktionen genutzt werden kann.

## 1.3 Normaler Login

### WPF
`LoginWindow.xaml`

- E-Mail
- Passwort
- Passwort anzeigen
- Anmelden
- Erstlogin-Code anfordern
- Passwort vergessen

### MAUI
`LoginPage.xaml.cs`

Zusätzlich:
- Vereinsanzeige
- gespeicherte letzte E-Mail
- Benutzer-/Rollenkontext
- Vereinswechsel
- biometrische Anmeldung

### Web aktuell
Direkt in `app/page.tsx`.

Vorhanden:
- E-Mail
- Passwort
- Anmeldung
- Rollen-/Benutzerkontext
- Vereinswechsel
- Fehleranzeige

### Bewertung
✅ Normaler Login grundsätzlich vorhanden.

### Offen
- Passwort-Sichtbarkeit
- Erstlogin-/OTP-Flow
- Passwort-vergessen-Flow
- strukturelle Auslagerung

### Ziel
```text
features/auth/
  LoginForm.tsx
  AuthFlow.tsx

services/auth/
  auth-service.ts

repositories/auth/
  auth-repository.ts
```

## 1.4 Einladung / Erstlogin / OTP anfordern

### WPF
Button: `Einladung / Erstlogin-Code anfordern`

### MAUI
`RequestOtpAsync(email)`

Danach OTP-Eingabe.

### Web aktuell
✅ `OtpRequestForm.tsx` fordert den Einladungs-/Erstlogin-Code über
`OtpFlow.tsx` → `auth-service.ts` → `auth-repository.ts` an.

### Ziel
```text
features/auth/
  OtpRequestForm.tsx

services/auth/
  auth-service.ts

repositories/auth/
  auth-repository.ts
```

## 1.5 OTP prüfen

### WPF
Eigener Loginabschnitt mit:
- OTP-Code
- Code prüfen
- zurück zum Login

### MAUI
`VerifyOtpAsync(email, code)`

Bei Erfolg Wechsel in den Passwort-setzen-Modus.

### Web aktuell
✅ `OtpVerifyForm.tsx` prüft den OTP-Code über den bestehenden Auth-Service
und wechselt bei Erfolg in den Passwort-setzen-Modus.

### Ziel
```text
features/auth/
  OtpVerifyForm.tsx
```

Flow:
```text
login
↓
otp-requested
↓
set-password
↓
login
```

## 1.6 Neues Passwort nach OTP setzen

### MAUI-Fachstand
Passwort muss erfüllen:
- mindestens 8 Zeichen
- Großbuchstaben
- Kleinbuchstaben
- mindestens eine Zahl
- mindestens ein Sonderzeichen
- Wiederholung stimmt überein

### Web aktuell
✅ `SetPasswordForm.tsx` setzt das Passwort über
`OtpFlow.tsx` → `auth-service.ts` → `auth-repository.ts`.

- die OTP-Prüfung liefert einen kurzlebigen Recovery-Kontext
- dieser wird nur im OTP-Flow gehalten und nicht als normale App-Session gespeichert
- Passwortregeln entsprechen dem MAUI-Fachstand
- nach erfolgreichem Passwortsetzen kehrt der Flow zum normalen Login zurück

### Ziel
```text
features/auth/
  SetPasswordForm.tsx

services/auth/
  auth-service.ts

lib/auth/
  password-policy.ts
```

## 1.7 Passwort vergessen

### WPF
`ResetPasswordWindow`

### MAUI
`SendPasswordResetEmailAsync(email)`

Danach:
OTP → prüfen → neues Passwort setzen.

### Web aktuell
✅ Der Login bietet einen getrennten „Passwort vergessen“-Einstieg.
Dieser fordert den Recovery-Code über `OtpFlow.tsx` → `auth-service.ts` →
`auth-repository.ts` mit der serverseitigen KGV-Zugangsprüfung von
`kgv-request-first-login-otp` an und verwendet anschließend dieselben
Recovery-OTP-, SetPassword- und Passwortregel-Komponenten wie der Erstlogin.

- der Recovery-Kontext bleibt ausschließlich kurzlebig im OTP-Flow
- nach erfolgreichem Passwortsetzen kehrt der Flow zum normalen Login zurück
- `sendPasswordReset` in `lib/supabase-auth.ts` bleibt für bestehende
  eingeloggte Verwaltungsbereiche unverändert

### Ziel
```text
features/auth/
  OtpFlow.tsx
  OtpRequestForm.tsx

services/auth/
  auth-service.ts

repositories/auth/
  auth-repository.ts
```

## 1.8 Verein wechseln

### MAUI
- Bestätigung
- abmelden
- Vereinskontext entfernen
- lokale Anmeldedaten zurücksetzen
- zurück zur Vereinsauswahl

### Web aktuell
✅ vorhanden:
- Bestätigungsabfrage über `ChangeClubAction.tsx`
- Edit-Locks werden vor dem Wechsel freigegeben
- vorhandene Session wird vor dem lokalen Löschen abgemeldet
- lokaler Vereins-, Session- und Workspace-Kontext wird über `clearClub()` gelöscht
- danach erscheint die Vereinsauswahl
- Fehler bei serverseitigem Lock-Release oder Abmelden blockieren den lokalen Wechsel nicht

### Ziel
```text
features/auth/
  ChangeClubAction.tsx

services/auth/
  club-service.ts
```

## 1.9 Session und Inaktivitäts-Logout

### Web aktuell
- Session aus LocalStorage
- 15-Minuten-Inaktivitäts-Logout
- Aktivität über mehrere Browser-Tabs synchronisiert
- Edit-Locks werden beim Logout freigegeben

### Erledigt in G1.9a
- Der 15-Minuten-Inaktivitätsmonitor wurde aus `page.tsx` nach
  `services/auth/session-service.ts` ausgelagert.
- Cross-Tab-Aktivität, Activity-Key je Verein und Benutzer sowie die gedrosselte
  LocalStorage-Aktualisierung bleiben erhalten.
- Das bestehende Logout- und Edit-Lock-Verhalten bleibt unverändert.

### Erledigt in G1.9b
- Ein gespeicherter Refresh-Token wird beim Session-Restore verwendet.
- Ein abgelaufener oder kurz vor Ablauf stehender Access-Token kann beim Start
  erneuert werden; neue Tokens werden in derselben BrowserSession gespeichert.
- Eine ungültige Refresh-Session wird lokal verworfen.

### Erledigt in G1.9c
- Der Access-Token wird während einer laufenden Sitzung rechtzeitig erneuert.
- Der React-Session-State erhält die erneuerten Tokens, ohne die
  15-Minuten-Inaktivitätsfrist zu verlängern.
- Ungültige Refresh-Tokens führen kontrolliert zu signed-out; transiente
  Netzwerkfehler löschen die gespeicherte Session nicht sofort.

### Erledigt in G1.9d
- Der Auth-/Session-State und sein Lifecycle liegen zentral in
  `features/auth/AuthProvider.tsx`.
- `page.tsx` konsumiert den Auth-Context und enthält keinen Auth-Lifecycle mehr.
- Startup-Restore, Login, Logout, Vereinswechsel, Inaktivitätsmonitor,
  Live-Refresh und Cross-Tab-Session-Synchronisation bleiben dort gebündelt.

### Bewertung
✅ Diese Browser-Erweiterungen sollen erhalten bleiben.

### Abschluss
✅ G1.9 ist abgeschlossen. Token-Refresh verlängert die 15-Minuten-
Inaktivitätsfrist nicht.

### Ziel
```text
features/auth/
  AuthProvider.tsx

services/auth/
  session-service.ts

repositories/auth/
  auth-repository.ts
```

## G1 Zielstruktur

```text
KGV.Web/
  app/
    page.tsx

  features/
    auth/
      AuthProvider.tsx
      ClubSelection.tsx
      LoginForm.tsx
      OtpRequestForm.tsx
      OtpVerifyForm.tsx
      SetPasswordForm.tsx
      ChangeClubAction.tsx

  components/
    scanner/
      QrScanner.tsx

  services/
    auth/
      auth-service.ts
      club-service.ts
      session-service.ts

  repositories/
    auth/
      auth-repository.ts
      club-repository.ts

  models/
    auth/
      club.ts

  lib/
    auth/
      password-policy.ts
```

### Abschluss G1
✅ G1 – Anmeldung & Vereinsauswahl ist abgeschlossen. Die vorhandenen
Auth-Komponenten decken Vereinsauswahl, Login, Erstlogin-/Recovery-OTP,
Passwortsetzen, Vereinswechsel sowie Session- und Refresh-Lifecycle ab.
`AuthFlow.tsx`, `ForgotPasswordForm.tsx` und `password-service.ts` werden in
der gewählten Architektur nicht benötigt und sind keine offenen Pflichtdateien.

---

# G2 – Startseite & Navigation

## 2.1 Hauptlayout / Navigation

### WPF
`MainWindow.xaml`

Grundaufbau:
```text
Sidebar links
+ Saisonwahl
+ Navigation
+ Content rechts
```

Mitgliedsbezogene Navigation erscheint abhängig vom ausgewählten Mitglied.

### MAUI
`AdminShell.cs` / `UserShell.cs`

Navigation wird anhand der Benutzerrechte aufgebaut.

### Web aktuell
✅ Grundstruktur bereits sehr passend:
- Kopfbereich
- linke Sidebar
- Saisonwahl
- rechte Contentfläche
- aktive Navigation
- rollenabhängige Menüpunkte

### Bewertung
Der Web-Aufbau liegt optisch/konzeptionell bereits näher an WPF als an MAUI.

**Das soll so bleiben.**

### Offen
`Workspace` ist noch Bestandteil der großen `page.tsx`.

### Ziel
```text
app/(workspace)/
  layout.tsx

features/navigation/
  Navigation.tsx
  NavigationConfig.ts
  MobileNavigation.tsx
  SeasonPicker.tsx
```

## 2.2 Rechteabhängige Navigation

### Web aktuell
Menüpunkte werden anhand des Permission-Kontexts erzeugt.

✅ Fachlich bereits umfangreich.

### Ziel
Die UI soll die Berechtigungsbits nicht selbst zusammensetzen.

Stattdessen z. B.:
```ts
navigationService.getNavigation(userContext)
```

### Zielstruktur
```text
services/workspace/
  navigation-service.ts
```

`navigation-service.ts` verwendet die zentrale Rechteauswertung aus G13. Er definiert weder Rollenbasis noch Permission-Bitmasken selbst.

## 2.3 Mitgliedskontext in Navigation

WPF und MAUI besitzen beide einen ausgewählten Mitgliedskontext.

Web besitzt ebenfalls:
- ausgewähltes Mitglied
- ausgewählte Parzelle
- saisonbezogenen Kontext

### Bewertung
✅ Konzept richtig.

### Offen
State ist momentan stark an `Workspace` gekoppelt.

### Ziel
```text
contexts/
  WorkspaceContext.tsx

services/workspace/
  workspace-service.ts
```

## 2.4 Mobile Navigation

### Web
Bei kleinen Displays wird die Sidebar durch eine mobile Navigation ersetzt.

### Bewertung
✅ Sinnvolle Web-Erweiterung.

## 2.5 Startseite

### WPF
`HomeView.xaml`

### MAUI
`HomePage.xaml.cs`
`HomeViewModel.cs`

### Web aktuell
`HomeDashboard` wurde aus `KGV.Web/app/page.tsx` ausgelagert nach:

```text
features/home/HomeDashboard.tsx
services/home/home-service.ts
repositories/home/home-repository.ts
```

Die Schichtung ist jetzt `HomeDashboard → home-service → home-repository → Supabase`.
Die UI lädt und schreibt nicht mehr direkt, sondern verwendet den Fachservice für:

- Arbeitseinsätze
- Termine
- Bekanntmachungen
- Pflichtstunden
- Arbeitseinsatz-Anmeldungen

### Bewertung
✅ Dashboard-UI und Datenzugriff sind getrennt; `page.tsx` behält nur die minimale Detail-Schnittstelle. Die weitere Auslagerung von `HomeDetail` bleibt innerhalb G2 offen.

### Datenzugriff
Das Repository kapselt die Abfragen und RPCs für:
```text
v_startseite_arbeitseinsatz
v_startseite_termine
v_startseite_bekanntmachungen
v_pflichtstunden_uebersicht
arbeitseinsatz_anmeldung
```

Das ist bis zur späteren fachbereichsübergreifenden Konsolidierung die zuständige Home-Aggregationsstelle. G2 offen: `HomeDetail`, Impressum und Workspace-Layout bleiben unverändert ausgelagert bzw. zu strukturieren.

## 2.6 Startseiten-Detail

### Web aktuell
✅ vorhanden:
- Detailansicht
- Vorheriger/Nächster Eintrag
- Arbeitseinsatz-Anmeldung
- Abmeldung
- Teilnehmerübersicht für Verwaltung
- Wechsel in Verwaltungsansicht

### Ziel
```text
features/home/
  HomeDetail.tsx
```

Lesedaten können über `home-service.ts` aggregiert werden. An-/Abmeldung zu Arbeitseinsätzen wird jedoch an den Registrierungsservice aus G8 delegiert; `HomeDetail.tsx` implementiert diese Statuslogik nicht selbst.

## 2.7 Impressum

### WPF
`ImpressumView.xaml`

### MAUI
`ImpressumPage.cs`

### Web
✅ vorhanden.

### Ziel
```text
features/imprint/
  ImprintPage.tsx

services/imprint/
  imprint-service.ts

repositories/imprint/
  imprint-repository.ts
```

## 2.8 Saisonkontext

### Web
Saisonwahl und Speicherung des Workspace-Kontextes sind vorhanden.

### Bewertung
✅ sinnvoll umgesetzt.

### Ziel
```text
features/navigation/
  SeasonPicker.tsx

services/workspace/
  workspace-service.ts

repositories/seasons/
  season-repository.ts
```

`season-repository.ts` ist dasselbe gemeinsame Saison-Repository, das in G12 fachlich verwaltet wird; G2 verwendet es nur lesend für den Saisonkontext.

## G2 Zielstruktur

```text
KGV.Web/
  app/
    (workspace)/
      layout.tsx
      page.tsx

  features/
    navigation/
      Navigation.tsx
      NavigationConfig.ts
      MobileNavigation.tsx
      SeasonPicker.tsx

    home/
      HomeDashboard.tsx
      HomeWorkAssignments.tsx
      HomeAppointments.tsx
      HomeAnnouncements.tsx
      HomeWorkHoursSummary.tsx
      HomeDetail.tsx

    imprint/
      ImprintPage.tsx

  services/
    workspace/
      workspace-service.ts
      navigation-service.ts

    home/
      home-service.ts

    imprint/
      imprint-service.ts

  repositories/
    seasons/
      season-repository.ts

    imprint/
      imprint-repository.ts

  contexts/
    WorkspaceContext.tsx
```

---

# G3 – Mitglieder

## 3.1 Mitgliedersuche

### WPF
`MemberSearchView.xaml`

Enthält:
- Suchfeld
- Mitgliederliste
- Gartennummernsuche
- neues Mitglied
- sortierbare Spalten

WPF enthält außerdem noch Debug-Ausgaben in der Oberfläche.

Diese sollen **nicht** übernommen werden.

### MAUI
`MemberSearchPage`
`MemberSearchViewModel`

### Web aktuell
✅ vorhanden.

Suche gleichzeitig nach:
- Name
- Vorname
- E-Mail
- Mitgliedsnummer
- Gartennummer

### Bewertung
Dieser Web-Ansatz ist fachlich besser als die alte WPF-Umschaltung „nach Gartennummer suchen“.

**Beibehalten.**

## 3.2 Suchdaten und Gartennummern

### Web aktuell
Die UI lädt derzeit selbst:
```text
mitglied
parzelle
parzellen_belegung
```

und baut daraus die Gartennummern eines Mitglieds zusammen.

### Änderung
Das gehört in den Fachservice.

UI soll nur erhalten:
```ts
MemberSearchResult {
    id
    name
    vorname
    email
    aktiv
    gartenNummern
}
```

### Ziel
```ts
memberService.searchMembers({
  query,
  includeInactive
})
```

## 3.3 Aktive / inaktive Mitglieder

### Web aktuell
✅ eigener Filter vorhanden.

### Ziel
Filterparameter an:
```ts
memberService.searchMembers(...)
```

## 3.4 Mitglied auswählen / Mitgliedskontext

### Web aktuell
✅ vorhanden.

Auswahl setzt Mitgliedskontext und stellt die mitgliedsbezogene Navigation bereit.

### Ziel
Nicht direkt im großen Workspace-State verwalten.

```text
contexts/
  WorkspaceContext.tsx
```

## 3.5 Mitgliedsstammdaten

### WPF
`MemberDetailView` / `StammdatenView`

### MAUI
`MemberDetailPage` / `MeineDatenPage`

### Web
✅ umfangreich umgesetzt.

### Offen
Komponente enthält derzeit noch zu viel eigene Datenlogik.

### Ziel
```text
features/members/
  MemberDetails.tsx
  MemberEditor.tsx

services/members/
  member-service.ts

repositories/members/
  member-repository.ts
```

Service z. B.:
```ts
getMember(...)
updateMember(...)
endMembership(...)
```

## 3.6 Mitglied neu anlegen

### Web
✅ bereits vorhanden bzw. vorbereitet.

### Ziel
```text
features/members/
  MemberCreate.tsx
```

UI ruft:
```ts
memberService.createMember(...)
```

auf.

## 3.7 Nebenmitglied

### WPF
eigene Nebenmitglied-Funktionen.

### MAUI
`NebenmitgliedPage.cs`

### Web
🟡 Funktionalität teilweise mit Stammdaten gekoppelt.

### Empfehlung
Fachlich klarer trennen.

```text
features/members/
  SecondaryMember.tsx

services/members/
  secondary-member-service.ts

repositories/members/
  secondary-member-repository.ts
```

## 3.8 Gärten des Mitglieds

### MAUI
`MemberGardensPage.cs`

### Web
✅ eigener Workspace bereits vorhanden.

### Architektur
Die Mitgliedsseite darf Parzellen-Komponenten wiederverwenden. Der fachliche Zugriff gehört jedoch zu G5:

```ts
parcelService.getParcelsForMember(memberId)
```

Ein optionaler Member-Facade darf diesen Aufruf nur delegieren, aber keine zweite Join-/Repository-Logik implementieren.

## 3.9 Wartungsverträge eines Mitglieds

### MAUI
`MemberWartungsvertraegePage.cs`

### Web
✅ vorhanden.

### Ergebnis nach G11
`MemberMaintenanceContracts.tsx` bleibt eine mitgliedsbezogene Darstellung, verwendet aber die Wartungsvertrags- und Zuordnungsservices aus G11. Es entsteht kein zweites Wartungsvertrags-Repository unter G3.

## 3.10 Admin / Rechte eines Mitglieds

### Web
✅ vorhanden.

### Ergebnis nach G13
Rollenbasis, Grants/Revocations und effektive Rechte werden ausschließlich durch den zentralen Access-/Permission-Bereich aus G13 ausgewertet. Die Mitgliedsseite stellt diesen Bereich nur im gewählten Mitgliedskontext dar.

## 3.11 Mitgliedsprotokolle

### Web
✅ vorhanden.

### Ergebnis nach G5/G10
Die Protokollfachlogik bleibt dem Parzellenbereich G5 zugeordnet. Entsteht daraus eine Datei oder wird eine Datei geöffnet/gespeichert, verwendet der Flow die gemeinsame Dokument-Infrastruktur aus G10. G3 baut dafür keine eigene Protokoll- oder Dokument-Datenzugriffsschicht auf.

## 3.12 Eigene Daten

Normale Nutzer greifen fachlich auf denselben Mitgliedsdatensatz zu.

### Ziel
Die UI darf wiederverwendet werden, aber der Service muss zwischen:
```text
eigene Daten bearbeiten
```
und
```text
fremdes Mitglied als Vorstand/Admin bearbeiten
```
unterscheiden.

Berechtigungen dürfen nicht ausschließlich durch ausgeblendete Buttons realisiert werden.

## G3 Zielstruktur

```text
KGV.Web/
  app/
    (workspace)/
      mitglieder/
        page.tsx

  features/
    members/
      MemberSearch.tsx
      MemberList.tsx
      MemberDetails.tsx
      MemberEditor.tsx
      MemberCreate.tsx
      SecondaryMember.tsx
      MemberGardens.tsx
      MemberMaintenanceContracts.tsx
      MemberProtocols.tsx

  services/
    members/
      member-service.ts
      secondary-member-service.ts

  repositories/
    members/
      member-repository.ts
      secondary-member-repository.ts

  models/
    members/
      member.ts
      member-search.ts
```

## Vorgesehene Member-Service-Funktionen

```ts
searchMembers(...)
getMember(...)
createMember(...)
updateMember(...)
endMembership(...)
```

---

# G4 – Mitgliedsantrag & Verträge

## 4.1 Mitgliedsantrag erzeugen

### WPF
`MitgliedsantragDialog.xaml`

WPF stellt vor der Erzeugung insbesondere dar:
- Beginn
- Jahresbeitrag
- Hinweis auf vollen/halben Beitrag
- anpassbarer Mitgliedsbeitrag

### MAUI
`MitgliedsantragDialogPage.cs` und `MitgliedsantragPreviewPage.cs`

MAUI ist hier fachlich führend. Der Ablauf umfasst:
- Mitgliedsdaten laden
- Beitragswerte bestimmen
- Vorschau erzeugen
- Unterschriften erfassen
- signiertes Dokument speichern
- Dokumentstatus prüfen

### Web aktuell
Der Webbereich besitzt bereits einen `ContractComposer` innerhalb von `app/page.tsx`.

Vorhanden:
- Dokumenttyp `mitgliedsantrag`
- Beginn
- Mitgliedsbeitrag
- Aufnahmegebühr
- PDF-Vorschau
- Unterschrift Mitglied
- optionale zweite/Vertreter-Unterschrift
- Unterschrift Verein
- Finalisieren und Speichern

### Bewertung
🟡 Grundfunktion ist vorhanden, aber der Browser-Flow ist derzeit zu generisch und bildet die aktuelle MAUI-Fachlogik nicht vollständig sichtbar ab.

### Offen
- Beitragsermittlung wie MAUI/WPF eindeutig übernehmen
- Status eines bestehenden signierten/unsignierten Antrags berücksichtigen
- Dokumentstatus und Wiederaufnahme eines Flows sauber abbilden
- Logik aus `page.tsx` lösen
- gesetzlicher Vertreter fachlich korrekt integrieren

### Ziel
```text
features/contracts/
  MembershipApplicationForm.tsx
  MembershipApplicationPreview.tsx

services/contracts/
  membership-application-service.ts

repositories/contracts/
  contract-repository.ts
```

Die generische Dokumentablage wird nicht in G4 dupliziert. Vorschauen und Vertragsgenerierung gehören zu G4; das Ablegen/Öffnen des fertigen Dokuments verwendet die gemeinsame Dokument-Infrastruktur aus G10.

Die UI ruft beispielsweise:
```ts
membershipApplicationService.prepare(...)
membershipApplicationService.preview(...)
membershipApplicationService.finalize(...)
```

und kennt keine Edge-Function-, Tabellen- oder Storage-Details.

---

## 4.2 Gesetzlicher Vertreter

### MAUI
Für Minderjährige existieren zwei fachlich verschiedene Fälle:

1. **Vertreter ist bereits Mitglied**
   - keinen neuen Vertreterdatensatz erzeugen
   - nur Beziehung zum vorhandenen Mitglied speichern

2. **Vertreter ist noch kein Mitglied**
   - Vertreterdatensatz anlegen
   - anschließend Beziehung speichern

Diese Trennung ist zwingend beizubehalten.

### Web aktuell
Der generische `ContractComposer` besitzt lediglich eine optionale zweite Unterschrift mit der Beschriftung `Gesetzlicher Vertreter (optional)`.

Eine vollständige Vertreterauswahl mit beiden MAUI-Fällen ist in diesem Flow nicht erkennbar.

### Bewertung
❌ Fachliche Lücke.

### Ziel
```text
features/contracts/
  LegalRepresentativeSelector.tsx
  LegalRepresentativeForm.tsx

services/contracts/
  legal-representative-service.ts

repositories/contracts/
  legal-representative-repository.ts
```

Service-Funktionen beispielsweise:
```ts
findExistingRepresentative(...)
linkExistingRepresentative(...)
createAndLinkRepresentative(...)
getRepresentativeForMember(...)
```

Das Repository kennt `mitglied_gesetzlicher_vertreter`; die UI nicht.

---

## 4.3 Mitgliedsantrag-Vorschau

### MAUI
Eigene Preview-Seite vor der endgültigen Ablage.

### Web aktuell
`generateContract(..., action: "preview")` liefert eine Vorschau-URL und öffnet diese in einem neuen Browserfenster/-tab.

### Bewertung
✅ technisch sinnvoll, aber aktuell stark an den generischen ContractComposer gekoppelt.

### Ziel
```text
features/contracts/
  ContractPreview.tsx
```

Service:
```ts
membershipApplicationService.preview(...)
```

---

## 4.4 Unterschriften

### MAUI
Eigene `VertragsSignaturPage.cs`.

### Web aktuell
`SignaturePad` mit Canvas ist bereits vorhanden.

### Bewertung
✅ guter browserspezifischer Ansatz.

### Offen
- aus `page.tsx` lösen
- Signaturzustand nicht im generischen ContractComposer verstreuen
- Vertreter-/Pächterrollen eindeutig fachlich benennen
- Pflicht-/Optional-Regeln über Service liefern

### Ziel
```text
components/signature/
  SignaturePad.tsx

features/contracts/
  ContractSignatures.tsx
```

---

## 4.5 Pachtvertrag vorbereiten

### WPF
Pachtvertrag ist aus dem Mitglied-/Parzellenkontext heraus erreichbar.

### MAUI
`PachtvertragDialogPage.cs` und `PachtvertragFlowHelper`.

Wesentliche Fachregel:
**Ein Pachtvertrag darf erst erzeugt werden, wenn ein signierter Mitgliedsantrag vorhanden ist.**

### Web aktuell
`ContractComposer` bietet `pachtvertrag` als auswählbaren Dokumenttyp und lädt aktive Parzellen des Mitglieds.

### Bewertung
🟡 technisch vorhanden, fachliche Sperre ist im sichtbaren Web-Flow nicht gleichwertig zu MAUI abgesichert.

### Offen
- `HasSignedMitgliedsantrag` vor Erzeugung prüfen
- Pachtvertrag stärker an konkrete Parzellenzuordnung binden
- Status vorhandener signierter/unsignierter Pachtverträge berücksichtigen

### Ziel
```text
features/contracts/
  LeaseContractForm.tsx
  LeaseContractPreview.tsx

services/contracts/
  lease-contract-service.ts
```

Service:
```ts
canCreateLeaseContract(memberId, parcelId)
prepareLeaseContract(...)
previewLeaseContract(...)
finalizeLeaseContract(...)
```

---

## 4.6 Pachtvertrag nach Parzellenzuweisung

### MAUI
`MemberGardenAssignPage.cs` verwendet einen klaren Produktivworkflow:

1. freie Parzelle zuweisen
2. prüfen, ob signierter Mitgliedsantrag existiert
3. nur dann Pachtvertrag anbieten
4. andernfalls Hinweis anzeigen

### Web aktuell
Parzellenzuweisung und ContractComposer sind getrennt und der gleiche fachliche Folgeablauf ist nicht erkennbar.

### Bewertung
❌ sollte an MAUI angeglichen werden.

### Ziel
Der Parzellen-Service löst nach erfolgreicher Zuweisung einen fachlichen Folgeentscheid aus, z. B.:

```ts
const result = await parcelAssignmentService.assignParcel(...)

if (result.canCreateLeaseContract) {
   // UI bietet Pachtvertrag an
}
```

Die UI entscheidet nicht selbst durch Datenbankabfragen, ob ein Vertrag erlaubt ist.

---

## 4.7 Bestehenden Pachtvertrag öffnen / unsignierten verwerfen

### MAUI
`MemberParzellenDetailPage.cs` unterscheidet:
- Pachtvertrag erzeugen
- signierten Pachtvertrag öffnen
- unsignierten Pachtvertrag verwerfen

### Web aktuell
Diese klare Statussteuerung ist im generischen ContractComposer nicht gleichwertig sichtbar.

### Bewertung
❌ offen.

### Ziel
```text
services/contracts/
  lease-contract-service.ts
```

mit z. B.:
```ts
getLeaseContractState(...)
openSignedLeaseContract(...)
discardUnsignedLeaseContract(...)
```

---

## G4 Zielstruktur

```text
KGV.Web/
  features/
    contracts/
      MembershipApplicationForm.tsx
      MembershipApplicationPreview.tsx
      LegalRepresentativeSelector.tsx
      LegalRepresentativeForm.tsx
      LeaseContractForm.tsx
      LeaseContractPreview.tsx
      ContractPreview.tsx
      ContractSignatures.tsx

  components/
    signature/
      SignaturePad.tsx

  services/
    contracts/
      membership-application-service.ts
      legal-representative-service.ts
      lease-contract-service.ts

  repositories/
    contracts/
      contract-repository.ts
      legal-representative-repository.ts
```

Fertige Vertragsdokumente werden über G10 (`document-service` / `document-repository`) abgelegt und geöffnet. G4 besitzt keine zweite generische Dokumentablage.

### G4 Kernaussage

Der aktuelle Web-`ContractComposer` soll **nicht nur in eine andere Datei verschoben** werden. Er sollte fachlich in Mitgliedsantrag, Vertreter und Pachtvertrag getrennt werden.

---

# G5 – Parzellen

## 5.1 Parzellenverwaltung

### WPF
`ParzellenVerwaltungView.xaml`

WPF ist die Layoutreferenz für die Verwaltungsansicht.

### MAUI
`ParzellenPage.cs`

MAUI liefert die aktuelle Fachlogik für:
- Parzellen laden
- Belegungen berücksichtigen
- Mitgliedskontext
- Detailnavigation
- Rechteprüfung

### Web aktuell
`ParcelWorkspace` befindet sich bereits in `app/parcel-workspaces.tsx`.

### Bewertung
✅ erste strukturelle Trennung gegenüber `page.tsx` ist bereits vorhanden.

### Offen
Auch `parcel-workspaces.tsx` enthält noch UI und Datenzugriff zusammen und sollte weiter fachlich zerlegt werden.

### Ziel
```text
features/parcels/
  ParcelList.tsx
  ParcelDetails.tsx
  ParcelEditor.tsx

services/parcels/
  parcel-service.ts

repositories/parcels/
  parcel-repository.ts
```

---

## 5.2 Parzellenliste und Auswahl

### Web aktuell
Parzellen werden geladen, angezeigt und im Workspace-Kontext ausgewählt.

### Ziel
UI:
```ts
parcelService.searchParcels(...)
parcelService.getParcel(...)
```

Repository kennt:
```text
parzelle
parzellen_belegung
```

---

## 5.3 Gärten eines Mitglieds

### MAUI
`MemberGardensPage.cs`

Lädt:
- ausgewähltes Mitglied
- dessen Belegungen
- zugehörige Parzellen
- aktive und historische Zuordnungen

### Web aktuell
`MemberGardensWorkspace` ist vorhanden.

### Bewertung
✅ fachlicher Bereich vorhanden.

### Architektur
Die gemeinsame Parzellenlogik darf wiederverwendet werden, aber der Mitgliedskontext wird über Service-Funktion geladen:

```ts
parcelService.getParcelsForMember(memberId)
```

Nicht direkt mit mehreren `readSupabase`-Aufrufen in der UI.

---

## 5.4 Parzelle einem Mitglied zuordnen

### MAUI
`MemberGardenAssignPage.cs`

Fachregeln:
- gültiger Mitgliedskontext erforderlich
- entsprechendes Fachrecht erforderlich
- nur aktuell freie Parzellen anbieten
- Zuweisungsdatum erfassen
- bestehende aktive Belegungen ausschließen

### Web aktuell
Parzellen-/Belegungsverwaltung ist vorhanden, aber der MAUI-Zuweisungsworkflow muss vollständig gegen die Weblogik abgeglichen werden.

### Ziel
```text
features/parcels/
  ParcelAssignmentForm.tsx

services/parcels/
  parcel-assignment-service.ts

repositories/parcels/
  parcel-assignment-repository.ts
```

Service:
```ts
getAssignableParcels(...)
assignParcelToMember(...)
endParcelAssignment(...)
```

---

## 5.5 Folgeaktion Pachtvertrag

Nach erfolgreicher Zuweisung gehört die Vertragsentscheidung fachlich zum Workflow, obwohl der Vertrag selbst G4 ist.

Servicegrenze:

```text
parcel-assignment-service
        ↓
liefert Zuweisungsergebnis
        ↓
lease-contract-service
```

Keine direkte Vertragsgenerierung in `ParcelAssignmentForm.tsx`.

---

## 5.6 Mitgliedsbezogene Parzellen-Detailansicht

### MAUI
`MemberParzellenDetailPage.cs`

Wichtig:
Diese Seite ist bewusst nur im Pfad **„Gärten des Mitglieds“** vorgesehen.

Sie bietet:
- Parzellenstammdaten
- Strom
- Wasser
- Dokumente
- Pachtvertrag
- Status des Mitgliedsantrags/Pachtvertrags

### Web aktuell
Eine entsprechende Detail-/Workspace-Struktur existiert bereits in `parcel-workspaces.tsx`.

### Bewertung
🟡 funktional vorhanden, aber fachliche Unterbereiche sind noch zu stark zusammengekoppelt.

### Ziel
```text
features/parcels/
  MemberParcelDetails.tsx
  ParcelQuickActions.tsx
```

---

## 5.7 Strom / Wasser aus Parzellenkontext

### WPF
Eigene Bereiche:
- `GartenStromView.xaml`
- `GartenWasserView.xaml`

### MAUI
Die Detailseite öffnet die entsprechenden Ablese-/Zählerflows.

### Web
Diese Funktionen sollen nicht im Parzellenservice dupliziert werden.

### Ziel
Parzellen-UI delegiert an G6:

```ts
meterService.getParcelMeterState(parcelId, "strom")
meterService.getParcelMeterState(parcelId, "wasser")
```

G5 kennt den Navigations-/Kontextübergang, G6 besitzt die Zählerfachlogik.

---

## 5.8 Parzellendokumente

### WPF
`GartenDokumenteView.xaml`

### MAUI
Parzellendetail öffnet Dokumentbereich.

### Web
Dokumentanzeige ist bereits mit `parcelId` möglich.

### Architektur
Dokumentlogik gehört fachlich zu G10.

G5 ruft nur:
```ts
documentService.getDocumentsForParcel(parcelId)
```

---

## 5.9 Parzellenprotokolle

### MAUI
`ParzellenProtokollePage.cs`

### Web
`ParcelProtocolsWorkspace` ist bereits vorhanden.

### Bewertung
✅ vorhanden.

### Ziel
```text
features/parcels/
  ParcelProtocols.tsx

services/parcels/
  parcel-protocol-service.ts
```

Nach G10 gilt: Die **Protokollfachlogik** bleibt in G5. Nur die Dateiablage bzw. das Öffnen eines erzeugten Protokolldokuments wird an die gemeinsame Dokument-Infrastruktur aus G10 delegiert. Es entsteht keine doppelte Dokumentlogik.

---

## 5.10 Parzellenablesungen

### MAUI
`ParzellenAblesungenPage.cs`

### Web
Zählerstände sind bereits aus dem Parzellen-/Zählerbereich erreichbar.

### Architektur
Die eigentliche Fachlogik gehört zu G6.

G5 stellt nur den Parzellenkontext bereit.

---

## G5 Zielstruktur

```text
KGV.Web/
  app/
    (workspace)/
      parzellen/
        page.tsx

  features/
    parcels/
      ParcelList.tsx
      ParcelDetails.tsx
      ParcelEditor.tsx
      MemberParcels.tsx
      MemberParcelDetails.tsx
      ParcelAssignmentForm.tsx
      ParcelQuickActions.tsx
      ParcelProtocols.tsx

  services/
    parcels/
      parcel-service.ts
      parcel-assignment-service.ts
      parcel-protocol-service.ts

  repositories/
    parcels/
      parcel-repository.ts
      parcel-assignment-repository.ts
      parcel-protocol-repository.ts

  models/
    parcels/
      parcel.ts
      parcel-assignment.ts
```

### G5 Kernaussage

`parcel-workspaces.tsx` ist bereits ein guter erster Schritt weg von der großen `page.tsx`, soll aber ebenfalls weiter getrennt werden:

```text
UI
↓
parcel-service / parcel-assignment-service
↓
parcel-repository
↓
Supabase
```

Die Querschnittsfunktionen bleiben klar abgegrenzt:
- Verträge → G4
- Zähler/Ablesungen → G6
- Dokumente → G10

---

# G6 – Zähler & Ablesungen

## 6.1 Ablesen-Übersicht

### WPF
`AblesenOverviewView.xaml`

WPF zeigt den Bereich als Funktionsübersicht mit eigenen Kacheln für:

- Ablesung erfassen
- Zählerwechsel
- RFID einrichten
- Fällige Zähler
- Ablesungen freigeben

Die Sichtbarkeit wird anhand der Fachrechte gesteuert.

### MAUI
`AblesenOverviewPage.cs`

MAUI ergänzt zusätzlich:

- Nutzerablesungen können zentral aktiviert/deaktiviert werden
- eigene Nutzerablesungen werden als Einreichung gespeichert
- Rollen mit entsprechender Berechtigung können direkt freigegeben speichern
- ausstehende Foto-Uploads werden beim Einstieg synchronisiert

### Web aktuell
`MeterOverview` liegt noch in `app/page.tsx`.

Vorhanden:

- Zählerübersicht
- Ablesung erfassen
- Jahresendablesung
- Eichfälligkeit
- offene Prüfungen
- Prüfverlauf
- Foto-Upload-Warteschlange
- Zählerwechsel
- Parzellenfilter

### Bewertung
✅ Funktional bereits umfangreich.

❌ Die Komponente lädt `zaehler`, `zaehler_ablesung`, `parzelle`, `parzellen_belegung` und `mitglied` direkt selbst.

### Ziel
```text
features/meters/
  MeterOverview.tsx
  MeterSummary.tsx

services/meters/
  meter-service.ts
  meter-permission-service.ts

repositories/meters/
  meter-repository.ts
```

UI:
```ts
meterService.getOverview(...)
```

Die UI kennt keine Tabellen- oder Viewnamen.

---

## 6.2 Rechte und Nutzerablesungen

### WPF / MAUI
Unterschieden werden insbesondere:

- Zähler lesen
- eigene Nutzerablesungen einreichen
- Zählerwechsel verwalten
- Ablesungen freigeben

Zusätzlich wird `allow_user_meter_reading_submissions` berücksichtigt.

### Web aktuell
Das Web besitzt bereits Rechte für:

```text
readMeters
manageMeterChanges
approveMeterReadings
```

Eigene Nutzerablesungen werden zusätzlich über `app_setting.allow_user_meter_reading_submissions` gesteuert.

### Bewertung
🟡 grundsätzlich vorhanden, aber Berechtigungs- und Settingslogik steckt noch in UI-Komponenten.

### Ziel
```text
services/meters/
  meter-access-service.ts
```

zum Beispiel:
```ts
getMeterCapabilities(userContext)
canSubmitOwnReading(...)
canApproveReading(...)
canManageMeterChanges(...)
```

---

## 6.3 Ablesung erfassen

### WPF
`AblesungErfassenView.xaml`

RFID-Kontext wird aufgelöst und vor der Erfassung angezeigt.

### MAUI
`AblesungErfassenPage.cs`

MAUI-Fachstand:

- RFID/NFC oder Ersatzweg über Parzelle + Medium
- aktiven Zähler bestimmen
- normales Ablesen
- Jahresendablesung mit `Art = jea`
- Einbauablesung mit `Art = einbau`
- Datum
- Zählerstand
- Foto
- Foto kann verpflichtend konfiguriert sein
- Nutzerablesung zunächst `eingereicht`
- berechtigte Rollen direkt `freigegeben`
- Stand darf bei chronologisch späterer Ablesung nicht unter der letzten freigegebenen Ablesung liegen
- Duplikate für gleichen Zähler, Datum und Art verhindern

### Web aktuell
`MeterReadingEntry`

Bereits vorhanden:

- normale Ablesung
- JEA
- Datum
- Zählerstand
- Foto
- Foto-Pflicht über `meter_reading_photo_required`
- Nutzerfreigabe über `allow_user_meter_reading_submissions`
- letzte freigegebene Ablesung prüfen
- Duplikatprüfung
- direkt freigegeben oder eingereicht speichern

### Bewertung
✅ fachlich schon sehr weit.

### Offen
- Datenbanklogik vollständig aus der UI lösen
- Einbauablesung als Bestandteil des Zählerwechsel-Flows gleichwertig zu MAUI integrieren
- RFID-Auflösung zentralisieren

### Ziel
```text
features/meters/
  MeterReadingForm.tsx

services/meters/
  meter-reading-service.ts

repositories/meters/
  meter-reading-repository.ts
```

Service:
```ts
prepareReadingContext(...)
validateReading(...)
submitReading(...)
getLastApprovedReading(...)
```

---

## 6.4 RFID-/NFC-Kontext auflösen

### MAUI
Die gemeinsame Fachlogik basiert auf einem klaren Zustandsmodell.

```text
RFID unbekannt
    ↓
RFID einrichten

RFID bekannt
+ kein aktiver Zähler
    ↓
Zählereinbau

RFID bekannt
+ aktiver Zähler
    ↓
Ablesung oder Zählerausbau
```

Die Fachzustände entsprechen:

```text
Unknown
KnownWithoutActiveMeter
KnownWithActiveMeter
```

### Web aktuell
`RfidBrowserScanner` verwendet Web-NFC und liest anschließend `v_rfid_scan_context`.

Bei fehlendem Web-NFC wird auf manuelle Zählerauswahl verwiesen.

### Bewertung
🟡 technische Browser-Unterstützung ist sinnvoll, aber der vollständige fachliche Zustandsautomat ist im Web noch nicht der zentrale Einstieg für alle Zähleraktionen.

### Ziel
```text
features/meters/rfid/
  RfidScanner.tsx
  RfidContextDisplay.tsx

services/meters/
  rfid-service.ts

repositories/meters/
  rfid-repository.ts
```

Service:
```ts
resolveRfid(uid)
getNextMeterAction(context)
```

Beispiel:
```ts
switch (context.state) {
  case "unknown":
    return "setup-rfid"
  case "known-without-active-meter":
    return "install-meter"
  case "known-with-active-meter":
    return "active-meter"
}
```

Die UI entscheidet nicht anhand einzelner Datenbankfelder selbst über den Workflow.

---

## 6.5 RFID einrichten

### WPF
`RfidEinrichtenView.xaml`

### MAUI
`RfidEinrichtenPage.cs`

Aktueller MAUI-Ablauf:

1. zuerst RFID scannen
2. prüfen, ob Tag bereits bekannt ist
3. nur bei unbekanntem Tag:
   - Parzelle wählen
   - Medium wählen
   - ggf. Ausstattung Strom/Wasser pflegen
   - RFID zuordnen
4. bekannten Tag nicht versehentlich erneut zuordnen

### Web aktuell
Ein vollständiger eigener RFID-Einrichtungsflow ist im Browser noch nicht gleichwertig zu MAUI vorhanden.

### Bewertung
❌ offen.

### Ziel
```text
features/meters/rfid/
  RfidSetupForm.tsx

services/meters/
  rfid-service.ts
```

Service:
```ts
resolveRfid(...)
assignRfid(...)
validateRfidAssignment(...)
```

---

## 6.6 Browser-Web-NFC

### Web aktuell
`RfidBrowserScanner` prüft:

- `NDEFReader`
- Secure Context
- UID des Tags
- `v_rfid_scan_context`

### Bewertung
✅ gute browserspezifische Ergänzung.

Web-NFC bleibt jedoch nur ein **Eingabekanal**.

Die Fachlogik darf nicht vom verwendeten Scanner abhängen.

```text
Web-NFC
manuelle UID
MAUI-NFC
        ↓
rfid-service
        ↓
gleicher Fachworkflow
```

---

## 6.7 Fällige Zähler

### WPF
`FaelligeZaehlerView.xaml`

### MAUI
`FaelligeZaehlerPage.cs`

MAUI verwendet die zentrale View:

```text
v_zaehler_eichstatus
```

und bietet Filter nach:

- Garten
- Anlage
- Medium
- Zähler
- Eichstatus

### Web aktuell
Das Web berechnet aktuell selbst aus `zaehler.eichfaellig_am`, welche Zähler innerhalb von zwölf Monaten fällig werden.

### Bewertung
🟡 Ergebnis ist brauchbar, aber die Fachquelle weicht von MAUI ab.

### Empfehlung
Für den endgültigen Webstand ebenfalls die zentrale Fachquelle `v_zaehler_eichstatus` verwenden.

### Ziel
```text
features/meters/
  DueMeters.tsx

services/meters/
  meter-calibration-service.ts

repositories/meters/
  meter-repository.ts
```

Service:
```ts
getDueMeters(filter)
```

---

## 6.8 Ablesungen freigeben

### WPF
`AblesungenFreigabeView.xaml`

### MAUI
`AblesungenFreigabePage.cs`

Vier Aktionen:

- Freigeben
- Ablehnen
- Korrigieren
- Entfernen

Für **alle vier Aktionen ist ein Kommentar verpflichtend**.

Korrigieren:
- Datum und Stand ändern
- anschließend direkt freigeben

Entfernen:
- Einreichung mit Begründung als abgelehnt markieren
- aus offenem Prüfprozess entfernen

### Web aktuell
`ReadingReview`

Bereits vorhanden:

- Freigeben
- Korrigieren
- Ablehnen
- Entfernen
- Pflichtkommentar
- Korrektur von Datum + Stand
- Prüfer
- Prüfdatum
- Prüfverlauf
- Bearbeitungssperre

### Bewertung
✅ sehr nah am aktuellen MAUI-Fachstand.

### Problem
Speicherung erfolgt direkt per `writeSupabase` in der React-Komponente.

### Ziel
```text
features/meters/
  ReadingReview.tsx
  ReadingReviewHistory.tsx

services/meters/
  meter-review-service.ts

repositories/meters/
  meter-reading-repository.ts
```

Service:
```ts
approveReading(...)
rejectReading(...)
correctReading(...)
removeReadingFromReview(...)
```

Pflichtkommentar und Korrekturregeln gehören in den Service.

---

## 6.9 Prüfverlauf

### Web aktuell
Das Web zeigt bereits:

- Entscheidung
- Prüfer
- Kommentar
- Status
- vorherige Ablesungen

### Bewertung
✅ gute PC-/Browser-Erweiterung.

Diese Funktion soll erhalten bleiben.

Sie wird jedoch künftig mit Daten aus:

```ts
meterReviewService.getReviewHistory(...)
```

versorgt.

---

## 6.10 Zählerwechsel – fachlicher Einstieg

### WPF / MAUI
Der Zählerwechsel ist RFID-/Kontext-gesteuert.

Die zentrale Logik lautet:

```text
RFID unbekannt
→ RFID einrichten

RFID bekannt ohne aktiven Zähler
→ Einbau

RFID bekannt mit aktivem Zähler
→ Ausbau
```

### Web aktuell
`MeterChange` verwendet aktuell einen vereinfachten PC-Workflow:

1. aktiven Zähler aus Dropdown auswählen
2. Ausbaudatum + Endstand erfassen
3. neue Zählernummer + Eichdatum erfassen
4. `remove_meter`
5. `create_meter_installation`

### Bewertung
🟡 als Verwaltungs-/Korrekturweg brauchbar.

❌ Er entspricht aber nicht dem produktiven MAUI-RFID-Workflow.

### Empfehlung
Beide Wege dürfen existieren:

```text
Produktiver Scan-Workflow
→ RFID-gesteuert wie MAUI

Verwaltungs-/Korrekturweg
→ manuelle Auswahl nach Parzelle/Zähler
```

Sie müssen aber denselben `meter-change-service` benutzen.

---

## 6.11 Zählerausbau

### MAUI
`ZaehlerwechselAusbauPage.cs`

Bei bekanntem RFID mit aktivem Zähler:

- Zählerkontext übernehmen
- Ausbaudatum
- Endstand
- ggf. Foto
- aktiven Zähler ausbauen

### Web aktuell
Im vereinfachten `MeterChange` wird der Ausbau über RPC `remove_meter` ausgeführt.

### Ziel
```text
features/meters/change/
  MeterRemovalForm.tsx

services/meters/
  meter-change-service.ts
```

Service:
```ts
removeMeter(...)
```

---

## 6.12 Zählereinbau

### MAUI
`ZaehlerwechselEinbauPage.cs`

Bei bekanntem RFID ohne aktiven Zähler:

- Einbaudatum
- Zählernummer
- Eichjahr
- neuen Zähler anlegen

Danach folgt **nicht sofort ein abgeschlossener Gesamtwechsel**, sondern:

```text
Zähler anlegen
↓
Ablese-Flow
↓
Anfangsablesung mit Art = einbau
↓
genau ein Foto in diesem Ableseschritt
```

### Web aktuell
Der Web-`MeterChange` legt den neuen Zähler direkt nach dem Ausbau an, besitzt aber keinen gleichwertigen anschließenden Einbau-Ablesungsflow.

### Bewertung
❌ wichtige fachliche Abweichung.

### Ziel
```text
features/meters/change/
  MeterInstallationForm.tsx

services/meters/
  meter-change-service.ts
```

Service-Ergebnis:
```ts
{
  meterId,
  nextStep: "initial-reading"
}
```

Anschließend wird der gemeinsame `meter-reading-service` mit:

```text
Art = einbau
```

gestartet.

---

## 6.13 Jahresendablesung

### MAUI
Jahresendablesung wird eindeutig mit:

```text
Art = jea
```

gespeichert.

### Web aktuell
✅ vorhanden.

Bei Auswahl JEA setzt Web bereits das Datum auf:

```text
31.12.<Saison>
```

### Ziel
Die Regel kommt künftig aus:

```ts
meterReadingService.prepareYearEndReading(season)
```

und nicht aus der UI-Komponente.

---

## 6.14 Fotos bei Ablesungen

### MAUI
Fotoaufnahme/-auswahl ist Bestandteil des Ableseflows.

Konfigurierbar über:

```text
meter_reading_photo_required
```

### Web aktuell
✅ vorhanden:

- Datei aufnehmen/auswählen
- Vorschau
- Foto-Pflicht
- direkter Upload
- lokale Warteschlange bei Uploadfehler
- optionaler Modus „nur bei erkanntem WLAN direkt hochladen“

### Bewertung
✅ Web bietet hier sogar sinnvolle browserspezifische Zusatzfunktionen.

### Architektur
Foto-Fachlogik soll nicht in `MeterReadingEntry` verbleiben.

```text
services/meters/
  meter-photo-service.ts

repositories/meters/
  meter-photo-repository.ts
```

Lokale Browserwarteschlange bleibt technische Infrastruktur.

---

## 6.15 Pending Foto-Uploads

### Web aktuell
`PendingPhotoUploads`

Vorhanden:

- lokale Speicherung
- Offline-Erkennung
- erneut versuchen
- alle erneut versuchen
- Foto anzeigen
- lokalen Eintrag löschen
- nach erfolgreichem Upload Ablesung mit Drive-Datei verknüpfen

### Bewertung
✅ sehr gute browserspezifische Erweiterung.

G15 bestätigt die Zuständigkeitsgrenze:

- fachliche Zuordnung des Fotos zur Ablesung → G6
- IndexedDB, Online-/Offline-Erkennung und lokale Queue-Technik → G15

`meter-photo-service.ts` aus G6 darf die lokale Queue verwenden, implementiert deren Browsertechnik aber nicht selbst.

---

## G6 Zielstruktur

```text
KGV.Web/
  app/
    (workspace)/
      ablesen/
        page.tsx

  features/
    meters/
      MeterOverview.tsx
      MeterSummary.tsx
      MeterReadingForm.tsx
      DueMeters.tsx
      ReadingReview.tsx
      ReadingReviewHistory.tsx

      rfid/
        RfidScanner.tsx
        RfidContextDisplay.tsx
        RfidSetupForm.tsx

      change/
        MeterChangeStart.tsx
        MeterRemovalForm.tsx
        MeterInstallationForm.tsx

      photos/
        MeterPhotoInput.tsx
        PendingPhotoUploads.tsx

  services/
    meters/
      meter-service.ts
      meter-access-service.ts
      meter-reading-service.ts
      meter-review-service.ts
      meter-change-service.ts
      meter-calibration-service.ts
      meter-photo-service.ts
      rfid-service.ts

  repositories/
    meters/
      meter-repository.ts
      meter-reading-repository.ts
      rfid-repository.ts
      meter-photo-repository.ts

  models/
    meters/
      meter.ts
      meter-reading.ts
      rfid-context.ts
      meter-change.ts
```

## G6 verbindlicher Workflow

```text
RFID / manuelle Kontextwahl
        ↓
rfid-service.resolve(...)
        ↓
┌──────────────────────────────────┐
│ Unknown                          │ → RFID einrichten
│ KnownWithoutActiveMeter          │ → Zähler einbauen
│ KnownWithActiveMeter             │ → Ablesen / Zähler ausbauen
└──────────────────────────────────┘
```

Beim Zählereinbau:

```text
Einbau speichern
↓
neuer aktiver Zähler
↓
gemeinsamer Ablese-Flow
↓
Art = einbau
↓
Anfangsstand + Foto
```

## G6 wichtigste Refactoring-Regel

Der aktuelle Webbereich enthält bereits einen großen Teil der benötigten Funktionalität. Deshalb soll G6 **nicht neu geschrieben**, sondern fachlich zerlegt werden:

```text
React UI
↓
Meter-/RFID-/Review-/Change-Service
↓
Repositories
↓
Supabase Views / Tabellen / RPCs
```

Die vorhandenen guten Browserfunktionen wie Web-NFC-Fallback, Foto-Warteschlange, Offline-Verhalten und Prüfverlauf bleiben erhalten.

---

# G7 – Arbeitsstunden

## 7.1 Arbeitsstunden-Übersicht

### WPF
`ArbeitsstundenView.xaml`

WPF zeigt eine klassische Tabellenansicht mit:

- Mitglied
- Datum
- Saison
- Stunden
- Beschreibung / Art der Arbeit
- Status
- Freigabe
- Freigabedatum
- Freigegeben von
- Neu
- Bearbeiten
- im Prüfmodus zusätzlich Freigeben

### MAUI
`MyArbeitsstundenPage.cs`

MAUI trennt stärker zwischen:

- ruhiger Übersicht
- eigenem Editor für Erfassen/Bearbeiten
- eigener Prüfübersicht
- eigener Prüfdetailseite

Die Übersicht zeigt außerdem den zentral berechneten Pflichtstundenstatus:

- Soll
- geleistet
- offen

### Web aktuell
`OwnWorkHours` liegt noch in `app/page.tsx`.

Vorhanden:

- Arbeitsstundenliste
- Erfassung
- Bearbeitung offener Einträge
- Löschen offener Einträge
- Statusanzeige
- Prüfverlauf
- Soll/Freigegeben/Offen-Zusammenfassung

### Bewertung
🟡 funktional bereits weit, aber die Pflichtstunden-Zusammenfassung ist aktuell fachlich nicht auf dem MAUI-/DB-Stand.

---

## 7.2 Pflichtstunden-Übersicht

### Zentrale Fachquelle
MAUI verwendet:

```text
v_pflichtstunden_uebersicht
```

Die View basiert auf:

```text
fn_berechne_pflichtstunden_status(...)
```

Damit liegt die Pflichtstundenberechnung zentral in der Datenbank und nicht in der UI.

Berücksichtigt werden unter anderem:

- Saison-Sollstunden
- aktive Mitgliedschaft
- Wartungsvertrag mit Pflichtstundenbefreiung
- Altersbefreiung
- Eintritt im zweiten Halbjahr
- freigegebene Arbeitsstunden
- Hauptmitglied + Nebenmitglied gemeinsam
- Euro je Fehlstunde
- Fehlbetrag

### Eintritt im laufenden Jahr

Eintritt ab:

```text
01.07.<Saisonjahr>
```

führt zu:

```text
50 % der Sollstunden
```

Eintritt vor dem 01.07. verwendet die normalen Sollstunden.

### Altersbefreiung

Die zentrale Regel verwendet das am Mitglied gespeicherte Feld:

```text
arbeitsstunden_altersregel_typ
```

mit:

```text
frau75
mann80
```

Daraus folgt:

- `frau75` → Befreiung ab 75
- `mann80` → Befreiung ab 80

### Wartungsvertrag

Ein aktiver zugeordneter Wartungsvertrag mit:

```text
befreit_von_pflichtstunden = true
```

setzt das Pflichtstunden-Soll auf 0.

### Geleistete Stunden

Für die Pflichtstundenberechnung zählen ausschließlich:

```text
freigegeben = true
```

Dabei werden Stunden von Haupt- und Nebenmitglied dem Hauptmitgliedskontext gemeinsam zugerechnet.

---

## 7.3 Wichtiger Fehler im aktuellen Web

### Web aktuell

`OwnWorkHours` liest aktuell:

```text
saison.pflichtstunden_soll
```

direkt und berechnet anschließend selbst:

```text
remaining = requiredHours - approved - open
```

Damit werden **noch nicht freigegebene Stunden bereits vom Soll abgezogen**.

Außerdem fehlen in dieser Webberechnung:

- Eintritt zweites Halbjahr
- Altersbefreiung
- Wartungsvertragsbefreiung
- zentrale Haupt-/Nebenmitglied-Zuordnung
- zentraler Regelgrund

### Bewertung
❌ Das muss geändert werden.

### Verbindliche Zielregel

Das Web berechnet Pflichtstunden **nicht selbst**.

Es liest ausschließlich die zentrale Fachübersicht:

```ts
workHourService.getDutyHoursSummary(memberId, seasonId)
```

Repository:

```text
v_pflichtstunden_uebersicht
```

Die UI erhält zum Beispiel:

```ts
type DutyHoursSummary = {
  seasonId: number
  seasonYear: number
  requiredHours: number
  approvedHours: number
  remainingHours: number
  exempt: boolean
  reason: string
  maintenanceContractExempt: boolean
  ageExempt: boolean
  joinedSecondHalf: boolean
  euroPerMissingHour: number
  missingAmount: number
}
```

Die Berechnung dieser Werte erfolgt nicht erneut in TypeScript.

---

## 7.4 Eigene Arbeitsstunden erfassen

### MAUI
`ArbeitsstundenEditorPage.cs`

Pflichtfelder:

- Mitgliedskontext
- Saison
- Datum
- Stunden > 0
- Art der Arbeit

Bei Hauptmitglied mit Nebenmitglied kann ausgewählt werden, für wen die Arbeitsstunden erfasst werden.

### Web aktuell

`OwnWorkHours` unterstützt:

- Datum
- Stunden
- Art der Arbeit

Der Mitgliedskontext kommt derzeit als `memberId` in die Komponente.

### Bewertung
🟡 Grundfunktion vorhanden.

### Offen

Die Haupt-/Nebenmitglied-Auswahl soll genauso wie MAUI über den Fachservice vorbereitet werden und nicht durch eigene UI-Abfragen entstehen.

### Ziel
```text
features/work-hours/
  WorkHoursOverview.tsx
  WorkHourEditor.tsx
  WorkHourMemberSelector.tsx

services/work-hours/
  work-hour-service.ts

repositories/work-hours/
  work-hour-repository.ts
```

Service:

```ts
getEntryOptions(memberContext)
createWorkHour(...)
updateWorkHour(...)
```

---

## 7.5 Nutzer-Erfassung und Prüfstatus

### MAUI
Für einen normalen Nutzer gilt:

```text
Arbeitsstunde erfassen
↓
freigegeben = false
↓
offener Prüffall / „in Prüfung“
↓
Vorstand/Admin entscheidet
```

Die Stunde zählt vor der Freigabe **nicht** zu den geleisteten Pflichtstunden.

### Web aktuell

Neue Einträge werden mit:

```text
status = "offen"
freigegeben = false
```

gespeichert.

### Bewertung
✅ Grundprinzip richtig.

### Wichtig
Die UI-Bezeichnung darf „offen“ oder „in Prüfung“ darstellen, aber die fachliche Entscheidung über den Status darf nicht in mehreren React-Komponenten unterschiedlich umgesetzt werden.

### Ziel
```ts
workHourService.createWorkHour(...)
```

Der Service bestimmt abhängig vom Benutzerkontext den korrekten Anfangsstatus.

---

## 7.6 Erfassung durch Admin / Vorstand

### WPF / MAUI
Neue Arbeitsstunden, die durch Admin oder Vorstand erfasst werden, werden sofort freigegeben.

Gespeichert werden:

```text
freigegeben = true
genehmigt_von = aktuelles Mitglied des Bearbeiters
genehmigt_am = Zeitpunkt
```

Voraussetzung ist, dass das angemeldete Konto einem Mitglied zugeordnet ist, damit die Freigabe nachvollziehbar bleibt.

### Web aktuell
`OwnWorkHours` speichert einen neuen Datensatz derzeit unabhängig vom Bearbeiter grundsätzlich als:

```text
status = "offen"
freigegeben = false
```

### Bewertung
❌ fachliche Abweichung.

### Ziel

```ts
workHourService.createWorkHour({
  memberId,
  seasonId,
  date,
  hours,
  workType,
  actor
})
```

Der Service entscheidet:

```text
User
→ in Prüfung

Admin/Vorstand
→ sofort freigegeben + Auditdaten
```

Diese Entscheidung darf nicht in `WorkHourEditor.tsx` liegen.

---

## 7.7 Bearbeiten vorhandener Arbeitsstunden

### MAUI

Nicht freigegebene Einträge:

```text
bearbeitbar
```

Freigegebene Einträge im normalen Nutzerpfad:

```text
nur lesen
```

MAUI zeigt bei freigegebenen Stunden ausdrücklich eine Read-only-Ansicht.

### Web aktuell

Offene, nicht abgelehnte Einträge:

- Bearbeiten
- Löschen

Freigegebene Einträge:

- keine Bearbeitungsaktion

### Bewertung
✅ Bearbeitungssperre nach Freigabe ist grundsätzlich vorhanden.

### Offen
Die Löschregel für selbst erfasste offene Einträge muss mit der gewünschten Fachregel vereinheitlicht werden. MAUI bietet im normalen Editor aktuell keine entsprechende direkte Löschaktion wie das Web.

### Empfehlung
Die Regel zentral im Service definieren:

```ts
canEditWorkHour(...)
canDeleteOwnPendingWorkHour(...)
```

und nicht nur durch sichtbare/unsichtbare Buttons steuern.

---

## 7.8 Hauptmitglied und Nebenmitglied

### MAUI / Datenbank

Arbeitsstunden können auf Haupt- oder Nebenmitglied gebucht werden.

Für die Pflichtstundenberechnung werden sie jedoch über:

```text
coalesce(m.hauptmitglied_id, m.id)
```

dem Hauptmitgliedskontext zusammengeführt.

### Web aktuell
Der aktuelle `OwnWorkHours` lädt nur den übergebenen `memberId`.

Damit kann die sichtbare Einzelauflistung je nach Kontext von der zentralen Pflichtstundenberechnung abweichen.

### Ziel

Service-Funktionen beispielsweise:

```ts
getWorkHoursForHousehold(...)
getWorkHoursForMember(...)
getDutyHoursSummary(...)
```

Dadurch ist klar getrennt:

- welche Person eine Stunde geleistet hat
- welchem Hauptmitgliedskontext sie für die Pflichtstunden zugerechnet wird

---

## 7.9 Arbeitsstunden-Prüfübersicht

### WPF
`ArbeitsstundenPruefungView.xaml`

### MAUI
`ArbeitsstundenReviewPage.cs`

Die Übersicht lädt ausschließlich offene Prüffälle über den gemeinsamen Fachpfad:

```text
GetOffeneArbeitsstundenZurFreigabeAsync()
```

Die eigentliche Entscheidung erfolgt anschließend pro Datensatz.

### Web aktuell
`WorkHoursOverview` lädt derzeit selbst alle Datensätze aus:

```text
arbeitsstunde
```

und filtert anschließend im Browser:

```ts
!item.freigegeben && item.status !== "abgelehnt"
```

### Bewertung
🟡 Ergebnis ähnlich, Architektur aber nicht gleichwertig.

### Ziel

```text
features/work-hours/review/
  WorkHourReviewList.tsx
  WorkHourReviewDetail.tsx

services/work-hours/
  work-hour-review-service.ts

repositories/work-hours/
  work-hour-review-repository.ts
```

UI:

```ts
workHourReviewService.getOpenReviewCases()
```

Die UI soll nicht selbst definieren, was ein „offener Prüffall“ ist.

---

## 7.10 Prüfaktionen

### MAUI
`ArbeitsstundenReviewDetailPage.cs`

Der aktuelle Fachprozess besitzt **vier Aktionen**:

```text
Freigeben
Ablehnen
Korrigieren
Löschen
```

Für **alle vier Aktionen ist ein Prüfkommentar Pflicht**.

### Freigeben

```text
Arbeitsstunde
→ freigegeben
→ Prüfer + Zeitpunkt
→ Prüfverlauf
```

### Ablehnen

```text
Arbeitsstunde
→ abgelehnt
→ aus offener Prüfliste entfernen
→ Prüfverlauf
```

### Korrigieren

Erforderlich:

- Datum
- Stunden > 0
- Art der Arbeit
- Pflichtkommentar

Danach:

```text
korrigieren
→ direkt freigeben
→ Vorher/Nachher im Prüfverlauf
```

### Löschen

```text
Datensatz im Prüfprozess löschen
→ Begründung Pflicht
→ Prüfverlauf bleibt nachvollziehbar erhalten
```

### Web aktuell

Vorhanden:

- Freigeben
- Ablehnen
- Korrigieren

Nicht vorhanden:

- **Löschen als Prüfaktion**

### Bewertung
🟡 drei von vier Aktionen vorhanden.

❌ Löschen im Prüfprozess fehlt.

---

## 7.11 Prüfprozess im Web nicht direkt patchen

### Web aktuell
`WorkHoursOverview` führt derzeit selbst aus:

```text
PATCH arbeitsstunde
INSERT arbeitsstunde_pruefverlauf
```

Damit kennt die React-Komponente:

- Statuswerte
- Auditfelder
- Freigaberegeln
- Prüfverlauf
- Snapshot-Struktur

### Ziel

Die UI soll nur noch aufrufen:

```ts
approveWorkHour(...)
rejectWorkHour(...)
correctWorkHour(...)
deleteWorkHourInReview(...)
```

im:

```text
work-hour-review-service.ts
```

Der Service sorgt für:

- Pflichtkommentar
- Prüfer
- Prüfzeitpunkt
- Statusänderung
- Freigabestatus
- Vorher-/Nachher-Snapshot
- Prüfverlauf
- Löschregel

---

## 7.12 Prüfverlauf

### MAUI
Der Prüfverlauf zeigt:

- Aktion
- Zeitpunkt
- Prüfer
- Kommentar
- Vorher
- Nachher

### Web aktuell
✅ bereits vorhanden:

- Aktion
- Datum
- Prüfer-ID
- Begründung

### Offen
Web sollte ebenfalls aufbereitete Prüfernamen und Vorher-/Nachher-Daten aus dem Service erhalten.

### Ziel
```ts
workHourReviewService.getHistory(workHourId)
```

---

## 7.13 Bearbeitungssperren

### MAUI
Die Prüfdetailseite besitzt einen eigenen Review-Lock inklusive Heartbeat.

### Web aktuell
Web nutzt bereits:

```text
useEditLock(...)
```

für den gewählten Arbeitsstunden-Datensatz.

### Bewertung
✅ wichtige Browserfunktion vorhanden.

### Ziel
Technische Lock-Verwaltung darf in einem Hook / Infrastrukturmodul bleiben.

Der Fachservice darf davon ausgehen, dass vor einer mutierenden Prüfaktion eine gültige Sperre vorhanden ist.

---

## 7.14 Fehlstunden / Geldbetrag

Die zentrale Pflichtstundenfunktion liefert bereits:

```text
euro_pro_fehlstunde
fehlbetrag
```

Diese Werte sollen nicht nochmals im Web berechnet werden.

G12 bestätigt dafür die Zielregel: Der Jahresabschluss übernimmt den zentral berechneten Fehlbetrag aus G7 und berechnet Pflichtstunden oder Fehlstunden nicht erneut.

---

## G7 Zielstruktur

```text
KGV.Web/
  app/
    (workspace)/
      arbeitsstunden/
        page.tsx

  features/
    work-hours/
      WorkHoursOverview.tsx
      DutyHoursSummary.tsx
      WorkHourList.tsx
      WorkHourEditor.tsx
      WorkHourMemberSelector.tsx

      review/
        WorkHourReviewList.tsx
        WorkHourReviewDetail.tsx
        WorkHourReviewHistory.tsx

  services/
    work-hours/
      work-hour-service.ts
      duty-hours-service.ts
      work-hour-review-service.ts

  repositories/
    work-hours/
      work-hour-repository.ts
      duty-hours-repository.ts
      work-hour-review-repository.ts

  models/
    work-hours/
      work-hour.ts
      duty-hours-summary.ts
      work-hour-review.ts
```

## G7 verbindliche Architektur

```text
WorkHours UI
    ↓
work-hour-service
    ↓
work-hour-repository
    ↓
arbeitsstunde
```

Pflichtstunden:

```text
DutyHoursSummary UI
    ↓
duty-hours-service
    ↓
duty-hours-repository
    ↓
v_pflichtstunden_uebersicht
```

Prüfung:

```text
Review UI
    ↓
work-hour-review-service
    ↓
work-hour-review-repository
    ↓
arbeitsstunde
arbeitsstunde_pruefverlauf
```

## G7 wichtigste Korrekturen für Web

1. **Pflichtstunden nicht mehr aus `saison.pflichtstunden_soll` selbst berechnen.**
2. **`v_pflichtstunden_uebersicht` als einzige zentrale Fachquelle verwenden.**
3. Offene/nicht freigegebene Stunden nicht als geleistete Pflichtstunden abziehen.
4. Haupt- und Nebenmitglied korrekt gemeinsam berücksichtigen.
5. Admin/Vorstand-Erfassung sofort freigeben.
6. Prüfprozess auf vier Aktionen vervollständigen:
   - Freigeben
   - Ablehnen
   - Korrigieren
   - Löschen
7. Prüfaktionen aus React heraus in einen Fachservice verschieben.
8. Prüfverlauf und Auditdaten zentral erzeugen.
9. Freigegebene Einträge im normalen Nutzerpfad nur lesbar lassen.

---

# G8 – Arbeitseinsätze

## 8.1 Verwaltungsübersicht

### WPF
`ArbeitseinsaetzeVerwaltungEditorView.xaml`

WPF verwendet eine klassische Split-Ansicht:

```text
Arbeitseinsatz-Liste links
        +
Editor rechts
```

Vorhanden:

- Aktualisieren
- Neu
- vorhandenen Einsatz auswählen
- Bearbeiten
- Löschen
- Speichern
- `Speichern + nächste Schicht`

### MAUI
`ArbeitseinsaetzeManagementPage.cs`

MAUI trennt stärker:

```text
Verwaltungsübersicht
↓
eigener Arbeitseinsatz-Editor
```

Die Übersicht ist chronologisch sortiert und öffnet den gewählten Datensatz in einem eigenen Editorpfad.

### Web aktuell
`WorkAssignmentsManagement` liegt noch in `app/page.tsx`.

Web verwendet bereits eine für den PC passende Split-Ansicht:

- Tabelle links
- Editor rechts
- Vorheriger/Nächster Datensatz
- Neu
- Aktualisieren
- Speichern
- Speichern + nächste Schicht
- Absagen
- Löschen
- Teilnehmerbereich

### Bewertung
✅ Der Web-Aufbau ist für den Desktop sehr passend und kann grundsätzlich bleiben.

❌ Datenzugriff und Geschäftsregeln liegen noch direkt in der React-Komponente.

### Ziel
```text
features/work-assignments/
  WorkAssignmentManagement.tsx
  WorkAssignmentList.tsx
  WorkAssignmentEditor.tsx

services/work-assignments/
  work-assignment-service.ts

repositories/work-assignments/
  work-assignment-repository.ts
```

UI:

```ts
workAssignmentService.getAssignments(...)
workAssignmentService.create(...)
workAssignmentService.update(...)
workAssignmentService.cancel(...)
workAssignmentService.delete(...)
```

---

## 8.2 Arbeitseinsatz-Daten

Der fachliche Datensatz enthält:

- Titel
- Beschreibung
- Datum
- Startzeit
- Endzeit
- Treffpunkt
- maximale Teilnehmerzahl
- Stundenwert
- sichtbar ab
- sichtbar bis
- Anmeldung bis
- aktiv

### WPF / MAUI / Web
Diese Felder sind in allen drei Umsetzungen grundsätzlich vorhanden.

### Bewertung
✅ Web bildet den aktuellen Datensatz bereits weitgehend vollständig ab.

---

## 8.3 Validierung des Editors

### WPF
Fachliche Hinweise:

- Titel und Datum Pflicht
- Endzeit darf nicht vor Startzeit liegen
- sichtbar bis darf nicht vor sichtbar ab liegen
- Teilnehmerbegrenzung:
  - unbegrenzt → `NULL`
  - begrenzt → Wert > 0
- Stundenwert darf nicht negativ sein

### MAUI
`ArbeitseinsaetzeEditorPage.cs`

Validiert ebenfalls:

- Titel
- Start-/Endzeit
- Teilnehmerbegrenzung
- Stundenwert >= 0
- Sichtbarkeitszeitraum

### Web aktuell
Web prüft zusätzlich:

```text
Anmeldeschluss darf nicht nach dem Einsatztag liegen
```

und übernimmt die übrigen Kernprüfungen.

### Bewertung
✅ Web-Validierung ist bereits mindestens auf dem sichtbaren MAUI-Stand.

### Ziel
Die Validierung wird trotzdem aus der React-Komponente entfernt:

```text
services/work-assignments/
  work-assignment-validation.ts
```

oder als Bestandteil von:

```text
work-assignment-service.ts
```

UI bekommt nur strukturierte Validierungsfehler zurück.

---

## 8.4 Mehrere Schichten

### WPF
`Speichern + nächste Schicht`

Mehrere Schichten werden als **eigene Arbeitseinsätze** gespeichert.

Nach dem Speichern wird die nächste Schicht vorbefüllt.

### MAUI
Gleicher aktueller Fachansatz:

```text
aktuellen Einsatz speichern
↓
neuen Datensatz vorbereiten
↓
Titel/Beschreibung/Datum/Treffpunkt usw. übernehmen
↓
Startzeit = bisherige Endzeit
↓
Dauer der vorherigen Schicht übernehmen
```

### Web aktuell
`prepareNextShift(...)`

✅ bereits entsprechend umgesetzt.

### Ziel
Die Berechnung der Folgeschicht gehört nicht in die UI:

```ts
workAssignmentService.createNextShiftDraft(source)
```

Die React-Komponente rendert nur das Ergebnis.

---

## 8.5 Sichtbarkeit auf der Startseite

### Zentrale Fachquelle

Für Nutzer wird verwendet:

```text
v_startseite_arbeitseinsatz
```

Die View liefert nur aktive und aktuell sichtbare Einsätze.

Zusätzlich liefert sie:

```text
angemeldet_count
freie_plaetze
```

### Web aktuell
Die Startseite verwendet bereits:

```text
v_startseite_arbeitseinsatz
```

### Bewertung
✅ richtig.

Dieser Lesepfad soll erhalten bleiben, aber über ein Repository gekapselt werden.

```text
repositories/work-assignments/
  work-assignment-public-repository.ts
```

---

## 8.6 Anmeldung durch das Mitglied

### Datenbank-Fachlogik

Die Anmeldung läuft über:

```text
sign_up_for_arbeitseinsatz
```

Dabei wird `arbeitseinsatz_anmeldung` auf:

```text
status = angemeldet
```

gesetzt.

Existiert bereits eine Kombination aus Einsatz + Mitglied, wird deren Status wieder auf `angemeldet` gesetzt.

### Zentrale Validierung

Der Datenbanktrigger prüft:

- Arbeitseinsatz muss aktiv sein
- Anmeldeschluss darf nicht überschritten sein
- ohne Anmeldeschluss darf der Einsatz nicht bereits in der Vergangenheit liegen
- maximale Teilnehmerzahl darf nicht überschritten werden

### MAUI
`HomeSectionDetailPage.cs`

Verwendet:

```text
SignUpForArbeitseinsatzAsync(...)
```

### Web aktuell
Die Startseite ruft direkt:

```text
sign_up_for_arbeitseinsatz
```

auf.

### Bewertung
✅ fachlich richtig.

### Architekturproblem
Der RPC-Aufruf befindet sich direkt in der React-Komponente.

### Ziel
```text
services/work-assignments/
  work-assignment-registration-service.ts

repositories/work-assignments/
  work-assignment-registration-repository.ts
```

UI:

```ts
registrationService.register(workAssignmentId, memberId)
```

---

## 8.7 Abmeldung durch das Mitglied

### Datenbank-Fachlogik

```text
sign_off_from_arbeitseinsatz
```

setzt:

```text
status = abgesagt
```

für die Kombination Arbeitseinsatz + Mitglied.

### MAUI
✅ eigener Abmeldebutton.

### Web
✅ eigener Abmeldebutton in der Detailansicht.

### Ziel

```ts
registrationService.signOff(workAssignmentId, memberId)
```

Keine direkten RPC-Namen mehr in der UI.

---

## 8.8 Anmeldestatus

Die Datenbank kennt für `arbeitseinsatz_anmeldung`:

```text
angemeldet
abgesagt
teilgenommen
nicht_erschienen
```

### Aktuell gesichert umgesetzt

MAUI nutzt sichtbar:

- angemeldet
- abgemeldet/abgesagt

Web nutzt zusätzlich im Verwaltungsbereich:

- teilgenommen

### Hinweis
Im geprüften MAUI-Code wurde kein produktiver Bedienflow gefunden, der Teilnehmer auf `teilgenommen` oder `nicht_erschienen` setzt.

Diese Statuswerte existieren in der Datenbank, sind aber nicht automatisch gleichbedeutend mit einem vollständig umgesetzten MAUI-Workflow.

---

## 8.9 Teilnehmerliste für Vorstand / Admin

### WPF
Die Arbeitseinsatz-Detailansicht kann:

- Teilnehmer anzeigen
- Mitglied suchen
- Teilnehmer hinzufügen
- Teilnehmer abmelden

### MAUI
`HomeSectionDetailPage.cs`

Vorstand/Admin sehen die Teilnehmerliste.

Im geprüften MAUI-Detailflow wurden jedoch keine Buttons zum manuellen Hinzufügen oder Abmelden anderer Mitglieder gefunden.

### Web aktuell
Die Startseiten-Detailansicht zeigt Vorstand/Admin bereits die angemeldeten Teilnehmer.

Zusätzlich besitzt die Web-Verwaltung:

```text
WorkAssignmentParticipants
```

mit:

- Teilnehmerliste
- Mitglied auswählen
- Mitglied anmelden
- Teilnehmer abmelden
- Teilnahme übernehmen

### Bewertung
✅ Web ist in diesem Punkt funktional bereits weiter als der geprüfte MAUI-Pfad.

### Empfehlung
Diese Webfunktion nicht entfernen.

Sie soll jedoch über den gleichen zentralen Registrierungsservice laufen.

---

## 8.10 Teilnehmer manuell anmelden

### Web aktuell

`WorkAssignmentParticipants` schreibt derzeit direkt in:

```text
arbeitseinsatz_anmeldung
```

mittels `POST` bzw. `PATCH`.

Dabei prüft die UI selbst:

- Einsatz aktiv
- Anmeldeschluss
- Teilnehmerlimit

### Problem
Diese Regeln existieren bereits zentral in der Datenbank.

Die UI sollte sie nicht noch einmal als führende Fachlogik besitzen.

### Ziel

```ts
registrationService.registerMember(workAssignmentId, memberId)
```

intern über den gleichen fachlichen Anmeldepfad/RPC.

Die UI darf Vorabinformationen für Bedienkomfort anzeigen, aber die Datenbank bleibt die verbindliche Validierungsinstanz.

---

## 8.11 Teilnehmer abmelden

### Web aktuell
Im Verwaltungsbereich wird der Status direkt per `PATCH` auf:

```text
abgesagt
```

gesetzt.

### Ziel
Auch die Verwaltungsabmeldung soll denselben Fachpfad verwenden:

```ts
registrationService.signOffMember(workAssignmentId, memberId)
```

Damit unterscheiden sich Eigenabmeldung und Admin-Abmeldung nur im aufrufenden Kontext, nicht in der fachlichen Statuslogik.

---

## 8.12 Teilnahme übernehmen

### Web aktuell

Web besitzt bereits eine Funktion:

```text
Teilnahme übernehmen
```

Dabei geschieht aktuell direkt in der React-Komponente:

```text
1. arbeitseinsatz_anmeldung.status = teilgenommen
2. neue arbeitsstunde anlegen
```

Die Arbeitsstunde bekommt:

```text
mitglied_id = Teilnehmer
saison_id = aktive Saison
datum = Einsatzdatum
stunden = Stundenwert des Einsatzes
art_der_arbeit = Titel des Einsatzes
status = offen
freigegeben = false
```

### Bewertung
🟡 nützliche Webfunktion, aber fachlich derzeit problematisch gekoppelt.

### Problem 1 – zwei getrennte Schreibvorgänge

Aktuell können diese beiden Schritte auseinanderlaufen:

```text
Status bereits teilgenommen
aber
Arbeitsstunde konnte nicht angelegt werden
```

oder umgekehrt bei späteren Änderungen.

### Problem 2 – G7-Regel

Der Vorgang erfolgt im Verwaltungsbereich durch Admin/Vorstand.

Nach der in G7 festgelegten Fachregel werden von Admin/Vorstand erfasste Arbeitsstunden **sofort freigegeben**.

Der aktuelle Webcode erzeugt die Arbeitsstunde dagegen mit:

```text
freigegeben = false
status = offen
```

### Bewertung
❌ hier besteht eine konkrete fachliche Inkonsistenz zwischen G7 und G8.

---

## 8.13 Zielworkflow „Teilnahme bestätigen“

Dieser Vorgang soll einen eigenen Fachservice bekommen:

```text
work-assignment-attendance-service.ts
```

UI:

```ts
attendanceService.confirmParticipation({
  workAssignmentId,
  memberId,
  seasonId
})
```

Fachlich:

```text
Teilnahme bestätigen
↓
Status = teilgenommen
↓
Arbeitsstunde aus Stundenwert erzeugen
↓
Admin/Vorstand-Erfassung
↓
Arbeitsstunde sofort freigeben
↓
Auditdaten setzen
```

### Empfehlung

Für die eigentliche Umsetzung sollte dieser Vorgang möglichst **atomar** erfolgen, zum Beispiel über eine passende Datenbank-RPC/Funktion.

Damit entstehen nicht zwei voneinander unabhängige Schreibvorgänge aus der React-App.

---

## 8.14 Doppelte Arbeitsstunden verhindern

Der aktuelle Webcode verhindert im sichtbaren `WorkAssignmentParticipants`-Flow nicht ausdrücklich auf Datenbankebene, dass für denselben Teilnehmer und denselben Arbeitseinsatz mehrfach eine Arbeitsstunde erzeugt wird.

Der Button ist nach `status = teilgenommen` zwar gesperrt, dies ist aber nur UI-Schutz.

### Empfehlung

Der zukünftige Attendance-Service bzw. die RPC sollte idempotent sein:

```text
gleicher Arbeitseinsatz
+ gleiches Mitglied
→ höchstens eine automatische Arbeitsstundenübernahme
```

Dafür muss bei der Umsetzung ein verlässlicher fachlicher Bezug zwischen Teilnahme und erzeugter Arbeitsstunde hergestellt werden.

Im aktuell geprüften Schema wurde kein `arbeitseinsatz_id`-Feld in `arbeitsstunde` als bestehende Verknüpfung gefunden.

Das ist daher ein **Umsetzungsvorschlag**, keine bereits vorhandene Funktion.

---

## 8.15 Nicht erschienen

Die Datenbank kennt:

```text
nicht_erschienen
```

Im geprüften WPF-/MAUI-/Web-Bedienpfad wurde jedoch kein vollständiger Verwaltungsworkflow gefunden, der diesen Status setzt.

### Empfehlung
Als späteren Teilnehmerstatus im Attendance-Service vorsehen:

```ts
attendanceService.markNoShow(...)
```

aber nicht als bereits vorhandene Fachfunktion dokumentieren.

---

## 8.16 Einsatz absagen

### Web aktuell
Web bietet:

```text
Absagen
```

und setzt:

```text
arbeitseinsatz.aktiv = false
```

### Datenbankwirkung
Neue Anmeldungen auf einen inaktiven Einsatz werden vom Trigger abgewiesen.

### Bewertung
✅ sinnvoll.

### Ziel

```ts
workAssignmentService.cancel(id)
```

---

## 8.17 Einsatz löschen

### WPF
✅ Löschen vorhanden.

### MAUI
Das Löschen erfolgt aus der Startseiten-/Detailverwaltung über:

```text
DeleteArbeitseinsatzAsync(...)
```

### Web
✅ endgültiges Löschen vorhanden.

### Ziel
Direktes `deleteSupabase` aus der React-Komponente entfernen:

```ts
workAssignmentService.delete(id)
```

Der Service entscheidet außerdem, ob fachlich eher Absagen oder endgültiges Löschen vorgesehen ist.

---

## 8.18 Bearbeitungssperre

### Web
`WorkAssignmentsManagement` nutzt bereits:

```text
useEditLock(...)
```

für vorhandene Einsätze.

### Bewertung
✅ beibehalten.

Der Lock bleibt technische Browser-Infrastruktur; die fachlichen Save-/Delete-/Cancel-Aktionen laufen über Services.

---

## G8 Zielstruktur

```text
KGV.Web/
  app/
    (workspace)/
      arbeitseinsaetze/
        page.tsx

  features/
    work-assignments/
      WorkAssignmentManagement.tsx
      WorkAssignmentList.tsx
      WorkAssignmentEditor.tsx
      WorkAssignmentDetail.tsx

      participants/
        WorkAssignmentParticipants.tsx
        WorkAssignmentParticipantList.tsx
        WorkAssignmentParticipantSelector.tsx

  services/
    work-assignments/
      work-assignment-service.ts
      work-assignment-registration-service.ts
      work-assignment-attendance-service.ts
      work-assignment-validation.ts

  repositories/
    work-assignments/
      work-assignment-repository.ts
      work-assignment-public-repository.ts
      work-assignment-registration-repository.ts

  models/
    work-assignments/
      work-assignment.ts
      work-assignment-registration.ts
      work-assignment-participant.ts
```

## G8 verbindliche Architektur

Verwaltung:

```text
Editor / Liste
↓
work-assignment-service
↓
work-assignment-repository
↓
arbeitseinsatz
```

An-/Abmeldung:

```text
Startseite / Teilnehmerverwaltung
↓
work-assignment-registration-service
↓
registration-repository
↓
sign_up_for_arbeitseinsatz
sign_off_from_arbeitseinsatz
↓
arbeitseinsatz_anmeldung
```

Teilnahme:

```text
Teilnehmerverwaltung
↓
work-assignment-attendance-service
↓
atomarer Fachvorgang
↓
arbeitseinsatz_anmeldung
+ Arbeitsstunde
```

## G8 wichtigste Korrekturen für Web

1. `WorkAssignmentsManagement` aus `page.tsx` herauslösen.
2. CRUD für `arbeitseinsatz` über Service/Repository führen.
3. An-/Abmeldung nicht direkt aus React per RPC/Table-Write ausführen.
4. Teilnehmerverwaltung auf denselben Registrierungsservice stellen.
5. `Speichern + nächste Schicht` beibehalten, aber Berechnung in den Service verschieben.
6. `v_startseite_arbeitseinsatz` als öffentliche Fachquelle beibehalten.
7. Die vorhandene Webfunktion **Teilnahme übernehmen** fachlich überarbeiten.
8. Teilnahme + Arbeitsstundenerzeugung möglichst atomar ausführen.
9. Von Admin/Vorstand übernommene Teilnahme muss die G7-Regel zur unmittelbaren Freigabe beachten.
10. Doppelte automatische Arbeitsstundenübernahmen zuverlässig verhindern.
11. `nicht_erschienen` ist im Schema vorhanden, aber noch kein nachgewiesener vollständiger Bedienflow.

---

# G9 – Termine & Bekanntmachungen

## 9.1 Termine – Verwaltung und Editor

### WPF
`TermineVerwaltungEditorView.xaml`

WPF verwendet die Desktop-Splitansicht mit Terminliste links und Editor rechts.

### MAUI
`TermineManagementPage.cs` und `TermineEditorPage.cs`

MAUI trennt Übersicht und Editor. Fachfelder:

- Titel
- Beschreibung
- Datum
- Startzeit
- Endzeit
- sichtbar ab
- sichtbar bis
- aktiv

Validierung:

- Titel ist Pflicht
- Endzeit darf nicht vor Startzeit liegen
- sichtbar bis darf nicht vor sichtbar ab liegen

### Web aktuell
`AppointmentManagement` in `app/page.tsx`.

Vorhanden:

- chronologische Liste
- Bearbeiten
- Neu
- Deaktivieren
- Löschen
- Vorher/Nächster Datensatz
- identische Kernvalidierungen
- Bearbeitungssperre

### Bewertung
✅ Funktional sehr nah an MAUI.

### Ziel
```text
features/appointments/
  AppointmentManagement.tsx
  AppointmentList.tsx
  AppointmentEditor.tsx

services/appointments/
  appointment-service.ts

repositories/appointments/
  appointment-repository.ts
```

CRUD und Validierung werden aus React herausgezogen.

---

## 9.2 Termine auf der Startseite

### Zentrale Fachquelle
```text
v_startseite_termine
```

Die View berücksichtigt:

- `aktiv = true`
- `sichtbar_ab`
- `sichtbar_bis`

### Web aktuell
✅ verwendet bereits `v_startseite_termine`.

Zusätzlich filtert Web auf Termine ab dem aktuellen Datum.

### Ziel
`appointment-repository.ts` bleibt die einzige Datenzugriffsstelle für diesen Termin-Lesepfad. `home-service.ts` aus G2 konsumiert ihn für das Dashboard; es gibt kein zusätzliches `home-repository` für dieselbe View.

---

## 9.3 Bekanntmachungen – Verwaltung

### WPF
`BekanntmachungenVerwaltungEditorView.xaml`

### MAUI
`BekanntmachungenManagementPage.cs` und `BekanntmachungEditorPage.cs`

Fachfelder:

- Titel
- HTML-Inhalt
- sichtbar ab
- sichtbar bis
- Sortierreihenfolge
- aktiv

### Web aktuell
`AnnouncementManagement` in `app/page.tsx`.

Vorhanden:

- Liste nach `sort_order`
- Aktivstatus
- Neu/Bearbeiten
- Deaktivieren
- Löschen
- Vorher/Nächster Datensatz
- HTML-Editor
- HTML-Vorschau
- Bearbeitungssperre

### Bewertung
✅ sehr gut umgesetzt.

---

## 9.4 HTML-Editor und Vorschau

### MAUI
HTML wird als HTML bearbeitet.

Hilfsbuttons:

- Absatz
- Überschrift
- Fett
- Link
- Liste

Eigene Vorschau über WebView.

### Web aktuell
✅ gleiche Snippet-Idee bereits vorhanden.

Die Vorschau wird in einem `sandbox`-Iframe mit restriktiver Content-Security-Policy gerendert.

### Bewertung
✅ gute browserspezifische Umsetzung.

### Ziel
```text
features/announcements/
  AnnouncementEditor.tsx
  AnnouncementHtmlEditor.tsx
  AnnouncementPreview.tsx
```

---

## 9.5 Bekanntmachungs-Validierung

### MAUI / Web
Übereinstimmend:

- Titel Pflicht
- HTML-Inhalt Pflicht
- sichtbar bis darf nicht vor sichtbar ab liegen
- Sortierreihenfolge muss ganzzahlig sein

### Ziel
```text
services/announcements/
  announcement-service.ts
  announcement-validation.ts

repositories/announcements/
  announcement-repository.ts
```

---

## 9.6 Bekanntmachungen auf der Startseite

### Zentrale Fachquelle
```text
v_startseite_bekanntmachungen
```

Berücksichtigt:

- aktiv
- sichtbar ab
- sichtbar bis
- Sortierung

### Web aktuell
✅ verwendet die View.

### Zielzuständigkeit
`announcement-repository.ts` bleibt die einzige Datenzugriffsstelle für diesen Bekanntmachungs-Lesepfad. `home-service.ts` aus G2 konsumiert ihn für das Dashboard.

### Wichtige Abweichung
Web wandelt den HTML-Inhalt auf der Startseite und in der Detailansicht aktuell über:

```ts
plainText(inhalt_html)
```

in reinen Text um.

Dadurch gehen verloren:

- Absätze
- Überschriften
- Fettdruck
- Listen
- Links

### MAUI / WPF
Bekanntmachungsdetails rendern den gespeicherten HTML-Inhalt.

### Bewertung
❌ Web speichert HTML korrekt, zeigt es im normalen Nutzerpfad aber nicht vollständig an.

### Ziel
Startseitenkarte darf weiterhin einen gekürzten Textauszug zeigen.

Die **Detailansicht** soll jedoch den HTML-Inhalt sicher rendern.

```text
features/announcements/
  AnnouncementContent.tsx
```

Dafür ist eine zentrale Sanitizing-/Rendering-Lösung vorzusehen; ungeprüftes `dangerouslySetInnerHTML` soll nicht direkt in mehreren Komponenten verteilt werden.

---

## 9.7 Deaktivieren und Löschen

Web bietet bei Terminen und Bekanntmachungen zusätzlich:

```text
Deaktivieren
```

durch `aktiv = false`.

WPF/MAUI unterstützen endgültiges Löschen.

### Bewertung
✅ Deaktivieren ist eine sinnvolle Web-Erweiterung und soll erhalten bleiben.

Beide Aktionen laufen künftig über den jeweiligen Service.

---

## G9 Zielstruktur

```text
KGV.Web/
  features/
    appointments/
      AppointmentManagement.tsx
      AppointmentList.tsx
      AppointmentEditor.tsx

    announcements/
      AnnouncementManagement.tsx
      AnnouncementList.tsx
      AnnouncementEditor.tsx
      AnnouncementHtmlEditor.tsx
      AnnouncementPreview.tsx
      AnnouncementContent.tsx

  services/
    appointments/
      appointment-service.ts
      appointment-validation.ts

    announcements/
      announcement-service.ts
      announcement-validation.ts

  repositories/
    appointments/
      appointment-repository.ts

    announcements/
      announcement-repository.ts
```

## G9 wichtigste Korrekturen für Web

1. Termin- und Bekanntmachungsverwaltung aus `page.tsx` herauslösen.
2. CRUD und Validierung über Services/Repositories führen.
3. `v_startseite_termine` und `v_startseite_bekanntmachungen` als zentrale öffentliche Quellen beibehalten.
4. HTML-Editor und sichere Vorschau beibehalten.
5. Bekanntmachungsdetails nicht mehr nur als `plainText(...)` anzeigen.
6. Deaktivieren als sinnvolle Web-Erweiterung erhalten.

---

# G10 – Dokumente

## 10.1 Gemeinsamer Dokumentbereich

### WPF
Relevante Bereiche:

- `DokumenteView.xaml`
- `DokumenteParzellenView.xaml`
- `GartenDokumenteView.xaml`
- `GartenDokumenteListeView.xaml`

WPF unterscheidet Mitglieds- und Parzellenkontext, nutzt aber denselben fachlichen Dokumentpfad.

### MAUI
`DokumentePage.xaml.cs`

MAUI hat den Bereich bereits vereinheitlicht und arbeitet mit zwei Owner-Kontexten:

```text
Mitglied
Parzelle
```

### Web aktuell
`DocumentList` in `app/page.tsx`

unterstützt ebenfalls:

```text
memberId
oder
parcelId
```

und wird für Parzellen kompakt wiederverwendet.

### Bewertung
✅ Das gemeinsame Web-Konzept ist richtig.

### Ziel
Nicht zwei getrennte Dokumentimplementierungen bauen, sondern eine gemeinsame fachliche Infrastruktur:

```text
features/documents/
  DocumentList.tsx
  DocumentUpload.tsx
  DocumentActions.tsx

services/documents/
  document-service.ts

repositories/documents/
  document-repository.ts
```

---

## 10.2 Mitgliedsdokumente

### MAUI
Mitgliedskontext wird über den ausgewählten Mitgliedsdatensatz bestimmt.

Je nach Recht:

- Dokumente lesen
- Dokumente hochladen
- Vertragsfolgeaktionen
- archivieren

### Web aktuell
✅ Mitgliedsdokumente werden anhand `mitglied_id` geladen.

### Offen
Die React-Komponente fragt die Tabelle `dokument` direkt ab.

### Ziel
```ts
documentService.getMemberDocuments(memberId)
```

---

## 10.3 Parzellendokumente

### MAUI
Der gleiche Dokumentbereich wird mit:

```text
scope = parzelle
parzelleId
```

verwendet.

### Web aktuell
✅ `DocumentList` unterstützt `parcelId`.

### Ziel
```ts
documentService.getParcelDocuments(parcelId)
```

G5 stellt lediglich den Parzellenkontext bereit; die Dokumentfachlogik bleibt in G10.

---

## 10.4 Dokumentrechte

### MAUI
Dokumentrechte werden klar getrennt:

```text
Lesen
Verwalten
Archivieren
```

`CanManageDocuments` basiert auf dem Dokument-Schreibrecht.

**Archivieren ist ausschließlich für Admin erlaubt.**

```text
CanArchiveDocuments
→ role == admin
```

### Web aktuell
Web unterscheidet:

```text
readDocuments
manageDocuments
```

`DocumentList` zeigt den Archivieren-Button aktuell aber immer bei:

```text
canManage = true
```

Damit sieht auch Vorstand mit Dokument-Schreibrecht die Archivierungsaktion.

Der Server lehnt sie zwar ab, weil die Edge Function beim Archivieren ausdrücklich Admin verlangt, die UI ist aber fachlich falsch.

### Bewertung
❌ konkrete Abweichung.

### Ziel
Der Service liefert getrennte Fähigkeiten:

```ts
{
  canRead,
  canUpload,
  canArchive,
  canUseContractFollowActions
}
```

`canArchive` darf nur für Admin true sein.

---

## 10.5 Normaler Dokument-Upload

### WPF / MAUI
Upload benötigt:

- gültigen Mitglieds- oder Parzellenkontext
- Titel
- Datei
- Dokument-Schreibrecht

### Web aktuell
✅ gleiche Grundfunktion vorhanden.

Web ruft zuerst:

```text
kgv-upload-document
```

auf und legt anschließend separat den Datensatz in:

```text
dokument
```

an.

### Technische Feststellung
Die Edge Function `kgv-upload-document` lädt im normalen Multipart-Upload die Datei nach Google Drive und liefert anschließend:

- `drive_file_id`
- `storage_path`
- Dateiname
- MIME-Type
- Größe

Sie legt in diesem Uploadpfad **nicht selbst** den `dokument`-Datensatz an.

### Problem
Im Web bestehen damit zwei getrennte Schritte:

```text
Datei erfolgreich in Drive
↓
INSERT dokument
```

Scheitert der zweite Schritt, kann eine verwaiste Drive-Datei entstehen.

### Ziel
Diese Koordination gehört vollständig in:

```text
document-service.ts
```

nicht in `DocumentList.tsx`.

Der Service muss bei einem Fehler nach erfolgreichem Drive-Upload entweder:

- sauber aufräumen, oder
- einen serverseitig atomar/kompensierend ausgeführten Dokument-Upload verwenden.

Die React-Komponente darf diese Zweischrittigkeit nicht kennen.

---

## 10.6 Ablage

Aktuell wird Google Drive als eigentliche Dateiablage genutzt.

Der Dokumentdatensatz enthält unter anderem:

```text
drive_file_id
storage_path
titel
dateiname
mime_type
size_bytes
mitglied_id / parzelle_id
```

### Ziel
Details wie Google Drive, Edge Function oder Storage-Pfad bleiben vollständig hinter Repository/Service verborgen.

UI arbeitet nur mit einem Dokumentmodell.

---

## 10.7 Dokument öffnen

### MAUI
Für PDF und Bilddateien:

```text
DownloadDokumentContentAsync
↓
PdfViewerPage
oder
ImageViewerPage
```

Andere Dateitypen werden über einen sicheren URL-Pfad geöffnet.

### Web aktuell
Web entscheidet:

```text
drive_file_id
→ kgv-upload-document action=download

sonst bucket + storage_path
→ Signed URL
```

und öffnet anschließend einen neuen Browser-Tab.

### Bewertung
✅ für den Browser grundsätzlich sinnvoll.

Ein eigener PDF-/Bildviewer ist nicht zwingend erforderlich, weil der Browser diese Dateitypen nativ gut darstellen kann.

### Ziel
Die Entscheidung, **wie** ein Dokument geöffnet wird, gehört dennoch in:

```ts
documentService.openDocument(...)
```

oder einen technischen `document-open-service`.

Die UI soll nicht zwischen Drive-ID und Storage-Pfad unterscheiden.

---

## 10.8 Sichere Dokumentfreigabe

Die Edge Function prüft beim Download serverseitig, ob der aktuelle Benutzer das konkrete Dokument lesen darf.

### Bewertung
✅ wichtiger Sicherheitsmechanismus.

Diese serverseitige Prüfung bleibt führend; ausgeblendete Buttons allein sind keine Berechtigung.

---

## 10.9 Archivieren statt physisch löschen

### MAUI
Der aktuelle Produktivpfad heißt ausdrücklich:

```text
Archivieren
```

Dabei wird die Originaldatei **nicht gelöscht**.

Gespeichert werden:

```text
archiviert_at
archiviert_by
archiviert_begruendung
```

Voraussetzungen:

- Admin
- Begründung mindestens 3 Zeichen
- zusätzliches Archivpasswort

### Web aktuell
✅ nutzt bereits `archiveDocument(...)`.

Vorhanden:

- Begründung
- Archivpasswort
- serverseitige Adminprüfung
- `archiviert_at`
- aktive Liste blendet archivierte Dokumente aus

### Bewertung
🟡 fachlich richtig, aber UI-Rechte müssen korrigiert werden.

### Ziel
```ts
documentService.archiveDocument(id, {
  reason,
  archivePassword
})
```

---

## 10.10 Archivierte Dokumente

### Web aktuell
`DocumentList` lädt auch `archiviert_at`, filtert dann aber:

```ts
!item.archiviert_at
```

und zeigt nur aktive Dokumente.

### Bewertung
✅ für den normalen Dokumentpfad richtig.

### Offen
Falls später eine Archivansicht benötigt wird, muss sie als eigener Adminpfad umgesetzt werden. Sie soll nicht versehentlich wieder in der normalen Mitgliedsliste erscheinen.

---

## 10.11 Dokumenttyp und Vertragsstatus

### WPF / MAUI
`DocumentInfo` erkennt aus Dateiname/Pfad/Titel die Formular-Metadaten:

```text
Dokumenttyp
Status
```

Beispiele:

```text
Mitgliedsvertrag
Pachtvertrag

Unsigniert
Signiert
```

Die Listen zeigen daher:

- Dokumenttyp
- Vertragsstatus
- Folgeaktionshinweis

### Web aktuell
`DocumentRecord` enthält diese aufbereiteten Informationen nicht.

Die Webliste zeigt nur:

- Titel
- Datei
- geändert
- Größe

### Bewertung
❌ wichtiger fachlicher Unterschied.

### Ziel
Der `document-service` soll ein Web-Domainmodell liefern:

```ts
type DocumentItem = {
  id: number
  title: string
  fileName: string
  mimeType: string
  sizeBytes: number | null
  updatedAt: string

  documentType?: "mitgliedsvertrag" | "pachtvertrag"
  contractStatus?: "unsigniert" | "signiert"
  contractActionHint?: string

  canOpen: boolean
  canArchive: boolean
  canUploadSignedVersion: boolean
  canDigitallySign: boolean
}
```

Die Erkennung soll zentral und nicht in mehreren React-Komponenten erfolgen.

---

## 10.12 Signierte Vertragsfassung hochladen

### WPF
Bei einer vorhandenen unsignierten Vertragsfassung erscheint:

```text
Signierten Scan hochladen
```

### MAUI
Bei unterstützten unsignierten Pachtverträgen:

```text
Signierten Scan ablegen
```

Die signierte Fassung wird als **eigenes Enddokument** gespeichert.

Die unsignierte Fassung bleibt erhalten.

### Web aktuell
❌ Diese dokumentbezogene Folgeaktion ist in `DocumentList` nicht gleichwertig vorhanden.

Der aktuelle `ContractComposer` erzeugt neue Verträge, ersetzt aber nicht die Dokumentlistenlogik für bereits vorhandene unsignierte Dokumente.

### Ziel
```text
features/documents/contracts/
  SignedContractUploadAction.tsx

services/documents/
  contract-document-service.ts
```

Fachregel:

```text
unsignierte Fassung
+
signierter Scan
↓
neues signiertes Dokument
+
unsignierte Fassung bleibt bestehen
```

---

## 10.13 Digitale Signatur aus der Dokumentliste

### MAUI
Für den unterstützten Pachtvertrags-Folgepfad gibt es zusätzlich:

```text
Digital signieren
```

über `VertragsSignaturPage`.

Wenn bereits ein signiertes Enddokument existiert, sollen diese Folgeaktionen nicht mehr angeboten werden.

### Web aktuell
Web besitzt bereits `SignaturePad` und den Vertragsgenerator aus G4, aber keinen gleichwertigen **Folgeaktionspfad direkt am unsignierten Dokument**.

### Ziel
G4 besitzt Signatur und Vertragsfachlogik.

G10 stellt lediglich die Dokumentaktion bereit und delegiert:

```text
DocumentList
↓
contract-document-service
↓
lease-contract-service / signature-service aus G4
```

Keine zweite Vertragslogik in G10.

---

## 10.14 Pachtvertrag – „Only Show“

### MAUI
Wenn für die aktuelle Parzelle bereits ein signierter Pachtvertrag existiert:

```text
nur anzeigen
```

und keine erneuten Folgeaktionen zum Signieren/Upload anbieten.

### Web aktuell
❌ in der Dokumentliste nicht gleichwertig umgesetzt.

### Ziel
Der Service liefert den Aktionsstatus fertig an die UI:

```ts
followUpMode:
  | "none"
  | "upload-signed"
  | "digital-sign"
  | "show-signed-only"
```

---

## 10.15 Vertragserstellung ist nicht Dokumentverwaltung

Der aktuelle Web-`DocumentList` enthält:

```text
ContractComposer
```

direkt innerhalb der Dokumentkomponente.

### Bewertung
❌ architektonisch nicht sauber.

Nach G4 gilt:

```text
Vertrag erstellen / signieren
→ G4 contracts

Dokument auflisten / hochladen / öffnen / archivieren
→ G10 documents
```

Beide Bereiche nutzen dieselbe Dokument-Infrastruktur, aber die UI- und Fachmodule bleiben getrennt.

---

## 10.16 Dateitypen

### WPF
Erlaubt u. a.:

- PDF
- DOC/DOCX
- XLS/XLSX
- TXT
- RTF
- JPG/JPEG
- PNG
- WEBP

### MAUI
Dateiauswahl erfolgt über den mobilen FilePicker; MIME-Type wird ermittelt bzw. übernommen.

### Web aktuell
`accept` ist aktuell enger:

```text
application/pdf,image/*,.doc,.docx,.odt
```

Damit fehlen im Browserdialog unter anderem explizit:

- XLS/XLSX
- TXT
- RTF

### Empfehlung
Die erlaubten Dokumenttypen zwischen Plattformen vereinheitlichen und zentral konfigurieren.

Die tatsächliche serverseitige Validierung bleibt maßgeblich; `accept` im Browser ist nur eine Auswahlhilfe.

---

## 10.17 Parzellendokumente aus dem Parzellenbereich

### Web
Parzellendokumente werden bereits kompakt in der Parzellenansicht eingebettet.

### Bewertung
✅ beibehalten.

Die Komponente soll jedoch künftig lediglich:

```ts
documentService.getParcelDocuments(parcelId)
```

verwenden.

Keine eigene Dokumentlogik in `parcel-workspaces.tsx`.

---

## G10 Zielstruktur

```text
KGV.Web/
  features/
    documents/
      DocumentList.tsx
      DocumentTable.tsx
      DocumentUpload.tsx
      DocumentActions.tsx
      DocumentOpenAction.tsx
      DocumentArchiveDialog.tsx

      contracts/
        ContractDocumentStatus.tsx
        SignedContractUploadAction.tsx
        ContractFollowUpActions.tsx

  services/
    documents/
      document-service.ts
      document-access-service.ts
      document-upload-service.ts
      contract-document-service.ts

  repositories/
    documents/
      document-repository.ts
      document-file-repository.ts

  models/
    documents/
      document.ts
      document-owner.ts
      contract-document-status.ts
```

## G10 verbindliche Architektur

```text
Document UI
↓
document-service
↓
document-repository
+
document-file-repository
↓
dokument
+
kgv-upload-document / Google Drive
```

Vertragsfolgeaktionen:

```text
Document UI
↓
contract-document-service
↓
G4 Vertrags-/Signaturservices
↓
gemeinsame Dokumentablage
```

## G10 wichtigste Korrekturen für Web

1. `DocumentList` aus `page.tsx` herauslösen.
2. Dokumentzugriff für Mitglied und Parzelle in einem gemeinsamen Service behalten.
3. Drive-/Storage-Unterscheidung vollständig aus React entfernen.
4. Upload als koordinierten Fachvorgang kapseln; verwaiste Drive-Dateien bei DB-Fehler vermeiden.
5. Archivieren in der UI **nur für Admin** anbieten.
6. Vertragsdokumenttyp und Status auch im Web anzeigen.
7. Signierten Scan als Folgeaktion für vorhandene unsignierte Pachtverträge ergänzen.
8. Unsigned bleibt beim Ablegen der signierten Fassung erhalten.
9. Bei bereits signiertem Pachtvertrag nur noch Anzeige, keine erneute Folgeaktion.
10. `ContractComposer` aus der Dokumentliste entfernen und G4 zuordnen.
11. Dateitypen plattformübergreifend vereinheitlichen.
12. Parzellendokumente bleiben Wiederverwendung desselben Dokumentmoduls.

---

# G11 – Wartungsverträge

## 11.1 Globale Wartungsvertragsübersicht

### WPF
`WartungsvertraegeVerwaltungView.xaml`

Die Verwaltungsübersicht zeigt:

- Titel
- Kurzbeschreibung
- maximales Kontingent
- belegt
- frei
- Aktivstatus

Aktionen:

- Aktualisieren
- Neu
- Öffnen
- Bearbeiten

### MAUI
`WartungsvertraegePage.cs`

MAUI verwendet dieselbe fachliche Übersicht über:

```text
GetWartungsvertraegeOverviewAsync()
```

und öffnet anschließend eine eigene Detailansicht.

Admin/Vorstand dürfen neue Verträge anlegen und verwalten.

### Web aktuell
`MaintenanceContracts` liegt noch in `app/page.tsx`.

Vorhanden:

- globale Liste
- Titel
- Bereich
- Beschreibung
- Belegung
- Aktivstatus
- Detailbereich
- Bearbeiten
- neue Verträge
- Mitgliederzuordnung

### Bewertung
✅ funktional bereits umfangreich.

❌ Die gesamte Fachlogik, mehrere Tabellenabfragen und Zuordnungsregeln liegen noch in einer React-Komponente.

### Ziel
```text
features/maintenance/
  MaintenanceContractOverview.tsx
  MaintenanceContractList.tsx
  MaintenanceContractDetail.tsx

services/maintenance/
  maintenance-contract-service.ts

repositories/maintenance/
  maintenance-contract-repository.ts
```

---

## 11.2 Detailansicht

### WPF / MAUI
Die Detailansicht zeigt insbesondere:

- Titel
- Beschreibung
- max. Kontingent
- belegt
- frei
- aktive Mitgliedszuordnungen
- Gültigkeit
- Gartennummern

Bei globaler Verwaltung zusätzlich:

- Bearbeiten
- Mitglieder zuweisen

### Web aktuell
✅ entsprechender Detailbereich vorhanden.

Zusätzlich zeigt Web:

- Bereich
- Pflichtstundenbefreiung
- Bemerkung
- beendete Zuordnungen

### Bewertung
🟡 Web ist teilweise umfangreicher als WPF/MAUI.

Diese Zusatzinformationen können bleiben, sollen aber fachlich sauber als:

```text
aktive Zuordnungen
historische Zuordnungen
```

getrennt werden.

---

## 11.3 Wartungsvertrag anlegen / bearbeiten

### WPF / MAUI
Der aktuell sichtbare Produktiveditor enthält:

- Titel
- Beschreibung
- max. Kontingent
- Aktivstatus

Validierung:

```text
Titel erforderlich
Max. Kontingent > 0
```

### Datenmodell
`WartungsvertragRecord` enthält darüber hinaus:

- `bereich`
- `befreit_von_pflichtstunden`
- `bemerkung`
- `is_demo`

### Web aktuell
Web bearbeitet bereits:

- Titel
- Beschreibung
- Bereich
- max. aktive Zuordnungen
- befreit von Pflichtstunden
- Bemerkung
- Aktivstatus

`is_demo` wird beim Produktivspeichern auf `false` gesetzt.

### Bewertung
✅ Web bildet das vorhandene Datenmodell vollständiger ab als der aktuelle WPF-/MAUI-Editor.

### Fachlicher Vorschlag
Diese Webfelder **nicht entfernen**.

Sie sind echte Datenbankfelder und `befreit_von_pflichtstunden` beeinflusst unmittelbar die Pflichtstundenberechnung.

Die gemeinsame Fachschicht sollte deshalb das vollständige Modell verwenden:

```ts
type MaintenanceContract = {
  id: number
  title: string
  description: string | null
  area: string | null
  maxActiveAssignments: number
  exemptsFromDutyHours: boolean
  active: boolean
  note: string | null
}
```

Langfristig sollten WPF/MAUI diese Felder ebenfalls aus derselben Fachdefinition beziehen.

---

## 11.4 Kein Hard-Delete als normaler Produktivpfad

Im geprüften WPF-/MAUI-Editor gibt es keinen normalen Löschen-Workflow für einen Wartungsvertrag.

Stattdessen existiert:

```text
aktiv = true / false
```

### Web
Web folgt diesem Ansatz bereits.

### Bewertung
✅ beibehalten.

Verträge mit Historie sollen nicht leichtfertig physisch gelöscht werden.

---

## 11.5 Mitgliedsbezogene Wartungsverträge

### WPF
`MemberWartungsvertraegeView.xaml`

### MAUI
`MemberWartungsvertraegePage.cs`

Die Mitgliedsansicht zeigt ausdrücklich:

```text
aktive Wartungsverträge
```

mit:

- Vertrag
- Gültigkeit
- Belegung
- Status

Admin/Vorstand können:

- freien Vertrag zuweisen
- aktive Zuordnung beenden

### Web aktuell
Bei `memberId` wird nach allen Zuordnungen des Mitglieds gefiltert:

```ts
memberAssignments = assignments.filter(...)
```

`visibleContracts` berücksichtigt danach **jede** historische Zuordnung.

Dadurch können auch bereits beendete Verträge weiterhin in der normalen Mitgliedsansicht erscheinen.

### Bewertung
❌ Abweichung zu MAUI.

### Ziel
Standardansicht:

```ts
maintenanceService.getActiveContractsForMember(memberId, date)
```

Historie optional separat:

```ts
maintenanceService.getContractHistoryForMember(memberId)
```

---

## 11.6 Hauptmitglied / Nebenmitglied

### Datenbank
Die Tabelle speichert ausdrücklich:

```text
wartungsvertrag_zuordnungen.hauptmitglied_id
```

Der DB-Trigger erzwingt:

```text
Wartungsverträge dürfen nur Hauptmitgliedern zugeordnet werden.
```

### Bedeutung
Ein Wartungsvertrag gehört fachlich zum Hauptmitgliedskontext.

Auch die Pflichtstundenbefreiung wird über das Hauptmitglied ausgewertet.

### Web aktuell
Im globalen Zuordnungs-Dropdown werden derzeit **alle aktiven Mitglieder** angeboten:

```ts
members.map(...)
```

also auch Nebenmitglieder.

Wird eine Nebenmitglied-ID direkt gespeichert, lehnt die Datenbank die Zuordnung ab.

### MAUI
Auch die globale mobile Auswahlliste kennzeichnet aktuell Haupt- und Nebenmitglieder. Die endgültige Datenbankregel bleibt jedoch eindeutig: gespeichert werden darf nur das Hauptmitglied.

### Ziel
Der Service normalisiert jeden Mitgliedskontext zuerst:

```ts
resolveMainMemberId(memberId)
```

danach:

```ts
assignContract(contractId, mainMemberId, validFrom)
```

Die UI soll möglichst den Hauptmitgliedskontext anzeigen und nicht erst einen DB-Fehler erzeugen.

---

## 11.7 Zuordnung aus globalem Vertragskontext

### WPF / MAUI
Eigener produktiver Weg:

```text
Wartungsvertrag
↓
Mitglieder zuweisen
```

MAUI zeigt:

- aktive Mitglieder
- Name
- Haupt-/Nebenmitglied-Kontext
- Gartennummern
- bereits aktive Zuordnung
- freie Plätze
- Sortierung nach Name oder Gartennummer
- Gültig-ab-Datum

Mehrere Mitglieder können in einem Vorgang markiert werden.

### Web aktuell
Im globalen Vertrag kann jeweils ein Mitglied aus einem Dropdown zugeordnet werden.

### Bewertung
🟡 Grundfunktion vorhanden, aber gegenüber MAUI weniger komfortabel.

### Ziel
```text
features/maintenance/assignments/
  AssignMembersToContract.tsx
  MaintenanceMemberSelector.tsx
```

Service:

```ts
maintenanceAssignmentService.getAssignableMembers(contractId, validFrom)
maintenanceAssignmentService.assignMembers(contractId, memberIds, validFrom)
```

Eine Mehrfachauswahl wie in MAUI ist sinnvoll.

---

## 11.8 Zuordnung aus Mitgliedskontext

### WPF / MAUI
Zweiter produktiver Weg:

```text
Mitglied
↓
Wartungsverträge
↓
freien Vertrag zuweisen
```

MAUI verwendet dafür:

```text
GetAssignableWartungsvertraegeForMitgliedAsync(...)
```

und erlaubt ebenfalls mehrere Verträge in einem Speichervorgang.

### Web aktuell
Web berechnet selbst:

```ts
assignableForMember =
  contracts.filter(
    aktiv
    && heutiges Kontingent frei
    && heute noch nicht zugeordnet
  )
```

### Problem
Diese Browserberechnung betrachtet nur die **heutige** aktive Belegung.

Eine neue Zuordnung kann aber mit:

```text
Gültig ab = zukünftiges Datum
```

beginnen.

Die Datenbank prüft Überschneidungen und Kontingent dagegen für den **gesamten betroffenen Zeitraum**.

### Bewertung
❌ Kandidatenermittlung im Browser ist fachlich zu einfach.

### Ziel
```ts
maintenanceAssignmentService.getAssignableContractsForMember(
  memberId,
  validFrom
)
```

Die Fachlogik bestimmt die Kandidaten passend zum gewünschten Gültigkeitsbeginn.

---

## 11.9 Zentrale Datenbankregeln für Zuordnungen

Der Trigger:

```text
validate_wartungsvertrag_zuordnung
```

ist die verbindliche letzte Instanz.

Er prüft:

### Nur Hauptmitglied

```text
hauptmitglied_id muss auf ein Hauptmitglied zeigen
```

### Keine zeitliche Doppelzuordnung

Gleiche Kombination:

```text
Vertrag + Hauptmitglied
```

darf sich zeitlich nicht mit einer anderen Zuordnung überschneiden.

### Kontingent über den Zeitraum

`max_aktive_zuordnungen` darf nicht nur heute, sondern an **keinem Zeitpunkt innerhalb des neuen Zuordnungszeitraums** überschritten werden.

### Bewertung
✅ sehr gute zentrale Absicherung.

### Architekturregel
Diese Regeln dürfen im Web für bessere UX vorgeprüft werden, aber:

```text
Datenbank = verbindliche Validierung
```

Der Browser ist nicht die führende Fachinstanz.

---

## 11.10 Belegung / Frei

### WPF / MAUI
Die Übersicht erhält bereits fachlich aufbereitete Werte:

```text
MaxKontingent
Belegt
Frei
```

über die Service-Funktion.

### Web aktuell
Web berechnet selbst:

```ts
occupancy(contractId)
```

aus den aktuell geladenen Zuordnungen und dem heutigen Datum.

### Bewertung
🟡 für eine Momentaufnahme brauchbar, aber unnötige Schattenlogik.

### Ziel
```ts
maintenanceContractService.getOverview()
```

liefert direkt:

```ts
{
  maxCapacity,
  occupied,
  free
}
```

wie WPF/MAUI.

---

## 11.11 Zuordnung beenden

### WPF / MAUI
Eine aktive Zuordnung wird beendet durch:

```text
gueltig_bis setzen
```

MAUI verwendet im Mitgliedspfad aktuell:

```text
heutiges Datum
```

### Web aktuell
Web erlaubt ein frei wählbares Enddatum.

Validierung:

```text
Enddatum >= Gültig-ab
```

### Bewertung
✅ die Web-Erweiterung ist sinnvoll.

### Ziel
Service:

```ts
maintenanceAssignmentService.endAssignment(
  assignmentId,
  validUntil
)
```

Repository führt das Update aus.

---

## 11.12 Historie nicht löschen

Das Datenmodell ist bewusst zeitbezogen:

```text
gueltig_ab
gueltig_bis
```

Eine beendete Zuordnung bleibt damit als Historie erhalten.

### Ziel
Kein physisches Löschen einer normalen beendeten Zuordnung.

Darstellung stattdessen:

```text
Aktiv
Historisch
Zukünftig
```

---

## 11.13 Pflichtstundenbefreiung

### Zentrale Fachlogik
G7 hat bereits festgelegt, dass Pflichtstunden über:

```text
fn_berechne_pflichtstunden_status(...)
```

bzw.

```text
v_pflichtstunden_uebersicht
```

ermittelt werden.

Ein Wartungsvertrag befreit nur, wenn:

```text
w.aktiv = true
w.befreit_von_pflichtstunden = true
```

und seine Zuordnung die betreffende Saison zeitlich überlappt.

### Bedeutung für G11
Die Wartungsvertrags-UI berechnet selbst **keine Pflichtstundenbefreiung**.

Sie pflegt lediglich:

```text
befreit_von_pflichtstunden
Gültigkeitszeitraum der Zuordnung
Aktivstatus
```

Die Auswirkung kommt zentral aus G7.

---

## 11.14 Auswirkung von Vertragsänderungen auf Auswertungen

Da die Pflichtstundenfunktion auf den aktuellen Vertragsdatensatz zugreift, können Änderungen an:

```text
aktiv
befreit_von_pflichtstunden
```

die Pflichtstunden-Auswertung beeinflussen.

### Ergebnis nach G12
Finalisierte `jahresabschluss_position`-Datensätze werden unveränderlich geschützt. Spätere Vertragsänderungen ändern diese gespeicherten Ergebnispositionen nicht automatisch.

Für eine vollständig revisionsfähige Rekonstruktion bleiben jedoch die in G12 beschriebenen zusätzlichen Maßnahmen nötig: vollständiger Input-Snapshot oder Schutz der abrechnungsrelevanten Quelldaten.

---

## 11.15 Demo-Daten

### Web
lädt Wartungsverträge mit:

```text
is_demo = false
```

### Bewertung
✅ Produktivdaten werden von Demo-Verträgen getrennt.

Diese Filterung gehört künftig ins Repository und nicht in die React-Komponente.

---

## 11.16 Rechte

### WPF / MAUI
Globale Verwaltung:

```text
Admin oder Vorstand
```

Mitgliedsansicht:

```text
read-only für berechtigten Nutzer
Admin/Vorstand zusätzlich zuweisen/beenden
```

### Web
Grundsätzlich entsprechend umgesetzt.

### Ziel
Berechtigungen zentral:

```ts
maintenanceAccessService.getCapabilities(userContext)
```

z. B.:

```ts
{
  canRead,
  canManageContracts,
  canAssignMembers,
  canEndAssignments
}
```

Nicht nur über ausgeblendete Buttons absichern.

---

## G11 Zielstruktur

```text
KGV.Web/
  app/
    (workspace)/
      wartungsvertraege/
        page.tsx

  features/
    maintenance/
      MaintenanceContractOverview.tsx
      MaintenanceContractList.tsx
      MaintenanceContractDetail.tsx
      MaintenanceContractEditor.tsx
      MemberMaintenanceContracts.tsx

      assignments/
        AssignMembersToContract.tsx
        AssignContractsToMember.tsx
        MaintenanceMemberSelector.tsx
        MaintenanceAssignmentList.tsx
        MaintenanceAssignmentHistory.tsx

  services/
    maintenance/
      maintenance-contract-service.ts
      maintenance-assignment-service.ts
      maintenance-access-service.ts

  repositories/
    maintenance/
      maintenance-contract-repository.ts
      maintenance-assignment-repository.ts

  models/
    maintenance/
      maintenance-contract.ts
      maintenance-contract-overview.ts
      maintenance-assignment.ts
```

## G11 verbindliche Architektur

Vertragsverwaltung:

```text
UI
↓
maintenance-contract-service
↓
maintenance-contract-repository
↓
wartungsvertraege
```

Zuordnungen:

```text
UI
↓
maintenance-assignment-service
↓
maintenance-assignment-repository
↓
wartungsvertrag_zuordnungen
↓
DB-Trigger validiert Zeitraum + Kontingent + Hauptmitglied
```

Pflichtstunden:

```text
Wartungsvertragsdaten
↓
zentrale Pflichtstundenfunktion aus G7
↓
keine eigene Berechnung in G11
```

## G11 wichtigste Korrekturen für Web

1. `MaintenanceContracts` aus `page.tsx` herauslösen.
2. Vertragsübersicht und Belegung nicht mehr in React selbst zusammensetzen.
3. Mitgliedsansicht standardmäßig auf **aktive Zuordnungen** begrenzen.
4. Historische Zuordnungen separat darstellen.
5. Haupt-/Nebenmitgliedskontext vor dem Speichern auf Hauptmitglied normalisieren.
6. Kandidaten für Zuordnungen nicht nur anhand der heutigen Belegung berechnen.
7. Zeitraum- und Kontingentregeln über den Fachservice vorbereiten; DB-Trigger bleibt führend.
8. Globalen MAUI-Komfort mit Mehrfachzuweisung als Ziel übernehmen.
9. `Bereich`, `befreit_von_pflichtstunden` und `Bemerkung` aus dem Web nicht entfernen.
10. Zuordnungen durch Gültigkeitsende beenden statt löschen.
11. Demo-Filter in Repository verschieben.
12. Pflichtstundenbefreiung ausschließlich über die zentrale G7-Fachlogik auswerten.
13. Auswirkung späterer Vertragsänderungen auf finalisierte Jahresabschlüsse in G12 prüfen.

---

# G12 – Saison & Jahresabschluss

## 12.1 Saisonverwaltung

### WPF
`SaisonverwaltungView.xaml`

WPF ist hier die klare Desktop-Referenz:

- Saisonliste links
- Editor rechts
- Jahr / ID
- Pacht pro qm
- Mitgliedsbeitrag
- Nebenmitgliedsbeitrag
- Aufnahmegebühr
- Gebühr Bauantrag
- Pflichtstunden Soll
- Euro pro Fehlstunde
- Bemerkung
- neue Saison vorschlagen
- Werte aus Vorjahr übernehmen

Fachregel:

```text
Saison-ID = Kalenderjahr
```

Vergangene Jahre sind schreibgeschützt.

Laufendes und zukünftige Jahre sind bearbeitbar.

### MAUI
`SaisonverwaltungPage.cs`

MAUI übernimmt dieselben Regeln.

Zusätzlich eindeutig:

```text
Saisonverwaltung nur Admin
```

Das Feld:

```text
Jahr / ID
```

ist in MAUI **read-only**.

### Web aktuell
`SeasonAdministration` ist bereits umgesetzt.

Vorhanden:

- Saisonliste
- Vorjahreswerte als Vorschlag
- alle aktuellen Saisonfelder
- vergangene Jahre schreibgeschützt
- Edit-Lock
- Speichern
- Workspace-Saison nach Speichern aktualisieren

### Bewertung
✅ insgesamt sehr nah an WPF/MAUI.

### Konkrete Abweichung
Im Web ist das Feld:

```text
Kalenderjahr
```

auch bei einem bestehenden Datensatz editierbar.

WPF und MAUI behandeln:

```text
ID = Jahr
```

als unveränderlichen Schlüssel.

### Ziel
Bei bestehender Saison:

```text
Jahr / ID read-only
```

Nur beim automatisch erzeugten neuen Saisonvorschlag wird das Jahr festgelegt.

---

## 12.2 Saisonwerte nicht in React pflegen

### Web aktuell
`SeasonAdministration` liest und schreibt direkt:

```text
saison
```

über `readSupabase` / `writeSupabase`.

### Ziel
```text
features/seasons/
  SeasonManagement.tsx
  SeasonList.tsx
  SeasonEditor.tsx

services/seasons/
  season-service.ts

repositories/seasons/
  season-repository.ts
```

Service:

```ts
getSeasons()
createNextSeasonProposal()
saveSeason(...)
canEditSeason(...)
```

---

## 12.3 Jahresabschluss – aktueller Webstand

### MAUI
Es existiert bereits ein mehrstufiger Jahresabschlussbereich:

- `JahresabschlussPage.cs`
- `JahresabschlussStammdatenPage.cs`
- `JahresabschlussRechnungEditorPage.cs`
- `JahresabschlussPruefungPage.cs`
- `JahresabschlussService.cs`

### Web aktuell
Der Navigationspunkt:

```text
Jahresabschluss
```

existiert bereits.

Eine fachliche Web-Komponente ist jedoch noch **nicht angeschlossen**.

Der Menüpunkt fällt derzeit in den allgemeinen Zustand:

```text
Bereich vorbereitet
```

### Bewertung
❌ G12-Jahresabschluss ist im Browser aktuell noch nicht umgesetzt.

### Wichtig
Hier soll nicht einfach der aktuelle MAUI-Code 1:1 nach TypeScript kopiert werden.

Der MAUI-Jahresabschluss ist selbst noch ein Entwicklungsstand und enthält noch offene fachliche Punkte.

---

## 12.4 Rechte

### Saisonverwaltung
WPF / MAUI:

```text
nur Admin
```

### Jahresabschluss
MAUI:

```text
Admin oder Vorstand
```

Die RLS-Policies der Jahresabschluss-Tabellen verwenden ebenfalls:

```text
is_admin_or_vorstand()
```

### Web aktuell
Der gesamte Navigationsblock:

```text
Verwaltung
```

inklusive Jahresabschluss wird derzeit nur bei:

```text
role === admin
```

angezeigt.

### Bewertung
❌ für den Jahresabschluss zu restriktiv.

### Ziel

```text
Saisonverwaltung
→ Admin

Jahresabschluss
→ Admin + Vorstand
```

Später kann dies zusätzlich über eigene Fachrechte verfeinert werden.

---

## 12.5 Jahresabschluss-Übersicht

### MAUI
`JahresabschlussPage.cs`

Ablauf:

```text
Saison auswählen
↓
Rechnungen der Saison laden
↓
Rechnung erfassen / bearbeiten
↓
Kosten- und Umlagearten verwalten
↓
Abschluss prüfen
```

Rechnungsliste zeigt:

- Lieferant
- Rechnungsdatum
- Gesamtbetrag

### Ziel Web
```text
features/annual-close/
  AnnualCloseOverview.tsx
  AnnualCloseStatus.tsx
  AnnualInvoiceList.tsx
```

Service:

```ts
annualCloseService.getOverview(seasonId)
```

---

## 12.6 Rechnungskopf

### MAUI
`JahresabschlussRechnungEditorPage.cs`

Rechnung enthält:

- Saison
- Lieferant
- Rechnungsnummer
- Rechnungsdatum
- Leistungszeitraum von/bis
- Gesamtbetrag
- Bemerkung

### Aktuelle MAUI-UI
Im sichtbaren Editor werden derzeit vor allem gepflegt:

- Lieferant
- Rechnungsnummer
- Rechnungsdatum
- Gesamtbetrag
- Bemerkung

Die Datenbank besitzt zusätzlich:

```text
leistungs_von
leistungs_bis
```

### Ziel Web
Das vollständige Datenmodell nutzen.

```text
features/annual-close/invoices/
  AnnualInvoiceEditor.tsx

services/annual-close/
  annual-invoice-service.ts

repositories/annual-close/
  annual-invoice-repository.ts
```

---

## 12.7 Kostenarten

### Datenbank / MAUI
Eigene Tabelle:

```text
kostenart
```

Felder:

- Bezeichnung
- Beschreibung
- aktiv

MAUI erlaubt:

- neue Kostenart
- aktivieren/deaktivieren

Bereits verwendete Arten werden nicht gelöscht.

### Ziel
```text
features/annual-close/master-data/
  CostTypeManagement.tsx
```

Kein Hard-Delete für verwendete Kostenarten.

---

## 12.8 Umlagearten

### Datenbank
Standardarten:

```text
U1     Umlage pro Mitglied
U2     Umlage nach Fläche und Belegungstagen
U-S    Strom-Grundkosten nach Anschluss und Belegungstagen
U-W    Wasser-Grundkosten nach Anschluss und Belegungstagen
V-S    Stromverbrauch
V-W    Wasserverbrauch
EINZEL Einzelkosten
KEINE  Keine Weiterverteilung
```

Technische Verteilungsarten:

```text
pro_mitglied
pro_flaeche_und_tag
anschluss_und_tag
verbrauch
einzel
keine
```

### MAUI
Stammdatenverwaltung erlaubt:

- neue Umlageart
- Kürzel
- Bezeichnung
- Verteilungsart
- aktiv/inaktiv

### Ziel
```text
features/annual-close/master-data/
  AllocationTypeManagement.tsx
```

---

## 12.9 Rechnungszuordnungen

Eine Rechnung kann auf beliebig viele Zuordnungszeilen verteilt werden.

Jede Zeile enthält:

- Rechnung
- Kostenart
- Umlageart
- Betrag
- optional Parzelle
- optional Mitglied
- Bemerkung

### Zentrale DB-Regel

```text
Summe Zuordnungen <= Rechnungsbetrag
```

Die Datenbank verhindert eine Überverteilung.

Für:

```text
EINZEL
```

muss eine Parzelle oder ein Mitglied angegeben sein.

### MAUI
Zusätzlich wird in der UI verhindert:

```text
bereits zugeordnet + neuer Betrag > Gesamtbetrag
```

### Ziel
DB-Regeln bleiben führend.

Web zeigt zusätzlich laufend:

```text
Rechnungsbetrag
Zugeordnet
Restbetrag
```

---

## 12.10 Unvollständig verteilte Rechnungen

### MAUI
Wenn:

```text
Summe Zuordnungen != Gesamtbetrag
```

wird angezeigt:

```text
Die Rechnung ist noch nicht vollständig verteilt
```

### Prüfservice
Eine nicht vollständig verteilte Rechnung ist ein:

```text
blockierender Fehler
```

### Bewertung
✅ fachlich sinnvoll.

### Ziel
Der Web-Editor soll unvollständige Rechnungen deutlich markieren.

---

## 12.11 Prüfbericht

### MAUI
`JahresabschlussPruefungPage.cs`

Unterscheidet:

```text
Blockierende Fehler
Warnungen
```

Aktionen:

- Prüfbericht aktualisieren
- Entwurf berechnen / neu berechnen
- endgültig abschließen

### Ziel
```text
features/annual-close/review/
  AnnualCloseReview.tsx
  AnnualCloseErrors.tsx
  AnnualCloseWarnings.tsx
```

---

## 12.12 Prüfung der Rechnungen

Aktuell blockierend:

```text
nicht vollständig verteilte Rechnung
```

### Bewertung
✅ übernehmen.

---

## 12.13 Prüfung der Jahresendablesungen

Hier besteht eine wichtige fachliche Abweichung innerhalb des aktuellen MAUI-Standes.

### Bereits vorhandene zentrale JEA-Fachlogik
Die RPC:

```text
rpc_export_jahresablesung_status
```

definiert:

Ein Zähler ist JEA-pflichtig, wenn er am:

```text
31.12.
```

noch eingebaut ist.

Gesucht wird ausdrücklich:

```text
art = jea
```

Für im Laufe des Jahres ausgebaute Zähler gilt:

```text
Ausbauablesung beendet den Zähler
→ keine zusätzliche JEA erforderlich
```

### Aktueller JahresabschlussService
`PruefeAsync(...)` prüft dagegen lediglich, ob für das Medium irgendeine Ablesung innerhalb des Saisonjahres existiert.

Damit kann beispielsweise:

```text
normale Ablesung vorhanden
JEA fehlt
```

im aktuellen Jahresabschluss-Prüfweg unter Umständen trotzdem als vorhanden gelten.

### Bewertung
❌ Jahresabschluss-Prüfung und vorhandene JEA-Fachlogik sind noch nicht vereinheitlicht.

### Ziel
Der Jahresabschluss muss dieselbe zentrale JEA-Regel verwenden wie:

```text
rpc_export_jahresablesung_status
```

Keine zweite Definition der Jahresendablesung im Web.

---

## 12.14 Foto-Prüfung

### G6
Die Foto-Pflicht ist konfigurierbar:

```text
meter_reading_photo_required
```

### Aktueller JahresabschlussService
Er behandelt fehlende Fotos pauschal als:

```text
Pflichtfoto fehlt
```

ohne im geprüften Code die Einstellung `meter_reading_photo_required` auszuwerten.

### Bewertung
🟡 muss vor endgültiger Jahresabschlussimplementierung vereinheitlicht werden.

### Ziel
Die zentrale Zähler-/JEA-Prüfung entscheidet abhängig von der tatsächlichen Vereinskonfiguration.

---

## 12.15 Verbrauchswarnungen

Der aktuelle Prüfservice prüft:

```text
Strom > 10.000
Wasser > 1.000
```

und erzeugt Warnungen.

### Problem
Die Verbrauchsdifferenz wird im aktuellen Service aus allen geladenen Ablesungen des Zählers gebildet und nicht eindeutig auf den Saisonzeitraum begrenzt.

### Ziel
Verbrauch ausschließlich aus den für die Saison gültigen Verbrauchssegmenten bestimmen.

---

## 12.16 Pächterwechsel

### Aktueller Prüfservice
Mehr als eine überlappende Belegung einer Parzelle innerhalb der Saison erzeugt:

```text
Pächterwechsel im Saisonzeitraum
```

als Warnung.

### Datenmodell
`jahresabschluss_verbrauch` besitzt bereits einen Grund:

```text
normal
zaehlerwechsel
paechterwechsel
zaehlerwechsel_und_paechterwechsel
```

### Bewertung
✅ Datenmodell ist auf segmentierte Verbrauchsberechnung vorbereitet.

❌ Die aktuelle Berechnung befüllt diese Verbrauchstabelle noch nicht.

---

## 12.17 Aktueller Berechnungsweg ist noch Zwischenstand

`JahresabschlussService.BerechneAsync(...)` berechnet derzeit hauptsächlich Positionen aus:

```text
jahresabschluss_rechnung
+
jahresabschluss_rechnung_zuordnung
```

### Wichtiger Befund
Das Interface selbst beschreibt als Ziel:

```text
serverseitig / RPC-gestützt
```

Die aktuelle Implementierung führt die Verteilungsberechnung jedoch noch in C# aus.

### Bewertung
🟡 MAUI ist hier aktueller Entwicklungsstand, nicht endgültige Facharchitektur.

### Konsequenz für Web
Die Berechnungslogik **nicht nach TypeScript portieren**.

---

## 12.18 U1 – Umlage pro Mitglied

Die Datenbank definiert:

```text
U1 → pro_mitglied
```

### Aktueller C#-Berechnungsweg
`BuildTargets(...)` besitzt keinen eigenen Zweig für:

```text
U1
```

Nicht speziell behandelte Umlagen erhalten aktuell als Gewicht:

```text
Belegungstage
```

### Bewertung
❌ entspricht nicht eindeutig der definierten Verteilungsart `pro_mitglied`.

### Ziel
Die Berechnung muss die Spalte:

```text
umlageart.verteilung
```

als fachliche Regel verwenden und nicht nur das Kürzel.

---

## 12.19 U2 – Fläche und Belegungstage

Aktueller C#-Berechnungsweg:

```text
Gewicht = Belegungstage × Parzellenfläche
```

### Bewertung
✅ entspricht grundsätzlich:

```text
pro_flaeche_und_tag
```

---

## 12.20 U-S / U-W – Grundkosten

Aktueller C#-Berechnungsweg:

- nur Parzellen mit entsprechendem Anschluss
- Gewicht nach Belegungstagen

### Bewertung
✅ grundsätzlich passend zu:

```text
anschluss_und_tag
```

---

## 12.21 V-S / V-W – Verbrauchskosten

Die Datenbank definiert:

```text
V-S → Stromverbrauch → verteilung = verbrauch
V-W → Wasserverbrauch → verteilung = verbrauch
```

### Aktueller C#-Berechnungsweg
Für `V-S` und `V-W` existiert in `BuildTargets(...)` kein eigener Verbrauchszweig.

Damit fallen sie aktuell in den allgemeinen Verteilungsweg.

### Bewertung
❌ echte verbrauchsabhängige Verteilung ist im aktuellen Berechnungsservice noch nicht implementiert.

### Ziel
Verbrauchskosten müssen aus den saisonbezogenen Zählersegmenten stammen:

```text
Startstand
Endstand
Verbrauch
Pächter
Parzelle
Zähler
Zeitraum
```

und in:

```text
jahresabschluss_verbrauch
```

nachvollziehbar gespeichert werden.

---

## 12.22 Pacht

Die Saison enthält:

```text
pacht_pro_qm
```

### Aktueller JahresabschlussService
Im geprüften `BerechneAsync(...)` wird daraus derzeit keine Jahresabschlussposition erzeugt.

### Bewertung
❌ noch offen.

### Ziel
Pachtpositionen müssen aus:

```text
Fläche
×
Pachtpreis
×
abrechnungsrelevanter Zeitraum
```

bzw. der festgelegten Vereinsregel erzeugt werden.

---

## 12.23 Mitgliedsbeiträge

Die Saison enthält:

- Mitgliedsbeitrag
- Nebenmitgliedsbeitrag

### Aktueller Berechnungsservice
Im geprüften Jahresabschluss-Berechnungsweg werden diese Werte noch nicht zu Positionen verarbeitet.

### Bewertung
❌ noch offen.

---

## 12.24 Pflichtstunden / Fehlstunden

### G7
Die zentrale Fachquelle ist:

```text
v_pflichtstunden_uebersicht
```

mit:

- Sollstunden
- Befreiungen
- geleisteten freigegebenen Stunden
- offenen Stunden
- Euro pro Fehlstunde
- Fehlbetrag

### Aktueller JahresabschlussService
Der Fehlbetrag wird im geprüften `BerechneAsync(...)` noch nicht als Jahresabschlussposition übernommen.

### Bewertung
❌ noch offen.

### Ziel
Der Jahresabschluss verwendet ausschließlich den zentral berechneten Fehlbetrag aus G7.

Keine neue Pflichtstundenberechnung im Jahresabschluss.

---

## 12.25 Wartungsverträge im Jahresabschluss

Damit ist der Prüfpunkt aus G11 geklärt.

### Vor dem Abschluss
Die Pflichtstundenübersicht berücksichtigt den zum Saisonzeitraum gültigen Wartungsvertrag.

### Nach dem endgültigen Abschluss
Die erzeugten:

```text
jahresabschluss_position
```

werden durch DB-Trigger unveränderlich.

Spätere Änderungen an Wartungsverträgen ändern diese gespeicherten Positionen nicht automatisch.

### Bewertung
✅ Ergebnis-Snapshot bleibt stabil.

### Noch offen
Die ursprünglichen Quelldaten – z. B. Wartungsvertrag oder Zuordnung – werden dadurch nicht automatisch eingefroren.

Für eine vollständig revisionsfähige Rekonstruktion sollte deshalb entweder:

- ein vollständiger Input-Snapshot gespeichert werden, oder
- nach Abschluss die abrechnungsrelevanten Quelldaten der Saison geschützt werden.

---

## 12.26 Vorauszahlungen

Die Datenbank besitzt bereits:

```text
jahresabschluss_vorauszahlung
```

mit:

- Saison
- Mitglied
- optional Parzelle
- Zahlungsdatum
- Betrag
- Verwendungszweck

### Aktueller Stand
Im geprüften MAUI-Service und in der UI ist dafür noch kein vollständiger Workflow vorhanden.

### Bewertung
❌ fachliche Grundlage vorhanden, Bedien- und Berechnungsweg noch offen.

---

## 12.27 Entwurf und Berechnungsversion

### Datenmodell
Jahresabschlussstatus:

```text
in_bearbeitung
berechnet
geprueft
abgeschlossen
```

Zusätzlich:

```text
berechnung_version
```

### Aktueller Service
Bei jeder Neuberechnung wird die Version erhöht.

Status wird dabei auf:

```text
berechnet
```

gesetzt.

### Bewertung
✅ Versionsidee vorhanden.

---

## 12.28 Status `geprueft`

Obwohl das Datenmodell kennt:

```text
geprueft
```

setzt der aktuell geprüfte Service diesen Status nicht.

Der Ablauf ist aktuell praktisch:

```text
berechnet
↓
abgeschlossen
```

### Bewertung
🟡 Statusmodell und Workflow sind noch nicht vollständig verbunden.

### Ziel
Entweder:

```text
geprueft
```

als echten verpflichtenden Workflow-Schritt nutzen,

oder den unnötigen Status aus dem Fachmodell entfernen.

---

## 12.29 Warnungen müssen begründet werden

### MAUI-Oberfläche
Überschrift:

```text
Warnungen – vor Abschluss begründen und bestätigen
```

### Aktueller Stand
Im geprüften Workflow existiert jedoch:

- keine Eingabe der Begründung
- keine persistierte Warnungsbestätigung
- keine Prüfung einer Begründung beim Finalisieren

Wenn keine blockierenden Fehler vorliegen, kann trotz Warnungen abgeschlossen werden.

### Bewertung
❌ Text und tatsächlicher Workflow stimmen noch nicht überein.

### Ziel
Warnungen erhalten vor Abschluss einen nachvollziehbaren Prüfdatensatz:

```text
Warnung
Begründung
bestätigt von
bestätigt am
```

---

## 12.30 Berechnung nicht atomar

### Aktueller Service

Neuberechnung:

```text
1. jahresabschluss auf berechnet setzen
2. bestehende Positionen löschen
3. neue Positionen einfügen
```

`ReplaceJahresabschlussPositionenAsync(...)` führt:

```text
DELETE
danach INSERT
```

als getrennte Datenbankoperationen aus.

### Risiko
Wenn der INSERT nach erfolgreichem DELETE fehlschlägt, kann ein:

```text
Status = berechnet
```

existieren, obwohl Positionen fehlen.

### Bewertung
❌ für einen revisionsrelevanten Abschluss nicht ausreichend atomar.

### Ziel
Eine serverseitige Transaktion/RPC, z. B.:

```text
rpc_jahresabschluss_berechnen(...)
```

führt in **einer Transaktion** aus:

- Prüfung
- Berechnung
- Snapshot
- Versionswechsel
- Positionen ersetzen

Web und MAUI rufen nur diese Fachoperation auf.

---

## 12.31 Endgültig abschließen

### MAUI
Vor Abschluss:

- Prüfservice erneut ausführen
- Berechnungsentwurf muss existieren
- Positionen müssen vorhanden sein
- Abschlussbestätigung anzeigen
- abschließendes Mitglied speichern

Anschließend:

```text
status = abgeschlossen
abgeschlossen_am
abgeschlossen_von
```

### Bewertung
✅ Grundworkflow sinnvoll.

### Ziel
Auch das Finalisieren serverseitig atomar:

```text
rpc_jahresabschluss_abschliessen(...)
```

---

## 12.32 Unveränderlicher Abschluss

### Datenbank
Trigger:

```text
prevent_abgeschlossener_jahresabschluss_change
```

verhindert:

- UPDATE
- DELETE

eines bereits abgeschlossenen Jahresabschlusses.

Zusätzliche Migration:

```text
prevent_final_jahresabschluss_position_change
```

verhindert:

- INSERT
- UPDATE
- DELETE

an Positionen eines abgeschlossenen Abschlusses.

### Bewertung
✅ sehr wichtige und richtige Absicherung.

Die Browser-App muss diese Regeln nicht nachbauen, sondern nur den Read-only-Zustand darstellen.

---

## 12.33 Quelldaten nach Abschluss

Aktuell geschützt:

```text
jahresabschluss
jahresabschluss_position
```

Nicht automatisch durch diesen Abschlussstatus geschützt sind unter anderem:

```text
jahresabschluss_rechnung
jahresabschluss_rechnung_zuordnung
saison
wartungsvertrag_zuordnungen
arbeitsstunde
zaehler_ablesung
```

### Konsequenz
Der endgültige Ergebnis-Snapshot bleibt zwar unverändert.

Die später sichtbaren Quelldaten können jedoch vom damaligen Berechnungsstand abweichen.

### Empfehlung
Vor produktivem Rechnungsabschluss festlegen:

```text
A) Input-Snapshot vollständig speichern
```

oder

```text
B) abrechnungsrelevante Quelldaten einer abgeschlossenen Saison sperren
```

Für Nachvollziehbarkeit ist Variante A meist flexibler, weil Korrekturen anschließend sauber über einen Korrekturabschluss laufen können.

---

## 12.34 Korrekturabschluss

### Datenbank
Die Foundation enthält bereits:

```text
jahresabschluss_korrektur
jahresabschluss_korrektur_position
```

mit Bezug auf:

```text
ursprungsabschluss_id
```

### Aktueller Stand
Im geprüften MAUI-Service und in der UI existiert noch kein vollständiger Korrekturworkflow.

### Bewertung
🟡 Datenmodell vorbereitet, Fachworkflow noch offen.

### Ziel
Nach endgültigem Abschluss niemals Originalpositionen ändern.

Korrektur:

```text
Originalabschluss
↓
Korrekturabschluss
↓
Differenzpositionen
↓
eigene Freigabe / Finalisierung
```

---

## 12.35 Browser-Zielarchitektur

Der Web-Jahresabschluss soll **nicht** eigene Rechenregeln enthalten.

```text
React UI
↓
annual-close-service
↓
annual-close-repository
↓
serverseitige Jahresabschluss-RPCs
↓
Supabase-Transaktion
```

### Zielstruktur
```text
KGV.Web/
  app/
    (workspace)/
      saisons/
        page.tsx
      jahresabschluss/
        page.tsx

  features/
    seasons/
      SeasonManagement.tsx
      SeasonList.tsx
      SeasonEditor.tsx

    annual-close/
      AnnualCloseOverview.tsx
      AnnualCloseStatus.tsx

      invoices/
        AnnualInvoiceList.tsx
        AnnualInvoiceEditor.tsx
        InvoiceAllocationEditor.tsx

      master-data/
        CostTypeManagement.tsx
        AllocationTypeManagement.tsx

      review/
        AnnualCloseReview.tsx
        AnnualCloseErrors.tsx
        AnnualCloseWarnings.tsx
        WarningConfirmation.tsx

      result/
        AnnualClosePositions.tsx
        AnnualCloseMemberSummary.tsx

      correction/
        AnnualCloseCorrection.tsx

  services/
    seasons/
      season-service.ts

    annual-close/
      annual-close-service.ts
      annual-invoice-service.ts
      annual-master-data-service.ts
      annual-correction-service.ts

  repositories/
    seasons/
      season-repository.ts

    annual-close/
      annual-close-repository.ts
      annual-invoice-repository.ts
      annual-master-data-repository.ts

  models/
    annual-close/
      annual-close.ts
      annual-invoice.ts
      annual-position.ts
      annual-review.ts
```

---

## G12 verbindlicher Ablauf

```text
Saison auswählen
↓
Abrechnungs-Stammdaten prüfen
↓
Rechnungen erfassen
↓
Rechnungen vollständig zuordnen
↓
JEA / Zählerprüfung
↓
Pächterwechsel / Zählerwechsel prüfen
↓
Pflichtstundenstatus übernehmen
↓
Pacht / Beiträge / Verbrauch / Umlagen / Einzelkosten / Vorauszahlungen berechnen
↓
Prüfbericht
↓
Warnungen begründen
↓
serverseitig atomaren Entwurf erzeugen
↓
Entwurf prüfen
↓
endgültig abschließen
↓
Snapshot unveränderlich
↓
spätere Änderungen nur über Korrekturabschluss
```

## G12 wichtigste Korrekturen für Web und Fachkern

1. Web-Jahresabschluss überhaupt als Fachbereich implementieren.
2. Jahresabschluss für **Admin und Vorstand** zugänglich machen; Saisonverwaltung bleibt Admin.
3. Saisonjahr bei bestehenden Saisons im Web read-only machen.
4. MAUI-Berechnungslogik nicht nach TypeScript kopieren.
5. Jahresabschlussberechnung in eine atomare serverseitige DB-Funktion/RPC verlagern.
6. JEA-Prüfung mit `rpc_export_jahresablesung_status` vereinheitlichen.
7. `meter_reading_photo_required` bei der Abschlussprüfung berücksichtigen.
8. Verbrauchsprüfung sauber auf den Saisonzeitraum begrenzen.
9. `V-S` und `V-W` tatsächlich nach Verbrauch verteilen.
10. `U1` tatsächlich nach der definierten Verteilungsart `pro_mitglied` behandeln.
11. Pachtpositionen ergänzen.
12. Mitglieds-/Nebenmitgliedsbeiträge ergänzen.
13. Pflichtstunden-Fehlbetrag aus der zentralen G7-Fachquelle übernehmen.
14. Vorauszahlungen in UI und Berechnung ergänzen.
15. `geprueft` entweder als echten Workflowstatus verwenden oder das Statusmodell vereinfachen.
16. Warnungen müssen wirklich begründet und bestätigt werden.
17. Entwurfsberechnung und Positionsersetzung atomar machen.
18. Ergebnis-Snapshot weiterhin unveränderlich halten.
19. Quelldaten-Nachvollziehbarkeit nach Abschluss zusätzlich absichern.
20. Korrekturabschluss als einzigen Weg für nachträgliche finanzielle Änderungen implementieren.

---

# G13 – Benutzer, Rollen & Rechte

## 13.1 Rollenmodell

Die fachlich führenden Rollen bleiben:

```text
admin
vorstand
user
```

Zusätzlich besitzt `app_user`:

```text
permission_grants
permission_revocations
```

Die wirksamen Rechte werden nach dem bestehenden Core-Modell berechnet:

```text
(Rollenbasis | zusätzliche Rechte) & ~entzogene Rechte
```

### Bewertung
✅ Dieses Modell ist in WPF, MAUI und Web grundsätzlich vorhanden.

---

## 13.2 Rollenbasis

Die zentrale Core-Definition ist derzeit:

### User

```text
Mitglieder sehen
eigene Daten sehen
```

### Vorstand

unter anderem:

```text
Mitglieder suchen/sehen/bearbeiten
Stammdaten anzeigen/lesen/bearbeiten
Parzellen anzeigen/lesen/bearbeiten
Dokumente lesen/verwalten
Arbeitsstunden lesen/verwalten
Zähler lesen
Zählerwechsel verwalten
Ablesungen freigeben
Rollen/Rechte lesen
```

### Admin

```text
Vorstand
+
Mitglieder aufnehmen / verpachten
+
Rollen verwalten
```

### Ziel
Diese Rollenbasis darf im Web nur **ein einziges Mal** definiert sein.

---

## 13.3 Kritische Abweichung im Web – zwei Rollenberechnungen

Web besitzt aktuell zwei getrennte Implementierungen:

```text
permissionsFor(context)
```

für Workspace/Navigation und

```text
rolePermissions(role)
```

für den Rechteeditor.

Diese sind nicht identisch.

### Konkretes Beispiel Vorstand

Die Core-Definition und `rolePermissions(...)` enthalten:

```text
showParzellen
readParzellen
writeParzellen
```

`permissionsFor(...)` enthält für Vorstand dagegen aktuell nur:

```text
readParzellen
```

### Folge
Der Rechteeditor kann beispielsweise:

```text
Parzellen → Bearbeiten
```

als Rollenstandard anzeigen, während der eigentliche Workspace dieses Recht nicht gleich berechnet.

### Bewertung
❌ konkreter Berechtigungsfehler.

### Verbindliche Korrektur
Ein zentrales Web-Permission-Modul:

```text
services/access/permission-service.ts
```

bzw. ein gemeinsamer Permission Catalog ist die einzige Quelle für:

- Rollenbasis
- Grants
- Revocations
- effektive Rechte
- Navigation
- UI-Aktionen

---

## 13.4 Permission Catalog

Der Core besitzt bereits einen strukturierten Katalog mit Fachbereichen:

```text
Mitglieder aufnehmen / verpachten
Stammdaten
Parzellen
Dokumente
Arbeitsstunden
Zählerwechsel
Ablesungsfreigaben
Rollen / Rechte
```

mit Zugriffsstufen:

```text
Aus
Lesen
Bearbeiten
```

### Web aktuell
Die gleiche Struktur wurde in `page.tsx` erneut manuell als:

```text
permissionAreas
```

angelegt.

### Ziel
Die Werte müssen exakt mit dem Core übereinstimmen.

Keine mehrfach gepflegten Bitmasken mehr über verschiedene React-Komponenten.

---

## 13.5 Rechteeditor

### WPF / MAUI
Der Rechteeditor zeigt:

- Rollenbasis
- verknüpften App-User
- zusätzliche Rechte
- entzogene Rechte
- effektive Rechte
- Fachbereiche mit Aus / Lesen / Bearbeiten
- auf Rollenstandard zurücksetzen
- speichern

### Web aktuell
`UserRightsAdministration`

unterstützt bereits:

- Mitgliedsauswahl
- App-User-Status
- Rollenbasis
- Fachrechte
- Reset auf Rollenstandard
- Speichern
- Edit-Lock

### Bewertung
✅ Oberfläche und Grundlogik sind bereits gut.

### Ziel
Aus `page.tsx` herauslösen und vollständig auf den zentralen Permission-Service stellen.

---

## 13.6 Rechte nur bei verknüpftem App-User

WPF / MAUI unterscheiden:

```text
Mitglied vorhanden
aber
noch kein App-User verknüpft
```

In diesem Zustand:

- die Rollenbasis bzw. der vorgesehene Benutzerkontext kann angezeigt werden
- produktive Benutzeraktionen bleiben begrenzt
- Rollen-/Override-Speicherung erfolgt erst mit belastbarer App-User-Verknüpfung

### Web
✅ grundsätzlich entsprechend umgesetzt.

---

## 13.7 Nutzer hinzufügen / Erstlogin

### MAUI / WPF
Einladung verwendet den bestehenden OTP-/Erstlogin-Pfad.

### Web aktuell
✅ `inviteAppUser(...)` wird verwendet.

### Ziel
Die React-Komponente ruft künftig nur:

```ts
userAccountService.inviteMember(...)
```

auf.

---

## 13.8 Passwort-Reset

### MAUI / WPF
Passwort-Reset läuft über den OTP-/Recovery-Hauptweg.

### Web
✅ bereits vorhanden.

### Ziel
```ts
userAccountService.requestPasswordReset(...)
```

---

## 13.9 App-Benutzerzuordnung entfernen

### MAUI / WPF
`AuthService.RemoveUserAsync(...)` prüft vor dem Entfernen den Verknüpfungsstatus.

Nur bei:

```text
MemberUserLinkStatus.Consistent
```

wird:

```text
mitglied.auth_user_id = null
app_user löschen
```

ausgeführt.

Anschließend wird kontrolliert, ob der Status:

```text
None
```

erreicht wurde.

Das zugrunde liegende Supabase-Auth-Konto wird in diesem aktuellen Pfad nicht gelöscht.

### Web aktuell
Web führt direkt aus:

```text
PATCH mitglied.auth_user_id = null
DELETE app_user
```

ohne vorherige Konsistenzprüfung.

### Bewertung
❌ wichtiger Unterschied.

### Ziel
```ts
userAccountService.removeMemberUserLink(memberId)
```

mit:

1. Linkstatus prüfen
2. nur konsistente Verbindung entfernen
3. Ergebnis erneut prüfen

Nicht zwei direkte React-Schreibvorgänge.

---

## 13.10 E-Mail-Änderung

### MAUI
Bei bestehendem `auth_user_id` darf eine E-Mail nicht einfach durch Vorstand/Admin am Mitglied geändert werden.

Der Nutzer selbst verwendet den vorgesehenen Self-Service-Mailänderungsweg inklusive OTP.

### Web
Die entsprechende Fachregel muss auch nach dem Refactoring erhalten bleiben.

G1 behandelt den eigentlichen Auth-/OTP-Flow.

---

## 13.11 Geschützte Rollenbearbeitung für Mitglied ID 7

WPF und MAUI enthalten aktuell ausdrücklich:

```text
Mitglied ID 7
→ Rollenbearbeitung gesperrt
```

### Web aktuell
Diese Sperre ist im geprüften Web-Rechteeditor nicht vorhanden.

### Bewertung
❌ Abweichung.

### Ziel
Nicht nur Button deaktivieren, sondern zentral:

```ts
roleService.canChangeRole(memberId)
```

und nach Möglichkeit serverseitig zusätzlich absichern.

---

## 13.12 Zentrale Einstellung Nutzerablesungen

WPF / MAUI Adminbereich besitzt den Schalter:

```text
allow_user_meter_reading_submissions
```

Damit wird zentral gesteuert, ob normale Nutzer eigene Ablesungen einreichen dürfen.

### Web aktuell
G6 liest diese Einstellung bereits bei der Ablesung.

Im Web-Rechte-/Adminbereich fehlt aber die Möglichkeit, sie zu ändern.

### Bewertung
❌ Verwaltung fehlt.

### Ziel
```text
features/access/
  AppSettings.tsx

services/access/
  app-setting-service.ts

repositories/access/
  app-setting-repository.ts
```

---

## 13.13 Zielstruktur

```text
KGV.Web/
  features/
    access/
      UserRightsAdministration.tsx
      RoleEditor.tsx
      PermissionAreaEditor.tsx
      UserAccountActions.tsx
      AppSettings.tsx

  services/
    access/
      permission-service.ts
      role-service.ts
      user-account-service.ts
      app-setting-service.ts

  repositories/
    access/
      app-user-repository.ts
      user-account-repository.ts
      app-setting-repository.ts

  models/
    access/
      roles.ts
      permissions.ts
      user-permission-settings.ts
```

## G13 wichtigste Korrekturen

1. Nur noch **eine** Rollen-/Permission-Berechnung im Web.
2. Vorstand-Parzellenrechte korrigieren.
3. Permission-Bits und Fachbereiche zentral definieren.
4. Navigation und UI aus demselben zentralen Access-Modell ableiten; ausdrücklich festgelegte rollenbasierte Sonderregeln bleiben zentral dokumentierte Ausnahmen.
5. Benutzerentfernung mit Linkstatus-Prüfung absichern.
6. Mitglied ID 7 auch im Web gegen Rollenänderung schützen.
7. `allow_user_meter_reading_submissions` im Adminbereich verwaltbar machen.
8. Rechteeditor aus `page.tsx` lösen.
9. Serverseitige RLS/Fachprüfung bleibt immer führend.

---

# G14 – Vereinskonfiguration & Exporte

## 14.1 Vereinskonfiguration

WPF und MAUI verwenden dieselbe zentrale aktive Vereinskonfiguration.

Enthalten sind:

- Vereinsname
- Kurzname
- Registerangabe
- Straße
- PLZ
- Ort
- Standard-E-Mail
- Standard-Telefon
- Website
- Kontoinhaber
- Bankname
- IBAN
- BIC
- Verwendungszweck Mitgliedsantrag
- Verwendungszweck Pachtvertrag
- Dokument-Ort
- Standard-Hinweistext
- Datenschutztext
- Datenschutz-Version
- Datenschutz-Stand
- Aktivstatus

### Web aktuell
`ClubConfigurationAdministration`

bildet diese Felder bereits nahezu vollständig ab.

### Bewertung
✅ fachlich gut umgesetzt.

---

## 14.2 Aktive Konfiguration

WPF / MAUI:

```text
aktive Konfiguration laden
```

Existiert noch keine:

```text
erster Speichervorgang legt sie an
```

### Web
✅ entsprechend umgesetzt.

---

## 14.3 Rechte der Vereinskonfiguration

WPF / MAUI:

```text
nur Admin bearbeitbar
```

### Web
Der Bereich liegt derzeit im Admin-Verwaltungsblock.

### Bewertung
✅ passend.

Die Server-/RLS-Prüfung bleibt zusätzlich erforderlich.

---

## 14.4 Validierung

WPF / MAUI validieren insbesondere:

```text
Datenschutz-Stand
```

als optionales Datum.

### Web
Verwendet dafür bereits ein HTML-Date-Feld.

### Ziel
Validierung trotzdem zentral:

```text
club-configuration-service.ts
```

und nicht dauerhaft nur durch Browserfeldtypen.

---

## 14.5 Export – heutige Facharchitektur

Die aktuelle MAUI-Lösung ist die maßgebliche Referenz.

Exportdefinitionen werden über Datenbank-Metadaten gesteuert:

```text
app_export_definition
app_export_filter_definition
app_export_column_definition
```

Eine Definition legt unter anderem fest:

- Titel
- Beschreibung
- Quellentyp
- Quellenname
- CSV erlaubt
- PDF erlaubt

Filter und Spalten werden separat konfiguriert.

### Ziel
Neue Exporte sollen möglichst ohne neue React-Sonderlogik angelegt werden können.

## 14.5.1 Datenschutz – aktive Mitglieder

Der Datenschutz-Export gehört fachlich zu G14. G3 und G5 liefern dabei lediglich
Mitglieds- und Parzellendaten.

Export-Key:

```text
datenschutz_aktive_mitglieder
```

Spalten in dieser Reihenfolge:

1. Garten Nr.
2. Name
3. E-Mail
4. E-Mail-Info
5. E-Mail-Rechnung
6. WhatsApp

Datenlogik:

- nur aktive Mitglieder (`mitglied.aktiv = true`)
- `mitglied_ende` ist `NULL` oder mindestens `current_date`
- Demodaten sind ausgeschlossen
- E-Mail = `mitglied.email`
- E-Mail-Info = `email_info_einwilligung`
- E-Mail-Rechnung = `email_rechnung_einwilligung`
- WhatsApp = `whatsapp_einwilligung`

## 14.5.2 Zähler – Eichfälligkeit

Der Export gehört fachlich zu **G14 – Vereinskonfiguration & Exporte**. Die
fachliche Quelle bleibt Zähler / Ablesung: `v_zaehler_eichstatus` bestimmt
weiterhin den Aktiv- und Ausbauzustand sowie die Fälligkeitsklassifikation.

- Zielposition Web: **Export / Auswertungen**
- Export-Key: `zaehler_eichfaelligkeit`
- Quelle: `rpc_export_zaehler_eichfaelligkeit`
- Relevante Zähler: `ueberfaellig` und `bald_faellig` aus der zentralen View
- Filter Zählerart: Alle / Strom / Wasser
- Ausgabe: Anzeige / CSV / PDF (bestehender Browser-Druckweg)

Die RPC ergänzt nur Parzellen-RFID und ausgabefertige Bezeichnungen; sie
berechnet die Eichfälligkeit nicht erneut. Damit verwenden Anzeige, CSV und PDF
dieselbe Datenbasis.

Gartenlogik:

- nur aktuell gültige `parzellen_belegung` (`von_datum` ist `NULL` oder höchstens
  `current_date`; `bis_datum` ist `NULL` oder mindestens `current_date`)
- Gartennummer über `parzelle.garten_nr`
- leere Gartennummern ausschließen, deduplizieren und sortieren
- mehrere Gartennummern mit `, ` verbinden
- Haupt- und Nebenmitglieder werden als fachliche Gruppe ausgegeben
- Gruppenschlüssel ist der Hauptmitgliedskontext
- die Gartensortierung richtet sich nach dem Hauptmitglied
- das Hauptmitglied erscheint vor seinen Nebenmitgliedern
- das PDF übernimmt die RPC-Reihenfolge und sortiert nicht eigenständig nach der sichtbaren Gartennummer

Architektur:

- Exportdefinition und Spalten kommen über `app_export_*`
- Datenquelle ist `rpc_export_datenschutz_aktive_mitglieder`
- der Export bleibt damit metadata-driven
- Web benötigt für Anzeige und CSV keinen neuen `export_key`-Sonderfall

Aktueller Plattformstand:

- MAUI verwendet Anzeige und CSV über die bestehende Exportinfrastruktur und
  besitzt zusätzlich ein spezielles Vereins-PDF im A4-Hochformat. Das
  Vereinsdokumentbranding liefert Logo, Vereinsname, Register, Vereins-E-Mail
  und grüne Linie; Tabellenköpfe werden auf Folgeseiten wiederholt, Ja/Nein-
  Spalten sind kompakt.
- Web kann Definition, RPC und Spalten bereits über das metadata-driven
  `ExportCenter` für Anzeige und CSV verwenden. PDF läuft weiterhin über
  `window.print()`; der spezielle .NET-PDF-Builder wird im Browser nicht
  verwendet.

Offener G14-Punkt: Soll Web später dieselbe deterministische Vereins-PDF
erzeugen, ist ein sauberer gemeinsamer oder serverseitiger PDF-Weg erforderlich.
Es wird keine zweite spezielle PDF-Implementierung in React oder `page.tsx`
gebaut.

---

## 14.6 Dynamische Filter

MAUI unterstützt:

- Text
- Zahl/Jahr
- Boolean
- Select
- statische Optionen
- Optionen aus RPC

### Web aktuell
✅ diese Grundmechanismen sind bereits vorhanden.

---

## 14.7 Exportergebnis

### MAUI
Auf größeren Geräten Tabellenansicht.

Auf Telefon:

- Datensatzansicht
- Vorher/Nächster

### Web
Tabellenansicht ist für Browser/PC passend.

Die Vorschau zeigt maximal:

```text
500 Datensätze
```

CSV enthält alle geladenen Datensätze.

### Bewertung
✅ sinnvolle browserspezifische Umsetzung.

---

## 14.8 CSV

### Web
Erzeugt CSV direkt im Browser:

- Semikolon
- korrektes Escaping
- UTF-8 BOM
- Browserdownload

### Bewertung
✅ passend.

CSV-Erzeugung soll jedoch aus React heraus in:

```text
export-csv-service.ts
```

verschoben werden.

---

## 14.9 PDF

### MAUI
Besitzt einen eigenen PDF-Exportweg.

### Web aktuell
Der Button:

```text
Drucken / als PDF speichern
```

verwendet:

```text
window.print()
```

### Bewertung
🟡 sinnvoller Browserweg, aber kein gleichwertiger deterministischer PDF-Generator.

### Ziel
Wenn Browser-Druck ausreicht:

```text
druckoptimiertes Exportlayout
→ Browser „Als PDF speichern“
```

Wenn später exakt reproduzierbare Vereins-PDFs erforderlich sind:

```text
serverseitiger PDF-Renderer
```

Keine komplizierte PDF-Logik direkt in React.

---

## 14.10 Kritischer Punkt – Web ist nicht vollständig metadata-driven

Obwohl die Tabellen `app_export_*` verwendet werden, besitzt Web mehrere Sonderfälle.

### Mitgliederliste

Bei:

```text
export_key = mitgliederliste
```

lädt React selbst:

- Mitglieder
- Parzellen
- Belegungen

und baut das Exportergebnis manuell zusammen.

### RFID-Status

Bei:

```text
export_key = rfid_status
```

ist:

```text
v_rfid_scan_context
```

direkt im React-Code fest verdrahtet.

### Arbeitsstundenübersicht

Filter werden im React-Code manuell auf RPC-Parameter umbenannt:

```text
jahr → p_jahr
stunden_offen → p_stunden_offen
...
```

### Bewertung
❌ widerspricht dem eigentlichen Metadatenprinzip.

### Ziel
Die **React-UI** kennt keine konkreten Exportkeys.

```ts
exportService.execute(definition, filters)
```

Der Service interpretiert:

- Quelle
- Filter
- Parameter
- Spalten

Wo die vorhandenen Datenbank-Metadaten einen Altfall noch nicht vollständig ausdrücken können, ist vorübergehend ein klar abgegrenzter Adapter im Export-Service zulässig. Diese Ausnahme widerspricht nicht dem Ziel: Sonderwissen darf nicht mehr in React liegen und soll, wo sinnvoll, später in die Export-Metadaten überführt werden.

---

## 14.11 Exportrechte

### MAUI
Export ist ausdrücklich:

```text
Admin oder Vorstand
```

### Web aktuell

Navigation:

```text
has(Permission.searchMembers)
```

Komponente:

```text
context.role !== "user"
```

### Folge
Ein User mit zusätzlich gewährtem `searchMembers` kann den Menüpunkt sehen, bekommt danach aber die Meldung, dass Export nur Vorstand/Admin erlaubt ist.

### Bewertung
❌ Navigation und Fachzugriff sind inkonsistent.

### Ziel
Bis ein eigenes Fachrecht `CanExport` existiert:

```text
Export sichtbar und nutzbar
→ Admin oder Vorstand
```

aus einem gemeinsamen Access-Service.

Langfristig kann ein eigenes Exportrecht ergänzt werden.

---

## 14.12 Zielstruktur

```text
KGV.Web/
  features/
    configuration/
      ClubConfiguration.tsx

    export/
      ExportCenter.tsx
      ExportDefinitionPicker.tsx
      ExportFilters.tsx
      ExportResults.tsx

  services/
    configuration/
      club-configuration-service.ts

    export/
      export-service.ts
      export-csv-service.ts
      export-print-service.ts

  repositories/
    configuration/
      club-configuration-repository.ts

    export/
      export-definition-repository.ts
      export-data-repository.ts

  models/
    configuration/
      club-configuration.ts

    export/
      export-definition.ts
      export-filter.ts
      export-column.ts
```

## G14 wichtigste Korrekturen

1. Vereinskonfiguration aus `page.tsx` herauslösen.
2. Direkten Tabellenzugriff über Service/Repository kapseln.
3. Export-UI vollständig metadata-driven machen; notwendige Altfall-Adapter nur im Export-Service kapseln.
4. Keine `export_key`-Sonderfälle mehr in React.
5. Dynamische Filter/RPC-Optionen beibehalten.
6. CSV-Erzeugung in eigenen Service verschieben.
7. Browser-Druck als PDF klar als Browserfunktion behandeln.
8. Exportnavigation und Exportberechtigung vereinheitlichen.
9. Export bis zu einem eigenen Permission-Flag konsistent auf Admin/Vorstand begrenzen.

---

# G15 – Browser- und Systemfunktionen

## 15.1 PWA / installierbare Browser-App

Web besitzt bereits:

```text
manifest.webmanifest
PwaController
service-worker.js
offline.html
```

In Produktion wird der Service Worker registriert.

Bei unterstützten Browsern erscheint über:

```text
beforeinstallprompt
```

die Aktion:

```text
App installieren
```

### Bewertung
✅ gute webnative Lösung.

---

## 15.2 Service Worker und Offline-Shell

Der Service Worker cached bewusst nur ungefährliche App-Ressourcen:

```text
offline.html
favicon
manifest
_next/assets
```

Navigation erfolgt:

```text
Network first
↓
bei Fehler offline.html
```

### Wichtig
Supabase- und andere externe Vereinsdaten werden **nicht** vom Service Worker gecacht.

### Bewertung
✅ konservativ und datenschutzfreundlich.

Der Browser besitzt damit einen **Offline-Fallback** und kann bei fehlendem Netz die statische Offline-Seite anzeigen. Die eigentliche Vereinsanwendung ist damit nicht offline arbeitsfähig.

Das ist aktuell die richtige Trennung.

---

## 15.3 Keine allgemeine Offline-Datenbank

Private Vereinsdaten sollen nicht automatisch komplett in IndexedDB oder Cache gespiegelt werden.

Lokale Speicherung wird nur dort verwendet, wo sie fachlich nötig ist, insbesondere:

```text
noch nicht hochgeladene Ablesefotos
```

### Bewertung
✅ beibehalten.

---

## 15.4 PWA-Updateverhalten

### WPF
Besitzt einen expliziten Update-Dialog mit:

- installierte Version
- neue Version
- Veröffentlichungsdatum
- Release Notes
- Pflichtupdate
- später/installieren

### Web
Updates laufen über den normalen Web-/Service-Worker-Lebenszyklus.

Ein eigener Versionsdialog existiert nicht.

### Bewertung
✅ kein Desktop-Updater nötig.

Optional kann später ein webtypischer Hinweis ergänzt werden:

```text
Neue Version verfügbar
→ Neu laden
```

statt einen Installer nachzubauen.

---

## 15.5 Session-Speicherung

Web speichert derzeit in `localStorage`:

```text
accessToken
refreshToken
expiresAt
User-ID/E-Mail
```

### Problem
Der `refreshToken` wird zwar gespeichert, aber im geprüften Webcode **nirgendwo zum Erneuern einer Session verwendet**.

Es existiert kein:

```text
grant_type=refresh_token
```

### Folge
Ist der Access Token abgelaufen:

- `loadSession()` akzeptiert die Sitzung nicht mehr
- beim Reload landet der Nutzer wieder beim Login
- laufende Requests können nach Tokenablauf fehlschlagen

obwohl der Nutzer eventuell die ganze Zeit aktiv war.

### Bewertung
❌ wichtige technische Lücke.

---

## 15.6 Ziel Session-Refresh

Ein zentraler Browser-Session-Service übernimmt:

```text
Access Token gültig
→ weiterverwenden

läuft bald ab
→ Refresh Token verwenden

Refresh erfolgreich
→ neue Session speichern

Refresh fehlgeschlagen
→ sauber abmelden
```

### Wichtig
Das 15-Minuten-Inaktivitätslogout bleibt davon unabhängig.

```text
Token-Refresh ≠ Inaktivitätsverlängerung
```

Aktivität bestimmt, ob der Benutzer eingeloggt bleiben darf.

Refresh hält lediglich die technische Supabase-Sitzung gültig.

---

## 15.7 15-Minuten-Inaktivität

Web besitzt bereits:

```text
15 Minuten
```

und beobachtet:

- Pointer
- Tastatur
- Touch
- Scrollen

### Cross-Tab
Aktivität wird über `localStorage` mit anderen Tabs desselben:

```text
Verein + Benutzer
```

synchronisiert.

### Bewertung
✅ sehr gute Browserumsetzung.

---

## 15.8 Logout / Verein wechseln

Beim Logout bzw. Vereinswechsel versucht Web zuerst:

```text
releaseAllBrowserEditLocks(...)
```

und meldet danach bei Supabase ab.

Lokaler Session-/Vereinskontext wird entfernt.

### Bewertung
✅ beibehalten.

---

## 15.9 Workspace-Persistenz

Der Browser speichert den Saisonkontext.

Mitglieds- und Parzellenkontext werden beim Speichern bewusst wieder auf:

```text
null
```

gesetzt.

### Bewertung
✅ sinnvoll.

Damit wird nicht dauerhaft im Browser gespeichert:

```text
welches konkrete Mitglied / welche Parzelle zuletzt bearbeitet wurde
```

Die Saison kann dagegen bequem erhalten bleiben.

---

## 15.10 Bearbeitungssperren

Web besitzt einen echten browserspezifischen Edit-Lock.

`useEditLock(...)`:

1. Lock anfordern
2. alle 4 Minuten verlängern
3. bei erneut sichtbarem Tab aktualisieren
4. beim Verlassen der Komponente freigeben

Standard-Timeout:

```text
600 Sekunden
```

Bei belegtem Lock wird angezeigt:

```text
Dieser Datensatz wird gerade von ... bearbeitet.
```

### Zusätzlich
PATCH/DELETE über die generischen Web-Helfer führen für registrierte Tabellen nochmals eine Lockprüfung durch.

### Bewertung
✅ sehr gute Mehrbenutzerabsicherung.

### Ziel
Lockpolitik künftig zentral im Daten-/Fachservice statt verteilt.

---

## 15.11 QR-Scanner

### MAUI
Verwendet native Kamera-/Barcodebibliothek.

### Web
Verwendet progressive Browserunterstützung über:

```text
BarcodeDetector
Kamera
```

und besitzt einen manuellen Fallback.

### Bewertung
✅ richtige Weblösung.

Browserunterstützung darf nicht Voraussetzung für die Fachfunktion sein.

---

## 15.12 Web-NFC / RFID

Web prüft:

```text
NDEFReader
+
Secure Context
```

Ist Web-NFC verfügbar:

```text
RFID-UID lesen
```

Ist es nicht verfügbar:

```text
manuelle Auswahl / MAUI-Spezialweg
```

### Bewertung
✅ sinnvoll.

Web-NFC ist ein optionaler Eingabekanal, nicht Teil der Fachlogik.

Dabei gilt die feste Trennung:

```text
components/scanner/NfcScanner.tsx
→ rein technischer Web-NFC-Zugriff

features/meters/rfid/RfidScanner.tsx
→ fachlicher G6-Wrapper, der UID/Kontext an den rfid-service übergibt
```

Der fachliche RFID-Zustandsautomat bleibt G6.

---

## 15.13 Kamera und Dateiauswahl

Bei Ablesefotos verwendet Web:

```html
<input type="file" accept="image/*" capture="environment">
```

Damit können unterstützte Mobilbrowser direkt die Rückkamera anbieten.

### Bewertung
✅ webnative Lösung.

Keine MAUI-Kameraabstraktion im Browser nachbauen.

---

## 15.14 Lokale Foto-Warteschlange

Web verwendet IndexedDB:

```text
kgv-browser-media-v1
pending-meter-photos
```

Gespeichert werden:

- Vereins-ID
- Ablesungs-ID
- Dateiname
- MIME-Type
- Bild-Blob
- Metadaten
- Status
- Versuchszähler
- letzter Fehler

### Funktionen

- lokal vormerken
- erneut versuchen
- alle erneut versuchen
- Vorschau
- lokalen Eintrag löschen
- nach erfolgreichem Upload Ablesung verknüpfen
- anschließend lokalen Blob entfernen

### Bewertung
✅ sehr gute browserspezifische Funktion.

---

## 15.15 Vereinsbezogene Trennung der Fotoqueue

Offene Fotos werden nach:

```text
clubId
```

gefiltert.

Damit zeigt ein anderer ausgewählter Verein nicht versehentlich die lokale Warteschlange eines anderen Vereins an.

### Bewertung
✅ wichtig und richtig.

---

## 15.16 Online-/Offline-Erkennung

`PendingPhotoUploads` verwendet:

```text
navigator.onLine
```

und Browserereignisse:

```text
online
offline
```

### Bewertung
✅ als Bedienhinweis sinnvoll.

Die tatsächliche Uploadantwort bleibt trotzdem entscheidend, da `navigator.onLine` keine sichere Aussage über die Erreichbarkeit von Supabase liefert.

---

## 15.17 „Nur WLAN hochladen“

Web besitzt eine lokale Einstellung:

```text
kgv-meter-photo-wifi-only
```

Wenn aktiviert, wird versucht:

```text
navigator.connection.type === "wifi"
```

zu erkennen.

### Besonderheit
Die Network Information API ist nicht in allen Browsern verfügbar.

Wenn WLAN nicht **positiv erkannt** werden kann, wird das Foto vorsichtshalber lokal in die Warteschlange gelegt.

### Bewertung
✅ konservativer Fallback.

Die UI sollte deutlich machen:

```text
„WLAN nicht erkannt“
```

bedeutet nicht zwingend:

```text
„kein WLAN vorhanden“
```

---

## 15.18 Lokale Fotos und Datenschutz

Die Warteschlange liegt im Browserprofil des verwendeten Geräts.

Sie ist nicht Teil des Service-Worker-Caches und wird nicht vereinsübergreifend angezeigt.

### Ziel
Klar kommunizieren:

```text
Offene Fotos befinden sich lokal auf diesem Gerät/in diesem Browser,
bis der Upload erfolgreich abgeschlossen oder der lokale Eintrag gelöscht wurde.
```

Kein automatisches Löschen beim Logout, da sonst ein fehlgeschlagener Upload verloren gehen könnte.

---

## 15.19 Demo-/Produktivtrennung

`supabase-auth.ts` besitzt bereits eine Liste von Tabellen, bei denen automatisch:

```text
is_demo = true/false
```

nach dem Kontotyp ergänzt wird.

### Bewertung
✅ wichtige Schutzschicht.

### Ziel
Diese technische Filterung gehört in den zentralen Supabase-Repository-Client und nicht in Fachkomponenten.

---

## 15.20 `supabase-auth.ts` ist zu breit

Die Datei enthält aktuell gleichzeitig:

- Vereinsauswahl
- Session
- Login
- AppUser-Kontext
- generisches REST CRUD
- RPC-Aufrufe
- Edit-Locks
- Einladung
- Passwort-Reset
- Dokument-Download
- Dokument-Upload
- Archivierung
- Vertragsgenerierung
- Foto-Upload

### Bewertung
❌ widerspricht der neuen Zielarchitektur.

### Ziel
Technische Basis und fachliche Adapter sauber aufteilen:

```text
lib/supabase/
  client.ts
  rest-client.ts
  rpc-client.ts

services/auth/
  session-service.ts
  club-service.ts

services/concurrency/
  edit-lock-service.ts

repositories/documents/
  document-file-repository.ts

repositories/contracts/
  contract-repository.ts

repositories/meters/
  meter-photo-repository.ts
```

Damit bleiben die bereits in G4, G6 und G10 definierten Fachservices führend; G15 erzeugt keine zweite Vertrags-, Dokument- oder Foto-Fachlogik.

---

## 15.21 Zielstruktur

```text
KGV.Web/
  app/
    PwaController.tsx

  components/
    scanner/
      QrScanner.tsx
      NfcScanner.tsx

  hooks/
    useEditLock.ts
    useOnlineStatus.ts

  lib/
    browser/
      media-capabilities.ts
      indexed-db.ts
      connectivity.ts

    supabase/
      client.ts
      rest-client.ts
      rpc-client.ts

  services/
    auth/
      session-service.ts

    offline/
      pending-photo-queue-service.ts

    concurrency/
      edit-lock-service.ts
```

## G15 wichtigste Korrekturen

1. Access-Token-Refresh mit dem bereits gespeicherten Refresh-Token implementieren.
2. 15-Minuten-Inaktivitätslogout davon getrennt erhalten.
3. Cross-Tab-Aktivität beibehalten.
4. PWA weiterhin nur als sichere Offline-Shell verwenden.
5. Keine pauschale Offline-Kopie von Vereinsdaten anlegen.
6. Foto-Warteschlange in IndexedDB beibehalten.
7. WLAN-Erkennung als optionale Browserhilfe behandeln.
8. QR und Web-NFC als progressive Features mit Fallback behalten.
9. Edit-Locks beibehalten und zentralisieren.
10. `supabase-auth.ts` in kleinere technische und fachliche Module zerlegen.
11. Demo-/Produktivfilter in der Datenzugriffsschicht erhalten.
12. Optional später einen webtypischen „Neue Version – neu laden“-Hinweis ergänzen.

---

# Gesamtfazit der Analyse G1–G15

Die Browser-App besitzt bereits überraschend viel produktive Funktionalität. Das Hauptproblem ist nicht, dass alles neu gebaut werden müsste, sondern dass viele Fachbereiche derzeit in:

```text
KGV.Web/app/page.tsx
```

und dem sehr breiten:

```text
KGV.Web/lib/supabase-auth.ts
```

zusammenlaufen.

Der Umbau soll deshalb überwiegend ein **fachliches Herauslösen und Vereinheitlichen** sein:

```text
UI
↓
Fachservice
↓
Repository / technische Services
↓
Supabase / Edge Functions / Browser APIs
```

WPF bleibt die Referenz für Desktop-Aufbau und Bedienmuster.

MAUI bleibt die wichtigste Referenz für aktuelle Fachlogik, Rechte und produktive Abläufe.

Wo MAUI selbst noch einen Entwicklungsstand besitzt – insbesondere beim Jahresabschluss – wird die Fachlogik zuerst zentral konsolidiert und nicht blind in Web übertragen.

Die unter **„Gruppenübergreifende Zuständigkeiten“** festgelegte Ownership ist für den Umbau verbindlich. Querschnittsseiten dürfen Fachservices anderer Gruppen verwenden; dieselbe Fachregel oder derselbe Tabellen-/View-Zugriff soll aber nicht in mehreren Gruppen parallel neu implementiert werden.

# Aktueller Bearbeitungsstand

| Gruppe | Status |
|---|---|
| G1 Anmeldung & Vereinsauswahl | ✅ analysiert |
| G2 Startseite & Navigation | ✅ analysiert |
| G3 Mitglieder | ✅ analysiert |
| G4 Mitgliedsantrag & Verträge | ✅ analysiert |
| G5 Parzellen | ✅ analysiert |
| G6 Zähler & Ablesungen | ✅ analysiert |
| G7 Arbeitsstunden | ✅ analysiert |
| G8 Arbeitseinsätze | ✅ analysiert |
| G9 Termine & Bekanntmachungen | ✅ analysiert |
| G10 Dokumente | ✅ analysiert |
| G11 Wartungsverträge | ✅ analysiert |
| G12 Saison & Jahresabschluss | ✅ analysiert |
| G13 Benutzer, Rollen & Rechte | ✅ analysiert |
| G14 Vereinskonfiguration & Exporte | ✅ analysiert |
| G15 Browser-/Systemfunktionen | ✅ analysiert |

## Begonnenes Web-Refactoring – architektonisches Grundgerüst

Für die schrittweise Herauslösung aus der bisherigen Übergangsstruktur wurden in
`KGV.Web` die Ordner `features`, `services`, `repositories`, `models` und
`contexts` angelegt. Sie trennen künftig fachgruppenspezifische UI,
Fachservices, Datenzugriffe, Fachmodelle und gemeinsamen React-Kontext.
Ergänzend wurden `lib/supabase` für gemeinsame technische Supabase-Basisfunktionen
und `lib/browser` für technische Browser-Infrastruktur vorbereitet.

Die bestehenden großen Dateien `app/page.tsx` und `lib/supabase-auth.ts` bleiben
in diesem Schritt unverändert und dienen weiter als Übergangsstruktur. Es wurde
noch keine Fachgruppe vollständig migriert und keine Fachlogik verschoben. Die
Unterordner für einzelne Fachgruppen entstehen erst mit deren kontrollierter
Migration. G12 (insbesondere Jahresabschluss) bleibt ausdrücklich ausgeklammert.

## G1 begonnen – erster vertikaler Schnitt ClubSelection

Der erste G1-Schnitt löst die Vereinsauswahl aus `KGV.Web/app/page.tsx` nach
`features/auth/ClubSelection.tsx`. Der direkt benötigte, generische QR-Scanner
liegt nun unter `components/scanner/QrScanner.tsx`. Die Auflösung der Vereins-ID
läuft über `services/auth/club-service.ts`; der darin gekapselte Zugriff auf
`resolve_vereinscode` liegt in `repositories/auth/club-repository.ts`. Das
gemeinsame Modell `ClubContext` liegt unter `models/auth/club.ts`.

Aus `page.tsx` wurden die ClubSelection- und QR-Scanner-UI entfernt. Aus
`lib/supabase-auth.ts` wurde ausschließlich `resolveClub` entfernt; Login,
Session, gespeicherter Vereinskontext und Vereinswechsel verbleiben zunächst
unverändert. Die weiteren G1-Bereiche Login, OTP, Passwort-Flow,
Session-Refresh und AuthProvider wurden anschließend in kontrollierten
Folgeschnitten abgeschlossen. G12 bleibt ausdrücklich ausgeklammert.
