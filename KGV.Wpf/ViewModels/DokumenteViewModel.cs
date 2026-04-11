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
        private readonly bool _canManageDocuments;
        private string _uploadTitel = string.Empty;
        private string _selectedFileName = string.Empty;
        private string _selectedFilePath = string.Empty;
        private string _statusMessage = string.Empty;
        private bool _isBusy;

        public DokumenteContext Context { get; }

        public ObservableCollection<DocumentInfo> MitgliedDokumente { get; } = new();

        public string KontextTitel => "Mitgliedsdokumente";

        public string KontextBeschreibung => Context.Member?.Id > 0
            ? $"Dokumente für {Context.Member.Nachname} {Context.Member.Vorname} (Mitglied-ID: {Context.Member.Id})"
            : "Es ist aktuell kein gültiges Mitglied für Dokumente ausgewählt.";

        public string EmptyStateMessage => "Für dieses Mitglied sind noch keine Dokumente vorhanden.";

        public bool HasDokumente => MitgliedDokumente.Count > 0;

        public bool HasNoDokumente => IsContextValid && !HasDokumente;

        public bool HasStatusMessage => !string.IsNullOrWhiteSpace(StatusMessage);

        public bool IsContextValid => Context.Member?.Id > 0;

        public bool CanManageDocuments => _canManageDocuments;

        public bool CanEditUpload => CanManageDocuments && !IsBusy && IsContextValid;

        public string VertragsDokumentHinweis
            => "Für Vertragsdokumente laden Sie hier signierte Scan-Fassungen hoch. Die direkte digitale Signatur bleibt MAUI vorbehalten; die unsignierte Fassung bleibt erhalten.";

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
            private set
            {
                if (!SetProperty(ref _statusMessage, value ?? string.Empty))
                    return;

                OnPropertyChanged(nameof(HasStatusMessage));
            }
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
                OpenCommand.RaiseCanExecuteChanged();
                DeleteCommand.RaiseCanExecuteChanged();
                OnPropertyChanged(nameof(CanEditUpload));
                OnPropertyChanged(nameof(HasNoDokumente));
            }
        }

        public bool CanUpload => !IsBusy
            && CanManageDocuments
            && Context.Member?.Id > 0
            && !string.IsNullOrWhiteSpace(UploadTitel)
            && !string.IsNullOrWhiteSpace(_selectedFilePath);

        public RelayCommand<object?> RefreshCommand { get; }
        public RelayCommand<DocumentInfo> OpenCommand { get; }
        public RelayCommand<DocumentInfo> DeleteCommand { get; }
        public RelayCommand<DocumentInfo> UploadSignedVersionCommand { get; }
        public RelayCommand<object?> SelectFileCommand { get; }
        public RelayCommand<object?> UploadCommand { get; }

        public DokumenteViewModel(ISupabaseService supabaseService, DokumenteContext ctx, bool canManageDocuments)
        {
            _supabaseService = supabaseService;
            Context = ctx;
            _canManageDocuments = canManageDocuments;

            RefreshCommand = new RelayCommand<object?>(_ => _ = LoadAsync(), _ => !IsBusy);
            OpenCommand = new RelayCommand<DocumentInfo>(doc =>
            {
                if (doc == null) return;
                _ = OpenAsync(doc);
            }, doc => !IsBusy && doc?.CanOpen == true);
            DeleteCommand = new RelayCommand<DocumentInfo>(doc =>
            {
                if (doc == null) return;
                _ = DeleteAsync(doc);
            }, doc => !IsBusy && CanManageDocuments && doc?.CanDelete == true);
            UploadSignedVersionCommand = new RelayCommand<DocumentInfo>(doc =>
            {
                if (doc == null) return;
                _ = UploadSignedVersionAsync(doc);
            }, doc => !IsBusy && CanManageDocuments && IsContextValid && doc?.CanUploadSignedContractVersion == true);
            SelectFileCommand = new RelayCommand<object?>(_ => SelectFile(), _ => CanEditUpload);
            UploadCommand = new RelayCommand<object?>(_ => _ = UploadAsync(), _ => CanUpload);
        }

        public async Task OnNavigatedToAsync()
        {
            await LoadAsync();
        }

        public Task OnNavigatedFromAsync() => Task.CompletedTask;

        private async Task<bool> LoadAsync(bool showDialogOnError = true)
        {
            if (!IsContextValid)
            {
                MitgliedDokumente.Clear();
                OnPropertyChanged(nameof(HasDokumente));
                OnPropertyChanged(nameof(HasNoDokumente));
                StatusMessage = "Bitte zuerst ein gültiges Mitglied auswählen.";
                return false;
            }

            try
            {
                IsBusy = true;
                MitgliedDokumente.Clear();
                foreach (var d in await _supabaseService.GetMitgliedDokumenteAsync(Context.Member.Id))
                    MitgliedDokumente.Add(d);

                OnPropertyChanged(nameof(HasDokumente));
                OnPropertyChanged(nameof(HasNoDokumente));
                return true;
            }
            catch (Exception ex)
            {
                StatusMessage = "Dokumente konnten nicht geladen werden.";
                if (showDialogOnError)
                {
                    MessageBox.Show($"Dokumente konnten nicht geladen werden: {ex.Message}", "Fehler", MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }

                return false;
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task UploadSignedVersionAsync(DocumentInfo? doc)
        {
            if (!CanManageDocuments)
            {
                StatusMessage = "Signierte Fassungen sind nur für Admin/Vorstand erlaubt.";
                MessageBox.Show(StatusMessage, "Hinweis", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (doc == null || Context.Member?.Id is not > 0 || !doc.CanUploadSignedContractVersion)
            {
                StatusMessage = "Bitte zuerst eine unsignierte Vertragsfassung auswählen.";
                return;
            }

            var dialog = new OpenFileDialog
            {
                Filter = "PDF-Dateien|*.pdf",
                Multiselect = false,
                CheckFileExists = true,
                Title = "Signierte Vertragsfassung auswählen"
            };

            if (dialog.ShowDialog() != true)
                return;

            IsBusy = true;
            try
            {
                var filePath = dialog.FileName;
                var fileBytes = await File.ReadAllBytesAsync(filePath);
                var result = await _supabaseService.UploadSignedVertragsdokumentAsync(
                    Context.Member.Id,
                    doc,
                    fileBytes,
                    Path.GetFileName(filePath),
                    GetMimeType(filePath));

                if (!result.Success)
                {
                    StatusMessage = result.Message;
                    return;
                }

                var reloaded = await LoadAsync(showDialogOnError: false);
                StatusMessage = reloaded
                    ? "Signierte Vertragsfassung hochgeladen. Die unsignierte Fassung bleibt erhalten."
                    : "Signierte Vertragsfassung hochgeladen. Die unsignierte Fassung bleibt erhalten. Bitte Liste aktualisieren.";
            }
            catch (Exception)
            {
                StatusMessage = "Signierte Vertragsfassung konnte nicht hochgeladen werden.";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void SelectFile()
        {
            if (!CanManageDocuments)
            {
                StatusMessage = "Dokumente sind hier nur lesbar.";
                MessageBox.Show("Dokumente sind hier nur lesbar.", "Hinweis", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

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
            if (!CanManageDocuments)
            {
                StatusMessage = "Upload ist nur für Admin/Vorstand erlaubt.";
                MessageBox.Show("Upload ist nur für Admin/Vorstand erlaubt.", "Hinweis", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

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

                _selectedFilePath = string.Empty;
                SelectedFileName = string.Empty;
                var reloaded = await LoadAsync(showDialogOnError: false);
                StatusMessage = reloaded
                    ? "Dokument hochgeladen."
                    : "Dokument hochgeladen. Bitte Liste aktualisieren.";
            }
            catch (Exception)
            {
                StatusMessage = "Dokument konnte nicht hochgeladen werden.";
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

                if (!doc.CanOpen)
                {
                    MessageBox.Show("Dieses Dokument ist noch nicht vollständig zum Öffnen verknüpft.", "Hinweis", MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    return;
                }

                var url = await _supabaseService.ResolveDokumentOpenUrlAsync(doc, 3600);
                if (string.IsNullOrWhiteSpace(url))
                {
                    MessageBox.Show("Dokument konnte aktuell nicht geöffnet werden.", "Fehler", MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    return;
                }

                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch (Exception)
            {
                MessageBox.Show("Dokument konnte aktuell nicht geöffnet werden.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task DeleteAsync(DocumentInfo? doc)
        {
            if (!CanManageDocuments)
            {
                StatusMessage = "Löschen ist nur für Admin/Vorstand erlaubt.";
                MessageBox.Show("Löschen ist nur für Admin/Vorstand erlaubt.", "Hinweis", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (doc == null || !doc.CanDelete || IsBusy)
                return;

            var name = !string.IsNullOrWhiteSpace(doc.Title) ? doc.Title : doc.Dateiname;
            var confirmed = MessageBox.Show(
                    $"Dokument '{name}' wirklich löschen?",
                    "Dokument löschen",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning)
                == MessageBoxResult.Yes;
            if (!confirmed)
                return;

            IsBusy = true;
            try
            {
                var result = await _supabaseService.DeleteDokumentAsync(doc);
                if (!result.Success)
                {
                    StatusMessage = result.Message;
                    return;
                }

                var reloaded = await LoadAsync(showDialogOnError: false);
                StatusMessage = reloaded
                    ? "Dokument gelöscht."
                    : "Dokument gelöscht. Bitte Liste aktualisieren.";
            }
            catch (Exception)
            {
                StatusMessage = "Dokument konnte aktuell nicht gelöscht werden.";
            }
            finally
            {
                IsBusy = false;
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
