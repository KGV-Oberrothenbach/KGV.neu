using KGV.Core.Interfaces;
using KGV.Core.Models;
using KGV.Core.Security;
using KGV.Helpers;
using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;

namespace KGV.ViewModels
{
    public sealed class FotoUploadTestViewModel : BaseViewModel, INavigationAware
    {
        private readonly IPhotoUploadTestService _photoUploadTestService;
        private readonly MainWindowViewModel _mainVm;
        private string _selectedFilePath = string.Empty;
        private string _selectedFileName = string.Empty;
        private string _selectedKind;
        private string _selectedMedium;
        private string _anlage = string.Empty;
        private string _garten = string.Empty;
        private string _zaehlernummer = string.Empty;
        private DateTime _datum = DateTime.Today;
        private string _statusMessage = string.Empty;
        private bool _isBusy;
        private PhotoUploadTestResult? _lastResult;

        public FotoUploadTestViewModel(IPhotoUploadTestService photoUploadTestService, MainWindowViewModel mainVm)
        {
            _photoUploadTestService = photoUploadTestService ?? throw new ArgumentNullException(nameof(photoUploadTestService));
            _mainVm = mainVm ?? throw new ArgumentNullException(nameof(mainVm));
            KindOptions = new ObservableCollection<string> { "ablesung", "ausbau", "einbau" };
            MediumOptions = new ObservableCollection<string> { "strom", "wasser" };
            _selectedKind = KindOptions[0];
            _selectedMedium = MediumOptions[0];
            SelectImageCommand = new RelayCommand<object?>(_ => SelectImage(), _ => !IsBusy && IsAuthorized);
            UploadCommand = new RelayCommand<object?>(_ => _ = UploadAsync(), _ => CanUpload);
        }

        public ObservableCollection<string> KindOptions { get; }
        public ObservableCollection<string> MediumOptions { get; }
        public RelayCommand<object?> SelectImageCommand { get; }
        public RelayCommand<object?> UploadCommand { get; }
        public string Title => "Foto-Upload testen";
        public string Description => "Temporäre Admin-Diagnosefläche für den echten Upload gegen `kgv-upload-photo`. Rohantworten und Transportfehler bleiben absichtlich sichtbar.";
        public bool IsAuthorized => _mainVm.UserContext.Role is UserRole.Admin or UserRole.Vorstand;
        public bool HasStatusMessage => !string.IsNullOrWhiteSpace(StatusMessage);
        public bool HasResult => LastResult != null;
        public bool CanUpload => IsAuthorized && !IsBusy && !string.IsNullOrWhiteSpace(_selectedFilePath) && !string.IsNullOrWhiteSpace(Anlage) && !string.IsNullOrWhiteSpace(Garten);

        public string SelectedFileName
        {
            get => _selectedFileName;
            private set => SetProperty(ref _selectedFileName, value);
        }

        public string SelectedKind
        {
            get => _selectedKind;
            set => SetProperty(ref _selectedKind, value);
        }

        public string SelectedMedium
        {
            get => _selectedMedium;
            set => SetProperty(ref _selectedMedium, value);
        }

        public string Anlage
        {
            get => _anlage;
            set
            {
                if (SetProperty(ref _anlage, value))
                    RaiseCommandStates();
            }
        }

        public string Garten
        {
            get => _garten;
            set
            {
                if (SetProperty(ref _garten, value))
                    RaiseCommandStates();
            }
        }

        public string Zaehlernummer
        {
            get => _zaehlernummer;
            set => SetProperty(ref _zaehlernummer, value);
        }

        public DateTime Datum
        {
            get => _datum;
            set => SetProperty(ref _datum, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            private set
            {
                if (SetProperty(ref _statusMessage, value))
                    OnPropertyChanged(nameof(HasStatusMessage));
            }
        }

        public bool IsBusy
        {
            get => _isBusy;
            private set
            {
                if (SetProperty(ref _isBusy, value))
                    RaiseCommandStates();
            }
        }

        public PhotoUploadTestResult? LastResult
        {
            get => _lastResult;
            private set
            {
                if (SetProperty(ref _lastResult, value))
                {
                    OnPropertyChanged(nameof(HasResult));
                    OnPropertyChanged(nameof(HttpStatusDisplay));
                    OnPropertyChanged(nameof(FileId));
                    OnPropertyChanged(nameof(ResultFileName));
                    OnPropertyChanged(nameof(RelativePath));
                    OnPropertyChanged(nameof(RawResponseBody));
                    OnPropertyChanged(nameof(ExceptionMessage));
                }
            }
        }

        public string HttpStatusDisplay => LastResult?.HttpStatusCode is int code
            ? $"{code} {LastResult.HttpStatusText}".Trim()
            : "—";
        public string FileId => string.IsNullOrWhiteSpace(LastResult?.FileId) ? "—" : LastResult!.FileId;
        public string ResultFileName => string.IsNullOrWhiteSpace(LastResult?.FileName) ? "—" : LastResult!.FileName;
        public string RelativePath => string.IsNullOrWhiteSpace(LastResult?.RelativePath) ? "—" : LastResult!.RelativePath;
        public string RawResponseBody => string.IsNullOrWhiteSpace(LastResult?.RawResponseBody) ? "—" : LastResult!.RawResponseBody;
        public string ExceptionMessage => string.IsNullOrWhiteSpace(LastResult?.ExceptionMessage) ? "—" : LastResult!.ExceptionMessage;

        public Task OnNavigatedFromAsync() => Task.CompletedTask;

        public Task OnNavigatedToAsync()
        {
            if (!IsAuthorized)
                StatusMessage = "Dieser Bereich ist nur für Admin oder Vorstand verfügbar.";

            return Task.CompletedTask;
        }

        private void SelectImage()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Bilddateien|*.jpg;*.jpeg;*.png;*.webp;*.bmp;*.gif|Alle Dateien|*.*",
                Multiselect = false,
                CheckFileExists = true
            };

            if (dialog.ShowDialog() != true)
                return;

            _selectedFilePath = dialog.FileName;
            SelectedFileName = Path.GetFileName(dialog.FileName);
            StatusMessage = string.Empty;
            RaiseCommandStates();
        }

        private async Task UploadAsync()
        {
            if (!CanUpload)
            {
                StatusMessage = "Bitte Bild, Anlage und Garten angeben.";
                return;
            }

            IsBusy = true;
            try
            {
                var fileBytes = await File.ReadAllBytesAsync(_selectedFilePath);
                var result = await _photoUploadTestService.UploadAsync(new PhotoUploadTestRequest
                {
                    FileName = SelectedFileName,
                    ContentType = GetContentType(_selectedFilePath),
                    FileContent = fileBytes,
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

        private void RaiseCommandStates()
        {
            OnPropertyChanged(nameof(CanUpload));
            SelectImageCommand.RaiseCanExecuteChanged();
            UploadCommand.RaiseCanExecuteChanged();
        }

        private static string GetContentType(string filePath)
        {
            var extension = Path.GetExtension(filePath)?.Trim().ToLowerInvariant();
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
    }
}
