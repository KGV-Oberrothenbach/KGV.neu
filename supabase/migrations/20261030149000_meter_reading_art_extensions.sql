-- PostgreSQL erlaubt die unmittelbare Verwendung eines neuen Enum-Werts erst
-- nach Abschluss der Migration, daher bewusst vor der Workflow-Migration.
alter type public.ablesung_art add value if not exists 'jae';
alter type public.ablesung_art add value if not exists 'einbau';
