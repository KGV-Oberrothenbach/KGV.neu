using KGV.Core.Interfaces;
using KGV.Core.Models;
using KGV.Core.Security;
using KGV.Maui.State;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace KGV.Maui.Pages;

public class MeineDatenPage : ContentPage
{
    private static readonly Regex EmailRegex = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private readonly ISupabaseService _supabaseService;
    private readonly IAuthService _authService;
    private readonly UserContextState _userContextState;
    private readonly MemberContextState _memberContextState;

    private readonly Label _headlineLabel;
    private readonly Label _statusLabel;
    private readonly Label _editHintLabel;
    private readonly Label _adminHintLabel;
    private readonly Label _vornameLabel;
    private readonly Label _nachnameLabel;
    private readonly Label _geburtsdatumLabel;
    private readonly Label _emailLabel;
    private readonly Label _emailEditHintLabel;
    private readonly Label _telefonLabel;
    private readonly Label _mobilLabel;
    private readonly Label _whatsappLabel;
    private readonly Label _strasseLabel;
    private readonly Label _plzLabel;
    private readonly Label _ortLabel;
    private readonly Label _rolleLabel;
    private readonly Label _mitgliedSeitLabel;
    private readonly Label _mitgliedEndeLabel;
    private readonly Label _aktivLabel;
    private readonly Label _bemerkungenLabel;
    private readonly Label _wartungsvertragLabel;
    private readonly Label _befreiungLabel;
    private readonly Label _pflichtstundenJahrLabel;
    private readonly Label _regelgrundLabel;
    private readonly Label _wartungsvertragHintLabel;
    private readonly Label _nebenmitgliedHintLabel;

    private readonly Entry _vornameEntry;
    private readonly Entry _nachnameEntry;
    private readonly Entry _emailEntry;
    private readonly Entry _telefonEntry;
    private readonly Entry _mobilEntry;
    private readonly Entry _strasseEntry;
    private readonly Entry _plzEntry;
    private readonly Entry _ortEntry;
    private readonly Editor _bemerkungenEditor;
    private readonly Switch _whatsappSwitch;
    private readonly DatePicker _geburtsdatumPicker;
    private readonly DatePicker _mitgliedSeitPicker;
    private readonly DatePicker _mitgliedEndePicker;
    private readonly Button _clearGeburtsdatumButton;
    private readonly Button _clearMitgliedSeitButton;
    private readonly Button _clearMitgliedEndeButton;

    private readonly Border _nebenmitgliedSectionCard;
    private readonly Border _wartungsvertragSectionCard;
    private readonly Border _adminSectionCard;
    private readonly VerticalStackLayout _adminMenuSection;
    private readonly Picker _rolePicker;
    private readonly Button _editButton;
    private readonly Button _saveButton;
    private readonly Button _cancelButton;
    private readonly Button _saveRoleButton;
    private readonly Button _documentsButton;
    private readonly Button _linkedMemberButton;
    private readonly Button _userManagementButton;
    private readonly Button _nebenmitgliedButton;
    private readonly HorizontalStackLayout _topActionSection;
    private readonly HorizontalStackLayout _editActionSection;

    private readonly List<View> _displayModeViews = new();
    private readonly List<View> _editModeViews = new();

    private MemberDTO? _currentMember;
    private MemberDTO? _linkedMember;
    private bool _currentMemberHasAuthUser;
    private DateTime? _editGeburtsdatum;
    private DateTime? _editMitgliedSeit;
    private DateTime? _editMitgliedEnde;
    private bool _isBusy;
    private bool _isEditMode;

    public MeineDatenPage(
        ISupabaseService supabaseService,
        IAuthService authService,
        UserContextState userContextState,
        MemberContextState memberContextState)
    {
        _supabaseService = supabaseService;
        _authService = authService;
        _userContextState = userContextState;
        _memberContextState = memberContextState;

        Title = "Stammdaten";

        _headlineLabel = new Label { FontSize = 24, FontAttributes = FontAttributes.Bold };
        _statusLabel = new Label { TextColor = Colors.DarkRed, LineBreakMode = LineBreakMode.WordWrap };
        _editHintLabel = new Label { TextColor = Colors.Gray, LineBreakMode = LineBreakMode.WordWrap };
        _adminHintLabel = new Label { TextColor = Colors.Gray, LineBreakMode = LineBreakMode.WordWrap };
        _vornameLabel = CreateValueLabel();
        _nachnameLabel = CreateValueLabel();
        _geburtsdatumLabel = CreateValueLabel();
        _emailLabel = CreateValueLabel();
        _emailEditHintLabel = new Label { TextColor = Colors.Gray, LineBreakMode = LineBreakMode.WordWrap, IsVisible = false };
        _telefonLabel = CreateValueLabel();
        _mobilLabel = CreateValueLabel();
        _whatsappLabel = CreateValueLabel();
        _strasseLabel = CreateValueLabel();
        _plzLabel = CreateValueLabel();
        _ortLabel = CreateValueLabel();
        _rolleLabel = CreateValueLabel();
        _mitgliedSeitLabel = CreateValueLabel();
        _mitgliedEndeLabel = CreateValueLabel();
        _aktivLabel = CreateValueLabel();
        _bemerkungenLabel = CreateValueLabel();
        _wartungsvertragLabel = CreateValueLabel();
        _befreiungLabel = CreateValueLabel();
        _pflichtstundenJahrLabel = CreateValueLabel();
        _regelgrundLabel = CreateValueLabel();
        _wartungsvertragHintLabel = new Label { TextColor = Colors.Gray, LineBreakMode = LineBreakMode.WordWrap };
        _nebenmitgliedHintLabel = new Label { TextColor = Colors.Gray, LineBreakMode = LineBreakMode.WordWrap, IsVisible = false };

        _vornameEntry = new Entry { Placeholder = "Vorname" };
        _nachnameEntry = new Entry { Placeholder = "Nachname" };
        _emailEntry = new Entry { Placeholder = "E-Mail", Keyboard = Keyboard.Email };
        _telefonEntry = new Entry { Placeholder = "Telefon" };
        _mobilEntry = new Entry { Placeholder = "Mobilnummer" };
        _strasseEntry = new Entry { Placeholder = "Straße / Hausnummer" };
        _plzEntry = new Entry { Placeholder = "PLZ", Keyboard = Keyboard.Numeric };
        _ortEntry = new Entry { Placeholder = "Ort" };
        _bemerkungenEditor = new Editor { AutoSize = EditorAutoSizeOption.TextChanges, Placeholder = "Bemerkung" };
        _whatsappSwitch = new Switch();

        _geburtsdatumPicker = CreateDatePicker(date =>
        {
            _editGeburtsdatum = date;
        });
        _mitgliedSeitPicker = CreateDatePicker(date =>
        {
            _editMitgliedSeit = date;
        });
        _mitgliedEndePicker = CreateDatePicker(date =>
        {
            _editMitgliedEnde = date;
        });

        _clearGeburtsdatumButton = CreateClearDateButton(() =>
        {
            _editGeburtsdatum = null;
            ApplyNullableDate(_geburtsdatumPicker, null);
        });
        _clearMitgliedSeitButton = CreateClearDateButton(() =>
        {
            _editMitgliedSeit = null;
            ApplyNullableDate(_mitgliedSeitPicker, null);
        });
        _clearMitgliedEndeButton = CreateClearDateButton(() =>
        {
            _editMitgliedEnde = null;
            ApplyNullableDate(_mitgliedEndePicker, null);
        });

        _rolePicker = new Picker { Title = "Rolle" };
        foreach (var role in UserRoles.AssignableRoles)
            _rolePicker.Items.Add(role);

        _editButton = new Button { Text = "Bearbeiten" };
        _editButton.Clicked += OnEditClicked;

        _saveButton = new Button { Text = "Speichern", IsVisible = false };
        _saveButton.Clicked += OnSaveClicked;

        _cancelButton = new Button { Text = "Abbrechen", IsVisible = false };
        _cancelButton.Clicked += OnCancelClicked;

        _saveRoleButton = new Button { Text = "Rolle speichern" };
        _saveRoleButton.Clicked += OnSaveRoleClicked;

        _documentsButton = new Button { Text = "Mitgliedsdokumente" };
        _documentsButton.Clicked += async (_, _) => await Shell.Current.GoToAsync(nameof(DokumentePage));

        _linkedMemberButton = new Button { Text = "Verknüpftes Mitglied öffnen", IsVisible = false };
        _linkedMemberButton.Clicked += OnLinkedMemberClicked;

        _nebenmitgliedButton = new Button { Text = "Nebenmitglied öffnen", IsVisible = false };
        _nebenmitgliedButton.Clicked += async (_, _) => await Shell.Current.GoToAsync(nameof(NebenmitgliedPage));

        _userManagementButton = new Button { Text = "Benutzerverwaltung" };
        _userManagementButton.Clicked += async (_, _) => await Shell.Current.GoToAsync(nameof(UserManagementPage));

        _topActionSection = new HorizontalStackLayout
        {
            Spacing = 8,
            Children = { _editButton }
        };

        _editActionSection = new HorizontalStackLayout
        {
            Spacing = 12,
            IsVisible = false,
            Children = { _cancelButton, _saveButton }
        };

        _adminMenuSection = new VerticalStackLayout
        {
            Spacing = 8,
            Children =
            {
                new Label { Text = "Admin-Menü", FontAttributes = FontAttributes.Bold, FontSize = 18 },
                _adminHintLabel,
                _rolePicker,
                _saveRoleButton,
                _userManagementButton
            }
        };

        _wartungsvertragSectionCard = CreateSection("Wartungsverträge / Pflichtstunden",
            CreateValueField("Bewertungsjahr", _pflichtstundenJahrLabel),
            CreateValueField("Wartungsvertrag", _wartungsvertragLabel),
            CreateValueField("Von Pflichtstunden befreit", _befreiungLabel),
            CreateValueField("Regelgrund", _regelgrundLabel),
            _wartungsvertragHintLabel);
        _nebenmitgliedSectionCard = CreateSection("Mitgliedskontext", _linkedMemberButton, _nebenmitgliedButton, _nebenmitgliedHintLabel);
        _adminSectionCard = CreateSection("Verwaltung", _adminMenuSection);

        var grunddatenSection = CreateSection("Grunddaten",
            CreateModeField("Nachname", _nachnameLabel, _nachnameEntry),
            CreateModeField("Vorname", _vornameLabel, _vornameEntry),
            CreateModeField("Geburtsdatum", _geburtsdatumLabel, CreateDateEditor(_geburtsdatumPicker, _clearGeburtsdatumButton)));

        var kontaktSection = CreateSection("Kontakt",
            CreateModeField("E-Mail", _emailLabel, CreateEmailEditor()),
            CreateModeField("Telefon", _telefonLabel, _telefonEntry),
            CreateModeField("Mobilnummer", _mobilLabel, _mobilEntry),
            CreateModeField("WhatsApp", _whatsappLabel, CreateSwitchEditor(_whatsappSwitch, "WhatsApp-Einwilligung")));

        var adresseSection = CreateSection("Adresse",
            CreateModeField("Straße / Hausnummer", _strasseLabel, _strasseEntry),
            CreateModeField("PLZ", _plzLabel, _plzEntry),
            CreateModeField("Ort", _ortLabel, _ortEntry));

        var mitgliedschaftSection = CreateSection("Mitgliedschaft",
            CreateValueField("Rolle", _rolleLabel),
            CreateModeField("Mitglied seit", _mitgliedSeitLabel, CreateDateEditor(_mitgliedSeitPicker, _clearMitgliedSeitButton)),
            CreateModeField("Mitglied Ende", _mitgliedEndeLabel, CreateDateEditor(_mitgliedEndePicker, _clearMitgliedEndeButton)),
            CreateValueField("Aktiv", _aktivLabel));

        var bemerkungenSection = CreateSection("Bemerkung",
            CreateModeField("Bemerkung", _bemerkungenLabel, _bemerkungenEditor));

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = 24,
                Spacing = 14,
                Children =
                {
                    _headlineLabel,
                    _statusLabel,
                    _topActionSection,
                    _editHintLabel,
                    grunddatenSection,
                    kontaktSection,
                    adresseSection,
                    mitgliedschaftSection,
                    bemerkungenSection,
                    _wartungsvertragSectionCard,
                    _nebenmitgliedSectionCard,
                    _adminSectionCard,
                    _documentsButton,
                    _editActionSection
                }
            }
        };

        Appearing += async (_, _) => await LoadAsync();
        SetEditMode(false);
    }

    private async Task LoadAsync()
    {
        if (_isBusy)
            return;

        _isBusy = true;
        UpdateActionState();

        try
        {
            _statusLabel.Text = string.Empty;

            var selectedMember = _memberContextState.SelectedMember;
            if (selectedMember?.Id is not > 0)
            {
                _headlineLabel.Text = "Kein Mitglied ausgewählt";
                _currentMember = null;
                _currentMemberHasAuthUser = false;
                SetEditMode(false);
                SetMemberFieldsEmpty();
                SetWartungsvertragFieldsEmpty();
                UpdateNebenmitgliedSection(null, null, null, false);
                UpdateAdminMenu(null);
                _nachnameLabel.Text = "Bitte zuerst in der Mitgliedersuche ein Mitglied auswählen.";
                _editHintLabel.Text = string.Empty;
                return;
            }

            var member = await _supabaseService.GetMitgliedByIdAsync(selectedMember.Id);
            if (member == null)
            {
                _currentMember = null;
                _currentMemberHasAuthUser = false;
                _statusLabel.Text = "Das ausgewählte Mitglied konnte nicht geladen werden.";
                SetMemberFieldsEmpty();
                SetWartungsvertragFieldsEmpty();
                UpdateNebenmitgliedSection(null, null, null, false);
                UpdateAdminMenu(null);
                return;
            }

            var contextMember = MapMember(member);
            _currentMember = contextMember;
            _currentMemberHasAuthUser = member.AuthUserId.HasValue;
            _memberContextState.SetSelectedMember(contextMember);

            _headlineLabel.Text = string.IsNullOrWhiteSpace(contextMember.DisplayName)
                ? $"Mitglied #{contextMember.Id}"
                : contextMember.DisplayName;

            PopulateDisplayFields(contextMember);
            PopulateEditFields(contextMember);
            UpdateAdminMenu(contextMember);
            UpdateEditHint(contextMember);
            await UpdateNebenmitgliedSectionAsync(member);
            await LoadWartungsvertragSummaryAsync(member.Id);
            UpdateActionState();
        }
        catch (Exception ex)
        {
            _statusLabel.Text = ex.Message;
        }
        finally
        {
            _isBusy = false;
            UpdateActionState();
        }
    }

    private void PopulateDisplayFields(MemberDTO member)
    {
        _nachnameLabel.Text = FormatValue(member.Nachname);
        _vornameLabel.Text = FormatValue(member.Vorname);
        _geburtsdatumLabel.Text = FormatDate(member.Geburtsdatum);
        _emailLabel.Text = FormatValue(member.Email);
        _telefonLabel.Text = FormatValue(member.Telefon);
        _mobilLabel.Text = FormatValue(member.Mobilnummer);
        _whatsappLabel.Text = member.WhatsappEinwilligung ? "Ja" : "Nein";
        _strasseLabel.Text = FormatValue(member.Strasse);
        _plzLabel.Text = FormatValue(member.PLZ);
        _ortLabel.Text = FormatValue(member.Ort);
        _rolleLabel.Text = FormatRole(member.Role);
        _mitgliedSeitLabel.Text = FormatDate(member.MitgliedSeit);
        _mitgliedEndeLabel.Text = FormatDate(member.MitgliedEnde);
        _aktivLabel.Text = member.Aktiv ? "Ja" : "Nein";
        _bemerkungenLabel.Text = FormatValue(member.Bemerkungen);
    }

    private void PopulateEditFields(MemberDTO member)
    {
        _vornameEntry.Text = member.Vorname;
        _nachnameEntry.Text = member.Nachname;
        _emailEntry.Text = member.Email;
        _telefonEntry.Text = member.Telefon;
        _mobilEntry.Text = member.Mobilnummer;
        _strasseEntry.Text = member.Strasse;
        _plzEntry.Text = member.PLZ;
        _ortEntry.Text = member.Ort;
        _bemerkungenEditor.Text = member.Bemerkungen;
        _whatsappSwitch.IsToggled = member.WhatsappEinwilligung;

        _editGeburtsdatum = member.Geburtsdatum;
        _editMitgliedSeit = member.MitgliedSeit;
        _editMitgliedEnde = member.MitgliedEnde;
        ApplyNullableDate(_geburtsdatumPicker, _editGeburtsdatum);
        ApplyNullableDate(_mitgliedSeitPicker, _editMitgliedSeit);
        ApplyNullableDate(_mitgliedEndePicker, _editMitgliedEnde);
        UpdateEmailEditState();
    }

    private bool CanEditCurrentMember(MemberDTO? member)
    {
        if (member?.Id is not > 0)
            return false;

        var currentRole = _userContextState.CurrentUserContext?.Role;
        return currentRole is UserRole.Admin or UserRole.Vorstand
            || _userContextState.CurrentMitgliedId == member.Id;
    }

    private void UpdateEditHint(MemberDTO? member)
    {
        if (member == null)
        {
            _editHintLabel.Text = string.Empty;
            return;
        }

        if (!CanEditCurrentMember(member))
        {
            _editHintLabel.Text = "Dieser Mitgliedskontext ist mobil aktuell nur lesbar.";
            return;
        }

        _editHintLabel.Text = _isEditMode
            ? "Bearbeiten aktiv. Änderungen können direkt mobil gespeichert oder verworfen werden."
            : "Über `Bearbeiten` können die Stammdaten dieses Mitglieds direkt mobil geändert werden.";
    }

    private void SetEditMode(bool isEditMode)
    {
        _isEditMode = isEditMode && CanEditCurrentMember(_currentMember);

        foreach (var view in _displayModeViews)
            view.IsVisible = !_isEditMode;

        foreach (var view in _editModeViews)
            view.IsVisible = _isEditMode;

        UpdateEditHint(_currentMember);
        UpdateEmailEditState();
        UpdateActionState();
    }

    private void UpdateActionState()
    {
        var canEdit = CanEditCurrentMember(_currentMember);
        _topActionSection.IsVisible = canEdit && !_isEditMode;
        _editButton.IsEnabled = canEdit && !_isBusy;

        _editActionSection.IsVisible = _isEditMode;
        _saveButton.IsVisible = _isEditMode;
        _cancelButton.IsVisible = _isEditMode;
        _saveButton.IsEnabled = _isEditMode && !_isBusy;
        _cancelButton.IsEnabled = _isEditMode && !_isBusy;

        _vornameEntry.IsEnabled = _isEditMode && !_isBusy;
        _nachnameEntry.IsEnabled = _isEditMode && !_isBusy;
        _telefonEntry.IsEnabled = _isEditMode && !_isBusy;
        _mobilEntry.IsEnabled = _isEditMode && !_isBusy;
        _strasseEntry.IsEnabled = _isEditMode && !_isBusy;
        _plzEntry.IsEnabled = _isEditMode && !_isBusy;
        _ortEntry.IsEnabled = _isEditMode && !_isBusy;
        _bemerkungenEditor.IsEnabled = _isEditMode && !_isBusy;
        _whatsappSwitch.IsEnabled = _isEditMode && !_isBusy;
        _geburtsdatumPicker.IsEnabled = _isEditMode && !_isBusy;
        _mitgliedSeitPicker.IsEnabled = _isEditMode && !_isBusy;
        _mitgliedEndePicker.IsEnabled = _isEditMode && !_isBusy;
        _clearGeburtsdatumButton.IsEnabled = _isEditMode && !_isBusy;
        _clearMitgliedSeitButton.IsEnabled = _isEditMode && !_isBusy;
        _clearMitgliedEndeButton.IsEnabled = _isEditMode && !_isBusy;
        _linkedMemberButton.IsEnabled = !_isBusy && _linkedMember?.Id is > 0;
        UpdateEmailEditState();
    }

    private async void OnLinkedMemberClicked(object? sender, EventArgs e)
    {
        if (_linkedMember?.Id is not > 0)
            return;

        _statusLabel.Text = string.Empty;
        SetEditMode(false);
        _memberContextState.SetSelectedMember(_linkedMember);

        if (_userContextState.CurrentUserContext?.Role is UserRole.Admin or UserRole.Vorstand
            && Application.Current is App app)
        {
            await app.SwitchToCurrentRootAsync("memberdetails");
            return;
        }

        await LoadAsync();
    }

    private bool CanEditEmailInCurrentContext()
    {
        if (!_isEditMode || _currentMember == null)
            return false;

        var currentRole = _userContextState.CurrentUserContext?.Role;
        return currentRole is UserRole.Admin or UserRole.Vorstand
            && !_currentMemberHasAuthUser;
    }

    private void UpdateEmailEditState()
    {
        var canEditEmail = CanEditEmailInCurrentContext();
        _emailEntry.IsEnabled = _isEditMode && !_isBusy;
        _emailEntry.IsReadOnly = !canEditEmail;
        _emailEditHintLabel.IsVisible = _isEditMode && _currentMemberHasAuthUser;
        _emailEditHintLabel.Text = _currentMemberHasAuthUser
            ? "Die Mailadresse ist hier schreibgeschützt. Sie muss vom Nutzer selbst über den bestehenden Supabase-/OTP-Mailänderungsweg geändert werden."
            : string.Empty;
    }

    private void OnEditClicked(object? sender, EventArgs e)
    {
        if (_currentMember == null)
            return;

        PopulateEditFields(_currentMember);
        _statusLabel.Text = string.Empty;
        SetEditMode(true);
    }

    private void OnCancelClicked(object? sender, EventArgs e)
    {
        if (_currentMember != null)
            PopulateEditFields(_currentMember);

        _statusLabel.Text = "Bearbeiten abgebrochen.";
        SetEditMode(false);
    }

    private async void OnSaveClicked(object? sender, EventArgs e)
    {
        var selectedMember = _memberContextState.SelectedMember;
        if (selectedMember?.Id is not > 0 || _currentMember == null)
            return;

        if (!CanEditCurrentMember(_currentMember))
        {
            await DisplayAlert("Hinweis", "Für dieses Mitglied sind mobil aktuell keine Bearbeitungsrechte vorhanden.", "OK");
            return;
        }

        if (string.IsNullOrWhiteSpace(_vornameEntry.Text))
        {
            await DisplayAlert("Validierung", "Vorname ist erforderlich.", "OK");
            _vornameEntry.Focus();
            return;
        }

        if (string.IsNullOrWhiteSpace(_nachnameEntry.Text))
        {
            await DisplayAlert("Validierung", "Nachname ist erforderlich.", "OK");
            _nachnameEntry.Focus();
            return;
        }

        var userId = _authService.CurrentUserId;
        if (string.IsNullOrWhiteSpace(userId))
        {
            await DisplayAlert("Fehler", "Nicht angemeldet. Bitte erneut einloggen.", "OK");
            return;
        }

        var lockAcquired = false;
        _isBusy = true;
        UpdateActionState();

        try
        {
            lockAcquired = await _supabaseService.TryLockMitgliedAsync(selectedMember.Id, userId);
            if (!lockAcquired)
            {
                await DisplayAlert("Gesperrt", "Datensatz ist aktuell gesperrt. Bitte später erneut versuchen.", "OK");
                return;
            }

            var current = await _supabaseService.GetMitgliedByIdAsync(selectedMember.Id);
            if (current == null)
            {
                await DisplayAlert("Fehler", "Mitglied konnte nicht geladen werden.", "OK");
                return;
            }

            var dto = MapMember(current);
            dto.Vorname = (_vornameEntry.Text ?? string.Empty).Trim();
            dto.Nachname = (_nachnameEntry.Text ?? string.Empty).Trim();
            dto.Telefon = (_telefonEntry.Text ?? string.Empty).Trim();
            dto.Mobilnummer = (_mobilEntry.Text ?? string.Empty).Trim();
            dto.Strasse = (_strasseEntry.Text ?? string.Empty).Trim();
            dto.PLZ = (_plzEntry.Text ?? string.Empty).Trim();
            dto.Ort = (_ortEntry.Text ?? string.Empty).Trim();
            dto.Bemerkungen = (_bemerkungenEditor.Text ?? string.Empty).Trim();
            dto.Geburtsdatum = _editGeburtsdatum;
            dto.MitgliedSeit = _editMitgliedSeit;
            dto.MitgliedEnde = _editMitgliedEnde;
            dto.WhatsappEinwilligung = _whatsappSwitch.IsToggled;

            var currentEmail = (current.Email ?? string.Empty).Trim();
            if (CanEditEmailInCurrentContext())
            {
                var editedEmail = (_emailEntry.Text ?? string.Empty).Trim();
                if (!string.Equals(editedEmail, currentEmail, StringComparison.OrdinalIgnoreCase))
                {
                    if (string.IsNullOrWhiteSpace(editedEmail))
                    {
                        await DisplayAlert("Validierung", "Die E-Mail-Adresse darf nicht leer sein.", "OK");
                        _emailEntry.Focus();
                        return;
                    }

                    if (!EmailRegex.IsMatch(editedEmail))
                    {
                        await DisplayAlert("Validierung", "Bitte eine gültige E-Mail-Adresse eingeben.", "OK");
                        _emailEntry.Focus();
                        return;
                    }

                    dto.Email = editedEmail;
                }
                else
                {
                    dto.Email = currentEmail;
                }
            }
            else
            {
                dto.Email = current.Email ?? dto.Email;
            }

            dto.Role = current.Role ?? dto.Role;

            var ok = await _supabaseService.UpdateMitgliedAsync(dto, userId);
            if (!ok)
            {
                await DisplayAlert("Fehler", "Stammdaten konnten nicht gespeichert werden.", "OK");
                return;
            }

            _memberContextState.SetSelectedMember(dto);
            _currentMember = dto;
            SetEditMode(false);
            _statusLabel.Text = "Stammdaten gespeichert.";
            await LoadAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Fehler", ex.Message, "OK");
        }
        finally
        {
            if (lockAcquired)
                await _supabaseService.ReleaseLockMitgliedAsync(selectedMember.Id, userId, force: false);

            _isBusy = false;
            UpdateActionState();
        }
    }

    private async Task UpdateNebenmitgliedSectionAsync(MitgliedRecord member)
    {
        if (member.Id <= 0)
        {
            UpdateNebenmitgliedSection(null, null, null, false);
            return;
        }

        if (member.HauptmitgliedId is > 0)
        {
            var hauptmitglied = await _supabaseService.GetMitgliedByIdAsync(member.HauptmitgliedId.Value);
            var hauptmitgliedName = hauptmitglied == null
                ? "Hauptmitglied"
                : BuildDisplayName(hauptmitglied.Vorname, hauptmitglied.Name);

            UpdateNebenmitgliedSection(
                $"Nebenmitglied · Zugeordnet zu Hauptmitglied: {hauptmitgliedName}",
                hauptmitglied == null ? null : MapMember(hauptmitglied),
                "Hauptmitglied öffnen",
                false);
            return;
        }

        var nebenmitglied = await _supabaseService.GetNebenmitgliedByHauptmitgliedIdAsync(member.Id);
        if (nebenmitglied == null)
        {
            UpdateNebenmitgliedSection(null, null, null, false);
            return;
        }

        UpdateNebenmitgliedSection(
            $"Hauptmitglied · Verknüpftes Nebenmitglied: {BuildDisplayName(nebenmitglied.Vorname, nebenmitglied.Name)}",
            MapMember(nebenmitglied),
            "Nebenmitglied öffnen",
            true);
    }

    private async Task LoadWartungsvertragSummaryAsync(int mitgliedId)
    {
        SetWartungsvertragFieldsEmpty();

        var summary = await _supabaseService.GetPflichtstundenUebersichtForMitgliedAsync(mitgliedId);
        if (summary == null)
        {
            _wartungsvertragHintLabel.Text = "Aktuell liegt für dieses Mitglied keine belastbare Wartungsvertrags-/Pflichtstundenübersicht vor.";
            return;
        }

        _pflichtstundenJahrLabel.Text = summary.Jahr?.ToString() ?? summary.SaisonJahr?.ToString() ?? "-";
        _wartungsvertragLabel.Text = summary.HatWartungsvertrag ? "Ja" : "Nein";
        _befreiungLabel.Text = summary.IstBefreit ? "Ja" : "Nein";
        _regelgrundLabel.Text = FormatValue(summary.Regelgrund);
        _wartungsvertragHintLabel.Text = _userContextState.CurrentUserContext?.Role is UserRole.Admin or UserRole.Vorstand
            ? "Ein eigener mobiler Verwaltungseditor für Wartungsverträge ist im aktuellen Stand noch nicht vorhanden; der Mitgliedskontext zeigt hier zunächst den belastbaren Status aus der Pflichtstunden-Übersicht."
            : "Die Angaben stammen aus der zentralen Pflichtstunden-Übersicht des ausgewählten Mitglieds.";
    }

    private void UpdateNebenmitgliedSection(string? hint, MemberDTO? linkedMember, string? linkedMemberButtonText, bool canOpenNebenmitgliedPage)
    {
        _linkedMember = linkedMember?.Id is > 0 ? linkedMember.Clone() : null;
        var hasHint = !string.IsNullOrWhiteSpace(hint);
        _linkedMemberButton.Text = string.IsNullOrWhiteSpace(linkedMemberButtonText) ? "Verknüpftes Mitglied öffnen" : linkedMemberButtonText;
        _linkedMemberButton.IsVisible = _linkedMember?.Id is > 0;
        _linkedMemberButton.IsEnabled = !_isBusy && _linkedMember?.Id is > 0;
        _nebenmitgliedButton.IsVisible = canOpenNebenmitgliedPage;
        _nebenmitgliedHintLabel.Text = hasHint ? hint : string.Empty;
        _nebenmitgliedHintLabel.IsVisible = hasHint;
        _nebenmitgliedSectionCard.IsVisible = _linkedMemberButton.IsVisible || canOpenNebenmitgliedPage || hasHint;
    }

    private void UpdateAdminMenu(MemberDTO? member)
    {
        var currentRole = _userContextState.CurrentUserContext?.Role;
        var hasAdminMenu = currentRole is UserRole.Admin or UserRole.Vorstand;
        _adminSectionCard.IsVisible = hasAdminMenu;
        _adminMenuSection.IsVisible = hasAdminMenu;
        _adminHintLabel.IsVisible = hasAdminMenu;
        _rolePicker.IsVisible = hasAdminMenu;
        _saveRoleButton.IsVisible = hasAdminMenu;
        _userManagementButton.IsVisible = currentRole == UserRole.Admin && member?.Id is > 0;

        if (!hasAdminMenu || member == null)
        {
            _adminHintLabel.Text = string.Empty;
            _rolePicker.SelectedItem = null;
            _rolePicker.IsEnabled = false;
            _saveRoleButton.IsEnabled = false;
            return;
        }

        var normalizedRole = NormalizeRole(member.Role);
        _rolePicker.SelectedItem = normalizedRole;
        _rolePicker.IsEnabled = currentRole == UserRole.Admin && member.Id != 7;
        _saveRoleButton.IsEnabled = _rolePicker.IsEnabled && !_isBusy;
        _adminHintLabel.Text = currentRole == UserRole.Admin
            ? "Benutzerverwaltung bleibt im Mitgliedskontext gebunden. Rollenänderungen laufen über denselben Lock-/Update-Pfad wie in WPF."
            : "Vorstand sieht den Mitgliedskontext und die freigegebenen Admin-Informationen; Admin-only-Punkte bleiben ausgeblendet.";
    }

    private async void OnSaveRoleClicked(object? sender, EventArgs e)
    {
        var selectedMember = _memberContextState.SelectedMember;
        if (selectedMember?.Id is not > 0)
            return;

        if (_userContextState.CurrentUserContext?.Role != UserRole.Admin)
        {
            await DisplayAlert("Hinweis", "Rollen können mobil nur von Admins gespeichert werden.", "OK");
            return;
        }

        var selectedRole = NormalizeRole(_rolePicker.SelectedItem as string);
        if (string.Equals(selectedRole, NormalizeRole(selectedMember.Role), StringComparison.OrdinalIgnoreCase))
        {
            await DisplayAlert("Hinweis", "Es gibt keine Rollenänderung zu speichern.", "OK");
            return;
        }

        var userId = _authService.CurrentUserId;
        if (string.IsNullOrWhiteSpace(userId))
        {
            await DisplayAlert("Fehler", "Nicht angemeldet. Bitte erneut einloggen.", "OK");
            return;
        }

        var lockAcquired = false;
        try
        {
            lockAcquired = await _supabaseService.TryLockMitgliedAsync(selectedMember.Id, userId);
            if (!lockAcquired)
            {
                await DisplayAlert("Gesperrt", "Datensatz ist aktuell gesperrt. Bitte später erneut versuchen.", "OK");
                return;
            }

            var rec = await _supabaseService.GetMitgliedByIdAsync(selectedMember.Id);
            if (rec == null)
            {
                await DisplayAlert("Fehler", "Mitglied konnte nicht geladen werden.", "OK");
                return;
            }

            var dto = MapMember(rec);
            dto.Role = selectedRole;

            var ok = await _supabaseService.UpdateMitgliedAsync(dto, userId);
            if (!ok)
            {
                await DisplayAlert("Fehler", "Rolle konnte nicht gespeichert werden.", "OK");
                return;
            }

            _memberContextState.SetSelectedMember(dto);
            await DisplayAlert("OK", "Rolle gespeichert.", "OK");
            await LoadAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Fehler", ex.Message, "OK");
        }
        finally
        {
            if (lockAcquired)
                await _supabaseService.ReleaseLockMitgliedAsync(selectedMember.Id, userId, force: false);
        }
    }

    private static Border CreateSection(string title, params View[] children)
    {
        var content = new VerticalStackLayout { Spacing = 8 };
        content.Children.Add(new Label { Text = title, FontAttributes = FontAttributes.Bold, FontSize = 18 });
        foreach (var child in children)
            content.Children.Add(child);

        return new Border
        {
            Padding = 16,
            Stroke = Colors.LightGray,
            Content = content
        };
    }

    private View CreateModeField(string title, Label valueLabel, View editView)
    {
        object? readOnlyStyleObj = null;
        Application.Current?.Resources?.TryGetValue("ReadOnlyField", out readOnlyStyleObj);
        object? entryBorderStyleObj = null;
        Application.Current?.Resources?.TryGetValue("EntryBorder", out entryBorderStyleObj);

        var displayContainer = readOnlyStyleObj is Style readOnlyStyle
            ? new Border { Style = readOnlyStyle, Content = valueLabel }
            : new Border { Stroke = Colors.LightGray, Padding = new Thickness(12, 10), Content = valueLabel };
        var editContainer = entryBorderStyleObj is Style entryBorderStyle
            ? new Border { Style = entryBorderStyle, Content = editView, IsVisible = false }
            : new Border { Stroke = Colors.LightGray, Padding = new Thickness(12, 10), Content = editView, IsVisible = false };

        _displayModeViews.Add(displayContainer);
        _editModeViews.Add(editContainer);

        return new VerticalStackLayout
        {
            Spacing = 4,
            Children =
            {
                new Label { Text = title, FontAttributes = FontAttributes.Bold, FontSize = 13 },
                displayContainer,
                editContainer
            }
        };
    }

    private static Label CreateValueLabel()
    {
        return new Label
        {
            LineBreakMode = LineBreakMode.WordWrap
        };
    }

    private static View CreateValueField(string title, Label valueLabel)
    {
        object? readOnlyStyleObj = null;
        if (Application.Current?.Resources != null)
            Application.Current.Resources.TryGetValue("ReadOnlyField", out readOnlyStyleObj);

        var valueContainer = readOnlyStyleObj is Style readOnlyStyle
            ? new Border { Style = readOnlyStyle, Content = valueLabel }
            : new Border { Stroke = Colors.LightGray, Padding = new Thickness(12, 10), Content = valueLabel };

        return new VerticalStackLayout
        {
            Spacing = 4,
            Children =
            {
                new Label { Text = title, FontAttributes = FontAttributes.Bold, FontSize = 13 },
                valueContainer
            }
        };
    }

    private static View CreateDateEditor(DatePicker picker, Button clearButton)
    {
        return new VerticalStackLayout
        {
            Spacing = 8,
            Children =
            {
                picker,
                clearButton
            }
        };
    }

    private static View CreateSwitchEditor(Switch toggle, string caption)
    {
        return new HorizontalStackLayout
        {
            Spacing = 12,
            Children =
            {
                toggle,
                new Label { Text = caption, VerticalTextAlignment = TextAlignment.Center }
            }
        };
    }

    private static DatePicker CreateDatePicker(Action<DateTime> onChanged)
    {
        var picker = new DatePicker { Format = "'Nicht gesetzt'" };
        picker.DateSelected += (_, e) =>
        {
            onChanged(e.NewDate.Date);
            ApplyNullableDate(picker, e.NewDate.Date);
        };
        return picker;
    }

    private static Button CreateClearDateButton(Action onClear)
    {
        var button = new Button { Text = "Leeren" };
        button.Clicked += (_, _) => onClear();
        return button;
    }

    private static void ApplyNullableDate(DatePicker picker, DateTime? value)
    {
        picker.Date = (value ?? DateTime.Today).Date;
        picker.Format = value.HasValue ? "dd.MM.yyyy" : "'Nicht gesetzt'";
    }

    private void SetMemberFieldsEmpty()
    {
        _vornameLabel.Text = string.Empty;
        _nachnameLabel.Text = string.Empty;
        _geburtsdatumLabel.Text = string.Empty;
        _emailLabel.Text = string.Empty;
        _emailEditHintLabel.Text = string.Empty;
        _emailEditHintLabel.IsVisible = false;
        _telefonLabel.Text = string.Empty;
        _mobilLabel.Text = string.Empty;
        _whatsappLabel.Text = string.Empty;
        _strasseLabel.Text = string.Empty;
        _plzLabel.Text = string.Empty;
        _ortLabel.Text = string.Empty;
        _rolleLabel.Text = string.Empty;
        _mitgliedSeitLabel.Text = string.Empty;
        _mitgliedEndeLabel.Text = string.Empty;
        _aktivLabel.Text = string.Empty;
        _bemerkungenLabel.Text = string.Empty;

        _vornameEntry.Text = string.Empty;
        _nachnameEntry.Text = string.Empty;
        _emailEntry.Text = string.Empty;
        _telefonEntry.Text = string.Empty;
        _mobilEntry.Text = string.Empty;
        _strasseEntry.Text = string.Empty;
        _plzEntry.Text = string.Empty;
        _ortEntry.Text = string.Empty;
        _bemerkungenEditor.Text = string.Empty;
        _whatsappSwitch.IsToggled = false;
        _editGeburtsdatum = null;
        _editMitgliedSeit = null;
        _editMitgliedEnde = null;
        ApplyNullableDate(_geburtsdatumPicker, null);
        ApplyNullableDate(_mitgliedSeitPicker, null);
        ApplyNullableDate(_mitgliedEndePicker, null);
    }

    private View CreateEmailEditor()
    {
        return new VerticalStackLayout
        {
            Spacing = 8,
            Children =
            {
                _emailEntry,
                _emailEditHintLabel
            }
        };
    }

    private void SetWartungsvertragFieldsEmpty()
    {
        _pflichtstundenJahrLabel.Text = "-";
        _wartungsvertragLabel.Text = "-";
        _befreiungLabel.Text = "-";
        _regelgrundLabel.Text = "-";
        _wartungsvertragHintLabel.Text = string.Empty;
    }

    private static string FormatDate(DateTime? value) => value?.ToString("dd.MM.yyyy") ?? "-";
    private static string FormatValue(string? value) => string.IsNullOrWhiteSpace(value) ? "-" : value.Trim();
    private static string FormatRole(string? role) => NormalizeRole(role);

    private static string NormalizeRole(string? role)
    {
        return UserRoles.Parse(role) switch
        {
            UserRole.Admin => UserRoles.Admin,
            UserRole.Vorstand => UserRoles.Vorstand,
            _ => UserRoles.User
        };
    }

    private static string BuildDisplayName(string? vorname, string? nachname)
    {
        var displayName = $"{vorname} {nachname}".Trim();
        return string.IsNullOrWhiteSpace(displayName) ? "Nebenmitglied" : displayName;
    }

    private static MemberDTO MapMember(MitgliedRecord rec)
    {
        return new MemberDTO
        {
            Id = rec.Id,
            Vorname = rec.Vorname ?? string.Empty,
            Nachname = rec.Name ?? string.Empty,
            Email = rec.Email ?? string.Empty,
            Telefon = rec.Telefon ?? string.Empty,
            Mobilnummer = rec.Handy ?? string.Empty,
            Strasse = rec.Adresse ?? string.Empty,
            PLZ = rec.Plz ?? string.Empty,
            Ort = rec.Ort ?? string.Empty,
            Geburtsdatum = rec.Geburtsdatum,
            MitgliedSeit = rec.MitgliedSeit,
            MitgliedEnde = rec.MitgliedEnde,
            Bemerkungen = rec.Bemerkung ?? string.Empty,
            WhatsappEinwilligung = rec.WhatsappEinwilligung,
            IstHauptmitglied = !rec.HauptmitgliedId.HasValue || rec.HauptmitgliedId.Value <= 0,
            Role = rec.Role ?? string.Empty
        };
    }
}
