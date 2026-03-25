# Lokale Artefakt-/Archivkandidaten

Stand: 2026-03-25

Diese Liste fasst lokale/blockfremde Artefakte zusammen, die im aktuellen Repo-Stand **nicht** als aktive Produktivstruktur eingeordnet wurden.

## Bereits in den Archivbereich verschoben
- `_Archiv/KGV.Maui/Pages/FotoUploadTestPage.cs`
- `_Archiv/KGV.Maui/ViewModels/FotoUploadTestViewModel.cs`
- `_Archiv/supabase/kgv-upload-photo/index.ts`

Begründung:
- Die WPF-Variante von `FotoUploadTest` ist aktiv verdrahtet und bleibt Teil der aktiven Basis.
- Die MAUI-Variante war im aktuellen Repo-Stand nicht über Shell/Route/Produktivseite angebunden und wurde deshalb nicht als aktive Zielstruktur eingeordnet.
- `supabase/kgv-upload-photo/index.ts` war nur noch ein älterer Nebenpfad neben dem aktiven Deploypfad `supabase/functions/kgv-upload-photo/index.ts`.

## Lokale Artefakte, die **nicht** ins aktive Repo übernommen wurden
### Lokale IDE-/Hilfsdateien
- `.vscode/`

### Lokale Exporte / sensible Bereiche
- `_AI_DB_EXPORT/`
- `_secrets/`

### Lokale Build-/Release-Artefakte
- `KGV-Setup-0.2.6.exe`
- `de.kgv.oberrothenbach-Signed.apk`

### Lokale Dubletten / Hilfskopien
- `KGV.Maui/Resources/AppIcon/appicon.png`
- `appicon.svg`
- `database.types.ts`
- `package.json`
- `package-lock.json`

## Aktiv ins Repo übernommen statt archiviert
- `.github/copilot-instructions.md`
- `KGV.Core/Interfaces/IPhotoUploadTestService.cs`
- `KGV.Core/Models/PhotoUploadTestRequest.cs`
- `KGV.Core/Models/PhotoUploadTestResult.cs`
- `KGV.Infrastructure/Services/PhotoUploadTestService.cs`
- `KGV.Wpf/ViewModels/FotoUploadTestViewModel.cs`
- `KGV.Wpf/Views/FotoUploadTestView.xaml.cs`
- zugehörige aktive WPF-/Core-/Infrastructure-Verdrahtung
- `supabase/.gitignore`
- `supabase/functions/kgv-upload-photo/deno.json`
- `supabase/functions/kgv-upload-photo/.npmrc`
