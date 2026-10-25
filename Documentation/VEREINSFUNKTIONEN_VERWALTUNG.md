# Verwaltung von Vereinsfunktionen

Die Verwaltungsoberfläche steht im Web unter **Verwaltung → Vereinsfunktionen**
und ist ausschließlich für die technischen Rollen `admin` und `vorstand`
erreichbar. Diese Rollen sind nur Zugriffsrechte; sie sind keine Vereinsämter.

Die Oberflächen pflegen unmittelbar die bestehenden Tabellen
`vereinsfunktion`, `vereinsfunktion_besetzung`, `vereinsfunktion_vertretung`
und `vereinsfunktion_verwendung`. Die Datenbank bleibt für Einzel- oder
Mehrfachbesetzung, inklusive Zeitgrenzen, Vertretungsgrenzen und
Selbstvertretungen die maßgebliche Instanz. Eine Besetzung wird zum Beenden
historisch mit `gueltig_bis` abgeschlossen und nicht gelöscht. Funktion,
Mitglied und Beginn einer angelegten Besetzung sind unveränderlich; eine
Nachbesetzung wird stets als neuer Datensatz angelegt. Das gilt auch für
zukünftige Besetzungen, damit sie nicht still umgeschrieben werden können.
Für eine irrtümlich angelegte zukünftige Besetzung gibt es bewusst keinen
Lösch- oder Umwidmungspfad: Das bestehende Modell kennt noch keine
historienfeste Stornierung. Dieser Restpunkt bleibt für eine spätere,
ausdrücklich auditierte Korrekturregel offen.

Der technische Schlüssel einer Funktion wird nur bei der Neuanlage erzeugt
und danach nicht mehr geändert. Eine sichtbare Bezeichnungsänderung verändert
ihn nicht. Eine Funktion kann jeden der fünf derzeit fachlich verwendeten
Zwecke genau einmal erhalten: `impressum`, `vertragsunterzeichnung`,
`mitgliedsaufnahme_abstimmung`, `mitgliedsaufnahme_entscheidung` und
`wartungsvertrag_entscheidung`.

Impressumswerte werden ausschließlich für eine `impressum`-Verwendung
bearbeitet: Gruppenkennung und -bezeichnung, beide Sortierungen,
Hauptkontakt und ausdrückliche Anschriftsfreigabe. Keine Bedienoberfläche
leitet diese Daten aus Funktionsbezeichnungen, Schlüsseln oder App-Rollen ab.

Die MAUI-App stellt denselben Verwaltungsbereich unter **Verwaltung →
Vereinsfunktionen** für `admin` und `vorstand` bereit. Sie verwendet den
zentralen `ISupabaseService` und die gemeinsamen Core-Records für Funktionen,
Besetzungen, Vertretungen und Verwendungen. Damit werden weder eigene
HTTP-Zugriffe noch abweichende Regeln oder Textheuristiken eingeführt.

Web und MAUI können die fünf Verwendungszwecke anlegen, bearbeiten und
entfernen. Die Zweckliste ist bewusst fachlich festgelegt; ein allgemeiner
Editor für beliebige neue Zweckschlüssel ist nicht Bestandteil der Verwaltung.

Das aktuelle Impressum verwendet ausschließlich die kontrollierte RPC
`impressum_kontakte()`. Sie löst aktive Funktionen mit der Verwendung
`impressum` und aktuell gültigen Besetzungen auf. Gruppierung, Hauptkontakt
und Anschriftsfreigabe sind strukturierte Metadaten; der Hauptkontakt zeigt
Funktion, Namen und – sofern von der RPC geliefert – Handynummer. Vertretungen
sind bewusst nicht Teil des Impressums.

`impressum_funktion_slot` bleibt ausschließlich als schreibgeschütztes
Legacy-Archiv für die damalige Übernahme erhalten. Es ist keine aktuelle
Impressumsquelle und erhält keine neuen Schreibpfade. Hardcodierte
Vereinsdaten in Branding-Fallbacks und alten PDF-Templates gehören nicht zum
aktuellen Web-/MAUI-Impressum und werden erst separat bereinigt.

Jeder Speichervorgang betrifft genau einen fachlichen Datensatz; die
Datenbank-Trigger prüfen weiterhin alle übergreifenden Regeln. Die mobile
Oberfläche ordnet diese Rückmeldungen verständlichen deutschen Hinweisen zu.
