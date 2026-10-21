# Vereinsfunktionen – V3 Fachmodell

Stand: 21.10.2026

## 1. Ziel

V3 legt die fachlichen Regeln für Vereinsfunktionen, Besetzungen, Amtszeiten,
Vertretungen und Verwendungszwecke fest.

Grundlage ist das mit V2 eingeführte allgemeine Modell:

- vereinsfunktion
- vereinsfunktion_besetzung
- vereinsfunktion_verwendung

V3 legt bewusst keine feste Liste von Vereinsämtern fest.
Konkrete Funktionen werden als Daten gepflegt und nicht im Anwendungscode
fest einprogrammiert.

Die technische App-Rolle in app_user.role bleibt vollständig von einer
Vereinsfunktion getrennt.

## 2. Vereinsfunktion

Eine Vereinsfunktion beschreibt ein fachliches Amt oder eine sonstige
Funktion innerhalb des Vereins.

Beispiele dürfen durch den Verein später frei angelegt werden; die Software
setzt keine konkrete Liste wie Vorsitz, Kassierer oder Schriftführer voraus.

Eine Vereinsfunktion besitzt insbesondere:

- einen stabilen technischen Schlüssel
- eine frei änderbare Bezeichnung
- einen Aktivstatus
- die ausdrücklich gepflegte Eigenschaft, ob zeitgleiche Mehrfachbesetzungen
  erlaubt sind (Standard: erlaubt)

Der technische Schlüssel bleibt nach erstmaliger Verwendung stabil.

Eine Änderung der sichtbaren Bezeichnung darf historische Zuordnungen und
Dokumente nicht verändern.

## 3. Besetzung

Eine Besetzung verbindet:

- eine Vereinsfunktion
- ein Mitglied
- einen Gültigkeitszeitraum
- eine Reihenfolge

Eine Vereinsfunktion darf grundsätzlich mehrfach gleichzeitig besetzt sein.

Damit können insbesondere Ausschüsse oder andere Funktionen mit mehreren
Personen abgebildet werden.

Die Software darf deshalb nicht grundsätzlich erzwingen, dass pro Funktion
nur genau eine aktive Person existiert.

Falls eine bestimmte Funktion später fachlich nur einfach besetzt werden
darf, muss dies ausdrücklich für diese Funktion definiert werden und darf
nicht aus ihrer Bezeichnung abgeleitet werden.

## 4. Amtszeiten und Historie

Besetzungen werden historisch geführt.

Eine bestehende Besetzung wird bei einem Personenwechsel nicht überschrieben,
sondern beendet.

Dafür gelten:

- gueltig_ab = Beginn der bekannten Besetzung
- gueltig_bis = Ende der Besetzung
- NULL bei gueltig_bis = derzeit ohne festgelegtes Ende

Historische Besetzungen bleiben erhalten.

Der in V2 bei einer Datenübernahme gesetzte Beginn stellt ausschließlich den
Übernahmezeitpunkt dar und darf nicht als tatsächlich rekonstruierter
Amtsbeginn interpretiert werden.

## 5. Zeitliche Überschneidungen

Zeitliche Überschneidungen sind grundsätzlich zulässig, weil eine Funktion
mehrfach besetzt sein kann.

Eine allgemeine Datenbankregel

"pro Funktion darf zu einem Zeitpunkt nur eine Person aktiv sein"

wird deshalb nicht eingeführt.

Soll eine konkrete Funktion später nur einfach besetzt werden dürfen, benötigt
sie dafür eine ausdrückliche fachliche Eigenschaft. Ist Mehrfachbesetzung für
eine Funktion nicht erlaubt, dürfen sich auch historische oder zukünftige
Besetzungszeiträume dieser Funktion nicht überschneiden.

## 6. Vertretungen

Eine Vertretung darf nicht aus der Bezeichnung einer Funktion oder aus
Textbestandteilen wie

- stellvertretend
- Stellvertreter
- 2. Vorsitz
- stv.

abgeleitet werden.

Zwei Fälle sind fachlich zu unterscheiden:

### 6.1 Eigenständige Funktion

Eine Funktion wie beispielsweise eine stellvertretende Funktion kann als
eigenständige Vereinsfunktion geführt werden.

Sie besitzt dann ihre eigene Besetzung und Amtszeit.

### 6.2 Zeitweise Vertretung einer anderen Funktion

Eine zeitweise Vertretung einer bereits besetzten Funktion ist eine eigene
fachliche Beziehung.

Sie darf nicht allein dadurch entstehen, dass eine Person irgendeine andere
Vereinsfunktion besitzt.

Für eine spätere Umsetzung muss ausdrücklich gespeichert werden:

- welche Besetzung vertreten wird
- durch welche Besetzung oder Person
- ab wann
- gegebenenfalls bis wann

V3b führt Vertretungen ausschließlich über zwei vorhandene Besetzungen. Eine
zusätzliche direkte Mitgliedsvertretung ist nicht nötig, weil die Vertreter-
Besetzung bereits Mitglied, Funktion und Amtszeit eindeutig festhält. Der
Vertretungszeitraum muss vollständig innerhalb der Amtszeit sowohl der
vertretenen als auch der vertretenden Besetzung liegen. Dieselbe Person darf
nicht ihre eigene Besetzung vertreten.

V3 führt keine automatische Vertretung aufgrund einer Funktionsbezeichnung ein.

## 7. Verwendungszwecke

Die Funktion selbst und ihre Verwendung werden getrennt behandelt.

Eine Vereinsfunktion kann für keinen, einen oder mehrere Zwecke vorgesehen
sein.

Bereits vorhandener Verwendungszweck:

- impressum

Geplanter weiterer Verwendungszweck:

- vertragsunterzeichnung

Weitere Zwecke können später ergänzt werden.

Aus einer Verwendung darf keine andere Verwendung automatisch folgen.

Insbesondere bedeutet:

- Anzeige im Impressum ist keine Zeichnungsberechtigung.
- Zeichnungsberechtigung erzeugt keine technische App-Berechtigung.
- Eine technische Rolle admin oder vorstand erzeugt keine Vereinsfunktion.

## 8. Vertragsunterzeichnung

Eine Person darf einen Vereinsvertrag nicht allein deshalb unterzeichnen,
weil sie

- app_user.role = admin
- app_user.role = vorstand
- im Impressum angezeigt wird

hat.

Für eine Vereinsunterzeichnung muss zum Zeitpunkt der Unterzeichnung eine
fachlich gültige Besetzung einer dafür vorgesehenen Vereinsfunktion bestehen.

Die zulässige Funktion wird über einen ausdrücklichen Verwendungszweck
vertragsunterzeichnung bestimmt.

Die konkrete Prüfung erfolgt gegen die zum Unterzeichnungszeitpunkt gültige
Besetzung.

## 9. Vertrags-Snapshot

Ein abgeschlossener Vertrag darf nicht nachträglich durch Änderungen an
Mitgliedern, Vereinsfunktionen oder Besetzungen verändert werden.

Bei der Vereinsunterzeichnung werden deshalb mindestens als Snapshot
gespeichert:

- Mitglied beziehungsweise Person
- angezeigter Name
- Vereinsfunktion
- Funktionsbezeichnung
- Zeitpunkt der Unterzeichnung

Die historische Besetzung bleibt zusätzlich als fachlicher Ursprung erhalten.

Der Vertrag rendert später aus dem Snapshot und nicht aus dem jeweils aktuellen
Stand der Vereinsfunktion.

## 10. Technische App-Rollen

app_user.role bleibt ausschließlich technische Berechtigungsrolle.

Zulässige Rollen bleiben:

- admin
- vorstand
- user

Diese Rollen steuern beispielsweise:

- Verwaltungszugriff
- Navigation
- Data-API-/RLS-Berechtigungen

Sie beschreiben ausdrücklich kein Vereinsamt.

Eine Vereinsfunktionsbesetzung darf daher niemals automatisch:

- app_user.role setzen
- app_user.role verändern
- Arbeitsstundenbefreiungen auslösen
- Unterzeichnungsrechte außerhalb ausdrücklich definierter Verwendungen erzeugen

## 11. Impressum

Die bisherige Erkennung von Vorsitz, Stellvertretung oder Bauausschuss aus
slot_key oder Funktionstext ist ausschließlich Legacy-Anzeigelogik.

Diese Textheuristik wird nicht Bestandteil des neuen Fachmodells.

Beim späteren Umbau des Impressums müssen Anzeige und Gruppierung aus
strukturierten Vereinsfunktionsdaten erfolgen.

Bis zu dieser Umstellung bleibt der bestehende produktive Impressumspfad
unverändert bestehen.

## 12. Pflegeberechtigung

Die Berechtigung, Vereinsfunktionen und Besetzungen zu verwalten, ist eine
technische Berechtigung.

Vorerst bleibt die mit V2 bestehende Regel bestehen:

- Schreiben: admin / vorstand
- normale Benutzer: nur die ausdrücklich freigegebenen Lesedaten

Eine fachliche Vereinsfunktion allein darf keine Verwaltungsberechtigung
erzeugen.

## 13. Datenmodell – Konsequenzen für V3

Das V2-Modell deckt bereits ab:

- frei definierbare Funktionen
- Mehrfachbesetzungen
- Beginn und Ende einer Besetzung
- Historie
- mehrere Verwendungszwecke

Zusätzlich zu prüfen bzw. umzusetzen sind:

1. fachliche Validierung von Unterzeichnern
2. Vertrags-Snapshot Person + Funktion
3. spätere strukturierte Impressumsgruppierung
4. Verwaltungsoberfläche für Funktionen, Besetzungen und Vertretungen

## 14. Nicht Bestandteil von V3a

V3a legt ausschließlich das Fachmodell fest.

Noch nicht Bestandteil dieses Schritts sind:

- konkrete Vereinsämter
- Produktivdaten
- Impressum-Umschaltung
- Web-Verwaltungsoberfläche
- MAUI-Verwaltungsoberfläche
- Vertragsänderungen
- Signaturänderungen
- automatische Datenmigrationen über V2 hinaus

## 15. Festgelegte Grundsätze

Zusammenfassend gelten:

1. Vereinsfunktionen sind frei konfigurierbare Fachdaten.
2. Technische App-Rollen und Vereinsämter bleiben strikt getrennt.
3. Mehrfachbesetzungen sind grundsätzlich zulässig.
4. Besetzungen werden historisch statt überschreibend geführt.
5. Vertretungen werden ausdrücklich modelliert und nicht aus Text erraten.
6. Verwendungszwecke sind voneinander unabhängig.
7. Impressumsanzeige bedeutet keine Zeichnungsberechtigung.
8. Unterzeichnungsrechte entstehen nur aus einer dafür vorgesehenen gültigen Besetzung.
9. Verträge speichern Person und Funktion als unveränderlichen Snapshot.
10. Historische Vertragsinhalte ändern sich nicht durch spätere Funktions- oder Personenwechsel.
