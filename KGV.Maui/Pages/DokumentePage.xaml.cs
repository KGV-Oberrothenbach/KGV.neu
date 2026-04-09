using KGV.Core.Interfaces;
using KGV.Core.Models;
using KGV.Core.Security;
using KGV.Maui.State;
using Microsoft.Maui;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Storage;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;

namespace KGV.Maui.Pages;

public class DokumentePage : ContentPage, IQueryAttributable
{
    private enum DokumentOwnerScope
    {
        Mitglied,
        Parzelle
    }

    private readonly ISupabaseService _supabaseService;
    private readonly MemberContextState _memberContextState;
    private readonly ParzellenContextState _parzellenContextState;
    private readonly UserContextState _userContextState;
    private readonly ObservableCollection<DocumentInfo> _documents = new();
    private readonly Label _headlineLabel;
    private readonly Label _contextLabel;
    private readonly Label _hintLabel;
    private readonly Label _uploadSectionTitleLabel;
    private readonly Label _statusLabel;
    private readonly Label _emptyLabel;
    private readonly Entry _titelEntry;
    private readonly Label _selectedFileLabel;
    private readonly Border _uploadSection;
    private readonly Button _pickFileButton;
    private readonly Button _uploadButton;
    private readonly Button _refreshButton;
    private readonly ActivityIndicator _activityIndicator;
    private readonly CollectionView _documentsView;
    private bool _isBusy;
    private DokumentOwnerScope _requestedScope = DokumentOwnerScope.Mitglied;
    private int? _requestedParzelleId;
    private byte[]? _selectedFileContent;
    private string _selectedFileName = string.Empty;
    private string _selectedFileContentType = "application/octet-stream";

    public DokumentePage(ISupabaseService supabaseService, MemberContextState memberContextState, ParzellenContextState parzellenContextState, UserContextState userContextState)
    {
        _supabaseService = supabaseService;
        _memberContextState = memberContextState;
        _parzellenContextState = parzellenContextState;
        _userContextState = userContextState;

        Title = "Dokumente";

        _headlineLabel = new Label { FontSize = 24, FontAttributes = FontAttributes.Bold };
        _contextLabel = new Label { FontAttributes = FontAttributes.Bold, FontSize = 16 };
        _hintLabel = new Label { TextColor = Colors.Gray, LineBreakMode = LineBreakMode.WordWrap };
        _uploadSectionTitleLabel = new Label { FontAttributes = FontAttributes.Bold };
        _statusLabel = new Label { TextColor = Colors.DarkSlateBlue, LineBreakMode = LineBreakMode.WordWrap };
        _emptyLabel = new Label { Text = "Keine Mitgliedsdokumente gefunden.", TextColor = Colors.Gray, IsVisible = false };
        _titelEntry = new Entry { Placeholder = "Dokumenttitel" };
        _titelEntry.TextChanged += (_, _) => UpdateUiState();
        _selectedFileLabel = new Label { Text = "Noch keine Datei ausgewählt.", TextColor = Colors.Gray, LineBreakMode = LineBreakMode.WordWrap };
        _pickFileButton = new Button { Text = "Datei auswählen" };
        _pickFileButton.Clicked += async (_, _) => await PickFileAsync();
        _uploadButton = new Button { Text = "Upload starten" };
        _uploadButton.Clicked += async (_, _) => await UploadAsync();
        _refreshButton = new Button { Text = "Dokumente aktualisieren" };
        _refreshButton.Clicked += async (_, _) => await LoadAsync();
        _activityIndicator = new ActivityIndicator { Color = Colors.DarkSlateBlue, IsVisible = false, IsRunning = false };

        _documentsView = new CollectionView
        {
            ItemsSource = _documents,
            SelectionMode = SelectionMode.None,
            ItemTemplate = new DataTemplate(() =>
            {
                var title = new Label { FontAttributes = FontAttributes.Bold };
                title.SetBinding(Label.TextProperty, nameof(DocumentInfo.Title));

                var fileName = new Label { FontSize = 13, TextColor = Colors.Black, LineBreakMode = LineBreakMode.TailTruncation };
                fileName.SetBinding(Label.TextProperty, new Binding(nameof(DocumentInfo.Dateiname), stringFormat: "Datei: {0}"));

                var subtitle = new Label { FontSize = 12, TextColor = Colors.Gray, LineBreakMode = LineBreakMode.WordWrap };
                subtitle.BindingContextChanged += (_, _) =>
                {
                    if (subtitle.BindingContext is DocumentInfo document)
                        subtitle.Text = BuildDocumentMetaText(document);
                };

                var actionButton = new Button { Text = "Einsehen / Download" };
                actionButton.SetBinding(IsEnabledProperty, nameof(DocumentInfo.CanOpen));
                actionButton.Clicked += async (_, _) =>
                {
                    if (_isBusy)
                        return;

                    if (actionButton.BindingContext is DocumentInfo document)
                        await OpenDocumentAsync(document);
                };

                var deleteButton = new Button { Text = "Löschen" };
                deleteButton.SetBinding(IsEnabledProperty, nameof(DocumentInfo.CanDelete));
                deleteButton.SetBinding(IsVisibleProperty, new Binding(nameof(CanManageDocuments), source: this));
                deleteButton.Clicked += async (_, _) =>
                {
                    if (_isBusy)
                        return;

                    if (deleteButton.BindingContext is DocumentInfo document)
                        await DeleteDocumentAsync(document);
                };

                var uploadSignedButton = new Button { Text = "Signierte Fassung ablegen", IsVisible = false };
                uploadSignedButton.BindingContextChanged += (_, _) =>
                {
                    uploadSignedButton.IsVisible = CanManageDocuments
                        && uploadSignedButton.BindingContext is DocumentInfo document
                        && document.CanUploadSignedContractVersion;
                };
                uploadSignedButton.Clicked += async (_, _) =>
                {
                    if (_isBusy)
                        return;

                    if (uploadSignedButton.BindingContext is DocumentInfo document)
                        await UploadSignedContractVersionAsync(document);
                };

                return new Border
                {
                    Padding = 12,
                    Margin = new Thickness(0, 0, 0, 8),
                    Stroke = Colors.LightGray,
                    Content = new VerticalStackLayout
                    {
                        Spacing = 4,
                        Children =
                        {
                            title,
                            fileName,
                            subtitle,
                            new HorizontalStackLayout
                            {
                                Spacing = 8,
                                Children = { actionButton, deleteButton, uploadSignedButton }
                            }
                        }
                    }
                };
            })
        };

        _uploadSection = CreateSection(
            _uploadSectionTitleLabel,
            new Label { Text = "Titel", FontAttributes = FontAttributes.Bold },
            _titelEntry,
            new Label { Text = "Datei", FontAttributes = FontAttributes.Bold },
            _selectedFileLabel,
            _pickFileButton,
            _uploadButton);

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = 24,
                Spacing = 12,
                Children =
                {
                    _headlineLabel,
                    _contextLabel,
                    _hintLabel,
                    _uploadSection,
                    _refreshButton,
                    _activityIndicator,
                    _statusLabel,
                    _emptyLabel,
                    _documentsView
                }
            }
        };

        Appearing += async (_, _) => await LoadAsync();
        UpdateUiState();
    }

    private async Task UploadSignedContractVersionAsync(DocumentInfo document)
    {
        if (_isBusy)
            return;

        if (!CanManageDocuments)
        {
            SetStatus("Signierte Fassungen sind nur für Admin/Vorstand erlaubt.", success: false);
            UpdateUiState();
            return;
        }

        var context = await ResolveContextAsync();
        ApplyContext(context);
        if (!context.IsValid || context.Scope != DokumentOwnerScope.Mitglied || context.OwnerId is not > 0)
        {
            SetStatus("Bitte zuerst ein gültiges Mitglied auswählen.", success: false);
            UpdateUiState();
            return;
        }

        if (!document.CanUploadSignedContractVersion)
        {
            SetStatus("Bitte zuerst eine unsignierte Vertragsfassung auswählen.", success: false);
            UpdateUiState();
            return;
        }

        try
        {
            var file = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "Signierte Vertragsfassung auswählen"
            });

            if (file == null)
                return;

            await using var stream = await file.OpenReadAsync();
            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream);

            _isBusy = true;
            UpdateUiState();

            var result = await _supabaseService.UploadSignedVertragsdokumentAsync(
                context.OwnerId.Value,
                document,
                memoryStream.ToArray(),
                file.FileName ?? string.Empty,
                string.IsNullOrWhiteSpace(file.ContentType) ? GetContentType(file.FileName ?? string.Empty) : file.ContentType);

            if (!result.Success)
            {
                SetStatus(result.Message, success: false);
                return;
            }

            var reloaded = await TryReloadDocumentsAsync(context);
            SetStatus(reloaded
                ? "Signierte Vertragsfassung hochgeladen."
                : "Signierte Vertragsfassung hochgeladen. Bitte Liste aktualisieren.", success: true);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DokumentePage] UploadSignedContractVersionAsync failed: {ex}");
            SetStatus("Signierte Vertragsfassung konnte nicht hochgeladen werden.", success: false);
        }
        finally
        {
            _isBusy = false;
            UpdateUiState();
        }
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        var scope = TryGetQueryString(query, "scope");
        _requestedScope = string.Equals(scope, "parzelle", StringComparison.OrdinalIgnoreCase)
            ? DokumentOwnerScope.Parzelle
            : DokumentOwnerScope.Mitglied;
        _requestedParzelleId = TryGetQueryInt(query, "parzelleId");
        SetStatus(string.Empty, success: true);
        UpdateUiState();
    }

    private async Task LoadAsync()
    {
        if (_isBusy)
            return;

        _isBusy = true;
        try
        {
            _documents.Clear();
            _emptyLabel.IsVisible = false;
            var context = await ResolveContextAsync();
            ApplyContext(context);
            if (!context.IsValid)
            {
                SetStatus(context.ValidationMessage, success: false);
                return;
            }

            if (!CanReadContext(context))
            {
                _documents.Clear();
                _emptyLabel.IsVisible = false;
                SetStatus("Mit den aktuellen Rechten ist dieser Dokumente-Kontext nicht freigegeben.", success: false);
                return;
            }

            await ReloadDocumentsAsync(context);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DokumentePage] LoadAsync failed: {ex}");
            SetStatus("Dokumente konnten nicht geladen werden.", success: false);
        }
        finally
        {
            _isBusy = false;
            UpdateUiState();
        }
    }

    private async Task PickFileAsync()
    {
        if (_isBusy)
            return;

        try
        {
            var file = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "Dokument auswählen"
            });

            if (file == null)
            {
                SetStatus("Dateiauswahl abgebrochen.", success: false);
                return;
            }

            await using var stream = await file.OpenReadAsync();
            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream);

            _selectedFileContent = memoryStream.ToArray();
            _selectedFileName = file.FileName ?? string.Empty;
            _selectedFileContentType = string.IsNullOrWhiteSpace(file.ContentType)
                ? GetContentType(_selectedFileName)
                : file.ContentType;

            if (string.IsNullOrWhiteSpace(_titelEntry.Text))
                _titelEntry.Text = Path.GetFileNameWithoutExtension(_selectedFileName);

            _selectedFileLabel.Text = $"Ausgewählt: {_selectedFileName}";
            _selectedFileLabel.TextColor = Colors.Black;
            SetStatus("Datei bereit zum Upload.", success: true);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DokumentePage] PickFileAsync failed: {ex}");
            SetStatus("Dateiauswahl fehlgeschlagen.", success: false);
        }

        UpdateUiState();
    }

    private async Task UploadAsync()
    {
        if (_isBusy)
            return;

        if (!CanManageDocuments)
        {
            SetStatus("Upload ist nur für Admin/Vorstand erlaubt.", success: false);
            UpdateUiState();
            return;
        }

        var context = await ResolveContextAsync();
        ApplyContext(context);
        if (!context.IsValid)
        {
            SetStatus(context.ValidationMessage, success: false);
            UpdateUiState();
            return;
        }

        var titel = (_titelEntry.Text ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(titel))
        {
            SetStatus("Bitte einen Dokumenttitel eingeben.", success: false);
            UpdateUiState();
            return;
        }

        if (_selectedFileContent == null || _selectedFileContent.Length == 0 || string.IsNullOrWhiteSpace(_selectedFileName))
        {
            SetStatus("Bitte zuerst eine Datei auswählen.", success: false);
            UpdateUiState();
            return;
        }

        _isBusy = true;
        UpdateUiState();
        try
        {
            var request = new DokumentUploadRequest
            {
                MitgliedId = context.Scope == DokumentOwnerScope.Mitglied ? context.OwnerId : null,
                ParzelleId = context.Scope == DokumentOwnerScope.Parzelle ? context.OwnerId : null,
                Titel = titel,
                FileName = _selectedFileName,
                MimeType = _selectedFileContentType,
                FileContent = _selectedFileContent
            };

            var result = await _supabaseService.CreateDokumentAsync(request);
            if (!result.Success)
            {
                SetStatus(result.Message, success: false);
                return;
            }

            var reloaded = await TryReloadDocumentsAsync(context);
            ResetUploadInputs(clearTitle: false);
            SetStatus(reloaded
                ? "Dokument hochgeladen."
                : "Dokument hochgeladen. Bitte Liste aktualisieren.", success: true);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DokumentePage] UploadAsync failed: {ex}");
            SetStatus("Dokument konnte nicht hochgeladen werden.", success: false);
        }
        finally
        {
            _isBusy = false;
            UpdateUiState();
        }
    }

    private async Task OpenDocumentAsync(DocumentInfo document)
    {
        if (_isBusy)
            return;

        try
        {
            if (!document.CanOpen)
            {
                SetStatus("Dieses Dokument ist noch nicht vollständig zum Öffnen verknüpft.", success: false);
                return;
            }

            var url = await _supabaseService.ResolveDokumentOpenUrlAsync(document, 3600);
            if (string.IsNullOrWhiteSpace(url))
            {
                SetStatus("Dokument konnte aktuell nicht geöffnet werden.", success: false);
                return;
            }

            await Launcher.Default.OpenAsync(url);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DokumentePage] OpenDocumentAsync failed: {ex}");
            SetStatus("Dokument konnte aktuell nicht geöffnet werden.", success: false);
        }
    }

    private async Task DeleteDocumentAsync(DocumentInfo document)
    {
        if (_isBusy)
            return;

        if (!CanManageDocuments)
        {
            SetStatus("Löschen ist nur für Admin/Vorstand erlaubt.", success: false);
            UpdateUiState();
            return;
        }

        if (!document.CanDelete)
        {
            SetStatus("Dokument kann aktuell nicht gelöscht werden.", success: false);
            return;
        }

        var confirmed = await DisplayAlert(
            "Dokument löschen",
            $"Dokument '{GetDocumentDisplayName(document)}' wirklich löschen?",
            "Löschen",
            "Abbrechen");
        if (!confirmed)
            return;

        var context = await ResolveContextAsync();
        ApplyContext(context);
        if (!context.IsValid)
        {
            SetStatus(context.ValidationMessage, success: false);
            UpdateUiState();
            return;
        }

        _isBusy = true;
        UpdateUiState();
        try
        {
            var result = await _supabaseService.DeleteDokumentAsync(document);
            if (!result.Success)
            {
                SetStatus(result.Message, success: false);
                return;
            }

            var reloaded = await TryReloadDocumentsAsync(context);
            SetStatus(reloaded
                ? "Dokument gelöscht."
                : "Dokument gelöscht. Bitte Liste aktualisieren.", success: true);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DokumentePage] DeleteDocumentAsync failed: {ex}");
            SetStatus("Dokument konnte aktuell nicht gelöscht werden.", success: false);
        }
        finally
        {
            _isBusy = false;
            UpdateUiState();
        }
    }

    private async Task<bool> TryReloadDocumentsAsync(DokumentPageContext context)
    {
        try
        {
            await ReloadDocumentsAsync(context);
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DokumentePage] TryReloadDocumentsAsync failed: {ex}");
            return false;
        }
    }

    private async Task ReloadDocumentsAsync(DokumentPageContext context)
    {
        var documents = context.Scope == DokumentOwnerScope.Parzelle
            ? await _supabaseService.GetParzelleDokumenteAsync(context.OwnerId!.Value)
            : await _supabaseService.GetMitgliedDokumenteAsync(context.OwnerId!.Value);

        _documents.Clear();
        foreach (var document in documents)
            _documents.Add(document);

        _emptyLabel.IsVisible = _documents.Count == 0;
    }

    private async Task<DokumentPageContext> ResolveContextAsync()
    {
        if (_requestedScope == DokumentOwnerScope.Parzelle)
        {
            var parzelleId = _requestedParzelleId ?? _parzellenContextState.SelectedParzelleId;
            if (parzelleId is not > 0)
            {
                return DokumentPageContext.Invalid(
                    DokumentOwnerScope.Parzelle,
                    "Keine Parzellendokumente verfügbar",
                    "Bitte zuerst eine gültige Parzelle auswählen.",
                    "Bitte zuerst im Parzellenpfad eine Parzelle auswählen.");
            }

            var detail = await _supabaseService.GetParzelleDetailAsync(parzelleId.Value);
            var displayName = detail?.DisplayName ?? $"Parzelle #{parzelleId.Value}";
            return DokumentPageContext.Valid(
                DokumentOwnerScope.Parzelle,
                parzelleId.Value,
                $"Parzellen-Dokumente – {displayName}",
                displayName,
                BuildContextHint(DokumentOwnerScope.Parzelle),
                "Keine Dokumente für diese Parzelle gefunden.");
        }

        var member = _memberContextState.SelectedMember;
        if (member?.Id is not > 0)
        {
            return DokumentPageContext.Invalid(
                DokumentOwnerScope.Mitglied,
                "Keine Mitgliedsdokumente verfügbar",
                "Bitte zuerst ein gültiges Mitglied auswählen.",
                "Bitte zuerst im Mitgliedspfad ein Mitglied auswählen.");
        }

        return DokumentPageContext.Valid(
            DokumentOwnerScope.Mitglied,
            member.Id,
            $"Dokumente – {member.DisplayName}",
            member.DisplayName,
            BuildContextHint(DokumentOwnerScope.Mitglied),
            "Keine Mitgliedsdokumente gefunden.");
    }

    private void ApplyContext(DokumentPageContext context)
    {
        _headlineLabel.Text = context.Headline;
        _contextLabel.Text = context.ContextLabel;
        _hintLabel.Text = BuildContextHint(context);
        _emptyLabel.Text = context.EmptyText;
        _uploadSectionTitleLabel.Text = context.Scope == DokumentOwnerScope.Parzelle
            ? "Parzellendokument hochladen"
            : "Mitgliedsdokument hochladen";
    }

    private void ResetUploadInputs(bool clearTitle)
    {
        if (clearTitle)
            _titelEntry.Text = string.Empty;

        _selectedFileContent = null;
        _selectedFileName = string.Empty;
        _selectedFileContentType = "application/octet-stream";
        _selectedFileLabel.Text = "Noch keine Datei ausgewählt.";
        _selectedFileLabel.TextColor = Colors.Gray;
    }

    private void SetStatus(string message, bool success)
    {
        _statusLabel.Text = message ?? string.Empty;
        _statusLabel.TextColor = success ? Colors.DarkSlateBlue : Colors.DarkRed;
    }

    private void UpdateUiState()
    {
        var canManageDocuments = CanManageDocuments;
        var canReadDocuments = CanReadRequestedContext();
        var hasContext = _requestedScope == DokumentOwnerScope.Parzelle
            ? (_requestedParzelleId ?? _parzellenContextState.SelectedParzelleId) is > 0
            : _memberContextState.SelectedMember?.Id is > 0;
        var canUpload = !_isBusy
            && canManageDocuments
            && canReadDocuments
            && hasContext
            && !string.IsNullOrWhiteSpace(_titelEntry.Text)
            && _selectedFileContent is { Length: > 0 }
            && !string.IsNullOrWhiteSpace(_selectedFileName);

        _uploadSection.IsVisible = canManageDocuments && canReadDocuments;
        _titelEntry.IsEnabled = !_isBusy && hasContext && canManageDocuments && canReadDocuments;
        _pickFileButton.IsEnabled = !_isBusy && hasContext && canManageDocuments && canReadDocuments;
        _uploadButton.IsEnabled = canUpload;
        _refreshButton.IsEnabled = !_isBusy && canReadDocuments;
        _documentsView.IsEnabled = !_isBusy && canReadDocuments;
        _documentsView.IsVisible = canReadDocuments;
        _activityIndicator.IsVisible = _isBusy;
        _activityIndicator.IsRunning = _isBusy;
    }

    public bool CanManageDocuments => _userContextState.CurrentUserContext?.Has(PermissionFlags.CanManageDocuments) == true;

    private string BuildContextHint(DokumentPageContext context)
    {
        var subject = context.Scope == DokumentOwnerScope.Parzelle
            ? "die aktuell ausgewählte Parzelle"
            : "das aktuell ausgewählte Mitglied";

        if (!CanReadContext(context))
            return $"Für {subject} ist der Dokumente-Zugriff mit dem aktuellen Rechtekontext nicht freigegeben.";

        return CanManageDocuments
            ? $"Es werden nur die Dokumente für {subject} angezeigt. Upload, Öffnen und Löschen laufen über den gemeinsamen Google-Drive-Dokumentpfad."
            : $"Es werden nur die Dokumente für {subject} angezeigt. Öffnen und Aktualisieren bleiben verfügbar.";
    }

    private string BuildContextHint(DokumentOwnerScope scope)
    {
        var subject = scope == DokumentOwnerScope.Parzelle
            ? "die aktuell ausgewählte Parzelle"
            : "das aktuell ausgewählte Mitglied";

        return CanManageDocuments
            ? $"Es werden nur die Dokumente für {subject} angezeigt. Upload, Öffnen und Löschen laufen über den gemeinsamen Google-Drive-Dokumentpfad."
            : $"Es werden nur die Dokumente für {subject} angezeigt. Öffnen und Aktualisieren bleiben verfügbar.";
    }

    private bool CanReadRequestedContext()
    {
        var userContext = _userContextState.CurrentUserContext;
        if (_requestedScope == DokumentOwnerScope.Parzelle)
        {
            if (PermissionChecks.CanReadDocuments(userContext))
                return true;

            return PermissionChecks.CanReadDocumentsForMember(userContext, _memberContextState.SelectedMember?.Id);
        }

        return PermissionChecks.CanReadDocumentsForMember(userContext, _memberContextState.SelectedMember?.Id);
    }

    private bool CanReadContext(DokumentPageContext context)
    {
        if (!context.IsValid)
            return false;

        var userContext = _userContextState.CurrentUserContext;
        if (context.Scope == DokumentOwnerScope.Mitglied)
            return PermissionChecks.CanReadDocumentsForMember(userContext, context.OwnerId);

        if (PermissionChecks.CanReadDocuments(userContext))
            return true;

        return PermissionChecks.CanReadDocumentsForMember(userContext, _memberContextState.SelectedMember?.Id);
    }

    private static Border CreateSection(View titleView, params View[] children)
    {
        var stack = new VerticalStackLayout { Spacing = 8 };
        stack.Children.Add(titleView);
        foreach (var child in children)
            stack.Children.Add(child);

        return new Border
        {
            Stroke = Colors.LightGray,
            Padding = 12,
            Content = stack
        };
    }

    private static string BuildDocumentMetaText(DocumentInfo document)
    {
        var formularMeta = document.FormularDokumentTypAnzeige == "-"
            ? string.Empty
            : $"Typ: {document.FormularDokumentTypAnzeige} · Status: {document.FormularDokumentStatusAnzeige} · ";
        var updated = document.UpdatedAt.HasValue
            ? $"Aktualisiert: {document.UpdatedAt.Value:dd.MM.yyyy HH:mm}"
            : "Aktualisiert: -";
        var size = document.Size.HasValue
            ? FormatFileSize(document.Size.Value)
            : "Größe unbekannt";
        return $"{formularMeta}{updated} · {size}";
    }

    private static string GetDocumentDisplayName(DocumentInfo document)
        => !string.IsNullOrWhiteSpace(document.Title)
            ? document.Title
            : (!string.IsNullOrWhiteSpace(document.Dateiname) ? document.Dateiname : "dieses Dokument");

    private static string FormatFileSize(long bytes)
    {
        if (bytes < 1024)
            return $"{bytes} B";
        if (bytes < 1024 * 1024)
            return $"{bytes / 1024d:0.#} KB";
        if (bytes < 1024 * 1024 * 1024)
            return $"{bytes / 1024d / 1024d:0.#} MB";

        return $"{bytes / 1024d / 1024d / 1024d:0.#} GB";
    }

    private static string GetContentType(string fileName)
    {
        return Path.GetExtension(fileName ?? string.Empty).ToLowerInvariant() switch
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

    private static int? TryGetQueryInt(IDictionary<string, object> query, string key)
    {
        var raw = TryGetQueryString(query, key);
        return int.TryParse(raw, out var value) && value > 0 ? value : null;
    }

    private static string? TryGetQueryString(IDictionary<string, object> query, string key)
    {
        if (!query.TryGetValue(key, out var raw) || raw == null)
            return null;

        var value = raw.ToString();
        return string.IsNullOrWhiteSpace(value) ? null : Uri.UnescapeDataString(value);
    }

    private sealed class DokumentPageContext
    {
        private DokumentPageContext()
        {
        }

        public bool IsValid { get; private init; }
        public DokumentOwnerScope Scope { get; private init; }
        public int? OwnerId { get; private init; }
        public string Headline { get; private init; } = string.Empty;
        public string ContextLabel { get; private init; } = string.Empty;
        public string HintText { get; private init; } = string.Empty;
        public string EmptyText { get; private init; } = string.Empty;
        public string ValidationMessage { get; private init; } = string.Empty;

        public static DokumentPageContext Valid(DokumentOwnerScope scope, int ownerId, string headline, string contextLabel, string hintText, string emptyText)
            => new()
            {
                IsValid = true,
                Scope = scope,
                OwnerId = ownerId,
                Headline = headline,
                ContextLabel = contextLabel,
                HintText = hintText,
                EmptyText = emptyText,
                ValidationMessage = string.Empty
            };

        public static DokumentPageContext Invalid(DokumentOwnerScope scope, string headline, string hintText, string validationMessage)
            => new()
            {
                IsValid = false,
                Scope = scope,
                OwnerId = null,
                Headline = headline,
                ContextLabel = string.Empty,
                HintText = hintText,
                EmptyText = string.Empty,
                ValidationMessage = validationMessage
            };
    }
}
