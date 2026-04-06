using KGV.Core.Interfaces;
using KGV.Core.Models;
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

public sealed class NebenmitgliedPage : ContentPage, IQueryAttributable
{
    private static readonly Regex PlzRegex = new("^\\d{5}$", RegexOptions.Compiled);

    private readonly ISupabaseService _supabaseService;
    private readonly UserContextState _state;
    private readonly MemberContextState _memberContextState;

    private MitgliedRecord? _hauptmitglied;
    private MitgliedRecord? _neben;
    private bool _isLoading;
    private bool _isCreateMode;

    private readonly Label _nameLabel;
    private readonly Label _hintLabel;
    private readonly Entry _vornameEntry;
    private readonly Entry _nachnameEntry;
    private readonly CheckBox _adresseUebernehmenCheckBox;
    private readonly VerticalStackLayout _createSection;
    private readonly Entry _telefonEntry;
    private readonly Entry _handyEntry;
    private readonly Entry _adresseEntry;
    private readonly Entry _plzEntry;
    private readonly Entry _ortEntry;
    private readonly Button _saveButton;

    public NebenmitgliedPage(ISupabaseService supabaseService, UserContextState state, MemberContextState memberContextState)
    {
        _supabaseService = supabaseService;
        _state = state;
        _memberContextState = memberContextState;

        Title = "Nebenmitglied";

        _nameLabel = new Label { FontSize = 22, FontAttributes = FontAttributes.Bold };
        _hintLabel = new Label { TextColor = Colors.Gray, LineBreakMode = LineBreakMode.WordWrap };

        _vornameEntry = new Entry { Placeholder = "Vorname" };
        _nachnameEntry = new Entry { Placeholder = "Nachname" };

        _adresseUebernehmenCheckBox = new CheckBox { IsChecked = true };

        _createSection = new VerticalStackLayout
        {
            Spacing = 8,
            IsVisible = false,
            Children =
            {
                new Label { Text = "Neues Nebenmitglied", FontAttributes = FontAttributes.Bold },
                _vornameEntry,
                _nachnameEntry,
                new HorizontalStackLayout
                {
                    Spacing = 8,
                    Children =
                    {
                        _adresseUebernehmenCheckBox,
                        new Label { Text = "Adresse des Hauptmitglieds übernehmen", VerticalTextAlignment = TextAlignment.Center }
                    }
                }
            }
        };

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
                    _hintLabel,
                    _createSection,
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

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        _isCreateMode = query.TryGetValue("mode", out var mode)
            && string.Equals(mode?.ToString(), "create", StringComparison.OrdinalIgnoreCase);
    }

    private async void OnAppearing(object? sender, EventArgs e)
    {
        if (_isLoading) return;
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        if (_isLoading)
            return;

        _isLoading = true;
        try
        {
            var selectedMember = _memberContextState.SelectedMember;
            if (selectedMember != null && !selectedMember.IstHauptmitglied)
            {
                await DisplayAlert("Hinweis", "Das ausgewählte Mitglied ist einem Hauptmitglied zugeordnet. Ein eigener Nebenmitgliedspfad ist hier nicht verfügbar.", "OK");
                _state.CurrentNebenMitgliedId = null;
                return;
            }

            int? mainId = null;
            if (selectedMember?.Id is > 0)
                mainId = selectedMember.Id;
            else if (_state.CurrentMitgliedId is > 0 and <= int.MaxValue)
                mainId = (int)_state.CurrentMitgliedId.Value;

            if (!mainId.HasValue)
            {
                await DisplayAlert("Fehler", "Hauptmitglied nicht gesetzt.", "OK");
                return;
            }

            _hauptmitglied = await _supabaseService.GetMitgliedByIdAsync(mainId.Value);
            if (_hauptmitglied == null)
            {
                await DisplayAlert("Fehler", "Hauptmitglied konnte nicht geladen werden.", "OK");
                return;
            }

            var rec = await _supabaseService.GetNebenmitgliedByHauptmitgliedIdAsync(mainId.Value);
            if (rec == null)
            {
                if (_isCreateMode)
                {
                    ConfigureCreateMode(_hauptmitglied);
                    _state.CurrentNebenMitgliedId = null;
                    _neben = null;
                    return;
                }

                await DisplayAlert("Hinweis", "Kein Nebenmitglied vorhanden.", "OK");
                _state.CurrentNebenMitgliedId = null;
                _neben = null;
                _nameLabel.Text = "Kein Nebenmitglied vorhanden";
                _telefonEntry.Text = string.Empty;
                _handyEntry.Text = string.Empty;
                _adresseEntry.Text = string.Empty;
                _plzEntry.Text = string.Empty;
                _ortEntry.Text = string.Empty;
                return;
            }

            _neben = rec;
            _state.CurrentNebenMitgliedId = rec.Id;
            _createSection.IsVisible = false;

            _nameLabel.Text = $"{rec.Vorname} {rec.Name}".Trim();
            _hintLabel.Text = "Kontakt/Adresse des bestehenden Nebenmitglieds.";

            _telefonEntry.Text = rec.Telefon ?? string.Empty;
            _handyEntry.Text = rec.Handy ?? string.Empty;
            _adresseEntry.Text = rec.Adresse ?? string.Empty;
            _plzEntry.Text = rec.Plz ?? string.Empty;
            _ortEntry.Text = rec.Ort ?? string.Empty;
        }
        finally
        {
            _isLoading = false;
        }
    }

    private async void OnSaveClicked(object? sender, EventArgs e)
    {
        if (_isCreateMode && _neben == null)
        {
            await CreateNebenmitgliedAsync();
            return;
        }

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

    private void ConfigureCreateMode(MitgliedRecord hauptmitglied)
    {
        _createSection.IsVisible = true;
        _nameLabel.Text = $"Nebenmitglied für {BuildDisplayName(hauptmitglied.Vorname, hauptmitglied.Name, hauptmitglied.Id)}";
        _hintLabel.Text = "Nach erfolgreicher Hauptmitglied-Neuanlage kann direkt ein Nebenmitglied angelegt werden. Die Adresse des Hauptmitglieds kann dabei als Vorbelegung übernommen werden.";
        _vornameEntry.Text = string.Empty;
        _nachnameEntry.Text = hauptmitglied.Name ?? string.Empty;
        _adresseUebernehmenCheckBox.IsChecked = true;
        _telefonEntry.Text = string.Empty;
        _handyEntry.Text = string.Empty;
        _adresseEntry.Text = hauptmitglied.Adresse ?? string.Empty;
        _plzEntry.Text = hauptmitglied.Plz ?? string.Empty;
        _ortEntry.Text = hauptmitglied.Ort ?? string.Empty;
    }

    private async Task CreateNebenmitgliedAsync()
    {
        if (_hauptmitglied == null)
        {
            await DisplayAlert("Fehler", "Hauptmitglied ist nicht geladen.", "OK");
            return;
        }

        var vorname = (_vornameEntry.Text ?? string.Empty).Trim();
        var nachname = (_nachnameEntry.Text ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(vorname) || string.IsNullOrWhiteSpace(nachname))
        {
            await DisplayAlert("Ungültige Eingabe", "Bitte Vorname und Nachname angeben.", "OK");
            return;
        }

        _saveButton.IsEnabled = false;
        try
        {
            var created = await _supabaseService.CreateNebenmitgliedAsync(new NebenmitgliedCreateDTO
            {
                HauptmitgliedId = _hauptmitglied.Id,
                Vorname = vorname,
                Nachname = nachname,
                AdresseUebernehmen = _adresseUebernehmenCheckBox.IsChecked
            });

            if (created == null)
            {
                await DisplayAlert("Fehler", "Nebenmitglied konnte nicht angelegt werden.", "OK");
                return;
            }

            _isCreateMode = false;
            _neben = created;
            _state.CurrentNebenMitgliedId = created.Id;
            await DisplayAlert("OK", "Nebenmitglied angelegt.", "OK");
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

    private static string BuildDisplayName(string? vorname, string? nachname, int fallbackId)
    {
        var displayName = $"{vorname} {nachname}".Trim();
        return string.IsNullOrWhiteSpace(displayName) ? $"Mitglied #{fallbackId}" : displayName;
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
