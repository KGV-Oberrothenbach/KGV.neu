# Technische Supabase-Basis

Dieser Ordner nimmt schrittweise die rein technische Basis aus
`lib/supabase-auth.ts` auf: Client-Konfiguration, REST-Zugriffe, RPC-Aufrufe
und die zentrale Demo-/Produktivfilterung. Er enthält keine Fachlogik.

`supabase-auth.ts` bleibt während der Migration die kompatible Übergangsfassade.
Auth-, Session- und fachliche Dateifunktionen werden erst bei der jeweiligen
Fachgruppenmigration in die dafür vorgesehenen Services und Repositories
überführt.
