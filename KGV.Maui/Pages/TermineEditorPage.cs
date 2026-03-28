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
    private readonly TimePicker _startTimePicker;
    private readonly TimePicker _endTimePicker;
    private readonly DatePicker _visibleFromDatePicker;
    private readonly TimePicker _visibleFromTimePicker;
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
        var defaultStartTime = CreateTerminStartDefault();
        var defaultVisibleFrom = CreateCurrentTimestampDefault();
        var defaultVisibleTo = CreateVisibleUntilEndOfDay(_datePicker.Date);

        _startTimePicker = new TimePicker { Time = defaultStartTime };

        _endTimePicker = new TimePicker { Time = CreateTerminEndDefault(defaultStartTime) };

        _visibleFromDatePicker = new DatePicker { Date = defaultVisibleFrom.Date };
        _visibleFromTimePicker = new TimePicker { Time = defaultVisibleFrom.TimeOfDay };

        _visibleToDatePicker = new DatePicker { Date = defaultVisibleTo.Date };
        _visibleToTimePicker = new TimePicker { Time = defaultVisibleTo.TimeOfDay };

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
                    CreateField("Startzeit", _startTimePicker),
                    CreateField("Endzeit", _endTimePicker),
                    CreateTimestampField("Sichtbar ab", _visibleFromDatePicker, _visibleFromTimePicker),
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
        _startTimePicker.Time = _existingRecord.StartUhrzeit ?? CreateTerminStartDefault();
        _endTimePicker.Time = _existingRecord.EndUhrzeit ?? CreateTerminEndDefault(_startTimePicker.Time);
        var visibleFrom = _existingRecord.SichtbarAb ?? CreateCurrentTimestampDefault();
        _visibleFromDatePicker.Date = visibleFrom.Date;
        _visibleFromTimePicker.Time = visibleFrom.TimeOfDay;
        var visibleTo = _existingRecord.SichtbarBis ?? CreateVisibleUntilEndOfDay(_datePicker.Date);
        _visibleToDatePicker.Date = visibleTo.Date;
        _visibleToTimePicker.Time = visibleTo.TimeOfDay;
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
        var startTime = CreateTerminStartDefault();
        _startTimePicker.Time = startTime;
        _endTimePicker.Time = CreateTerminEndDefault(startTime);
        var visibleFrom = CreateCurrentTimestampDefault();
        _visibleFromDatePicker.Date = visibleFrom.Date;
        _visibleFromTimePicker.Time = visibleFrom.TimeOfDay;
        var visibleTo = CreateVisibleUntilEndOfDay(_datePicker.Date);
        _visibleToDatePicker.Date = visibleTo.Date;
        _visibleToTimePicker.Time = visibleTo.TimeOfDay;
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
                var created = await _supabaseService.CreateTerminAsync(record.ToInsertRecord());
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

        var startTime = _startTimePicker.Time;
        var endTime = _endTimePicker.Time;
        if (endTime < startTime)
        {
            _statusLabel.Text = "Die Endzeit darf nicht vor der Startzeit liegen.";
            _endTimePicker.Focus();
            return false;
        }

        var visibleFrom = _visibleFromDatePicker.Date.Date.Add(_visibleFromTimePicker.Time);
        var visibleTo = _visibleToDatePicker.Date.Date.Add(_visibleToTimePicker.Time);

        if (visibleTo < visibleFrom)
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
        _startTimePicker.IsEnabled = enabled;
        _endTimePicker.IsEnabled = enabled;
        _visibleFromDatePicker.IsEnabled = enabled;
        _visibleFromTimePicker.IsEnabled = enabled;
        _visibleToDatePicker.IsEnabled = enabled;
        _visibleToTimePicker.IsEnabled = enabled;
        _activeSwitch.IsEnabled = enabled;
        _saveButton.IsEnabled = enabled;
        _cancelButton.IsEnabled = enabled;
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

    private static DateTime CreateCurrentTimestampDefault()
    {
        var now = DateTime.Now;
        return new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, 0);
    }

    private static TimeSpan CreateTerminStartDefault()
    {
        var now = CreateCurrentTimestampDefault();
        return now.TimeOfDay;
    }

    private static TimeSpan CreateTerminEndDefault(TimeSpan start)
    {
        var candidate = start.Add(TimeSpan.FromHours(1));
        return candidate > new TimeSpan(23, 59, 0) ? new TimeSpan(23, 59, 0) : candidate;
    }

    private static DateTime CreateVisibleUntilEndOfDay(DateTime date)
        => new(date.Year, date.Month, date.Day, 23, 59, 0);
}
