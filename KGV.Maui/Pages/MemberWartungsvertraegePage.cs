using KGV.Core.Interfaces;
using KGV.Core.Models;
using KGV.Maui.State;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace KGV.Maui.Pages;

public sealed class MemberWartungsvertraegePage : ContentPage
{
    private readonly ISupabaseService _supabaseService;
    private readonly MemberContextState _memberContextState;
    private readonly ObservableCollection<MemberWartungsvertragItem> _items = new();
    private readonly Label _headlineLabel;
    private readonly Label _countLabel;
    private readonly Label _statusLabel;
    private readonly CollectionView _itemsView;
    private bool _isBusy;

    public MemberWartungsvertraegePage(ISupabaseService supabaseService, MemberContextState memberContextState)
    {
        _supabaseService = supabaseService;
        _memberContextState = memberContextState;
        Title = "Wartungsverträge";

        _headlineLabel = new Label { FontSize = 24, FontAttributes = FontAttributes.Bold };
        _countLabel = new Label { FontSize = 12, TextColor = Colors.Gray };
        _statusLabel = new Label { TextColor = Colors.DarkSlateBlue, LineBreakMode = LineBreakMode.WordWrap };

        var refreshButton = new Button { Text = "Aktualisieren" };
        refreshButton.Clicked += async (_, _) => await LoadAsync();

        _itemsView = new CollectionView
        {
            SelectionMode = SelectionMode.Single,
            ItemsSource = _items,
            EmptyView = new Label
            {
                Text = "Für dieses Mitglied liegen aktuell keine aktiven Wartungsverträge vor.",
                TextColor = Colors.Gray
            },
            ItemTemplate = new DataTemplate(() =>
            {
                var title = new Label { FontAttributes = FontAttributes.Bold, LineBreakMode = LineBreakMode.WordWrap };
                title.SetBinding(Label.TextProperty, nameof(MemberWartungsvertragItem.Titel));

                var validity = new Label { FontSize = 12, TextColor = Colors.Gray, LineBreakMode = LineBreakMode.WordWrap };
                validity.SetBinding(Label.TextProperty, nameof(MemberWartungsvertragItem.GueltigkeitText));

                var usage = new Label { FontSize = 12, TextColor = Colors.DarkSlateBlue, LineBreakMode = LineBreakMode.WordWrap };
                usage.SetBinding(Label.TextProperty, nameof(MemberWartungsvertragItem.BelegungText));

                return new Border
                {
                    Stroke = Colors.LightGray,
                    Padding = 12,
                    Margin = new Thickness(0, 0, 0, 8),
                    Content = new VerticalStackLayout
                    {
                        Spacing = 4,
                        Children = { title, validity, usage }
                    }
                };
            })
        };

        _itemsView.SelectionChanged += async (_, e) =>
        {
            var selected = e.CurrentSelection?.Count > 0 ? e.CurrentSelection[0] as MemberWartungsvertragItem : null;
            _itemsView.SelectedItem = null;
            if (selected == null)
                return;

            await Shell.Current.GoToAsync($"{nameof(WartungsvertragDetailPage)}?wartungsvertragId={selected.Id}");
        };

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = 24,
                Spacing = 12,
                Children =
                {
                    _headlineLabel,
                    new Label
                    {
                        Text = "Mitgliedsbezogene ReadOnly-Übersicht der aktuell aktiven Wartungsverträge. Antippen öffnet dieselbe Detailansicht wie im globalen Bereich.",
                        TextColor = Colors.Gray,
                        LineBreakMode = LineBreakMode.WordWrap
                    },
                    refreshButton,
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
            _statusLabel.Text = "Daten werden geladen.";
            _items.Clear();

            var selectedMember = _memberContextState.SelectedMember;
            if (selectedMember?.Id is not > 0)
            {
                _headlineLabel.Text = "Wartungsverträge";
                _countLabel.Text = string.Empty;
                _statusLabel.Text = "Bitte zuerst ein Mitglied auswählen.";
                return;
            }

            var member = await _supabaseService.GetMitgliedByIdAsync(selectedMember.Id);
            var displayName = member == null
                ? $"Mitglied #{selectedMember.Id}"
                : BuildDisplayName(member.Vorname, member.Name, selectedMember.Id);
            _headlineLabel.Text = $"Wartungsverträge von {displayName}";

            var items = await _supabaseService.GetWartungsvertraegeForMitgliedAsync(selectedMember.Id);
            foreach (var item in items)
                _items.Add(item);

            _countLabel.Text = items.Count > 0
                ? $"{items.Count} aktive Zuordnung(en)"
                : "Keine aktiven Wartungsverträge.";
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
        }
    }

    private static string BuildDisplayName(string? vorname, string? nachname, int fallbackId)
    {
        var displayName = $"{vorname} {nachname}".Trim();
        return string.IsNullOrWhiteSpace(displayName) ? $"Mitglied #{fallbackId}" : displayName;
    }
}
