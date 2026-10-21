using KGV.Core.Interfaces;
using KGV.Core.Models;
using KGV.Core.Security;
using KGV.Maui.State;
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
    private readonly UserContextState _userContextState;
    private readonly ObservableCollection<WartungsvertragAssignedMemberItem> _currentMembers = new();
    private readonly ObservableCollection<WartungsvertragAssignedMemberItem> _futureMembers = new();
    private readonly ObservableCollection<WartungsvertragAssignedMemberItem> _historicMembers = new();
    private readonly Label _titleLabel;
    private readonly Label _descriptionLabel;
    private readonly Label _bereichLabel;
    private readonly Label _maxKontingentLabel;
    private readonly Label _belegtLabel;
    private readonly Label _freiLabel;
    private readonly Label _arbeitsstundenwirkungLabel;
    private readonly Label _aktivLabel;
    private readonly Label _bemerkungLabel;
    private readonly Label _statusLabel;
    private readonly Label _currentMembersEmptyLabel;
    private readonly CollectionView _currentMembersView;
    private readonly CollectionView _futureMembersView;
    private readonly CollectionView _historicMembersView;
    private readonly Button _refreshButton;
    private readonly Button _editButton;
    private readonly Button _assignButton;
    private long _wartungsvertragId;
    private bool _isBusy;
    private bool _adminModeRequested;
    private bool _canManage;

    public WartungsvertragDetailPage(ISupabaseService supabaseService, UserContextState userContextState)
    {
        _supabaseService = supabaseService;
        _userContextState = userContextState;
        Title = "Wartungsvertrag";

        _titleLabel = new Label { FontSize = 24, FontAttributes = FontAttributes.Bold, LineBreakMode = LineBreakMode.WordWrap };
        _descriptionLabel = new Label { TextColor = Colors.Gray, LineBreakMode = LineBreakMode.WordWrap };
        _bereichLabel = new Label { LineBreakMode = LineBreakMode.WordWrap };
        _maxKontingentLabel = new Label();
        _belegtLabel = new Label();
        _freiLabel = new Label();
        _arbeitsstundenwirkungLabel = new Label { LineBreakMode = LineBreakMode.WordWrap };
        _aktivLabel = new Label();
        _bemerkungLabel = new Label { LineBreakMode = LineBreakMode.WordWrap };
        _statusLabel = new Label { TextColor = Colors.DarkSlateBlue, LineBreakMode = LineBreakMode.WordWrap };
        _currentMembersEmptyLabel = new Label { Text = "Keine aktuellen Mitgliedszuordnungen.", TextColor = Colors.Gray, IsVisible = false };

        _refreshButton = new Button { Text = "Aktualisieren" };
        _refreshButton.Clicked += async (_, _) => await LoadAsync();
        _editButton = new Button { Text = "Bearbeiten", IsVisible = false };
        _editButton.Clicked += async (_, _) => await OpenEditorAsync();
        _assignButton = new Button { Text = "Mitglieder zuweisen", IsVisible = false };
        _assignButton.Clicked += async (_, _) => await OpenAssignMembersAsync();

        _currentMembersView = CreateMembersView(_currentMembers);
        _futureMembersView = CreateMembersView(_futureMembers);
        _historicMembersView = CreateMembersView(_historicMembers);

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
                    new FlexLayout
                    {
                        Direction = FlexDirection.Row,
                        Wrap = FlexWrap.Wrap,
                        Children = { _refreshButton, _editButton, _assignButton }
                    },
                    CreateInfoSection("Max. Kontingent", _maxKontingentLabel),
                    CreateInfoSection("Belegt", _belegtLabel),
                    CreateInfoSection("Frei", _freiLabel),
                    CreateInfoSection("Bereich", _bereichLabel),
                    CreateInfoSection("Arbeitsstundenwirkung", _arbeitsstundenwirkungLabel),
                    CreateInfoSection("Status", _aktivLabel),
                    CreateInfoSection("Bemerkung", _bemerkungLabel),
                    _statusLabel,
                    new Label { Text = "Aktuelle Zuordnungen", FontAttributes = FontAttributes.Bold, FontSize = 18 },
                    _currentMembersEmptyLabel,
                    _currentMembersView,
                    new Label { Text = "Zukünftige Zuordnungen", FontAttributes = FontAttributes.Bold, FontSize = 18 },
                    _futureMembersView,
                    new Label { Text = "Historische Zuordnungen", FontAttributes = FontAttributes.Bold, FontSize = 18 },
                    _historicMembersView
                }
            }
        };
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (!TryGetLongValue(query, "wartungsvertragId", out var wartungsvertragId))
            return;

        _wartungsvertragId = wartungsvertragId;
        _adminModeRequested = TryGetBoolValue(query, "adminMode");
        _ = LoadAsync();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        if (_isBusy || _wartungsvertragId <= 0)
            return;

        _isBusy = true;
        try
        {
            _canManage = _adminModeRequested && PermissionChecks.CanEditAllMembers(_userContextState.CurrentUserContext);
            UpdateActionVisibility();
            SetBusyState(true);
            _statusLabel.Text = "Daten werden geladen.";
            _currentMembers.Clear(); _futureMembers.Clear(); _historicMembers.Clear();
            _currentMembersEmptyLabel.IsVisible = false;

            var detail = await _supabaseService.GetWartungsvertragDetailAsync(_wartungsvertragId);
            if (detail == null)
            {
                _titleLabel.Text = "Wartungsvertrag";
                _descriptionLabel.Text = "Der ausgewählte Wartungsvertrag konnte nicht geladen werden.";
                _maxKontingentLabel.Text = "-";
                _belegtLabel.Text = "-";
                _freiLabel.Text = "-";
                _bereichLabel.Text = "-";
                _arbeitsstundenwirkungLabel.Text = "-";
                _aktivLabel.Text = "-";
                _bemerkungLabel.Text = "-";
                _statusLabel.Text = string.Empty;
                return;
            }

            _titleLabel.Text = detail.Titel;
            _descriptionLabel.Text = detail.BeschreibungText;
            _maxKontingentLabel.Text = detail.MaxKontingent.ToString();
            _belegtLabel.Text = detail.Belegt.ToString();
            _freiLabel.Text = detail.Frei.ToString();
            _bereichLabel.Text = string.IsNullOrWhiteSpace(detail.Bereich) ? "Kein Bereich hinterlegt." : detail.Bereich;
            _arbeitsstundenwirkungLabel.Text = detail.ArbeitsstundenwirkungText;
            _aktivLabel.Text = detail.AktivText;
            _bemerkungLabel.Text = string.IsNullOrWhiteSpace(detail.Bemerkung) ? "Keine Bemerkung hinterlegt." : detail.Bemerkung;

            foreach (var member in detail.AktuelleZugeordneteMitglieder) _currentMembers.Add(member);
            foreach (var member in detail.ZukuenftigeZugeordneteMitglieder) _futureMembers.Add(member);
            foreach (var member in detail.HistorischeZugeordneteMitglieder) _historicMembers.Add(member);

            _currentMembersEmptyLabel.IsVisible = _currentMembers.Count == 0;
            _statusLabel.Text = _currentMembers.Count + _futureMembers.Count + _historicMembers.Count > 0
                ? $"{_currentMembers.Count} aktuell, {_futureMembers.Count} zukünftig, {_historicMembers.Count} historisch."
                : string.Empty;
        }
        catch (Exception ex)
        {
            _statusLabel.Text = ex.Message;
        }
        finally
        {
            _isBusy = false;
            SetBusyState(false);
        }
    }

    private async Task OpenEditorAsync()
    {
        if (!_canManage || _wartungsvertragId <= 0 || _isBusy)
            return;

        await Shell.Current.GoToAsync($"{nameof(WartungsvertragEditorPage)}?wartungsvertragId={_wartungsvertragId}&origin=detail");
    }

    private async Task OpenAssignMembersAsync()
    {
        if (!_canManage || _wartungsvertragId <= 0 || _isBusy)
            return;

        await Shell.Current.GoToAsync($"{nameof(WartungsvertragAssignMembersPage)}?wartungsvertragId={_wartungsvertragId}");
    }

    private void UpdateActionVisibility()
    {
        _editButton.IsVisible = _canManage;
        _assignButton.IsVisible = _canManage;
    }

    private void SetBusyState(bool isBusy)
    {
        _refreshButton.IsEnabled = !isBusy;
        _editButton.IsEnabled = _canManage && !isBusy;
        _assignButton.IsEnabled = _canManage && !isBusy;
        _currentMembersView.IsEnabled = !isBusy;
        _futureMembersView.IsEnabled = !isBusy;
        _historicMembersView.IsEnabled = !isBusy;
    }

    private static CollectionView CreateMembersView(ObservableCollection<WartungsvertragAssignedMemberItem> members) => new()
    {
        SelectionMode = SelectionMode.None,
        ItemsSource = members,
        ItemTemplate = new DataTemplate(() =>
        {
            var title = new Label { FontAttributes = FontAttributes.Bold, LineBreakMode = LineBreakMode.WordWrap };
            title.SetBinding(Label.TextProperty, nameof(WartungsvertragAssignedMemberItem.DisplayName));
            var subtitle = new Label { FontSize = 12, TextColor = Colors.Gray, LineBreakMode = LineBreakMode.WordWrap };
            subtitle.SetBinding(Label.TextProperty, nameof(WartungsvertragAssignedMemberItem.Subtitle));
            return new Border { Stroke = Colors.LightGray, Padding = 12, Margin = new Thickness(0, 0, 0, 8), Content = new VerticalStackLayout { Spacing = 4, Children = { title, subtitle } } };
        })
    };

    private static bool TryGetBoolValue(IDictionary<string, object> query, string key)
    {
        if (!query.TryGetValue(key, out var raw) || raw == null)
            return false;

        return raw switch
        {
            bool boolValue => boolValue,
            string text when bool.TryParse(Uri.UnescapeDataString(text), out var parsedBool) => parsedBool,
            string text when int.TryParse(Uri.UnescapeDataString(text), out var parsedInt) => parsedInt != 0,
            int intValue => intValue != 0,
            long longValue => longValue != 0,
            _ => false
        };
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
