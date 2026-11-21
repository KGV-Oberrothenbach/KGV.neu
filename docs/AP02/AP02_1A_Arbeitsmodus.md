# AP-02 / AP-03 – verbindlicher schlanker Umsetzungsmodus

Ab 21.11.2026 nach ausdrücklicher Nutzerweisung: **Umsetzen statt wiederholter Vollinventuren**.

- Fachliche Grundlage: AP-01-Katalog (10 Gruppen, 55 Rechte), Sollmatrix und fachliche E01–E05-Entscheidungen. Keine Neuentscheidung der abgeschlossenen Punkte.
- Bei AP-02 und AP-03 nur **einen kurzen Ausgangsstands-Check** (Branch/HEAD, Migrationen, Git-Status) und **gezielte Tests der tatsächlich geänderten Pfade** plus kleine Integrations-/Smokechecks.
- Kein erneuter Komplettdurchlauf aller 464 SQL-Funktionen, 136 Policies, 109 Trigger, UI-Funktionen und 246 Testfälle in jedem Teilpaket.
- Bei konkretem Sicherheitsproblem gezielt nachprüfen und den berührten Scope absichern; nicht den Umbau pauschal stoppen. Nicht getestete Funktionalität nicht als bestanden deklarieren.
- Erst Änderungen lokal auf Arbeitsbranch, danach Demo mit ausdrücklicher Freigabe und kurzem Negativtest; Produktion nur nach separatem Beschluss, Backup und verifizierter Migration.
- WPF ist ausgeschlossen.

## AP-02.1A – Schattenmodell

Neue Tabellen und ein ausschließlich lesender Vorschau-RPC. Keine Änderung an `app_user.role`, alten Rechtemasken, Logins, RLS-Policies von Bestandstabellen, Edge-Funktionen oder realen Geschäfts-RPCs.

**Wichtig:** Neue Benutzerhauptrollen sind bewusst nicht automatisch gesetzt. Ein Legacy-Admin kann fachlich Bauausschuss, Vorstand oder etwas anderes sein. Die neue Admin-Zusatzrolle wird ausschließlich für bereits verifizierte geschützte Konten im neuen Schattenmodell eingetragen. Der alte Zugang bleibt bis zur späteren Umstellung maßgeblich. Kein fachliches Schreibrecht wird durch den neuen Katalog freigeschaltet.

**F01 offen:** Finale/Korrekturabschlüsse bleiben im Schattenkatalog gesperrt. `wertermittlung.bearbeiten` ist als noch nicht implementiert deaktiviert. **F02/F03** werden vor Übergang der betreffenden einzelnen Fachpfade entschieden, nicht für jeden anderen Pfad.

**Nächste Teilschritte:** AP-02.1B Benutzerzuordnung und Admin-geschützte Mutations-RPCs; AP-02.2 effektiver produktiver Guard + Adapter; AP-02.3 Web/MAUI-Refresh; AP-02.4 einzelne RLS/RPC/Edge/Foto-Pfade; AP-02.5 UI; Demo-Abnahme; separates Produktiv-Fenster.
