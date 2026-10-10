using KGV.Core.Interfaces;
using KGV.Core.Models;
using KGV.Core.Security;
using KGV.Core.Utilities;
using KGV.Maui.State;
using KGV.Maui.ViewModels;
using KGV.Maui.Utilities;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using System.Globalization;
using Debug = System.Diagnostics.Debug;

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
    private readonly Switch _useVisibleFrom;
    private readonly Switch _useVisibleTo;
    private readonly Button _htmlTabButton;
    private readonly Button _previewTabButton;
    private readonly VerticalStackLayout _htmlEditorSection;
    private readonly VerticalStackLayout _previewSection;
    private readonly WebView _previewWebView;
    private readonly Button _saveButton;
    private readonly Button _cancelButton;
    private readonly Button _lifecycleButton;

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

        var defaultVisibleFrom = CreateCurrentTimestampDefault();
        var defaultVisibleTo = defaultVisibleFrom.AddMonths(1);
        _visibleFromDatePicker = new DatePicker { Date = defaultVisibleFrom.Date };
        _visibleFromTimeEntry = new Entry { Placeholder = "HH:mm", Keyboard = Keyboard.Text, Text = defaultVisibleFrom.ToString("HH:mm", CultureInfo.CurrentCulture) };
        _visibleToDatePicker = new DatePicker { Date = defaultVisibleTo.Date };
        _visibleToTimeEntry = new Entry { Placeholder = "HH:mm", Keyboard = Keyboard.Text, Text = defaultVisibleTo.ToString("HH:mm", CultureInfo.CurrentCulture) };
        _sortOrderEntry = new Entry { Placeholder = "Sortierreihenfolge", Keyboard = Keyboard.Numeric };
        _useVisibleFrom = new Switch { IsToggled = true };
        _useVisibleTo = new Switch { IsToggled = true };
        _useVisibleFrom.Toggled += (_, _) => SetEnabledState(true);
        _useVisibleTo.Toggled += (_, _) => SetEnabledState(true);

        _htmlTabButton = new Button { Text = "HTML" };
        _htmlTabButton.Clicked += (_, _) => SetHtmlMode(showPreview: false);

        _previewTabButton = new Button { Text = "Vorschau" };
        _previewTabButton.Clicked += (_, _) => SetHtmlMode(showPreview: true);

        _previewWebView = new WebView
        {
            HeightRequest = 260,
            Source = new HtmlWebViewSource { Html = HtmlContentHelper.BuildHtmlDocument(null) }
        };
        _previewWebView.Navigating += async (_, e) => await HandlePreviewNavigationAsync(e.Url, () => e.Cancel = true);

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

        _lifecycleButton = new Button { IsVisible = false, BackgroundColor = Colors.IndianRed, TextColor = Colors.White };
        _lifecycleButton.Clicked += async (_, _) => await ToggleActiveAsync();

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
                    CreateField("Sichtbar ab verwenden", _useVisibleFrom), CreateTimestampField("Sichtbar ab", _visibleFromDatePicker, _visibleFromTimeEntry),
                    CreateField("Sichtbar bis verwenden", _useVisibleTo), CreateTimestampField("Sichtbar bis", _visibleToDatePicker, _visibleToTimeEntry),
                    CreateField("Sortierreihenfolge", _sortOrderEntry),
                    new FlexLayout
                    {
                        Direction = FlexDirection.Row,
                        Wrap = FlexWrap.Wrap,
                        Children = { _cancelButton, _lifecycleButton, _saveButton }
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
            _isAuthorized = PermissionChecks.CanManageAnnouncements(_userContextState.CurrentUserContext);
            if (!_isAuthorized)
            {
                _headlineLabel.Text = "Bekanntmachung";
                _descriptionLabel.Text = "Für diesen Editor fehlt die Berechtigung Bekanntmachungen verwalten.";
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
        var visibleFrom = _existingRecord.SichtbarAb ?? CreateCurrentTimestampDefault();
        var visibleTo = _existingRecord.SichtbarBis ?? visibleFrom.AddMonths(1);
        _visibleFromDatePicker.Date = visibleFrom.Date;
        _visibleFromTimeEntry.Text = visibleFrom.ToString("HH:mm", CultureInfo.CurrentCulture);
        _visibleToDatePicker.Date = visibleTo.Date;
        _visibleToTimeEntry.Text = visibleTo.ToString("HH:mm", CultureInfo.CurrentCulture);
        _sortOrderEntry.Text = _existingRecord.SortOrder?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
        _useVisibleFrom.IsToggled = _existingRecord.SichtbarAb.HasValue;
        _useVisibleTo.IsToggled = _existingRecord.SichtbarBis.HasValue;
        _lifecycleButton.IsVisible = true; _lifecycleButton.Text = _existingRecord.Aktiv ? "Deaktivieren" : "Wieder aktivieren";
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
        var visibleFrom = CreateCurrentTimestampDefault();
        var visibleTo = visibleFrom.AddMonths(1);
        _visibleFromDatePicker.Date = visibleFrom.Date;
        _visibleFromTimeEntry.Text = visibleFrom.ToString("HH:mm", CultureInfo.CurrentCulture);
        _visibleToDatePicker.Date = visibleTo.Date;
        _visibleToTimeEntry.Text = visibleTo.ToString("HH:mm", CultureInfo.CurrentCulture);
        _sortOrderEntry.Text = string.Empty;
        _useVisibleFrom.IsToggled = _useVisibleTo.IsToggled = true;
        _lifecycleButton.IsVisible = false;
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

            var saveMessage = _existingRecord == null
                ? "Bekanntmachung erstellt."
                : "Bekanntmachung gespeichert.";

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

            await CompleteSuccessfulSaveAsync(saveMessage);
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

        DateTime? visibleFrom = null;
        var normalizedVisibleFrom = string.Empty;
        if (_useVisibleFrom.IsToggled && !TryBuildOptionalTimestamp(_visibleFromDatePicker.Date!.Value, _visibleFromTimeEntry.Text, out visibleFrom, out normalizedVisibleFrom, out var visibleFromError))
        {
            _statusLabel.Text = visibleFromError;
            _visibleFromTimeEntry.Text = normalizedVisibleFrom;
            _visibleFromTimeEntry.Focus();
            return false;
        }

        DateTime? visibleTo = null;
        var normalizedVisibleTo = string.Empty;
        if (_useVisibleTo.IsToggled && !TryBuildOptionalTimestamp(_visibleToDatePicker.Date!.Value, _visibleToTimeEntry.Text, out visibleTo, out normalizedVisibleTo, out var visibleToError))
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
            Aktiv = _existingRecord?.Aktiv ?? true,
            IsDemo = _existingRecord?.IsDemo ?? false
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

    private static DateTime CreateCurrentTimestampDefault()
    {
        return Vereinszeit.NowToMinute();
    }

    private void SetEnabledState(bool enabled)
    {
        _titleEntry.IsEnabled = enabled;
        _htmlEditor.IsEnabled = enabled;
        _useVisibleFrom.IsEnabled = _useVisibleTo.IsEnabled = enabled;
        _visibleFromDatePicker.IsEnabled = _visibleFromTimeEntry.IsEnabled = enabled && _useVisibleFrom.IsToggled;
        _visibleToDatePicker.IsEnabled = _visibleToTimeEntry.IsEnabled = enabled && _useVisibleTo.IsToggled;
        _sortOrderEntry.IsEnabled = enabled;
        _htmlTabButton.IsEnabled = enabled;
        _previewTabButton.IsEnabled = enabled;
        _saveButton.IsEnabled = enabled;
        _cancelButton.IsEnabled = enabled;
        _lifecycleButton.IsEnabled = enabled && _existingRecord != null;
    }

    private async Task ToggleActiveAsync()
    {
        var existingRecord = _existingRecord;
        if (!_isAuthorized)
            return;

        if (existingRecord is null || existingRecord.Id <= 0)
            return;

        var targetActive = !existingRecord.Aktiv;
        if (!targetActive && !await DisplayAlertAsync("Bekanntmachung deaktivieren", $"Soll die Bekanntmachung „{existingRecord.Titel ?? "ohne Titel"}“ wirklich deaktiviert werden?", "Deaktivieren", "Abbrechen"))
            return;
        var updateRecord = new BekanntmachungRecord { Id = existingRecord.Id, Titel = existingRecord.Titel, InhaltHtml = existingRecord.InhaltHtml, SichtbarAb = existingRecord.SichtbarAb, SichtbarBis = existingRecord.SichtbarBis, SortOrder = existingRecord.SortOrder, Aktiv = targetActive, CreatedAt = existingRecord.CreatedAt, UpdatedAt = existingRecord.UpdatedAt, IsDemo = existingRecord.IsDemo };
        _statusLabel.Text = targetActive ? "Bekanntmachung wird wieder aktiviert." : "Bekanntmachung wird deaktiviert.";
        _statusLabel.TextColor = Colors.DarkSlateBlue;
        SetEnabledState(false);

        try
        {
            await Task.Yield();

            var success = await _supabaseService.UpdateBekanntmachungAsync(updateRecord);
            if (!success)
            {
                _statusLabel.Text = "Bekanntmachung konnte nicht aktualisiert werden.";
                _statusLabel.TextColor = Colors.IndianRed;
                return;
            }

            existingRecord.Aktiv = targetActive;
            _lifecycleButton.Text = targetActive ? "Deaktivieren" : "Wieder aktivieren";
            await CompleteSuccessfulSaveAsync(targetActive ? "Bekanntmachung wurde wieder aktiviert." : "Bekanntmachung wurde deaktiviert.");
        }
        catch (Exception ex)
        {
            _statusLabel.Text = ex.Message;
            _statusLabel.TextColor = Colors.IndianRed;
        }
        finally
        {
            SetEnabledState(true);
        }
    }

    private void SetHtmlMode(bool showPreview)
    {
        _htmlEditorSection.IsVisible = !showPreview;
        _previewSection.IsVisible = showPreview;
        if (showPreview)
            RefreshPreview();
    }

    private async Task HandlePreviewNavigationAsync(string? url, Action cancel)
    {
        if (SafeWebViewNavigation.IsInternal(url)) return;
        cancel();
        if (!SafeWebViewNavigation.TryGetAllowedExternal(url, out var uri) || uri is null)
            return;
        try { await Launcher.Default.OpenAsync(uri); }
        catch { _statusLabel.Text = "Der Link konnte nicht geöffnet werden."; }
    }

    private void RefreshPreview()
    {
        _previewWebView.Source = new HtmlWebViewSource
        {
            Html = HtmlContentHelper.BuildHtmlDocument(_htmlEditor.Text)
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

    private async Task CompleteSuccessfulSaveAsync(string successMessage)
    {
        _statusLabel.Text = successMessage;
        _statusLabel.TextColor = Colors.Green;

        _homeViewModel.Invalidate();
        try
        {
            await _homeViewModel.ReloadAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[BekanntmachungEditorPage] Startseiten-Aktualisierung fehlgeschlagen: {ex}");
        }

        try
        {
            await NavigateToOverviewAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[BekanntmachungEditorPage] Post-save navigation failed after successful save: {ex}");
            _statusLabel.Text = $"{successMessage} Startseite wird beim nächsten Öffnen aktualisiert.";
            _statusLabel.TextColor = Colors.DarkGoldenrod;
        }
    }

    private Task NavigateToOverviewAsync()
    {
        return Shell.Current.GoToAsync("//management_announcements");
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

}
