using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using KGV.Core.Interfaces;
using KGV.Core.Models;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace KGV.Maui.Pages;

/// <summary>Verwaltung der für den Abschluss verwendeten Kosten- und Umlagearten.</summary>
public sealed class JahresabschlussStammdatenPage : ContentPage
{
    private readonly ISupabaseService _supabase;
    private readonly ObservableCollection<KostenartRecord> _kostenarten = new();
    private readonly ObservableCollection<UmlageartRecord> _umlagearten = new();

    public JahresabschlussStammdatenPage(ISupabaseService supabase)
    {
        _supabase = supabase;
        Title = "Abrechnungs-Stammdaten";
        var kosten = CreateList(_kostenarten, x => $"{x.Bezeichnung} · {(x.Aktiv ? "aktiv" : "inaktiv")}", ToggleKostenartAsync);
        var umlagen = CreateList(_umlagearten, x => $"{x.Kuerzel}: {x.Bezeichnung} · {x.Verteilung} · {(x.Aktiv ? "aktiv" : "inaktiv")}", ToggleUmlageartAsync);
        var addKosten = new Button { Text = "Kostenart hinzufügen" }; addKosten.Clicked += async (_, _) => await AddKostenartAsync();
        var addUmlage = new Button { Text = "Umlageart hinzufügen" }; addUmlage.Clicked += async (_, _) => await AddUmlageartAsync();
        Content = new ScrollView { Content = new VerticalStackLayout { Padding = 24, Spacing = 10, Children =
        {
            new Label { Text = "Kostenarten", FontSize = 22, FontAttributes = FontAttributes.Bold }, addKosten, kosten,
            new Label { Text = "Umlagearten", FontSize = 22, FontAttributes = FontAttributes.Bold, Margin = new Microsoft.Maui.Thickness(0, 16, 0, 0) }, addUmlage, umlagen,
            new Label { Text = "Zum Aktivieren oder Deaktivieren einen Eintrag antippen. Bereits verwendete Arten werden nicht gelöscht.", TextColor = Colors.Gray, FontSize = 12, LineBreakMode = Microsoft.Maui.LineBreakMode.WordWrap }
        } } };
        Appearing += async (_, _) => await LoadAsync();
    }

    private static CollectionView CreateList<T>(ObservableCollection<T> items, Func<T, string> format, Func<T, Task> selected) where T : class
    {
        var list = new CollectionView { ItemsSource = items, SelectionMode = SelectionMode.Single, HeightRequest = 210 };
        list.ItemTemplate = new DataTemplate(() => { var label = new Label { Padding = 8 }; label.SetBinding(Label.TextProperty, new Binding(path: ".", converter: new TextConverter<T>(format))); return label; });
        list.SelectionChanged += async (_, e) => { if (e.CurrentSelection.Count > 0 && e.CurrentSelection[0] is T item) { list.SelectedItem = null; await selected(item); } };
        return list;
    }

    private async Task LoadAsync()
    {
        _kostenarten.Clear(); foreach (var item in await _supabase.GetKostenartenAsync(true)) _kostenarten.Add(item);
        _umlagearten.Clear(); foreach (var item in await _supabase.GetUmlageartenAsync(true)) _umlagearten.Add(item);
    }
    private async Task AddKostenartAsync()
    {
        var name = await DisplayPromptAsync("Kostenart", "Bezeichnung"); if (string.IsNullOrWhiteSpace(name)) return;
        var beschreibung = await DisplayPromptAsync("Kostenart", "Beschreibung (optional)");
        await _supabase.SaveKostenartAsync(new KostenartRecord { Bezeichnung = name.Trim(), Beschreibung = beschreibung, Aktiv = true }); await LoadAsync();
    }
    private async Task AddUmlageartAsync()
    {
        var kuerzel = await DisplayPromptAsync("Umlageart", "Kürzel, z. B. SONST"); if (string.IsNullOrWhiteSpace(kuerzel)) return;
        var name = await DisplayPromptAsync("Umlageart", "Bezeichnung"); if (string.IsNullOrWhiteSpace(name)) return;
        var verteilung = await DisplayActionSheetAsync("Verteilung", "Abbrechen", null, "pro_mitglied", "pro_flaeche_und_tag", "anschluss_und_tag", "verbrauch", "einzel", "keine");
        if (verteilung == "Abbrechen") return;
        await _supabase.SaveUmlageartAsync(new UmlageartRecord { Kuerzel = kuerzel.Trim().ToUpperInvariant(), Bezeichnung = name.Trim(), Verteilung = verteilung, Aktiv = true }); await LoadAsync();
    }
    private async Task ToggleKostenartAsync(KostenartRecord item) { item.Aktiv = !item.Aktiv; await _supabase.SaveKostenartAsync(item); await LoadAsync(); }
    private async Task ToggleUmlageartAsync(UmlageartRecord item) { item.Aktiv = !item.Aktiv; await _supabase.SaveUmlageartAsync(item); await LoadAsync(); }

    private sealed class TextConverter<T>(Func<T, string> format) : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture) => value is T item ? format(item) : string.Empty;
        public object ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture) => throw new NotSupportedException();
    }
}
