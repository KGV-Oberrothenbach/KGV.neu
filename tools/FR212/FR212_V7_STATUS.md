# FR2-12 V7 – lokale Vorbereitung, keine Freigabe

Basis: main dd394206ee6d14cd648159a2e91d4693ffa66210, V6-Nachweis (37 Dateien), DemoDB 291, VDB 255 Migrationen. Regeln: Documentation/KGV_TECHNISCHE_ARBEITSREGELN.md.

Session-Ergänzung: SQL-MD5 5672503ba3a2c9dde9dfaedeb7dee5d7, isolierter transaktionaler Demo-Test PASS einschließlich fremder, ungültiger, fehlender, abgelaufener und widerrufener Session, Kontosperre, Eigenzugriff, Signatur-RPC. Rollback unabhängig bestätigt, 42 Sessions unverändert, keine Fixtures. Erst den V6-Entwurf, danach diese Ergänzung in **derselben** geprüften Migration ausführen (noch nicht erzeugen/installieren). Vollständige kombinierte Prüfung mit aktualisierten Session-Fixtures steht aus. Alte SQL-Testfälle mit nur `sub` müssen echte synthetische Session-Zuordnungen im Rollback erhalten. Kein Abbau von Sicherheitsgrenzen.

OTP: gleiche Antwort für gültig formatierte Anfragen unabhängig vom Konto- oder Versandstatus; tatsächliche Zustellung nicht behaupten. Timing-Seitenkanäle, direkter Auth-Recovery-Endpunkt, Rate-Limits und tatsächlicher OTP-Versand bleiben separat zu prüfen. Keine echte Mail ausgelöst. Gemeinsamer AuthService geändert, keine WPF-Dateien geändert oder gebaut.

MAUI: gemeinsamer Kopf für beide Shells, aktuelle Auth-Identität statt ausgewähltem Mitglied, Zweizeilig: KGV Oberrothenbach oder Demo DB, darunter eigener Anzeigename oder verifizierte Auth-Mailadresse. Keine Projekt-ID, Rollenbezeichnung oder Schlüssel. Vorhandene individuelle TitleViews bleiben erhalten; Flyout zeigt den Kontext auch dort. Layout, Lesbarkeit und Navigation auf echtem Android-Gerät noch offen. Vereinswechsel und Logout im UI-E2E prüfen.

Keine Migration, Deployment, Commit, Push oder Produktivänderung. Authentifizierte HTTP-E2E und UI-Abnahme zurückgestellt. Die vollständige Sicherheitsfreigabe bleibt offen.

Vorabprüfung: 14/14 OTP-Verhaltenstests mit dem per Git-Blob b52b0f7b1b6adf971608416621db66b90bfad6d2 verifizierten Original und den V7-Ersetzungen. Node/TypeScript-Transpilation, simulierte Auth-/DB-Aufrufe; kein HTTP-E2E, kein Mailversand. SQL-MD5 wie oben bestätigt. Keine PowerShell- oder .NET-/MAUI-Laufzeit in der Erstellungsumgebung; lokale Prüfung erforderlich.
