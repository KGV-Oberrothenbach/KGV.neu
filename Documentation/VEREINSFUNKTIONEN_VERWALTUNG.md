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

Die MAUI-App zeigt die bestehende mobile Verwaltungsarchitektur derzeit nicht
als parallele Schreiboberfläche für diesen neuen, mehrteiligen Datenbereich.
Die fachlich vollständige Pflege erfolgt im Web; MAUI nutzt weiterhin die
identische Impressum-RPC als Leseweg. Vor einer mobilen Schreiboberfläche ist
eine eigene, atomare Service-/Editorstrecke erforderlich, damit keine
Teiländerungen an Besetzungen und Vertretungen entstehen.
