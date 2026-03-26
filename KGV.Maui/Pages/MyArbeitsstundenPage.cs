using KGV.Core.Interfaces;
using KGV.Core.Models;
using KGV.Maui.State;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace KGV.Maui.Pages;

public sealed class MyArbeitsstundenPage : ContentPage
{
    private readonly ISupabaseService _supabaseService;
    private readonly UserContextState _state;
    private readonly MemberContextState _memberContextState;

    private bool _isLoading;
    private bool _loadScheduled;
    private bool _isNavigating;

    private readonly CollectionView _list;
    private readonly Label _status;
    private readonly Label _summarySaisonLabel;
    private readonly Label _summarySollLabel;
    private readonly Label _summaryGeleistetLabel;
    private readonly Label _summaryOffenLabel;

    private readonly List<MemberOption> _options = new();
    private readonly List<ArbeitsstundeDTO> _items = new();

    public MyArbeitsstundenPage(ISupabaseService supabaseService, UserContextState state, MemberContextState memberContextState)
    {
        _supabaseService = supabaseService;
        _state = state;
        _memberContextState = memberContextState;

        Title = "Meine Arbeitsstunden";

        _status = new Label { TextColor = Colors.DarkSlateBlue, LineBreakMode = LineBreakMode.WordWrap };

        _summarySaisonLabel = new Label { FontSize = 12, TextColor = Colors.Gray };
        _summarySollLabel = new Label { FontSize = 24, FontAttributes = FontAttributes.Bold, HorizontalTextAlignment = TextAlignment.Center };
        _summaryGeleistetLabel = new Label { FontSize = 24, FontAttributes = FontAttributes.Bold, HorizontalTextAlignment = TextAlignment.Center };
        _summaryOffenLabel = new Label { FontSize = 24, FontAttributes = FontAttributes.Bold, HorizontalTextAlignment = TextAlignment.Center };

        var newButton = new Button { Text = "Neu erfassen" };
        newButton.Clicked += async (_, _) => await Shell.Current.GoToAsync($"{nameof(ArbeitsstundenEditorPage)}?entryId=0");

        _list = new CollectionView
        {
            SelectionMode = SelectionMode.Single,
            ItemsSource = _items,
            EmptyView = new Label
            {
                Text = "Aktuell liegen noch keine eigenen Arbeitsstunden vor.",
                TextColor = Colors.Gray
            },
            ItemTemplate = new DataTemplate(() =>
            {
                var title = new Label { FontAttributes = FontAttributes.Bold };
                title.SetBinding(Label.TextProperty, new Binding(path: ".", converter: new ArbeitsstundeTitleConverter()));

                var sub = new Label { FontSize = 12, TextColor = Colors.Gray, LineBreakMode = LineBreakMode.WordWrap };
                sub.SetBinding(Label.TextProperty, new Binding(path: ".", converter: new ArbeitsstundeSubConverter()));

                var access = new Label { FontSize = 12, TextColor = Colors.DarkSlateBlue };
                access.SetBinding(Label.TextProperty, new Binding(path: ".", converter: new ArbeitsstundeAccessConverter()));

                return new Border
                {
                    Stroke = Colors.LightGray,
                    Padding = 12,
                    Margin = new Thickness(0, 0, 0, 8),
                    Content = new VerticalStackLayout
                    {
                        Spacing = 4,
                        Children = { title, sub, access }
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
                    new Label
                    {
                        Text = "Die Übersicht bleibt bewusst ruhig. Erfassen und Bearbeiten öffnen als eigener mobiler Schritt statt im selben Sammelformular.",
                        TextColor = Colors.Gray,
                        LineBreakMode = LineBreakMode.WordWrap
                    },
                    newButton,
                    _status,
                    new Label { Text = "Vorhandene Arbeitsstunden", FontAttributes = FontAttributes.Bold },
                    _list
                }
            }
        };

        Appearing += OnAppearing;
    }

    private void OnAppearing(object? sender, EventArgs e)
    {
        if (_isLoading || _loadScheduled)
            return;

        _loadScheduled = true;
        Dispatcher.Dispatch(async () =>
        {
            await Task.Yield();
            _loadScheduled = false;
            await RefreshAsync();
        });
    }

    private async Task RefreshAsync()
    {
        if (_isLoading)
            return;

        _isLoading = true;
        _status.Text = string.Empty;
        try
        {
            var contextMemberId = GetContextMemberId();
            if (!contextMemberId.HasValue)
            {
                _status.Text = "MitgliedId fehlt.";
                return;
            }

            await EnsureOptionsAsync();
            await LoadSummaryAsync();
            await LoadListAsync();
        }
        finally
        {
            _isLoading = false;
        }
    }

    private async Task EnsureOptionsAsync()
    {
        _options.Clear();

        var contextMemberId = GetContextMemberId();
        if (!contextMemberId.HasValue)
            return;

        var mainId = contextMemberId.Value;
        var selectedMember = _memberContextState.SelectedMember;
        var useSelectedMemberContext = _state.CurrentUserContext?.Role is KGV.Core.Security.UserRole.Admin or KGV.Core.Security.UserRole.Vorstand
            && selectedMember?.Id is > 0;

        var mainLabel = useSelectedMemberContext && selectedMember?.IstHauptmitglied == false
            ? "Ausgewähltes Mitglied"
            : "Hauptmitglied";

        _options.Add(new MemberOption(mainId, mainLabel));

        var allowNebenmitglied = !useSelectedMemberContext || selectedMember?.IstHauptmitglied != false;
        if (allowNebenmitglied)
        {
            var neben = await _supabaseService.GetNebenmitgliedByHauptmitgliedIdAsync(mainId);
            if (neben != null)
                _options.Add(new MemberOption(neben.Id, $"Nebenmitglied: {neben.Name} {neben.Vorname}".Trim()));
        }
    }

    private async Task LoadSummaryAsync()
    {
        var contextMemberId = GetContextMemberId();
        if (!contextMemberId.HasValue)
        {
            SetSummary(null);
            return;
        }

        var summary = await _supabaseService.GetPflichtstundenUebersichtForMitgliedAsync(contextMemberId.Value);
        SetSummary(summary);
    }

    private async Task LoadListAsync()
    {
        _items.Clear();

        var ids = _options.Select(o => o.MitgliedId).Distinct().ToArray();
        var list = await _supabaseService.GetArbeitsstundenAsync(ids);

        foreach (var a in list.OrderByDescending(x => x.Datum).ThenByDescending(x => x.Id))
            _items.Add(a);

        _list.ItemsSource = null;
        _list.ItemsSource = _items;
        _list.SelectedItem = null;
    }

    private async void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        var selected = e.CurrentSelection?.FirstOrDefault() as ArbeitsstundeDTO;
        if (selected == null)
            return;

        if (_isNavigating)
        {
            _list.SelectedItem = null;
            return;
        }

        _isNavigating = true;
        _list.SelectedItem = null;
        try
        {
            await Shell.Current.GoToAsync($"{nameof(ArbeitsstundenEditorPage)}?entryId={selected.Id}");
        }
        finally
        {
            _isNavigating = false;
        }
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

    private int? GetContextMemberId()
    {
        if (_state.CurrentUserContext?.Role is KGV.Core.Security.UserRole.Admin or KGV.Core.Security.UserRole.Vorstand)
        {
            var selectedId = _memberContextState.SelectedMember?.Id;
            if (selectedId is > 0)
                return selectedId.Value;
        }

        return _state.CurrentMitgliedId is > 0 and <= int.MaxValue
            ? (int)_state.CurrentMitgliedId.Value
            : null;
    }

    private sealed record MemberOption(int MitgliedId, string Display);

    private sealed class ArbeitsstundeTitleConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
        {
            if (value is not ArbeitsstundeDTO a) return string.Empty;
            return $"{a.Datum:dd.MM.yyyy} – {a.Stunden:0.##}h";
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture) => throw new NotSupportedException();
    }

    private sealed class ArbeitsstundeSubConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
        {
            if (value is not ArbeitsstundeDTO a) return string.Empty;
            var who = $"{a.Nachname} {a.Vorname}".Trim();
            var description = string.IsNullOrWhiteSpace(a.Beschreibung) ? "ohne Beschreibung" : a.Beschreibung.Trim();
            return string.IsNullOrWhiteSpace(who) ? description : $"{who}: {description}";
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture) => throw new NotSupportedException();
    }

    private sealed class ArbeitsstundeAccessConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
        {
            if (value is not ArbeitsstundeDTO a) return string.Empty;
            return a.Freigegeben
                ? "Freigegeben – öffnet nur als Ansicht"
                : "Noch nicht freigegeben – öffnet zur Bearbeitung";
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture) => throw new NotSupportedException();
    }
}
