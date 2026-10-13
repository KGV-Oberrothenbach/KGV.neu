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

Alle technischen Clients benötigen einen expliziten `ClubContext`.
REST-, RPC-, Storage- und authentifizierte Edge-Aufrufe erhalten außerdem eine
`BrowserSession`; Fachrepositories rufen sie mit `{ club, session }` auf.
Öffentliche Edge-Aufrufe aus dem Auth-/OTP-Flow übergeben `{ club }` ohne
Session. Services und Features reichen den bereits im `AuthProvider`
verfügbaren Verein durch. Kein Transportpfad liest `loadClub()` oder
Browser-Speicher, und es gibt keine globale Vereinsvariable.

Die Transport-Kompatibilitätsfassade ist entfernt. `lib/supabase-auth.ts`
enthält nur noch Auth-, Session-, Club- und Workspace-Persistenz-nahe
Verantwortung. Edit-Lock-Orchestrierung und fachliche Edge-Wrapper liegen in
ihren jeweiligen Service-/Repository-Schichten.
