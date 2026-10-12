using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using KGV.Core.Interfaces;
using KGV.Core.Models;
using KGV.Core.Security;
using KGV.Maui.State;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace KGV.Maui.Pages;

/// <summary>Rechnungsübersicht einer Saison mit Einstieg in Erfassung und Stammdaten.</summary>
public sealed class JahresabschlussPage : ContentPage
{
    private readonly ISupabaseService _supabase;
    private readonly IJahresabschlussService _abschluss;
    private readonly UserContextState _context;
    private readonly Picker _saisonPicker = new() { Title = "Saison wählen" };
    private readonly Label _status = new() { TextColor = Colors.DarkSlateBlue, LineBreakMode = Microsoft.Maui.LineBreakMode.WordWrap };
    private readonly ObservableCollection<JahresabschlussRechnungRecord> _rechnungen = new();
    private readonly Button _neueRechnung;
    private readonly Button _stammdaten;
    private readonly Button _pruefen;
    private bool _loading;

    public JahresabschlussPage(ISupabaseService supabase, IJahresabschlussService abschluss, UserContextState context)
    {
        _supabase = supabase;
        _abschluss = abschluss;
        _context = context;
        Title = "Jahresabschluss";
        _saisonPicker.SelectedIndexChanged += async (_, _) => await LoadRechnungenAsync();

        var rechnungenView = new CollectionView
        {
            ItemsSource = _rechnungen,
            SelectionMode = SelectionMode.Single,
            EmptyView = new Label { Text = "Für diese Saison sind noch keine Rechnungen erfasst.", TextColor = Colors.Gray },
            ItemTemplate = new DataTemplate(() =>
            {
                var supplier = new Label { FontAttributes = FontAttributes.Bold };
                supplier.SetBinding(Label.TextProperty, nameof(JahresabschlussRechnungRecord.Lieferant));
                var details = new Label { FontSize = 12, TextColor = Colors.Gray };
                details.SetBinding(Label.TextProperty, new Binding(path: ".", converter: new RechnungSummaryConverter()));
                return new Border { Stroke = Colors.LightGray, Padding = 12, Margin = new Microsoft.Maui.Thickness(0, 0, 0, 8), Content = new VerticalStackLayout { Children = { supplier, details } } };
            })
        };
        rechnungenView.SelectionChanged += async (_, e) =>
        {
            if (e.CurrentSelection.FirstOrDefault() is JahresabschlussRechnungRecord rechnung && _saisonPicker.SelectedItem is SaisonRecord saison)
            {
                rechnungenView.SelectedItem = null;
                if (!CanPerformYearEndClosing)
                {
                    _rechnungen.Clear();
                    ApplyPermissionState();
                    _status.Text = "Für den Jahresabschluss fehlt die Berechtigung.";
                    return;
                }
                await Navigation.PushAsync(new JahresabschlussRechnungEditorPage(_supabase, saison, rechnung));
            }
        };

        _neueRechnung = new Button { Text = "Rechnung erfassen" };
        _neueRechnung.Clicked += async (_, _) => { if (CanPerformYearEndClosing && _saisonPicker.SelectedItem is SaisonRecord saison) await Navigation.PushAsync(new JahresabschlussRechnungEditorPage(_supabase, saison)); };
        _stammdaten = new Button { Text = "Kosten- und Umlagearten verwalten" };
        _stammdaten.Clicked += async (_, _) => { if (CanPerformYearEndClosing) await Navigation.PushAsync(new JahresabschlussStammdatenPage(_supabase)); };
        _pruefen = new Button { Text = "Abschluss prüfen" };
        _pruefen.Clicked += async (_, _) => { if (CanPerformYearEndClosing && _saisonPicker.SelectedItem is SaisonRecord saison) await Navigation.PushAsync(new JahresabschlussPruefungPage(_abschluss, saison, _context)); };

        Content = new ScrollView { Content = new VerticalStackLayout { Padding = 24, Spacing = 12, Children =
        {
            new Label { Text = "Jahresabschluss", FontSize = 24, FontAttributes = FontAttributes.Bold },
            new Label { Text = "Rechnungen erfassen und ihre Umlagen vollständig zuordnen.", TextColor = Colors.Gray, LineBreakMode = Microsoft.Maui.LineBreakMode.WordWrap },
            _saisonPicker, _neueRechnung, _stammdaten, _pruefen, _status, rechnungenView
        } } };
        Appearing += async (_, _) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        if (_loading || !CanPerformYearEndClosing)
        {
            _rechnungen.Clear();
            ApplyPermissionState();
            _status.Text = "Für den Jahresabschluss fehlt die Berechtigung.";
            return;
        }
        _loading = true;
        try
        {
            ApplyPermissionState();
            var saisons = (await _supabase.GetSaisonRecordsAsync()).OrderByDescending(x => x.Jahr).ToList();
            _saisonPicker.ItemsSource = saisons;
            _saisonPicker.ItemDisplayBinding = new Binding(nameof(SaisonRecord.Jahr));
            _saisonPicker.SelectedItem ??= saisons.FirstOrDefault(x => x.Jahr == DateTime.Today.Year) ?? saisons.FirstOrDefault();
            await LoadRechnungenAsync();
        }
        catch (Exception ex) { _status.Text = $"Jahresabschluss konnte nicht geladen werden: {ex.Message}"; }
        finally { _loading = false; ApplyPermissionState(); }
    }

    private async Task LoadRechnungenAsync()
    {
        if (_loading || !CanPerformYearEndClosing)
        {
            _rechnungen.Clear();
            ApplyPermissionState();
            return;
        }
        if (_saisonPicker.SelectedItem is not SaisonRecord saison) return;
        _rechnungen.Clear();
        foreach (var rechnung in await _supabase.GetJahresabschlussRechnungenAsync(saison.Id)) _rechnungen.Add(rechnung);
        _status.Text = _rechnungen.Count == 0 ? string.Empty : $"{_rechnungen.Count} Rechnung(en) für {saison.Jahr}. Zum Bearbeiten antippen.";
    }

    private bool CanPerformYearEndClosing => PermissionChecks.CanPerformYearEndClosing(_context.CurrentUserContext);

    private void ApplyPermissionState()
    {
        var enabled = CanPerformYearEndClosing && !_loading;
        _saisonPicker.IsEnabled = enabled;
        _neueRechnung.IsEnabled = enabled;
        _stammdaten.IsEnabled = enabled;
        _pruefen.IsEnabled = enabled;
    }
}

internal sealed class RechnungSummaryConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture) => value is JahresabschlussRechnungRecord r ? $"{r.Rechnungsdatum:dd.MM.yyyy} · {r.Gesamtbetrag:N2} €" : string.Empty;
    public object ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture) => throw new NotSupportedException();
}
