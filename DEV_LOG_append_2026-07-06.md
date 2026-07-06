# DEV_LOG Ergänzung – 2026-07-06

MAUI: Mitgliedsantrag – UI-Buttons angepasst (Download / Löschen & Neu)

- Ziel: Stabileren Umgang mit lokal persistierten Mitgliedsanträgen auf mobilen Geräten ermöglichen. Der bisherige "Mitgliedsantrag Signatur"-Button wurde entfernt; stattdessen wurden zwei neue Aktionen implementiert:
  - Mitgliedsantrag Download: öffnet den Share-Chooser (whatsapp / Druck-App / andere Apps). Fallback: Launcher.OpenAsync (System-Viewer).
  - Mitgliedsantrag Löschen und Neu: Bestätigung, Löschen der lokalen Datei (Mitgliedsantrag_{id}.pdf) und Start des Erstellungs-Flows via CreateMitgliedsantragAsync.

- Geänderte Datei: KGV.Maui/Pages/MemberDetailPage.cs
- Build: `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich (lokal).
- Commit: feat(mitgliedsantrag): add download and delete-and-new buttons; remove signatur button (branch feature/persistent-pdf-viewer) — bereits gepusht.

Hinweis:
- Share-Chooser ist primärer Weg; Fallback verwendet Launcher. Auf Android ist ein Laufzeittest erforderlich, da FileProvider/URI-Policies das direkte Öffnen beeinflussen können.
- Verhalten nach Löschen: aktueller Flow startet CreateMitgliedsantragAsync automatisch; falls ein zusätzlicher Confirm vor Restart gewünscht ist, bitte angeben.
