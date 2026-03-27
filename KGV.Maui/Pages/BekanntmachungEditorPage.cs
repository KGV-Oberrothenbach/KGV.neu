using KGV.Core.Interfaces;
using KGV.Core.Models;
using KGV.Core.Security;
using KGV.Core.Utilities;
using KGV.Maui.State;
using KGV.Maui.ViewModels;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using System.Globalization;

namespace KGV.Maui.Pages;

public sealed class BekanntmachungEditorPage : ContentPage, IQueryAttributable
{
    private readonly ISupabaseService _supabaseService;
    private readonly UserContextState _userContextState;

    private readonly Label _headlineLabel;
    private readonly Label _descriptionLabel;
    private readonly Label _statusLabel;
    private readonly Entry _titleEntry;
    private readonly Editor _htmlEditor;
    private readonly DatePicker _visibleFromDatePicker;
    private readonly Entry _visibleFromTimeEntry;
    private readonly DatePicker _visibleToDatePicker;
    private readonly Entry _visibleToTimeEntry;
    private readonly Entry _sortOrderEntry;
    private readonly Switch _activeSwitch;
    private readonly Button _htmlTabButton;
    private readonly Button _previewTabButton;
    private readonly VerticalStackLayout _htmlEditorSection;
    private readonly VerticalStackLayout _previewSection;
    private readonly WebView _previewWebView;
    private readonly Button _saveButton;
    private readonly Button _cancelButton;

    private long? _entryId;
    private BekanntmachungRecord? _existingRecord;
    private bool _isLoading;
    private bool _loadScheduled;
    private bool _isAuthorized;
    private readonly KGV.Maui.ViewModels.HomeViewModel _homeViewModel;

    public BekanntmachungEditorPage(ISupabaseService supabaseService, UserContextState userContextState, KGV.Maui.ViewModels.HomeViewModel homeViewModel)
    {
        _supabaseService = supabaseService;
        _userContextState = userContextState;
        _homeViewModel = homeViewModel;

        Title = "Bekanntmachung";

        _headlineLabel = new Label { FontSize = 24, FontAttributes = FontAttributes.Bold, LineBreakMode = LineBreakMode.WordWrap };
        _descriptionLabel = new Label { TextColor = Colors.Gray, LineBreakMode = LineBreakMode.WordWrap };
        _statusLabel = new Label { TextColor = Colors.DarkRed, LineBreakMode = LineBreakMode.WordWrap };

        _titleEntry = new Entry { Placeholder = "Titel" };
        _htmlEditor = new Editor
        {
            AutoSize = EditorAutoSizeOption.TextChanges,
            HeightRequest = 220,
            Placeholder = "HTML-Inhalt"
        };
        _htmlEditor.TextChanged += (_, _) => RefreshPreview();

        _visibleFromDatePicker = new DatePicker { Date = DateTime.Today };
        _visibleFromTimeEntry = new Entry { Placeholder = "HH:mm", Keyboard = Keyboard.Text };
        _visibleToDatePicker = new DatePicker { Date = DateTime.Today };
        _visibleToTimeEntry = new Entry { Placeholder = "HH:mm", Keyboard = Keyboard.Text };
        _sortOrderEntry = new Entry { Placeholder = "Sortierreihenfolge", Keyboard = Keyboard.Numeric };
        _activeSwitch = new Switch { IsToggled = true };

        _htmlTabButton = new Button { Text = "HTML" };
        _htmlTabButton.Clicked += (_, _) => SetHtmlMode(showPreview: false);

        _previewTabButton = new Button { Text = "Vorschau" };
        _previewTabButton.Clicked += (_, _) => SetHtmlMode(showPreview: true);

        _previewWebView = new WebView
        {
            HeightRequest = 260,
            Source = new HtmlWebViewSource { Html = BuildPreviewDocument(null) }
        };

        _htmlEditorSection = new VerticalStackLayout
        {
            Spacing = 8,
            Children =
            {
                CreateSnippetBar(),
                _htmlEditor
            }
        };

        _previewSection = new VerticalStackLayout
        {
            Spacing = 8,
            IsVisible = false,
            Children =
            {
                new Label
                {
                    Text = "Mobile HTML-Vorschau des gespeicherten Inhalts. Die Bearbeitung bleibt im HTML-Tab touch-tauglich, ohne neue Schattenlogik neben dem bestehenden HTML-Feld zu eröffnen.",
                    TextColor = Colors.Gray,
                    LineBreakMode = LineBreakMode.WordWrap
                },
                _previewWebView
            }
        };

        _saveButton = new Button { Text = "Speichern" };
        _saveButton.Clicked += async (_, _) => await SaveAsync();

        _cancelButton = new Button { Text = "Abbrechen" };
        _cancelButton.Clicked += async (_, _) => await NavigateToOverviewAsync();

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = 24,
                Spacing = 12,
                Children =
                {
                    _headlineLabel,
                    _descriptionLabel,
                    _statusLabel,
                    CreateField("Titel *", _titleEntry),
                    new Label { Text = "HTML-Inhalt *", FontAttributes = FontAttributes.Bold, FontSize = 12, TextColor = Colors.Gray },
                    new HorizontalStackLayout { Spacing = 8, Children = { _htmlTabButton, _previewTabButton } },
                    _htmlEditorSection,
                    _previewSection,
                    CreateTimestampField("Sichtbar ab", _visibleFromDatePicker, _visibleFromTimeEntry),
                    CreateTimestampField("Sichtbar bis", _visibleToDatePicker, _visibleToTimeEntry),
                    CreateField("Sortierreihenfolge", _sortOrderEntry),
                    CreateField("Aktiv", _activeSwitch),
                    new HorizontalStackLayout
                    {
                        Spacing = 8,
                        Children = { _cancelButton, _saveButton }
                    }
                }
            }
        };

        SetHtmlMode(showPreview: false);
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        var entryId = TryReadLong(query, "entryId");
        _entryId = entryId is > 0 ? entryId : null;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        if (_isLoading || _loadScheduled)
            return;

        _loadScheduled = true;
        Dispatcher.Dispatch(async () =>
        {
            await Task.Yield();
            _loadScheduled = false;
            await LoadAsync();
        });
    }

    private async Task LoadAsync()
    {
        if (_isLoading)
            return;

        _isLoading = true;
        _statusLabel.Text = "Daten werden geladen.";

        try
        {
            _isAuthorized = _userContextState.CurrentUserContext?.Role is UserRole.Admin or UserRole.Vorstand;
            if (!_isAuthorized)
            {
                _headlineLabel.Text = "Bekanntmachung";
                _descriptionLabel.Text = "Dieser Editor ist nur für Admin/Vorstand verfügbar.";
                SetEnabledState(false);
                return;
            }

            if (_entryId.HasValue && _entryId.Value > 0)
                await LoadExistingRecordAsync(_entryId.Value);
            else
                ConfigureNewRecord();
        }
        catch (Exception ex)
        {
            _statusLabel.Text = ex.Message;
            SetEnabledState(false);
        }
        finally
        {
            _isLoading = false;
        }
    }

    private async Task LoadExistingRecordAsync(long entryId)
    {
        var records = await _supabaseService.GetBekanntmachungenVerwaltungAsync();
        _existingRecord = records.FirstOrDefault(x => x.Id == entryId);
        if (_existingRecord == null)
        {
            _headlineLabel.Text = "Bekanntmachung bearbeiten";
            _descriptionLabel.Text = "Der angeforderte Datensatz konnte nicht geladen werden. Bitte kehre zur Übersicht zurück und öffne ihn erneut.";
            SetEnabledState(false);
            return;
        }

        Title = "Bekanntmachung bearbeiten";
        _headlineLabel.Text = "Bekanntmachung bearbeiten";
        _descriptionLabel.Text = "Eigener mobiler Editorpfad für bestehende Bekanntmachungen. Die Übersicht bleibt dadurch eine ruhige reine Listenansicht.";
        _titleEntry.Text = _existingRecord.Titel ?? string.Empty;
        _htmlEditor.Text = _existingRecord.InhaltHtml ?? string.Empty;
        _visibleFromDatePicker.Date = _existingRecord.SichtbarAb?.Date ?? DateTime.Today;
        _visibleFromTimeEntry.Text = _existingRecord.SichtbarAb?.ToString("HH:mm", CultureInfo.CurrentCulture) ?? string.Empty;
        _visibleToDatePicker.Date = _existingRecord.SichtbarBis?.Date ?? DateTime.Today;
        _visibleToTimeEntry.Text = _existingRecord.SichtbarBis?.ToString("HH:mm", CultureInfo.CurrentCulture) ?? string.Empty;
        _sortOrderEntry.Text = _existingRecord.SortOrder?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
        _activeSwitch.IsToggled = _existingRecord.Aktiv;
        SetEnabledState(true);
        RefreshPreview();
    }

    private void ConfigureNewRecord()
    {
        _existingRecord = null;
        Title = "Bekanntmachung neu";
        _headlineLabel.Text = "Neue Bekanntmachung";
        _descriptionLabel.Text = "Eigener mobiler Editorpfad für neue Bekanntmachungen. HTML-Bearbeitung bleibt erhalten, ohne die Übersicht wieder zur Mischseite zu machen.";
        _titleEntry.Text = string.Empty;
        _htmlEditor.Text = "<p></p>";
        _visibleFromDatePicker.Date = DateTime.Today;
        _visibleFromTimeEntry.Text = string.Empty;
        _visibleToDatePicker.Date = DateTime.Today;
        _visibleToTimeEntry.Text = string.Empty;
        _sortOrderEntry.Text = string.Empty;
        _activeSwitch.IsToggled = true;
        SetEnabledState(true);
        RefreshPreview();
    }

    private async Task SaveAsync()
    {
        if (!_isAuthorized)
            return;

        _statusLabel.Text = "Daten werden gespeichert.";
        _statusLabel.TextColor = Colors.DarkSlateBlue;
        SetEnabledState(false);

        try
        {
            await Task.Yield();

            if (!TryBuildRecord(out var record))
                return;

            if (_existingRecord == null)
            {
                var created = await _supabaseService.CreateBekanntmachungAsync(record.ToInsertRecord());
                if (created == null)
                {
                    _statusLabel.Text = "Bekanntmachung konnte nicht erstellt werden.";
                    return;
                }
            }
            else
            {
                var success = await _supabaseService.UpdateBekanntmachungAsync(record);
                if (!success)
                {
                    _statusLabel.Text = "Bekanntmachung konnte nicht gespeichert werden.";
                    return;
                }
            }

            _homeViewModel.Invalidate();
            await NavigateToOverviewAsync();
        }
        catch (Exception ex)
        {
            _statusLabel.Text = ex.Message;
        }
        finally
        {
            SetEnabledState(true);
        }
    }

    private bool TryBuildRecord(out BekanntmachungRecord record)
    {
        record = new BekanntmachungRecord();

        if (string.IsNullOrWhiteSpace(_titleEntry.Text))
        {
            _statusLabel.Text = "Titel ist ein Pflichtfeld.";
            _titleEntry.Focus();
            return false;
        }

        if (string.IsNullOrWhiteSpace(_htmlEditor.Text))
        {
            _statusLabel.Text = "HTML-Inhalt ist ein Pflichtfeld.";
            SetHtmlMode(showPreview: false);
            _htmlEditor.Focus();
            return false;
        }

        if (!TryBuildOptionalTimestamp(_visibleFromDatePicker.Date, _visibleFromTimeEntry.Text, out var visibleFrom, out var normalizedVisibleFrom, out var visibleFromError))
        {
            _statusLabel.Text = visibleFromError;
            _visibleFromTimeEntry.Text = normalizedVisibleFrom;
            _visibleFromTimeEntry.Focus();
            return false;
        }

        if (!TryBuildOptionalTimestamp(_visibleToDatePicker.Date, _visibleToTimeEntry.Text, out var visibleTo, out var normalizedVisibleTo, out var visibleToError))
        {
            _statusLabel.Text = visibleToError;
            _visibleToTimeEntry.Text = normalizedVisibleTo;
            _visibleToTimeEntry.Focus();
            return false;
        }

        _visibleFromTimeEntry.Text = normalizedVisibleFrom;
        _visibleToTimeEntry.Text = normalizedVisibleTo;

        if (visibleFrom.HasValue && visibleTo.HasValue && visibleTo.Value < visibleFrom.Value)
        {
            _statusLabel.Text = "Sichtbar bis darf nicht vor Sichtbar ab liegen.";
            _visibleToTimeEntry.Focus();
            return false;
        }

        int? sortOrder = null;
        if (!string.IsNullOrWhiteSpace(_sortOrderEntry.Text))
        {
            if (!int.TryParse(_sortOrderEntry.Text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedSortOrder))
            {
                _statusLabel.Text = "Sortierreihenfolge muss eine ganze Zahl sein.";
                _sortOrderEntry.Focus();
                return false;
            }

            sortOrder = parsedSortOrder;
            _sortOrderEntry.Text = parsedSortOrder.ToString(CultureInfo.InvariantCulture);
        }

        record = new BekanntmachungRecord
        {
            Titel = _titleEntry.Text.Trim(),
            InhaltHtml = _htmlEditor.Text.Trim(),
            SichtbarAb = visibleFrom,
            SichtbarBis = visibleTo,
            SortOrder = sortOrder,
            Aktiv = _activeSwitch.IsToggled
        };

        if (_existingRecord != null)
            record.Id = _existingRecord.Id;

        return true;
    }

    private static bool TryBuildOptionalTimestamp(DateTime selectedDate, string? timeText, out DateTime? value, out string normalizedTime, out string error)
    {
        value = null;
        normalizedTime = string.Empty;
        error = string.Empty;

        if (string.IsNullOrWhiteSpace(timeText))
            return true;

        if (!TemporalInputParser.TryNormalizeTimeText(timeText, out normalizedTime, out var time))
        {
            error = "Zeitangaben müssen als HH:mm eingegeben werden.";
            return false;
        }

        value = selectedDate.Date.Add(time ?? TimeSpan.Zero);
        return true;
    }

    private void SetEnabledState(bool enabled)
    {
        _titleEntry.IsEnabled = enabled;
        _htmlEditor.IsEnabled = enabled;
        _visibleFromDatePicker.IsEnabled = enabled;
        _visibleFromTimeEntry.IsEnabled = enabled;
        _visibleToDatePicker.IsEnabled = enabled;
        _visibleToTimeEntry.IsEnabled = enabled;
        _sortOrderEntry.IsEnabled = enabled;
        _activeSwitch.IsEnabled = enabled;
        _htmlTabButton.IsEnabled = enabled;
        _previewTabButton.IsEnabled = enabled;
        _saveButton.IsEnabled = enabled;
        _cancelButton.IsEnabled = enabled;
    }

    private void SetHtmlMode(bool showPreview)
    {
        _htmlEditorSection.IsVisible = !showPreview;
        _previewSection.IsVisible = showPreview;
        if (showPreview)
            RefreshPreview();
    }

    private void RefreshPreview()
    {
        _previewWebView.Source = new HtmlWebViewSource
        {
            Html = BuildPreviewDocument(_htmlEditor.Text)
        };
    }

    private View CreateSnippetBar()
    {
        var layout = new FlexLayout
        {
            Wrap = Microsoft.Maui.Layouts.FlexWrap.Wrap,
            Direction = Microsoft.Maui.Layouts.FlexDirection.Row,
            JustifyContent = Microsoft.Maui.Layouts.FlexJustify.Start,
            AlignItems = Microsoft.Maui.Layouts.FlexAlignItems.Start
        };
        layout.Children.Add(CreateSnippetButton("Absatz", "<p>Text</p>"));
        layout.Children.Add(CreateSnippetButton("Überschrift", "<h3>Überschrift</h3>"));
        layout.Children.Add(CreateSnippetButton("Fett", "<strong>Betonung</strong>"));
        layout.Children.Add(CreateSnippetButton("Link", "<a href=\"https://\">Linktext</a>"));
        layout.Children.Add(CreateSnippetButton("Liste", "<ul>\n  <li>Punkt 1</li>\n  <li>Punkt 2</li>\n</ul>"));
        return layout;
    }

    private Button CreateSnippetButton(string title, string snippet)
    {
        var button = new Button { Text = title, Margin = new Thickness(0, 0, 8, 8) };
        button.Clicked += (_, _) => InsertHtmlSnippet(snippet);
        return button;
    }

    private void InsertHtmlSnippet(string snippet)
    {
        var existing = _htmlEditor.Text ?? string.Empty;
        var cursor = Math.Clamp(_htmlEditor.CursorPosition, 0, existing.Length);
        var selectionLength = Math.Clamp(_htmlEditor.SelectionLength, 0, existing.Length - cursor);
        var replacement = snippet;

        if (selectionLength > 0)
            existing = existing.Remove(cursor, selectionLength);

        _htmlEditor.Text = existing.Insert(cursor, replacement);
        _htmlEditor.CursorPosition = cursor + replacement.Length;
        _htmlEditor.SelectionLength = 0;
        RefreshPreview();
    }

    private static View CreateField(string title, View field)
    {
        return new VerticalStackLayout
        {
            Spacing = 4,
            Children =
            {
                new Label { Text = title, FontAttributes = FontAttributes.Bold, FontSize = 12, TextColor = Colors.Gray },
                field
            }
        };
    }

    private static View CreateTimestampField(string title, DatePicker datePicker, Entry timeEntry)
    {
        Grid.SetColumn(datePicker, 0);
        Grid.SetColumn(timeEntry, 1);

        return new VerticalStackLayout
        {
            Spacing = 4,
            Children =
            {
                new Label { Text = title, FontAttributes = FontAttributes.Bold, FontSize = 12, TextColor = Colors.Gray },
                new Grid
                {
                    ColumnDefinitions = new ColumnDefinitionCollection
                    {
                        new ColumnDefinition(GridLength.Star),
                        new ColumnDefinition(new GridLength(120))
                    },
                    ColumnSpacing = 8,
                    Children =
                    {
                        datePicker,
                        timeEntry
                    }
                }
            }
        };
    }

    private Task NavigateToOverviewAsync()
    {
        return Shell.Current.GoToAsync("//home");
    }

    private static long? TryReadLong(IDictionary<string, object> query, string key)
    {
        if (!query.TryGetValue(key, out var raw) || raw == null)
            return null;

        return raw switch
        {
            long longValue => longValue,
            int intValue => intValue,
            string text when long.TryParse(Uri.UnescapeDataString(text), out var parsed) => parsed,
            _ => null
        };
    }

    private static string BuildPreviewDocument(string? html)
    {
        var body = string.IsNullOrWhiteSpace(html)
            ? "<p style='color:#666;'>Noch kein HTML-Inhalt vorhanden.</p>"
            : html;

        return $"<html><head><meta charset='utf-8'><style>body{{font-family:'Segoe UI';padding:16px;}} table{{border-collapse:collapse;}} td,th{{border:1px solid #ccc;padding:4px;}}</style></head><body>{body}</body></html>";
    }
}
