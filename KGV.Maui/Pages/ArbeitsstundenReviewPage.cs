using KGV.Core.Interfaces;
using KGV.Core.Models;
using KGV.Maui.State;

namespace KGV.Maui.Pages;

public sealed class ArbeitsstundenReviewPage : ContentPage
{
    private readonly ISupabaseService _supabaseService;
    private readonly ArbeitsstundenReviewState _reviewState;

    private readonly List<ArbeitsstundeDTO> _items = new();
    private readonly CollectionView _list;
    private readonly Label _status;
    private readonly Label _countLabel;

    public ArbeitsstundenReviewPage(ISupabaseService supabaseService, ArbeitsstundenReviewState reviewState)
    {
        _supabaseService = supabaseService;
        _reviewState = reviewState;

        Title = "Arbeitsstunden freigeben";

        _status = new Label { TextColor = Colors.DarkSlateBlue, LineBreakMode = LineBreakMode.WordWrap };
        _countLabel = new Label { TextColor = Colors.Gray, FontSize = 12 };

        _list = new CollectionView
        {
            SelectionMode = SelectionMode.Single,
            ItemsSource = _items,
            EmptyView = new Label
            {
                Text = "Aktuell liegen keine offenen Prüffälle vor.",
                TextColor = Colors.Gray
            },
            ItemTemplate = new DataTemplate(() =>
            {
                var title = new Label { FontAttributes = FontAttributes.Bold };
                title.SetBinding(Label.TextProperty, new Binding(path: ".", converter: new HeaderConverter()));

                var desc = new Label { FontSize = 12, TextColor = Colors.Gray, LineBreakMode = LineBreakMode.WordWrap };
                desc.SetBinding(Label.TextProperty, nameof(ArbeitsstundeDTO.Beschreibung));

                var reviewHint = new Label { FontSize = 12, TextColor = Colors.DarkSlateBlue };
                reviewHint.Text = "Antippen öffnet die Einzeldatensatz-Prüfung.";

                return new Border
                {
                    Stroke = Colors.LightGray,
                    Padding = 12,
                    Margin = new Thickness(0, 0, 0, 8),
                    Content = new VerticalStackLayout
                    {
                        Spacing = 4,
                        Children = { title, desc, reviewHint }
                    }
                };
            })
        };
        _list.SelectionChanged += OnSelectionChanged;

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = 24,
                Spacing = 12,
                Children =
                {
                    new Label
                    {
                        Text = "Offene Prüffälle bleiben in der Übersicht ruhig als Liste. Die eigentliche Entscheidung erfolgt anschließend pro Datensatz auf einer eigenen mobilen Prüfseite.",
                        TextColor = Colors.Gray,
                        LineBreakMode = LineBreakMode.WordWrap
                    },
                    new Button { Text = "Neu laden", Command = new Command(async () => await LoadAsync()) },
                    _countLabel,
                    _status,
                    _list
                }
            }
        };

        Appearing += OnAppearing;
    }

    private async void OnAppearing(object? sender, EventArgs e)
    {
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        _status.Text = string.Empty;
        _items.Clear();

        try
        {
            var entries = await _supabaseService.GetOffeneArbeitsstundenZurFreigabeAsync();
            _reviewState.SetEntries(entries);

            foreach (var entry in _reviewState.Entries)
                _items.Add(entry);

            _list.ItemsSource = null;
            _list.ItemsSource = _items;
            _countLabel.Text = _items.Count > 0
                ? $"{_items.Count} offener Prüffall/Fälle"
                : "Keine offenen Prüffälle";

            if (_items.Count == 0)
                _status.Text = "Aktuell liegen keine offenen Arbeitsstunden vor.";

            if (Shell.Current is AdminShell shell)
                await shell.RefreshWorkhoursReviewMenuAsync();
        }
        catch (Exception ex)
        {
            _status.Text = ex.Message;
            _countLabel.Text = string.Empty;
        }
    }

    private async void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        var selected = e.CurrentSelection?.FirstOrDefault() as ArbeitsstundeDTO;
        if (selected == null)
            return;

        _list.SelectedItem = null;

        if (!_reviewState.SetCurrentById(selected.Id))
            return;

        await Shell.Current.GoToAsync(nameof(ArbeitsstundenReviewDetailPage));
    }

    private sealed class HeaderConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
        {
            if (value is not ArbeitsstundeDTO a) return string.Empty;
            var who = $"{a.Nachname} {a.Vorname}".Trim();
            return $"{who} – {a.Datum:dd.MM.yyyy} – {a.Stunden:0.##}h";
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture) => throw new NotSupportedException();
    }
}
