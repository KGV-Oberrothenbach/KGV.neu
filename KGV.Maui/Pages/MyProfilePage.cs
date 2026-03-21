using KGV.Core.Interfaces;
using KGV.Core.Models;
using KGV.Maui.State;
using System.Linq;
using System.Text.RegularExpressions;

namespace KGV.Maui.Pages;

public sealed class MyProfilePage : ContentPage
{
    private static readonly Regex PlzRegex = new("^\\d{5}$", RegexOptions.Compiled);

    private readonly ISupabaseService _supabaseService;
    private readonly IAuthService _authService;
    private readonly UserContextState _state;

    private MitgliedRecord? _member;
    private bool _loaded;

    private readonly Label _nameLabel;
    private readonly Label _emailLabel;

    private readonly Entry _telefonEntry;
    private readonly Entry _handyEntry;
    private readonly Entry _adresseEntry;
    private readonly Entry _plzEntry;
    private readonly Entry _ortEntry;

    private readonly Label _statusLabel;
    private readonly Button _changeEmailButton;
    private readonly Button _saveButton;
    private readonly Button _checkAddressButton;

    public MyProfilePage(ISupabaseService supabaseService, IAuthService authService, UserContextState state)
    {
        _supabaseService = supabaseService;
        _authService = authService;
        _state = state;

        Title = "Meine Stammdaten";

        _nameLabel = new Label { FontSize = 22, FontAttributes = FontAttributes.Bold };
        _emailLabel = new Label();

        _telefonEntry = new Entry { Placeholder = "Telefon" };
        _handyEntry = new Entry { Placeholder = "Handy" };

        _adresseEntry = new Entry { Placeholder = "Adresse (Pflicht)" };
        _plzEntry = new Entry { Placeholder = "PLZ (Pflicht)", Keyboard = Keyboard.Numeric };
        _ortEntry = new Entry { Placeholder = "Ort (Pflicht)" };

        _statusLabel = new Label { TextColor = Colors.Red };

        _changeEmailButton = new Button { Text = "Mailadresse ändern" };
        _changeEmailButton.Clicked += OnChangeEmailClicked;

        _checkAddressButton = new Button { Text = "Adresse prüfen" };
        _checkAddressButton.Clicked += OnCheckAddressClicked;

        if (Application.Current?.Resources != null && Application.Current.Resources.TryGetValue("AccentButton", out var accentStyle) && accentStyle is Style s1)
            _checkAddressButton.Style = s1;

        _saveButton = new Button { Text = "Speichern" };
        _saveButton.Clicked += OnSaveClicked;

        object? cardStyleObj = null;
        if (Application.Current?.Resources != null)
            Application.Current.Resources.TryGetValue("Card", out cardStyleObj);
        var cardStyle = cardStyleObj as Style;

        object? entryBorderStyleObj = null;
        if (Application.Current?.Resources != null)
            Application.Current.Resources.TryGetValue("EntryBorder", out entryBorderStyleObj);
        var entryBorderStyle = entryBorderStyleObj as Style;

        object? readOnlyStyleObj = null;
        if (Application.Current?.Resources != null)
            Application.Current.Resources.TryGetValue("ReadOnlyField", out readOnlyStyleObj);
        var readOnlyStyle = readOnlyStyleObj as Style;

        Border WrapEntry(Entry entry)
            => entryBorderStyle != null
                ? new Border { Style = entryBorderStyle, Content = entry }
                : new Border { Content = entry };

        Border WrapCard(View content)
            => cardStyle != null
                ? new Border { Style = cardStyle, Content = content }
                : new Border { Content = content };

        var header = new VerticalStackLayout
        {
            Spacing = 8,
            Children =
            {
                _nameLabel,
                readOnlyStyle != null ? new Border { Style = readOnlyStyle, Content = _emailLabel } : _emailLabel,
                _statusLabel
            }
        };

        var kontakt = new VerticalStackLayout
        {
            Spacing = 10,
            Children =
            {
                new Label { Text = "Kontakt", FontAttributes = FontAttributes.Bold },
                WrapEntry(_telefonEntry),
                WrapEntry(_handyEntry)
            }
        };

        var adresse = new VerticalStackLayout
        {
            Spacing = 10,
            Children =
            {
                new Label { Text = "Adresse", FontAttributes = FontAttributes.Bold },
                WrapEntry(_adresseEntry),
                WrapEntry(_plzEntry),
                WrapEntry(_ortEntry)
            }
        };

        var actions = new HorizontalStackLayout
        {
            Spacing = 12,
            Children = { _checkAddressButton, _changeEmailButton, _saveButton }
        };

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = 24,
                Spacing = 14,
                Children =
                {
                    WrapCard(header),
                    WrapCard(kontakt),
                    WrapCard(adresse),
                    actions
                }
            }
        };

        Appearing += OnAppearing;
    }

    private async void OnChangeEmailClicked(object? sender, EventArgs e)
    {
        var currentEmail = _emailLabel.Text?.Trim() ?? string.Empty;
        var newEmail = await DisplayPromptAsync("Mailadresse ändern", "Neue Mailadresse eingeben", initialValue: currentEmail, keyboard: Keyboard.Email);
        if (string.IsNullOrWhiteSpace(newEmail))
            return;

        newEmail = newEmail.Trim();
        if (string.Equals(newEmail, currentEmail, StringComparison.OrdinalIgnoreCase))
        {
            await DisplayAlert("Hinweis", "Bitte eine andere Mailadresse eingeben.", "OK");
            return;
        }

        SetBusy(true);
        try
        {
            var requested = await _authService.RequestEmailChangeAsync(newEmail);
            if (!requested)
            {
                await DisplayAlert("Fehler", "OTP-Code für die Mailadressänderung konnte nicht angefordert werden.", "OK");
                return;
            }

            var code = await DisplayPromptAsync("OTP-Code", "Code aus der neuen Mailadresse eingeben", keyboard: Keyboard.Numeric);
            if (string.IsNullOrWhiteSpace(code))
            {
                await DisplayAlert("Hinweis", "Die Mailadressänderung bleibt unbestätigt, bis der OTP-Code eingegeben wurde.", "OK");
                return;
            }

            var verified = await _authService.VerifyEmailChangeOtpAsync(newEmail, code.Trim());
            if (!verified)
            {
                await DisplayAlert("Fehler", "OTP-Code konnte nicht bestätigt werden.", "OK");
                return;
            }

            await DisplayAlert("OK", "Mailadresse erfolgreich geändert.", "OK");
            _loaded = false;
            await LoadAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Fehler", ex.Message, "OK");
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async void OnAppearing(object? sender, EventArgs e)
    {
        if (_loaded) return;
        _loaded = true;
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        _statusLabel.Text = string.Empty;

        if (_state.CurrentUserId == null)
        {
            _statusLabel.Text = "Nicht angemeldet.";
            return;
        }

        var rec = await _supabaseService.GetMitgliedByAuthUserIdAsync(_state.CurrentUserId.Value);
        if (rec == null)
        {
            _statusLabel.Text = "Mitglied nicht gefunden.";
            return;
        }

        _member = rec;
        _state.CurrentMitgliedId = rec.Id;

        _nameLabel.Text = $"{rec.Vorname} {rec.Name}".Trim();
        _emailLabel.Text = rec.Email ?? string.Empty;

        _telefonEntry.Text = rec.Telefon ?? string.Empty;
        _handyEntry.Text = rec.Handy ?? string.Empty;

        _adresseEntry.Text = rec.Adresse ?? string.Empty;
        _plzEntry.Text = rec.Plz ?? string.Empty;
        _ortEntry.Text = rec.Ort ?? string.Empty;
    }

    private async void OnSaveClicked(object? sender, EventArgs e)
    {
        if (_member == null)
        {
            await DisplayAlert("Fehler", "Mitglied ist nicht geladen.", "OK");
            return;
        }

        var telefon = (_telefonEntry.Text ?? string.Empty).Trim();
        var handy = (_handyEntry.Text ?? string.Empty).Trim();
        var adresse = (_adresseEntry.Text ?? string.Empty).Trim();
        var plz = (_plzEntry.Text ?? string.Empty).Trim();
        var ort = (_ortEntry.Text ?? string.Empty).Trim();

        var error = Validate(adresse, plz, ort, telefon, handy);
        if (!string.IsNullOrEmpty(error))
        {
            await DisplayAlert("Ungültige Eingabe", error, "OK");
            return;
        }

        SetBusy(true);
        try
        {
            var ok = await _supabaseService.UpdateOwnContactAsync(_member.Id, EmptyToNull(telefon), EmptyToNull(handy), adresse, plz, ort);
            if (!ok)
            {
                await DisplayAlert("Fehler", "Speichern fehlgeschlagen.", "OK");
                return;
            }

            await DisplayAlert("OK", "Gespeichert.", "OK");
            await LoadAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Fehler", ex.Message, "OK");
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async void OnCheckAddressClicked(object? sender, EventArgs e)
    {
        // Stub: funktioniert auch ohne API-Key
        var adresse = (_adresseEntry.Text ?? string.Empty).Trim();
        var plz = (_plzEntry.Text ?? string.Empty).Trim();
        var ort = (_ortEntry.Text ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(adresse) || string.IsNullOrWhiteSpace(plz) || string.IsNullOrWhiteSpace(ort))
        {
            await DisplayAlert("Hinweis", "Bitte Adresse, PLZ und Ort ausfüllen.", "OK");
            return;
        }

        var okPlz = PlzRegex.IsMatch(plz);
        await DisplayAlert("Adresse prüfen", okPlz ? "Format wirkt plausibel." : "PLZ ist ungültig.", "OK");
    }

    private static string? Validate(string adresse, string plz, string ort, string telefon, string handy)
    {
        if (string.IsNullOrWhiteSpace(adresse)) return "Adresse ist Pflicht.";
        if (string.IsNullOrWhiteSpace(plz)) return "PLZ ist Pflicht.";
        if (!PlzRegex.IsMatch(plz)) return "PLZ muss 5-stellig sein (Regex ^\\d{5}$).";
        if (string.IsNullOrWhiteSpace(ort)) return "Ort ist Pflicht.";

        if (!IsValidPhone(telefon)) return "Telefon ist nicht plausibel.";
        if (!IsValidPhone(handy)) return "Handy ist nicht plausibel.";

        return null;
    }

    private static bool IsValidPhone(string value)
    {
        value = (value ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(value)) return true; // optional

        // erlaubte Zeichen: Ziffern + Leerzeichen + + / - ( )
        foreach (var ch in value)
        {
            if (char.IsDigit(ch)) continue;
            if (ch is ' ' or '+' or '/' or '-' or '(' or ')') continue;
            return false;
        }

        var digits = new string(value.Where(char.IsDigit).ToArray());
        return digits.Length >= 6;
    }

    private static string? EmptyToNull(string s) => string.IsNullOrWhiteSpace(s) ? null : s;

    private void SetBusy(bool busy)
    {
        _saveButton.IsEnabled = !busy;
        _checkAddressButton.IsEnabled = !busy;
        _changeEmailButton.IsEnabled = !busy;
    }
}
