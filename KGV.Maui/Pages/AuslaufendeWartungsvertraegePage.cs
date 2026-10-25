using System.Collections.ObjectModel;
using KGV.Core.Interfaces;
using KGV.Core.Models;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace KGV.Maui.Pages;

/// <summary>Read-only server-sorted entry point; no local due-date classification is applied.</summary>
public sealed class AuslaufendeWartungsvertraegePage : ContentPage
{
    private readonly ISupabaseService _service;
    private readonly ObservableCollection<WartungsvertragFortfuehrungDetail> _items = [];
    private readonly Label _status = new() { TextColor = Colors.DarkSlateBlue };
    private readonly CollectionView _list;
    public AuslaufendeWartungsvertraegePage(ISupabaseService service)
    {
        _service = service; Title = "Auslaufende Wartungsverträge";
        _list = new CollectionView { ItemsSource = _items, SelectionMode = SelectionMode.Single, EmptyView = new Label { Text = "Keine auslaufenden Wartungsverträge.", TextColor = Colors.Gray }, ItemTemplate = new DataTemplate(() =>
        {
            var title = new Label { FontAttributes = FontAttributes.Bold }; title.SetBinding(Label.TextProperty, nameof(WartungsvertragFortfuehrungDetail.WartungsvertragTitel));
            var term = new Label { TextColor = Colors.Gray }; term.SetBinding(Label.TextProperty, nameof(WartungsvertragFortfuehrungDetail.GueltigBis), stringFormat: "Vertragsende: {0:dd.MM.yyyy}");
            var member = new Label { TextColor = Colors.Gray }; member.SetBinding(Label.TextProperty, nameof(WartungsvertragFortfuehrungDetail.MitgliedName));
            return new Border { Stroke = Colors.LightGray, Padding = 12, Margin = new Thickness(0, 0, 0, 8), Content = new VerticalStackLayout { Children = { title, member, term } } };
        }) };
        _list.SelectionChanged += async (_, e) => { if (e.CurrentSelection.FirstOrDefault() is WartungsvertragFortfuehrungDetail item) await Shell.Current.GoToAsync($"{nameof(WartungsvertragFortfuehrungPage)}?zuordnungId={item.VorgaengerZuordnungId}"); _list.SelectedItem = null; };
        var refresh = new Button { Text = "Aktualisieren" }; refresh.Clicked += async (_, _) => await LoadAsync();
        Content = new VerticalStackLayout { Padding = 24, Spacing = 12, Children = { new Label { Text = Title, FontSize = 24, FontAttributes = FontAttributes.Bold }, new Label { Text = "Nach Vertragsende sortiert. Fälligkeitshinweise werden ausschließlich vom Serverzustand abgeleitet.", TextColor = Colors.Gray }, refresh, _status, _list } };
        Appearing += async (_, _) => await LoadAsync();
    }
    private async Task LoadAsync() { try { _items.Clear(); foreach (var item in (await _service.GetAuslaufendeWartungsvertragZuordnungenAsync()).OrderBy(x => x.GueltigBis)) _items.Add(item); _status.Text = string.Empty; } catch { _status.Text = "Die auslaufenden Wartungsverträge konnten nicht geladen werden."; } }
}
