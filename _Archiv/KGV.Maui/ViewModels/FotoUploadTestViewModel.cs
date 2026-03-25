using KGV.Core.Interfaces;
using KGV.Core.Models;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;

namespace KGV.Maui.ViewModels;

public sealed class FotoUploadTestViewModel : INotifyPropertyChanged
{
    private readonly IPhotoUploadTestService _photoUploadTestService;
    private readonly IAuthService _authService;
    private byte[]? _selectedFileContent;
    private string _selectedFileName = string.Empty;
    private string _selectedContentType = "application/octet-stream";
    private string _selectedKind;
    private string _selectedMedium;
    private string _anlage = string.Empty;
    private string _garten = string.Empty;
    private string _zaehlernummer = string.Empty;
    private DateTime _datum = DateTime.Today;
    private string _statusMessage = string.Empty;
    private bool _isBusy;
    private PhotoUploadTestResult? _lastResult;

    public FotoUploadTestViewModel(IPhotoUploadTestService photoUploadTestService, IAuthService authService)
    {
        _photoUploadTestService = photoUploadTestService;
        _authService = authService;
        KindOptions.Add("ablesung");
        KindOptions.Add("ausbau");
        KindOptions.Add("einbau");
        MediumOptions.Add("strom");
        MediumOptions.Add("wasser");
        _selectedKind = KindOptions[0];
        _selectedMedium = MediumOptions[0];
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<string> KindOptions { get; } = new();
    public ObservableCollection<string> MediumOptions { get; } = new();
    public bool IsAuthorized => _authService.IsAdmin || _authService.IsVorstand;
    public bool HasStatusMessage => !string.IsNullOrWhiteSpace(StatusMessage);
    public bool HasResult => LastResult != null;
    public bool CanPickImage => IsAuthorized && !IsBusy;
    public bool CanUpload => IsAuthorized && !IsBusy && _selectedFileContent?.Length > 0 && !string.IsNullOrWhiteSpace(Anlage) && !string.IsNullOrWhiteSpace(Garten);

    public string SelectedFileName
    {
        get => _selectedFileName;
        private set
        {
            if (_selectedFileName == value)
                return;

            _selectedFileName = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanPickImage));
            OnPropertyChanged(nameof(CanUpload));
        }
    }

    public string SelectedKind
    {
        get => _selectedKind;
        set
        {
            if (_selectedKind == value)
                return;

            _selectedKind = value;
            OnPropertyChanged();
        }
    }

    public string SelectedMedium
    {
        get => _selectedMedium;
        set
        {
            if (_selectedMedium == value)
                return;

            _selectedMedium = value;
            OnPropertyChanged();
        }
    }

    public string Anlage
    {
        get => _anlage;
        set
        {
            if (_anlage == value)
                return;

            _anlage = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanPickImage));
            OnPropertyChanged(nameof(CanUpload));
        }
    }

    public string Garten
    {
        get => _garten;
        set
        {
            if (_garten == value)
                return;

            _garten = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanPickImage));
            OnPropertyChanged(nameof(CanUpload));
        }
    }

    public string Zaehlernummer
    {
        get => _zaehlernummer;
        set
        {
            if (_zaehlernummer == value)
                return;

            _zaehlernummer = value;
            OnPropertyChanged();
        }
    }

    public DateTime Datum
    {
        get => _datum;
        set
        {
            if (_datum == value)
                return;

            _datum = value;
            OnPropertyChanged();
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set
        {
            if (_statusMessage == value)
                return;

            _statusMessage = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasStatusMessage));
        }
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (_isBusy == value)
                return;

            _isBusy = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanPickImage));
            OnPropertyChanged(nameof(CanUpload));
        }
    }

    public PhotoUploadTestResult? LastResult
    {
        get => _lastResult;
        private set
        {
            if (_lastResult == value)
                return;

            _lastResult = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasResult));
            OnPropertyChanged(nameof(HttpStatusDisplay));
            OnPropertyChanged(nameof(FileId));
            OnPropertyChanged(nameof(ResultFileName));
            OnPropertyChanged(nameof(RelativePath));
            OnPropertyChanged(nameof(RawResponseBody));
            OnPropertyChanged(nameof(ExceptionMessage));
        }
    }

    public string HttpStatusDisplay => LastResult?.HttpStatusCode is int code ? $"{code} {LastResult.HttpStatusText}".Trim() : "—";
    public string FileId => string.IsNullOrWhiteSpace(LastResult?.FileId) ? "—" : LastResult!.FileId;
    public string ResultFileName => string.IsNullOrWhiteSpace(LastResult?.FileName) ? "—" : LastResult!.FileName;
    public string RelativePath => string.IsNullOrWhiteSpace(LastResult?.RelativePath) ? "—" : LastResult!.RelativePath;
    public string RawResponseBody => string.IsNullOrWhiteSpace(LastResult?.RawResponseBody) ? "—" : LastResult!.RawResponseBody;
    public string ExceptionMessage => string.IsNullOrWhiteSpace(LastResult?.ExceptionMessage) ? "—" : LastResult!.ExceptionMessage;

    public Task InitializeAsync()
    {
        if (!IsAuthorized)
            StatusMessage = "Dieser Bereich ist nur für Admin oder Vorstand verfügbar.";

        return Task.CompletedTask;
    }

    public async Task PickImageAsync()
    {
        var fileResult = await FilePicker.Default.PickAsync(new PickOptions
        {
            PickerTitle = "Bild wählen",
            FileTypes = FilePickerFileType.Images
        });

        if (fileResult == null)
            return;

        await using var stream = await fileResult.OpenReadAsync();
        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream);
        _selectedFileContent = memoryStream.ToArray();
        _selectedContentType = string.IsNullOrWhiteSpace(fileResult.ContentType)
            ? GetContentType(fileResult.FileName)
            : fileResult.ContentType;
        SelectedFileName = fileResult.FileName;
        StatusMessage = string.Empty;
    }

    public async Task UploadAsync()
    {
        if (!CanUpload)
        {
            StatusMessage = "Bitte Bild, Anlage und Garten angeben.";
            return;
        }

        IsBusy = true;
        try
        {
            var result = await _photoUploadTestService.UploadAsync(new PhotoUploadTestRequest
            {
                FileName = SelectedFileName,
                ContentType = _selectedContentType,
                FileContent = _selectedFileContent ?? Array.Empty<byte>(),
                Kind = SelectedKind,
                Medium = SelectedMedium,
                Anlage = Anlage,
                Garten = Garten,
                Zaehlernummer = string.IsNullOrWhiteSpace(Zaehlernummer) ? null : Zaehlernummer.Trim(),
                Datum = Datum
            });

            LastResult = result;
            StatusMessage = result.Success
                ? "Upload erfolgreich abgeschlossen."
                : string.IsNullOrWhiteSpace(result.ErrorSummary)
                    ? "Upload fehlgeschlagen."
                    : result.ErrorSummary;
        }
        catch (Exception ex)
        {
            LastResult = new PhotoUploadTestResult
            {
                Success = false,
                ExceptionMessage = ex.Message
            };
            StatusMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private static string GetContentType(string? fileName)
    {
        var extension = Path.GetExtension(fileName)?.Trim().ToLowerInvariant();
        return extension switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".webp" => "image/webp",
            ".bmp" => "image/bmp",
            ".gif" => "image/gif",
            _ => "application/octet-stream"
        };
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
