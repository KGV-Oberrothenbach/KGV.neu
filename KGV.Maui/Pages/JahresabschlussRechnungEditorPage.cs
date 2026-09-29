using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using KGV.Core.Interfaces;
using KGV.Core.Models;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace KGV.Maui.Pages;

/// <summary>Erfasst einen Rechnungskopf und dessen frei viele Umlagezeilen.</summary>
public sealed class JahresabschlussRechnungEditorPage : ContentPage
{
    private readonly ISupabaseService _supabase;
    private readonly SaisonRecord _saison;
    private JahresabschlussRechnungRecord _rechnung;
    private readonly Entry _lieferant = new() { Placeholder = "Lieferant" };
    private readonly Entry _nummer = new() { Placeholder = "Rechnungsnummer (optional)" };
    private readonly DatePicker _datum = new() { Date = DateTime.Today };
    private readonly Entry _gesamtbetrag = new() { Placeholder = "Gesamtbetrag, z. B. 125,50", Keyboard = Microsoft.Maui.Keyboard.Numeric };
    private readonly Editor _bemerkung = new() { Placeholder = "Bemerkung (optional)", AutoSize = EditorAutoSizeOption.TextChanges };
    private readonly Picker _kostenart = new() { Title = "Kostenart" };
    private readonly Picker _umlageart = new() { Title = "Umlageart" };
    private readonly Entry _betrag = new() { Placeholder = "Betrag", Keyboard = Microsoft.Maui.Keyboard.Numeric };
    private readonly Entry _zielSuche = new() { Placeholder = "Nur EINZEL: Gartennummer oder Mitglied suchen" };
    private readonly Label _ziel = new() { TextColor = Colors.DarkSlateBlue, LineBreakMode = Microsoft.Maui.LineBreakMode.WordWrap };
    private readonly Label _verteilung = new() { TextColor = Colors.DarkSlateBlue, FontAttributes = FontAttributes.Bold };
    private readonly Label _status = new() { TextColor = Colors.Firebrick, LineBreakMode = Microsoft.Maui.LineBreakMode.WordWrap };
    private readonly ObservableCollection<JahresabschlussRechnungZuordnungRecord> _zuordnungen = new();
    private List<KostenartRecord> _kostenarten = new();
    private List<UmlageartRecord> _umlagearten = new();
    private ParzelleRecord? _zielParzelle;
    private MitgliedRecord? _zielMitglied;

    public JahresabschlussRechnungEditorPage(ISupabaseService supabase, SaisonRecord saison, JahresabschlussRechnungRecord? rechnung = null)
    {
        _supabase = supabase;
        _saison = saison;
        _rechnung = rechnung ?? new JahresabschlussRechnungRecord { SaisonId = saison.Id, Rechnungsdatum = DateTime.Today };
        Title = rechnung == null ? "Rechnung erfassen" : "Rechnung bearbeiten";
        _umlageart.SelectedIndexChanged += (_, _) => UpdateTargetHint();

        var save = new Button { Text = "Rechnung speichern" }; save.Clicked += async (_, _) => await SaveRechnungAsync();
        var suchen = new Button { Text = "Ziel suchen" }; suchen.Clicked += async (_, _) => await SearchTargetAsync();
        var add = new Button { Text = "Zuordnungszeile hinzufügen" }; add.Clicked += async (_, _) => await AddZuordnungAsync();
        var lines = new CollectionView
        {
            ItemsSource = _zuordnungen, SelectionMode = SelectionMode.Single,
            EmptyView = new Label { Text = "Noch keine Zuordnungszeile vorhanden.", TextColor = Colors.Gray },
            ItemTemplate = new DataTemplate(() =>
            {
                var label = new Label { Padding = 8, LineBreakMode = Microsoft.Maui.LineBreakMode.WordWrap };
                label.SetBinding(Label.TextProperty, new Binding(path: ".", converter: new ZuordnungConverter(this)));
                return label;
            })
        };
        lines.SelectionChanged += async (_, e) =>
        {
            if (e.CurrentSelection.FirstOrDefault() is JahresabschlussRechnungZuordnungRecord item)
            {
                lines.SelectedItem = null;
                if (await DisplayAlertAsync("Zuordnungszeile", "Diese Zuordnungszeile entfernen?", "Entfernen", "Behalten"))
                { await _supabase.DeleteJahresabschlussRechnungZuordnungAsync(item.Id); await LoadZuordnungenAsync(); }
            }
        };

        Content = new ScrollView { Content = new VerticalStackLayout { Padding = 24, Spacing = 10, Children =
        {
            new Label { Text = $"Saison {_saison.Jahr}", FontSize = 22, FontAttributes = FontAttributes.Bold }, _lieferant, _nummer, _datum, _gesamtbetrag, _bemerkung, save,
            new BoxView { HeightRequest = 1, Color = Colors.LightGray, Margin = new Microsoft.Maui.Thickness(0, 8) },
            new Label { Text = "Umlage zuordnen", FontSize = 20, FontAttributes = FontAttributes.Bold }, _kostenart, _umlageart, _betrag, _zielSuche, suchen, _ziel, add, _verteilung, _status, lines,
            new Label { Text = "Zeile antippen, um sie zu entfernen. Für Änderungen Zeile entfernen und neu anlegen.", FontSize = 12, TextColor = Colors.Gray, LineBreakMode = Microsoft.Maui.LineBreakMode.WordWrap }
        } } };
        Appearing += async (_, _) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        _lieferant.Text = _rechnung.Lieferant; _nummer.Text = _rechnung.Rechnungsnummer; _datum.Date = _rechnung.Rechnungsdatum == default ? DateTime.Today : _rechnung.Rechnungsdatum;
        _gesamtbetrag.Text = _rechnung.Gesamtbetrag == 0 ? string.Empty : _rechnung.Gesamtbetrag.ToString("N2", CultureInfo.CurrentCulture); _bemerkung.Text = _rechnung.Bemerkung;
        _kostenarten = await _supabase.GetKostenartenAsync(); _umlagearten = await _supabase.GetUmlageartenAsync();
        _kostenart.ItemsSource = _kostenarten; _kostenart.ItemDisplayBinding = new Binding(nameof(KostenartRecord.Bezeichnung));
        _umlageart.ItemsSource = _umlagearten; _umlageart.ItemDisplayBinding = new Binding(nameof(UmlageartRecord.Bezeichnung));
        await LoadZuordnungenAsync(); UpdateTargetHint();
    }

    private async Task SaveRechnungAsync()
    {
        if (!TryParseAmount(_gesamtbetrag.Text, out var total) || string.IsNullOrWhiteSpace(_lieferant.Text)) { _status.Text = "Lieferant und gültiger Gesamtbetrag sind erforderlich."; return; }
        _rechnung.Lieferant = _lieferant.Text.Trim(); _rechnung.Rechnungsnummer = EmptyToNull(_nummer.Text); _rechnung.Rechnungsdatum = _datum.Date ?? DateTime.Today;
        _rechnung.Gesamtbetrag = total; _rechnung.Bemerkung = EmptyToNull(_bemerkung.Text);
        var saved = await _supabase.SaveJahresabschlussRechnungAsync(_rechnung);
        if (saved == null) { _status.Text = "Rechnung konnte nicht gespeichert werden."; return; }
        _rechnung = saved; _status.Text = "Rechnung gespeichert. Jetzt können Umlagezeilen erfasst werden."; await LoadZuordnungenAsync();
    }

    private async Task AddZuordnungAsync()
    {
        if (_rechnung.Id <= 0) { _status.Text = "Bitte zuerst den Rechnungskopf speichern."; return; }
        if (_kostenart.SelectedItem is not KostenartRecord kosten || _umlageart.SelectedItem is not UmlageartRecord umlage || !TryParseAmount(_betrag.Text, out var betrag)) { _status.Text = "Kostenart, Umlageart und ein gültiger Betrag sind erforderlich."; return; }
        if (umlage.Kuerzel == "EINZEL" && _zielParzelle == null && _zielMitglied == null) { _status.Text = "Für EINZEL muss ein Garten oder Mitglied ausgewählt werden."; return; }
        var alreadyAssigned = _zuordnungen.Sum(x => x.Betrag);
        if (alreadyAssigned + betrag > _rechnung.Gesamtbetrag) { _status.Text = "Die Zuordnungssumme darf den Rechnungsbetrag nicht übersteigen."; return; }
        var saved = await _supabase.SaveJahresabschlussRechnungZuordnungAsync(new JahresabschlussRechnungZuordnungRecord { RechnungId = _rechnung.Id, KostenartId = kosten.Id, UmlageartId = umlage.Id, Betrag = betrag, ParzelleId = _zielParzelle?.Id, MitgliedId = _zielMitglied?.Id });
        if (saved == null) { _status.Text = "Die Zuordnung konnte nicht gespeichert werden."; return; }
        _betrag.Text = string.Empty; _zielSuche.Text = string.Empty; _zielParzelle = null; _zielMitglied = null; _ziel.Text = string.Empty; _status.Text = string.Empty; await LoadZuordnungenAsync();
    }

    private async Task SearchTargetAsync()
    {
        var query = (_zielSuche.Text ?? string.Empty).Trim(); if (string.IsNullOrWhiteSpace(query)) { _status.Text = "Bitte Gartennummer oder Namen eingeben."; return; }
        var parzelle = await _supabase.GetParzelleByNumberAsync(query);
        if (parzelle != null) { _zielParzelle = parzelle; _zielMitglied = null; _ziel.Text = $"Ziel: Garten {parzelle.DisplayName}"; return; }
        var matches = (await _supabase.GetMitgliederAsync()).Where(x => ($"{x.Vorname} {x.Name}").Contains(query, StringComparison.OrdinalIgnoreCase)).Take(8).ToList();
        if (matches.Count == 0) { _status.Text = "Kein Garten und kein Mitglied gefunden."; return; }
        var choices = matches.Select(x => $"{x.Vorname} {x.Name}").ToArray(); var choice = await DisplayActionSheetAsync("Mitglied auswählen", "Abbrechen", null, choices);
        var selected = matches.FirstOrDefault(x => $"{x.Vorname} {x.Name}" == choice); if (selected == null) return;
        _zielMitglied = selected; _zielParzelle = null; _ziel.Text = $"Ziel: Mitglied {selected.Vorname} {selected.Name}";
    }

    private async Task LoadZuordnungenAsync()
    {
        _zuordnungen.Clear(); if (_rechnung.Id > 0) foreach (var line in await _supabase.GetJahresabschlussRechnungZuordnungenAsync(_rechnung.Id)) _zuordnungen.Add(line);
        var sum = _zuordnungen.Sum(x => x.Betrag); var remaining = _rechnung.Gesamtbetrag - sum;
        _verteilung.Text = _rechnung.Id <= 0 ? "Rechnung zuerst speichern." : $"Zugeordnet: {sum:N2} € · Rest: {remaining:N2} €";
        if (_rechnung.Id > 0 && remaining != 0) _status.Text = "Die Rechnung ist noch nicht vollständig verteilt und wird später die Berechnung blockieren.";
    }

    private void UpdateTargetHint() => _zielSuche.IsVisible = (_umlageart.SelectedItem as UmlageartRecord)?.Kuerzel == "EINZEL";
    private static bool TryParseAmount(string? value, out decimal result) => decimal.TryParse(value, NumberStyles.Number, CultureInfo.CurrentCulture, out result) || decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out result);
    private static string? EmptyToNull(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private sealed class ZuordnungConverter(JahresabschlussRechnungEditorPage page) : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is not JahresabschlussRechnungZuordnungRecord z) return string.Empty;
            var kosten = page._kostenarten.FirstOrDefault(x => x.Id == z.KostenartId)?.Bezeichnung ?? "Kostenart";
            var umlage = page._umlagearten.FirstOrDefault(x => x.Id == z.UmlageartId)?.Kuerzel ?? "Umlage";
            return $"{kosten} · {umlage} · {z.Betrag:N2} €";
        }
        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
    }
}
