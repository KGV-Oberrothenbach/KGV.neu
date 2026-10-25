using KGV.Core.Interfaces;
using KGV.Core.Models;
using KGV.Core.Security;
using KGV.Core.Services;
using KGV.Core.Utilities;
using KGV.Maui.State;
using Microsoft.Maui;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KGV.Maui.Pages;

public sealed class MemberDetailPage : ContentPage, IQueryAttributable
{
    private readonly ISupabaseService _supabaseService;
    private readonly IAuthService _authService;
    private readonly PreparedMembershipApplicationResumeService _preparedMembershipApplicationResumeService;
    private readonly IContractGenerationClient _contractGenerationClient;
    private readonly IPreparedContractRepository _preparedContractRepository;
    private readonly MitgliedsantragVereinsunterlagenService _vereinsunterlagenService;
    private readonly MemberSearchRefreshState _memberSearchRefreshState;
    private readonly UserContextState _userContextState;
    private readonly MemberContextState _memberContextState;

    private MitgliedRecord? _memberRecord;
    private KommunikationsvorgangDto? _activeMembershipApplicationContract;
    private bool _hasLinkedAppUser;
    private bool _hasSignedMitgliedsantrag;
    private bool _isBusy;
    private bool _isCreateMode;

    private readonly Label _headlineLabel;
    private readonly Label _statusLabel;
    private readonly Label _emailHintLabel;
    private readonly Label _rolleLabel;
    private readonly Label _appUserHintLabel;
    private readonly Label _mitgliedsantragDiagnoseLabel;
    private readonly Label _mitgliedschaftStatusLabel;
    private readonly Picker _arbeitsstundenAltersregelTypPicker;
    private readonly Entry _vornameEntry;
    private readonly Entry _nachnameEntry;
    private readonly Entry _emailEntry;
    private readonly Switch _geburtsdatumEnabledSwitch;
    private readonly DatePicker _geburtsdatumPicker;
    private readonly Entry _telefonEntry;
    private readonly Entry _mobilEntry;
    private readonly Switch _whatsappSwitch;
    private readonly Switch _rechnungMailSwitch;
    private readonly Switch _infoMailSwitch;
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
    private readonly Button _mitgliedsantragButton;
    private readonly Button _zuHauptmitgliedMachenButton;
    private readonly Button _cancelMembershipButton;
    private readonly Button _saveButton;
    private readonly Button _cancelButton;
    private readonly View _arbeitsstundenAltersregelTypField;
    private readonly MemberClubDocumentsSection _clubDocumentsSection;

    public MemberDetailPage(
        ISupabaseService supabaseService,
        IAuthService authService,
        PreparedMembershipApplicationResumeService preparedMembershipApplicationResumeService,
        IContractGenerationClient contractGenerationClient,
        IPreparedContractRepository preparedContractRepository,
        MitgliedsantragVereinsunterlagenService vereinsunterlagenService,
        MemberSearchRefreshState memberSearchRefreshState,
        UserContextState userContextState,
        MemberContextState memberContextState)
    {
        _supabaseService = supabaseService;
        _authService = authService;
        _preparedMembershipApplicationResumeService = preparedMembershipApplicationResumeService;
        _contractGenerationClient = contractGenerationClient;
        _preparedContractRepository = preparedContractRepository;
        _vereinsunterlagenService = vereinsunterlagenService;
        _memberSearchRefreshState = memberSearchRefreshState;
        _userContextState = userContextState;
        _memberContextState = memberContextState;

        Title = "Stammdaten";

        _headlineLabel = new Label { FontSize = 24, FontAttributes = FontAttributes.Bold };
        _statusLabel = new Label { TextColor = Colors.DarkRed, LineBreakMode = LineBreakMode.WordWrap };
        _emailEntry = new Entry { Placeholder = "E-Mail", Keyboard = Keyboard.Email };
        _emailHintLabel = new Label { TextColor = Colors.Gray, LineBreakMode = LineBreakMode.WordWrap };
        _rolleLabel = CreateReadOnlyLabel();
        _appUserHintLabel = new Label { TextColor = Colors.Gray, LineBreakMode = LineBreakMode.WordWrap };
        _mitgliedsantragDiagnoseLabel = new Label { TextColor = Colors.DarkOrange, LineBreakMode = LineBreakMode.WordWrap, FontSize = 12 };
        _mitgliedschaftStatusLabel = new Label { TextColor = Colors.Gray, LineBreakMode = LineBreakMode.WordWrap, IsVisible = false };
        _arbeitsstundenAltersregelTypPicker = new Picker { Title = "Altersregel wählen" };
        foreach (var option in MemberDTO.HauptmitgliedArbeitsstundenAltersregelTypOptions)
            _arbeitsstundenAltersregelTypPicker.Items.Add(option);

        _vornameEntry = new Entry { Placeholder = "Vorname" };
        _nachnameEntry = new Entry { Placeholder = "Nachname" };

        _geburtsdatumEnabledSwitch = new Switch();
        _geburtsdatumPicker = new DatePicker { IsEnabled = false };
        _geburtsdatumEnabledSwitch.Toggled += (_, e) => _geburtsdatumPicker.IsEnabled = e.Value;

        _telefonEntry = new Entry { Placeholder = "Telefon" };
        _mobilEntry = new Entry { Placeholder = "Mobilnummer" };
        _whatsappSwitch = new Switch();
        _rechnungMailSwitch = new Switch();
        _infoMailSwitch = new Switch();

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

        _mitgliedsantragButton = new Button { Text = "Mitgliedsantrag erstellen", IsVisible = false };
        _mitgliedsantragButton.Clicked += async (_, _) =>
        {
            if (_activeMembershipApplicationContract is not null)
                await Shell.Current.GoToAsync("//kommunikation");
            else if (_memberRecord?.Id is > 0)
                await CreateMitgliedsantragAsync(_memberRecord.Id);
        };

        _zuHauptmitgliedMachenButton = new Button { Text = "Zu Hauptmitglied machen", IsVisible = false };
        _zuHauptmitgliedMachenButton.Clicked += async (_, _) => await PromoteNebenmitgliedToHauptmitgliedAsync();

        _cancelMembershipButton = new Button { Text = "Mitgliedschaft beenden", IsVisible = false, BackgroundColor = Colors.IndianRed, TextColor = Colors.White };
        _cancelMembershipButton.Clicked += async (_, _) => await CancelMembershipAsync();

        _cancelButton = new Button { Text = "Abbrechen" };
        _cancelButton.Clicked += async (_, _) =>
        {
            if (_isCreateMode)
                await Shell.Current.GoToAsync("..");
            else
                await LoadAsync();
        };

        _saveButton = new Button { Text = "Speichern" };
        _saveButton.Clicked += OnSaveClicked;

        _arbeitsstundenAltersregelTypField = CreatePickerField("Arbeitsstunden-Altersregel", _arbeitsstundenAltersregelTypPicker);
        _clubDocumentsSection = new MemberClubDocumentsSection(_supabaseService, _userContextState, this);

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
                        _arbeitsstundenAltersregelTypField,
                        CreateEditorField("E-Mail", _emailEntry),
                        _emailHintLabel,
                        CreateReadOnlyField("Rolle", _rolleLabel)),
                    CreateSection("Kontakt",
                        CreateEditorField("Telefon", _telefonEntry),
                        CreateEditorField("Mobilnummer", _mobilEntry),
                        CreateSwitchField("WhatsApp", _whatsappSwitch),
                        CreateSwitchField("Rechnung per Mail", _rechnungMailSwitch),
                        CreateSwitchField("Info per Mail", _infoMailSwitch)),
                    CreateSection("Adresse",
                        CreateEditorField("Straße / Hausnummer", _strasseEntry),
                        CreateEditorField("PLZ", _plzEntry),
                        CreateEditorField("Ort", _ortEntry)),
                    CreateSection("Mitgliedschaft",
                        CreateOptionalDateField("Mitglied seit", _mitgliedSeitEnabledSwitch, _mitgliedSeitPicker),
                        CreateOptionalDateField("Mitglied Ende", _mitgliedEndeEnabledSwitch, _mitgliedEndePicker),
                        CreateEditorField("Bemerkungen", _bemerkungenEditor),
                        _mitgliedschaftStatusLabel,
                        _zuHauptmitgliedMachenButton),
                    CreateSection("Admin-Menü",
                        _appUserHintLabel,
                        _nutzerHinzufuegenButton,
                        _benutzerverwaltungButton),
                    _clubDocumentsSection,
                    _mitgliedsantragButton,
                    _mitgliedsantragDiagnoseLabel,
                    _cancelMembershipButton,
                    new FlexLayout
                    {
                        Direction = FlexDirection.Row,
                        Wrap = FlexWrap.Wrap,
                        Children = { _cancelButton, _saveButton }
                    }
                }
            }
        };

        Appearing += async (_, _) => await LoadAsync();
    }

    // Run signature capture sequence for the given placeholders and apply them into the provided PDF bytes.
    private async Task<byte[]> CreateAndApplySignaturesAsync(byte[] pdfBytes, IReadOnlyList<KGV.Core.Models.SignaturePlaceholder> placeholders, Func<KGV.Core.Models.SignaturePlaceholder, string> titleForPlaceholder)
    {
        var captures = new List<(KGV.Core.Models.SignaturePlaceholder placeholder, KGV.Core.Models.DigitalSignatureCapture capture)>();

        foreach (var placeholder in placeholders)
        {
            var title = titleForPlaceholder(placeholder);
            var request = new DokumentUploadRequest
            {
                Titel = title,
                FileName = "temp.pdf",
                MimeType = "application/pdf",
                FileContent = pdfBytes
            };

            var capture = await CaptureMitgliedsantragSignatureAsync(request, title);
            if (capture == null)
                throw new OperationCanceledException("Signaturvorgang abgebrochen.");

            captures.Add((placeholder, capture));
        }

        var updated = KGV.Core.Utilities.SignedVertragsdokumentPdfBuilder.InsertSignaturesIntoPdf(pdfBytes, captures);
        return updated;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        _isCreateMode = query.TryGetValue("mode", out var mode)
            && string.Equals(mode?.ToString(), "new", StringComparison.OrdinalIgnoreCase);
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
            if (_isCreateMode)
            {
                ConfigureCreateMode();
                return;
            }

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
            _hasSignedMitgliedsantrag = await _supabaseService.HasSignedMitgliedsantragAsync(member.Id);
            _activeMembershipApplicationContract = await _preparedContractRepository.FindActiveAsync(PreparedContractConstants.MembershipApplication, member.Id, null);
            await UpdateMitgliedschaftStatusAsync(member);
            var memberDto = MapMember(member);
            var permissionSettings = await _supabaseService.GetUserPermissionSettingsAsync(member.Id);
            memberDto.Role = NormalizeRole(permissionSettings?.Role ?? UserRoles.User);
            _memberContextState.SetSelectedMember(memberDto);
            _hasLinkedAppUser = member.AuthUserId.HasValue || await HasLinkedAppUserAsync(member.Id);

            _headlineLabel.Text = FormatMemberDisplayName(member);

            _nachnameEntry.Text = memberDto.Nachname;
            _vornameEntry.Text = memberDto.Vorname;
            _emailEntry.Text = memberDto.Email;
            _rolleLabel.Text = FormatValue(memberDto.Role);
            _arbeitsstundenAltersregelTypPicker.SelectedItem = MemberDTO.HauptmitgliedArbeitsstundenAltersregelTypOptions.Contains(memberDto.ArbeitsstundenAltersregelTyp, StringComparer.Ordinal)
                ? memberDto.ArbeitsstundenAltersregelTyp
                : null;
            _telefonEntry.Text = memberDto.Telefon;
            _mobilEntry.Text = memberDto.Mobilnummer;
            _whatsappSwitch.IsToggled = memberDto.WhatsappEinwilligung;
            _rechnungMailSwitch.IsToggled = memberDto.EmailRechnungEinwilligung;
            _infoMailSwitch.IsToggled = memberDto.EmailInfoEinwilligung;
            _strasseEntry.Text = memberDto.Strasse;
            _plzEntry.Text = memberDto.PLZ;
            _ortEntry.Text = memberDto.Ort;
            _bemerkungenEditor.Text = memberDto.Bemerkungen;

            SetOptionalDate(_geburtsdatumEnabledSwitch, _geburtsdatumPicker, memberDto.Geburtsdatum);
            SetOptionalDate(_mitgliedSeitEnabledSwitch, _mitgliedSeitPicker, memberDto.MitgliedSeit);
            SetOptionalDate(_mitgliedEndeEnabledSwitch, _mitgliedEndePicker, memberDto.MitgliedEnde);

            UpdateArbeitsstundenAltersregelVisibility(memberDto);
            UpdateAdminActions(memberDto);
            await _clubDocumentsSection.LoadAsync(member);
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
        _activeMembershipApplicationContract = null;
        _hasLinkedAppUser = false;
        _hasSignedMitgliedsantrag = false;
        _mitgliedschaftStatusLabel.Text = string.Empty;
        _mitgliedschaftStatusLabel.IsVisible = false;
        _zuHauptmitgliedMachenButton.IsVisible = false;
        _nachnameEntry.Text = string.Empty;
        _vornameEntry.Text = string.Empty;
        _emailEntry.Text = string.Empty;
        _emailEntry.IsReadOnly = true;
        _emailHintLabel.Text = string.Empty;
        _rolleLabel.Text = "-";
        _arbeitsstundenAltersregelTypPicker.SelectedItem = null;
        _telefonEntry.Text = string.Empty;
        _mobilEntry.Text = string.Empty;
        _whatsappSwitch.IsToggled = false;
        _rechnungMailSwitch.IsToggled = false;
        _infoMailSwitch.IsToggled = false;
        _strasseEntry.Text = string.Empty;
        _plzEntry.Text = string.Empty;
        _ortEntry.Text = string.Empty;
        _bemerkungenEditor.Text = string.Empty;
        SetOptionalDate(_geburtsdatumEnabledSwitch, _geburtsdatumPicker, null);
        SetOptionalDate(_mitgliedSeitEnabledSwitch, _mitgliedSeitPicker, null);
        SetOptionalDate(_mitgliedEndeEnabledSwitch, _mitgliedEndePicker, null);
        UpdateAdminActions(null);
        UpdateFormActions(null);
        UpdateCancelMembershipButton(null);
        _clubDocumentsSection.IsVisible = false;
    }

    private void ConfigureCreateMode()
    {
        _memberRecord = null;
        _activeMembershipApplicationContract = null;
        _hasLinkedAppUser = false;
        _hasSignedMitgliedsantrag = false;
        _mitgliedschaftStatusLabel.Text = string.Empty;
        _mitgliedschaftStatusLabel.IsVisible = false;
        _zuHauptmitgliedMachenButton.IsVisible = false;
        _headlineLabel.Text = "Neues Mitglied";
        _statusLabel.Text = "Neues Mitglied anlegen.";
        _nachnameEntry.Text = string.Empty;
        _vornameEntry.Text = string.Empty;
        _emailEntry.Text = string.Empty;
        _emailEntry.IsReadOnly = false;
        _emailHintLabel.Text = "Für neue Mitglieder kann eine E-Mail-Adresse direkt hinterlegt werden.";
        _rolleLabel.Text = UserRoles.User;
        _arbeitsstundenAltersregelTypPicker.SelectedItem = null;
        _telefonEntry.Text = string.Empty;
        _mobilEntry.Text = string.Empty;
        _whatsappSwitch.IsToggled = false;
        _rechnungMailSwitch.IsToggled = false;
        _infoMailSwitch.IsToggled = false;
        _strasseEntry.Text = string.Empty;
        _plzEntry.Text = string.Empty;
        _ortEntry.Text = string.Empty;
        _bemerkungenEditor.Text = string.Empty;
        SetOptionalDate(_geburtsdatumEnabledSwitch, _geburtsdatumPicker, null);
        SetOptionalDate(_mitgliedSeitEnabledSwitch, _mitgliedSeitPicker, DateTime.Today);
        SetOptionalDate(_mitgliedEndeEnabledSwitch, _mitgliedEndePicker, null);
        _saveButton.Text = "Mitglied anlegen";
        _nutzerHinzufuegenButton.IsVisible = false;
        _benutzerverwaltungButton.IsVisible = false;
        _mitgliedsantragButton.IsVisible = false;
        _mitgliedsantragDiagnoseLabel.Text = BuildMitgliedsantragDiagnoseText(null);
        _appUserHintLabel.Text = "Der App-User wird nicht direkt beim Anlegen erzeugt, sondern später über den bestehenden Invite-/Benutzerverwaltungsweg.";
        UpdateArbeitsstundenAltersregelVisibility(new MemberDTO { IstHauptmitglied = true });
        UpdateFormActions(null);
        UpdateCancelMembershipButton(null);
        _clubDocumentsSection.IsVisible = false;
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

        UpdateFormActions(member);
        UpdateArbeitsstundenAltersregelVisibility(member);
        UpdateCancelMembershipButton(member);
    }

    private async Task UpdateMitgliedschaftStatusAsync(MitgliedRecord member)
    {
        if (member.HauptmitgliedId is not > 0)
        {
            _mitgliedschaftStatusLabel.Text = string.Empty;
            _mitgliedschaftStatusLabel.IsVisible = false;
            _zuHauptmitgliedMachenButton.IsVisible = false;
            return;
        }

        var hauptmitglied = await _supabaseService.GetMitgliedByIdAsync(member.HauptmitgliedId.Value);
        var hauptmitgliedName = FormatMemberDisplayName(hauptmitglied);
        _mitgliedschaftStatusLabel.Text = $"Nebenmitglied von {hauptmitgliedName}";
        _mitgliedschaftStatusLabel.IsVisible = true;
        _zuHauptmitgliedMachenButton.IsVisible = true;
    }

    private async Task PromoteNebenmitgliedToHauptmitgliedAsync()
    {
        if (_isBusy || _isCreateMode || _memberRecord?.Id is not > 0 || _memberRecord.HauptmitgliedId is not > 0)
            return;

        var nebenmitglied = _memberRecord;
        var hauptmitglied = await _supabaseService.GetMitgliedByIdAsync(nebenmitglied.HauptmitgliedId.Value);
        var nebenmitgliedName = FormatMemberDisplayName(nebenmitglied);
        var hauptmitgliedName = FormatMemberDisplayName(hauptmitglied);
        var confirmed = await DisplayAlertAsync(
            "Zu Hauptmitglied machen",
            $"{nebenmitgliedName} wird zu einem eigenständigen Hauptmitglied.\nDie Zuordnung zu {hauptmitgliedName} wird aufgehoben.\nDas bisherige Hauptmitglied bleibt unverändert bestehen.\n\nFortfahren?",
            "Fortfahren",
            "Abbrechen");
        if (!confirmed)
            return;

        _isBusy = true;
        MitgliedRecord? promoted = null;
        try
        {
            promoted = await _supabaseService.PromoteNebenmitgliedToHauptmitgliedAsync(nebenmitglied.Id);
            if (promoted == null || promoted.HauptmitgliedId is > 0)
            {
                await DisplayAlertAsync("Fehler", "Das Nebenmitglied konnte nicht zu einem Hauptmitglied gemacht werden.", "OK");
                return;
            }

            _memberRecord = promoted;
            _memberContextState.SetSelectedMember(MapMember(promoted));
            _memberSearchRefreshState.RequestReload();
            await DisplayAlertAsync("OK", "Das Mitglied ist jetzt ein eigenständiges Hauptmitglied.", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Fehler", ex.Message, "OK");
            return;
        }
        finally
        {
            _isBusy = false;
        }

        if (promoted != null)
            await LoadAsync();
    }

    private void UpdateFormActions(MemberDTO? member)
    {
        var canCreateMitglied = PermissionChecks.CanCreateMitglied(_userContextState.CurrentUserContext);
        var canCreateMemberApplication = !_isCreateMode
            && member?.Id is > 0
            && member.IstHauptmitglied
            && canCreateMitglied
            && !_hasSignedMitgliedsantrag
            && _activeMembershipApplicationContract is null;

        var activeStatus = _activeMembershipApplicationContract?.Status;
        _mitgliedsantragButton.IsVisible = canCreateMemberApplication || activeStatus is not null;
        _mitgliedsantragButton.IsEnabled = _mitgliedsantragButton.IsVisible;
        _mitgliedsantragButton.Text = activeStatus is null ? "Mitgliedsantrag erstellen" : "In Vorgängen öffnen";
        _mitgliedsantragDiagnoseLabel.Text = activeStatus switch
        {
            PreparedContractConstants.Prepared => "Mitgliedsantrag: Zur Signatur vorbereitet",
            PreparedContractConstants.ReadyForSignature => "Mitgliedsantrag: Signatur läuft",
            PreparedContractConstants.FinalReview => "Mitgliedsantrag: Letzte Prüfung",
            _ when _hasSignedMitgliedsantrag => "Signierter Mitgliedsantrag liegt vor.",
            _ => string.Empty
        };
    }

    private static string FormatMemberDisplayName(MitgliedRecord? member)
    {
        if (member == null)
            return "Mitglied nicht verfügbar";

        var name = string.Join(' ', new[] { member?.Vorname, member?.Name }
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!.Trim()));
        if (member.Mitgliedsnummer <= 0)
            return "Mitgliedsnummer nicht verfügbar";

        return string.IsNullOrWhiteSpace(name)
            ? $"Mgl.-Nr. {member.Mitgliedsnummer}"
            : $"{name} · Mgl.-Nr. {member.Mitgliedsnummer}";
    }

    private string BuildMitgliedsantragDiagnoseText(MemberDTO? member)
    {
        var currentUserContext = _userContextState.CurrentUserContext;
        var memberId = member?.Id ?? 0;
        var canCreateMitglied = PermissionChecks.CanCreateMitglied(currentUserContext);
        var reasons = new List<string>();

        if (_isCreateMode)
            reasons.Add("Create-Modus aktiv");

        if (memberId <= 0)
            reasons.Add("member.Id <= 0");

        if (!canCreateMitglied)
            reasons.Add("CanCreateMitglied = false");

        var reasonText = reasons.Count == 0
            ? "Button sollte sichtbar sein."
            : $"Button unsichtbar wegen: {string.Join(", ", reasons)}";

        return $"[TEMP Diagnose Mitgliedsantrag] Mode={(_isCreateMode ? "Create" : "Detail")}, member.Id={memberId}, CanCreateMitglied={canCreateMitglied}, Rolle={currentUserContext?.Role.ToString() ?? "-"}. {reasonText}";
    }

    private void UpdateCancelMembershipButton(MemberDTO? member)
    {
        var canManageMembership = !_isCreateMode
            && member?.Id is > 0
            && member.IstHauptmitglied
            && !member.MitgliedEnde.HasValue
            && _userContextState.CurrentUserContext?.Role is UserRole.Admin or UserRole.Vorstand;

        _cancelMembershipButton.IsVisible = canManageMembership;
    }

    private async Task CancelMembershipAsync()
    {
        if (_memberRecord == null || _isCreateMode || _memberRecord.HauptmitgliedId.HasValue || _memberRecord.MitgliedEnde.HasValue)
            return;

        var userId = _authService.CurrentUserId;
        if (string.IsNullOrWhiteSpace(userId))
        {
            await DisplayAlertAsync("Fehler", "Nicht angemeldet. Bitte erneut einloggen.", "OK");
            return;
        }

        var secondaryMember = await _supabaseService.GetNebenmitgliedByHauptmitgliedIdAsync(_memberRecord.Id);
        MembershipEndDecision? decision = null;
        if (secondaryMember != null)
        {
            var action = await DisplayActionSheetAsync(
                "Folgeentscheid für Nebenmitglied",
                "Abbrechen",
                null,
                "Nebenmitglied ebenfalls beenden",
                "Nebenmitglied zum Hauptmitglied machen");

            if (string.Equals(action, "Abbrechen", StringComparison.Ordinal))
                return;

            decision = string.Equals(action, "Nebenmitglied ebenfalls beenden", StringComparison.Ordinal)
                ? MembershipEndDecision.EndSecondaryMember
                : MembershipEndDecision.PromoteSecondaryMember;
        }

        var confirmationText = decision switch
        {
            MembershipEndDecision.EndSecondaryMember => "Sollen Haupt- und Nebenmitglied zum heutigen Vereinsdatum beendet werden?",
            MembershipEndDecision.PromoteSecondaryMember => "Soll die Mitgliedschaft des Hauptmitglieds beendet und das Nebenmitglied als Hauptmitglied weitergeführt werden?",
            _ => "Soll die Mitgliedschaft zum heutigen Vereinsdatum beendet werden?"
        };
        var confirmed = await DisplayAlertAsync("Mitgliedschaft beenden", confirmationText, "Beenden", "Abbrechen");
        if (!confirmed)
            return;

        var lockAcquired = await _supabaseService.TryLockMitgliedAsync(_memberRecord.Id, userId);
        if (!lockAcquired)
        {
            await DisplayAlertAsync("Gesperrt", "Datensatz ist aktuell gesperrt. Bitte später erneut versuchen.", "OK");
            return;
        }

        try
        {
            var result = await _supabaseService.EndMembershipAsync(_memberRecord.Id, decision);
            if (!result.Success || result.UpdatedMainMember == null)
            {
                await DisplayAlertAsync("Fehler", string.IsNullOrWhiteSpace(result.Message) ? "Mitgliedschaft konnte nicht beendet werden." : result.Message, "OK");
                return;
            }

            _memberRecord = result.UpdatedMainMember;
            _memberContextState.SetSelectedMember(MapMember(result.UpdatedMainMember));
            _memberSearchRefreshState.RequestReload();
            await DisplayAlertAsync("OK", result.Message, "OK");
            await LoadAsync();
        }
        finally
        {
            await _supabaseService.ReleaseLockMitgliedAsync(_memberRecord.Id, userId, force: false);
        }
    }

    private async void OnNutzerHinzufuegenClicked(object? sender, EventArgs e)
    {
        if (_memberRecord == null)
            return;

        var currentRole = _userContextState.CurrentUserContext?.Role;
        if (currentRole is not UserRole.Admin and not UserRole.Vorstand)
        {
            await DisplayAlertAsync("Hinweis", "Nutzer hinzufügen ist mobil nur für Admin oder Vorstand freigegeben.", "OK");
            return;
        }

        var targetUser = CreateInviteUser(_memberRecord);
        if (string.IsNullOrWhiteSpace(targetUser.Email))
        {
            await DisplayAlertAsync("Hinweis", "Für 'Nutzer hinzufügen' wird eine E-Mail-Adresse im ausgewählten Mitglied benötigt.", "OK");
            return;
        }

        _isBusy = true;
        try
        {
            var result = await _authService.InviteUserAsync(targetUser);
            _statusLabel.Text = result.Success && (!result.MailSent || result.Outcome == "prepared_mail_failed")
                ? "Nutzerkonto wurde angelegt, die Willkommensmail konnte jedoch nicht versendet werden."
                : result.Message ?? (result.Success ? "Nutzerkonto wurde angelegt und die Willkommensmail versendet." : "Nutzer hinzufügen fehlgeschlagen.");
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

    private async Task CreateMitgliedsantragAsync(int mitgliedId, bool manageBusyState = true)
    {
        if ((manageBusyState && _isBusy) || mitgliedId <= 0)
            return;

        if (manageBusyState)
            _isBusy = true;

        try
        {
            if (!Guid.TryParse(_authService.CurrentUserId, out var currentUserId))
            {
                await DisplayAlertAsync("Mitgliedsantrag", "Der angemeldete Benutzer konnte nicht eindeutig bestimmt werden. Bitte erneut anmelden und den Vorgang wiederholen.", "OK");
                return;
            }

            var resumeResult = await _preparedMembershipApplicationResumeService.ResolveAsync(mitgliedId, currentUserId);
            switch (resumeResult.Decision)
            {
                case PreparedMembershipApplicationResumeDecision.New:
                    await ResumePreparedMitgliedsantragAsync(mitgliedId, null);
                    return;
                case PreparedMembershipApplicationResumeDecision.ResumeOwn:
                case PreparedMembershipApplicationResumeDecision.ResumeClaimed:
                    await ResumePreparedMitgliedsantragAsync(mitgliedId, resumeResult.Contract);
                    return;
                case PreparedMembershipApplicationResumeDecision.ForeignAssigned:
                    await DisplayAlertAsync("Mitgliedsantrag", "Der vorbereitete Mitgliedsantrag ist bereits einem anderen Benutzer zugewiesen und kann hier nicht bearbeitet werden.", "OK");
                    return;
                case PreparedMembershipApplicationResumeDecision.SignatureLocked:
                    await DisplayAlertAsync("Mitgliedsantrag", "Der Vorgang ist bereits zur Signatur vorbereitet oder gestartet. Der Entwurf darf nicht mehr bearbeitet werden.", "OK");
                    return;
                case PreparedMembershipApplicationResumeDecision.ClaimConflict:
                    await DisplayAlertAsync("Mitgliedsantrag", "Der Vorgang wurde zwischenzeitlich geändert oder übernommen. Bitte laden Sie neu und versuchen Sie es erneut.", "OK");
                    return;
                case PreparedMembershipApplicationResumeDecision.InvalidStatus:
                    await DisplayAlertAsync("Mitgliedsantrag", "Der vorbereitete Mitgliedsantrag befindet sich in einem ungültigen Workflow-Status und kann hier nicht bearbeitet werden.", "OK");
                    return;
                default:
                    await DisplayAlertAsync("Mitgliedsantrag", "Der Status des vorbereiteten Mitgliedsantrags konnte nicht verarbeitet werden.", "OK");
                    return;
            }

        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Mitgliedsantrag", ex.Message, "OK");
        }
        finally
        {
            if (manageBusyState)
                _isBusy = false;
        }
    }

    private async Task<MitgliedsantragDokumentRequest?> PromptMitgliedsantragRequestAsync(int mitgliedId, MitgliedsantragDokumentRequest? initialRequest = null, Func<MitgliedsantragDokumentRequest?, bool>? hasPreparedUnsavedChanges = null)
    {
        var member = _memberRecord?.Id == mitgliedId
            ? _memberRecord
            : await _supabaseService.GetMitgliedByIdAsync(mitgliedId);
        if (member == null)
        {
            await DisplayAlertAsync("Mitgliedsantrag", "Mitglied konnte nicht geladen werden.", "OK");
            return null;
        }

        MitgliedsantragBeitragVorschlag vorschlag;
        try
        {
            var saisons = await _supabaseService.GetSaisonRecordsAsync();
            vorschlag = MitgliedsantragBeitragHelper.CreateSuggestion(member, saisons);
        }
        catch (InvalidOperationException ex)
        {
            await DisplayAlertAsync("Mitgliedsantrag", ex.Message, "OK");
            return null;
        }

        var effectiveBeginnDatum = initialRequest?.BeginnDatum.Date ?? vorschlag.BeginnDatum;
        if (initialRequest != null)
            vorschlag = vorschlag with { BeginnDatum = effectiveBeginnDatum };
        var gesetzlicherVertreterAufloesung = await _supabaseService.ResolveGesetzlicherVertreterAsync(mitgliedId, effectiveBeginnDatum);
        var nebenmitglied = gesetzlicherVertreterAufloesung.IstMinderjaehrig
            ? null
            : await _supabaseService.GetNebenmitgliedByHauptmitgliedIdAsync(mitgliedId);
        var nebenmitgliedEinbeziehen = initialRequest?.NebenmitgliedEinbeziehen ?? false;
        if (!gesetzlicherVertreterAufloesung.IstMinderjaehrig && nebenmitglied != null && initialRequest == null)
        {
            nebenmitgliedEinbeziehen = await DisplayAlertAsync(
                "Nebenmitglied",
                $"Für {member.Vorname} {member.Name} ist {nebenmitglied.Vorname} {nebenmitglied.Name} als Nebenmitglied hinterlegt. Soll es den Mitgliedsantrag als zweite private Partei mitunterzeichnen?",
                "Ja",
                "Nein");
        }
        var vertreterMitglieder = gesetzlicherVertreterAufloesung.IstMinderjaehrig
            ? await _supabaseService.GetMitgliederAsync()
            : new List<MitgliedRecord>();

        var dialogPage = new MitgliedsantragDialogPage(member, vorschlag, gesetzlicherVertreterAufloesung, vertreterMitglieder, initialRequest, hasPreparedUnsavedChanges);
        await Navigation.PushModalAsync(new NavigationPage(dialogPage));
        var request = await dialogPage.WaitForResultAsync();
        if (request != null && !gesetzlicherVertreterAufloesung.IstMinderjaehrig)
        {
            request.NebenmitgliedEinbeziehen = nebenmitgliedEinbeziehen;
            request.NebenmitgliedId = nebenmitgliedEinbeziehen ? nebenmitglied?.Id : null;
        }
        return request;
    }

    private async Task<MitgliedsantragPreviewDecision> ShowMitgliedsantragPreviewAsync(DokumentUploadRequest previewUploadRequest, bool persistForSignature = true, string? preparedActionLabel = null)
    {
        var previewPage = new MitgliedsantragPreviewPage(previewUploadRequest, persistForSignature, preparedActionLabel);
        await Navigation.PushModalAsync(new NavigationPage(previewPage));
        return await previewPage.WaitForResultAsync();
    }

    private async Task ResumePreparedMitgliedsantragAsync(int mitgliedId, KommunikationsvorgangDto? contract)
    {
        GesetzlicherVertreterAufloesung? resolution = null;
        MitgliedsantragDokumentRequest? initial = null;
        if (contract is not null)
        {
            if (PreparedContractDraftParser.Parse(contract.EntwurfDaten) is not PreparedMembershipApplicationDraft draft || draft.MemberId != mitgliedId)
                throw new PreparedContractValidationException("Der vorbereitete Mitgliedsantrag passt nicht zum geöffneten Mitglied.");
            if (!DateTime.TryParseExact(draft.StartDate, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var startDate))
                throw new PreparedContractValidationException("Das gespeicherte Startdatum des Mitgliedsantrags ist ungültig.");
            resolution = await _supabaseService.ResolveGesetzlicherVertreterAsync(mitgliedId, startDate);
            initial = PreparedMembershipApplicationMapper.ToDocumentRequest(draft, resolution.IstMinderjaehrig);
        }
        while (true)
        {
            var request = await PromptMitgliedsantragRequestAsync(mitgliedId, initial, contract is null ? null : current =>
            {
                if (current is null) return true;
                try { return !PreparedContractDraftParser.AreEquivalent(contract.EntwurfDaten, PreparedContractDraftParser.Serialize(PreparedMembershipApplicationMapper.ToPreparedDraft(current, resolution!.IstMinderjaehrig))); }
                catch (PreparedContractValidationException) { return true; }
            });
            if (request is null)
                return;
            var saisons = await _supabaseService.GetSaisonRecordsAsync();
            var saison = saisons.FirstOrDefault(x => SaisonverwaltungHelper.GetSaisonJahr(x) == request.BeginnDatum.Year);
            if (saison?.Aufnahmegebuehr is not >= 0) throw new InvalidOperationException($"Für die Saison {request.BeginnDatum.Year} fehlt eine gültige Aufnahmegebühr.");
            var snapshot = await EnsureVereinsunterlagenForMitgliedsantragAsync(mitgliedId);
            if (snapshot is null) { initial = request; continue; }
            request.VereinsunterlagenSnapshot = snapshot;
            request.VereinsunterlagenNachweis = VereinsunterlagenNachweisHelper.FormatForPdf(request.VereinsunterlagenSnapshot);
            var serialized = PreparedContractDraftParser.Serialize(PreparedMembershipApplicationMapper.ToPreparedDraft(request, resolution?.IstMinderjaehrig ?? request.IstMinderjaehrig));
            if (contract is null)
            {
                _activeMembershipApplicationContract = await _preparedContractRepository.CreateAsync(PreparedContractConstants.MembershipApplication, mitgliedId, null, serialized);
                contract = _activeMembershipApplicationContract;
                UpdateFormActions(_memberRecord is null ? null : MapMember(_memberRecord));
            }
            else if (!PreparedContractDraftParser.AreEquivalent(contract.EntwurfDaten, serialized))
            {
                contract = await _preparedContractRepository.UpdateDraftAsync(contract.Id, contract.Revision, serialized);
            }
            var pdf = await _contractGenerationClient.PreviewMembershipApplicationAsync(request, decimal.Round(saison.Aufnahmegebuehr.Value, 2, MidpointRounding.AwayFromZero));
            var decision = await ShowMitgliedsantragPreviewAsync(new DokumentUploadRequest { MitgliedId = mitgliedId, Titel = "Mitgliedsantrag – Vorschau", FileName = "Mitgliedsantrag_Vorschau.pdf", MimeType = "application/pdf", FileContent = pdf }, persistForSignature: false, preparedActionLabel: contract is null ? "Zur Signatur vorbereiten" : "Änderungen speichern");
            if (decision == MitgliedsantragPreviewDecision.BackToEditor) { initial = request; continue; }
            if (decision == MitgliedsantragPreviewDecision.SavePrepared)
            {
                if (PreparedContractDraftParser.AreEquivalent(contract!.EntwurfDaten, serialized))
                    await DisplayAlertAsync("Mitgliedsantrag", "Änderungen sind bereits gespeichert.", "OK");
                else
                {
                    await _preparedContractRepository.UpdateDraftAsync(contract.Id, contract.Revision, serialized);
                    await DisplayAlertAsync("Mitgliedsantrag", "Änderungen wurden gespeichert.", "OK");
                }
                if (_activeMembershipApplicationContract is not null && await DisplayAlertAsync("Mitgliedsantrag", "Der Mitgliedsantrag wurde vorbereitet und unter Vorgänge > Signaturen > Meine gespeichert. Möchten Sie jetzt zur Vorgangsübersicht wechseln?", "Vorgänge öffnen", "Hier bleiben"))
                    await Shell.Current.GoToAsync("//kommunikation");
                return;
            }
            if (contract is not null && !PreparedContractDraftParser.AreEquivalent(contract.EntwurfDaten, PreparedContractDraftParser.Serialize(PreparedMembershipApplicationMapper.ToPreparedDraft(request, resolution!.IstMinderjaehrig))))
                await DisplayAlertAsync("Ungespeicherte Änderungen", "Ihre Änderungen am vorbereiteten Mitgliedsantrag wurden nicht gespeichert.", "OK");
            return;
        }
    }

    private async Task<IReadOnlyList<VereinsunterlagenNachweisSnapshotItem>?> EnsureVereinsunterlagenForMitgliedsantragAsync(int mitgliedId)
    {
        var status = await _supabaseService.GetMitgliedVereinsdokumentStatusAsync(mitgliedId, DateOnly.FromDateTime(DateTime.Today));
        try { return VereinsunterlagenNachweisHelper.CreateSnapshot(status); }
        catch (PreparedContractValidationException)
        {
            if (status.HatFehlendeVereinsfassungen)
            {
                await DisplayAlertAsync("Mitgliedsantrag", "Für mindestens eine Pflichtunterlage ist keine aktuelle Vereinsfassung hinterlegt. Die Vorschau bleibt gesperrt.", "OK");
                return null;
            }

            var missing = status.Dokumente.Where(item => item.Status == "fehlt" && item.VereinsdokumentFassungId.HasValue).ToList();
            if (missing.Count == 0) { await DisplayAlertAsync("Mitgliedsantrag", "Die Pflichtunterlagen sind nicht vollständig bereitgestellt.", "OK"); return null; }
            var list = string.Join("\n", missing.Select(item => $"• {item.Bezeichnung} ({item.Fassung})"));
            var action = await DisplayActionSheetAsync($"Folgende Unterlagen fehlen:\n{list}", "Abbrechen", null, "Digital senden", "Papierübergabe erfassen");
            if (action == "Digital senden")
            {
                if (string.IsNullOrWhiteSpace(_memberRecord?.Email)) { await DisplayAlertAsync("Digital senden", "Für dieses Mitglied ist keine E-Mail-Adresse hinterlegt. Eine Adresse des gesetzlichen Vertreters wird nicht automatisch verwendet.", "OK"); return null; }
                if (!await DisplayAlertAsync("Digital senden", $"Mitglied: {FormatMemberDisplayName(_memberRecord)}\nE-Mail: {_memberRecord!.Email}\n\n{list}", "Senden", "Abbrechen")) return null;
                var result = await _supabaseService.SendMitgliedVereinsunterlagenDigitalAsync(mitgliedId, missing.Select(item => item.VereinsdokumentFassungId!.Value).ToList());
                if (!result.Success) { await DisplayAlertAsync("Digital senden", DeliveryMessage(result), "OK"); return null; }
            }
            else if (action == "Papierübergabe erfassen")
            {
                var value = await DisplayPromptAsync("Papierübergabe", "Tatsächlicher Übergabezeitpunkt (TT.MM.JJJJ HH:MM)", initialValue: DateTime.Now.ToString("dd.MM.yyyy HH:mm"));
                if (!DateTime.TryParse(value, System.Globalization.CultureInfo.GetCultureInfo("de-DE"), System.Globalization.DateTimeStyles.AllowWhiteSpaces, out var deliveredAt) || deliveredAt > DateTime.Now) { await DisplayAlertAsync("Papierübergabe", "Bitte einen gültigen, nicht zukünftigen Übergabezeitpunkt eingeben.", "OK"); return null; }
                if (!await DisplayAlertAsync("Papierübergabe", $"{list}\n\nDie ausgewählten Unterlagen wurden dem Mitglied tatsächlich in Papierform übergeben.", "Erfassen", "Abbrechen")) return null;
                await _supabaseService.RecordMitgliedVereinsunterlagenPapierAsync(new MitgliedVereinsunterlagenBereitstellungRequest { MitgliedId = mitgliedId, VereinsdokumentFassungIds = missing.Select(item => item.VereinsdokumentFassungId!.Value).ToList(), BereitgestelltAm = deliveredAt });
            }
            else return null;
            var reloadedStatus = await _supabaseService.GetMitgliedVereinsdokumentStatusAsync(mitgliedId, DateOnly.FromDateTime(DateTime.Today));
            try { return VereinsunterlagenNachweisHelper.CreateSnapshot(reloadedStatus); }
            catch (PreparedContractValidationException) { await DisplayAlertAsync("Mitgliedsantrag", "Die Pflichtunterlagen sind nach der Bereitstellung weiterhin nicht vollständig. Die Vorschau bleibt gesperrt.", "OK"); return null; }
        }
    }

    private static string DeliveryMessage(MitgliedVereinsunterlagenBereitstellungResult result) => result.DiagnosticCode switch
    {
        "DOCUMENT_FILE_MISSING" => "Mindestens eine ausgewählte Fassung hat keine hinterlegte PDF-Datei.",
        "DIGITAL_SEND_FAILED" => "Der Versand wurde vom Maildienst nicht bestätigt.",
        "DELIVERY_RECORD_FAILED" => "Die E-Mail wurde versandt, aber der Bereitstellungsnachweis konnte nicht gespeichert werden.",
        _ => result.Message
    };

    private async Task<DigitalSignatureCapture?> CaptureMitgliedsantragSignatureAsync(DokumentUploadRequest previewUploadRequest, string unterschriftTitel, bool isLastSignature = true)
    {
        var sourceDocument = new DocumentInfo
        {
            Title = previewUploadRequest.Titel,
            Dateiname = previewUploadRequest.FileName,
            Name = previewUploadRequest.FileName,
            MimeType = previewUploadRequest.MimeType,
            // For local signing flows we provide the persistent local preview path so the file can be reopened if needed
            StoragePath = KGV.Maui.Services.Documents.DocumentStorage.GetPersistentFilePath(previewUploadRequest.FileName)
        };

        // Use shared signature helper so MA and PV use identical push/wait logic
        return await SignatureFlowHelper.CaptureSignatureAsync(Navigation, sourceDocument, unterschriftTitel, isLastSignature: isLastSignature, forceLandscape: false);
    }

    private async void OnSaveClicked(object? sender, EventArgs e)
    {
        var currentUserContext = _userContextState.CurrentUserContext;
        var canSave = _isCreateMode
            ? PermissionChecks.CanCreateMitglied(currentUserContext)
            : currentUserContext?.Role is UserRole.Admin or UserRole.Vorstand;

        if (!canSave)
        {
            await DisplayAlertAsync("Hinweis", _isCreateMode
                ? "Mitglieder anlegen ist mobil nur mit dem Fachrecht 'CreateMitglied' oder als Admin/Vorstand freigegeben."
                : "Stammdaten können mobil nur von Admin oder Vorstand gespeichert werden.", "OK");
            return;
        }

        if (string.IsNullOrWhiteSpace(_vornameEntry.Text))
        {
            await DisplayAlertAsync("Validierung", "Vorname ist erforderlich.", "OK");
            _vornameEntry.Focus();
            return;
        }

        if (string.IsNullOrWhiteSpace(_nachnameEntry.Text))
        {
            await DisplayAlertAsync("Validierung", "Nachname ist erforderlich.", "OK");
            _nachnameEntry.Focus();
            return;
        }

        var requiresArbeitsstundenAltersregel = _isCreateMode || _memberRecord?.HauptmitgliedId == null;
        var selectedArbeitsstundenAltersregelTyp = _arbeitsstundenAltersregelTypPicker.SelectedItem as string;
        if (requiresArbeitsstundenAltersregel && string.IsNullOrWhiteSpace(selectedArbeitsstundenAltersregelTyp))
        {
            await DisplayAlertAsync("Validierung", "Für Hauptmitglieder ist die Arbeitsstunden-Altersregel erforderlich.", "OK");
            _arbeitsstundenAltersregelTypPicker.Focus();
            return;
        }

        if (_isCreateMode)
        {
            _isBusy = true;
            try
            {
                var created = await _supabaseService.CreateMitgliedAsync(new MemberDTO
                {
                    Vorname = _vornameEntry.Text.Trim(),
                    Nachname = _nachnameEntry.Text.Trim(),
                    Email = (_emailEntry.Text ?? string.Empty).Trim(),
                    Telefon = (_telefonEntry.Text ?? string.Empty).Trim(),
                    Mobilnummer = (_mobilEntry.Text ?? string.Empty).Trim(),
                    Strasse = (_strasseEntry.Text ?? string.Empty).Trim(),
                    PLZ = (_plzEntry.Text ?? string.Empty).Trim(),
                    Ort = (_ortEntry.Text ?? string.Empty).Trim(),
                    Bemerkungen = (_bemerkungenEditor.Text ?? string.Empty).Trim(),
                    WhatsappEinwilligung = _whatsappSwitch.IsToggled,
                    EmailRechnungEinwilligung = _rechnungMailSwitch.IsToggled,
                    EmailInfoEinwilligung = _infoMailSwitch.IsToggled,
                    ArbeitsstundenAltersregelTyp = selectedArbeitsstundenAltersregelTyp ?? string.Empty,
                    Geburtsdatum = _geburtsdatumEnabledSwitch.IsToggled ? _geburtsdatumPicker.Date : null,
                    MitgliedSeit = _mitgliedSeitEnabledSwitch.IsToggled ? _mitgliedSeitPicker.Date : null,
                    MitgliedEnde = _mitgliedEndeEnabledSwitch.IsToggled ? _mitgliedEndePicker.Date : null,
                    Aktiv = true,
                    IstHauptmitglied = true,
                    Role = UserRoles.User
                });

                if (created == null)
                {
                    _statusLabel.Text = "Mitglied konnte nicht angelegt werden.";
                    return;
                }

                _memberSearchRefreshState.RequestReload();
                ApplyCreatedMemberContext(created);

                var createMitgliedsantrag = await DisplayAlertAsync(
                    "Mitgliedsantrag",
                    "Mitglied angelegt. Mitgliedsantrag erstellen?",
                    "Ja",
                    "Nein");

                if (createMitgliedsantrag)
                    await CreateMitgliedsantragAsync(created.Id, manageBusyState: false);

                var createNebenmitglied = await DisplayAlertAsync(
                    "Nebenmitglied anlegen",
                    "Mitglied angelegt. Soll jetzt ein Nebenmitglied angelegt werden?",
                    "Ja",
                    "Nein");

                if (createNebenmitglied)
                {
                    await Shell.Current.GoToAsync($"{nameof(NebenmitgliedPage)}?mode=create");
                    return;
                }

                _statusLabel.Text = "Mitglied angelegt. Stammdaten bleiben geöffnet; der Mitgliedsantrag kann hier weiter erzeugt werden.";
            }
            catch (Exception ex)
            {
                _statusLabel.Text = ex.Message;
            }
            finally
            {
                _isBusy = false;
            }

            return;
        }

        if (_memberRecord == null)
            return;

        var userId = _authService.CurrentUserId;
        if (string.IsNullOrWhiteSpace(userId))
        {
            await DisplayAlertAsync("Fehler", "Nicht angemeldet. Bitte erneut einloggen.", "OK");
            return;
        }

        var lockAcquired = false;
        _isBusy = true;
        try
        {
            lockAcquired = await _supabaseService.TryLockMitgliedAsync(_memberRecord.Id, userId);
            if (!lockAcquired)
            {
                await DisplayAlertAsync("Gesperrt", "Datensatz ist aktuell gesperrt. Bitte später erneut versuchen.", "OK");
                return;
            }

            var current = await _supabaseService.GetMitgliedByIdAsync(_memberRecord.Id);
            if (current == null)
            {
                await DisplayAlertAsync("Fehler", "Mitglied konnte nicht geladen werden.", "OK");
                return;
            }

            var dto = MapMember(current);
            dto.Vorname = (_vornameEntry.Text ?? string.Empty).Trim();
            dto.Nachname = (_nachnameEntry.Text ?? string.Empty).Trim();
            dto.Geburtsdatum = GetOptionalDate(_geburtsdatumEnabledSwitch, _geburtsdatumPicker);
            dto.Telefon = (_telefonEntry.Text ?? string.Empty).Trim();
            dto.Mobilnummer = (_mobilEntry.Text ?? string.Empty).Trim();
            dto.WhatsappEinwilligung = _whatsappSwitch.IsToggled;
            dto.EmailRechnungEinwilligung = _rechnungMailSwitch.IsToggled;
            dto.EmailInfoEinwilligung = _infoMailSwitch.IsToggled;
            dto.Strasse = (_strasseEntry.Text ?? string.Empty).Trim();
            dto.PLZ = (_plzEntry.Text ?? string.Empty).Trim();
            dto.Ort = (_ortEntry.Text ?? string.Empty).Trim();
            dto.Bemerkungen = (_bemerkungenEditor.Text ?? string.Empty).Trim();
            dto.MitgliedSeit = GetOptionalDate(_mitgliedSeitEnabledSwitch, _mitgliedSeitPicker);
            dto.MitgliedEnde = GetOptionalDate(_mitgliedEndeEnabledSwitch, _mitgliedEndePicker);
            dto.ArbeitsstundenAltersregelTyp = requiresArbeitsstundenAltersregel
                ? selectedArbeitsstundenAltersregelTyp ?? string.Empty
                : current.ArbeitsstundenAltersregelTyp;
            dto.Role = current.Role ?? dto.Role;
            dto.Email = _hasLinkedAppUser
                ? current.Email ?? dto.Email
                : (_emailEntry.Text ?? string.Empty).Trim();

            var ok = await _supabaseService.UpdateMitgliedAsync(dto, userId);
            if (!ok)
            {
                await DisplayAlertAsync("Fehler", "Stammdaten konnten nicht gespeichert werden.", "OK");
                return;
            }

            _memberContextState.SetSelectedMember(dto);
            _statusLabel.Text = "Stammdaten gespeichert.";
            await DisplayAlertAsync("OK", "Stammdaten gespeichert.", "OK");
            await LoadAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Fehler", ex.Message, "OK");
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

    private void ApplyCreatedMemberContext(MitgliedRecord created)
    {
        _isCreateMode = false;
        _memberRecord = created;
        _hasLinkedAppUser = created.AuthUserId.HasValue;

        var memberDto = MapMember(created);
        _memberContextState.SetSelectedMember(memberDto);

        _headlineLabel.Text = FormatMemberDisplayName(created);

        _nachnameEntry.Text = memberDto.Nachname;
        _vornameEntry.Text = memberDto.Vorname;
        _emailEntry.Text = memberDto.Email;
        _rolleLabel.Text = FormatValue(memberDto.Role);
        _arbeitsstundenAltersregelTypPicker.SelectedItem = MemberDTO.HauptmitgliedArbeitsstundenAltersregelTypOptions.Contains(memberDto.ArbeitsstundenAltersregelTyp, StringComparer.Ordinal)
            ? memberDto.ArbeitsstundenAltersregelTyp
            : null;
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

        _saveButton.Text = "Speichern";
        UpdateArbeitsstundenAltersregelVisibility(memberDto);
        UpdateAdminActions(memberDto);
    }

    private static AppUserDTO CreateInviteUser(MitgliedRecord member)
    {
        var displayName = string.Join(' ', new[] { member.Vorname, member.Name }
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!.Trim()));

        return new AppUserDTO
        {
            MitgliedId = member.Id,
            DisplayName = FormatMemberDisplayName(member),
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
            Mitgliedsnummer = rec.Mitgliedsnummer,
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
            EmailRechnungEinwilligung = rec.EmailRechnungEinwilligung,
            EmailInfoEinwilligung = rec.EmailInfoEinwilligung,
            ArbeitsstundenAltersregelTyp = rec.ArbeitsstundenAltersregelTyp,
            MitgliedSeit = rec.MitgliedSeit,
            MitgliedEnde = rec.MitgliedEnde,
            Role = rec.Role ?? string.Empty,
            IstHauptmitglied = rec.HauptmitgliedId == null
        };
    }

    private void UpdateArbeitsstundenAltersregelVisibility(MemberDTO? member)
    {
        _arbeitsstundenAltersregelTypField.IsVisible = _isCreateMode || member?.IstHauptmitglied == true;
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
        => toggle.IsToggled ? picker.Date!.Value.Date : null;

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

    private static View CreatePickerField(string title, Picker picker)
    {
        return new VerticalStackLayout
        {
            Spacing = 4,
            Children =
            {
                new Label { Text = title, FontAttributes = FontAttributes.Bold, FontSize = 12, TextColor = Colors.Gray },
                picker
            }
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
