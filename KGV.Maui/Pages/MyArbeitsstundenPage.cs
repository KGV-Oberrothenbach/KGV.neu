using KGV.Core.Interfaces;
using KGV.Core.Models;
using KGV.Maui.State;

namespace KGV.Maui.Pages;

public sealed class MyArbeitsstundenPage : ContentPage
{
    private readonly ISupabaseService _supabaseService;
    private readonly UserContextState _state;

    private bool _loaded;
    private int? _currentSaisonId;

    private readonly Picker _forWhomPicker;
    private readonly DatePicker _datePicker;
    private readonly Entry _hoursEntry;
    private readonly Entry _descEntry;
    private readonly Button _addButton;

    private readonly CollectionView _list;
    private readonly Label _status;

    private readonly List<MemberOption> _options = new();
    private readonly List<ArbeitsstundeDTO> _items = new();

    public MyArbeitsstundenPage(ISupabaseService supabaseService, UserContextState state)
    {
        _supabaseService = supabaseService;
        _state = state;

        Title = "Meine Arbeitsstunden";

        _forWhomPicker = new Picker { Title = "Für wen?" };
        _forWhomPicker.ItemDisplayBinding = new Binding(nameof(MemberOption.Display));

        _datePicker = new DatePicker { Date = DateTime.Today };

        _hoursEntry = new Entry { Placeholder = "Stunden (z.B. 2,5)", Keyboard = Keyboard.Numeric };
        _descEntry = new Entry { Placeholder = "Art der Arbeit" };

        _addButton = new Button { Text = "Arbeitsstunde erfassen" };
        _addButton.Clicked += OnAddClicked;

        _status = new Label { TextColor = Colors.Red };

        _list = new CollectionView
        {
            ItemsSource = _items,
            ItemTemplate = new DataTemplate(() =>
            {
                var title = new Label { FontAttributes = FontAttributes.Bold };
                title.SetBinding(Label.TextProperty, new Binding(path: ".", converter: new ArbeitsstundeTitleConverter()));

                var sub = new Label { FontSize = 12, TextColor = Colors.Gray };
                sub.SetBinding(Label.TextProperty, new Binding(path: ".", converter: new ArbeitsstundeSubConverter()));

                return new VerticalStackLayout
                {
                    Padding = new Thickness(0, 8),
                    Children = { title, sub, new BoxView { HeightRequest = 1, Color = Colors.LightGray } }
                };
            })
        };

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = 24,
                Spacing = 12,
                Children =
                {
                    _forWhomPicker,
                    _datePicker,
                    _hoursEntry,
                    _descEntry,
                    _addButton,
                    _status,
                    new Label { Text = "Bisher erfasst", FontAttributes = FontAttributes.Bold },
                    _list
                }
            }
        };

        Appearing += OnAppearing;
    }

    private async void OnAppearing(object? sender, EventArgs e)
    {
        if (_loaded) return;
        _loaded = true;
        await InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        _status.Text = string.Empty;

        if (_state.CurrentMitgliedId == null || _state.CurrentMitgliedId.Value > int.MaxValue)
        {
            _status.Text = "MitgliedId fehlt.";
            return;
        }

        await EnsureSeasonAsync();
        await EnsureOptionsAsync();
        await LoadListAsync();
    }

    private async Task EnsureSeasonAsync()
    {
        if (_currentSaisonId.HasValue)
            return;

        var saisonen = await _supabaseService.GetSaisonRecordsAsync();
        if (saisonen == null || saisonen.Count == 0)
            return;

        var year = DateTime.Today.Year;
        var selected = saisonen.FirstOrDefault(s => s.Jahr == year) ?? saisonen.OrderByDescending(s => s.Jahr).First();
        _currentSaisonId = selected.Id;
    }

    private async Task EnsureOptionsAsync()
    {
        _options.Clear();

        var mainId = (int)_state.CurrentMitgliedId!.Value;
        _options.Add(new MemberOption(mainId, "Hauptmitglied"));

        if (_state.CurrentNebenMitgliedId != null && _state.CurrentNebenMitgliedId.Value <= int.MaxValue)
        {
            var neben = await _supabaseService.GetNebenmitgliedByHauptmitgliedIdAsync(mainId);
            if (neben != null)
            {
                _options.Add(new MemberOption(neben.Id, $"Nebenmitglied: {neben.Name} {neben.Vorname}".Trim()));
            }
        }

        _forWhomPicker.IsVisible = _options.Count > 1;
        _forWhomPicker.ItemsSource = _options;
        _forWhomPicker.SelectedItem = _options[0];
    }

    private async Task LoadListAsync()
    {
        _items.Clear();

        var ids = _options.Select(o => o.MitgliedId).Distinct().ToArray();
        var list = await _supabaseService.GetArbeitsstundenAsync(ids);

        foreach (var a in list)
            _items.Add(a);

        _list.ItemsSource = null;
        _list.ItemsSource = _items;
    }

    private async void OnAddClicked(object? sender, EventArgs e)
    {
        _status.Text = string.Empty;

        if (!_currentSaisonId.HasValue)
        {
            await DisplayAlert("Fehler", "Saison konnte nicht ermittelt werden.", "OK");
            return;
        }

        var opt = _forWhomPicker.SelectedItem as MemberOption;
        if (opt == null)
        {
            await DisplayAlert("Fehler", "Bitte " + '"' + "Für wen?" + '"' + " wählen.", "OK");
            return;
        }

        var desc = (_descEntry.Text ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(desc))
        {
            await DisplayAlert("Fehler", "Bitte Art der Arbeit angeben.", "OK");
            return;
        }

        if (!decimal.TryParse((_hoursEntry.Text ?? string.Empty).Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var hours))
        {
            await DisplayAlert("Fehler", "Stunden sind ungültig.", "OK");
            return;
        }

        if (hours <= 0 || hours > 24)
        {
            await DisplayAlert("Fehler", "Stunden müssen zwischen 0 und 24 liegen.", "OK");
            return;
        }

        _addButton.IsEnabled = false;
        try
        {
            var rec = new ArbeitsstundeRecord
            {
                MitgliedId = opt.MitgliedId,
                SaisonId = _currentSaisonId.Value,
                Datum = _datePicker.Date.Date,
                Stunden = hours,
                ArtDerArbeit = desc,
                Status = "offen",
                Freigegeben = false
            };

            var ok = await _supabaseService.AddArbeitsstundeAsync(rec);
            if (!ok)
            {
                await DisplayAlert("Fehler", "Speichern fehlgeschlagen.", "OK");
                return;
            }

            _hoursEntry.Text = string.Empty;
            _descEntry.Text = string.Empty;

            await LoadListAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Fehler", ex.Message, "OK");
        }
        finally
        {
            _addButton.IsEnabled = true;
        }
    }

    private sealed record MemberOption(int MitgliedId, string Display);

    private sealed class ArbeitsstundeTitleConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
        {
            if (value is not ArbeitsstundeDTO a) return string.Empty;
            var status = string.IsNullOrWhiteSpace(a.Status)
                ? (a.Freigegeben ? "genehmigt" : "offen")
                : a.Status;

            return $"{a.Datum:dd.MM.yyyy} – {a.Stunden:0.##}h – {status}";
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture) => throw new NotSupportedException();
    }

    private sealed class ArbeitsstundeSubConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
        {
            if (value is not ArbeitsstundeDTO a) return string.Empty;
            var who = $"{a.Nachname} {a.Vorname}".Trim();
            return string.IsNullOrWhiteSpace(who) ? a.Beschreibung : $"{who}: {a.Beschreibung}";
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture) => throw new NotSupportedException();
    }
}
