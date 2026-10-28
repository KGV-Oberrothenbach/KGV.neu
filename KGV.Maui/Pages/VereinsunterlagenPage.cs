using System.Collections.ObjectModel;
using System.Net.Http;
using KGV.Core.Interfaces;
using KGV.Core.Models;
using KGV.Core.Security;
using KGV.Core.Utilities;
using KGV.Maui.State;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Devices;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Storage;

namespace KGV.Maui.Pages;

public sealed class VereinsunterlagenPage : ContentPage
{
    private readonly ISupabaseService _supabaseService;
    private readonly UserContextState _userContextState;
    private readonly ObservableCollection<DocumentTypeItem> _documentTypes = new();
    private readonly VerticalStackLayout _formHost = new();
    private readonly Label _statusLabel = new() { LineBreakMode = LineBreakMode.WordWrap };
    private bool _isBusy;
    private DocumentTypeItem? _editingType;
    private Entry? _fassungEntry;
    private DatePicker? _gueltigAbPicker;
    private FileResult? _selectedFile;
    private Label? _selectedFileLabel;

    public VereinsunterlagenPage(ISupabaseService supabaseService, UserContextState userContextState)
    {
        _supabaseService = supabaseService;
        _userContextState = userContextState;
        Title = "Vereinsunterlagen";

        var documentTypesView = new CollectionView
        {
            ItemsSource = _documentTypes,
            SelectionMode = SelectionMode.None,
            ItemTemplate = new DataTemplate(CreateDocumentTypeView)
        };

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = 24,
                Spacing = 14,
                Children =
                {
                    new Label { Text = "Verwaltung", FontSize = 24, FontAttributes = FontAttributes.Bold },
                    new Label { Text = "Vereinsunterlagen", FontSize = 18, FontAttributes = FontAttributes.Bold },
                    new Label
                    {
                        Text = "Verwalten Sie Satzung und Ordnungen als unveränderliche Fassungen. Inhaltliche Änderungen werden immer als neue Fassung angelegt.",
                        TextColor = Colors.Gray,
                        LineBreakMode = LineBreakMode.WordWrap
                    },
                    _statusLabel,
                    documentTypesView,
                    _formHost
                }
            }
        };

        Appearing += async (_, _) => await LoadAsync();
    }

    private bool CanManage => _userContextState.CurrentUserContext?.Role is UserRole.Admin or UserRole.Vorstand;

    private async Task LoadAsync()
    {
        if (_isBusy)
            return;

        if (!CanManage)
        {
            _documentTypes.Clear();
            SetStatus("Für Vereinsunterlagen fehlt die Berechtigung.", true);
            return;
        }

        try
        {
            _isBusy = true;
            SetStatus(string.Empty);
            var types = await _supabaseService.GetVereinsdokumentTypenAsync();
            var now = DateTime.Today;

            var loaded = new List<DocumentTypeItem>();
            foreach (var type in types)
                loaded.Add(new DocumentTypeItem(type, await _supabaseService.GetVereinsdokumentFassungenAsync(type.Code), now));

            _documentTypes.Clear();
            foreach (var item in loaded)
                _documentTypes.Add(item);
        }
        catch (Exception ex)
        {
            SetStatus(ToUserMessage(ex, "Vereinsunterlagen konnten nicht geladen werden."), true);
        }
        finally
        {
            _isBusy = false;
        }
    }

    private View CreateDocumentTypeView()
    {
        var title = new Label { FontSize = 18, FontAttributes = FontAttributes.Bold };
        title.SetBinding(Label.TextProperty, nameof(DocumentTypeItem.Title));
        var relevant = new Label { TextColor = Colors.DarkSlateBlue, LineBreakMode = LineBreakMode.WordWrap };
        relevant.SetBinding(Label.TextProperty, nameof(DocumentTypeItem.RelevantVersion));
        var add = new Button { Text = "Neue Fassung", HorizontalOptions = LayoutOptions.End };
        add.Clicked += (_, _) =>
        {
            if ((add.BindingContext as DocumentTypeItem) is { } item)
                OpenForm(item);
        };
        Grid.SetColumn(add, 1);

        var history = new CollectionView
        {
            SelectionMode = SelectionMode.None,
            HeightRequest = 230,
            ItemTemplate = new DataTemplate(() =>
            {
                var version = new Label { FontAttributes = FontAttributes.Bold };
                version.SetBinding(Label.TextProperty, nameof(DocumentVersionItem.Fassung));
                var details = new Label { FontSize = 12, TextColor = Colors.Gray, LineBreakMode = LineBreakMode.WordWrap };
                details.SetBinding(Label.TextProperty, nameof(DocumentVersionItem.Details));
                var status = new Label { FontSize = 12, TextColor = Colors.DarkSlateBlue };
                status.SetBinding(Label.TextProperty, nameof(DocumentVersionItem.Status));
                return new Border
                {
                    Stroke = Colors.LightGray,
                    Padding = 10,
                    Margin = new Thickness(0, 0, 0, 6),
                    Content = new VerticalStackLayout { Spacing = 3, Children = { version, status, details } }
                };
            })
        };
        history.SetBinding(ItemsView.ItemsSourceProperty, nameof(DocumentTypeItem.Versions));

        return new Border
        {
            Stroke = Colors.LightGray,
            Padding = 16,
            Margin = new Thickness(0, 0, 0, 10),
            Content = new VerticalStackLayout
            {
                Spacing = 9,
                Children =
                {
                    new Grid
                    {
                        ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) },
                        Children = { title, add }
                    },
                    relevant,
                    new Label { Text = "Fassungshistorie", FontAttributes = FontAttributes.Bold, FontSize = 14 },
                    history
                }
            }
        };
    }

    private void OpenForm(DocumentTypeItem type)
    {
        if (!CanManage || _isBusy)
            return;

        _editingType = type;
        _selectedFile = null;
        _fassungEntry = new Entry { Placeholder = "z. B. Fassung 2026" };
        _gueltigAbPicker = new DatePicker { Date = DateTime.Today, Format = "dd.MM.yyyy" };
        _selectedFileLabel = new Label { Text = "Keine PDF-Datei ausgewählt.", TextColor = Colors.Gray, LineBreakMode = LineBreakMode.WordWrap };
        var chooseFile = new Button { Text = "PDF-Datei auswählen" };
        chooseFile.Clicked += async (_, _) => await PickPdfAsync();
        var save = new Button { Text = "Angaben prüfen und speichern" };
        save.Clicked += async (_, _) => await ReviewAndSaveAsync();
        var cancel = new Button { Text = "Abbrechen" };
        cancel.Clicked += (_, _) => CloseForm();

        _formHost.Children.Clear();
        _formHost.Children.Add(CreateSection(
            $"Neue Fassung: {type.Type.Bezeichnung}",
            CreateField("Dokumenttyp", new Entry { Text = type.Type.Bezeichnung, IsReadOnly = true }),
            CreateField("Fassung / Bezeichnung", _fassungEntry),
            CreateField("Gültig ab", _gueltigAbPicker),
            chooseFile,
            _selectedFileLabel,
            save,
            cancel));
    }

    private async Task PickPdfAsync()
    {
        try
        {
            var pdfTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
            {
                [DevicePlatform.Android] = new[] { "application/pdf" }
            });
            var file = await FilePicker.Default.PickAsync(new PickOptions { PickerTitle = "PDF-Datei auswählen", FileTypes = pdfTypes });
            if (file == null)
                return;
            if (!file.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            {
                SetStatus("Bitte wählen Sie eine PDF-Datei aus.", true);
                return;
            }

            _selectedFile = file;
            _selectedFileLabel!.Text = $"Ausgewählt: {file.FileName}";
            SetStatus(string.Empty);
        }
        catch (Exception ex)
        {
            SetStatus(ToUserMessage(ex, "Die Dateiauswahl konnte nicht geöffnet werden."), true);
        }
    }

    private async Task ReviewAndSaveAsync()
    {
        var buildResult = await TryBuildRequestAsync();
        if (buildResult.Request == null)
        {
            SetStatus(buildResult.Message, true);
            return;
        }
        var request = buildResult.Request;

        var confirmed = await DisplayAlertAsync(
            "Neue Fassung prüfen",
            $"Dokumenttyp: {_editingType!.Type.Bezeichnung}\nFassung: {request.Fassung}\nGültig ab: {request.GueltigAb:dd.MM.yyyy}\nDateiname: {request.Dateiname}",
            "Endgültig speichern",
            "Zurück");
        if (!confirmed)
            return;

        try
        {
            _isBusy = true;
            SetStatus("Fassung wird gespeichert …");
            await _supabaseService.CreateVereinsdokumentFassungAsync(request);
            var typeName = _editingType!.Type.Bezeichnung;
            var typeCode = _editingType.Type.Code;
            CloseForm();
            await LoadAfterSaveAsync(typeCode);
            SetStatus($"Die neue Fassung für „{typeName}“ wurde gespeichert.");
        }
        catch (Exception ex)
        {
            SetStatus(ToUserMessage(ex, "Die Fassung konnte nicht gespeichert werden."), true);
        }
        finally
        {
            _isBusy = false;
        }
    }

    private async Task LoadAfterSaveAsync(string typeCode)
    {
        var index = _documentTypes.ToList().FindIndex(candidate => candidate.Type.Code == typeCode);
        if (index < 0)
            return;
        var existing = _documentTypes[index];
        _documentTypes[index] = new DocumentTypeItem(existing.Type, await _supabaseService.GetVereinsdokumentFassungenAsync(typeCode), DateTime.Today);
    }

    private async Task<(VereinsdokumentFassungCreateRequest? Request, string Message)> TryBuildRequestAsync()
    {
        if (_editingType == null || string.IsNullOrWhiteSpace(_fassungEntry?.Text))
            return (null, "Bitte geben Sie eine Fassung oder Bezeichnung ein.");
        if (_gueltigAbPicker?.Date is not DateTime gueltigAb || gueltigAb == default)
            return (null, "Bitte geben Sie ein gültiges Datum an.");
        if (_selectedFile == null)
            return (null, "Bitte wählen Sie eine PDF-Datei aus.");

        try
        {
            using var source = await _selectedFile.OpenReadAsync();
            using var content = new MemoryStream();
            await source.CopyToAsync(content);
            return (new VereinsdokumentFassungCreateRequest
            {
                DokumenttypCode = _editingType.Type.Code,
                Fassung = _fassungEntry.Text.Trim(),
                GueltigAb = DateOnly.FromDateTime(gueltigAb),
                Dateiname = _selectedFile.FileName,
                PdfInhalt = content.ToArray()
            }, string.Empty);
        }
        catch (Exception ex)
        {
            return (null, ToUserMessage(ex, "Die gewählte Datei konnte nicht gelesen werden."));
        }
    }

    private void CloseForm()
    {
        _editingType = null;
        _selectedFile = null;
        _fassungEntry = null;
        _gueltigAbPicker = null;
        _selectedFileLabel = null;
        _formHost.Children.Clear();
    }

    private void SetStatus(string message, bool isError = false)
    {
        _statusLabel.Text = message;
        _statusLabel.TextColor = isError ? Colors.DarkRed : Colors.DarkSlateBlue;
    }

    private static string ToUserMessage(Exception exception, string fallback)
    {
        var code = exception.Message;
        if (code.Contains("FILE_NOT_PDF", StringComparison.OrdinalIgnoreCase)) return "Bitte wählen Sie eine PDF-Datei aus.";
        if (code.Contains("AUTH_REQUIRED", StringComparison.OrdinalIgnoreCase)) return "Sie sind nicht mehr angemeldet.";
        if (code.Contains("UPLOAD_FAILED", StringComparison.OrdinalIgnoreCase)) return "Die Datei konnte nicht hochgeladen werden.";
        if (code.Contains("RPC_FAILED_CLEANUP_FAILED", StringComparison.OrdinalIgnoreCase)) return "Fehler beim Speichern; die technische Bereinigung konnte nicht vollständig abgeschlossen werden.";
        if (code.Contains("RPC_FAILED", StringComparison.OrdinalIgnoreCase)) return "Die Fassung konnte wegen einer ungültigen zeitlichen Reihenfolge nicht angelegt werden.";
        if (exception is HttpRequestException) return "Netzwerkfehler. Bitte prüfen Sie Ihre Verbindung.";
        return fallback;
    }

    private static Border CreateSection(string title, params View[] children)
    {
        var stack = new VerticalStackLayout { Spacing = 10 };
        stack.Children.Add(new Label { Text = title, FontAttributes = FontAttributes.Bold, FontSize = 18 });
        foreach (var child in children) stack.Children.Add(child);
        return new Border { Stroke = Colors.LightGray, Padding = 16, Content = stack };
    }

    private static View CreateField(string title, View editor) => new VerticalStackLayout
    {
        Spacing = 4,
        Children = { new Label { Text = title, FontAttributes = FontAttributes.Bold }, editor }
    };

    private sealed class DocumentTypeItem
    {
        public VereinsdokumentTypRecord Type { get; }
        public ObservableCollection<DocumentVersionItem> Versions { get; } = new();
        public string Title => Type.Bezeichnung;
        public string RelevantVersion { get; private set; } = "Keine Fassung vorhanden";

        public DocumentTypeItem(VereinsdokumentTypRecord type, IEnumerable<VereinsdokumentFassungRecord> versions, DateTime referenceDate)
        {
            Type = type;
            ApplyVersions(versions, referenceDate);
        }

        public void ApplyVersions(IEnumerable<VereinsdokumentFassungRecord> versions, DateTime referenceDate)
        {
            var ordered = versions.OrderByDescending(version => version.GueltigAb).ToList();
            var current = VereinsdokumentFassungHelper.GetValidClubDocumentVersion(ordered, Type.Code, referenceDate);
            Versions.Clear();
            foreach (var version in ordered)
            {
                var status = current?.Id == version.Id
                    ? "Aktuell"
                    : VereinsdokumentFassungHelper.IsFuture(version, referenceDate) ? "Zukünftig" : "Abgelaufen";
                Versions.Add(new DocumentVersionItem(version, status));
            }
            RelevantVersion = ordered.Count == 0
                ? "Keine Fassung vorhanden"
                : current == null ? "Keine aktuell gültige Fassung vorhanden" : $"Aktuelle Fassung: {current.Fassung} (seit {current.GueltigAb:dd.MM.yyyy})";
        }
    }

    private sealed class DocumentVersionItem
    {
        public string Fassung { get; }
        public string Status { get; }
        public string Details { get; }
        public DocumentVersionItem(VereinsdokumentFassungRecord version, string status)
        {
            Fassung = version.Fassung;
            Status = status;
            Details = $"Gültig ab: {version.GueltigAb:dd.MM.yyyy} · Gültig bis: {(version.GueltigBis.HasValue ? version.GueltigBis.Value.ToString("dd.MM.yyyy") : "–")}\nDatei: {version.Dateiname ?? "–"} · Erstellt: {version.ErstelltAm:dd.MM.yyyy}";
        }
    }
}
