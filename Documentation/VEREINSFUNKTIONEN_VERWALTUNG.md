# Verwaltung von Vereinsfunktionen

Die Verwaltungsoberfläche steht im Web unter **Verwaltung → Vereinsfunktionen**
und ist ausschließlich für die technischen Rollen `admin` und `vorstand`
erreichbar. Diese Rollen sind nur Zugriffsrechte; sie sind keine Vereinsämter.

Die Oberfläche pflegt unmittelbar die bestehenden Tabellen
`vereinsfunktion`, `vereinsfunktion_besetzung`, `vereinsfunktion_vertretung`
und `vereinsfunktion_verwendung`. Die Datenbank bleibt für Einzel- oder
Mehrfachbesetzung, inklusive Zeitgrenzen, Vertretungsgrenzen und
Selbstvertretungen die maßgebliche Instanz. Eine Besetzung wird zum Beenden
historisch mit `gueltig_bis` abgeschlossen und nicht gelöscht.

Impressumswerte werden ausschließlich für eine `impressum`-Verwendung
bearbeitet: Gruppenkennung und -bezeichnung, beide Sortierungen,
Hauptkontakt und ausdrückliche Anschriftsfreigabe. Keine Bedienoberfläche
leitet diese Daten aus Funktionsbezeichnungen, Schlüsseln oder App-Rollen ab.

Die MAUI-App stellt denselben Verwaltungsbereich unter **Verwaltung →
Vereinsfunktionen** für `admin` und `vorstand` bereit. Sie verwendet den
zentralen `ISupabaseService` und die gemeinsamen Core-Records für Funktionen,
Besetzungen, Vertretungen und Verwendungen. Damit werden weder eigene
HTTP-Zugriffe noch abweichende Regeln oder Textheuristiken eingeführt.

Jeder Speichervorgang betrifft genau einen fachlichen Datensatz; die
Datenbank-Trigger prüfen weiterhin alle übergreifenden Regeln. Die mobile
Oberfläche ordnet diese Rückmeldungen verständlichen deutschen Hinweisen zu.
