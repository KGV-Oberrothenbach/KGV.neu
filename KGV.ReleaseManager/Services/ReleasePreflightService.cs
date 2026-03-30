using System.Diagnostics;
using System.IO;
using KGV.ReleaseManager.Models;

namespace KGV.ReleaseManager.Services;

public sealed class ReleasePreflightService
{
    private readonly GitCommandService _gitCommandService;
    private readonly ProcessExecutionService _processExecutionService;
    private readonly ReleaseArtifactService _releaseArtifactService;

    public ReleasePreflightService(
        GitCommandService gitCommandService,
        ProcessExecutionService processExecutionService,
        ReleaseArtifactService releaseArtifactService)
    {
        _gitCommandService = gitCommandService;
        _processExecutionService = processExecutionService;
        _releaseArtifactService = releaseArtifactService;
    }

    public async Task<ReleasePreflightResult> RunAsync(ReleaseExecutionRequest request, CancellationToken cancellationToken = default)
    {
        var checks = new List<ReleasePreflightCheckResult>();
        var sourceRepoExists = !string.IsNullOrWhiteSpace(request.SourceRepoPath) && Directory.Exists(request.SourceRepoPath);
        var buildAndroid = request.BuildApk || request.BuildAab;

        checks.Add(sourceRepoExists
            ? Ok("Quellrepo", $"Quellrepo gefunden: {request.SourceRepoPath}")
            : Error("Quellrepo", "Quellrepo-Pfad für KGV.neu fehlt oder ist lokal nicht erreichbar."));

        var gitExecutableCheck = await RunProcessCheckAsync(
            "Git-Executable",
            _gitCommandService.CreateVersionCommand(),
            result => $"Git ist aufrufbar: {FirstNonEmpty(result.StandardOutput, result.StandardError)}",
            _ => "Git-Executable nicht gefunden oder nicht aufrufbar.",
            cancellationToken);
        checks.Add(gitExecutableCheck);

        checks.Add(await CheckGitRepositoryAsync(
            "Quellrepo Git",
            request.SourceRepoPath,
            sourceRepoExists,
            cancellationToken));

        checks.Add(CheckReadableFiles(
            "Projekt-/Versionsdateien",
            BuildRequiredFileList(request, sourceRepoExists),
            sourceRepoExists,
            "Erforderliche Projekt- und Versionsdateien sind lesbar."));

        checks.Add(CheckWritableDirectory(
            "Release-Ausgabeordner",
            request.ReleaseOutputRootPath,
            "Release-Ausgabeordner ist beschreibbar."));

        if (request.BuildWpf)
        {
            var wpfTargetExists = !string.IsNullOrWhiteSpace(request.WpfTargetRepoPath) && Directory.Exists(request.WpfTargetRepoPath);
            checks.Add(wpfTargetExists
                ? Ok("WPF-Zielrepo", $"WPF-Zielrepo gefunden: {request.WpfTargetRepoPath}")
                : Error("WPF-Zielrepo", "Zielrepo für WPF-Veröffentlichung fehlt oder ist lokal nicht erreichbar."));

            checks.Add(await CheckGitRepositoryAsync(
                "WPF-Zielrepo Git",
                request.WpfTargetRepoPath,
                wpfTargetExists,
                cancellationToken));

            var innoScript = _releaseArtifactService.FindInnoSetupScript(request.SourceRepoPath);
            checks.Add(string.IsNullOrWhiteSpace(innoScript.ScriptPath)
                ? Error("WPF-Setup-Skript", innoScript.Message)
                : CheckReadableFiles(
                    "WPF-Setup-Skript",
                    new[] { innoScript.ScriptPath },
                    true,
                    $"WPF-Setup-Skript ist lesbar: {innoScript.ScriptPath}"));

            checks.Add(await CheckInnoSetupAsync(request.InnoSetupCompilerPath, cancellationToken));
        }

        if (buildAndroid)
        {
            var mauiProjectPath = Path.Combine(request.SourceRepoPath ?? string.Empty, "KGV.Maui", "KGV.Maui.csproj");
            checks.Add(File.Exists(mauiProjectPath)
                ? Ok("Android-Projekt", $"Android-Projekt gefunden: {mauiProjectPath}")
                : Error("Android-Projekt", "Android-Projektpfad bzw. `KGV.Maui.csproj` wurde nicht gefunden."));

            if (request.BuildApk)
            {
                checks.Add(CheckCreatableDirectory(
                    "APK-Ausgabepfad",
                    request.ApkOutputPath,
                    "APK-Ausgabepfad ist vorhanden oder erstellbar."));
            }

            if (request.BuildAab)
            {
                checks.Add(CheckCreatableDirectory(
                    "AAB-Ausgabepfad",
                    request.AabOutputPath,
                    "AAB-Ausgabepfad ist vorhanden oder erstellbar."));
            }

            checks.Add(File.Exists(request.AndroidKeystorePath)
                ? Ok("Android-Keystore", $"Keystore-Datei gefunden: {request.AndroidKeystorePath}")
                : Error("Android-Keystore", "Keystore-Datei fehlt."));

            checks.Add(string.IsNullOrWhiteSpace(request.AndroidKeystoreAlias)
                ? Error("Android-Keystore-Alias", "Keystore-Alias fehlt.")
                : Ok("Android-Keystore-Alias", $"Keystore-Alias gesetzt: {request.AndroidKeystoreAlias}"));
        }

        return new ReleasePreflightResult
        {
            Checks = checks
        };
    }

    private async Task<ReleasePreflightCheckResult> CheckGitRepositoryAsync(
        string checkName,
        string repositoryPath,
        bool repositoryPathExists,
        CancellationToken cancellationToken)
    {
        if (!repositoryPathExists)
        {
            return Error(checkName, "Git-Repository nicht prüfbar, weil der Pfad fehlt.");
        }

        var revParse = await _processExecutionService.RunAsync(
            _gitCommandService.CreateRevParseWorkTreeCommand(repositoryPath),
            checkName,
            cancellationToken);
        if (!revParse.Success || !string.Equals(revParse.StandardOutput.Trim(), "true", StringComparison.OrdinalIgnoreCase))
        {
            return Error(checkName, "Git-Repo nicht initialisiert oder lokal nicht erreichbar.");
        }

        var remoteOrigin = await _processExecutionService.RunAsync(
            _gitCommandService.CreateRemoteOriginCommand(repositoryPath),
            checkName,
            cancellationToken);
        if (!remoteOrigin.Success || string.IsNullOrWhiteSpace(remoteOrigin.StandardOutput))
        {
            return Error(checkName, "Git-Repo ist lokal vorhanden, aber `origin` ist nicht lesbar konfiguriert.");
        }

        return Ok(checkName, $"Git-Repo ist initialisiert und `origin` ist erreichbar: {remoteOrigin.StandardOutput.Trim()}");
    }

    private async Task<ReleasePreflightCheckResult> CheckInnoSetupAsync(string compilerPath, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(compilerPath) || !File.Exists(compilerPath))
        {
            return Error("Inno Setup", "Inno Setup nicht gefunden.");
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = compilerPath,
            Arguments = "/?",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            WorkingDirectory = Path.GetDirectoryName(compilerPath) ?? string.Empty
        };

        var result = await _processExecutionService.RunAsync(startInfo, "Inno Setup", cancellationToken);
        if (!string.IsNullOrWhiteSpace(result.ExceptionMessage))
        {
            return Error("Inno Setup", "Inno Setup nicht gefunden oder nicht aufrufbar.");
        }

        var processOutput = FirstNonEmpty(result.StandardOutput, result.StandardError);
        if (result.ExitCode is 0 or 1 && processOutput.Contains("Inno Setup", StringComparison.OrdinalIgnoreCase))
        {
            return Ok("Inno Setup", $"Inno Setup ist aufrufbar: {compilerPath}");
        }

        return Error("Inno Setup", "Inno Setup nicht gefunden oder nicht aufrufbar.");
    }

    private async Task<ReleasePreflightCheckResult> RunProcessCheckAsync(
        string checkName,
        ProcessStartInfo startInfo,
        Func<ProcessExecutionResult, string> successMessageFactory,
        Func<ProcessExecutionResult, string> failureMessageFactory,
        CancellationToken cancellationToken)
    {
        var result = await _processExecutionService.RunAsync(startInfo, checkName, cancellationToken);
        return result.Success
            ? Ok(checkName, successMessageFactory(result))
            : Error(checkName, failureMessageFactory(result));
    }

    private List<string> BuildRequiredFileList(ReleaseExecutionRequest request, bool sourceRepoExists)
    {
        if (!sourceRepoExists)
        {
            return [];
        }

        var files = new List<string>
        {
            Path.Combine(request.SourceRepoPath, "KGV.slnx"),
            Path.Combine(request.SourceRepoPath, "KGV.Wpf", "KGV.Wpf.csproj"),
            Path.Combine(request.SourceRepoPath, "KGV.Maui", "KGV.Maui.csproj")
        };

        return files;
    }

    private static ReleasePreflightCheckResult CheckReadableFiles(
        string checkName,
        IReadOnlyCollection<string> filePaths,
        bool sourceRepoExists,
        string successMessage)
    {
        if (!sourceRepoExists)
        {
            return Error(checkName, "Projekt- und Versionsdateien nicht prüfbar, weil das Quellrepo fehlt.");
        }

        if (filePaths.Count == 0)
        {
            return Error(checkName, "Erforderliche Projekt- und Versionsdateien wurden nicht gefunden.");
        }

        foreach (var filePath in filePaths.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (!File.Exists(filePath))
            {
                return Error(checkName, $"Erforderliche Datei fehlt: {filePath}");
            }

            try
            {
                using var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            }
            catch (Exception ex)
            {
                return Error(checkName, $"Erforderliche Datei ist nicht lesbar: {filePath} ({ex.Message})");
            }
        }

        return Ok(checkName, successMessage);
    }

    private static ReleasePreflightCheckResult CheckCreatableDirectory(string checkName, string path, string successMessage)
        => CheckDirectory(checkName, path, successMessage, writeProbe: false);

    private static ReleasePreflightCheckResult CheckWritableDirectory(string checkName, string path, string successMessage)
        => CheckDirectory(checkName, path, successMessage, writeProbe: true);

    private static ReleasePreflightCheckResult CheckDirectory(string checkName, string path, string successMessage, bool writeProbe)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return Error(checkName, $"{checkName} fehlt.");
        }

        var existedBefore = Directory.Exists(path);

        try
        {
            Directory.CreateDirectory(path);

            if (writeProbe)
            {
                var probeFilePath = Path.Combine(path, $".kgv-preflight-{Guid.NewGuid():N}.tmp");
                File.WriteAllText(probeFilePath, "preflight");
                File.Delete(probeFilePath);
            }

            return Ok(checkName, successMessage);
        }
        catch (Exception ex)
        {
            return Error(checkName, $"{checkName} nicht nutzbar: {ex.Message}");
        }
        finally
        {
            try
            {
                if (!existedBefore && Directory.Exists(path) && !Directory.EnumerateFileSystemEntries(path).Any())
                {
                    Directory.Delete(path);
                }
            }
            catch
            {
            }
        }
    }

    private static string FirstNonEmpty(params string[] values)
        => values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim() ?? string.Empty;

    private static ReleasePreflightCheckResult Ok(string name, string message)
        => new()
        {
            State = ReleasePreflightCheckState.Ok,
            Name = name,
            Message = message
        };

    private static ReleasePreflightCheckResult Error(string name, string message)
        => new()
        {
            State = ReleasePreflightCheckState.Error,
            Name = name,
            Message = message
        };
}
