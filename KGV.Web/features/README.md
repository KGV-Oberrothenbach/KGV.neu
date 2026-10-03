# Fachliche UI-Module

Dieser Ordner ist die künftige Heimat fachgruppenspezifischer React-Komponenten.
Die Komponenten bleiben für Darstellung, Formularzustand, Nutzeraktionen und
Navigation zuständig. Sie enthalten weder direkte Supabase-Zugriffe noch
fachliche Workflow- oder Berechtigungsregeln.

Unterordner werden erst beim kontrollierten Herauslösen einer Fachgruppe
angelegt, zum Beispiel `auth`, `navigation`, `members`, `parcels`, `meters`,
`work-hours`, `work-assignments`, `appointments`, `announcements`, `documents`,
`maintenance`, `access`, `configuration` und `export`.

G12 (Saison und Jahresabschluss) wird hier vorerst nicht migriert oder fachlich
umgebaut.
