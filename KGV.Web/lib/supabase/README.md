# Technische Supabase-Transportbasis

Die Dateien in diesem Ordner enthalten ausschließlich technischen Transport.
Sie erhalten Verein und Access Token stets explizit als `SupabaseClientContext`.
Sie lesen weder Browser-Speicher noch `loadClub()` und halten keine globale
„aktuelle Verein“-Variable.

- `client-context.ts`: kleiner Club-/Session-Kontext sowie SSR-taugliche
  Supabase-Konfigurationsauflösung.
- `rest-client.ts`: PostgREST für Lesen, Schreiben und Löschen. Die bisherige
  Demo-Filterliste ist reine Kompatibilitätslogik, keine Sicherheitsgrenze;
  RLS bleibt verbindlich.
- `rpc-client.ts`: generische RPC-Aufrufe.
- `edge-client.ts`: generische JSON- und Raw-Aufrufe an Edge Functions.
- `storage-client.ts`: generische signierte Storage-URLs.

In G15.6a bleibt `lib/supabase-auth.ts` eine Kompatibilitätsfassade für die
vielen bestehenden Caller. Sie erstellt übergangsweise aus `loadClub()` einen
expliziten Transportkontext und delegiert an diese Clients. Auth, Session,
Workspace-Persistenz, Edit-Lock-Orchestrierung und Fachwrapper bleiben dort.

G15.6b migriert die Fachcaller auf einen expliziten `ClubContext` und entfernt
diese Übergangsbrücke.
