using KGV.Core.Interfaces;
using KGV.Core.Models;
using KGV.Maui.State;
using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace KGV.Maui.Pages;

public sealed class ArbeitseinsaetzeEditorPage : ContentPage, IQueryAttributable
{
    private readonly ISupabaseService _supabaseService;
    private readonly UserContextState _userContextState;
    private readonly ArbeitseinsaetzeManagementState _managementState;

    private readonly Entry _titleEntry;
    private readonly Editor _descriptionEditor;
    private readonly DatePicker _datePicker;
    private readonly CheckBox _hasStartTimeCheckBox;
    private readonly TimePicker _startTimePicker;
    private readonly CheckBox _hasEndTimeCheckBox;
    private readonly TimePicker _endTimePicker;
    private readonly Entry _treffpunktEntry;
    private readonly CheckBox _hasTeilnehmerbegrenzungCheckBox;
    private readonly Entry _maxTeilnehmerEntry;
    private readonly Entry _stundenWertEntry;
    private readonly CheckBox _hasSichtbarAbCheckBox;
    private readonly DatePicker _sichtbarAbDatePicker;
    private readonly TimePicker _sichtbarAbTimePicker;
    private readonly CheckBox _hasSichtbarBisCheckBox;
    private readonly DatePicker _sichtbarBisDatePicker;
    private readonly TimePicker _sichtbarBisTimePicker;
    private readonly CheckBox _hasAnmeldungBisCheckBox;
    private readonly DatePicker _anmeldungBisDatePicker;
    private readonly TimePicker _anmeldungBisTimePicker;
    private readonly Switch _aktivSwitch;
    private readonly Label _statusLabel;
    private readonly Label _positionLabel;
    private readonly Button _saveButton;
    private readonly Button _previousButton;
    private readonly Button _nextButton;

    private long? _editingEntryId;
    private EditorSnapshot? _initialSnapshot;
    private bool _isBusy;

    public ArbeitseinsaetzeEditorPage(
        ISupabaseService supabaseService,
        UserContextState userContextState,
        ArbeitseinsaetzeManagementState managementState)
    {
        _supabaseService = supabaseService;
        _userContextState = userContextState;
        _managementState = managementState;

        Title = "Arbeitseinsatz";
        BackgroundColor = Color.FromArgb("#F5F5F5");

        _titleEntry = new Entry { Placeholder = "Titel des Arbeitseinsatzes" };
        _descriptionEditor = new Editor { Placeholder = "Beschreibung (optional)", HeightRequest = 110, AutoSize = EditorAutoSizeOption.TextChanges };
        _datePicker = new DatePicker { Date = DateTime.Today };

        _hasStartTimeCheckBox = new CheckBox();
        _startTimePicker = new TimePicker { IsEnabled = false };
        _hasStartTimeCheckBox.CheckedChanged += (_, e) => _startTimePicker.IsEnabled = e.Value;

        _hasEndTimeCheckBox = new CheckBox();
        _endTimePicker = new TimePicker { IsEnabled = false };
        _hasEndTimeCheckBox.CheckedChanged += (_, e) => _endTimePicker.IsEnabled = e.Value;

        _treffpunktEntry = new Entry { Placeholder = "Treffpunkt (optional)" };

        _hasTeilnehmerbegrenzungCheckBox = new CheckBox();
        _maxTeilnehmerEntry = new Entry { Placeholder = "Max. Teilnehmer", Keyboard = Keyboard.Numeric, IsEnabled = false };
        _hasTeilnehmerbegrenzungCheckBox.CheckedChanged += (_, e) => _maxTeilnehmerEntry.IsEnabled = e.Value;

        _stundenWertEntry = new Entry { Placeholder = "Stundenwert (optional)", Keyboard = Keyboard.Numeric };

        _hasSichtbarAbCheckBox = new CheckBox();
        _sichtbarAbDatePicker = new DatePicker { Date = DateTime.Today, IsEnabled = false };
        _sichtbarAbTimePicker = new TimePicker { IsEnabled = false };
        _hasSichtbarAbCheckBox.CheckedChanged += (_, e) =>
        {
            _sichtbarAbDatePicker.IsEnabled = e.Value;
            _sichtbarAbTimePicker.IsEnabled = e.Value;
        };

        _hasSichtbarBisCheckBox = new CheckBox();
        _sichtbarBisDatePicker = new DatePicker { Date = DateTime.Today, IsEnabled = false };
        _sichtbarBisTimePicker = new TimePicker { IsEnabled = false };
        _hasSichtbarBisCheckBox.CheckedChanged += (_, e) =>
        {
            _sichtbarBisDatePicker.IsEnabled = e.Value;
            _sichtbarBisTimePicker.IsEnabled = e.Value;
        };

        _hasAnmeldungBisCheckBox = new CheckBox();
        _anmeldungBisDatePicker = new DatePicker { Date = DateTime.Today, IsEnabled = false };
        _anmeldungBisTimePicker = new TimePicker { IsEnabled = false };
        _hasAnmeldungBisCheckBox.CheckedChanged += (_, e) =>
        {
            _anmeldungBisDatePicker.IsEnabled = e.Value;
            _anmeldungBisTimePicker.IsEnabled = e.Value;
        };

        _aktivSwitch = new Switch { IsToggled = true };
        _statusLabel = new Label { TextColor = Colors.IndianRed, LineBreakMode = LineBreakMode.WordWrap, IsVisible = false };
        _positionLabel = new Label
        {
            HorizontalOptions = LayoutOptions.Fill,
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center,
            IsVisible = false
        };

        _saveButton = new Button { Text = "Speichern", WidthRequest = 120 };
        _saveButton.Clicked += async (_, _) => await SaveAsync(navigateToOverviewAfterSave: false);

        var cancelButton = new Button { Text = "Zur Übersicht", WidthRequest = 120 };
        cancelButton.Clicked += async (_, _) => await NavigateToOverviewAsync();

        _previousButton = new Button { Text = "←", WidthRequest = 56, IsVisible = false };
        _previousButton.Clicked += async (_, _) => await MovePreviousAsync();

        _nextButton = new Button { Text = "→", WidthRequest = 56, IsVisible = false };
        _nextButton.Clicked += async (_, _) => await MoveNextAsync();

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = 24,
                Spacing = 14,
                Children =
                {
                    new Label { Text = "Arbeitseinsatz-Editor", FontSize = 24, FontAttributes = FontAttributes.Bold },
                    new Label
                    {
                        Text = "Getrennter mobiler Produktivpfad für Verwaltung mit ruhiger Datensatznavigation statt Mischseite.",
                        TextColor = Colors.Gray,
                        LineBreakMode = LineBreakMode.WordWrap
                    },
                    CreateField("Titel *", _titleEntry),
                    CreateField("Beschreibung", _descriptionEditor),
                    CreateField("Datum *", _datePicker),
                    CreateCheckField("Startzeit verwenden", _hasStartTimeCheckBox),
                    CreateField("Startzeit", _startTimePicker),
                    CreateCheckField("Endzeit verwenden", _hasEndTimeCheckBox),
                    CreateField("Endzeit", _endTimePicker),
                    CreateField("Treffpunkt", _treffpunktEntry),
                    CreateCheckField("Teilnehmerbegrenzung", _hasTeilnehmerbegrenzungCheckBox),
                    CreateField("Max. Teilnehmer", _maxTeilnehmerEntry),
                    CreateField("Stundenwert", _stundenWertEntry),
                    CreateCheckField("Sichtbar ab verwenden", _hasSichtbarAbCheckBox),
                    CreateField("Sichtbar ab Datum", _sichtbarAbDatePicker),
                    CreateField("Sichtbar ab Zeit", _sichtbarAbTimePicker),
                    CreateCheckField("Sichtbar bis verwenden", _hasSichtbarBisCheckBox),
                    CreateField("Sichtbar bis Datum", _sichtbarBisDatePicker),
                    CreateField("Sichtbar bis Zeit", _sichtbarBisTimePicker),
                    CreateCheckField("Anmeldung bis verwenden", _hasAnmeldungBisCheckBox),
                    CreateField("Anmeldung bis Datum", _anmeldungBisDatePicker),
                    CreateField("Anmeldung bis Zeit", _anmeldungBisTimePicker),
                    CreateSwitchField("Aktiv", _aktivSwitch),
                    _statusLabel,
                    new HorizontalStackLayout
                    {
                        Spacing = 12,
                        Margin = new Thickness(0, 12, 0, 0),
                        Children = { cancelButton, _saveButton }
                    },
                    CreateNavigationFooter()
                }
            }
        };
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("entryId", out var entryIdObj))
        {
            var entryIdText = entryIdObj?.ToString();
            if (long.TryParse(entryIdText, out var entryId))
                _editingEntryId = entryId;
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        if (_isBusy)
            return;

        if (_userContextState.CurrentUserContext?.Role is not (KGV.Core.Security.UserRole.Admin or KGV.Core.Security.UserRole.Vorstand))
        {
            _statusLabel.Text = "Keine Berechtigung.";
            _statusLabel.TextColor = Colors.IndianRed;
            _statusLabel.IsVisible = true;
            _saveButton.IsEnabled = false;
            return;
        }

        await EnsureNavigationStateAsync(_editingEntryId);

        if (_editingEntryId.HasValue)
        {
            if (!_managementState.SetCurrentById(_editingEntryId.Value) || _managementState.CurrentEntry == null)
            {
                _statusLabel.Text = "Arbeitseinsatz nicht gefunden.";
                _statusLabel.TextColor = Colors.IndianRed;
                _statusLabel.IsVisible = true;
                _saveButton.IsEnabled = false;
                return;
            }

            ApplyRecordToForm(_managementState.CurrentEntry);
            Title = "Arbeitseinsatz bearbeiten";
        }
        else
        {
            ResetEditorForNew();
            Title = "Neuer Arbeitseinsatz";
        }

        UpdateNavigationFooter();
    }

    private async Task EnsureNavigationStateAsync(long? selectedEntryId = null)
    {
        if (_managementState.TotalCount == 0 || (selectedEntryId.HasValue && !_managementState.SetCurrentById(selectedEntryId.Value)))
            await ReloadNavigationStateAsync(selectedEntryId);
    }

    private async Task ReloadNavigationStateAsync(long? selectedEntryId = null)
    {
        var entries = await _supabaseService.GetArbeitseinsaetzeVerwaltungAsync();
        _managementState.SetEntries(entries, selectedEntryId);
    }

    private void ApplyRecordToForm(ArbeitseinsatzRecord record)
    {
        _editingEntryId = record.Id;
        _titleEntry.Text = record.Titel ?? string.Empty;
        _descriptionEditor.Text = record.Beschreibung ?? string.Empty;
        _datePicker.Date = record.Datum == default ? DateTime.Today : record.Datum.Date;

        _hasStartTimeCheckBox.IsChecked = record.StartUhrzeit.HasValue;
        _startTimePicker.Time = record.StartUhrzeit ?? new TimeSpan(8, 0, 0);

        _hasEndTimeCheckBox.IsChecked = record.EndUhrzeit.HasValue;
        _endTimePicker.Time = record.EndUhrzeit ?? new TimeSpan(12, 0, 0);

        _treffpunktEntry.Text = record.Treffpunkt ?? string.Empty;
        _hasTeilnehmerbegrenzungCheckBox.IsChecked = record.MaxTeilnehmer.HasValue;
        _maxTeilnehmerEntry.Text = record.MaxTeilnehmer?.ToString(CultureInfo.CurrentCulture) ?? string.Empty;
        _stundenWertEntry.Text = record.StundenWert > 0
            ? record.StundenWert.ToString("0.##", CultureInfo.CurrentCulture)
            : string.Empty;

        _hasSichtbarAbCheckBox.IsChecked = record.SichtbarAb.HasValue;
        _sichtbarAbDatePicker.Date = record.SichtbarAb?.Date ?? DateTime.Today;
        _sichtbarAbTimePicker.Time = record.SichtbarAb?.TimeOfDay ?? TimeSpan.Zero;

        _hasSichtbarBisCheckBox.IsChecked = record.SichtbarBis.HasValue;
        _sichtbarBisDatePicker.Date = record.SichtbarBis?.Date ?? DateTime.Today;
        _sichtbarBisTimePicker.Time = record.SichtbarBis?.TimeOfDay ?? TimeSpan.Zero;

        _hasAnmeldungBisCheckBox.IsChecked = record.AnmeldungBis.HasValue;
        _anmeldungBisDatePicker.Date = record.AnmeldungBis?.Date ?? DateTime.Today;
        _anmeldungBisTimePicker.Time = record.AnmeldungBis?.TimeOfDay ?? TimeSpan.Zero;

        _aktivSwitch.IsToggled = record.Aktiv;
        _statusLabel.IsVisible = false;
        _saveButton.IsEnabled = true;
        _initialSnapshot = CaptureSnapshot();
    }

    private void ResetEditorForNew()
    {
        _editingEntryId = null;
        _titleEntry.Text = string.Empty;
        _descriptionEditor.Text = string.Empty;
        _datePicker.Date = DateTime.Today;
        _hasStartTimeCheckBox.IsChecked = false;
        _startTimePicker.Time = new TimeSpan(8, 0, 0);
        _hasEndTimeCheckBox.IsChecked = false;
        _endTimePicker.Time = new TimeSpan(12, 0, 0);
        _treffpunktEntry.Text = string.Empty;
        _hasTeilnehmerbegrenzungCheckBox.IsChecked = false;
        _maxTeilnehmerEntry.Text = string.Empty;
        _stundenWertEntry.Text = string.Empty;
        _hasSichtbarAbCheckBox.IsChecked = false;
        _sichtbarAbDatePicker.Date = DateTime.Today;
        _sichtbarAbTimePicker.Time = TimeSpan.Zero;
        _hasSichtbarBisCheckBox.IsChecked = false;
        _sichtbarBisDatePicker.Date = DateTime.Today;
        _sichtbarBisTimePicker.Time = TimeSpan.Zero;
        _hasAnmeldungBisCheckBox.IsChecked = false;
        _anmeldungBisDatePicker.Date = DateTime.Today;
        _anmeldungBisTimePicker.Time = TimeSpan.Zero;
        _aktivSwitch.IsToggled = true;
        _statusLabel.IsVisible = false;
        _saveButton.IsEnabled = true;
        _initialSnapshot = CaptureSnapshot();
    }

    private async Task<bool> SaveAsync(bool navigateToOverviewAfterSave)
    {
        if (_isBusy)
            return false;

        _statusLabel.IsVisible = false;
        if (!TryBuildRecord(out var record))
            return false;

        _isBusy = true;
        try
        {
            ArbeitseinsatzRecord? persistedRecord;
            if (_editingEntryId.HasValue)
            {
                var updated = await _supabaseService.UpdateArbeitseinsatzAsync(record!);
                if (!updated)
                {
                    _statusLabel.Text = "Der Arbeitseinsatz konnte nicht gespeichert werden.";
                    _statusLabel.TextColor = Colors.IndianRed;
                    _statusLabel.IsVisible = true;
                    return false;
                }

                persistedRecord = record;
                _statusLabel.Text = "Arbeitseinsatz aktualisiert.";
            }
            else
            {
                persistedRecord = await _supabaseService.CreateArbeitseinsatzAsync(record!);
                if (persistedRecord == null)
                {
                    _statusLabel.Text = "Der Arbeitseinsatz konnte nicht erstellt werden.";
                    _statusLabel.TextColor = Colors.IndianRed;
                    _statusLabel.IsVisible = true;
                    return false;
                }

                _editingEntryId = persistedRecord.Id;
                _statusLabel.Text = "Arbeitseinsatz erstellt.";
            }

            _statusLabel.TextColor = Colors.Green;
            _statusLabel.IsVisible = true;

            await ReloadNavigationStateAsync(_editingEntryId);
            if (_editingEntryId.HasValue && _managementState.CurrentEntry != null)
                ApplyRecordToForm(_managementState.CurrentEntry);
            else if (persistedRecord != null)
                ApplyRecordToForm(persistedRecord);

            UpdateNavigationFooter();

            if (navigateToOverviewAfterSave)
                await NavigateToOverviewAsync();

            return true;
        }
        catch (Exception ex)
        {
            _statusLabel.Text = $"Fehler beim Speichern: {ex.Message}";
            _statusLabel.TextColor = Colors.IndianRed;
            _statusLabel.IsVisible = true;
            return false;
        }
        finally
        {
            _isBusy = false;
        }
    }

    private bool TryBuildRecord(out ArbeitseinsatzRecord? record)
    {
        record = null;

        if (string.IsNullOrWhiteSpace(_titleEntry.Text))
        {
            ShowValidationError("Titel ist ein Pflichtfeld.", _titleEntry);
            return false;
        }

        TimeSpan? startTime = _hasStartTimeCheckBox.IsChecked ? _startTimePicker.Time : null;
        TimeSpan? endTime = _hasEndTimeCheckBox.IsChecked ? _endTimePicker.Time : null;
        if (startTime.HasValue && endTime.HasValue && endTime.Value < startTime.Value)
        {
            ShowValidationError("Die Endzeit darf nicht vor der Startzeit liegen.", _endTimePicker);
            return false;
        }

        int? maxTeilnehmer = null;
        if (_hasTeilnehmerbegrenzungCheckBox.IsChecked)
        {
            if (!int.TryParse(_maxTeilnehmerEntry.Text?.Trim(), out var parsedMax) || parsedMax <= 0)
            {
                ShowValidationError("Max. Teilnehmer muss größer als 0 sein, wenn eine Begrenzung aktiv ist.", _maxTeilnehmerEntry);
                return false;
            }

            maxTeilnehmer = parsedMax;
        }

        var stundenWert = 0m;
        if (!string.IsNullOrWhiteSpace(_stundenWertEntry.Text))
        {
            if (!TryParseDecimalFlexible(_stundenWertEntry.Text.Trim(), out var parsedStundenWert) || parsedStundenWert < 0)
            {
                ShowValidationError("Stundenwert muss größer oder gleich 0 sein.", _stundenWertEntry);
                return false;
            }

            stundenWert = parsedStundenWert;
        }

        DateTime? sichtbarAb = _hasSichtbarAbCheckBox.IsChecked
            ? _sichtbarAbDatePicker.Date.Date.Add(_sichtbarAbTimePicker.Time)
            : null;
        DateTime? sichtbarBis = _hasSichtbarBisCheckBox.IsChecked
            ? _sichtbarBisDatePicker.Date.Date.Add(_sichtbarBisTimePicker.Time)
            : null;
        DateTime? anmeldungBis = _hasAnmeldungBisCheckBox.IsChecked
            ? _anmeldungBisDatePicker.Date.Date.Add(_anmeldungBisTimePicker.Time)
            : null;

        if (sichtbarAb.HasValue && sichtbarBis.HasValue && sichtbarBis.Value < sichtbarAb.Value)
        {
            ShowValidationError("Sichtbar bis darf nicht vor Sichtbar ab liegen.", _sichtbarBisDatePicker);
            return false;
        }

        record = new ArbeitseinsatzRecord
        {
            Id = _editingEntryId.GetValueOrDefault(),
            Titel = _titleEntry.Text?.Trim(),
            Beschreibung = string.IsNullOrWhiteSpace(_descriptionEditor.Text) ? null : _descriptionEditor.Text.Trim(),
            Datum = _datePicker.Date.Date,
            StartUhrzeit = startTime,
            EndUhrzeit = endTime,
            Treffpunkt = string.IsNullOrWhiteSpace(_treffpunktEntry.Text) ? null : _treffpunktEntry.Text.Trim(),
            MaxTeilnehmer = maxTeilnehmer,
            StundenWert = stundenWert,
            SichtbarAb = sichtbarAb,
            SichtbarBis = sichtbarBis,
            AnmeldungBis = anmeldungBis,
            Aktiv = _aktivSwitch.IsToggled
        };

        return true;
    }

    private void ShowValidationError(string message, VisualElement element)
    {
        _statusLabel.Text = message;
        _statusLabel.TextColor = Colors.IndianRed;
        _statusLabel.IsVisible = true;
        element.Focus();
    }

    private async Task MovePreviousAsync()
    {
        if (!await SavePendingChangesBeforeMoveAsync())
            return;

        if (!_managementState.MovePrevious() || _managementState.CurrentEntry == null)
            return;

        ApplyRecordToForm(_managementState.CurrentEntry);
        Title = "Arbeitseinsatz bearbeiten";
        UpdateNavigationFooter();
    }

    private async Task MoveNextAsync()
    {
        if (!await SavePendingChangesBeforeMoveAsync())
            return;

        if (!_managementState.MoveNext() || _managementState.CurrentEntry == null)
            return;

        ApplyRecordToForm(_managementState.CurrentEntry);
        Title = "Arbeitseinsatz bearbeiten";
        UpdateNavigationFooter();
    }

    private async Task<bool> SavePendingChangesBeforeMoveAsync()
    {
        if (!HasPendingChanges())
            return true;

        return await SaveAsync(navigateToOverviewAfterSave: false);
    }

    private bool HasPendingChanges()
    {
        return _initialSnapshot is { } snapshot && !snapshot.Equals(CaptureSnapshot());
    }

    private EditorSnapshot CaptureSnapshot()
    {
        return new EditorSnapshot(
            _titleEntry.Text ?? string.Empty,
            _descriptionEditor.Text ?? string.Empty,
            DateOnly.FromDateTime(_datePicker.Date),
            _hasStartTimeCheckBox.IsChecked,
            _hasStartTimeCheckBox.IsChecked ? _startTimePicker.Time : null,
            _hasEndTimeCheckBox.IsChecked,
            _hasEndTimeCheckBox.IsChecked ? _endTimePicker.Time : null,
            _treffpunktEntry.Text ?? string.Empty,
            _hasTeilnehmerbegrenzungCheckBox.IsChecked,
            _maxTeilnehmerEntry.Text ?? string.Empty,
            _stundenWertEntry.Text ?? string.Empty,
            _hasSichtbarAbCheckBox.IsChecked,
            _hasSichtbarAbCheckBox.IsChecked ? _sichtbarAbDatePicker.Date.Date.Add(_sichtbarAbTimePicker.Time) : null,
            _hasSichtbarBisCheckBox.IsChecked,
            _hasSichtbarBisCheckBox.IsChecked ? _sichtbarBisDatePicker.Date.Date.Add(_sichtbarBisTimePicker.Time) : null,
            _hasAnmeldungBisCheckBox.IsChecked,
            _hasAnmeldungBisCheckBox.IsChecked ? _anmeldungBisDatePicker.Date.Date.Add(_anmeldungBisTimePicker.Time) : null,
            _aktivSwitch.IsToggled);
    }

    private void UpdateNavigationFooter()
    {
        var showNavigation = _editingEntryId.HasValue && _managementState.TotalCount > 0;
        _previousButton.IsVisible = showNavigation;
        _nextButton.IsVisible = showNavigation;
        _positionLabel.IsVisible = showNavigation;
        _previousButton.IsEnabled = showNavigation && _managementState.CanMovePrevious;
        _nextButton.IsEnabled = showNavigation && _managementState.CanMoveNext;
        _positionLabel.Text = showNavigation
            ? $"{_managementState.CurrentIndex + 1}/{_managementState.TotalCount}"
            : string.Empty;
    }

    private Task NavigateToOverviewAsync()
    {
        return Shell.Current.GoToAsync("management_workassignments");
    }

    private static View CreateField(string labelText, View input)
    {
        return new VerticalStackLayout
        {
            Spacing = 6,
            Children =
            {
                new Label { Text = labelText, FontAttributes = FontAttributes.Bold },
                input
            }
        };
    }

    private static View CreateCheckField(string labelText, CheckBox checkBox)
    {
        return new HorizontalStackLayout
        {
            Spacing = 8,
            Children =
            {
                checkBox,
                new Label { Text = labelText, VerticalOptions = LayoutOptions.Center }
            }
        };
    }

    private static View CreateSwitchField(string labelText, Switch control)
    {
        return new HorizontalStackLayout
        {
            Spacing = 8,
            Children =
            {
                new Label { Text = labelText, VerticalOptions = LayoutOptions.Center },
                control
            }
        };
    }

    private Grid CreateNavigationFooter()
    {
        var grid = new Grid
        {
            Margin = new Thickness(0, 8, 0, 0),
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            }
        };

        grid.Add(_previousButton);
        Grid.SetColumn(_previousButton, 0);

        grid.Add(_positionLabel);
        Grid.SetColumn(_positionLabel, 1);

        grid.Add(_nextButton);
        Grid.SetColumn(_nextButton, 2);

        return grid;
    }

    private static bool TryParseDecimalFlexible(string rawValue, out decimal value)
    {
        if (decimal.TryParse(rawValue, NumberStyles.Number, CultureInfo.CurrentCulture, out value))
            return true;

        return decimal.TryParse(rawValue.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out value);
    }

    private readonly record struct EditorSnapshot(
        string Title,
        string Description,
        DateOnly Date,
        bool HasStartTime,
        TimeSpan? StartTime,
        bool HasEndTime,
        TimeSpan? EndTime,
        string Treffpunkt,
        bool HasParticipantLimit,
        string MaxParticipants,
        string HoursValue,
        bool HasVisibleFrom,
        DateTime? VisibleFrom,
        bool HasVisibleTo,
        DateTime? VisibleTo,
        bool HasRegistrationUntil,
        DateTime? RegistrationUntil,
        bool IsActive);
}

