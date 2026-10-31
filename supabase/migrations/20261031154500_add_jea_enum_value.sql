-- Ergänzt den kanonischen Enum-Wert für Jahresendablesungen.
-- Bestehende Werte: normal, ausbau, jae, einbau
-- Kanonischer technischer Wert der Anwendung: jea
--
-- Bewusst eigene Migration:
-- PostgreSQL verlangt, dass ein neu hinzugefügter Enum-Wert erst nach
-- Abschluss der Migration verwendet wird.

alter type public.ablesung_art
  add value if not exists 'jea';
