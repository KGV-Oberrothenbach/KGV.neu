using KGV.Core.Interfaces;
using KGV.Core.Models;
using KGV.Core.Security;
using KGV.Maui.State;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace KGV.Maui.Pages;

public sealed class MemberDetailPage : ContentPage
{
    private readonly ISupabaseService _supabaseService;
    private readonly IAuthService _authService;
    private readonly UserContextState _userContextState;
    private readonly MemberContextState _memberContextState;

    private MitgliedRecord? _memberRecord;
    private bool _hasLinkedAppUser;
    private bool _isBusy;

    private readonly Label _headlineLabel;
    private readonly Label _statusLabel;
    private readonly Label _emailHintLabel;
    private readonly Label _rolleLabel;
    private readonly Label _appUserHintLabel;
    private readonly Entry _vornameEntry;
    private readonly Entry _nachnameEntry;
    private readonly Entry _emailEntry;
    private readonly Switch _geburtsdatumEnabledSwitch;
    private readonly DatePicker _geburtsdatumPicker;
    private readonly Entry _telefonEntry;
    private readonly Entry _mobilEntry;
    private readonly Switch _whatsappSwitch;
    private readonly Entry _strasseEntry;
    private readonly Entry _plzEntry;
    private readonly Entry _ortEntry;
    private readonly Editor _bemerkungenEditor;
    private readonly Switch _mitgliedSeitEnabledSwitch;
    private readonly DatePicker _mitgliedSeitPicker;
    private readonly Switch _mitgliedEndeEnabledSwitch;
    private readonly DatePicker _mitgliedEndePicker;
    private readonly Button _nutzerHinzufuegenButton;
    private readonly Button _benutzerverwaltungButton;
    private readonly Button _saveButton;
    private readonly Button _cancelButton;

    public MemberDetailPage(
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
        _emailEntry = new Entry { Placeholder = "E-Mail", Keyboard = Keyboard.Email };
        _emailHintLabel = new Label { TextColor = Colors.Gray, LineBreakMode = LineBreakMode.WordWrap };
        _rolleLabel = CreateReadOnlyLabel();
        _appUserHintLabel = new Label { TextColor = Colors.Gray, LineBreakMode = LineBreakMode.WordWrap };

        _vornameEntry = new Entry { Placeholder = "Vorname" };
        _nachnameEntry = new Entry { Placeholder = "Nachname" };

        _geburtsdatumEnabledSwitch = new Switch();
        _geburtsdatumPicker = new DatePicker { IsEnabled = false };
        _geburtsdatumEnabledSwitch.Toggled += (_, e) => _geburtsdatumPicker.IsEnabled = e.Value;

        _telefonEntry = new Entry { Placeholder = "Telefon" };
        _mobilEntry = new Entry { Placeholder = "Mobilnummer" };
        _whatsappSwitch = new Switch();

        _strasseEntry = new Entry { Placeholder = "Straße / Hausnummer" };
        _plzEntry = new Entry { Placeholder = "PLZ", Keyboard = Keyboard.Numeric };
        _ortEntry = new Entry { Placeholder = "Ort" };
        _bemerkungenEditor = new Editor { AutoSize = EditorAutoSizeOption.TextChanges, Placeholder = "Bemerkungen" };

        _mitgliedSeitEnabledSwitch = new Switch();
        _mitgliedSeitPicker = new DatePicker { IsEnabled = false };
        _mitgliedSeitEnabledSwitch.Toggled += (_, e) => _mitgliedSeitPicker.IsEnabled = e.Value;

        _mitgliedEndeEnabledSwitch = new Switch();
        _mitgliedEndePicker = new DatePicker { IsEnabled = false };
        _mitgliedEndeEnabledSwitch.Toggled += (_, e) => _mitgliedEndePicker.IsEnabled = e.Value;

        _nutzerHinzufuegenButton = new Button { Text = "Nutzer hinzufügen", IsVisible = false };
        _nutzerHinzufuegenButton.Clicked += OnNutzerHinzufuegenClicked;

        _benutzerverwaltungButton = new Button { Text = "Benutzerverwaltung", IsVisible = false };
        _benutzerverwaltungButton.Clicked += async (_, _) => await Shell.Current.GoToAsync(nameof(UserManagementPage));

        _cancelButton = new Button { Text = "Abbrechen" };
        _cancelButton.Clicked += async (_, _) => await LoadAsync();

        _saveButton = new Button { Text = "Speichern" };
        _saveButton.Clicked += OnSaveClicked;

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
                    CreateSection("Grunddaten",
                        CreateEditorField("Nachname", _nachnameEntry),
                        CreateEditorField("Vorname", _vornameEntry),
                        CreateOptionalDateField("Geburtsdatum", _geburtsdatumEnabledSwitch, _geburtsdatumPicker),
                        CreateEditorField("E-Mail", _emailEntry),
                        _emailHintLabel,
                        CreateReadOnlyField("Rolle", _rolleLabel)),
                    CreateSection("Kontakt",
                        CreateEditorField("Telefon", _telefonEntry),
                        CreateEditorField("Mobilnummer", _mobilEntry),
                        CreateSwitchField("WhatsApp", _whatsappSwitch)),
                    CreateSection("Adresse",
                        CreateEditorField("Straße / Hausnummer", _strasseEntry),
                        CreateEditorField("PLZ", _plzEntry),
                        CreateEditorField("Ort", _ortEntry)),
                    CreateSection("Mitgliedschaft",
                        CreateOptionalDateField("Mitglied seit", _mitgliedSeitEnabledSwitch, _mitgliedSeitPicker),
                        CreateOptionalDateField("Mitglied Ende", _mitgliedEndeEnabledSwitch, _mitgliedEndePicker),
                        CreateEditorField("Bemerkungen", _bemerkungenEditor)),
                    CreateSection("Admin-Menü",
                        _appUserHintLabel,
                        _nutzerHinzufuegenButton,
                        _benutzerverwaltungButton),
                    new HorizontalStackLayout
                    {
                        Spacing = 12,
                        Children = { _cancelButton, _saveButton }
                    }
                }
            }
        };

        Appearing += async (_, _) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        if (_isBusy)
            return;

        _isBusy = true;
        try
        {
            _statusLabel.Text = string.Empty;

            var selectedMember = _memberContextState.SelectedMember;
            if (selectedMember?.Id is not > 0)
            {
                _headlineLabel.Text = "Kein Mitglied ausgewählt";
                _statusLabel.Text = "Bitte zuerst in der Mitgliedersuche ein Mitglied auswählen.";
                SetEmptyState();
                return;
            }

            var member = await _supabaseService.GetMitgliedByIdAsync(selectedMember.Id);
            if (member == null)
            {
                _headlineLabel.Text = "Mitglied nicht gefunden";
                _statusLabel.Text = "Das ausgewählte Mitglied konnte nicht geladen werden.";
                SetEmptyState();
                return;
            }

            _memberRecord = member;
            var memberDto = MapMember(member);
            var permissionSettings = await _supabaseService.GetUserPermissionSettingsAsync(member.Id);
            memberDto.Role = NormalizeRole(permissionSettings?.Role ?? UserRoles.User);
            _memberContextState.SetSelectedMember(memberDto);
            _hasLinkedAppUser = member.AuthUserId.HasValue || await HasLinkedAppUserAsync(member.Id);

            _headlineLabel.Text = string.IsNullOrWhiteSpace(memberDto.DisplayName)
                ? $"Mitglied #{memberDto.Id}"
                : memberDto.DisplayName;

            _nachnameEntry.Text = memberDto.Nachname;
            _vornameEntry.Text = memberDto.Vorname;
            _emailEntry.Text = memberDto.Email;
            _rolleLabel.Text = FormatValue(memberDto.Role);
            _telefonEntry.Text = memberDto.Telefon;
            _mobilEntry.Text = memberDto.Mobilnummer;
            _whatsappSwitch.IsToggled = memberDto.WhatsappEinwilligung;
            _strasseEntry.Text = memberDto.Strasse;
            _plzEntry.Text = memberDto.PLZ;
            _ortEntry.Text = memberDto.Ort;
            _bemerkungenEditor.Text = memberDto.Bemerkungen;

            SetOptionalDate(_geburtsdatumEnabledSwitch, _geburtsdatumPicker, memberDto.Geburtsdatum);
            SetOptionalDate(_mitgliedSeitEnabledSwitch, _mitgliedSeitPicker, memberDto.MitgliedSeit);
            SetOptionalDate(_mitgliedEndeEnabledSwitch, _mitgliedEndePicker, memberDto.MitgliedEnde);

            UpdateAdminActions(memberDto);
        }
        catch (Exception)
        {
            _statusLabel.Text = "Nutzer hinzufügen fehlgeschlagen. Bitte später erneut versuchen.";
        }
        finally
        {
            _isBusy = false;
        }
    }

    private void SetEmptyState()
    {
        _memberRecord = null;
        _hasLinkedAppUser = false;
        _nachnameEntry.Text = string.Empty;
        _vornameEntry.Text = string.Empty;
        _emailEntry.Text = string.Empty;
        _emailEntry.IsReadOnly = true;
        _emailHintLabel.Text = string.Empty;
        _rolleLabel.Text = "-";
        _telefonEntry.Text = string.Empty;
        _mobilEntry.Text = string.Empty;
        _whatsappSwitch.IsToggled = false;
        _strasseEntry.Text = string.Empty;
        _plzEntry.Text = string.Empty;
        _ortEntry.Text = string.Empty;
        _bemerkungenEditor.Text = string.Empty;
        SetOptionalDate(_geburtsdatumEnabledSwitch, _geburtsdatumPicker, null);
        SetOptionalDate(_mitgliedSeitEnabledSwitch, _mitgliedSeitPicker, null);
        SetOptionalDate(_mitgliedEndeEnabledSwitch, _mitgliedEndePicker, null);
        UpdateAdminActions(null);
    }

    private void UpdateAdminActions(MemberDTO? member)
    {
        var canManageUsers = _userContextState.CurrentUserContext?.Role is UserRole.Admin or UserRole.Vorstand;
        var canEditEmailInMemberContext = member != null && !_hasLinkedAppUser;

        _emailEntry.IsReadOnly = !canEditEmailInMemberContext;
        _emailHintLabel.Text = member == null
            ? string.Empty
            : _hasLinkedAppUser
                ? "Für dieses Mitglied besteht bereits ein App-User/Auth-User. Die Mailadresse kann im normalen Stammdatenpfad nicht direkt geändert werden und muss vom Nutzer selbst über den vorgesehenen Self-Service-Mailänderungsweg geändert werden."
                : "Für dieses Mitglied besteht noch kein App-User/Auth-User. Die Mailadresse kann hier im Stammdatenpfad gespeichert werden und wird anschließend für den produktiven Invite-/Erstlogin-Flow verwendet.";

        _benutzerverwaltungButton.IsVisible = canManageUsers && member?.Id is > 0;
        _nutzerHinzufuegenButton.IsVisible = canManageUsers && member?.Id is > 0 && !_hasLinkedAppUser;
        _nutzerHinzufuegenButton.IsEnabled = _nutzerHinzufuegenButton.IsVisible && !string.IsNullOrWhiteSpace(member?.Email);

        _appUserHintLabel.Text = !canManageUsers || member == null
            ? "Benutzerverwaltung und Nutzer hinzufügen sind mobil nur für Admin oder Vorstand verfügbar."
            : _hasLinkedAppUser
                ? "Für dieses Mitglied besteht bereits ein App-User. Weitere Auth-Aktionen laufen über die gebundene Benutzerverwaltung."
                : string.IsNullOrWhiteSpace(member.Email)
                    ? "Für 'Nutzer hinzufügen' wird eine E-Mail-Adresse im ausgewählten Mitglied benötigt."
                    : "Für dieses Mitglied besteht aktuell noch kein App-User. Über 'Nutzer hinzufügen' wird derselbe produktive Invite-/Erstlogin-Flow wie in WPF gestartet.";
    }

    private async void OnNutzerHinzufuegenClicked(object? sender, EventArgs e)
    {
        if (_memberRecord == null)
            return;

        var currentRole = _userContextState.CurrentUserContext?.Role;
        if (currentRole is not UserRole.Admin and not UserRole.Vorstand)
        {
            await DisplayAlert("Hinweis", "Nutzer hinzufügen ist mobil nur für Admin oder Vorstand freigegeben.", "OK");
            return;
        }

        var targetUser = CreateInviteUser(_memberRecord);
        if (string.IsNullOrWhiteSpace(targetUser.Email))
        {
            await DisplayAlert("Hinweis", "Für 'Nutzer hinzufügen' wird eine E-Mail-Adresse im ausgewählten Mitglied benötigt.", "OK");
            return;
        }

        _isBusy = true;
        try
        {
            var result = await _authService.InviteUserAsync(targetUser);
            _statusLabel.Text = result.Message ?? (result.Success ? "Der produktive Invite-/Erstlogin-Flow wurde angestoßen." : "Nutzer hinzufügen fehlgeschlagen.");
            if (result.Success)
                await LoadAsync();
        }
        catch (Exception)
        {
            _statusLabel.Text = "Nutzer hinzufügen fehlgeschlagen. Bitte später erneut versuchen.";
        }
        finally
        {
            _isBusy = false;
        }
    }

    private async void OnSaveClicked(object? sender, EventArgs e)
    {
        if (_memberRecord == null)
            return;

        var currentRole = _userContextState.CurrentUserContext?.Role;
        if (currentRole is not UserRole.Admin and not UserRole.Vorstand)
        {
            await DisplayAlert("Hinweis", "Stammdaten können mobil nur von Admin oder Vorstand gespeichert werden.", "OK");
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
        try
        {
            lockAcquired = await _supabaseService.TryLockMitgliedAsync(_memberRecord.Id, userId);
            if (!lockAcquired)
            {
                await DisplayAlert("Gesperrt", "Datensatz ist aktuell gesperrt. Bitte später erneut versuchen.", "OK");
                return;
            }

            var current = await _supabaseService.GetMitgliedByIdAsync(_memberRecord.Id);
            if (current == null)
            {
                await DisplayAlert("Fehler", "Mitglied konnte nicht geladen werden.", "OK");
                return;
            }

            var dto = MapMember(current);
            dto.Vorname = (_vornameEntry.Text ?? string.Empty).Trim();
            dto.Nachname = (_nachnameEntry.Text ?? string.Empty).Trim();
            dto.Geburtsdatum = GetOptionalDate(_geburtsdatumEnabledSwitch, _geburtsdatumPicker);
            dto.Telefon = (_telefonEntry.Text ?? string.Empty).Trim();
            dto.Mobilnummer = (_mobilEntry.Text ?? string.Empty).Trim();
            dto.WhatsappEinwilligung = _whatsappSwitch.IsToggled;
            dto.Strasse = (_strasseEntry.Text ?? string.Empty).Trim();
            dto.PLZ = (_plzEntry.Text ?? string.Empty).Trim();
            dto.Ort = (_ortEntry.Text ?? string.Empty).Trim();
            dto.Bemerkungen = (_bemerkungenEditor.Text ?? string.Empty).Trim();
            dto.MitgliedSeit = GetOptionalDate(_mitgliedSeitEnabledSwitch, _mitgliedSeitPicker);
            dto.MitgliedEnde = GetOptionalDate(_mitgliedEndeEnabledSwitch, _mitgliedEndePicker);
            dto.Role = current.Role ?? dto.Role;
            dto.Email = _hasLinkedAppUser
                ? current.Email ?? dto.Email
                : (_emailEntry.Text ?? string.Empty).Trim();

            var ok = await _supabaseService.UpdateMitgliedAsync(dto, userId);
            if (!ok)
            {
                await DisplayAlert("Fehler", "Stammdaten konnten nicht gespeichert werden.", "OK");
                return;
            }

            _memberContextState.SetSelectedMember(dto);
            _statusLabel.Text = "Stammdaten gespeichert.";
            await DisplayAlert("OK", "Stammdaten gespeichert.", "OK");
            await LoadAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Fehler", ex.Message, "OK");
        }
        finally
        {
            if (lockAcquired)
                await _supabaseService.ReleaseLockMitgliedAsync(_memberRecord.Id, userId, force: false);

            _isBusy = false;
        }
    }

    private async Task<bool> HasLinkedAppUserAsync(int mitgliedId)
    {
        var users = await _authService.GetAppUsersAsync();
        return users.Any(x => x.MitgliedId == mitgliedId && x.AuthUserId.HasValue);
    }

    private static AppUserDTO CreateInviteUser(MitgliedRecord member)
    {
        var displayName = string.Join(' ', new[] { member.Vorname, member.Name }
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!.Trim()));

        return new AppUserDTO
        {
            MitgliedId = member.Id,
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? $"Mitglied #{member.Id}" : displayName,
            Email = member.Email ?? string.Empty,
            Role = UserRoles.User,
            Aktiv = true
        };
    }

    private static MemberDTO MapMember(MitgliedRecord rec)
    {
        return new MemberDTO
        {
            Id = rec.Id,
            Vorname = rec.Vorname ?? string.Empty,
            Nachname = rec.Name ?? string.Empty,
            Geburtsdatum = rec.Geburtsdatum,
            Strasse = rec.Adresse ?? string.Empty,
            PLZ = rec.Plz ?? string.Empty,
            Ort = rec.Ort ?? string.Empty,
            Telefon = rec.Telefon ?? string.Empty,
            Mobilnummer = rec.Handy ?? string.Empty,
            Email = rec.Email ?? string.Empty,
            Bemerkungen = rec.Bemerkung ?? string.Empty,
            WhatsappEinwilligung = rec.WhatsappEinwilligung,
            MitgliedSeit = rec.MitgliedSeit,
            MitgliedEnde = rec.MitgliedEnde,
            Role = rec.Role ?? string.Empty,
            IstHauptmitglied = rec.HauptmitgliedId == null
        };
    }

    private static string NormalizeRole(string? role)
        => UserRoles.ToStorageValue(UserRoles.Parse(role));

    private static void SetOptionalDate(Switch toggle, DatePicker picker, DateTime? value)
    {
        toggle.IsToggled = value.HasValue;
        picker.Date = (value ?? DateTime.Today).Date;
        picker.IsEnabled = toggle.IsToggled;
    }

    private static DateTime? GetOptionalDate(Switch toggle, DatePicker picker)
        => toggle.IsToggled ? picker.Date.Date : null;

    private static Label CreateReadOnlyLabel()
        => new() { LineBreakMode = LineBreakMode.WordWrap };

    private static Border CreateSection(string title, params View[] children)
    {
        var stack = new VerticalStackLayout { Spacing = 8 };
        stack.Children.Add(new Label { Text = title, FontAttributes = FontAttributes.Bold, FontSize = 18 });

        foreach (var child in children)
            stack.Children.Add(child);

        return new Border
        {
            Stroke = Colors.LightGray,
            StrokeThickness = 1,
            Padding = 14,
            Content = stack
        };
    }

    private static View CreateEditorField(string title, InputView input)
    {
        return new VerticalStackLayout
        {
            Spacing = 4,
            Children =
            {
                new Label { Text = title, FontAttributes = FontAttributes.Bold, FontSize = 12, TextColor = Colors.Gray },
                input
            }
        };
    }

    private static View CreateReadOnlyField(string title, View valueView)
    {
        return new VerticalStackLayout
        {
            Spacing = 4,
            Children =
            {
                new Label { Text = title, FontAttributes = FontAttributes.Bold, FontSize = 12, TextColor = Colors.Gray },
                valueView
            }
        };
    }

    private static View CreateSwitchField(string title, Switch toggle)
    {
        return new VerticalStackLayout
        {
            Spacing = 4,
            Children =
            {
                new Label { Text = title, FontAttributes = FontAttributes.Bold, FontSize = 12, TextColor = Colors.Gray },
                new HorizontalStackLayout
                {
                    Spacing = 8,
                    Children = { toggle, new Label { Text = "aktiv", VerticalTextAlignment = TextAlignment.Center } }
                }
            }
        };
    }

    private static View CreateOptionalDateField(string title, Switch toggle, DatePicker picker)
    {
        return new VerticalStackLayout
        {
            Spacing = 4,
            Children =
            {
                new Label { Text = title, FontAttributes = FontAttributes.Bold, FontSize = 12, TextColor = Colors.Gray },
                new HorizontalStackLayout
                {
                    Spacing = 8,
                    Children =
                    {
                        toggle,
                        picker
                    }
                }
            }
        };
    }

    private static string FormatValue(string? value)
        => string.IsNullOrWhiteSpace(value) ? "-" : value.Trim();
}
