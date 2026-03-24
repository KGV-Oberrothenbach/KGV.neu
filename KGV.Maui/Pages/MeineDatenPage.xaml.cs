using KGV.Core.Interfaces;
using KGV.Core.Models;
using KGV.Core.Security;
using KGV.Maui.State;
using System.Collections.ObjectModel;
using System.Linq;

namespace KGV.Maui.Pages;

public class MeineDatenPage : ContentPage
{
    private readonly ISupabaseService _supabaseService;
    private readonly IAuthService _authService;
    private readonly UserContextState _userContextState;
    private readonly MemberContextState _memberContextState;
    private readonly ParzellenContextState _parzellenContextState;

    private readonly Label _headlineLabel;
    private readonly Label _statusLabel;
    private readonly Label _gardensEmptyLabel;
    private readonly Label _adminHintLabel;
    private readonly Label _vornameLabel;
    private readonly Label _nachnameLabel;
    private readonly Label _geburtsdatumLabel;
    private readonly Label _emailLabel;
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
    private readonly Border _adminSectionCard;
    private readonly Border _nebenmitgliedSectionCard;
    private readonly Border _wartungsvertragSectionCard;
    private readonly VerticalStackLayout _adminMenuSection;
    private readonly Picker _rolePicker;
    private readonly Button _editButton;
    private readonly Button _assignGardenButton;
    private readonly Button _saveRoleButton;
    private readonly Button _documentsButton;
    private readonly Button _userManagementButton;
    private readonly Button _nebenmitgliedButton;

    private readonly ObservableCollection<GartenAssignmentItem> _gardenAssignments = new();

    private bool _isBusy;

    public MeineDatenPage(
        ISupabaseService supabaseService,
        IAuthService authService,
        UserContextState userContextState,
        MemberContextState memberContextState,
        ParzellenContextState parzellenContextState)
    {
        _supabaseService = supabaseService;
        _authService = authService;
        _userContextState = userContextState;
        _memberContextState = memberContextState;
        _parzellenContextState = parzellenContextState;

        Title = "Stammdaten";

        _headlineLabel = new Label { FontSize = 24, FontAttributes = FontAttributes.Bold };
        _statusLabel = new Label { TextColor = Colors.DarkRed, LineBreakMode = LineBreakMode.WordWrap };
        _gardensEmptyLabel = new Label { TextColor = Colors.Gray, Text = "Keine aktiven oder historischen Garten-Zuordnungen geladen." };
        _adminHintLabel = new Label { TextColor = Colors.Gray, LineBreakMode = LineBreakMode.WordWrap };
        _vornameLabel = CreateValueLabel();
        _nachnameLabel = CreateValueLabel();
        _geburtsdatumLabel = CreateValueLabel();
        _emailLabel = CreateValueLabel();
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

        _rolePicker = new Picker { Title = "Rolle" };
        foreach (var role in UserRoles.AssignableRoles)
            _rolePicker.Items.Add(role);

        _editButton = new Button { Text = "Bearbeiten" };
        _editButton.Clicked += OnEditClicked;

        _assignGardenButton = new Button { Text = "Parzelle zuordnen" };
        _assignGardenButton.Clicked += OnAssignGardenClicked;

        _saveRoleButton = new Button { Text = "Rolle speichern" };
        _saveRoleButton.Clicked += OnSaveRoleClicked;

        _documentsButton = new Button { Text = "Mitgliedsdokumente" };
        _documentsButton.Clicked += async (_, _) => await Shell.Current.GoToAsync(nameof(DokumentePage));

        _nebenmitgliedButton = new Button { Text = "Nebenmitglied öffnen", IsVisible = false };
        _nebenmitgliedButton.Clicked += async (_, _) => await Shell.Current.GoToAsync(nameof(NebenmitgliedPage));

        _userManagementButton = new Button { Text = "Benutzerverwaltung" };
        _userManagementButton.Clicked += async (_, _) => await Shell.Current.GoToAsync(nameof(UserManagementPage));

        var gardensView = new CollectionView
        {
            SelectionMode = SelectionMode.Single,
            HeightRequest = 220,
            ItemsSource = _gardenAssignments,
            ItemTemplate = new DataTemplate(() =>
            {
                var title = new Label { FontAttributes = FontAttributes.Bold };
                title.SetBinding(Label.TextProperty, nameof(GartenAssignmentItem.Title));

                var subtitle = new Label { FontSize = 12, TextColor = Colors.Gray, LineBreakMode = LineBreakMode.WordWrap };
                subtitle.SetBinding(Label.TextProperty, nameof(GartenAssignmentItem.Subtitle));

                return new Border
                {
                    Padding = 12,
                    Margin = new Thickness(0, 0, 0, 8),
                    Stroke = Colors.LightGray,
                    Content = new VerticalStackLayout
                    {
                        Spacing = 4,
                        Children = { title, subtitle }
                    }
                };
            })
        };

        gardensView.SelectionChanged += async (_, e) =>
        {
            var selected = e.CurrentSelection?.FirstOrDefault() as GartenAssignmentItem;
            gardensView.SelectedItem = null;
            if (selected == null)
                return;

            _parzellenContextState.SetMemberContext(selected.ParzelleId, selected.Title);
            await Shell.Current.GoToAsync("//parzellen");
        };

        var gardenHintLabel = new Label
        {
            Text = "Tippen öffnet den Gartenkontext. Operatives Ablesen und Zählerwechsel bleiben im eigenen Ablesen-Bereich.",
            TextColor = Colors.Gray,
            LineBreakMode = LineBreakMode.WordWrap
        };

        var editHintLabel = new Label
        {
            Text = "Bearbeiten öffnet für eigene Stammdaten den vorhandenen mobilen Profilpfad. Für fremde Mitglieder bleibt der Kontext aktuell bewusst read-only, solange kein belastbarer mobiler Volleditor vorliegt.",
            TextColor = Colors.Gray,
            LineBreakMode = LineBreakMode.WordWrap
        };

        var topActionSection = new HorizontalStackLayout
        {
            Spacing = 8,
            Children = { _editButton }
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
        _nebenmitgliedSectionCard = CreateSection("Mitgliedskontext", _nebenmitgliedButton, _nebenmitgliedHintLabel);
        _adminSectionCard = CreateSection("Verwaltung", _adminMenuSection);

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
                    topActionSection,
                    editHintLabel,
                    CreateSection("Grunddaten",
                        CreateValueField("Nachname", _nachnameLabel),
                        CreateValueField("Vorname", _vornameLabel),
                        CreateValueField("Geburtsdatum", _geburtsdatumLabel)),
                    CreateSection("Kontakt",
                        CreateValueField("E-Mail", _emailLabel),
                        CreateValueField("Telefon", _telefonLabel),
                        CreateValueField("Mobilnummer", _mobilLabel),
                        CreateValueField("WhatsApp", _whatsappLabel)),
                    CreateSection("Adresse",
                        CreateValueField("Straße / Hausnummer", _strasseLabel),
                        CreateValueField("PLZ", _plzLabel),
                        CreateValueField("Ort", _ortLabel)),
                    CreateSection("Mitgliedschaft",
                        CreateValueField("Rolle", _rolleLabel),
                        CreateValueField("Mitglied seit", _mitgliedSeitLabel),
                        CreateValueField("Mitglied Ende", _mitgliedEndeLabel),
                        CreateValueField("Aktiv", _aktivLabel),
                        CreateValueField("Bemerkungen", _bemerkungenLabel)),
                    _wartungsvertragSectionCard,
                    _nebenmitgliedSectionCard,
                    CreateSection("Gärten", _assignGardenButton, gardenHintLabel, gardensView, _gardensEmptyLabel),
                    _adminSectionCard,
                    _documentsButton
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
                SetMemberFieldsEmpty();
                SetWartungsvertragFieldsEmpty();
                UpdateNebenmitgliedSection(null, false);
                _nachnameLabel.Text = "Bitte zuerst in der Mitgliedersuche ein Mitglied auswählen.";
                _gardenAssignments.Clear();
                UpdateAdminMenu(null);
                _gardensEmptyLabel.IsVisible = true;
                return;
            }

            var member = await _supabaseService.GetMitgliedByIdAsync(selectedMember.Id);
            if (member == null)
            {
                _statusLabel.Text = "Das ausgewählte Mitglied konnte nicht geladen werden.";
                return;
            }

            var contextMember = MapMember(member);
            _memberContextState.SetSelectedMember(contextMember);

            _headlineLabel.Text = string.IsNullOrWhiteSpace(contextMember.DisplayName)
                ? $"Mitglied #{contextMember.Id}"
                : contextMember.DisplayName;
            _nachnameLabel.Text = FormatValue(contextMember.Nachname);
            _vornameLabel.Text = FormatValue(contextMember.Vorname);
            _geburtsdatumLabel.Text = FormatDate(contextMember.Geburtsdatum);
            _emailLabel.Text = FormatValue(contextMember.Email);
            _telefonLabel.Text = FormatValue(contextMember.Telefon);
            _mobilLabel.Text = FormatValue(contextMember.Mobilnummer);
            _whatsappLabel.Text = contextMember.WhatsappEinwilligung ? "Ja" : "Nein";
            _strasseLabel.Text = FormatValue(contextMember.Strasse);
            _plzLabel.Text = FormatValue(contextMember.PLZ);
            _ortLabel.Text = FormatValue(contextMember.Ort);
            _rolleLabel.Text = FormatRole(contextMember.Role);
            _mitgliedSeitLabel.Text = FormatDate(contextMember.MitgliedSeit);
            _mitgliedEndeLabel.Text = FormatDate(contextMember.MitgliedEnde);
            _aktivLabel.Text = contextMember.Aktiv ? "Ja" : "Nein";
            _bemerkungenLabel.Text = FormatValue(contextMember.Bemerkungen);

            await LoadWartungsvertragSummaryAsync(contextMember.Id);
            await UpdateNebenmitgliedSectionAsync(contextMember);
            await LoadGartenAssignmentsAsync(contextMember.Id);
            UpdateAdminMenu(contextMember);
        }
        catch (Exception ex)
        {
            _statusLabel.Text = ex.Message;
        }
        finally
        {
            _isBusy = false;
        }
    }

    private async void OnEditClicked(object? sender, EventArgs e)
    {
        var selectedMember = _memberContextState.SelectedMember;
        if (selectedMember?.Id is not > 0)
        {
            await DisplayAlert("Hinweis", "Bitte zuerst ein Mitglied auswählen.", "OK");
            return;
        }

        if (_userContextState.CurrentMitgliedId == selectedMember.Id)
        {
            await Shell.Current.GoToAsync(nameof(MyProfilePage));
            return;
        }

        await DisplayAlert(
            "Bearbeiten",
            "Ein eigener mobiler Volleditor für fremde Stammdaten ist im aktuellen Stand noch nicht vorhanden. Für eigene Stammdaten steht bereits der vorhandene Profilpfad zur Verfügung.",
            "OK");
    }

    private async void OnAssignGardenClicked(object? sender, EventArgs e)
    {
        var selectedMember = _memberContextState.SelectedMember;
        if (selectedMember?.Id is not > 0)
        {
            await DisplayAlert("Hinweis", "Bitte zuerst ein Mitglied auswählen.", "OK");
            return;
        }

        _parzellenContextState.Clear();
        await Shell.Current.GoToAsync("//parzellen");
    }

    private async Task LoadGartenAssignmentsAsync(int mitgliedId)
    {
        var parzellen = await _supabaseService.GetAllParzellenAsync();
        var belegungen = await _supabaseService.GetBelegungenForMitgliedAsync(mitgliedId);
        var parzellenById = (parzellen ?? new List<ParzelleRecord>())
            .Where(x => x.Id > 0)
            .ToDictionary(x => x.Id);

        _gardenAssignments.Clear();
        foreach (var belegung in (belegungen ?? new List<ParzellenBelegungRecord>()).OrderByDescending(x => x.VonDatum ?? DateTime.MinValue))
        {
            parzellenById.TryGetValue(belegung.ParzelleId, out var parzelle);
            var gartenNr = string.IsNullOrWhiteSpace(parzelle?.GartenNr) ? belegung.ParzelleId.ToString() : parzelle!.GartenNr!;
            var anlage = string.IsNullOrWhiteSpace(parzelle?.Anlage) ? "-" : parzelle.Anlage;
            var bisText = belegung.BisDatum.HasValue ? belegung.BisDatum.Value.ToString("dd.MM.yyyy") : "aktiv";

            _gardenAssignments.Add(new GartenAssignmentItem(
                belegung.ParzelleId,
                $"Garten {gartenNr} ({anlage})",
                $"Von {FormatDate(belegung.VonDatum)} bis {bisText}"));
        }

        _gardensEmptyLabel.IsVisible = _gardenAssignments.Count == 0;
    }

    private async Task UpdateNebenmitgliedSectionAsync(MemberDTO member)
    {
        if (member.Id <= 0)
        {
            UpdateNebenmitgliedSection(null, false);
            return;
        }

        if (!member.IstHauptmitglied)
        {
            UpdateNebenmitgliedSection("Dieses Mitglied ist einem Hauptmitglied zugeordnet; ein eigener Nebenmitgliedspfad ist hier nicht relevant.", false);
            return;
        }

        var nebenmitglied = await _supabaseService.GetNebenmitgliedByHauptmitgliedIdAsync(member.Id);
        if (nebenmitglied == null)
        {
            UpdateNebenmitgliedSection("Für dieses Hauptmitglied ist aktuell kein Nebenmitglied hinterlegt.", false);
            return;
        }

        UpdateNebenmitgliedSection($"Vorhandenes Nebenmitglied: {BuildDisplayName(nebenmitglied.Vorname, nebenmitglied.Name)}", true);
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

    private void UpdateNebenmitgliedSection(string? hint, bool canOpen)
    {
        var hasHint = !string.IsNullOrWhiteSpace(hint);
        _nebenmitgliedButton.IsVisible = canOpen;
        _nebenmitgliedHintLabel.Text = hasHint ? hint : string.Empty;
        _nebenmitgliedHintLabel.IsVisible = hasHint;
        _nebenmitgliedSectionCard.IsVisible = canOpen || hasHint;
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
        _saveRoleButton.IsEnabled = _rolePicker.IsEnabled;
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

    private void SetMemberFieldsEmpty()
    {
        _vornameLabel.Text = string.Empty;
        _nachnameLabel.Text = string.Empty;
        _geburtsdatumLabel.Text = string.Empty;
        _emailLabel.Text = string.Empty;
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
    }

    private void SetWartungsvertragFieldsEmpty()
    {
        _pflichtstundenJahrLabel.Text = "-";
        _wartungsvertragLabel.Text = "-";
        _befreiungLabel.Text = "-";
        _regelgrundLabel.Text = "-";
        _wartungsvertragHintLabel.Text = string.Empty;
    }

    private static string BuildAddressText(MemberDTO member)
    {
        var parts = new[]
        {
            string.IsNullOrWhiteSpace(member.Strasse) ? null : member.Strasse,
            string.IsNullOrWhiteSpace(member.PLZ) && string.IsNullOrWhiteSpace(member.Ort) ? null : $"{member.PLZ} {member.Ort}".Trim()
        };

        return string.Join(", ", parts.Where(x => !string.IsNullOrWhiteSpace(x))) switch
        {
            { Length: > 0 } text => text,
            _ => "-"
        };
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

    private sealed record GartenAssignmentItem(int ParzelleId, string Title, string Subtitle);
}
