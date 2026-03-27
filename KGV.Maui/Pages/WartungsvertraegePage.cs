using KGV.Core.Interfaces;
using KGV.Core.Models;
using KGV.Core.Security;
using KGV.Maui.State;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace KGV.Maui.Pages;

public sealed class WartungsvertraegePage : ContentPage
{
    private readonly ISupabaseService _supabaseService;
    private readonly UserContextState _userContextState;
    private readonly ObservableCollection<WartungsvertragOverviewItem> _items = new();
    private readonly Label _countLabel;
    private readonly Label _statusLabel;
    private readonly CollectionView _itemsView;
    private readonly Button _refreshButton;
    private readonly Button _newButton;
    private bool _isBusy;
    private bool _canManage;

    public WartungsvertraegePage(ISupabaseService supabaseService, UserContextState userContextState)
    {
        _supabaseService = supabaseService;
        _userContextState = userContextState;
        Title = "Wartungsverträge";

        _countLabel = new Label { FontSize = 12, TextColor = Colors.Gray };
        _statusLabel = new Label { TextColor = Colors.DarkSlateBlue, LineBreakMode = LineBreakMode.WordWrap };

        _refreshButton = new Button { Text = "Aktualisieren" };
        _refreshButton.Clicked += async (_, _) => await LoadAsync();
        _newButton = new Button { Text = "Neu", IsVisible = false };
        _newButton.Clicked += async (_, _) => await Shell.Current.GoToAsync(nameof(WartungsvertragEditorPage));

        _itemsView = new CollectionView
        {
            SelectionMode = SelectionMode.Single,
            ItemsSource = _items,
            EmptyView = new Label
            {
                Text = "Aktuell liegen keine Wartungsverträge vor.",
                TextColor = Colors.Gray
            },
            ItemTemplate = new DataTemplate(() =>
            {
                var title = new Label { FontAttributes = FontAttributes.Bold, LineBreakMode = LineBreakMode.WordWrap };
                title.SetBinding(Label.TextProperty, nameof(WartungsvertragOverviewItem.Titel));

                var description = new Label { FontSize = 12, TextColor = Colors.Gray, LineBreakMode = LineBreakMode.WordWrap };
                description.SetBinding(Label.TextProperty, nameof(WartungsvertragOverviewItem.Kurzbeschreibung));

                var usage = new Label { FontSize = 12, TextColor = Colors.DarkSlateBlue };
                usage.SetBinding(Label.TextProperty, nameof(WartungsvertragOverviewItem.BelegungText));

                return new Border
                {
                    Stroke = Colors.LightGray,
                    Padding = 12,
                    Margin = new Thickness(0, 0, 0, 8),
                    Content = new VerticalStackLayout
                    {
                        Spacing = 4,
                        Children = { title, description, usage }
                    }
                };
            })
        };

        _itemsView.SelectionChanged += async (_, e) =>
        {
            var selected = e.CurrentSelection?.Count > 0 ? e.CurrentSelection[0] as WartungsvertragOverviewItem : null;
            _itemsView.SelectedItem = null;
            if (selected == null)
                return;

            var adminMode = _canManage ? "&adminMode=1" : string.Empty;
            await Shell.Current.GoToAsync($"{nameof(WartungsvertragDetailPage)}?wartungsvertragId={selected.Id}{adminMode}");
        };

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = 24,
                Spacing = 12,
                Children =
                {
                    new Label { Text = "Wartungsverträge", FontSize = 24, FontAttributes = FontAttributes.Bold },
                    new Label
                    {
                        Text = "Globale mobile Übersicht der Wartungsverträge mit Kontingent und aktueller Belegung. Antippen öffnet die ReadOnly-Detailansicht.",
                        TextColor = Colors.Gray,
                        LineBreakMode = LineBreakMode.WordWrap
                    },
                    new HorizontalStackLayout
                    {
                        Spacing = 8,
                        Children = { _refreshButton, _newButton }
                    },
                    _countLabel,
                    _statusLabel,
                    _itemsView
                }
            }
        };

        Appearing += async (_, _) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        if (_isBusy)
            return;

        _isBusy = true;
        try
        {
            _canManage = _userContextState.CurrentUserContext?.Role is UserRole.Admin or UserRole.Vorstand;
            _newButton.IsVisible = _canManage;
            SetBusyState(true);
            _statusLabel.Text = "Daten werden geladen.";
            _items.Clear();

            var items = await _supabaseService.GetWartungsvertraegeOverviewAsync();
            foreach (var item in items)
                _items.Add(item);

            _countLabel.Text = items.Count > 0
                ? $"{items.Count} Wartungsvertrag/Verträge"
                : "Aktuell liegen keine Wartungsverträge vor.";
            _statusLabel.Text = string.Empty;
        }
        catch (Exception ex)
        {
            _countLabel.Text = string.Empty;
            _statusLabel.Text = ex.Message;
        }
        finally
        {
            _isBusy = false;
            SetBusyState(false);
        }
    }

    private void SetBusyState(bool isBusy)
    {
        _refreshButton.IsEnabled = !isBusy;
        _newButton.IsEnabled = !isBusy;
        _itemsView.IsEnabled = !isBusy;
    }
}
