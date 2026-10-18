# Passkeys / WebAuthn

Die Web-Anmeldung verwendet Supabase Auth Passkeys. Die App verarbeitet keine biometrischen Daten, Passwörter oder WebAuthn-Credentials selbst; Browser und Betriebssystem (zum Beispiel Safari mit Face ID/Touch ID oder Windows Hello) führen die lokale Freigabe aus.

## Festgelegte RP-ID

Die RP-ID ist dauerhaft **`a-b-i.net`**. Dadurch sind beide vorgesehenen HTTPS-Origins zulässig, ohne die RP-ID bei einem Wechsel zwischen Produktion und Test zu ändern:

- `https://app.a-b-i.net`
- `https://test-app.a-b-i.net`

Die RP-ID darf weder ein Schema, einen Pfad noch einen Port enthalten. Eine spätere Änderung macht bereits registrierte Passkeys für diese Anwendung unbrauchbar.

## Supabase-Konfiguration

In **jedem** verwendeten Supabase-Projekt (KGV Demo und VereinsDB separat) unter **Authentication → Passkeys** aktivieren:

- RP-ID: `a-b-i.net`
- RP display name: `KGV`
- Origins: die beiden Origins oben, kommasepariert

Die Web-App erzeugt den Supabase-Client ausschließlich aus dem vor der Anmeldung ausgewählten Verein. Damit wird ein Passkey immer beim zugehörigen Supabase-Projekt geprüft; eine Demo-Anmeldung kann keine VereinsDB-Sitzung erzeugen und umgekehrt. Nach Passkey-Anmeldung erfolgt dieselbe serverseitige Benutzer-, Rollen- und Berechtigungsprüfung wie nach Passwort-Anmeldung.
