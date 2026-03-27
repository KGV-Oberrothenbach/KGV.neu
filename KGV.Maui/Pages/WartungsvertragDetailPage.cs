using KGV.Core.Interfaces;
using KGV.Core.Models;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace KGV.Maui.Pages;

public sealed class WartungsvertragDetailPage : ContentPage, IQueryAttributable
{
    private readonly ISupabaseService _supabaseService;
    private readonly ObservableCollection<WartungsvertragAssignedMemberItem> _members = new();
    private readonly Label _titleLabel;
    private readonly Label _descriptionLabel;
    private readonly Label _maxKontingentLabel;
    private readonly Label _belegtLabel;
    private readonly Label _freiLabel;
    private readonly Label _statusLabel;
    private readonly Label _membersEmptyLabel;
    private readonly CollectionView _membersView;
    private long _wartungsvertragId;
    private bool _isBusy;
    private long _lastLoadedId;

    public WartungsvertragDetailPage(ISupabaseService supabaseService)
    {
        _supabaseService = supabaseService;
        Title = "Wartungsvertrag";

        _titleLabel = new Label { FontSize = 24, FontAttributes = FontAttributes.Bold, LineBreakMode = LineBreakMode.WordWrap };
        _descriptionLabel = new Label { TextColor = Colors.Gray, LineBreakMode = LineBreakMode.WordWrap };
        _maxKontingentLabel = new Label();
        _belegtLabel = new Label();
        _freiLabel = new Label();
        _statusLabel = new Label { TextColor = Colors.DarkSlateBlue, LineBreakMode = LineBreakMode.WordWrap };
        _membersEmptyLabel = new Label { Text = "Aktuell keine aktiven Mitgliedszuordnungen.", TextColor = Colors.Gray, IsVisible = false };

        var refreshButton = new Button { Text = "Aktualisieren" };
        refreshButton.Clicked += async (_, _) => await LoadAsync(forceReload: true);

        _membersView = new CollectionView
        {
            SelectionMode = SelectionMode.None,
            ItemsSource = _members,
            ItemTemplate = new DataTemplate(() =>
            {
                var title = new Label { FontAttributes = FontAttributes.Bold, LineBreakMode = LineBreakMode.WordWrap };
                title.SetBinding(Label.TextProperty, nameof(WartungsvertragAssignedMemberItem.DisplayName));

                var subtitle = new Label { FontSize = 12, TextColor = Colors.Gray, LineBreakMode = LineBreakMode.WordWrap };
                subtitle.SetBinding(Label.TextProperty, nameof(WartungsvertragAssignedMemberItem.Subtitle));

                return new Border
                {
                    Stroke = Colors.LightGray,
                    Padding = 12,
                    Margin = new Thickness(0, 0, 0, 8),
                    Content = new VerticalStackLayout
                    {
                        Spacing = 4,
                        Children = { title, subtitle }
                    }
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
                    _titleLabel,
                    _descriptionLabel,
                    refreshButton,
                    CreateInfoSection("Max. Kontingent", _maxKontingentLabel),
                    CreateInfoSection("Belegt", _belegtLabel),
                    CreateInfoSection("Frei", _freiLabel),
                    _statusLabel,
                    new Label { Text = "Aktive Zuordnungen", FontAttributes = FontAttributes.Bold, FontSize = 18 },
                    _membersEmptyLabel,
                    _membersView
                }
            }
        };
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (!TryGetLongValue(query, "wartungsvertragId", out var wartungsvertragId))
            return;

        _wartungsvertragId = wartungsvertragId;
        _ = LoadAsync(forceReload: true);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadAsync(forceReload: false);
    }

    private async Task LoadAsync(bool forceReload)
    {
        if (_isBusy || _wartungsvertragId <= 0)
            return;

        if (!forceReload && _lastLoadedId == _wartungsvertragId)
            return;

        _isBusy = true;
        try
        {
            _statusLabel.Text = "Daten werden geladen.";
            _members.Clear();
            _membersEmptyLabel.IsVisible = false;

            var detail = await _supabaseService.GetWartungsvertragDetailAsync(_wartungsvertragId);
            if (detail == null)
            {
                _titleLabel.Text = "Wartungsvertrag";
                _descriptionLabel.Text = "Der ausgewählte Wartungsvertrag konnte nicht geladen werden.";
                _maxKontingentLabel.Text = "-";
                _belegtLabel.Text = "-";
                _freiLabel.Text = "-";
                _statusLabel.Text = string.Empty;
                _lastLoadedId = 0;
                return;
            }

            _titleLabel.Text = detail.Titel;
            _descriptionLabel.Text = detail.BeschreibungText;
            _maxKontingentLabel.Text = detail.MaxKontingent.ToString();
            _belegtLabel.Text = detail.Belegt.ToString();
            _freiLabel.Text = detail.Frei.ToString();

            foreach (var member in detail.ZugeordneteMitglieder)
                _members.Add(member);

            _membersEmptyLabel.IsVisible = _members.Count == 0;
            _statusLabel.Text = _members.Count > 0
                ? $"{_members.Count} aktive Zuordnung(en) geladen."
                : string.Empty;
            _lastLoadedId = _wartungsvertragId;
        }
        catch (Exception ex)
        {
            _statusLabel.Text = ex.Message;
            _lastLoadedId = 0;
        }
        finally
        {
            _isBusy = false;
        }
    }

    private static bool TryGetLongValue(IDictionary<string, object> query, string key, out long value)
    {
        value = 0;
        if (!query.TryGetValue(key, out var raw) || raw == null)
            return false;

        return raw switch
        {
            long longValue => (value = longValue) > 0,
            int intValue => (value = intValue) > 0,
            string text when long.TryParse(Uri.UnescapeDataString(text), out var parsed) => (value = parsed) > 0,
            _ => false
        };
    }

    private static Border CreateInfoSection(string title, Label valueLabel)
    {
        return new Border
        {
            Stroke = Colors.LightGray,
            Padding = 12,
            Content = new VerticalStackLayout
            {
                Spacing = 4,
                Children =
                {
                    new Label { Text = title, FontAttributes = FontAttributes.Bold },
                    valueLabel
                }
            }
        };
    }
}
