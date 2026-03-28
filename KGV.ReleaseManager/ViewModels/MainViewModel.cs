using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using KGV.ReleaseManager.Models;

namespace KGV.ReleaseManager.ViewModels;

public sealed class MainViewModel : INotifyPropertyChanged
{
    private ReleaseManagerSettings _settings = new();
    private string _currentVersion = "Nicht erkannt";
    private string _targetVersion = string.Empty;
    private string _detectedWpfVersion = "Nicht gefunden";
    private string _detectedAndroidVersion = "Nicht gefunden";
    private string _detectedWpfVersionValue = string.Empty;
    private string _detectedAndroidVersionValue = string.Empty;
    private string _versionStatusText = "Noch nicht geprüft.";
    private string _versionWarningText = string.Empty;
    private VersionBumpType _selectedVersionBump = VersionBumpType.Patch;
    private string _logSourcePath = "-";
    private string _logSourceStatusText = "Noch nicht geprüft.";
    private string _releaseFolderStatusText = "Noch kein Veröffentlichungsordner vorbereitet.";
    private string _releaseStateText = "Noch kein Release ausgeführt.";
    private bool _buildWpf = true;
    private bool _buildApk = true;
    private bool _buildAab = true;
    private string _exportText = string.Empty;
    private string _importedSummary = string.Empty;
    private string _lastKnownReleaseText = "Noch kein gespeicherter Release-Anker.";
    private string _releaseChangesStatusText = "Release-Änderungen noch nicht ausgewertet.";
    private string _releaseChangesPreview = string.Empty;
    private string _releaseNotesStoragePath = string.Empty;
    private string _wpfReleaseNotesStoragePath = string.Empty;
    private string _androidReleaseNotesStoragePath = string.Empty;
    private string _lastKnownWpfReleaseText = "Noch kein gespeicherter WPF-Release-Anker.";
    private string _lastKnownAndroidReleaseText = "Noch kein gespeicherter Android-Release-Anker.";
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
        set
        {
            _currentVersion = value;
            OnPropertyChanged();
            UpdateTargetVersion();
        }
    }

    public string TargetVersion
    {
        get => _targetVersion;
        set { _targetVersion = value; OnPropertyChanged(); }
    }

    public string DetectedWpfVersion
    {
        get => _detectedWpfVersion;
        set { _detectedWpfVersion = value; OnPropertyChanged(); }
    }

    public string DetectedAndroidVersion
    {
        get => _detectedAndroidVersion;
        set { _detectedAndroidVersion = value; OnPropertyChanged(); }
    }

    public string VersionStatusText
    {
        get => _versionStatusText;
        set { _versionStatusText = value; OnPropertyChanged(); }
    }

    public string VersionWarningText
    {
        get => _versionWarningText;
        set
        {
            _versionWarningText = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasVersionWarning));
        }
    }

    public bool HasVersionWarning => !string.IsNullOrWhiteSpace(VersionWarningText);

    public VersionBumpType SelectedVersionBump
    {
        get => _selectedVersionBump;
        set
        {
            _selectedVersionBump = value;
            OnPropertyChanged();
            UpdateTargetVersion();
        }
    }

    public IReadOnlyList<VersionBumpType> VersionBumpOptions { get; } = Enum.GetValues<VersionBumpType>();

    public string LogSourcePath
    {
        get => _logSourcePath;
        set { _logSourcePath = value; OnPropertyChanged(); }
    }

    public string LogSourceStatusText
    {
        get => _logSourceStatusText;
        set { _logSourceStatusText = value; OnPropertyChanged(); }
    }

    public string ReleaseFolderStatusText
    {
        get => _releaseFolderStatusText;
        set { _releaseFolderStatusText = value; OnPropertyChanged(); }
    }

    public string ReleaseStateText
    {
        get => _releaseStateText;
        set { _releaseStateText = value; OnPropertyChanged(); }
    }

    public bool BuildWpf
    {
        get => _buildWpf;
        set
        {
            _buildWpf = value;
            OnPropertyChanged();
            UpdateTargetVersion();
        }
    }

    public bool BuildApk
    {
        get => _buildApk;
        set
        {
            _buildApk = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasAndroidReleaseSelection));
            UpdateTargetVersion();
        }
    }

    public bool BuildAab
    {
        get => _buildAab;
        set
        {
            _buildAab = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasAndroidReleaseSelection));
            UpdateTargetVersion();
        }
    }

    public bool HasAndroidReleaseSelection => BuildApk || BuildAab;

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

    public string LastKnownReleaseText
    {
        get => _lastKnownReleaseText;
        set { _lastKnownReleaseText = value; OnPropertyChanged(); }
    }

    public string ReleaseChangesStatusText
    {
        get => _releaseChangesStatusText;
        set { _releaseChangesStatusText = value; OnPropertyChanged(); }
    }

    public string ReleaseChangesPreview
    {
        get => _releaseChangesPreview;
        set { _releaseChangesPreview = value; OnPropertyChanged(); }
    }

    public string ReleaseNotesStoragePath
    {
        get => _releaseNotesStoragePath;
        set { _releaseNotesStoragePath = value; OnPropertyChanged(); }
    }

    public string StatusText
    {
        get => _statusText;
        set { _statusText = value; OnPropertyChanged(); }
    }

    public string WpfReleaseNotesStoragePath
    {
        get => _wpfReleaseNotesStoragePath;
        set { _wpfReleaseNotesStoragePath = value; OnPropertyChanged(); }
    }

    public string AndroidReleaseNotesStoragePath
    {
        get => _androidReleaseNotesStoragePath;
        set { _androidReleaseNotesStoragePath = value; OnPropertyChanged(); }
    }

    public string LastKnownWpfReleaseText
    {
        get => _lastKnownWpfReleaseText;
        set { _lastKnownWpfReleaseText = value; OnPropertyChanged(); }
    }

    public string LastKnownAndroidReleaseText
    {
        get => _lastKnownAndroidReleaseText;
        set { _lastKnownAndroidReleaseText = value; OnPropertyChanged(); }
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
        ValidateRequiredPath(Settings.ReleaseOutputRootPath, "Lokaler Basisordner für Veröffentlichungen", errors);
        ValidateOptionalDirectory(Settings.WpfTargetRepoPath, "Lokaler Pfad zum Zielrepo für WPF-Release", errors);
        ValidateOptionalPath(Settings.ApkOutputPath, "Lokaler Ausgabeordner für APK", errors);
        ValidateOptionalPath(Settings.AabOutputPath, "Lokaler Ausgabeordner für AAB", errors);

        if (!string.IsNullOrWhiteSpace(Settings.SourceRepoPath)
            && !File.Exists(Path.Combine(Settings.SourceRepoPath, "KGV.slnx")))
        {
            errors.Add("Der Pfad zum Quellprojekt KGV.neu enthält keine `KGV.slnx`.");
        }

        if (!string.IsNullOrWhiteSpace(Settings.AndroidKeystorePath)
            && !File.Exists(Settings.AndroidKeystorePath))
        {
            errors.Add($"Der konfigurierte Keystore-Pfad wurde nicht gefunden: {Settings.AndroidKeystorePath}");
        }

        if (!string.IsNullOrWhiteSpace(Settings.InnoSetupCompilerPath)
            && !File.Exists(Settings.InnoSetupCompilerPath))
        {
            errors.Add($"Der konfigurierte Pfad zu `ISCC.exe` wurde nicht gefunden: {Settings.InnoSetupCompilerPath}");
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

    public void AppendStatus(string message
    )
    {
        var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}";
        StatusText = string.IsNullOrWhiteSpace(StatusText) ? line : $"{StatusText}{Environment.NewLine}{line}";
        FooterText = message;
    }

    public void ApplyVersionDetection(VersionDetectionResult result)
    {
        _detectedWpfVersionValue = result.WpfVersion;
        _detectedAndroidVersionValue = result.AndroidVersion;

        DetectedWpfVersion = string.IsNullOrWhiteSpace(result.WpfVersion) ? "Nicht gefunden" : result.WpfVersion;
        DetectedAndroidVersion = string.IsNullOrWhiteSpace(result.AndroidVersion)
            ? "Nicht gefunden"
            : string.IsNullOrWhiteSpace(result.AndroidVersionCode)
                ? result.AndroidVersion
                : $"{result.AndroidVersion} (Code {result.AndroidVersionCode})";
        CurrentVersion = BuildCurrentVersionSummary(result);

        VersionStatusText = result.HasError
            ? result.ErrorMessage
            : string.IsNullOrWhiteSpace(result.StatusMessage)
                ? "Versionsermittlung abgeschlossen."
                : result.StatusMessage;

        VersionWarningText = result.WarningMessage;
    }

    public void ApplyLogSourceStatus(LogSourceStatus status)
    {
        LogSourcePath = string.IsNullOrWhiteSpace(status.Path) ? "-" : status.Path;
        LogSourceStatusText = string.IsNullOrWhiteSpace(status.Message)
            ? "Logquelle nicht geprüft."
            : status.Message;
    }

    public void ApplyReleaseNotesAnalysis(ReleaseNotesAnalysisResult result)
    {
        LastKnownReleaseText = string.IsNullOrWhiteSpace(result.LastKnownReleaseText)
            ? "Kein letzter gespeicherter Release-Anker vorhanden."
            : result.LastKnownReleaseText;

        ReleaseChangesStatusText = string.IsNullOrWhiteSpace(result.Message)
            ? "Release-Änderungen wurden ausgewertet."
            : result.Message;

        ReleaseChangesPreview = string.IsNullOrWhiteSpace(result.ChangesPreview)
            ? "Noch keine relevanten Änderungen ermittelt."
            : result.ChangesPreview;

        ExportText = string.IsNullOrWhiteSpace(result.ExportText) ? string.Empty : result.ExportText;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    private void UpdateTargetVersion()
    {
        var baseVersion = ResolveSelectedBaseVersion();
        if (!TryParseVersionParts(baseVersion, out var major, out var minor, out var patch))
        {
            TargetVersion = string.Empty;
            return;
        }

        TargetVersion = SelectedVersionBump switch
        {
            VersionBumpType.Major => $"{major + 1}.0.0",
            VersionBumpType.Minor => $"{major}.{minor + 1}.0",
            _ => $"{major}.{minor}.{patch + 1}"
        };
    }

    private string ResolveSelectedBaseVersion()
    {
        var candidates = new List<string>();

        if (BuildWpf && TryParseVersionParts(_detectedWpfVersionValue, out _, out _, out _))
        {
            candidates.Add(_detectedWpfVersionValue);
        }

        if (HasAndroidReleaseSelection && TryParseVersionParts(_detectedAndroidVersionValue, out _, out _, out _))
        {
            candidates.Add(_detectedAndroidVersionValue);
        }

        if (candidates.Count == 0)
        {
            return string.Empty;
        }

        return candidates.Aggregate(SelectHigherVersion);
    }

    private static string BuildCurrentVersionSummary(VersionDetectionResult result)
    {
        if (!string.IsNullOrWhiteSpace(result.CurrentVersion))
        {
            return result.IsCurrentVersionShared
                ? result.CurrentVersion
                : result.CurrentVersion;
        }

        if (result.IsWpfVersionDetected && result.IsAndroidVersionDetected)
        {
            return $"WPF {result.WpfVersion} / Android {result.AndroidVersion}";
        }

        if (result.IsWpfVersionDetected)
        {
            return $"WPF {result.WpfVersion}";
        }

        if (result.IsAndroidVersionDetected)
        {
            return $"Android {result.AndroidVersion}";
        }

        return "Nicht erkannt";
    }

    private static string SelectHigherVersion(string left, string right)
    {
        return CompareVersion(left, right) >= 0 ? left : right;
    }

    private static int CompareVersion(string left, string right)
    {
        if (!TryParseVersionParts(left, out var leftMajor, out var leftMinor, out var leftPatch))
        {
            return -1;
        }

        if (!TryParseVersionParts(right, out var rightMajor, out var rightMinor, out var rightPatch))
        {
            return 1;
        }

        var majorComparison = leftMajor.CompareTo(rightMajor);
        if (majorComparison != 0)
        {
            return majorComparison;
        }

        var minorComparison = leftMinor.CompareTo(rightMinor);
        if (minorComparison != 0)
        {
            return minorComparison;
        }

        return leftPatch.CompareTo(rightPatch);
    }

    private static bool TryParseVersionParts(string version, out int major, out int minor, out int patch)
    {
        major = 0;
        minor = 0;
        patch = 0;

        if (string.IsNullOrWhiteSpace(version))
        {
            return false;
        }

        var match = Regex.Match(version.Trim(), "^(?<major>\\d+)\\.(?<minor>\\d+)\\.(?<patch>\\d+)(?:[.-].*)?$");
        if (!match.Success)
        {
            return false;
        }

        return int.TryParse(match.Groups["major"].Value, out major)
               && int.TryParse(match.Groups["minor"].Value, out minor)
               && int.TryParse(match.Groups["patch"].Value, out patch);
    }

    private static void ValidateRequiredDirectory(string path, string label, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            errors.Add($"{label} ist ein Pflichtfeld.");
            return;
        }

        if (!Directory.Exists(path))
        {
            errors.Add($"{label} wurde lokal nicht gefunden: {path}");
        }
    }

    private static void ValidateRequiredPath(string path, string label, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            errors.Add($"{label} ist ein Pflichtfeld.");
            return;
        }

        if (!Path.IsPathRooted(path))
        {
            errors.Add($"{label} muss ein absoluter lokaler Pfad sein: {path}");
        }
    }

    private static void ValidateOptionalDirectory(string path, string label, List<string> errors)
    {
        if (!string.IsNullOrWhiteSpace(path) && !Directory.Exists(path))
        {
            errors.Add($"{label} wurde lokal nicht gefunden: {path}");
        }
    }

    private static void ValidateOptionalPath(string path, string label, List<string> errors)
    {
        if (!string.IsNullOrWhiteSpace(path) && !Path.IsPathRooted(path))
        {
            errors.Add($"{label} muss ein absoluter lokaler Pfad sein: {path}");
        }
    }
}
