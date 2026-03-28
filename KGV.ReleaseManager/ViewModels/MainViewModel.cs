using System;
using System.ComponentModel;
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

    public void AppendStatus(string message)
    {
        var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}";
        StatusText = string.IsNullOrWhiteSpace(StatusText) ? line : $"{StatusText}{Environment.NewLine}{line}";
        FooterText = message;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
