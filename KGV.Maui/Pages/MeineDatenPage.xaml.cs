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
    private readonly Label _memberInfoLabel;
    private readonly Label _contactLabel;
    private readonly Label _addressLabel;
    private readonly Label _memberSinceLabel;
    private readonly Label _statusLabel;
    private readonly Label _gardensEmptyLabel;
    private readonly Label _adminHintLabel;
    private readonly Picker _rolePicker;
    private readonly Button _saveRoleButton;
    private readonly Button _documentsButton;
    private readonly Button _userManagementButton;

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

        Title = "Mitgliedskontext";

        _headlineLabel = new Label { FontSize = 24, FontAttributes = FontAttributes.Bold };
        _memberInfoLabel = new Label { LineBreakMode = LineBreakMode.WordWrap };
        _contactLabel = new Label { LineBreakMode = LineBreakMode.WordWrap };
        _addressLabel = new Label { LineBreakMode = LineBreakMode.WordWrap };
        _memberSinceLabel = new Label { LineBreakMode = LineBreakMode.WordWrap };
        _statusLabel = new Label { TextColor = Colors.DarkRed, LineBreakMode = LineBreakMode.WordWrap };
        _gardensEmptyLabel = new Label { TextColor = Colors.Gray, Text = "Keine aktiven oder historischen Garten-Zuordnungen geladen." };
        _adminHintLabel = new Label { TextColor = Colors.Gray, LineBreakMode = LineBreakMode.WordWrap };

        _rolePicker = new Picker { Title = "Rolle" };
        foreach (var role in UserRoles.AssignableRoles)
            _rolePicker.Items.Add(role);

        _saveRoleButton = new Button { Text = "Rolle speichern" };
        _saveRoleButton.Clicked += OnSaveRoleClicked;

        _documentsButton = new Button { Text = "Mitgliedsdokumente öffnen" };
        _documentsButton.Clicked += async (_, _) => await Shell.Current.GoToAsync(nameof(DokumentePage));

        _userManagementButton = new Button { Text = "Benutzerverwaltung" };
        _userManagementButton.Clicked += async (_, _) => await Shell.Current.GoToAsync(nameof(UserManagementPage));

        var refreshButton = new Button { Text = "Aktualisieren" };
        refreshButton.Clicked += async (_, _) => await LoadAsync();

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
            Text = "Tippen öffnet den Gartenkontext mit Strom, Wasser und Garten-Dokumenten.",
            TextColor = Colors.Gray,
            LineBreakMode = LineBreakMode.WordWrap
        };

        var adminMenuSection = new VerticalStackLayout
        {
            Spacing = 8,
            Children =
            {
                new Label { Text = "Admin-Menü", FontAttributes = FontAttributes.Bold, FontSize = 18 },
                _adminHintLabel,
                _rolePicker,
                _userManagementButton,
                _saveRoleButton
            }
        };

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
                    CreateSection("Mitglied", _memberInfoLabel, _contactLabel, _addressLabel, _memberSinceLabel),
                    CreateSection("Gärten / Parzellen", _documentsButton, gardenHintLabel, gardensView, _gardensEmptyLabel),
                    adminMenuSection,
                    refreshButton
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
                _memberInfoLabel.Text = "Bitte zuerst in der Mitgliedersuche ein Mitglied auswählen.";
                _contactLabel.Text = string.Empty;
                _addressLabel.Text = string.Empty;
                _memberSinceLabel.Text = string.Empty;
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
            _memberInfoLabel.Text = $"E-Mail: {FormatValue(contextMember.Email)}\nRolle: {FormatRole(contextMember.Role)}";
            _contactLabel.Text = $"Telefon: {FormatValue(contextMember.Telefon)}\nHandy: {FormatValue(contextMember.Mobilnummer)}";
            _addressLabel.Text = $"Adresse: {BuildAddressText(contextMember)}";
            _memberSinceLabel.Text = $"Mitglied seit: {FormatDate(contextMember.MitgliedSeit)}\nMitglied Ende: {FormatDate(contextMember.MitgliedEnde)}";

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

    private void UpdateAdminMenu(MemberDTO? member)
    {
        var currentRole = _userContextState.CurrentUserContext?.Role;
        var hasAdminMenu = currentRole is UserRole.Admin or UserRole.Vorstand;
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

    private static View CreateSection(string title, params View[] children)
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
            Role = rec.Role ?? string.Empty
        };
    }

    private sealed record GartenAssignmentItem(int ParzelleId, string Title, string Subtitle);
}
