using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using KGV.Core.Interfaces;
using KGV.Core.Models;
using KGV.Helpers;
using Microsoft.Win32;

namespace KGV.ViewModels
{
    public sealed class DokumenteViewModel : BaseViewModel, INavigationAware
    {
        private readonly ISupabaseService _supabaseService;
        private string _uploadTitel = string.Empty;
        private string _selectedFileName = string.Empty;
        private string _selectedFilePath = string.Empty;
        private string _statusMessage = string.Empty;
        private bool _isBusy;

        public DokumenteContext Context { get; }

        public ObservableCollection<DocumentInfo> MitgliedDokumente { get; } = new();

        public string UploadTitel
        {
            get => _uploadTitel;
            set
            {
                if (!SetProperty(ref _uploadTitel, value ?? string.Empty))
                    return;

                UploadCommand.RaiseCanExecuteChanged();
            }
        }

        public string SelectedFileName
        {
            get => _selectedFileName;
            private set
            {
                if (!SetProperty(ref _selectedFileName, value ?? string.Empty))
                    return;

                UploadCommand.RaiseCanExecuteChanged();
            }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            private set => SetProperty(ref _statusMessage, value ?? string.Empty);
        }

        public bool IsBusy
        {
            get => _isBusy;
            private set
            {
                if (!SetProperty(ref _isBusy, value))
                    return;

                SelectFileCommand.RaiseCanExecuteChanged();
                UploadCommand.RaiseCanExecuteChanged();
                RefreshCommand.RaiseCanExecuteChanged();
            }
        }

        public bool CanUpload => !IsBusy
            && Context.Member?.Id > 0
            && !string.IsNullOrWhiteSpace(UploadTitel)
            && !string.IsNullOrWhiteSpace(_selectedFilePath);

        public RelayCommand<object?> RefreshCommand { get; }
        public RelayCommand<DocumentInfo> OpenCommand { get; }
        public RelayCommand<object?> SelectFileCommand { get; }
        public RelayCommand<object?> UploadCommand { get; }

        public DokumenteViewModel(ISupabaseService supabaseService, DokumenteContext ctx)
        {
            _supabaseService = supabaseService;
            Context = ctx;

            RefreshCommand = new RelayCommand<object?>(_ => _ = LoadAsync(), _ => !IsBusy);
            OpenCommand = new RelayCommand<DocumentInfo>(doc =>
            {
                if (doc == null) return;
                _ = OpenAsync(doc);
            });
            SelectFileCommand = new RelayCommand<object?>(_ => SelectFile(), _ => !IsBusy);
            UploadCommand = new RelayCommand<object?>(_ => _ = UploadAsync(), _ => CanUpload);
        }

        public async Task OnNavigatedToAsync()
        {
            await LoadAsync();
        }

        public Task OnNavigatedFromAsync() => Task.CompletedTask;

        private async Task LoadAsync()
        {
            try
            {
                IsBusy = true;
                MitgliedDokumente.Clear();
                foreach (var d in await _supabaseService.GetMitgliedDokumenteAsync(Context.Member.Id))
                    MitgliedDokumente.Add(d);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Dokumente konnten nicht geladen werden: {ex.Message}", "Fehler", MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void SelectFile()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Dokumente|*.pdf;*.doc;*.docx;*.xls;*.xlsx;*.txt;*.rtf;*.jpg;*.jpeg;*.png;*.webp|Alle Dateien|*.*",
                Multiselect = false,
                CheckFileExists = true
            };

            if (dialog.ShowDialog() != true)
                return;

            _selectedFilePath = dialog.FileName;
            SelectedFileName = Path.GetFileName(dialog.FileName);
            if (string.IsNullOrWhiteSpace(UploadTitel))
                UploadTitel = Path.GetFileNameWithoutExtension(dialog.FileName);

            StatusMessage = string.Empty;
        }

        private async Task UploadAsync()
        {
            if (!CanUpload)
            {
                StatusMessage = "Bitte Titel und Dokumentdatei auswählen.";
                return;
            }

            IsBusy = true;
            try
            {
                var fileBytes = await File.ReadAllBytesAsync(_selectedFilePath);
                var result = await _supabaseService.CreateDokumentAsync(new DokumentUploadRequest
                {
                    MitgliedId = Context.Member.Id,
                    Titel = UploadTitel,
                    FileName = Path.GetFileName(_selectedFilePath),
                    MimeType = GetMimeType(_selectedFilePath),
                    FileContent = fileBytes
                });

                if (!result.Success)
                {
                    StatusMessage = result.Message;
                    return;
                }

                StatusMessage = "Dokument wurde hochgeladen.";
                _selectedFilePath = string.Empty;
                SelectedFileName = string.Empty;
                UploadTitel = string.Empty;
                await LoadAsync();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Upload fehlgeschlagen: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task OpenAsync(DocumentInfo? doc)
        {
            try
            {
                if (doc == null)
                    return;

                var url = await _supabaseService.ResolveDokumentOpenUrlAsync(doc, 3600);
                if (string.IsNullOrWhiteSpace(url))
                {
                    MessageBox.Show("Dokument konnte nicht geöffnet werden (kein URL).", "Fehler", MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    return;
                }

                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Öffnen fehlgeschlagen: {ex.Message}", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static string GetMimeType(string filePath)
        {
            return Path.GetExtension(filePath).ToLowerInvariant() switch
            {
                ".pdf" => "application/pdf",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".xls" => "application/vnd.ms-excel",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                ".txt" => "text/plain",
                ".rtf" => "application/rtf",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".webp" => "image/webp",
                _ => "application/octet-stream"
            };
        }
    }
}
