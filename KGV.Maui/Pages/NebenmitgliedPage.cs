using KGV.Core.Interfaces;
using KGV.Core.Models;
using KGV.Maui.State;
using System.Linq;
using System.Text.RegularExpressions;

namespace KGV.Maui.Pages;

public sealed class NebenmitgliedPage : ContentPage
{
    private static readonly Regex PlzRegex = new("^\\d{5}$", RegexOptions.Compiled);

    private readonly ISupabaseService _supabaseService;
    private readonly UserContextState _state;

    private MitgliedRecord? _neben;
    private bool _loaded;

    private readonly Label _nameLabel;
    private readonly Entry _telefonEntry;
    private readonly Entry _handyEntry;
    private readonly Entry _adresseEntry;
    private readonly Entry _plzEntry;
    private readonly Entry _ortEntry;
    private readonly Button _saveButton;

    public NebenmitgliedPage(ISupabaseService supabaseService, UserContextState state)
    {
        _supabaseService = supabaseService;
        _state = state;

        Title = "Nebenmitglied";

        _nameLabel = new Label { FontSize = 22, FontAttributes = FontAttributes.Bold };

        _telefonEntry = new Entry { Placeholder = "Telefon" };
        _handyEntry = new Entry { Placeholder = "Handy" };
        _adresseEntry = new Entry { Placeholder = "Adresse (Pflicht)" };
        _plzEntry = new Entry { Placeholder = "PLZ (Pflicht)", Keyboard = Keyboard.Numeric };
        _ortEntry = new Entry { Placeholder = "Ort (Pflicht)" };

        _saveButton = new Button { Text = "Speichern" };
        _saveButton.Clicked += OnSaveClicked;

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = 24,
                Spacing = 12,
                Children =
                {
                    _nameLabel,
                    new Label { Text = "Kontakt/Adresse (nur diese Felder sind editierbar)", FontAttributes = FontAttributes.Italic },
                    _telefonEntry,
                    _handyEntry,
                    _adresseEntry,
                    _plzEntry,
                    _ortEntry,
                    _saveButton
                }
            }
        };

        Appearing += OnAppearing;
    }

    private async void OnAppearing(object? sender, EventArgs e)
    {
        if (_loaded) return;
        _loaded = true;
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        if (_state.CurrentMitgliedId == null || _state.CurrentMitgliedId.Value > int.MaxValue)
        {
            await DisplayAlert("Fehler", "Hauptmitglied nicht gesetzt.", "OK");
            return;
        }

        var mainId = (int)_state.CurrentMitgliedId.Value;
        var rec = await _supabaseService.GetNebenmitgliedByHauptmitgliedIdAsync(mainId);
        if (rec == null)
        {
            await DisplayAlert("Hinweis", "Kein Nebenmitglied vorhanden.", "OK");
            _state.CurrentNebenMitgliedId = null;
            return;
        }

        _neben = rec;
        _state.CurrentNebenMitgliedId = rec.Id;

        _nameLabel.Text = $"{rec.Vorname} {rec.Name}".Trim();

        _telefonEntry.Text = rec.Telefon ?? string.Empty;
        _handyEntry.Text = rec.Handy ?? string.Empty;
        _adresseEntry.Text = rec.Adresse ?? string.Empty;
        _plzEntry.Text = rec.Plz ?? string.Empty;
        _ortEntry.Text = rec.Ort ?? string.Empty;
    }

    private async void OnSaveClicked(object? sender, EventArgs e)
    {
        if (_neben == null)
        {
            await DisplayAlert("Fehler", "Nebenmitglied ist nicht geladen.", "OK");
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

        _saveButton.IsEnabled = false;
        try
        {
            var ok = await _supabaseService.UpdateOwnContactAsync(_neben.Id, EmptyToNull(telefon), EmptyToNull(handy), adresse, plz, ort);
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
            _saveButton.IsEnabled = true;
        }
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
        if (string.IsNullOrEmpty(value)) return true;

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
}
