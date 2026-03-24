using KGV.Core.Interfaces;
using KGV.Core.Models;
using KGV.Maui.State;

namespace KGV.Maui.Pages;

public sealed class MyArbeitsstundenPage : ContentPage
{
    private readonly ISupabaseService _supabaseService;
    private readonly UserContextState _state;

    private bool _isLoading;
    private int? _currentSaisonId;
    private ArbeitsstundeDTO? _selectedArbeitsstunde;

    private readonly Picker _forWhomPicker;
    private readonly DatePicker _datePicker;
    private readonly Entry _hoursEntry;
    private readonly Editor _descEditor;
    private readonly Button _addButton;
    private readonly Button _cancelEditButton;

    private readonly CollectionView _list;
    private readonly Label _status;
    private readonly Label _editorHint;
    private readonly Label _summarySaisonLabel;
    private readonly Label _summarySollLabel;
    private readonly Label _summaryGeleistetLabel;
    private readonly Label _summaryOffenLabel;

    private readonly List<MemberOption> _options = new();
    private readonly List<ArbeitsstundeDTO> _items = new();

    public MyArbeitsstundenPage(ISupabaseService supabaseService, UserContextState state)
    {
        _supabaseService = supabaseService;
        _state = state;

        Title = "Arbeitsstunden erfassen";

        _forWhomPicker = new Picker { Title = "Für wen?" };
        _forWhomPicker.ItemDisplayBinding = new Binding(nameof(MemberOption.Display));

        _datePicker = new DatePicker { Date = DateTime.Today };

        _hoursEntry = new Entry { Placeholder = "Stunden (z.B. 2,5)", Keyboard = Keyboard.Numeric };
        _descEditor = new Editor { Placeholder = "Art der Arbeit", AutoSize = EditorAutoSizeOption.TextChanges, HeightRequest = 110 };

        _addButton = new Button { Text = "Arbeitsstunde erfassen" };
        _addButton.Clicked += OnAddClicked;

        _cancelEditButton = new Button { Text = "Bearbeiten abbrechen", IsVisible = false };
        _cancelEditButton.Clicked += (_, _) => ResetEditor();

        _status = new Label { TextColor = Colors.DarkSlateBlue, LineBreakMode = LineBreakMode.WordWrap };
        _editorHint = new Label { TextColor = Colors.Gray, LineBreakMode = LineBreakMode.WordWrap };

        _summarySaisonLabel = new Label { FontSize = 12, TextColor = Colors.Gray };
        _summarySollLabel = new Label { FontSize = 24, FontAttributes = FontAttributes.Bold, HorizontalTextAlignment = TextAlignment.Center };
        _summaryGeleistetLabel = new Label { FontSize = 24, FontAttributes = FontAttributes.Bold, HorizontalTextAlignment = TextAlignment.Center };
        _summaryOffenLabel = new Label { FontSize = 24, FontAttributes = FontAttributes.Bold, HorizontalTextAlignment = TextAlignment.Center };

        _list = new CollectionView
        {
            SelectionMode = SelectionMode.Single,
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
        _list.SelectionChanged += OnSelectionChanged;

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = 24,
                Spacing = 12,
                Children =
                {
                    new Label { Text = "Pflichtstunden-Überblick", FontAttributes = FontAttributes.Bold },
                    _summarySaisonLabel,
                    new Grid
                    {
                        ColumnDefinitions = new ColumnDefinitionCollection
                        {
                            new ColumnDefinition(GridLength.Star),
                            new ColumnDefinition(GridLength.Star),
                            new ColumnDefinition(GridLength.Star)
                        },
                        ColumnSpacing = 12,
                        Children =
                        {
                            CreateSummaryCard("Soll", _summarySollLabel, 0),
                            CreateSummaryCard("Geleistet", _summaryGeleistetLabel, 1),
                            CreateSummaryCard("Offen", _summaryOffenLabel, 2)
                        }
                    },
                    _forWhomPicker,
                    _datePicker,
                    _hoursEntry,
                    _descEditor,
                    _editorHint,
                    new HorizontalStackLayout
                    {
                        Spacing = 8,
                        Children = { _cancelEditButton, _addButton }
                    },
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
        await RefreshAsync();
    }

    private async Task RefreshAsync()
    {
        if (_isLoading)
            return;

        _isLoading = true;
        _status.Text = string.Empty;
        try
        {
            if (_state.CurrentMitgliedId == null || _state.CurrentMitgliedId.Value > int.MaxValue)
            {
                _status.Text = "MitgliedId fehlt.";
                return;
            }

            await EnsureSeasonAsync();
            await EnsureOptionsAsync();
            await LoadSummaryAsync();
            await LoadListAsync();
            ResetEditor();
        }
        finally
        {
            _isLoading = false;
        }
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

    private async Task LoadSummaryAsync()
    {
        if (_state.CurrentMitgliedId == null || _state.CurrentMitgliedId.Value > int.MaxValue)
        {
            SetSummary(null);
            return;
        }

        var summary = await _supabaseService.GetPflichtstundenUebersichtForMitgliedAsync((int)_state.CurrentMitgliedId.Value);
        SetSummary(summary);
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
        _list.SelectedItem = null;
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

        var desc = (_descEditor.Text ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(desc))
        {
            await DisplayAlert("Fehler", "Bitte Art der Arbeit angeben.", "OK");
            return;
        }

        if (!TryParseHours(_hoursEntry.Text, out var hours))
        {
            await DisplayAlert("Fehler", "Stunden sind ungültig.", "OK");
            return;
        }

        if (hours <= 0)
        {
            await DisplayAlert("Fehler", "Stunden müssen größer als 0 sein.", "OK");
            return;
        }

        _addButton.IsEnabled = false;
        try
        {
            var rec = new ArbeitsstundeRecord
            {
                Id = _selectedArbeitsstunde?.Id ?? 0,
                MitgliedId = opt.MitgliedId,
                SaisonId = _currentSaisonId.Value,
                Datum = _datePicker.Date.Date,
                Stunden = hours,
                ArtDerArbeit = desc,
                Status = _selectedArbeitsstunde?.Status,
                Freigegeben = _selectedArbeitsstunde?.Freigegeben ?? false,
                GenehmigtAm = _selectedArbeitsstunde?.FreigegebenAm,
                GenehmigtVon = _selectedArbeitsstunde?.FreigegebenVonId
            };

            var ok = _selectedArbeitsstunde == null
                ? await _supabaseService.AddArbeitsstundeAsync(rec)
                : await _supabaseService.UpdateArbeitsstundeAsync(rec);
            if (!ok)
            {
                await DisplayAlert("Fehler", "Speichern fehlgeschlagen.", "OK");
                return;
            }

            _hoursEntry.Text = string.Empty;
            _descEditor.Text = string.Empty;

            await LoadSummaryAsync();
            await LoadListAsync();
            ResetEditor();
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

    private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        var selected = e.CurrentSelection?.FirstOrDefault() as ArbeitsstundeDTO;
        if (selected == null)
        {
            ResetEditor();
            return;
        }

        _selectedArbeitsstunde = selected;
        _datePicker.Date = selected.Datum.Date;
        _hoursEntry.Text = selected.Stunden.ToString("0.##", System.Globalization.CultureInfo.CurrentCulture);
        _descEditor.Text = selected.Beschreibung ?? string.Empty;
        _forWhomPicker.SelectedItem = _options.FirstOrDefault(x => x.MitgliedId == selected.MitgliedId) ?? _options.FirstOrDefault();
        _addButton.Text = "Arbeitsstunde speichern";
        _cancelEditButton.IsVisible = true;
        _editorHint.Text = $"Bearbeite Arbeitsstunde vom {selected.Datum:dd.MM.yyyy}. Der bestehende Freigabestatus bleibt erhalten.";
    }

    private void ResetEditor()
    {
        _selectedArbeitsstunde = null;
        _datePicker.Date = DateTime.Today;
        _hoursEntry.Text = string.Empty;
        _descEditor.Text = string.Empty;
        _addButton.Text = "Arbeitsstunde erfassen";
        _cancelEditButton.IsVisible = false;
        _editorHint.Text = "Tippe einen bestehenden Eintrag an, um ihn im selben Formular zu bearbeiten.";
        _list.SelectedItem = null;

        if (_options.Count > 0)
            _forWhomPicker.SelectedItem = _options[0];
    }

    private void SetSummary(PflichtstundenUebersichtRecord? summary)
    {
        _summarySaisonLabel.Text = summary?.SaisonJahr is > 0
            ? $"Saison {summary.SaisonJahr}"
            : "Aktuell keine Pflichtstundenübersicht verfügbar";

        _summarySollLabel.Text = FormatHours(summary?.PflichtstundenSoll);
        _summaryGeleistetLabel.Text = FormatHours(summary?.GeleisteteStunden);
        _summaryOffenLabel.Text = FormatHours(summary?.OffeneStunden);
    }

    private static Border CreateSummaryCard(string title, Label valueLabel, int column)
    {
        var titleLabel = new Label
        {
            Text = title,
            FontSize = 12,
            TextColor = Colors.Gray,
            HorizontalTextAlignment = TextAlignment.Center
        };

        var stack = new VerticalStackLayout
        {
            Spacing = 4,
            Children = { titleLabel, valueLabel }
        };

        var border = new Border
        {
            Stroke = Colors.LightGray,
            Padding = 12,
            Content = stack
        };

        Grid.SetColumn(border, column);
        return border;
    }

    private static string FormatHours(decimal? value)
    {
        return value.HasValue
            ? value.Value.ToString("0.##", System.Globalization.CultureInfo.CurrentCulture)
            : "–";
    }

    private static bool TryParseHours(string? input, out decimal value)
    {
        value = 0;
        if (string.IsNullOrWhiteSpace(input))
            return false;

        if (decimal.TryParse(input, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.CurrentCulture, out value))
            return true;

        if (decimal.TryParse(input, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out value))
            return true;

        var normalized = input.Replace(',', '.');
        return decimal.TryParse(normalized, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out value);
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
