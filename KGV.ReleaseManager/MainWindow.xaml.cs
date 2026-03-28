using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
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

    public MainWindow()
    {
        InitializeComponent();

        var settingsFile = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "KGV.ReleaseManager",
            "settings.json");

        _settingsService = new SettingsService(settingsFile);
        _versionService = new VersionService();
        _releaseFolderService = new ReleaseFolderService();
        _logExtractionService = new LogExtractionService();
        _releaseNotesService = new ReleaseNotesImportExportService();

        _viewModel = new MainViewModel();
        _viewModel.SettingsStoragePath = settingsFile;
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
    }

    private void AutoIncrementVersion_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.NextVersion = _versionService.IncrementPatch(_viewModel.CurrentVersion);
        _viewModel.AppendStatus($"Nächste Version gesetzt: {_viewModel.NextVersion}");
    }

    private void CreateReleaseFolder_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_viewModel.Settings.ReleaseOutputRootPath))
        {
            _viewModel.AppendStatus("ReleaseOutput-Pfad fehlt.");
            return;
        }

        var folder = _releaseFolderService.EnsureVersionFolder(
            _viewModel.Settings.ReleaseOutputRootPath,
            string.IsNullOrWhiteSpace(_viewModel.NextVersion) ? _viewModel.CurrentVersion : _viewModel.NextVersion);

        _viewModel.AppendStatus($"Releaseordner angelegt: {folder}");
    }

    private void CreateExportPrompt_Click(object sender, RoutedEventArgs e)
    {
        var logFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Documentation", "KGV_Fortschritt_ausfuehrlich.md");
        var excerpt = File.Exists(logFile)
            ? _logExtractionService.GetLatestSection(logFile)
            : "Kein Log gefunden. TODO: echtes Projektlog aus KGV.neu einbinden.";

        _viewModel.ExportText = _releaseNotesService.CreateChatPrompt(
            _viewModel.CurrentVersion,
            string.IsNullOrWhiteSpace(_viewModel.NextVersion) ? _viewModel.CurrentVersion : _viewModel.NextVersion,
            excerpt);

        _viewModel.AppendStatus("Export-Prompt erzeugt.");
    }

    private void ImportSummary_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.ImportedSummary = _releaseNotesService.NormalizeImportedSummary(_viewModel.ExportText);
        _viewModel.AppendStatus("Zusammenfassung übernommen.");
    }

    private void RunDryRelease_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.AppendStatus("Dry Run gestartet.");
        _viewModel.AppendStatus("TODO: echte Validierung der Pfade, Versionsdateien und Build-Voraussetzungen ergänzen.");
        _viewModel.AppendStatus("Dry Run beendet.");
    }

    private void RunRelease_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.AppendStatus("Release gestartet.");
        _viewModel.AppendStatus("TODO: Git, Build, Setup, APK, AAB, Rollback und Publishing verdrahten.");
        _viewModel.AppendStatus("Release aktuell nur als Scaffold vorbereitet.");
    }

    private void LoadSettings(bool showMessageBoxOnFailure)
    {
        var loadResult = _settingsService.Load();
        _viewModel.Settings = loadResult.Settings;
        _viewModel.AppendStatus(loadResult.Message);

        if (showMessageBoxOnFailure && !loadResult.LoadedFromDisk)
        {
            MessageBox.Show(loadResult.Message, "Einstellungen laden", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
