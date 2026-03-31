using System.IO;
using KGV.ReleaseManager.Models;

namespace KGV.ReleaseManager.Services;

public sealed class ReleaseExecutionService
{
    private const string StepReadOriginalVersions = "Ausgangsversionen lesen";
    private const string StepWriteVersions = "Versionen erhöhen/schreiben";
    private const string StepBuildWpfArtifacts = "WPF-Artefakte bauen";
    private const string StepBuildApk = "Android-APK bauen";
    private const string StepBuildAab = "Android-AAB bauen";
    private const string StepPublishArtifacts = "Veröffentlichungsordner befüllen";
    private const string StepWriteMarker = "Marker schreiben";
    private const string StepCommit = "Commit ausführen";
    private const string StepPush = "Push ausführen";
    private const string StepRollback = "Rollback";
    private const string StepCompletion = "Abschluss";

    private readonly ReleaseFolderService _releaseFolderService;
    private readonly BuildCommandService _buildCommandService;
    private readonly ProcessExecutionService _processExecutionService;
    private readonly ReleaseVersionFileService _releaseVersionFileService;
    private readonly ReleaseArtifactService _releaseArtifactService;
    private readonly GitCommandService _gitCommandService;
    private readonly ReleaseMarkerService _releaseMarkerService;
    private readonly VersionService _versionService;

    public ReleaseExecutionService(
        ReleaseFolderService releaseFolderService,
        BuildCommandService buildCommandService,
        ProcessExecutionService processExecutionService,
        ReleaseVersionFileService releaseVersionFileService,
        ReleaseArtifactService releaseArtifactService,
        GitCommandService gitCommandService,
        ReleaseMarkerService releaseMarkerService,
        VersionService versionService)
    {
        _releaseFolderService = releaseFolderService;
        _buildCommandService = buildCommandService;
        _processExecutionService = processExecutionService;
        _releaseVersionFileService = releaseVersionFileService;
        _releaseArtifactService = releaseArtifactService;
        _gitCommandService = gitCommandService;
        _releaseMarkerService = releaseMarkerService;
        _versionService = versionService;
    }

    public Task<ReleaseExecutionResult> ValidateAsync(ReleaseExecutionRequest request)
    {
        var messages = new List<string>
        {
            "Dry Run gestartet."
        };
        var stepTracker = CreateStepTracker();
        ApplySelectionSkips(stepTracker, request);
        var errors = ValidateRequest(request, messages);
        if (errors.Count > 0)
        {
            stepTracker.Fail(StepCompletion, $"Dry Run fehlgeschlagen. Grund: {errors[0]}");
            stepTracker.MarkRemainingPendingAsSkipped("Wegen vorherigem Fehler nicht ausgeführt.");
            var finalMessages = messages.Concat(errors).ToList();
            AppendFinalEvaluation(finalMessages, ReleaseExecutionOverallState.Failed, markerWritten: false, commitExecuted: false, pushExecuted: false);
            return Task.FromResult(new ReleaseExecutionResult
            {
                Success = false,
                OverallState = ReleaseExecutionOverallState.Failed,
                Message = errors[0],
                Messages = finalMessages,
                ReleaseFolderPath = BuildReleaseFolderPath(request),
                Steps = stepTracker.Build()
            });
        }

        LogStepStart(messages, StepReadOriginalVersions);
        var versionRead = TryReadOriginalVersions(request, messages);
        if (!versionRead.Success)
        {
            stepTracker.Fail(StepReadOriginalVersions, versionRead.Message);
            stepTracker.Fail(StepCompletion, $"Dry Run fehlgeschlagen. Grund: {versionRead.Message}");
            stepTracker.MarkRemainingPendingAsSkipped("Wegen vorherigem Fehler nicht ausgeführt.");
            AppendFinalEvaluation(messages, ReleaseExecutionOverallState.Failed, markerWritten: false, commitExecuted: false, pushExecuted: false);
            return Task.FromResult(new ReleaseExecutionResult
            {
                Success = false,
                OverallState = ReleaseExecutionOverallState.Failed,
                Message = versionRead.Message,
                Messages = messages,
                ReleaseFolderPath = BuildReleaseFolderPath(request),
                Steps = stepTracker.Build()
            });
        }

        stepTracker.Success(StepReadOriginalVersions, versionRead.Message);
        LogStepSuccess(messages, StepReadOriginalVersions, versionRead.Message);

        stepTracker.Skip(StepWriteVersions, "Dry Run: Versionsdateien wurden nicht geändert.");
        stepTracker.Skip(StepPublishArtifacts, "Dry Run: Veröffentlichungsordner wurde nicht befüllt.");
        stepTracker.Skip(StepWriteMarker, "Dry Run: kein Marker geschrieben.");
        stepTracker.Skip(StepCommit, "Dry Run: kein Commit ausgeführt.");
        stepTracker.Skip(StepPush, "Dry Run: kein Push ausgeführt.");
        stepTracker.Skip(StepRollback, "Dry Run: kein Rollback erforderlich.");
        stepTracker.Success(StepCompletion, "Dry Run erfolgreich abgeschlossen.");

        messages.Add("Dry Run erfolgreich. Es wurden keine Marker, Commits oder Pushes ausgeführt.");
        AppendFinalEvaluation(messages, ReleaseExecutionOverallState.Successful, markerWritten: false, commitExecuted: false, pushExecuted: false);

        return Task.FromResult(new ReleaseExecutionResult
        {
            Success = true,
            OverallState = ReleaseExecutionOverallState.Successful,
            Message = "Dry Run erfolgreich. Es wurden keine Marker, Commits oder Pushes ausgeführt.",
            Messages = messages,
            ReleaseFolderPath = BuildReleaseFolderPath(request),
            Steps = stepTracker.Build()
        });
    }

    public async Task<ReleaseExecutionResult> ExecuteAsync(ReleaseExecutionRequest request, CancellationToken cancellationToken = default)
    {
        var messages = new List<string>
        {
            "Echt-Release gestartet."
        };
        var artifacts = new List<string>();
        var stepTracker = CreateStepTracker();
        ApplySelectionSkips(stepTracker, request);
        var sourceRepositoryState = new GitRepositoryTransactionState(request.SourceRepoPath, "Quellrepo Git");
        var targetRepositoryState = request.BuildWpf
            ? new GitRepositoryTransactionState(request.WpfTargetRepoPath, "WPF-Zielrepo Git")
            : null;
        var backups = new List<VersionFileBackup>();
        var markerWritten = false;
        var releaseFolderPath = BuildReleaseFolderPath(request);
        var releaseFolderExistedBefore = true;
        var stagingRoot = Path.Combine(Path.GetTempPath(), "KGV.ReleaseManager", request.TargetVersion, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(stagingRoot);
        string? stagedWpfSetupArtifact = null;
        string? stagedApkArtifact = null;
        string? stagedAabArtifact = null;

        var errors = ValidateRequest(request, messages);
        if (errors.Count > 0)
        {
            stepTracker.Fail(StepCompletion, $"Release fehlgeschlagen. Grund: {errors[0]}");
            stepTracker.MarkRemainingPendingAsSkipped("Wegen vorherigem Fehler nicht ausgeführt.");
            var failureMessages = messages.Concat(errors).ToList();
            AppendFinalEvaluation(failureMessages, ReleaseExecutionOverallState.Failed, markerWritten: false, commitExecuted: false, pushExecuted: false);
            return new ReleaseExecutionResult
            {
                Success = false,
                OverallState = ReleaseExecutionOverallState.Failed,
                Message = errors[0],
                Messages = failureMessages,
                ReleaseFolderPath = releaseFolderPath,
                Steps = stepTracker.Build()
            };
        }

        var folderResult = _releaseFolderService.PrepareVersionFolder(request.ReleaseOutputRootPath, request.TargetVersion);
        messages.Add(folderResult.Message);
        releaseFolderPath = folderResult.VersionFolderPath;
        releaseFolderExistedBefore = folderResult.ExistedBefore;
        if (!folderResult.Success)
        {
            stepTracker.Fail(StepPublishArtifacts, folderResult.Message);
            stepTracker.Fail(StepCompletion, $"Release fehlgeschlagen. Grund: {folderResult.Message}");
            stepTracker.MarkRemainingPendingAsSkipped("Wegen vorherigem Fehler nicht ausgeführt.");
            AppendFinalEvaluation(messages, ReleaseExecutionOverallState.Failed, markerWritten: false, commitExecuted: false, pushExecuted: false);
            return new ReleaseExecutionResult
            {
                Success = false,
                OverallState = ReleaseExecutionOverallState.Failed,
                Message = folderResult.Message,
                Messages = messages,
                ReleaseFolderPath = releaseFolderPath,
                Steps = stepTracker.Build()
            };
        }

        try
        {
            LogStepStart(messages, StepReadOriginalVersions);
            var versionRead = TryReadOriginalVersions(request, messages);
            if (!versionRead.Success)
            {
                return await RollbackFailureAsync(
                    versionRead.Message,
                    StepReadOriginalVersions,
                    markerWritten,
                    stepTracker,
                    backups,
                    messages,
                    releaseFolderPath,
                    artifacts,
                    releaseFolderExistedBefore,
                    sourceRepositoryState,
                    targetRepositoryState,
                    cancellationToken);
            }

            stepTracker.Success(StepReadOriginalVersions, versionRead.Message);
            LogStepSuccess(messages, StepReadOriginalVersions, versionRead.Message);

            LogStepStart(messages, StepWriteVersions);
            var versionWriteResult = _releaseVersionFileService.WriteTargetVersion(
                request.SourceRepoPath,
                request.TargetVersion,
                request.BuildWpf,
                request.BuildApk || request.BuildAab);
            messages.Add(versionWriteResult.Message);
            backups.AddRange(versionWriteResult.Backups);
            if (!versionWriteResult.Success)
            {
                return await RollbackFailureAsync(
                    versionWriteResult.Message,
                    StepWriteVersions,
                    markerWritten,
                    stepTracker,
                    backups,
                    messages,
                    releaseFolderPath,
                    artifacts,
                    releaseFolderExistedBefore,
                    sourceRepositoryState,
                    targetRepositoryState,
                    cancellationToken);
            }

            stepTracker.Success(StepWriteVersions, versionWriteResult.Message);
            LogStepSuccess(messages, StepWriteVersions, versionWriteResult.Message);

            if (request.BuildWpf)
            {
                LogStepStart(messages, StepBuildWpfArtifacts);
                var wpfProjectPath = Path.Combine(request.SourceRepoPath, "KGV.Wpf", "KGV.Wpf.csproj");
                var wpfBuild = await _processExecutionService.RunAsync(
                    _buildCommandService.CreateDotnetBuildCommand(wpfProjectPath, "Release"),
                    "WPF Build",
                    cancellationToken);
                messages.Add(wpfBuild.GetUserFacingMessage());
                if (!wpfBuild.Success)
                {
                    return await RollbackFailureAsync(
                        "WPF Build fehlgeschlagen.",
                        StepBuildWpfArtifacts,
                        markerWritten,
                        stepTracker,
                        backups,
                        messages,
                        releaseFolderPath,
                        artifacts,
                        releaseFolderExistedBefore,
                        sourceRepositoryState,
                        targetRepositoryState,
                        cancellationToken);
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
                    return await RollbackFailureAsync(
                        "WPF-Setup-Erzeugung fehlgeschlagen.",
                        StepBuildWpfArtifacts,
                        markerWritten,
                        stepTracker,
                        backups,
                        messages,
                        releaseFolderPath,
                        artifacts,
                        releaseFolderExistedBefore,
                        sourceRepositoryState,
                        targetRepositoryState,
                        cancellationToken);
                }

                var setupArtifact = _releaseArtifactService.FindNewestArtifact(wpfStaging, "*.exe", DateTime.UtcNow.AddMinutes(-10));
                if (string.IsNullOrWhiteSpace(setupArtifact))
                {
                    return await RollbackFailureAsync(
                        "Die erzeugte WPF-Setup-Datei wurde nicht gefunden.",
                        StepBuildWpfArtifacts,
                        markerWritten,
                        stepTracker,
                        backups,
                        messages,
                        releaseFolderPath,
                        artifacts,
                        releaseFolderExistedBefore,
                        sourceRepositoryState,
                        targetRepositoryState,
                        cancellationToken);
                }

                stagedWpfSetupArtifact = setupArtifact;
                var wpfMessage = $"WPF-Artefakte vorbereitet: {stagedWpfSetupArtifact}";
                messages.Add(wpfMessage);
                stepTracker.Success(StepBuildWpfArtifacts, wpfMessage);
                LogStepSuccess(messages, StepBuildWpfArtifacts, wpfMessage);
            }

            if (request.BuildApk)
            {
                LogStepStart(messages, StepBuildApk);
                var apkArtifact = await BuildAndroidArtifactAsync(request, "apk", cancellationToken, messages);
                if (string.IsNullOrWhiteSpace(apkArtifact))
                {
                    return await RollbackFailureAsync(
                        "APK-Erzeugung fehlgeschlagen.",
                        StepBuildApk,
                        markerWritten,
                        stepTracker,
                        backups,
                        messages,
                        releaseFolderPath,
                        artifacts,
                        releaseFolderExistedBefore,
                        sourceRepositoryState,
                        targetRepositoryState,
                        cancellationToken);
                }

                stagedApkArtifact = apkArtifact;
                var apkMessage = $"APK vorbereitet: {stagedApkArtifact}";
                messages.Add(apkMessage);
                stepTracker.Success(StepBuildApk, apkMessage);
                LogStepSuccess(messages, StepBuildApk, apkMessage);
            }

            if (request.BuildAab)
            {
                LogStepStart(messages, StepBuildAab);
                var aabArtifact = await BuildAndroidArtifactAsync(request, "aab", cancellationToken, messages);
                if (string.IsNullOrWhiteSpace(aabArtifact))
                {
                    return await RollbackFailureAsync(
                        "AAB-Erzeugung fehlgeschlagen.",
                        StepBuildAab,
                        markerWritten,
                        stepTracker,
                        backups,
                        messages,
                        releaseFolderPath,
                        artifacts,
                        releaseFolderExistedBefore,
                        sourceRepositoryState,
                        targetRepositoryState,
                        cancellationToken);
                }

                stagedAabArtifact = aabArtifact;
                var aabMessage = $"AAB vorbereitet: {stagedAabArtifact}";
                messages.Add(aabMessage);
                stepTracker.Success(StepBuildAab, aabMessage);
                LogStepSuccess(messages, StepBuildAab, aabMessage);
            }

            LogStepStart(messages, StepPublishArtifacts);
            try
            {
                PublishArtifacts(
                    request,
                    releaseFolderPath,
                    stagedWpfSetupArtifact,
                    stagedApkArtifact,
                    stagedAabArtifact,
                    artifacts,
                    messages);
            }
            catch (Exception ex)
            {
                messages.Add($"Veröffentlichung der Artefakte fehlgeschlagen: {ex.Message}");
                return await RollbackFailureAsync(
                    "Veröffentlichung der Artefakte fehlgeschlagen.",
                    StepPublishArtifacts,
                    markerWritten,
                    stepTracker,
                    backups,
                    messages,
                    releaseFolderPath,
                    artifacts,
                    releaseFolderExistedBefore,
                    sourceRepositoryState,
                    targetRepositoryState,
                    cancellationToken);
            }

            stepTracker.Success(StepPublishArtifacts, "Veröffentlichungsordner und Zielpfade wurden befüllt.");
            LogStepSuccess(messages, StepPublishArtifacts, "Veröffentlichungsordner und Zielpfade wurden befüllt.");

            BackupFileIfNeeded(backups, _releaseMarkerService.ResolveProgressLogPath(request.SourceRepoPath));
            LogStepStart(messages, StepWriteMarker);
            var markerResult = _releaseMarkerService.AppendReleaseMarker(request.SourceRepoPath, request.TargetVersion);
            messages.Add(markerResult.Message);
            if (!markerResult.Success)
            {
                return await RollbackFailureAsync(
                    "Release-Marker konnte nicht geschrieben werden.",
                    StepWriteMarker,
                    markerWritten,
                    stepTracker,
                    backups,
                    messages,
                    releaseFolderPath,
                    artifacts,
                    releaseFolderExistedBefore,
                    sourceRepositoryState,
                    targetRepositoryState,
                    cancellationToken);
            }

            markerWritten = true;
            stepTracker.Success(StepWriteMarker, markerResult.Message);
            LogStepSuccess(messages, StepWriteMarker, markerResult.Message);

            LogStepStart(messages, StepCommit);
            var sourceHeadResult = await CaptureOriginalHeadAsync(sourceRepositoryState, messages, cancellationToken);
            if (!sourceHeadResult.Success)
            {
                return await RollbackFailureAsync(
                    sourceHeadResult.Message,
                    StepCommit,
                    markerWritten,
                    stepTracker,
                    backups,
                    messages,
                    releaseFolderPath,
                    artifacts,
                    releaseFolderExistedBefore,
                    sourceRepositoryState,
                    targetRepositoryState,
                    cancellationToken);
            }

            if (targetRepositoryState is not null)
            {
                var targetHeadResult = await CaptureOriginalHeadAsync(targetRepositoryState, messages, cancellationToken);
                if (!targetHeadResult.Success)
                {
                    return await RollbackFailureAsync(
                        targetHeadResult.Message,
                        StepCommit,
                        markerWritten,
                        stepTracker,
                        backups,
                        messages,
                        releaseFolderPath,
                        artifacts,
                        releaseFolderExistedBefore,
                        sourceRepositoryState,
                        targetRepositoryState,
                        cancellationToken);
                }
            }

            var sourceCommitResult = await CommitIfNeededAsync(
                sourceRepositoryState,
                _gitCommandService.CreateReleaseCommitMessage(request.TargetVersion, "source release state"),
                requireChanges: true,
                messages,
                cancellationToken);
            if (!sourceCommitResult.Success)
            {
                return await RollbackFailureAsync(
                    sourceCommitResult.Message,
                    StepCommit,
                    markerWritten,
                    stepTracker,
                    backups,
                    messages,
                    releaseFolderPath,
                    artifacts,
                    releaseFolderExistedBefore,
                    sourceRepositoryState,
                    targetRepositoryState,
                    cancellationToken);
            }

            if (targetRepositoryState is not null)
            {
                var targetCommitResult = await CommitIfNeededAsync(
                    targetRepositoryState,
                    _gitCommandService.CreateReleaseCommitMessage(request.TargetVersion, "publish WPF setup artifacts"),
                    requireChanges: false,
                    messages,
                    cancellationToken);
                if (!targetCommitResult.Success)
                {
                    return await RollbackFailureAsync(
                        targetCommitResult.Message,
                        StepCommit,
                        markerWritten,
                        stepTracker,
                        backups,
                        messages,
                        releaseFolderPath,
                        artifacts,
                        releaseFolderExistedBefore,
                        sourceRepositoryState,
                        targetRepositoryState,
                        cancellationToken);
                }
            }

            var commitMessage = BuildCommitSummary(sourceRepositoryState, targetRepositoryState);
            stepTracker.Success(StepCommit, commitMessage);
            LogStepSuccess(messages, StepCommit, commitMessage);

            LogStepStart(messages, StepPush);
            if (targetRepositoryState is not null)
            {
                var targetPushResult = await PushIfNeededAsync(targetRepositoryState, messages, cancellationToken);
                if (!targetPushResult.Success)
                {
                    return await RollbackFailureAsync(
                        targetPushResult.Message,
                        StepPush,
                        markerWritten,
                        stepTracker,
                        backups,
                        messages,
                        releaseFolderPath,
                        artifacts,
                        releaseFolderExistedBefore,
                        sourceRepositoryState,
                        targetRepositoryState,
                        cancellationToken);
                }
            }

            var sourcePushResult = await PushIfNeededAsync(sourceRepositoryState, messages, cancellationToken);
            if (!sourcePushResult.Success)
            {
                return await RollbackFailureAsync(
                    sourcePushResult.Message,
                    StepPush,
                    markerWritten,
                    stepTracker,
                    backups,
                    messages,
                    releaseFolderPath,
                    artifacts,
                    releaseFolderExistedBefore,
                    sourceRepositoryState,
                    targetRepositoryState,
                    cancellationToken);
            }

            var pushMessage = BuildPushSummary(sourceRepositoryState, targetRepositoryState);
            stepTracker.Success(StepPush, pushMessage);
            LogStepSuccess(messages, StepPush, pushMessage);
            stepTracker.Skip(StepRollback, "Kein Rollback erforderlich.");
            stepTracker.Success(StepCompletion, "Release erfolgreich abgeschlossen.");
            messages.Add("Release erfolgreich abgeschlossen.");
            AppendFinalEvaluation(messages, ReleaseExecutionOverallState.Successful, markerWritten: true, commitExecuted: true, pushExecuted: true);

            return new ReleaseExecutionResult
            {
                Success = true,
                OverallState = ReleaseExecutionOverallState.Successful,
                MarkerWritten = true,
                CommitExecuted = true,
                PushExecuted = true,
                Message = "Release erfolgreich abgeschlossen.",
                Messages = messages,
                ArtifactPaths = artifacts,
                ReleaseFolderPath = releaseFolderPath,
                Steps = stepTracker.Build()
            };
        }
        catch (Exception ex)
        {
            messages.Add($"Release mit Ausnahme fehlgeschlagen: {ex.Message}");
            return await RollbackFailureAsync(
                "Release mit Ausnahme fehlgeschlagen.",
                StepCompletion,
                markerWritten,
                stepTracker,
                backups,
                messages,
                releaseFolderPath,
                artifacts,
                releaseFolderExistedBefore,
                sourceRepositoryState,
                targetRepositoryState,
                cancellationToken);
        }
        finally
        {
            TryDeleteDirectory(stagingRoot, messages: null);
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
        var effectiveKeyPassword = string.IsNullOrWhiteSpace(request.AndroidKeyPassword)
            ? request.AndroidStorePassword
            : request.AndroidKeyPassword;
        var androidBuild = await _processExecutionService.RunAsync(
            _buildCommandService.CreateAndroidPublishCommand(
                mauiProjectPath,
                packageFormat,
                request.AndroidPackageName,
                request.AndroidKeystorePath,
                request.AndroidKeystoreAlias,
                request.AndroidStorePassword,
                effectiveKeyPassword),
            $"Android {packageFormat.ToUpperInvariant()} Build",
            cancellationToken);

        messages.Add(androidBuild.GetUserFacingMessage(
            request.AndroidStorePassword,
            effectiveKeyPassword,
            request.AndroidKeystorePath));
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

            var hasKeystore = !string.IsNullOrWhiteSpace(request.AndroidKeystorePath) && File.Exists(request.AndroidKeystorePath);
            var hasAlias = !string.IsNullOrWhiteSpace(request.AndroidKeystoreAlias);
            var hasStorePassword = !string.IsNullOrWhiteSpace(request.AndroidStorePassword);
            var hasKeyPassword = !string.IsNullOrWhiteSpace(request.AndroidKeyPassword);

            if (!hasKeystore)
                messages.Add("Hinweis: Kein Android-Keystore konfiguriert. Android-Artefakte werden (sofern möglich) unsigniert gebaut.");
            if (!hasAlias)
                messages.Add("Hinweis: Kein Android-Keystore-Alias konfiguriert. Android-Artefakte werden (sofern möglich) unsigniert gebaut.");
            if (!hasStorePassword && !hasKeyPassword)
                messages.Add("Hinweis: Keine Android-Signing-Passwörter eingegeben. Android-Artefakte werden (sofern möglich) unsigniert gebaut.");

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

    private void PublishArtifacts(
        ReleaseExecutionRequest request,
        string releaseFolderPath,
        string? stagedWpfSetupArtifact,
        string? stagedApkArtifact,
        string? stagedAabArtifact,
        List<string> artifacts,
        List<string> messages)
    {
        if (!string.IsNullOrWhiteSpace(stagedWpfSetupArtifact))
        {
            var wpfTargetDirectory = _releaseArtifactService.ResolveWpfTargetDirectory(request.WpfTargetRepoPath);
            if (string.IsNullOrWhiteSpace(wpfTargetDirectory.TargetDirectory))
            {
                throw new InvalidOperationException(wpfTargetDirectory.Message);
            }

            var releaseSetupPath = _releaseArtifactService.CopyArtifact(stagedWpfSetupArtifact, Path.Combine(releaseFolderPath, "WPF"));
            artifacts.Add(releaseSetupPath);
            messages.Add($"WPF-Setup in den Versionsordner kopiert: {releaseSetupPath}");

            var wpfRepoSetupPath = _releaseArtifactService.CopyArtifact(stagedWpfSetupArtifact, wpfTargetDirectory.TargetDirectory);
            artifacts.Add(wpfRepoSetupPath);
            messages.Add($"WPF-Setup in das lokale Zielrepo kopiert: {wpfRepoSetupPath}");

            var stableSetupPath = _releaseArtifactService.CopyArtifact(stagedWpfSetupArtifact, wpfTargetDirectory.TargetDirectory, "KGV-Setup.exe");
            artifacts.Add(stableSetupPath);
            messages.Add($"WPF-Setup als aktuelle Setup-Datei aktualisiert: {stableSetupPath}");
        }

        if (!string.IsNullOrWhiteSpace(stagedApkArtifact))
        {
            var releaseApkPath = _releaseArtifactService.CopyArtifact(stagedApkArtifact, Path.Combine(releaseFolderPath, "Android", "APK"));
            artifacts.Add(releaseApkPath);
            messages.Add($"APK in den Versionsordner kopiert: {releaseApkPath}");

            var apkOutputPath = _releaseArtifactService.CopyArtifact(stagedApkArtifact, request.ApkOutputPath);
            artifacts.Add(apkOutputPath);
            messages.Add($"APK in den konfigurierten Ausgabeordner kopiert: {apkOutputPath}");
        }

        if (!string.IsNullOrWhiteSpace(stagedAabArtifact))
        {
            var releaseAabPath = _releaseArtifactService.CopyArtifact(stagedAabArtifact, Path.Combine(releaseFolderPath, "Android", "AAB"));
            artifacts.Add(releaseAabPath);
            messages.Add($"AAB in den Versionsordner kopiert: {releaseAabPath}");

            var aabOutputPath = _releaseArtifactService.CopyArtifact(stagedAabArtifact, request.AabOutputPath);
            artifacts.Add(aabOutputPath);
            messages.Add($"AAB in den konfigurierten Ausgabeordner kopiert: {aabOutputPath}");
        }
    }

    private async Task<ReleaseExecutionResult> RollbackFailureAsync(
        string message,
        string failedStepName,
        bool markerWritten,
        ReleaseStepTracker stepTracker,
        List<VersionFileBackup> backups,
        List<string> messages,
        string releaseFolderPath,
        List<string>? copiedArtifacts,
        bool releaseFolderExistedBefore,
        GitRepositoryTransactionState sourceRepositoryState,
        GitRepositoryTransactionState? targetRepositoryState,
        CancellationToken cancellationToken)
    {
        stepTracker.Fail(failedStepName, message);
        LogStepFailure(messages, failedStepName, message);

        var rollbackRequired = backups.Count > 0
            || sourceRepositoryState.CommitCreated
            || sourceRepositoryState.PushCompleted
            || targetRepositoryState?.CommitCreated == true
            || targetRepositoryState?.PushCompleted == true;

        if (!rollbackRequired)
        {
            stepTracker.Skip(StepRollback, "Kein Rollback erforderlich.");
            stepTracker.MarkRemainingPendingAsSkipped("Wegen vorherigem Fehler nicht ausgeführt.");
            var failureSummary = $"Release fehlgeschlagen. Grund: {message}";
            stepTracker.Fail(StepCompletion, failureSummary);
            AppendFinalEvaluation(messages, ReleaseExecutionOverallState.Failed, markerWritten: false, commitExecuted: false, pushExecuted: false);
            return new ReleaseExecutionResult
            {
                Success = false,
                OverallState = ReleaseExecutionOverallState.Failed,
                Message = failureSummary,
                Messages = messages,
                ReleaseFolderPath = releaseFolderPath,
                ArtifactPaths = copiedArtifacts?.ToList() ?? new List<string>(),
                Steps = stepTracker.Build()
            };
        }

        LogStepStart(messages, StepRollback);
        var rollbackSucceeded = true;
        var versionRestoreSucceeded = true;
        var sourceResetPerformed = false;
        var targetResetPerformed = false;

        if (targetRepositoryState is not null)
        {
            rollbackSucceeded &= await TryRollbackGitRepositoryAsync(targetRepositoryState, messages, cancellationToken);
            targetResetPerformed = targetRepositoryState.ResetPerformed;
        }

        rollbackSucceeded &= await TryRollbackGitRepositoryAsync(sourceRepositoryState, messages, cancellationToken);
        sourceResetPerformed = sourceRepositoryState.ResetPerformed;

        if (backups.Count > 0)
        {
            if (sourceRepositoryState.PushCompleted)
            {
                rollbackSucceeded = false;
                versionRestoreSucceeded = false;
                messages.Add("Versionsänderungen und Marker können nicht automatisch zurückgesetzt werden, weil der Quellrepo-Commit bereits gepusht wurde.");
            }
            else if (!sourceResetPerformed)
            {
                var restoreResult = _releaseVersionFileService.RestoreBackups(backups);
                versionRestoreSucceeded = restoreResult.Success;
                foreach (var restoreMessage in restoreResult.Messages)
                {
                    messages.Add(restoreMessage);
                }

                messages.Add(restoreResult.Success
                    ? "Versionsänderungen wurden nach dem Fehler zurückgesetzt."
                    : "Versionsänderungen konnten nur teilweise zurückgesetzt werden.");
                rollbackSucceeded &= restoreResult.Success;
            }
            else
            {
                messages.Add("Versionsänderungen wurden durch Git-Rollback auf den Ausgangsstand zurückgesetzt.");
            }
        }

        CleanupArtifacts(copiedArtifacts, messages);

        if (!releaseFolderExistedBefore)
        {
            if (TryDeleteDirectory(releaseFolderPath, messages))
            {
                messages.Add($"Unvollständiger Veröffentlichungsordner wurde entfernt: {releaseFolderPath}");
            }
            else
            {
                messages.Add($"Unvollständiger Veröffentlichungsordner konnte nicht vollständig entfernt werden: {releaseFolderPath}");
            }
        }

        stepTracker.MarkRemainingPendingAsSkipped("Wegen vorherigem Fehler nicht ausgeführt.");

        var finalCommitExecuted = sourceRepositoryState.PushCompleted
            || targetRepositoryState?.PushCompleted == true
            || (sourceRepositoryState.CommitCreated && !sourceResetPerformed)
            || (targetRepositoryState?.CommitCreated == true && !targetResetPerformed);
        var finalPushExecuted = sourceRepositoryState.PushCompleted || targetRepositoryState?.PushCompleted == true;
        var finalMarkerWritten = markerWritten
            && (sourceRepositoryState.PushCompleted
                || (sourceRepositoryState.CommitCreated && !sourceResetPerformed)
                || !versionRestoreSucceeded);

        var overallState = rollbackSucceeded
            ? ReleaseExecutionOverallState.FailedRollbackSuccessful
            : ReleaseExecutionOverallState.FailedRollbackIncomplete;
        var finalMessage = rollbackSucceeded
            ? $"Release fehlgeschlagen, Rollback erfolgreich. Grund: {message}"
            : $"Release fehlgeschlagen, Rollback unvollständig. Grund: {message}";

        if (rollbackSucceeded)
        {
            if (backups.Count > 0 && (!finalCommitExecuted || versionRestoreSucceeded))
            {
                stepTracker.Revert(
                    StepWriteVersions,
                    sourceResetPerformed
                        ? "Versionsänderungen wurden durch Git-Rollback zurückgesetzt."
                        : "Versionsänderungen wurden im Rollback zurückgesetzt.");
            }

            if (markerWritten && !finalMarkerWritten)
            {
                stepTracker.Revert(StepWriteMarker, "Release-Marker wurde im Rollback zurückgesetzt.");
            }

            if ((sourceRepositoryState.CommitCreated || targetRepositoryState?.CommitCreated == true) && !finalCommitExecuted)
            {
                stepTracker.Revert(StepCommit, "Lokale Commits wurden im Rollback zurückgesetzt.");
            }

            stepTracker.Success(StepRollback, "Rollback erfolgreich abgeschlossen.");
            LogStepSuccess(messages, StepRollback, "Rollback erfolgreich abgeschlossen.");
        }
        else
        {
            stepTracker.Fail(StepRollback, "Rollback konnte nur teilweise abgeschlossen werden.");
            LogStepFailure(messages, StepRollback, "Rollback konnte nur teilweise abgeschlossen werden.");
        }

        stepTracker.Fail(StepCompletion, finalMessage);
        AppendFinalEvaluation(messages, overallState, finalMarkerWritten, finalCommitExecuted, finalPushExecuted);

        return new ReleaseExecutionResult
        {
            Success = false,
            RolledBack = rollbackSucceeded,
            OverallState = overallState,
            MarkerWritten = finalMarkerWritten,
            CommitExecuted = finalCommitExecuted,
            PushExecuted = finalPushExecuted,
            Message = finalMessage,
            Messages = messages,
            ReleaseFolderPath = releaseFolderPath,
            ArtifactPaths = copiedArtifacts?.ToList() ?? new List<string>(),
            Steps = stepTracker.Build()
        };
    }

    private async Task<(bool Success, string Message)> CaptureOriginalHeadAsync(
        GitRepositoryTransactionState repositoryState,
        List<string> messages,
        CancellationToken cancellationToken)
    {
        var headResult = await _processExecutionService.RunAsync(
            _gitCommandService.CreateRevParseHeadCommand(repositoryState.RepositoryPath),
            $"{repositoryState.DisplayName} Ausgangs-HEAD",
            cancellationToken);
        messages.Add(headResult.GetUserFacingMessage());
        if (!headResult.Success || string.IsNullOrWhiteSpace(headResult.StandardOutput))
        {
            return (false, $"{repositoryState.DisplayName}: Ausgangs-Commit konnte nicht gelesen werden.");
        }

        repositoryState.OriginalHead = headResult.StandardOutput.Trim();
        messages.Add($"{repositoryState.DisplayName}: Ausgangs-Commit gesichert.");
        return (true, string.Empty);
    }

    private async Task<(bool Success, string Message)> CommitIfNeededAsync(
        GitRepositoryTransactionState repositoryState,
        string commitMessage,
        bool requireChanges,
        List<string> messages,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(repositoryState.RepositoryPath) || !Directory.Exists(repositoryState.RepositoryPath))
        {
            var missingMessage = $"{repositoryState.DisplayName}: Repositorypfad wurde nicht gefunden: {repositoryState.RepositoryPath}";
            messages.Add(missingMessage);
            return (false, missingMessage);
        }

        var statusResult = await _processExecutionService.RunAsync(
            _gitCommandService.CreatePorcelainStatusCommand(repositoryState.RepositoryPath),
            $"{repositoryState.DisplayName} Status",
            cancellationToken);
        messages.Add(statusResult.GetUserFacingMessage());
        if (!statusResult.Success)
        {
            return (false, $"{repositoryState.DisplayName}: Status konnte nicht gelesen werden.");
        }

        var hasChanges = !string.IsNullOrWhiteSpace(statusResult.StandardOutput);
        if (!hasChanges)
        {
            if (requireChanges)
            {
                var noChangesMessage = $"{repositoryState.DisplayName}: Es wurden keine commitbaren Änderungen gefunden.";
                messages.Add(noChangesMessage);
                return (false, noChangesMessage);
            }

            messages.Add($"{repositoryState.DisplayName}: Keine neuen Änderungen für einen Commit gefunden.");
            return (true, string.Empty);
        }

        var addResult = await _processExecutionService.RunAsync(
            _gitCommandService.CreateAddAllCommand(repositoryState.RepositoryPath),
            $"{repositoryState.DisplayName} Add",
            cancellationToken);
        messages.Add(addResult.GetUserFacingMessage());
        if (!addResult.Success)
        {
            return (false, $"{repositoryState.DisplayName}: Änderungen konnten nicht gestaged werden.");
        }

        var commitResult = await _processExecutionService.RunAsync(
            _gitCommandService.CreateCommitCommand(repositoryState.RepositoryPath, commitMessage),
            $"{repositoryState.DisplayName} Commit",
            cancellationToken);
        messages.Add(commitResult.GetUserFacingMessage());
        if (!commitResult.Success)
        {
            return (false, $"{repositoryState.DisplayName}: Commit konnte nicht erstellt werden.");
        }

        repositoryState.CommitCreated = true;
        return (true, string.Empty);
    }

    private async Task<(bool Success, string Message)> PushIfNeededAsync(
        GitRepositoryTransactionState repositoryState,
        List<string> messages,
        CancellationToken cancellationToken)
    {
        if (!repositoryState.CommitCreated)
        {
            messages.Add($"{repositoryState.DisplayName}: Kein lokaler Commit zum Push vorhanden.");
            return (true, string.Empty);
        }

        var pushResult = await _processExecutionService.RunAsync(
            _gitCommandService.CreatePushCommand(repositoryState.RepositoryPath),
            $"{repositoryState.DisplayName} Push",
            cancellationToken);
        messages.Add(pushResult.GetUserFacingMessage());
        if (!pushResult.Success)
        {
            return (false, $"{repositoryState.DisplayName}: Push fehlgeschlagen.");
        }

        repositoryState.PushCompleted = true;
        return (true, string.Empty);
    }

    private async Task<bool> TryRollbackGitRepositoryAsync(
        GitRepositoryTransactionState repositoryState,
        List<string> messages,
        CancellationToken cancellationToken)
    {
        if (!repositoryState.CommitCreated)
        {
            return true;
        }

        if (repositoryState.PushCompleted)
        {
            messages.Add($"{repositoryState.DisplayName}: Commit wurde bereits gepusht und kann nicht automatisch zurückgesetzt werden.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(repositoryState.OriginalHead))
        {
            messages.Add($"{repositoryState.DisplayName}: Ausgangs-Commit fehlt für das Git-Rollback.");
            return false;
        }

        var resetResult = await _processExecutionService.RunAsync(
            _gitCommandService.CreateResetHardCommand(repositoryState.RepositoryPath, repositoryState.OriginalHead),
            $"{repositoryState.DisplayName} Rollback",
            cancellationToken);
        messages.Add(resetResult.GetUserFacingMessage());
        if (!resetResult.Success)
        {
            return false;
        }

        repositoryState.ResetPerformed = true;
        messages.Add($"{repositoryState.DisplayName}: Lokaler Commit wurde auf den Ausgangsstand zurückgesetzt.");
        return true;
    }

    private (bool Success, string Message) TryReadOriginalVersions(ReleaseExecutionRequest request, List<string> messages)
    {
        var versionResult = _versionService.DetectVersions(request.SourceRepoPath);
        if (!string.IsNullOrWhiteSpace(versionResult.StatusMessage))
        {
            messages.Add(versionResult.StatusMessage);
        }

        if (versionResult.HasWarning && !string.IsNullOrWhiteSpace(versionResult.WarningMessage))
        {
            messages.Add(versionResult.WarningMessage);
        }

        if (versionResult.HasError && !string.IsNullOrWhiteSpace(versionResult.ErrorMessage))
        {
            return (false, versionResult.ErrorMessage);
        }

        if (request.BuildWpf && !versionResult.IsWpfVersionDetected)
        {
            return (false, "Ausgangsversion für WPF konnte nicht gelesen werden.");
        }

        if ((request.BuildApk || request.BuildAab) && !versionResult.IsAndroidVersionDetected)
        {
            return (false, "Ausgangsversion für Android konnte nicht gelesen werden.");
        }

        var versionParts = new List<string>();
        if (request.BuildWpf)
        {
            versionParts.Add($"WPF {versionResult.WpfVersion}");
        }

        if (request.BuildApk || request.BuildAab)
        {
            versionParts.Add($"Android {versionResult.AndroidVersion}");
        }

        var message = versionParts.Count == 0
            ? "Keine Produktversionen für diesen Lauf ausgewählt."
            : $"Ausgangsversionen gelesen: {string.Join(" / ", versionParts)}";
        return (true, message);
    }

    private static ReleaseStepTracker CreateStepTracker()
        => new([
            StepReadOriginalVersions,
            StepWriteVersions,
            StepBuildWpfArtifacts,
            StepBuildApk,
            StepBuildAab,
            StepPublishArtifacts,
            StepWriteMarker,
            StepCommit,
            StepPush,
            StepRollback,
            StepCompletion
        ]);

    private static void ApplySelectionSkips(ReleaseStepTracker stepTracker, ReleaseExecutionRequest request)
    {
        if (!request.BuildWpf)
        {
            stepTracker.Skip(StepBuildWpfArtifacts, "Für diesen Lauf nicht ausgewählt.");
        }

        if (!request.BuildApk)
        {
            stepTracker.Skip(StepBuildApk, "Für diesen Lauf nicht ausgewählt.");
        }

        if (!request.BuildAab)
        {
            stepTracker.Skip(StepBuildAab, "Für diesen Lauf nicht ausgewählt.");
        }
    }

    private static string BuildCommitSummary(GitRepositoryTransactionState sourceRepositoryState, GitRepositoryTransactionState? targetRepositoryState)
    {
        var parts = new List<string>
        {
            sourceRepositoryState.CommitCreated
                ? "Quellrepo lokal commitet"
                : "Quellrepo ohne neuen lokalen Commit"
        };

        if (targetRepositoryState is not null)
        {
            parts.Add(targetRepositoryState.CommitCreated
                ? "WPF-Zielrepo lokal commitet"
                : "WPF-Zielrepo ohne neuen lokalen Commit");
        }

        return string.Join(", ", parts);
    }

    private static string BuildPushSummary(GitRepositoryTransactionState sourceRepositoryState, GitRepositoryTransactionState? targetRepositoryState)
    {
        var parts = new List<string>();
        if (targetRepositoryState is not null)
        {
            parts.Add(targetRepositoryState.PushCompleted
                ? "WPF-Zielrepo gepusht"
                : "WPF-Zielrepo ohne Push");
        }

        parts.Add(sourceRepositoryState.PushCompleted
            ? "Quellrepo gepusht"
            : "Quellrepo ohne Push");
        return string.Join(", ", parts);
    }

    private static void LogStepStart(List<string> messages, string stepName)
        => messages.Add($"Release-Schritt gestartet: {stepName}");

    private static void LogStepSuccess(List<string> messages, string stepName, string message)
        => messages.Add($"Release-Schritt erfolgreich: {stepName}. {message}");

    private static void LogStepFailure(List<string> messages, string stepName, string message)
        => messages.Add($"Release-Schritt fehlgeschlagen: {stepName}. {message}");

    private static void AppendFinalEvaluation(
        List<string> messages,
        ReleaseExecutionOverallState overallState,
        bool markerWritten,
        bool commitExecuted,
        bool pushExecuted)
    {
        messages.Add($"Marker final: {(markerWritten ? "ja" : "nein")}");
        messages.Add($"Commit final: {(commitExecuted ? "ja" : "nein")}");
        messages.Add($"Push final: {(pushExecuted ? "ja" : "nein")}");
        messages.Add($"Gesamtbewertung: {overallState switch
        {
            ReleaseExecutionOverallState.Successful => "erfolgreich",
            ReleaseExecutionOverallState.FailedRollbackSuccessful => "fehlgeschlagen, rollback erfolgreich",
            ReleaseExecutionOverallState.FailedRollbackIncomplete => "fehlgeschlagen, rollback unvollständig",
            _ => "fehlgeschlagen"
        }}");
    }

    private static bool TryDeleteDirectory(string path, List<string>? messages)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(path) && Directory.Exists(path))
            {
                Directory.Delete(path, true);
            }

            return true;
        }
        catch (Exception ex)
        {
            messages?.Add($"Verzeichnis konnte nicht entfernt werden: {path} ({ex.Message})");
            return false;
        }
    }

    private static void CleanupArtifacts(List<string>? copiedArtifacts, List<string> messages)
    {
        if (copiedArtifacts is null || copiedArtifacts.Count == 0)
        {
            return;
        }

        foreach (var artifactPath in copiedArtifacts.Where(path => !string.IsNullOrWhiteSpace(path)).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            try
            {
                if (File.Exists(artifactPath))
                {
                    File.Delete(artifactPath);
                    messages.Add($"Unvollständiges Artefakt wurde entfernt: {artifactPath}");
                }
            }
            catch (Exception ex)
            {
                messages.Add($"Unvollständiges Artefakt konnte nicht entfernt werden: {artifactPath} ({ex.Message})");
            }
        }
    }

    private static void BackupFileIfNeeded(ICollection<VersionFileBackup> backups, string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath) || backups.Any(backup => string.Equals(backup.FilePath, filePath, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        backups.Add(new VersionFileBackup
        {
            FilePath = filePath,
            OriginalContent = File.ReadAllText(filePath)
        });
    }

    private sealed class GitRepositoryTransactionState
    {
        public GitRepositoryTransactionState(string repositoryPath, string displayName)
        {
            RepositoryPath = repositoryPath;
            DisplayName = displayName;
        }

        public string RepositoryPath { get; }
        public string DisplayName { get; }
        public string OriginalHead { get; set; } = string.Empty;
        public bool CommitCreated { get; set; }
        public bool PushCompleted { get; set; }
        public bool ResetPerformed { get; set; }
    }

    private sealed class ReleaseStepTracker
    {
        private readonly IReadOnlyList<string> _stepOrder;
        private readonly Dictionary<string, ReleaseExecutionStepResult> _steps;

        public ReleaseStepTracker(IReadOnlyList<string> stepOrder)
        {
            _stepOrder = stepOrder;
            _steps = stepOrder.ToDictionary(
                step => step,
                step => new ReleaseExecutionStepResult
                {
                    Name = step,
                    State = ReleaseExecutionStepState.Pending,
                    Message = "Noch nicht ausgeführt."
                },
                StringComparer.Ordinal);
        }

        public void Success(string stepName, string message)
        {
            Update(stepName, ReleaseExecutionStepState.Successful, message);
        }

        public void Fail(string stepName, string message)
        {
            Update(stepName, ReleaseExecutionStepState.Failed, message);
        }

        public void Skip(string stepName, string message)
        {
            if (_steps.TryGetValue(stepName, out var step)
                && step.State is ReleaseExecutionStepState.Successful or ReleaseExecutionStepState.Failed)
            {
                return;
            }

            Update(stepName, ReleaseExecutionStepState.Skipped, message);
        }

        public void Revert(string stepName, string message)
        {
            Update(stepName, ReleaseExecutionStepState.Reverted, message);
        }

        public void MarkRemainingPendingAsSkipped(string message)
        {
            foreach (var step in _steps.Values.Where(step => step.State == ReleaseExecutionStepState.Pending))
            {
                step.State = ReleaseExecutionStepState.Skipped;
                step.Message = message;
            }
        }

        public IReadOnlyList<ReleaseExecutionStepResult> Build()
            => _stepOrder.Select(stepName => _steps[stepName]).ToList();

        private void Update(string stepName, ReleaseExecutionStepState state, string message)
        {
            if (!_steps.TryGetValue(stepName, out var step))
            {
                return;
            }

            step.State = state;
            step.Message = message;
        }
    }
}
