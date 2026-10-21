# Impressum aus Vereinsfunktionen

Das Impressum liest ausschließlich aktive Funktionen mit dem Verwendungszweck
`impressum` und deren am aktuellen Stichtag gültige Besetzungen. Die Anzeige
verwendet keine Namen, Schlüssel oder Textteile zur fachlichen Einordnung.

Die `impressum`-Verwendung trägt die Anzeige-Metadaten: stabiler
Gruppenschlüssel, Gruppenbezeichnung und -reihenfolge sowie die beiden
ausdrücklichen Eigenschaften `impressum_ist_hauptkontakt` und
`impressum_zeige_ladungsfaehige_anschrift`. Eine Anschrift kann nur für einen
Hauptkontakt freigegeben werden. Bestehende Funktionen werden neutral der
Gruppe „Weitere Funktionen“ zugeordnet; es findet keine Interpretation von
Legacy-Slots statt.

Die RPC `impressum_kontakte()` gibt normalen angemeldeten Benutzern nur die
notwendigen Kontaktfelder zurück. Sie liefert weder historische Besetzungen,
Vertretungen noch Vertragsverwendungen oder sonstige Mitgliedsstammdaten.
