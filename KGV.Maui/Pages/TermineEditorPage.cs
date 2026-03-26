using KGV.Core.Interfaces;
using KGV.Core.Models;
using KGV.Core.Security;
using KGV.Maui.State;
using KGV.Maui.ViewModels;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace KGV.Maui.Pages;

public sealed class TermineEditorPage : ContentPage, IQueryAttributable
{
    private readonly ISupabaseService _supabaseService;
    private readonly UserContextState _userContextState;

    private readonly Label _headlineLabel;
    private readonly Label _descriptionLabel;
    private readonly Label _statusLabel;
    private readonly Entry _titleEntry;
    private readonly Editor _descriptionEditor;
    private readonly DatePicker _datePicker;
    private readonly CheckBox _hasStartTimeCheckBox;
    private readonly TimePicker _startTimePicker;
    private readonly CheckBox _hasEndTimeCheckBox;
    private readonly TimePicker _endTimePicker;
    private readonly CheckBox _hasVisibleFromCheckBox;
    private readonly DatePicker _visibleFromDatePicker;
    private readonly TimePicker _visibleFromTimePicker;
    private readonly CheckBox _hasVisibleToCheckBox;
    private readonly DatePicker _visibleToDatePicker;
    private readonly TimePicker _visibleToTimePicker;
    private readonly Switch _activeSwitch;
    private readonly Button _saveButton;
    private readonly Button _cancelButton;

    private long? _entryId;
    private TerminRecord? _existingRecord;
    private bool _isLoading;
    private bool _loadScheduled;
    private bool _isAuthorized;
    private readonly KGV.Maui.ViewModels.HomeViewModel _homeViewModel;

    public TermineEditorPage(ISupabaseService supabaseService, UserContextState userContextState, KGV.Maui.ViewModels.HomeViewModel homeViewModel)
    {
        _supabaseService = supabaseService;
        _userContextState = userContextState;
        _homeViewModel = homeViewModel;

        Title = "Termin";

        _headlineLabel = new Label { FontSize = 24, FontAttributes = FontAttributes.Bold, LineBreakMode = Microsoft.Maui.LineBreakMode.WordWrap };
        _descriptionLabel = new Label { TextColor = Colors.Gray, LineBreakMode = Microsoft.Maui.LineBreakMode.WordWrap };
        _statusLabel = new Label { TextColor = Colors.DarkRed, LineBreakMode = Microsoft.Maui.LineBreakMode.WordWrap };

        _titleEntry = new Entry { Placeholder = "Titel" };
        _descriptionEditor = new Editor { AutoSize = EditorAutoSizeOption.TextChanges, HeightRequest = 140, Placeholder = "Beschreibung" };
        _datePicker = new DatePicker { Date = DateTime.Today };

        _startTimePicker = new TimePicker { Time = new TimeSpan(8, 0, 0), IsEnabled = false };
        _hasStartTimeCheckBox = new CheckBox();
        _hasStartTimeCheckBox.CheckedChanged += (_, e) => _startTimePicker.IsEnabled = e.Value;

        _endTimePicker = new TimePicker { Time = new TimeSpan(12, 0, 0), IsEnabled = false };
        _hasEndTimeCheckBox = new CheckBox();
        _hasEndTimeCheckBox.CheckedChanged += (_, e) => _endTimePicker.IsEnabled = e.Value;

        _visibleFromDatePicker = new DatePicker { Date = DateTime.Today, IsEnabled = false };
        _visibleFromTimePicker = new TimePicker { Time = new TimeSpan(8, 0, 0), IsEnabled = false };
        _hasVisibleFromCheckBox = new CheckBox();
        _hasVisibleFromCheckBox.CheckedChanged += (_, e) => SetTimestampEnabled(_visibleFromDatePicker, _visibleFromTimePicker, e.Value);

        _visibleToDatePicker = new DatePicker { Date = DateTime.Today, IsEnabled = false };
        _visibleToTimePicker = new TimePicker { Time = new TimeSpan(18, 0, 0), IsEnabled = false };
        _hasVisibleToCheckBox = new CheckBox();
        _hasVisibleToCheckBox.CheckedChanged += (_, e) => SetTimestampEnabled(_visibleToDatePicker, _visibleToTimePicker, e.Value);

        _activeSwitch = new Switch { IsToggled = true };

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
                    CreateField("Beschreibung", _descriptionEditor),
                    CreateField("Datum *", _datePicker),
                    CreateCheckField("Startzeit verwenden", _hasStartTimeCheckBox),
                    CreateField("Startzeit", _startTimePicker),
                    CreateCheckField("Endzeit verwenden", _hasEndTimeCheckBox),
                    CreateField("Endzeit", _endTimePicker),
                    CreateCheckField("Sichtbar ab verwenden", _hasVisibleFromCheckBox),
                    CreateTimestampField("Sichtbar ab", _visibleFromDatePicker, _visibleFromTimePicker),
                    CreateCheckField("Sichtbar bis verwenden", _hasVisibleToCheckBox),
                    CreateTimestampField("Sichtbar bis", _visibleToDatePicker, _visibleToTimePicker),
                    CreateField("Aktiv", _activeSwitch),
                    new HorizontalStackLayout
                    {
                        Spacing = 8,
                        Children = { _cancelButton, _saveButton }
                    }
                }
            }
        };
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
                _headlineLabel.Text = "Termin";
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
        var records = await _supabaseService.GetTermineVerwaltungAsync();
        _existingRecord = records.FirstOrDefault(x => x.Id == entryId);
        if (_existingRecord == null)
        {
            _headlineLabel.Text = "Termin bearbeiten";
            _descriptionLabel.Text = "Der angeforderte Termin konnte nicht geladen werden. Bitte kehre zur Übersicht zurück und öffne ihn erneut.";
            SetEnabledState(false);
            return;
        }

        Title = "Termin bearbeiten";
        _headlineLabel.Text = "Termin bearbeiten";
        _descriptionLabel.Text = "Eigener mobiler Editorpfad für bestehende Termine. Die Übersicht bleibt dadurch eine ruhige chronologische Listenansicht.";
        _titleEntry.Text = _existingRecord.Titel ?? string.Empty;
        _descriptionEditor.Text = _existingRecord.Beschreibung ?? string.Empty;
        _datePicker.Date = _existingRecord.Datum == default ? DateTime.Today : _existingRecord.Datum.Date;
        _hasStartTimeCheckBox.IsChecked = _existingRecord.StartUhrzeit.HasValue;
        _startTimePicker.Time = _existingRecord.StartUhrzeit ?? new TimeSpan(8, 0, 0);
        _hasEndTimeCheckBox.IsChecked = _existingRecord.EndUhrzeit.HasValue;
        _endTimePicker.Time = _existingRecord.EndUhrzeit ?? new TimeSpan(12, 0, 0);
        _hasVisibleFromCheckBox.IsChecked = _existingRecord.SichtbarAb.HasValue;
        _visibleFromDatePicker.Date = _existingRecord.SichtbarAb?.Date ?? _datePicker.Date;
        _visibleFromTimePicker.Time = _existingRecord.SichtbarAb?.TimeOfDay ?? new TimeSpan(8, 0, 0);
        _hasVisibleToCheckBox.IsChecked = _existingRecord.SichtbarBis.HasValue;
        _visibleToDatePicker.Date = _existingRecord.SichtbarBis?.Date ?? _datePicker.Date;
        _visibleToTimePicker.Time = _existingRecord.SichtbarBis?.TimeOfDay ?? new TimeSpan(18, 0, 0);
        _activeSwitch.IsToggled = _existingRecord.Aktiv;
        SetEnabledState(true);
    }

    private void ConfigureNewRecord()
    {
        _existingRecord = null;
        Title = "Termin neu";
        _headlineLabel.Text = "Neuer Termin";
        _descriptionLabel.Text = "Eigener mobiler Editorpfad für neue Termine. Übersicht und Bearbeitung bleiben dadurch fachlich getrennt.";
        _titleEntry.Text = string.Empty;
        _descriptionEditor.Text = string.Empty;
        _datePicker.Date = DateTime.Today;
        _hasStartTimeCheckBox.IsChecked = false;
        _startTimePicker.Time = new TimeSpan(8, 0, 0);
        _hasEndTimeCheckBox.IsChecked = false;
        _endTimePicker.Time = new TimeSpan(12, 0, 0);
        _hasVisibleFromCheckBox.IsChecked = false;
        _visibleFromDatePicker.Date = DateTime.Today;
        _visibleFromTimePicker.Time = new TimeSpan(8, 0, 0);
        _hasVisibleToCheckBox.IsChecked = false;
        _visibleToDatePicker.Date = DateTime.Today;
        _visibleToTimePicker.Time = new TimeSpan(18, 0, 0);
        _activeSwitch.IsToggled = true;
        SetEnabledState(true);
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
                var created = await _supabaseService.CreateTerminAsync(record);
                if (created == null)
                {
                    _statusLabel.Text = "Termin konnte nicht erstellt werden.";
                    return;
                }
            }
            else
            {
                var success = await _supabaseService.UpdateTerminAsync(record);
                if (!success)
                {
                    _statusLabel.Text = "Termin konnte nicht gespeichert werden.";
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

    private bool TryBuildRecord(out TerminRecord record)
    {
        record = new TerminRecord();

        if (string.IsNullOrWhiteSpace(_titleEntry.Text))
        {
            _statusLabel.Text = "Titel ist ein Pflichtfeld.";
            _titleEntry.Focus();
            return false;
        }

        TimeSpan? startTime = _hasStartTimeCheckBox.IsChecked ? _startTimePicker.Time : null;
        TimeSpan? endTime = _hasEndTimeCheckBox.IsChecked ? _endTimePicker.Time : null;
        if (startTime.HasValue && endTime.HasValue && endTime.Value < startTime.Value)
        {
            _statusLabel.Text = "Die Endzeit darf nicht vor der Startzeit liegen.";
            _endTimePicker.Focus();
            return false;
        }

        var visibleFrom = _hasVisibleFromCheckBox.IsChecked
            ? _visibleFromDatePicker.Date.Date.Add(_visibleFromTimePicker.Time)
            : (DateTime?)null;
        var visibleTo = _hasVisibleToCheckBox.IsChecked
            ? _visibleToDatePicker.Date.Date.Add(_visibleToTimePicker.Time)
            : (DateTime?)null;

        if (visibleFrom.HasValue && visibleTo.HasValue && visibleTo.Value < visibleFrom.Value)
        {
            _statusLabel.Text = "Sichtbar bis darf nicht vor Sichtbar ab liegen.";
            _visibleToTimePicker.Focus();
            return false;
        }

        record = new TerminRecord
        {
            Titel = _titleEntry.Text.Trim(),
            Beschreibung = string.IsNullOrWhiteSpace(_descriptionEditor.Text) ? null : _descriptionEditor.Text.Trim(),
            Datum = _datePicker.Date,
            StartUhrzeit = startTime,
            EndUhrzeit = endTime,
            SichtbarAb = visibleFrom,
            SichtbarBis = visibleTo,
            Aktiv = _activeSwitch.IsToggled
        };

        if (_existingRecord != null)
            record.Id = _existingRecord.Id;

        return true;
    }

    private void SetEnabledState(bool enabled)
    {
        _titleEntry.IsEnabled = enabled;
        _descriptionEditor.IsEnabled = enabled;
        _datePicker.IsEnabled = enabled;
        _hasStartTimeCheckBox.IsEnabled = enabled;
        _startTimePicker.IsEnabled = enabled && _hasStartTimeCheckBox.IsChecked;
        _hasEndTimeCheckBox.IsEnabled = enabled;
        _endTimePicker.IsEnabled = enabled && _hasEndTimeCheckBox.IsChecked;
        _hasVisibleFromCheckBox.IsEnabled = enabled;
        SetTimestampEnabled(_visibleFromDatePicker, _visibleFromTimePicker, enabled && _hasVisibleFromCheckBox.IsChecked);
        _hasVisibleToCheckBox.IsEnabled = enabled;
        SetTimestampEnabled(_visibleToDatePicker, _visibleToTimePicker, enabled && _hasVisibleToCheckBox.IsChecked);
        _activeSwitch.IsEnabled = enabled;
        _saveButton.IsEnabled = enabled;
        _cancelButton.IsEnabled = enabled;
    }

    private static void SetTimestampEnabled(DatePicker datePicker, TimePicker timePicker, bool enabled)
    {
        datePicker.IsEnabled = enabled;
        timePicker.IsEnabled = enabled;
    }

    private Task NavigateToOverviewAsync()
    {
        return Shell.Current.GoToAsync("//home");
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

    private static View CreateCheckField(string title, CheckBox checkBox)
    {
        return new HorizontalStackLayout
        {
            Spacing = 8,
            Children =
            {
                checkBox,
                new Label { Text = title, VerticalTextAlignment = Microsoft.Maui.TextAlignment.Center }
            }
        };
    }

    private static View CreateTimestampField(string title, DatePicker datePicker, TimePicker timePicker)
    {
        Grid.SetColumn(datePicker, 0);
        Grid.SetColumn(timePicker, 1);

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
                        new ColumnDefinition(Microsoft.Maui.GridLength.Star),
                        new ColumnDefinition(new Microsoft.Maui.GridLength(140))
                    },
                    ColumnSpacing = 8,
                    Children =
                    {
                        datePicker,
                        timePicker
                    }
                }
            }
        };
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
