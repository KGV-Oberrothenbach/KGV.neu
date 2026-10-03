# Datenzugriff

Repositories kapseln Tabellen-, View-, RPC-, Edge-Function- und Storage-Zugriffe
eines Fachbereichs. Sie liefern Daten-DTOs und führen Datenmutationen aus, aber
enthalten keine React-Zustände, Navigation oder Dialoglogik.

Gemeinsame technische Supabase-Helfer liegen unter `lib/supabase/`. Fachliche
Repository-Unterordner werden erst angelegt, wenn der zugehörige Fachbereich
schrittweise aus der Übergangsstruktur herausgelöst wird.
