using KGV.Core.Interfaces;
using KGV.Core.Models;
using KGV.Maui.State;

namespace KGV.Maui.Pages;

public sealed class ArbeitsstundenReviewPage : ContentPage
{
    private readonly ISupabaseService _supabaseService;
    private readonly UserContextState _state;

    private readonly List<ArbeitsstundeDTO> _items = new();
    private readonly CollectionView _list;
    private readonly Label _status;
    private bool _loaded;

    public ArbeitsstundenReviewPage(ISupabaseService supabaseService, UserContextState state)
    {
        _supabaseService = supabaseService;
        _state = state;

        Title = "Arbeitsstunden prüfen";

        _status = new Label { TextColor = Colors.Red };

        _list = new CollectionView
        {
            ItemsSource = _items,
            ItemTemplate = new DataTemplate(() =>
            {
                var header = new Label { FontAttributes = FontAttributes.Bold };
                header.SetBinding(Label.TextProperty, new Binding(path: ".", converter: new HeaderConverter()));

                var desc = new Label { FontSize = 12, TextColor = Colors.Gray };
                desc.SetBinding(Label.TextProperty, nameof(ArbeitsstundeDTO.Beschreibung));

                var approve = new Button { Text = "Genehmigen", BackgroundColor = Colors.LightGreen };
                approve.Clicked += OnApproveClicked;
                approve.SetBinding(Button.CommandParameterProperty, new Binding(path: "."));

                var reject = new Button { Text = "Ablehnen", BackgroundColor = Colors.LightPink };
                reject.Clicked += OnRejectClicked;
                reject.SetBinding(Button.CommandParameterProperty, new Binding(path: "."));

                return new VerticalStackLayout
                {
                    Padding = new Thickness(0, 8),
                    Spacing = 6,
                    Children =
                    {
                        header,
                        desc,
                        new HorizontalStackLayout { Spacing = 12, Children = { approve, reject } },
                        new BoxView { HeightRequest = 1, Color = Colors.LightGray }
                    }
                };
            })
        };

        Content = new VerticalStackLayout
        {
            Padding = 24,
            Spacing = 12,
            Children =
            {
                new Button { Text = "Neu laden", Command = new Command(async () => await LoadAsync()) },
                _status,
                _list
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
        _status.Text = string.Empty;
        _items.Clear();

        try
        {
            var groups = await _supabaseService.GetUnapprovedArbeitsstundenByMitgliedAsync();
            foreach (var g in groups)
            {
                var list = await _supabaseService.GetArbeitsstundenAsync(g.MitgliedId);
                foreach (var a in list)
                {
                    if (a.Freigegeben) continue;

                    var status = (a.Status ?? string.Empty).Trim();
                    if (!string.IsNullOrEmpty(status) && !status.Equals("offen", StringComparison.OrdinalIgnoreCase))
                        continue;

                    _items.Add(a);
                }
            }

            _list.ItemsSource = null;
            _list.ItemsSource = _items;

            if (_items.Count == 0)
                _status.Text = "Keine offenen Arbeitsstunden.";
        }
        catch (Exception ex)
        {
            _status.Text = ex.Message;
        }
    }

    private async void OnApproveClicked(object? sender, EventArgs e)
    {
        if ((sender as Button)?.CommandParameter is not ArbeitsstundeDTO dto)
            return;

        if (!TryGetApproverId(out var approverId))
            return;

        await UpdateStatusAsync(dto, approverId, approved: true);
    }

    private async void OnRejectClicked(object? sender, EventArgs e)
    {
        if ((sender as Button)?.CommandParameter is not ArbeitsstundeDTO dto)
            return;

        if (!TryGetApproverId(out var approverId))
            return;

        await UpdateStatusAsync(dto, approverId, approved: false);
    }

    private bool TryGetApproverId(out int approverId)
    {
        approverId = 0;
        if (_state.CurrentMitgliedId == null || _state.CurrentMitgliedId.Value > int.MaxValue)
        {
            _ = DisplayAlert("Fehler", "Genehmiger-MitgliedId fehlt.", "OK");
            return false;
        }

        approverId = (int)_state.CurrentMitgliedId.Value;
        return true;
    }

    private async Task UpdateStatusAsync(ArbeitsstundeDTO dto, int approverId, bool approved)
    {
        try
        {
            var now = DateTime.UtcNow;
            var record = new ArbeitsstundeRecord
            {
                Id = dto.Id,
                MitgliedId = dto.MitgliedId,
                SaisonId = dto.SaisonId,
                Datum = dto.Datum.Date,
                Stunden = dto.Stunden,
                ArtDerArbeit = dto.Beschreibung,
                Status = approved ? "genehmigt" : "abgelehnt",
                Freigegeben = approved,
                GenehmigtAm = now,
                GenehmigtVon = approverId
            };

            var ok = await _supabaseService.UpdateArbeitsstundeAsync(record);
            if (!ok)
            {
                await DisplayAlert("Fehler", "Update fehlgeschlagen.", "OK");
                return;
            }

            await LoadAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Fehler", ex.Message, "OK");
        }
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
