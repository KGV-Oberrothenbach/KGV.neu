using KGV.Core.Interfaces;
using KGV.Core.Models;
using KGV.Core.Security;
using KGV.Maui.State;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;

namespace KGV.Maui.Pages;

public sealed class HomeManagementPage : ContentPage, IQueryAttributable
{
    private enum ManagementSection
    {
        WorkAssignments,
        Appointments,
        Announcements
    }

    private readonly ISupabaseService _supabaseService;
    private readonly UserContextState _userContextState;
    private readonly ObservableCollection<ManagementEntry> _entries = new();
    private readonly Picker _sectionPicker;
    private readonly CollectionView _entriesView;
    private readonly Label _descriptionLabel;
    private readonly Label _statusLabel;
    private readonly Label _editorCaptionLabel;
    private readonly Entry _titleEntry;
    private readonly Editor _descriptionEditor;
    private readonly DatePicker _datePicker;
    private readonly CheckBox _hasStartTimeCheckBox;
    private readonly TimePicker _startTimePicker;
    private readonly CheckBox _hasEndTimeCheckBox;
    private readonly TimePicker _endTimePicker;
    private readonly Entry _treffpunktEntry;
    private readonly CheckBox _hasMaxParticipantsCheckBox;
    private readonly Entry _maxParticipantsEntry;
    private readonly Entry _hoursEntry;
    private readonly Editor _htmlEditor;
    private readonly Entry _sortOrderEntry;
    private readonly Switch _activeSwitch;
    private readonly VerticalStackLayout _workAssignmentSection;
    private readonly VerticalStackLayout _appointmentSection;
    private readonly VerticalStackLayout _announcementSection;
    private readonly VerticalStackLayout _editorContainer;
    private readonly Button _saveButton;
    private readonly Button _newButton;
    private readonly Button _refreshButton;

    private List<ArbeitseinsatzRecord> _workAssignments = new();
    private List<TerminRecord> _appointments = new();
    private List<BekanntmachungRecord> _announcements = new();
    private ManagementSection _currentSection = ManagementSection.WorkAssignments;
    private long? _selectedEntryId;
    private long? _requestedEntryId;
    private bool _requestedNewMode;
    private bool _lockSectionSelection;
    private bool _initialized;
    private bool _isBusy;

    public HomeManagementPage(ISupabaseService supabaseService, UserContextState userContextState)
    {
        _supabaseService = supabaseService;
        _userContextState = userContextState;
        Title = "Verwaltung";

        _descriptionLabel = new Label { LineBreakMode = LineBreakMode.WordWrap, TextColor = Colors.Gray };
        _statusLabel = new Label { LineBreakMode = LineBreakMode.WordWrap, TextColor = Colors.DarkRed };
        _editorCaptionLabel = new Label { FontAttributes = FontAttributes.Bold, FontSize = 18 };

        _sectionPicker = new Picker { Title = "Bereich" };
        _sectionPicker.ItemsSource = new[] { "Arbeitseinsätze", "Termine", "Bekanntmachungen" };
        _sectionPicker.SelectedIndexChanged += async (_, _) =>
        {
            if (_sectionPicker.SelectedIndex < 0)
                return;

            _currentSection = (ManagementSection)_sectionPicker.SelectedIndex;
            await LoadCurrentSectionAsync(resetSelection: true);
        };

        _refreshButton = new Button { Text = "Aktualisieren" };
        _refreshButton.Clicked += async (_, _) => await LoadCurrentSectionAsync(resetSelection: false);

        _newButton = new Button { Text = "Neu" };
        _newButton.Clicked += (_, _) =>
        {
            _entriesView.SelectedItem = null;
            ResetEditorForNew();
        };

        _entriesView = new CollectionView
        {
            SelectionMode = SelectionMode.Single,
            HeightRequest = 220,
            ItemsSource = _entries,
            ItemTemplate = new DataTemplate(() =>
            {
                var title = new Label { FontAttributes = FontAttributes.Bold };
                title.SetBinding(Label.TextProperty, nameof(ManagementEntry.Title));

                var subtitle = new Label { FontSize = 12, TextColor = Colors.Gray, LineBreakMode = LineBreakMode.WordWrap };
                subtitle.SetBinding(Label.TextProperty, nameof(ManagementEntry.Subtitle));

                return new VerticalStackLayout
                {
                    Padding = new Thickness(0, 8),
                    Children = { title, subtitle }
                };
            })
        };
        _entriesView.SelectionChanged += (_, e) =>
        {
            var selected = e.CurrentSelection?.FirstOrDefault() as ManagementEntry;
            _selectedEntryId = selected?.Id;
            PopulateEditorFromSelection();
        };

        _titleEntry = new Entry { Placeholder = "Titel" };
        _descriptionEditor = new Editor { AutoSize = EditorAutoSizeOption.TextChanges, Placeholder = "Beschreibung" };
        _datePicker = new DatePicker { Date = DateTime.Today };
        _hasStartTimeCheckBox = new CheckBox();
        _hasStartTimeCheckBox.CheckedChanged += (_, e) => _startTimePicker.IsEnabled = e.Value;
        _startTimePicker = new TimePicker { Time = new TimeSpan(8, 0, 0) };
        _startTimePicker.IsEnabled = false;
        _hasEndTimeCheckBox = new CheckBox();
        _hasEndTimeCheckBox.CheckedChanged += (_, e) => _endTimePicker.IsEnabled = e.Value;
        _endTimePicker = new TimePicker { Time = new TimeSpan(12, 0, 0) };
        _endTimePicker.IsEnabled = false;
        _treffpunktEntry = new Entry { Placeholder = "Treffpunkt" };
        _hasMaxParticipantsCheckBox = new CheckBox();
        _hasMaxParticipantsCheckBox.CheckedChanged += (_, e) => _maxParticipantsEntry.IsEnabled = e.Value;
        _maxParticipantsEntry = new Entry { Placeholder = "Max. Teilnehmer", Keyboard = Keyboard.Numeric };
        _maxParticipantsEntry.IsEnabled = false;
        _hoursEntry = new Entry { Placeholder = "Stundenwert", Keyboard = Keyboard.Numeric };
        _htmlEditor = new Editor { AutoSize = EditorAutoSizeOption.TextChanges, Placeholder = "HTML-Inhalt" };
        _sortOrderEntry = new Entry { Placeholder = "Sortierreihenfolge", Keyboard = Keyboard.Numeric };
        _activeSwitch = new Switch { IsToggled = true };

        _workAssignmentSection = new VerticalStackLayout
        {
            Spacing = 8,
            Children =
            {
                CreateField("Titel", _titleEntry),
                CreateField("Beschreibung", _descriptionEditor),
                CreateField("Datum", _datePicker),
                CreateCheckField("Startzeit verwenden", _hasStartTimeCheckBox),
                CreateField("Startzeit", _startTimePicker),
                CreateCheckField("Endzeit verwenden", _hasEndTimeCheckBox),
                CreateField("Endzeit", _endTimePicker),
                CreateField("Treffpunkt", _treffpunktEntry),
                CreateCheckField("Teilnehmerbegrenzung", _hasMaxParticipantsCheckBox),
                CreateField("Max. Teilnehmer", _maxParticipantsEntry),
                CreateField("Stundenwert", _hoursEntry),
                CreateSwitchField("Aktiv", _activeSwitch)
            }
        };

        _appointmentSection = new VerticalStackLayout
        {
            Spacing = 8,
            Children =
            {
                CreateField("Titel", _titleEntry),
                CreateField("Beschreibung", _descriptionEditor),
                CreateField("Datum", _datePicker),
                CreateCheckField("Startzeit verwenden", _hasStartTimeCheckBox),
                CreateField("Startzeit", _startTimePicker),
                CreateCheckField("Endzeit verwenden", _hasEndTimeCheckBox),
                CreateField("Endzeit", _endTimePicker),
                CreateSwitchField("Aktiv", _activeSwitch)
            }
        };

        _announcementSection = new VerticalStackLayout
        {
            Spacing = 8,
            Children =
            {
                CreateField("Titel", _titleEntry),
                CreateField("HTML-Inhalt", _htmlEditor),
                CreateField("Sortierreihenfolge", _sortOrderEntry),
                CreateSwitchField("Aktiv", _activeSwitch)
            }
        };

        _saveButton = new Button { Text = "Speichern" };
        _saveButton.Clicked += async (_, _) => await SaveAsync();

        _editorContainer = new VerticalStackLayout
        {
            Spacing = 12,
            Children =
            {
                _editorCaptionLabel,
                _workAssignmentSection,
                _appointmentSection,
                _announcementSection,
                _saveButton
            }
        };

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = 24,
                Spacing = 12,
                Children =
                {
                    new Label { Text = "Verwaltungsoberflächen", FontSize = 24, FontAttributes = FontAttributes.Bold },
                    _descriptionLabel,
                    _sectionPicker,
                    new HorizontalStackLayout
                    {
                        Spacing = 8,
                        Children = { _refreshButton, _newButton }
                    },
                    _entriesView,
                    _editorContainer,
                    _statusLabel
                }
            }
        };
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("section", out var rawValue))
        {
            var value = rawValue?.ToString();
            _currentSection = value?.ToLowerInvariant() switch
            {
                "appointments" => ManagementSection.Appointments,
                "announcements" => ManagementSection.Announcements,
                _ => ManagementSection.WorkAssignments
            };
        }

        if (query.TryGetValue("entryId", out var entryIdRaw)
            && long.TryParse(entryIdRaw?.ToString(), out var entryId))
        {
            _requestedEntryId = entryId;
            _requestedNewMode = false;
        }

        if (query.TryGetValue("mode", out var modeRaw)
            && string.Equals(modeRaw?.ToString(), "new", StringComparison.OrdinalIgnoreCase))
        {
            _requestedNewMode = true;
            _requestedEntryId = null;
        }

        if (query.TryGetValue("lockSection", out var lockRaw)
            && bool.TryParse(lockRaw?.ToString(), out var lockSection))
        {
            _lockSectionSelection = lockSection;
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (_userContextState.CurrentUserContext?.Role is not (UserRole.Admin or UserRole.Vorstand))
        {
            _statusLabel.Text = "Diese Verwaltung ist nur für Admin/Vorstand verfügbar.";
            SetAuthorizedState(false);
            return;
        }

        SetAuthorizedState(true);

        if (_currentSection == ManagementSection.Announcements)
        {
            await RedirectAnnouncementsAsync();
            return;
        }

        _sectionPicker.IsVisible = !_lockSectionSelection;
        _sectionPicker.IsEnabled = !_lockSectionSelection;

        _sectionPicker.SelectedIndex = (int)_currentSection;
        if (!_initialized)
        {
            _initialized = true;
            await LoadCurrentSectionAsync(resetSelection: true);
            return;
        }

        await LoadCurrentSectionAsync(resetSelection: false);
    }

    private Task RedirectAnnouncementsAsync()
    {
        if (_requestedNewMode)
            return Shell.Current.GoToAsync(nameof(BekanntmachungEditorPage));

        if (_requestedEntryId.HasValue)
            return Shell.Current.GoToAsync($"{nameof(BekanntmachungEditorPage)}?entryId={_requestedEntryId.Value}");

        return Shell.Current.GoToAsync("//management_announcements");
    }

    private async Task LoadCurrentSectionAsync(bool resetSelection)
    {
        if (_isBusy)
            return;

        SetBusy(true);
        try
        {
            _statusLabel.Text = string.Empty;
            UpdateSectionVisibility();
            UpdateDescription();

            switch (_currentSection)
            {
                case ManagementSection.WorkAssignments:
                    _workAssignments = await _supabaseService.GetArbeitseinsaetzeVerwaltungAsync();
                    FillEntries(_workAssignments.Select(x => new ManagementEntry(x.Id, x.Titel ?? "(ohne Titel)", BuildWorkAssignmentSubtitle(x))));
                    break;
                case ManagementSection.Appointments:
                    _appointments = await _supabaseService.GetTermineVerwaltungAsync();
                    FillEntries(_appointments.Select(x => new ManagementEntry(x.Id, x.Titel ?? "(ohne Titel)", BuildAppointmentSubtitle(x))));
                    break;
                case ManagementSection.Announcements:
                    _announcements = await _supabaseService.GetBekanntmachungenVerwaltungAsync();
                    FillEntries(_announcements.Select(x => new ManagementEntry(x.Id, x.Titel ?? "(ohne Titel)", BuildAnnouncementSubtitle(x))));
                    break;
            }

            if (_requestedNewMode)
            {
                _selectedEntryId = null;
                _entriesView.SelectedItem = null;
                ResetEditorForNew();
                _requestedNewMode = false;
                _requestedEntryId = null;
                return;
            }

            if (_requestedEntryId.HasValue)
            {
                _selectedEntryId = _requestedEntryId.Value;
                var requested = _entries.FirstOrDefault(x => x.Id == _selectedEntryId);
                _entriesView.SelectedItem = requested;
                PopulateEditorFromSelection();
                _requestedEntryId = null;
                return;
            }

            if (resetSelection)
            {
                _selectedEntryId = null;
                _entriesView.SelectedItem = null;
                ResetEditorForNew();
            }
            else
            {
                var selected = _entries.FirstOrDefault(x => x.Id == _selectedEntryId);
                _entriesView.SelectedItem = selected;
                PopulateEditorFromSelection();
            }
        }
        catch (Exception ex)
        {
            _statusLabel.Text = ex.Message;
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void FillEntries(IEnumerable<ManagementEntry> source)
    {
        _entries.Clear();
        foreach (var entry in source)
            _entries.Add(entry);
    }

    private void UpdateSectionVisibility()
    {
        _workAssignmentSection.IsVisible = _currentSection == ManagementSection.WorkAssignments;
        _appointmentSection.IsVisible = _currentSection == ManagementSection.Appointments;
        _announcementSection.IsVisible = _currentSection == ManagementSection.Announcements;
    }

    private void UpdateDescription()
    {
        _descriptionLabel.Text = _currentSection switch
        {
            ManagementSection.WorkAssignments => "Mobiler Verwaltungszugang für Arbeitseinsätze auf dem bestehenden Shared-Servicepfad `arbeitseinsatz`.",
            ManagementSection.Appointments => "Mobiler Verwaltungszugang für Termine auf dem bestehenden Shared-Servicepfad `termin`.",
            _ => "Mobiler Verwaltungszugang für Bekanntmachungen auf dem bestehenden Shared-Servicepfad `bekanntmachung` inklusive HTML-Inhalt."
        };
    }

    private void ResetEditorForNew()
    {
        _selectedEntryId = null;
        _editorCaptionLabel.Text = _currentSection switch
        {
            ManagementSection.WorkAssignments => "Neuer Arbeitseinsatz",
            ManagementSection.Appointments => "Neuer Termin",
            _ => "Neue Bekanntmachung"
        };

        _titleEntry.Text = string.Empty;
        _descriptionEditor.Text = string.Empty;
        _datePicker.Date = DateTime.Today;
        _hasStartTimeCheckBox.IsChecked = false;
        _startTimePicker.Time = new TimeSpan(8, 0, 0);
        _startTimePicker.IsEnabled = false;
        _hasEndTimeCheckBox.IsChecked = false;
        _endTimePicker.Time = new TimeSpan(12, 0, 0);
        _endTimePicker.IsEnabled = false;
        _treffpunktEntry.Text = string.Empty;
        _hasMaxParticipantsCheckBox.IsChecked = false;
        _maxParticipantsEntry.Text = string.Empty;
        _maxParticipantsEntry.IsEnabled = false;
        _hoursEntry.Text = string.Empty;
        _htmlEditor.Text = string.Empty;
        _sortOrderEntry.Text = string.Empty;
        _activeSwitch.IsToggled = true;
    }

    private void PopulateEditorFromSelection()
    {
        if (_selectedEntryId == null)
        {
            ResetEditorForNew();
            return;
        }

        switch (_currentSection)
        {
            case ManagementSection.WorkAssignments:
                var workAssignment = _workAssignments.FirstOrDefault(x => x.Id == _selectedEntryId.Value);
                if (workAssignment == null)
                {
                    ResetEditorForNew();
                    return;
                }

                _editorCaptionLabel.Text = "Arbeitseinsatz bearbeiten";
                _titleEntry.Text = workAssignment.Titel ?? string.Empty;
                _descriptionEditor.Text = workAssignment.Beschreibung ?? string.Empty;
                _datePicker.Date = workAssignment.Datum == default ? DateTime.Today : workAssignment.Datum.Date;
                _hasStartTimeCheckBox.IsChecked = workAssignment.StartUhrzeit.HasValue;
                _startTimePicker.Time = workAssignment.StartUhrzeit ?? new TimeSpan(8, 0, 0);
                _startTimePicker.IsEnabled = workAssignment.StartUhrzeit.HasValue;
                _hasEndTimeCheckBox.IsChecked = workAssignment.EndUhrzeit.HasValue;
                _endTimePicker.Time = workAssignment.EndUhrzeit ?? new TimeSpan(12, 0, 0);
                _endTimePicker.IsEnabled = workAssignment.EndUhrzeit.HasValue;
                _treffpunktEntry.Text = workAssignment.Treffpunkt ?? string.Empty;
                _hasMaxParticipantsCheckBox.IsChecked = workAssignment.MaxTeilnehmer.HasValue;
                _maxParticipantsEntry.Text = workAssignment.MaxTeilnehmer?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
                _maxParticipantsEntry.IsEnabled = workAssignment.MaxTeilnehmer.HasValue;
                _hoursEntry.Text = workAssignment.StundenWert.ToString(CultureInfo.InvariantCulture);
                _activeSwitch.IsToggled = workAssignment.Aktiv;
                break;
            case ManagementSection.Appointments:
                var appointment = _appointments.FirstOrDefault(x => x.Id == _selectedEntryId.Value);
                if (appointment == null)
                {
                    ResetEditorForNew();
                    return;
                }

                _editorCaptionLabel.Text = "Termin bearbeiten";
                _titleEntry.Text = appointment.Titel ?? string.Empty;
                _descriptionEditor.Text = appointment.Beschreibung ?? string.Empty;
                _datePicker.Date = appointment.Datum == default ? DateTime.Today : appointment.Datum.Date;
                _hasStartTimeCheckBox.IsChecked = appointment.StartUhrzeit.HasValue;
                _startTimePicker.Time = appointment.StartUhrzeit ?? new TimeSpan(8, 0, 0);
                _startTimePicker.IsEnabled = appointment.StartUhrzeit.HasValue;
                _hasEndTimeCheckBox.IsChecked = appointment.EndUhrzeit.HasValue;
                _endTimePicker.Time = appointment.EndUhrzeit ?? new TimeSpan(12, 0, 0);
                _endTimePicker.IsEnabled = appointment.EndUhrzeit.HasValue;
                _activeSwitch.IsToggled = appointment.Aktiv;
                break;
            case ManagementSection.Announcements:
                var announcement = _announcements.FirstOrDefault(x => x.Id == _selectedEntryId.Value);
                if (announcement == null)
                {
                    ResetEditorForNew();
                    return;
                }

                _editorCaptionLabel.Text = "Bekanntmachung bearbeiten";
                _titleEntry.Text = announcement.Titel ?? string.Empty;
                _htmlEditor.Text = announcement.InhaltHtml ?? string.Empty;
                _sortOrderEntry.Text = announcement.SortOrder?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
                _activeSwitch.IsToggled = announcement.Aktiv;
                break;
        }
    }

    private async Task SaveAsync()
    {
        if (_isBusy)
            return;

        SetBusy(true);
        try
        {
            _statusLabel.Text = string.Empty;
            switch (_currentSection)
            {
                case ManagementSection.WorkAssignments:
                    await SaveWorkAssignmentAsync();
                    break;
                case ManagementSection.Appointments:
                    await SaveAppointmentAsync();
                    break;
                case ManagementSection.Announcements:
                    await SaveAnnouncementAsync();
                    break;
            }
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async Task SaveWorkAssignmentAsync()
    {
        int? maxParticipants = null;
        if (string.IsNullOrWhiteSpace(_titleEntry.Text))
        {
            _statusLabel.Text = "Bitte einen Titel eingeben.";
            return;
        }

        if (!TryParseOptionalDecimal(_hoursEntry.Text, out var hoursValue))
        {
            _statusLabel.Text = "Bitte einen gültigen Stundenwert eingeben.";
            return;
        }

        if (hoursValue < 0)
        {
            _statusLabel.Text = "Der Stundenwert darf nicht negativ sein.";
            return;
        }

        if (_hasMaxParticipantsCheckBox.IsChecked)
        {
            if (!TryParsePositiveInt(_maxParticipantsEntry.Text, out var parsedMaxParticipants))
            {
                _statusLabel.Text = "Bitte eine gültige Teilnehmerbegrenzung eingeben.";
                return;
            }

            maxParticipants = parsedMaxParticipants;
        }

        if (!ValidateTimeRange())
            return;

        var record = _selectedEntryId.HasValue
            ? Clone(_workAssignments.FirstOrDefault(x => x.Id == _selectedEntryId.Value) ?? new ArbeitseinsatzRecord())
            : new ArbeitseinsatzRecord();

        record.Titel = _titleEntry.Text.Trim();
        record.Beschreibung = CleanOptionalText(_descriptionEditor.Text);
        record.Datum = _datePicker.Date;
        record.StartUhrzeit = _hasStartTimeCheckBox.IsChecked ? _startTimePicker.Time : null;
        record.EndUhrzeit = _hasEndTimeCheckBox.IsChecked ? _endTimePicker.Time : null;
        record.Treffpunkt = CleanOptionalText(_treffpunktEntry.Text);
        record.MaxTeilnehmer = maxParticipants;
        record.StundenWert = hoursValue ?? 0m;
        record.Aktiv = _activeSwitch.IsToggled;

        if (_selectedEntryId.HasValue)
        {
            var ok = await _supabaseService.UpdateArbeitseinsatzAsync(record);
            _statusLabel.Text = ok ? "Arbeitseinsatz gespeichert." : "Arbeitseinsatz konnte nicht gespeichert werden.";
            if (!ok)
                return;
        }
        else
        {
            var created = await _supabaseService.CreateArbeitseinsatzAsync(record);
            _statusLabel.Text = created != null ? "Arbeitseinsatz erstellt." : "Arbeitseinsatz konnte nicht erstellt werden.";
            if (created == null)
                return;
            _selectedEntryId = created.Id;
        }

        await LoadCurrentSectionAsync(resetSelection: false);
    }

    private async Task SaveAppointmentAsync()
    {
        if (string.IsNullOrWhiteSpace(_titleEntry.Text))
        {
            _statusLabel.Text = "Bitte einen Titel eingeben.";
            return;
        }

        if (!ValidateTimeRange())
            return;

        var record = _selectedEntryId.HasValue
            ? Clone(_appointments.FirstOrDefault(x => x.Id == _selectedEntryId.Value) ?? new TerminRecord())
            : new TerminRecord();

        record.Titel = _titleEntry.Text.Trim();
        record.Beschreibung = CleanOptionalText(_descriptionEditor.Text);
        record.Datum = _datePicker.Date;
        record.StartUhrzeit = _hasStartTimeCheckBox.IsChecked ? _startTimePicker.Time : null;
        record.EndUhrzeit = _hasEndTimeCheckBox.IsChecked ? _endTimePicker.Time : null;
        record.Aktiv = _activeSwitch.IsToggled;

        if (_selectedEntryId.HasValue)
        {
            var ok = await _supabaseService.UpdateTerminAsync(record);
            _statusLabel.Text = ok ? "Termin gespeichert." : "Termin konnte nicht gespeichert werden.";
            if (!ok)
                return;
        }
        else
        {
            var created = await _supabaseService.CreateTerminAsync(record);
            _statusLabel.Text = created != null ? "Termin erstellt." : "Termin konnte nicht erstellt werden.";
            if (created == null)
                return;
            _selectedEntryId = created.Id;
        }

        await LoadCurrentSectionAsync(resetSelection: false);
    }

    private bool ValidateTimeRange()
    {
        if (_hasStartTimeCheckBox.IsChecked && _hasEndTimeCheckBox.IsChecked && _endTimePicker.Time < _startTimePicker.Time)
        {
            _statusLabel.Text = "Die Endzeit darf nicht vor der Startzeit liegen.";
            return false;
        }

        return true;
    }

    private void SetAuthorizedState(bool isAuthorized)
    {
        _sectionPicker.IsEnabled = isAuthorized && !_isBusy;
        _refreshButton.IsEnabled = isAuthorized && !_isBusy;
        _newButton.IsEnabled = isAuthorized && !_isBusy;
        _entriesView.IsEnabled = isAuthorized && !_isBusy;
        _editorContainer.IsVisible = isAuthorized;
        _saveButton.IsEnabled = isAuthorized && !_isBusy;
    }

    private void SetBusy(bool busy)
    {
        _isBusy = busy;
        var isAuthorized = _userContextState.CurrentUserContext?.Role is UserRole.Admin or UserRole.Vorstand;
        _sectionPicker.IsEnabled = isAuthorized && !busy;
        _refreshButton.IsEnabled = isAuthorized && !busy;
        _newButton.IsEnabled = isAuthorized && !busy;
        _entriesView.IsEnabled = isAuthorized && !busy;
        _saveButton.IsEnabled = isAuthorized && !busy;
    }

    private async Task SaveAnnouncementAsync()
    {
        if (string.IsNullOrWhiteSpace(_titleEntry.Text) || string.IsNullOrWhiteSpace(_htmlEditor.Text))
        {
            _statusLabel.Text = "Titel und HTML-Inhalt sind erforderlich.";
            return;
        }

        if (!TryParseOptionalInt(_sortOrderEntry.Text, out var sortOrder))
        {
            _statusLabel.Text = "Bitte eine gültige Sortierreihenfolge eingeben.";
            return;
        }

        var record = _selectedEntryId.HasValue
            ? Clone(_announcements.FirstOrDefault(x => x.Id == _selectedEntryId.Value) ?? new BekanntmachungRecord())
            : new BekanntmachungRecord();

        record.Titel = _titleEntry.Text.Trim();
        record.InhaltHtml = _htmlEditor.Text.Trim();
        record.SortOrder = sortOrder;
        record.Aktiv = _activeSwitch.IsToggled;

        if (_selectedEntryId.HasValue)
        {
            var ok = await _supabaseService.UpdateBekanntmachungAsync(record);
            _statusLabel.Text = ok ? "Bekanntmachung gespeichert." : "Bekanntmachung konnte nicht gespeichert werden.";
            if (!ok)
                return;
        }
        else
        {
            var created = await _supabaseService.CreateBekanntmachungAsync(record);
            _statusLabel.Text = created != null ? "Bekanntmachung erstellt." : "Bekanntmachung konnte nicht erstellt werden.";
            if (created == null)
                return;
            _selectedEntryId = created.Id;
        }

        await LoadCurrentSectionAsync(resetSelection: false);
    }

    private static View CreateField(string title, View view)
    {
        return new VerticalStackLayout
        {
            Spacing = 4,
            Children =
            {
                new Label { Text = title, FontAttributes = FontAttributes.Bold, FontSize = 12, TextColor = Colors.Gray },
                view
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
                new Label { Text = title, VerticalTextAlignment = TextAlignment.Center }
            }
        };
    }

    private static View CreateSwitchField(string title, Switch switchControl)
    {
        return new HorizontalStackLayout
        {
            Spacing = 8,
            Children =
            {
                new Label { Text = title, VerticalTextAlignment = TextAlignment.Center },
                switchControl
            }
        };
    }

    private static string? CleanOptionalText(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static bool TryParseOptionalInt(string? value, out int? result)
    {
        result = null;
        if (string.IsNullOrWhiteSpace(value))
            return true;

        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
        {
            result = parsed;
            return true;
        }

        return false;
    }

    private static bool TryParsePositiveInt(string? value, out int result)
    {
        result = 0;
        return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out result) && result > 0;
    }

    private static bool TryParseOptionalDecimal(string? value, out decimal? result)
    {
        result = null;
        if (string.IsNullOrWhiteSpace(value))
            return true;

        if (decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed)
            || decimal.TryParse(value, NumberStyles.Number, CultureInfo.CurrentCulture, out parsed))
        {
            result = parsed;
            return true;
        }

        return false;
    }

    private static string BuildWorkAssignmentSubtitle(ArbeitseinsatzRecord record)
    {
        return $"{record.Datum:dd.MM.yyyy} · {(string.IsNullOrWhiteSpace(record.Treffpunkt) ? "kein Treffpunkt" : record.Treffpunkt)}";
    }

    private static string BuildAppointmentSubtitle(TerminRecord record)
    {
        return $"{record.Datum:dd.MM.yyyy} · {(record.StartUhrzeit.HasValue ? record.StartUhrzeit.Value.ToString(@"hh\:mm") : "ohne Uhrzeit")}";
    }

    private static string BuildAnnouncementSubtitle(BekanntmachungRecord record)
    {
        return record.Aktiv ? "aktiv" : "inaktiv";
    }

    private static ArbeitseinsatzRecord Clone(ArbeitseinsatzRecord source)
    {
        return new ArbeitseinsatzRecord
        {
            Id = source.Id,
            Titel = source.Titel,
            Beschreibung = source.Beschreibung,
            Datum = source.Datum,
            StartUhrzeit = source.StartUhrzeit,
            EndUhrzeit = source.EndUhrzeit,
            Treffpunkt = source.Treffpunkt,
            MaxTeilnehmer = source.MaxTeilnehmer,
            StundenWert = source.StundenWert,
            SichtbarAb = source.SichtbarAb,
            SichtbarBis = source.SichtbarBis,
            AnmeldungBis = source.AnmeldungBis,
            Aktiv = source.Aktiv,
            IsDemo = source.IsDemo
        };
    }

    private static TerminRecord Clone(TerminRecord source)
    {
        return new TerminRecord
        {
            Id = source.Id,
            Titel = source.Titel,
            Beschreibung = source.Beschreibung,
            Datum = source.Datum,
            StartUhrzeit = source.StartUhrzeit,
            EndUhrzeit = source.EndUhrzeit,
            SichtbarAb = source.SichtbarAb,
            SichtbarBis = source.SichtbarBis,
            Aktiv = source.Aktiv
        };
    }

    private static BekanntmachungRecord Clone(BekanntmachungRecord source)
    {
        return new BekanntmachungRecord
        {
            Id = source.Id,
            Titel = source.Titel,
            InhaltHtml = source.InhaltHtml,
            SichtbarAb = source.SichtbarAb,
            SichtbarBis = source.SichtbarBis,
            SortOrder = source.SortOrder,
            Aktiv = source.Aktiv
        };
    }

    private sealed record ManagementEntry(long Id, string Title, string Subtitle);
}
