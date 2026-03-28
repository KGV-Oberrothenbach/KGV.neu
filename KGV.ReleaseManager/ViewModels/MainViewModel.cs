using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using KGV.ReleaseManager.Models;

namespace KGV.ReleaseManager.ViewModels;

public sealed class MainViewModel : INotifyPropertyChanged
{
    private ReleaseManagerSettings _settings = new();
    private string _currentVersion = "0.1.0";
    private string _nextVersion = "0.1.1";
    private bool _buildWpf = true;
    private bool _buildApk = true;
    private bool _buildAab = true;
    private string _exportText = string.Empty;
    private string _importedSummary = string.Empty;
    private string _statusText = "Bereit.";
    private string _footerText = "Noch kein Release ausgeführt.";
    private string _settingsStoragePath = string.Empty;

    public event PropertyChangedEventHandler? PropertyChanged;

    public ReleaseManagerSettings Settings
    {
        get => _settings;
        set { _settings = value; OnPropertyChanged(); }
    }

    public string CurrentVersion
    {
        get => _currentVersion;
        set { _currentVersion = value; OnPropertyChanged(); }
    }

    public string NextVersion
    {
        get => _nextVersion;
        set { _nextVersion = value; OnPropertyChanged(); }
    }

    public bool BuildWpf
    {
        get => _buildWpf;
        set { _buildWpf = value; OnPropertyChanged(); }
    }

    public bool BuildApk
    {
        get => _buildApk;
        set { _buildApk = value; OnPropertyChanged(); }
    }

    public bool BuildAab
    {
        get => _buildAab;
        set { _buildAab = value; OnPropertyChanged(); }
    }

    public string ExportText
    {
        get => _exportText;
        set { _exportText = value; OnPropertyChanged(); }
    }

    public string ImportedSummary
    {
        get => _importedSummary;
        set { _importedSummary = value; OnPropertyChanged(); }
    }

    public string StatusText
    {
        get => _statusText;
        set { _statusText = value; OnPropertyChanged(); }
    }

    public string FooterText
    {
        get => _footerText;
        set { _footerText = value; OnPropertyChanged(); }
    }

    public string SettingsStoragePath
    {
        get => _settingsStoragePath;
        set { _settingsStoragePath = value; OnPropertyChanged(); }
    }

    public IReadOnlyList<string> ValidateSettings()
    {
        Settings.Normalize();

        var errors = new List<string>();
        ValidateRequiredDirectory(Settings.SourceRepoPath, "Lokaler Pfad zum Quellprojekt KGV.neu", errors);
        ValidateRequiredDirectory(Settings.WpfTargetRepoPath, "Lokaler Pfad zum Zielrepo für WPF-Release", errors);
        ValidateRequiredDirectory(Settings.ApkOutputPath, "Lokaler Ausgabeordner für APK", errors);
        ValidateRequiredDirectory(Settings.AabOutputPath, "Lokaler Ausgabeordner für AAB", errors);
        ValidateRequiredDirectory(Settings.ReleaseOutputRootPath, "Lokaler Basisordner für Veröffentlichungen", errors);

        if (!string.IsNullOrWhiteSpace(Settings.SourceRepoPath)
            && !File.Exists(Path.Combine(Settings.SourceRepoPath, "KGV.slnx")))
        {
            errors.Add("Der Pfad zum Quellprojekt KGV.neu enthält keine `KGV.slnx`.");
        }

        if (!string.IsNullOrWhiteSpace(Settings.AndroidPackageName)
            && Settings.AndroidPackageName.Any(char.IsWhiteSpace))
        {
            errors.Add("Package Name darf keine Leerzeichen enthalten.");
        }

        if (!string.IsNullOrWhiteSpace(Settings.PlayTrackName)
            && Settings.PlayTrackName.Any(char.IsWhiteSpace))
        {
            errors.Add("Play-Track sollte keine Leerzeichen enthalten.");
        }

        if (!string.IsNullOrWhiteSpace(Settings.StoreUrl)
            && !Uri.TryCreate(Settings.StoreUrl, UriKind.Absolute, out _))
        {
            errors.Add("Store-URL / Store-Link ist keine gültige absolute URL.");
        }

        return errors;
    }

    public void AppendStatus(string message)
    {
        var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}";
        StatusText = string.IsNullOrWhiteSpace(StatusText) ? line : $"{StatusText}{Environment.NewLine}{line}";
        FooterText = message;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    private static void ValidateRequiredDirectory(string path, string label, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            errors.Add($"{label} ist ein Pflichtfeld.");
            return;
        }

        if (!Directory.Exists(path))
            errors.Add($"{label} wurde lokal nicht gefunden: {path}");
    }
}
