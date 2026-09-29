using System;
using System.Linq;
using System.Threading.Tasks;
using KGV.Core.Interfaces;
using KGV.Core.Models;
using KGV.Maui.State;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace KGV.Maui.Pages;

/// <summary>Prüfbericht vor der späteren verbindlichen Abschlussberechnung.</summary>
public sealed class JahresabschlussPruefungPage : ContentPage
{
    private readonly IJahresabschlussService _abschluss;
    private readonly SaisonRecord _saison;
    private readonly UserContextState _userContext;
    private readonly Label _status = new() { FontAttributes = FontAttributes.Bold, LineBreakMode = Microsoft.Maui.LineBreakMode.WordWrap };
    private readonly VerticalStackLayout _fehler = new() { Spacing = 6 };
    private readonly VerticalStackLayout _warnungen = new() { Spacing = 6 };

    public JahresabschlussPruefungPage(IJahresabschlussService abschluss, SaisonRecord saison, UserContextState userContext)
    {
        _abschluss = abschluss; _saison = saison; _userContext = userContext; Title = "Abschluss prüfen";
        var refresh = new Button { Text = "Prüfbericht aktualisieren" }; refresh.Clicked += async (_, _) => await LoadAsync();
        var calculate = new Button { Text = "Entwurf berechnen / neu berechnen" }; calculate.Clicked += async (_, _) => await CalculateAsync();
        var finalize = new Button { Text = "Jahresabschluss endgültig abschließen", BackgroundColor = Colors.DarkRed, TextColor = Colors.White }; finalize.Clicked += async (_, _) => await FinalizeAsync();
        Content = new ScrollView { Content = new VerticalStackLayout { Padding = 24, Spacing = 12, Children =
        {
            new Label { Text = $"Prüfung Jahresabschluss {saison.Jahr}", FontSize = 22, FontAttributes = FontAttributes.Bold }, refresh, calculate, finalize, _status,
            new Label { Text = "Blockierende Fehler", FontSize = 18, FontAttributes = FontAttributes.Bold }, _fehler,
            new Label { Text = "Warnungen – vor Abschluss begründen und bestätigen", FontSize = 18, FontAttributes = FontAttributes.Bold }, _warnungen
        } } };
        Appearing += async (_, _) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        _status.Text = "Prüfung läuft …"; _status.TextColor = Colors.DarkSlateBlue; _fehler.Children.Clear(); _warnungen.Children.Clear();
        try
        {
            var result = await _abschluss.PruefeAsync(_saison);
            _status.Text = result.KannAbschliessen ? "Keine blockierenden Fehler gefunden." : $"{result.Fehler.Count} blockierende Fehler gefunden.";
            _status.TextColor = result.KannAbschliessen ? Colors.DarkGreen : Colors.Firebrick;
            foreach (var text in result.Fehler) _fehler.Children.Add(CreateItem(text, Colors.Firebrick));
            foreach (var text in result.Warnungen) _warnungen.Children.Add(CreateItem(text, Colors.DarkOrange));
            if (!result.Fehler.Any()) _fehler.Children.Add(CreateItem("Keine", Colors.DarkGreen));
            if (!result.Warnungen.Any()) _warnungen.Children.Add(CreateItem("Keine", Colors.DarkGreen));
        }
        catch (Exception ex) { _status.Text = $"Prüfung fehlgeschlagen: {ex.Message}"; _status.TextColor = Colors.Firebrick; }
    }

    private async Task CalculateAsync()
    {
        _status.Text = "Berechnung läuft …"; _status.TextColor = Colors.DarkSlateBlue;
        try
        {
            var result = await _abschluss.BerechneAsync(_saison);
            _status.Text = result.Erfolgreich
                ? $"{result.Meldung} {result.PositionsAnzahl} Positionen, {result.Gesamtbetrag:N2} €."
                : result.Meldung ?? "Berechnung konnte nicht durchgeführt werden.";
            _status.TextColor = result.Erfolgreich ? Colors.DarkGreen : Colors.Firebrick;
            await LoadAsync();
        }
        catch (Exception ex) { _status.Text = $"Berechnung fehlgeschlagen: {ex.Message}"; _status.TextColor = Colors.Firebrick; }
    }

    private async Task FinalizeAsync()
    {
        if (_userContext.CurrentMitgliedId is not > 0) { _status.Text = "Das angemeldete Mitglied konnte nicht bestimmt werden."; _status.TextColor = Colors.Firebrick; return; }
        var confirmed = await DisplayAlertAsync("Endgültig abschließen", "Der Abschluss und seine Positionen können danach nicht mehr geändert werden. Korrekturen erfolgen später nur über einen Korrekturabschluss.", "Endgültig abschließen", "Abbrechen");
        if (!confirmed) return;
        var result = await _abschluss.AbschliessenAsync(_saison, _userContext.CurrentMitgliedId.Value);
        _status.Text = result.Meldung ?? "Abschluss fehlgeschlagen."; _status.TextColor = result.Erfolgreich ? Colors.DarkGreen : Colors.Firebrick;
    }

    private static Label CreateItem(string text, Color color) => new() { Text = "• " + text, TextColor = color, LineBreakMode = Microsoft.Maui.LineBreakMode.WordWrap };
}
