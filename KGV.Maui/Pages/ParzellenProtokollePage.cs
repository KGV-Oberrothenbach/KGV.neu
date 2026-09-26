using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Extensions.DependencyInjection;
using KGV.Core.Interfaces;
using KGV.Core.Models;
using KGV.Maui.State;

namespace KGV.Maui.Pages;

/// <summary>Gemeinsamer Einstieg für Mitglieds-Protokolle mit Parzellenbezug.</summary>
public sealed class ParzellenProtokollePage : ContentPage
{
    private readonly ISupabaseService _supabase;
    private readonly UserContextState _userContext;
    private readonly Picker _typPicker = new() { Title = "Protokollart" };
    private readonly Picker _mitgliedPicker = new() { Title = "Mitglied auswählen" };
    private readonly Picker _parzellePicker = new() { Title = "Parzelle auswählen" };
    private readonly Picker _vorstand2Picker = new() { Title = "Zweiten Vorstand auswählen" };
    private readonly Switch _begleitpersonSwitch = new();
    private readonly Picker _begleitmitgliedPicker = new() { Title = "Nebenmitglied auswählen" };
    private readonly Entry _begleitpersonName = new() { Placeholder = "Name der Begleitperson" };
    private readonly Label _vorstand1Label = new() { TextColor = Colors.DimGray };
    private readonly VerticalStackLayout _form = new() { Spacing = 10, IsVisible = false };
    private bool _loaded;

    public ParzellenProtokollePage()
    {
        var services = Application.Current?.Handler?.MauiContext?.Services
            ?? throw new InvalidOperationException("MAUI-Services sind aktuell nicht verfügbar.");
        _supabase = services.GetRequiredService<ISupabaseService>();
        _userContext = services.GetRequiredService<UserContextState>();
        Title = "Protokolle";
        BackgroundColor = Colors.White;

        _typPicker.ItemsSource = new[] { "Übernahmeprotokoll", "Rückgabeprotokoll", "Begehungsprotokoll" };
        _typPicker.SelectedIndexChanged += (_, _) => _form.IsVisible = _typPicker.SelectedIndex >= 0;
        _begleitpersonSwitch.Toggled += (_, e) =>
        {
            _begleitmitgliedPicker.IsVisible = e.Value;
            _begleitpersonName.IsVisible = e.Value;
        };

        _form.Children.Add(CreateField("Mitglied", _mitgliedPicker));
        _form.Children.Add(CreateField("Parzelle", _parzellePicker));
        _form.Children.Add(CreateField("Vorstand 1", _vorstand1Label));
        _form.Children.Add(CreateField("Vorstand 2", _vorstand2Picker));
        _form.Children.Add(CreateField("Pächter bringt Begleitperson mit", _begleitpersonSwitch));
        _begleitmitgliedPicker.IsVisible = false;
        _begleitpersonName.IsVisible = false;
        _form.Children.Add(_begleitmitgliedPicker);
        _form.Children.Add(_begleitpersonName);
        var continueButton = new Button { Text = "Weiter zur Protokollerfassung" };
        continueButton.Clicked += async (_, _) => await ValidateSelectionAsync();
        _form.Children.Add(continueButton);

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = 20,
                Spacing = 14,
                Children =
                {
                    new Label { Text = "Parzellen-Protokolle", FontSize = 24, FontAttributes = FontAttributes.Bold },
                    new Label
                    {
                        Text = "Protokolle werden dem Mitglied zugeordnet. Die Parzelle, Ablesungen, Fotos und Unterschriften bilden den nachvollziehbaren fachlichen Bezug.",
                        LineBreakMode = LineBreakMode.WordWrap,
                        TextColor = Colors.DimGray
                    },
                    CreateField("Protokollart", _typPicker),
                    _form
                }
            }
        };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_loaded) return;
        _loaded = true;
        try
        {
            var members = (await _supabase.GetMitgliederAsync()).Where(m => m.Aktiv).OrderBy(m => m.Name).ToList();
            _mitgliedPicker.ItemsSource = members;
            _mitgliedPicker.ItemDisplayBinding = new Binding("Name");
            _begleitmitgliedPicker.ItemsSource = members;
            _begleitmitgliedPicker.ItemDisplayBinding = new Binding("Name");
            _parzellePicker.ItemsSource = (await _supabase.GetAllParzellenAsync()).Where(p => p.Aktiv).OrderBy(p => p.GartenNrSortKey).ToList();
            _parzellePicker.ItemDisplayBinding = new Binding("DisplayName");
            _vorstand2Picker.ItemsSource = members.Where(m => string.Equals(m.Role, "admin", StringComparison.OrdinalIgnoreCase) || string.Equals(m.Role, "vorstand", StringComparison.OrdinalIgnoreCase)).ToList();
            _vorstand2Picker.ItemDisplayBinding = new Binding("Name");
            _vorstand1Label.Text = _userContext.CurrentMitgliedId is long id
                ? (members.FirstOrDefault(m => m.Id == id) is { } current ? $"{current.Vorname} {current.Name}" : "Angemeldetes Vorstandsmitglied")
                : "Angemeldetes Vorstandsmitglied";
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Protokolle", $"Stammdaten konnten nicht geladen werden: {ex.Message}", "OK");
        }
    }

    private async Task ValidateSelectionAsync()
    {
        if (_mitgliedPicker.SelectedItem is not MitgliedRecord || _parzellePicker.SelectedItem is not ParzelleRecord || _vorstand2Picker.SelectedItem is not MitgliedRecord)
        {
            await DisplayAlertAsync("Validierung", "Bitte Mitglied, Parzelle und das zweite Vorstandsmitglied auswählen.", "OK");
            return;
        }
        if (_begleitpersonSwitch.IsToggled && _begleitmitgliedPicker.SelectedItem is not MitgliedRecord && string.IsNullOrWhiteSpace(_begleitpersonName.Text))
        {
            await DisplayAlertAsync("Validierung", "Bitte ein Nebenmitglied auswählen oder den Namen der Begleitperson eintragen.", "OK");
            return;
        }
        await DisplayAlertAsync("Protokolle", "Auswahl übernommen. Im nächsten Schritt folgen Ablesungen, Fotos und Unterschriften.", "OK");
    }

    private static View CreateField(string label, View field) => new VerticalStackLayout { Spacing = 3, Children = { new Label { Text = label, FontAttributes = FontAttributes.Bold }, field } };

}
