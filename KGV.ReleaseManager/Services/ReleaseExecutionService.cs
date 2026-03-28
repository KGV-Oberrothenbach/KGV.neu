using System.IO;
using KGV.ReleaseManager.Models;

namespace KGV.ReleaseManager.Services;

public sealed class ReleaseExecutionService
{
    private readonly ReleaseFolderService _releaseFolderService;
    private readonly BuildCommandService _buildCommandService;
    private readonly ProcessExecutionService _processExecutionService;
    private readonly ReleaseVersionFileService _releaseVersionFileService;
    private readonly ReleaseArtifactService _releaseArtifactService;

    public ReleaseExecutionService(
        ReleaseFolderService releaseFolderService,
        BuildCommandService buildCommandService,
        ProcessExecutionService processExecutionService,
        ReleaseVersionFileService releaseVersionFileService,
        ReleaseArtifactService releaseArtifactService)
    {
        _releaseFolderService = releaseFolderService;
        _buildCommandService = buildCommandService;
        _processExecutionService = processExecutionService;
        _releaseVersionFileService = releaseVersionFileService;
        _releaseArtifactService = releaseArtifactService;
    }

    public Task<ReleaseExecutionResult> ValidateAsync(ReleaseExecutionRequest request)
    {
        var messages = new List<string>();
        var errors = ValidateRequest(request, messages);
        var result = new ReleaseExecutionResult
        {
            Success = errors.Count == 0,
            Message = errors.Count == 0
                ? "Dry Run erfolgreich. Der Release kann mit den aktuellen Einstellungen gestartet werden, sofern externe Tools verfügbar sind."
                : errors[0],
            Messages = messages.Concat(errors).ToList(),
            ReleaseFolderPath = BuildReleaseFolderPath(request)
        };

        return Task.FromResult(result);
    }

    public async Task<ReleaseExecutionResult> ExecuteAsync(ReleaseExecutionRequest request, CancellationToken cancellationToken = default)
    {
        var messages = new List<string>();
        var artifacts = new List<string>();
        var errors = ValidateRequest(request, messages);
        if (errors.Count > 0)
        {
            return new ReleaseExecutionResult
            {
                Message = errors[0],
                Messages = messages.Concat(errors).ToList(),
                ReleaseFolderPath = BuildReleaseFolderPath(request)
            };
        }

        var folderResult = _releaseFolderService.PrepareVersionFolder(request.ReleaseOutputRootPath, request.TargetVersion);
        messages.Add(folderResult.Message);
        if (!folderResult.Success)
        {
            return new ReleaseExecutionResult
            {
                Message = folderResult.Message,
                Messages = messages,
                ReleaseFolderPath = folderResult.VersionFolderPath
            };
        }

        var backups = new List<VersionFileBackup>();
        var releaseFolderPath = folderResult.VersionFolderPath;
        var stagingRoot = Path.Combine(Path.GetTempPath(), "KGV.ReleaseManager", request.TargetVersion, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(stagingRoot);

        try
        {
            var versionWriteResult = _releaseVersionFileService.WriteTargetVersion(request.SourceRepoPath, request.TargetVersion);
            messages.Add(versionWriteResult.Message);
            if (!versionWriteResult.Success)
            {
                return new ReleaseExecutionResult
                {
                    Message = versionWriteResult.Message,
                    Messages = messages,
                    ReleaseFolderPath = releaseFolderPath
                };
            }

            backups.AddRange(versionWriteResult.Backups);

            if (request.BuildWpf)
            {
                var wpfProjectPath = Path.Combine(request.SourceRepoPath, "KGV.Wpf", "KGV.Wpf.csproj");
                var wpfTargetDirectory = _releaseArtifactService.ResolveWpfTargetDirectory(request.WpfTargetRepoPath);
                var wpfBuild = await _processExecutionService.RunAsync(
                    _buildCommandService.CreateDotnetBuildCommand(wpfProjectPath, "Release"),
                    "WPF Build",
                    cancellationToken);
                messages.Add(wpfBuild.GetUserFacingMessage());
                if (!wpfBuild.Success)
                {
                    return RollbackFailure("WPF Build fehlgeschlagen.", backups, messages, releaseFolderPath);
                }

                var innoScript = _releaseArtifactService.FindInnoSetupScript(request.SourceRepoPath);
                var wpfStaging = Path.Combine(stagingRoot, "WPF");
                Directory.CreateDirectory(wpfStaging);
                var setupBaseName = $"KGV-Setup-{request.TargetVersion}";
                var innoRun = await _processExecutionService.RunAsync(
                    _buildCommandService.CreateInnoCompileCommand(
                        request.InnoSetupCompilerPath,
                        innoScript.ScriptPath,
                        wpfStaging,
                        setupBaseName,
                        request.TargetVersion),
                    "WPF Setup",
                    cancellationToken);
                messages.Add(innoRun.GetUserFacingMessage());
                if (!innoRun.Success)
                {
                    return RollbackFailure("WPF-Setup-Erzeugung fehlgeschlagen.", backups, messages, releaseFolderPath);
                }

                var setupArtifact = _releaseArtifactService.FindNewestArtifact(wpfStaging, "*.exe", DateTime.UtcNow.AddMinutes(-10));
                if (string.IsNullOrWhiteSpace(setupArtifact))
                {
                    return RollbackFailure("Die erzeugte WPF-Setup-Datei wurde nicht gefunden.", backups, messages, releaseFolderPath);
                }

                var releaseSetupPath = _releaseArtifactService.CopyArtifact(setupArtifact, Path.Combine(releaseFolderPath, "WPF"));
                artifacts.Add(releaseSetupPath);
                messages.Add($"WPF-Setup in den Versionsordner kopiert: {releaseSetupPath}");

                var wpfRepoSetupPath = _releaseArtifactService.CopyArtifact(setupArtifact, wpfTargetDirectory.TargetDirectory);
                artifacts.Add(wpfRepoSetupPath);
                messages.Add($"WPF-Setup in das lokale Zielrepo kopiert: {wpfRepoSetupPath}");

                var stableSetupPath = Path.Combine(wpfTargetDirectory.TargetDirectory, "KGV-Setup.exe");
                if (File.Exists(stableSetupPath))
                {
                    var latestSetupPath = _releaseArtifactService.CopyArtifact(setupArtifact, wpfTargetDirectory.TargetDirectory, "KGV-Setup.exe");
                    artifacts.Add(latestSetupPath);
                    messages.Add($"WPF-Setup als aktuelle Setup-Datei aktualisiert: {latestSetupPath}");
                }
            }

            if (request.BuildApk)
            {
                var apkArtifact = await BuildAndroidArtifactAsync(request, "apk", cancellationToken, messages);
                if (string.IsNullOrWhiteSpace(apkArtifact))
                {
                    return RollbackFailure("APK-Erzeugung fehlgeschlagen.", backups, messages, releaseFolderPath);
                }

                var releaseApkPath = _releaseArtifactService.CopyArtifact(apkArtifact, Path.Combine(releaseFolderPath, "Android", "APK"));
                artifacts.Add(releaseApkPath);
                messages.Add($"APK in den Versionsordner kopiert: {releaseApkPath}");

                var apkOutputPath = _releaseArtifactService.CopyArtifact(apkArtifact, request.ApkOutputPath);
                artifacts.Add(apkOutputPath);
                messages.Add($"APK in den konfigurierten Ausgabeordner kopiert: {apkOutputPath}");
            }

            if (request.BuildAab)
            {
                var aabArtifact = await BuildAndroidArtifactAsync(request, "aab", cancellationToken, messages);
                if (string.IsNullOrWhiteSpace(aabArtifact))
                {
                    return RollbackFailure("AAB-Erzeugung fehlgeschlagen.", backups, messages, releaseFolderPath);
                }

                var releaseAabPath = _releaseArtifactService.CopyArtifact(aabArtifact, Path.Combine(releaseFolderPath, "Android", "AAB"));
                artifacts.Add(releaseAabPath);
                messages.Add($"AAB in den Versionsordner kopiert: {releaseAabPath}");

                var aabOutputPath = _releaseArtifactService.CopyArtifact(aabArtifact, request.AabOutputPath);
                artifacts.Add(aabOutputPath);
                messages.Add($"AAB in den konfigurierten Ausgabeordner kopiert: {aabOutputPath}");
            }

            messages.Add("Release erfolgreich abgeschlossen.");
            return new ReleaseExecutionResult
            {
                Success = true,
                Message = "Release erfolgreich abgeschlossen.",
                Messages = messages,
                ArtifactPaths = artifacts,
                ReleaseFolderPath = releaseFolderPath
            };
        }
        catch (Exception ex)
        {
            var failureMessages = messages.Append($"Release mit Ausnahme fehlgeschlagen: {ex.Message}").ToList();
            return RollbackFailure("Release fehlgeschlagen.", backups, failureMessages, releaseFolderPath);
        }
        finally
        {
            TryDeleteDirectory(stagingRoot);
        }
    }

    private async Task<string> BuildAndroidArtifactAsync(
        ReleaseExecutionRequest request,
        string packageFormat,
        CancellationToken cancellationToken,
        List<string> messages)
    {
        var mauiProjectPath = Path.Combine(request.SourceRepoPath, "KGV.Maui", "KGV.Maui.csproj");
        var buildStartedUtc = DateTime.UtcNow;
        var androidBuild = await _processExecutionService.RunAsync(
            _buildCommandService.CreateAndroidPublishCommand(
                mauiProjectPath,
                packageFormat,
                request.AndroidKeystorePath,
                request.AndroidKeystoreAlias,
                request.AndroidStorePassword,
                request.AndroidKeyPassword),
            $"Android {packageFormat.ToUpperInvariant()} Build",
            cancellationToken);

        messages.Add(androidBuild.GetUserFacingMessage());
        if (!androidBuild.Success)
        {
            return string.Empty;
        }

        var extension = $"*.{packageFormat}";
        var searchRoot = Path.Combine(request.SourceRepoPath, "KGV.Maui", "bin", "Release");
        var artifact = _releaseArtifactService.FindNewestArtifact(searchRoot, extension, buildStartedUtc.AddMinutes(-1));
        if (string.IsNullOrWhiteSpace(artifact))
        {
            messages.Add($"{packageFormat.ToUpperInvariant()}-Artefakt wurde nach dem Build nicht gefunden.");
            return string.Empty;
        }

        return artifact;
    }

    private List<string> ValidateRequest(ReleaseExecutionRequest request, List<string> messages)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(request.SourceRepoPath) || !Directory.Exists(request.SourceRepoPath))
        {
            errors.Add("Der konfigurierte Quellpfad wurde nicht gefunden.");
            return errors;
        }

        if (string.IsNullOrWhiteSpace(request.TargetVersion))
        {
            errors.Add("Es ist keine Zielversion vorhanden.");
        }

        if (!request.BuildWpf && !request.BuildApk && !request.BuildAab)
        {
            errors.Add("Es ist kein Releaseziel ausgewählt.");
        }

        if (string.IsNullOrWhiteSpace(request.ReleaseOutputRootPath))
        {
            errors.Add("Der Basisordner für Veröffentlichungen fehlt.");
        }

        if (request.BuildWpf)
        {
            var wpfProjectPath = Path.Combine(request.SourceRepoPath, "KGV.Wpf", "KGV.Wpf.csproj");
            if (!File.Exists(wpfProjectPath))
            {
                errors.Add("Das WPF-Projekt `KGV.Wpf.csproj` wurde nicht gefunden.");
            }

            if (string.IsNullOrWhiteSpace(request.InnoSetupCompilerPath) || !File.Exists(request.InnoSetupCompilerPath))
            {
                errors.Add("Für WPF-Setups fehlt ein gültiger Pfad zu `ISCC.exe`.");
            }

            if (string.IsNullOrWhiteSpace(request.WpfTargetRepoPath) || !Directory.Exists(request.WpfTargetRepoPath))
            {
                errors.Add("Das lokale Zielrepo für WPF-Setups wurde nicht gefunden.");
            }

            var innoScript = _releaseArtifactService.FindInnoSetupScript(request.SourceRepoPath);
            messages.Add(innoScript.Message);
            if (string.IsNullOrWhiteSpace(innoScript.ScriptPath))
            {
                errors.Add(innoScript.Message);
            }

            var wpfTargetDirectory = _releaseArtifactService.ResolveWpfTargetDirectory(request.WpfTargetRepoPath);
            messages.Add(wpfTargetDirectory.Message);
            if (string.IsNullOrWhiteSpace(wpfTargetDirectory.TargetDirectory))
            {
                errors.Add(wpfTargetDirectory.Message);
            }
        }

        if (request.BuildApk || request.BuildAab)
        {
            var mauiProjectPath = Path.Combine(request.SourceRepoPath, "KGV.Maui", "KGV.Maui.csproj");
            if (!File.Exists(mauiProjectPath))
            {
                errors.Add("Das MAUI-Projekt `KGV.Maui.csproj` wurde nicht gefunden.");
            }

            if (string.IsNullOrWhiteSpace(request.AndroidKeystorePath) || !File.Exists(request.AndroidKeystorePath))
            {
                errors.Add("Für Android-Builds fehlt ein gültiger Keystore-Pfad.");
            }

            if (string.IsNullOrWhiteSpace(request.AndroidKeystoreAlias))
            {
                errors.Add("Für Android-Builds fehlt der Keystore-Alias.");
            }

            if (string.IsNullOrWhiteSpace(request.AndroidStorePassword))
            {
                errors.Add("Für Android-Builds fehlt das Keystore-Passwort zur Laufzeit.");
            }

            if (request.BuildApk && string.IsNullOrWhiteSpace(request.ApkOutputPath))
            {
                errors.Add("Für APK-Builds fehlt der Ausgabeordner für APK.");
            }

            if (request.BuildAab && string.IsNullOrWhiteSpace(request.AabOutputPath))
            {
                errors.Add("Für AAB-Builds fehlt der Ausgabeordner für AAB.");
            }
        }

        messages.Add($"Geplanter Veröffentlichungsordner: {BuildReleaseFolderPath(request)}");
        return errors;
    }

    private static string BuildReleaseFolderPath(ReleaseExecutionRequest request)
    {
        return string.IsNullOrWhiteSpace(request.ReleaseOutputRootPath) || string.IsNullOrWhiteSpace(request.TargetVersion)
            ? string.Empty
            : Path.Combine(request.ReleaseOutputRootPath, request.TargetVersion);
    }

    private ReleaseExecutionResult RollbackFailure(string message, List<VersionFileBackup> backups, List<string> messages, string releaseFolderPath)
    {
        var rolledBack = false;

        if (backups.Count > 0)
        {
            _releaseVersionFileService.RestoreBackups(backups);
            messages.Add("Versionsänderungen wurden nach dem Fehler zurückgesetzt.");
            rolledBack = true;
        }

        return new ReleaseExecutionResult
        {
            Success = false,
            RolledBack = rolledBack,
            Message = message,
            Messages = messages,
            ReleaseFolderPath = releaseFolderPath
        };
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(path) && Directory.Exists(path))
            {
                Directory.Delete(path, true);
            }
        }
        catch
        {
        }
    }
}
