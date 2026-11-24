# FR2-11: HTTP-E2E-Matrix – vorbereitet, **nicht ausgeführt**

Basis: `main` `d945f914eecd5cc10c6df1abe894b65759c9d45b`, DemoDB mit 290 Migrationen, Stand 24.11.2026. FR2-10 ist integriert. Vor den späteren E2E-Tests Rechte, Projektreferenzen, Test-IDs und Deployments nochmals prüfen.

**Testkonten:** lokale DPAPI-Identitäten `admin.secret` und `mitglied.secret`, ergänzt um zeitweilige, nach Testende wiederhergestellte Fachrechts-Overrides. Nur DemoDB, nur synthetische Daten. Keine Token/Passwortausgabe.

**Unbedingt zwei unabhängige Assertions:** HTTP-Status **und** fachlich korrekter Datensatz-/Dateiinhalt. Negative Tests vor den positiven. Jede Testausführung mit Status `not_run` starten.

| Test-ID | Identität / Rechte | Request / Aktion | Erwartung | Unabhängige Kontrolle |
|---|---|---|---|---|
| FR211-01 | ohne JWT | POST `/rest/v1/rpc/rpc_execute_export` | 401/403, keine Zeilen | keine Datenänderung |
| FR211-02 | Mitglied ohne Exportrecht | beliebige gültige Exportart | 403, keine Zeilen | Policy/Override-Zustand |
| FR211-03 | nur `export.ausfuehren` | `mitgliederliste` | 403 (Auswertung/Fachleserecht fehlt) | keine Mitgliederzeilen |
| FR211-04 | nur `auswertung.lesen` | `mitgliederliste` | 403 (Exportrecht fehlt) | keine Mitgliederzeilen |
| FR211-05 | beide + `mitglied.lesen` und `parzelle.lesen` | `mitgliederliste` | Erfolg | DB-IDs, Filter, Spalten, Zeilenanzahl |
| FR211-06 | beide, ohne `mitglied.lesen` | `datenschutz_aktive_mitglieder` | 403 | keine PII |
| FR211-07 | beide, `parzelle.lesen` ohne `mitglied.lesen` | `parzellenliste` | 403 | Pächter dürfen nicht erscheinen |
| FR211-08 | alle vier benötigten Rechte | `parzellenliste`, `belegung=belegt` | Erfolg | DB-Filter, Parzellenanzahl |
| FR211-09 | Export + Auswertung, ohne `arbeitsstunden.lesen` | `arbeitsstunden_uebersicht` | 403 | keine Stunden-/Personendaten |
| FR211-10 | alle benötigten Rechte | Arbeitsstundenübersicht mit Jahr/Ansicht | Erfolg | Datensatz-IDs, Stunden, Jahr, Berechnungen |
| FR211-11 | Export + Auswertung, ohne `zaehler.lesen` | `jahresablesung_status` | 403 | keine Ablesedaten |
| FR211-12 | benötigte Rechte | `jahresablesung_status` mit Jahr | Erfolg | DB-Ablesungen und Status |
| FR211-13 | Export + Auswertung, ohne `mitglied.lesen` | `zaehler_eichfaelligkeit` | 403 | keine personenbezogenen Spalten |
| FR211-14 | alle benötigten Rechte | `zaehler_eichfaelligkeit` | Erfolg | Eichdatum, Zähler-ID, Zuordnung |
| FR211-15 | Vollberechtigt | veränderte Export-ID | 400/403 | keine unerwarteten Daten |
| FR211-16 | Vollberechtigt | unbekannter Parameter / fremde Objekt-ID | 400/403 | keine ungefilterten Fremdobjekte |
| FR211-17 | Vollberechtigt | ungültige JSON-Typen, 8193-Byte-Parameter | 400 | keine Daten |
| FR211-18 | Vollberechtigt | 1001+ synthetische Zeilen (JAE: 5001+) | hartes Limit, kein Teilergebnis | alle Originaldaten unverändert |
| FR211-19 | Anmeldung ohne Exportrecht | direkte Quell-RPCs und `assert_export_access` | 403 bzw. EXECUTE verweigert | ACLs in `pg_proc` |
| FR211-20 | Vollberechtigt, ohne `serienbrief.erstellen` | serienbrieffähiger Export | tabellarische Zeilen ohne technische Empfänger-IDs | Schlüsselmengenvergleich |
| FR211-21 | Vollberechtigt inkl. `serienbrief.erstellen` | Eichfälligkeits-Export / Serienbrief | technisch notwendige Zuordnungen, gültige Empfänger | getrennte Serienbrief-ACL-Prüfung |
| FR211-22 | Vollberechtigt | PDF `kgv-generate-export-pdf` | gültiges PDF, freigegebene Spalten | Download, SHA-256, PDF-Inhalt/Seiten |
| FR211-23 | ohne Exportrecht | PDF `kgv-generate-export-pdf` | 403, kein PDF | kein Storageartefakt |
| FR211-24 | Vollberechtigt | CSV-Download | gültiges CSV, nur erlaubte Felder | Dateiinhalt/Zeilenanzahl gegen DB |
| FR211-25 | Vollberechtigt | JAE-XLSX-Download | gültige XLSX, nur legitime Datensätze | Tabellenblatt-/Formel-/Datenvergleich |
| FR211-26 | ohne `export.ausfuehren` | JAE-XLSX auch außerhalb UI | keine Daten herausgebbar | Backend-Prüfung `rpc_fr211_jae_export` |
| FR211-27 | zwei Benutzer mit abweichendem Objektscope | gleiche Exportdefinition | keine fremden Datensätze | unabhängiger Scope-Abgleich |
| FR211-28 | parallel und wiederholt | gleicher Export mit gleichen Parametern | keine Duplikate/Serverartefakte | gleiches Ergebnis, keine Mutationen |
| FR211-29 | beide Web-/MAUI-Kontexte | Fachrecht entzogen/gewährt | Navigation und Buttons korrekt | tatsächliche API-Antwort, nicht nur UI |
| FR211-30 | Vollberechtigt / nicht berechtigt | Statistiken jenseits Export-Centers | richtige fachliche Lesescopes | Web-/MAUI-View und DB-Werte |

Vor finaler Freigabe müssen Objektbezüge, alternative Statistiken und alle Client-/HTTP-Pfade geprüft sein. Bereits fachlich freigegebene operative Lesedaten können außerhalb des Export-Centers weiterhin lokal gespeichert werden; die Exportfunktion ist separat geschützt. Die Authentifizierten HTTP-E2E-Tests werden auf Nutzerentscheidung erst nach allen FR2-Paketen gemeinsam durchgeführt – bis dahin `offen`, niemals `bestanden`.
