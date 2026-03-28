using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using KGV.ReleaseManager.Models;
using KGV.ReleaseManager.Services;
using KGV.ReleaseManager.ViewModels;

namespace KGV.ReleaseManager;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private readonly SettingsService _settingsService;
    private readonly VersionService _versionService;
    private readonly ReleaseFolderService _releaseFolderService;
    private readonly LogExtractionService _logExtractionService;
    private readonly ReleaseNotesImportExportService _releaseNotesService;
    private readonly ReleaseNotesHistoryService _releaseNotesHistoryService;
    private readonly ReleaseNotesAnalysisService _releaseNotesAnalysisService;
    private readonly ReleaseExecutionService _releaseExecutionService;
    private readonly RuntimeSecretPromptService _runtimeSecretPromptService;
    private ReleaseNotesAnalysisResult? _lastReleaseNotesAnalysisResult;

    public MainWindow()
    {
        InitializeComponent();

        var settingsFile = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "KGV.ReleaseManager",
            "settings.json");
        var releaseNotesHistoryFile = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "KGV.ReleaseManager",
            "release-notes-history.json");

        _settingsService = new SettingsService(settingsFile);
        _versionService = new VersionService();
        _releaseFolderService = new ReleaseFolderService();
        _logExtractionService = new LogExtractionService();
        _releaseNotesService = new ReleaseNotesImportExportService();
        _releaseNotesHistoryService = new ReleaseNotesHistoryService(releaseNotesHistoryFile);
        _releaseNotesAnalysisService = new ReleaseNotesAnalysisService(
            _logExtractionService,
            _releaseNotesService,
            _releaseNotesHistoryService);
        _releaseExecutionService = new ReleaseExecutionService(
            _releaseFolderService,
            new BuildCommandService(),
            new ProcessExecutionService(),
            new ReleaseVersionFileService(),
            new ReleaseArtifactService());
        _runtimeSecretPromptService = new RuntimeSecretPromptService();

        _viewModel = new MainViewModel();
        _viewModel.SettingsStoragePath = settingsFile;
        _viewModel.ReleaseNotesStoragePath = releaseNotesHistoryFile;
        DataContext = _viewModel;
        _viewModel.AppendStatus("Projektgerüst geladen.");
        LoadSettings(showMessageBoxOnFailure: false);
    }

    private void LoadSettings_Click(object sender, RoutedEventArgs e)
        => LoadSettings(showMessageBoxOnFailure: true);

    private void SaveSettings_Click(object sender, RoutedEventArgs e)
    {
        var validationErrors = _viewModel.ValidateSettings();
        if (validationErrors.Count > 0)
        {
            var message = string.Join(Environment.NewLine, validationErrors);
            MessageBox.Show(message, "Einstellungen prüfen", MessageBoxButton.OK, MessageBoxImage.Warning);
            _viewModel.AppendStatus("Einstellungen nicht gespeichert: Validierungsfehler.");
            return;
        }

        var saveResult = _settingsService.Save(_viewModel.Settings);
        if (!saveResult.Success)
        {
            MessageBox.Show(saveResult.Message, "Einstellungen speichern", MessageBoxButton.OK, MessageBoxImage.Error);
            _viewModel.AppendStatus(saveResult.Message);
            return;
        }

        _viewModel.AppendStatus(saveResult.Message);
        RefreshProjectState();
    }

    private void RefreshProjectState_Click(object sender, RoutedEventArgs e)
    {
        RefreshProjectState();
    }

    private void CreateReleaseFolder_Click(object sender, RoutedEventArgs e)
    {
        var result = _releaseFolderService.PrepareVersionFolder(
            _viewModel.Settings.ReleaseOutputRootPath,
            _viewModel.TargetVersion);

        _viewModel.ReleaseFolderStatusText = result.Message;
        _viewModel.AppendStatus(result.Message);

        if (!result.Success)
        {
            MessageBox.Show(result.Message, "Veröffentlichungsordner", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void CreateExportPrompt_Click(object sender, RoutedEventArgs e)
    {
        RefreshReleaseNotesState(preserveImportedSummary: true);
        _viewModel.AppendStatus("Exporttext für die Release-Aufbereitung erzeugt.");
    }

    private void CopyExportText_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_viewModel.ExportText))
        {
            RefreshReleaseNotesState(preserveImportedSummary: true);
        }

        if (string.IsNullOrWhiteSpace(_viewModel.ExportText))
        {
            MessageBox.Show("Es konnte kein Exporttext erzeugt werden.", "Änderungen kopieren", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            Clipboard.SetText(_viewModel.ExportText);
            _viewModel.AppendStatus("Exporttext in die Zwischenablage kopiert.");
            MessageBox.Show("Exporttext wurde in die Zwischenablage kopiert.", "Änderungen kopieren", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            _viewModel.AppendStatus($"Zwischenablage konnte nicht beschrieben werden: {ex.Message}");
            MessageBox.Show($"Zwischenablage konnte nicht beschrieben werden: {ex.Message}", "Änderungen kopieren", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void SaveImportedSummary_Click(object sender, RoutedEventArgs e)
    {
        RefreshReleaseNotesState(preserveImportedSummary: true);

        var importResult = _releaseNotesService.ParseImportedSummary(_viewModel.ImportedSummary);
        if (!importResult.Success)
        {
            _viewModel.AppendStatus(importResult.Message);
            MessageBox.Show(importResult.Message, "Zusammenfassung importieren", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(_viewModel.TargetVersion))
        {
            const string message = "Für den Import muss zuerst eine Zielversion ermittelt werden.";
            _viewModel.AppendStatus(message);
            MessageBox.Show(message, "Zusammenfassung importieren", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var historyEntry = new ReleaseNotesHistoryEntry
        {
            Version = _viewModel.TargetVersion,
            SavedAtUtc = DateTime.UtcNow,
            LogSourcePath = _lastReleaseNotesAnalysisResult?.LogSourcePath ?? _viewModel.LogSourcePath,
            SourceDescription = _lastReleaseNotesAnalysisResult?.SourceDescription ?? string.Empty,
            LogAnchorHeading = _lastReleaseNotesAnalysisResult?.AnchorHeading ?? string.Empty,
            ExportText = _viewModel.ExportText,
            RawLogExcerpt = _lastReleaseNotesAnalysisResult?.ChangesPreview ?? string.Empty,
            Title = importResult.Title,
            ShortDescription = importResult.ShortDescription,
            WpfReleaseText = importResult.WpfReleaseText,
            AndroidReleaseText = importResult.AndroidReleaseText,
            ImportedRawText = importResult.NormalizedText
        };

        var saveResult = _releaseNotesHistoryService.SaveEntry(historyEntry);
        _viewModel.AppendStatus(saveResult.Message);
        if (!saveResult.Success)
        {
            MessageBox.Show(saveResult.Message, "Zusammenfassung importieren", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        _viewModel.ImportedSummary = importResult.NormalizedText;
        RefreshReleaseNotesState(preserveImportedSummary: false);
        MessageBox.Show(saveResult.Message, "Zusammenfassung importieren", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private async void RunDryRelease_Click(object sender, RoutedEventArgs e)
    {
        await ExecuteReleaseAsync(isDryRun: true);
    }

    private async void RunRelease_Click(object sender, RoutedEventArgs e)
    {
        await ExecuteReleaseAsync(isDryRun: false);
    }

    private void LoadSettings(bool showMessageBoxOnFailure)
    {
        var loadResult = _settingsService.Load();
        _viewModel.Settings = loadResult.Settings;
        _viewModel.AppendStatus(loadResult.Message);
        RefreshProjectState(preserveImportedSummary: false);

        if (showMessageBoxOnFailure && !loadResult.LoadedFromDisk)
        {
            MessageBox.Show(loadResult.Message, "Einstellungen laden", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void RefreshProjectState(bool preserveImportedSummary = true)
    {
        var versionResult = _versionService.DetectVersions(_viewModel.Settings.SourceRepoPath);
        _viewModel.ApplyVersionDetection(versionResult);
        _viewModel.AppendStatus(versionResult.HasError ? versionResult.ErrorMessage : versionResult.StatusMessage);

        if (versionResult.HasWarning)
        {
            _viewModel.AppendStatus(versionResult.WarningMessage);
        }

        var logSourceStatus = _logExtractionService.DetectPrimaryLogSource(_viewModel.Settings.SourceRepoPath);
        _viewModel.ApplyLogSourceStatus(logSourceStatus);
        _viewModel.AppendStatus(logSourceStatus.Message);

        RefreshReleaseNotesState(preserveImportedSummary);
    }

    private void RefreshReleaseNotesState(bool preserveImportedSummary)
    {
        _lastReleaseNotesAnalysisResult = _releaseNotesAnalysisService.Analyze(
            _viewModel.Settings.SourceRepoPath,
            _viewModel.CurrentVersion,
            _viewModel.TargetVersion);

        _viewModel.ApplyReleaseNotesAnalysis(_lastReleaseNotesAnalysisResult);

        var latestEntry = _releaseNotesHistoryService.GetLatestEntry();
        if (latestEntry is not null && (!preserveImportedSummary || string.IsNullOrWhiteSpace(_viewModel.ImportedSummary)))
        {
            _viewModel.ImportedSummary = latestEntry.ImportedRawText;
        }

        if (!string.IsNullOrWhiteSpace(_lastReleaseNotesAnalysisResult.Message))
        {
            _viewModel.AppendStatus(_lastReleaseNotesAnalysisResult.Message);
        }
    }

    private async Task ExecuteReleaseAsync(bool isDryRun)
    {
        var validationErrors = _viewModel.ValidateSettings();
        if (validationErrors.Count > 0)
        {
            var message = string.Join(Environment.NewLine, validationErrors);
            _viewModel.ReleaseStateText = "Settings unvollständig.";
            _viewModel.AppendStatus(message);
            MessageBox.Show(message, isDryRun ? "Dry Run" : "Release starten", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var request = CreateReleaseExecutionRequest();
        if ((request.BuildApk || request.BuildAab) && !isDryRun)
        {
            var secrets = _runtimeSecretPromptService.PromptForAndroidSigningSecrets(this);
            if (secrets is null)
            {
                _viewModel.ReleaseStateText = "Release abgebrochen: Android-Passworteingabe wurde abgebrochen.";
                _viewModel.AppendStatus(_viewModel.ReleaseStateText);
                return;
            }

            request.AndroidStorePassword = secrets.StorePassword;
            request.AndroidKeyPassword = secrets.KeyPassword;
        }
        else if (request.BuildApk || request.BuildAab)
        {
            request.AndroidStorePassword = "dry-run";
            request.AndroidKeyPassword = "dry-run";
        }

        _viewModel.ReleaseStateText = isDryRun ? "Dry Run läuft..." : "Release läuft...";
        _viewModel.AppendStatus(_viewModel.ReleaseStateText);

        var result = isDryRun
            ? await _releaseExecutionService.ValidateAsync(request)
            : await _releaseExecutionService.ExecuteAsync(request);

        foreach (var line in result.Messages.Where(line => !string.IsNullOrWhiteSpace(line)))
        {
            _viewModel.AppendStatus(line);
        }

        _viewModel.ReleaseStateText = result.Message;
        _viewModel.ReleaseFolderStatusText = string.IsNullOrWhiteSpace(result.ReleaseFolderPath)
            ? _viewModel.ReleaseFolderStatusText
            : $"Letzter Releaseordner: {result.ReleaseFolderPath}";

        if (result.Success)
        {
            MessageBox.Show(result.Message, isDryRun ? "Dry Run" : "Release", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        else
        {
            MessageBox.Show(result.Message, isDryRun ? "Dry Run" : "Release", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        RefreshProjectState();
    }

    private ReleaseExecutionRequest CreateReleaseExecutionRequest()
    {
        return new ReleaseExecutionRequest
        {
            SourceRepoPath = _viewModel.Settings.SourceRepoPath,
            WpfTargetRepoPath = _viewModel.Settings.WpfTargetRepoPath,
            TargetVersion = _viewModel.TargetVersion,
            ReleaseOutputRootPath = _viewModel.Settings.ReleaseOutputRootPath,
            ApkOutputPath = _viewModel.Settings.ApkOutputPath,
            AabOutputPath = _viewModel.Settings.AabOutputPath,
            InnoSetupCompilerPath = _viewModel.Settings.InnoSetupCompilerPath,
            AndroidKeystorePath = _viewModel.Settings.AndroidKeystorePath,
            AndroidKeystoreAlias = _viewModel.Settings.AndroidKeystoreAlias,
            BuildWpf = _viewModel.BuildWpf,
            BuildApk = _viewModel.BuildApk,
            BuildAab = _viewModel.BuildAab
        };
    }
}
