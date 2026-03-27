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
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace KGV.Maui.Pages;

public sealed class WartungsvertragAssignMembersPage : ContentPage, IQueryAttributable
{
    private const string SortByName = "Name";
    private const string SortByGarden = "Gartennummer";

    private readonly ISupabaseService _supabaseService;
    private readonly UserContextState _userContextState;
    private readonly ObservableCollection<AssignableMemberItem> _visibleItems = new();
    private readonly List<AssignableMemberItem> _allItems = new();
    private readonly Label _headlineLabel;
    private readonly Label _descriptionLabel;
    private readonly Label _capacityLabel;
    private readonly Label _statusLabel;
    private readonly Picker _sortPicker;
    private readonly DatePicker _gueltigAbDatePicker;
    private readonly CollectionView _itemsView;
    private readonly Button _saveButton;
    private readonly Button _cancelButton;

    private long _wartungsvertragId;
    private bool _isAuthorized;
    private bool _isBusy;
    private bool _loadScheduled;
    private int _freiePlaetze;

    public WartungsvertragAssignMembersPage(ISupabaseService supabaseService, UserContextState userContextState)
    {
        _supabaseService = supabaseService;
        _userContextState = userContextState;
        Title = "Mitglieder zuweisen";

        _headlineLabel = new Label { FontSize = 24, FontAttributes = FontAttributes.Bold, LineBreakMode = LineBreakMode.WordWrap };
        _descriptionLabel = new Label { TextColor = Colors.Gray, LineBreakMode = LineBreakMode.WordWrap };
        _capacityLabel = new Label { TextColor = Colors.DarkSlateBlue, LineBreakMode = LineBreakMode.WordWrap };
        _statusLabel = new Label { TextColor = Colors.DarkSlateBlue, LineBreakMode = LineBreakMode.WordWrap };

        _sortPicker = new Picker { Title = "Sortierung" };
        _sortPicker.ItemsSource = new List<string> { SortByName, SortByGarden };
        _sortPicker.SelectedIndex = 0;
        _sortPicker.SelectedIndexChanged += (_, _) => ApplySorting();

        _gueltigAbDatePicker = new DatePicker { Date = DateTime.Today };

        _itemsView = new CollectionView
        {
            SelectionMode = SelectionMode.None,
            ItemsSource = _visibleItems,
            ItemTemplate = new DataTemplate(() =>
            {
                var checkBox = new CheckBox();
                checkBox.SetBinding(CheckBox.IsCheckedProperty, nameof(AssignableMemberItem.IsSelected), mode: BindingMode.TwoWay);
                checkBox.SetBinding(CheckBox.IsEnabledProperty, nameof(AssignableMemberItem.CanSelect));

                var nameLabel = new Label { FontAttributes = FontAttributes.Bold, LineBreakMode = LineBreakMode.WordWrap };
                nameLabel.SetBinding(Label.TextProperty, nameof(AssignableMemberItem.DisplayName));

                var gardenLabel = new Label { FontSize = 12, TextColor = Colors.Gray, LineBreakMode = LineBreakMode.WordWrap };
                gardenLabel.SetBinding(Label.TextProperty, nameof(AssignableMemberItem.GartenText));

                var statusLabel = new Label { FontSize = 12, TextColor = Colors.DarkSlateBlue, LineBreakMode = LineBreakMode.WordWrap };
                statusLabel.SetBinding(Label.TextProperty, nameof(AssignableMemberItem.StatusText));

                return new Border
                {
                    Stroke = Colors.LightGray,
                    Padding = 12,
                    Margin = new Thickness(0, 0, 0, 8),
                    Content = new HorizontalStackLayout
                    {
                        Spacing = 12,
                        Children =
                        {
                            checkBox,
                            new VerticalStackLayout
                            {
                                Spacing = 4,
                                HorizontalOptions = LayoutOptions.Fill,
                                Children = { nameLabel, gardenLabel, statusLabel }
                            }
                        }
                    }
                };
            })
        };

        _saveButton = new Button { Text = "Zuordnungen speichern" };
        _saveButton.Clicked += async (_, _) => await SaveAsync();
        _cancelButton = new Button { Text = "Abbrechen" };
        _cancelButton.Clicked += async (_, _) => await Shell.Current.GoToAsync("..");

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = 24,
                Spacing = 12,
                Children =
                {
                    _headlineLabel,
                    _descriptionLabel,
                    _capacityLabel,
                    _statusLabel,
                    CreateField("Sortierung", _sortPicker),
                    CreateField("Gültig ab", _gueltigAbDatePicker),
                    _itemsView,
                    new HorizontalStackLayout
                    {
                        Spacing = 8,
                        Children = { _cancelButton, _saveButton }
                    }
                }
            }
        };
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        _wartungsvertragId = TryReadLong(query, "wartungsvertragId");
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        if (_isBusy || _loadScheduled)
            return;

        _loadScheduled = true;
        Dispatcher.Dispatch(async () =>
        {
            await Task.Yield();
            _loadScheduled = false;
            await LoadAsync();
        });
    }

    private async Task LoadAsync()
    {
        if (_isBusy)
            return;

        _isBusy = true;
        _statusLabel.Text = "Wartungsvertrag wird geladen.";

        try
        {
            _isAuthorized = _userContextState.CurrentUserContext?.Role is UserRole.Admin or UserRole.Vorstand;
            if (!_isAuthorized)
            {
                _headlineLabel.Text = "Mitglieder zuweisen";
                _descriptionLabel.Text = "Diese Ansicht ist nur für Admin/Vorstand verfügbar.";
                SetEnabledState(false);
                return;
            }

            var detail = await _supabaseService.GetWartungsvertragDetailAsync(_wartungsvertragId);
            if (detail == null)
            {
                _headlineLabel.Text = "Mitglieder zuweisen";
                _descriptionLabel.Text = "Der ausgewählte Wartungsvertrag konnte nicht geladen werden.";
                SetEnabledState(false);
                return;
            }

            _headlineLabel.Text = $"Mitglieder zuweisen: {detail.Titel}";
            _descriptionLabel.Text = "Globale mobile Zuweisung aktiver Mitglieder mit Sortierung nach Name oder Gartennummer. Bereits aktive Zuordnungen bleiben sichtbar, aber gesperrt.";
            _freiePlaetze = detail.Frei;
            UpdateCapacityLabel();
            _allItems.Clear();
            _visibleItems.Clear();

            var members = await _supabaseService.GetMitgliederAsync();
            var parzellen = await _supabaseService.GetAllParzellenAsync();
            var belegungen = await _supabaseService.GetAllParzellenBelegungenAsync();
            var assignedIds = detail.ZugeordneteMitglieder
                .Where(x => x.MitgliedId > 0)
                .Select(x => x.MitgliedId)
                .ToHashSet();
            var gardensByMemberId = BuildGardenLookup(parzellen, belegungen);

            foreach (var member in members
                .Where(OperationalDataFilter.IsOperationalMember)
                .Where(x => x.Aktiv)
                )
            {
                var item = new AssignableMemberItem(
                    member.Id,
                    BuildDisplayName(member),
                    gardensByMemberId.TryGetValue(member.Id, out var gardens) ? gardens : string.Empty,
                    assignedIds.Contains(member.Id),
                    UpdateSelectionState);
                _allItems.Add(item);
            }

            ApplySorting();
            UpdateSelectionState();
            _statusLabel.Text = string.Empty;
            SetEnabledState(true);
        }
        catch (Exception ex)
        {
            _statusLabel.Text = ex.Message;
            SetEnabledState(false);
        }
        finally
        {
            _isBusy = false;
        }
    }

    private void ApplySorting()
    {
        var selectedSort = _sortPicker.SelectedItem as string ?? SortByName;
        var ordered = selectedSort == SortByGarden
            ? _allItems
                .OrderBy(x => GetGartenNrSortKey(x.GartenNummern))
                .ThenBy(x => x.GartenNummern, StringComparer.CurrentCultureIgnoreCase)
                .ThenBy(x => x.DisplayName, StringComparer.CurrentCultureIgnoreCase)
            : _allItems
                .OrderBy(x => x.DisplayName, StringComparer.CurrentCultureIgnoreCase)
                .ThenBy(x => x.GartenNummern, StringComparer.CurrentCultureIgnoreCase);

        _visibleItems.Clear();
        foreach (var item in ordered)
            _visibleItems.Add(item);
    }

    private void UpdateSelectionState()
    {
        var selectedCount = _allItems.Count(x => x.IsSelected && !x.IsAlreadyAssigned);
        var remaining = Math.Max(0, _freiePlaetze - selectedCount);
        foreach (var item in _allItems)
            item.CanSelect = !item.IsAlreadyAssigned && (item.IsSelected || remaining > 0);

        UpdateCapacityLabel();
        _saveButton.IsEnabled = _isAuthorized && !_isBusy && selectedCount > 0;
    }

    private async Task SaveAsync()
    {
        if (!_isAuthorized || _isBusy)
            return;

        var selectedIds = _allItems
            .Where(x => x.IsSelected && !x.IsAlreadyAssigned)
            .Select(x => x.MitgliedId)
            .ToList();
        if (selectedIds.Count == 0)
        {
            _statusLabel.Text = "Bitte mindestens ein neues Mitglied auswählen.";
            return;
        }

        _isBusy = true;
        SetEnabledState(false);
        _statusLabel.Text = "Zuordnungen werden gespeichert.";

        try
        {
            var result = await _supabaseService.AssignMitgliederToWartungsvertragAsync(_wartungsvertragId, _gueltigAbDatePicker.Date, selectedIds);
            if (!result.Success)
            {
                _statusLabel.Text = result.Message;
                return;
            }

            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            _statusLabel.Text = ex.Message;
        }
        finally
        {
            _isBusy = false;
            SetEnabledState(true);
            UpdateSelectionState();
        }
    }

    private void UpdateCapacityLabel()
    {
        var selectedCount = _allItems.Count(x => x.IsSelected && !x.IsAlreadyAssigned);
        var remaining = Math.Max(0, _freiePlaetze - selectedCount);
        _capacityLabel.Text = remaining <= 0
            ? "Kein freier Platz mehr verfügbar."
            : remaining == 1
                ? "Noch 1 Platz frei."
                : $"Noch {remaining} Plätze frei.";
    }

    private void SetEnabledState(bool isEnabled)
    {
        _sortPicker.IsEnabled = isEnabled;
        _gueltigAbDatePicker.IsEnabled = isEnabled;
        _cancelButton.IsEnabled = isEnabled;
        _saveButton.IsEnabled = isEnabled && _isAuthorized && _allItems.Count(x => x.IsSelected && !x.IsAlreadyAssigned) > 0;
        foreach (var item in _allItems)
            item.CanSelect = isEnabled && !item.IsAlreadyAssigned && (item.IsSelected || _allItems.Count(x => x.IsSelected && !x.IsAlreadyAssigned) < _freiePlaetze);
    }

    private static Border CreateField(string title, View input)
    {
        return new Border
        {
            Stroke = Colors.LightGray,
            Padding = 12,
            Content = new VerticalStackLayout
            {
                Spacing = 6,
                Children =
                {
                    new Label { Text = title, FontAttributes = FontAttributes.Bold, FontSize = 12, TextColor = Colors.Gray },
                    input
                }
            }
        };
    }

    private static Dictionary<int, string> BuildGardenLookup(IReadOnlyCollection<ParzelleRecord> parzellen, IReadOnlyCollection<ParzellenBelegungRecord> belegungen)
    {
        var parzellenById = parzellen
            .Where(x => x.Id > 0)
            .ToDictionary(x => x.Id);
        var today = DateTime.Today;

        return belegungen
            .Where(x => x.MitgliedId > 0)
            .Where(x => x.ParzelleId > 0 && parzellenById.ContainsKey(x.ParzelleId))
            .Where(x => IsActiveBelegungOn(x, today))
            .GroupBy(x => x.MitgliedId)
            .ToDictionary(
                x => x.Key,
                x => string.Join(", ", x
                    .Select(b => parzellenById[b.ParzelleId].GartenNr)
                    .Where(g => !string.IsNullOrWhiteSpace(g))
                    .Select(g => g!.Trim())
                    .Distinct(StringComparer.CurrentCultureIgnoreCase)
                    .OrderBy(GetGartenNrSortKey)
                    .ThenBy(g => g, StringComparer.CurrentCultureIgnoreCase)));
    }

    private static bool IsActiveBelegungOn(ParzellenBelegungRecord belegung, DateTime date)
    {
        var target = date.Date;
        var start = belegung.VonDatum?.Date;
        var end = belegung.BisDatum?.Date;
        return (!start.HasValue || start.Value <= target)
            && (!end.HasValue || end.Value >= target);
    }

    private static string BuildDisplayName(MitgliedRecord member)
    {
        var displayName = $"{member.Vorname} {member.Name}".Trim();
        displayName = string.IsNullOrWhiteSpace(displayName) ? member.Email ?? $"Mitglied #{member.Id}" : displayName;
        return member.HauptmitgliedId is > 0
            ? $"{displayName} (Nebenmitglied)"
            : $"{displayName} (Hauptmitglied)";
    }

    private static int GetGartenNrSortKey(string? gartenNummern)
    {
        if (string.IsNullOrWhiteSpace(gartenNummern))
            return int.MaxValue;

        var firstGarden = gartenNummern
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Trim())
            .FirstOrDefault();
        if (string.IsNullOrWhiteSpace(firstGarden))
            return int.MaxValue;

        var digits = new string(firstGarden.TakeWhile(char.IsDigit).ToArray());
        return int.TryParse(digits, out var number) ? number : int.MaxValue;
    }

    private static long TryReadLong(IDictionary<string, object> query, string key)
    {
        if (!query.TryGetValue(key, out var value) || value == null)
            return 0;

        return value switch
        {
            long longValue => longValue,
            int intValue => intValue,
            string text when long.TryParse(Uri.UnescapeDataString(text), out var parsed) => parsed,
            _ => 0
        };
    }

    private sealed class AssignableMemberItem : INotifyPropertyChanged
    {
        private readonly Action _selectionChanged;
        private bool _isSelected;
        private bool _canSelect;

        public AssignableMemberItem(int mitgliedId, string displayName, string gartenNummern, bool isAlreadyAssigned, Action selectionChanged)
        {
            MitgliedId = mitgliedId;
            DisplayName = displayName;
            GartenNummern = gartenNummern;
            IsAlreadyAssigned = isAlreadyAssigned;
            _selectionChanged = selectionChanged;
            _canSelect = !isAlreadyAssigned;
        }

        public int MitgliedId { get; }
        public string DisplayName { get; }
        public string GartenNummern { get; }
        public string GartenText => string.IsNullOrWhiteSpace(GartenNummern) ? "Kein aktiver Garten" : $"Gärten: {GartenNummern}";
        public bool IsAlreadyAssigned { get; }
        public string StatusText => IsAlreadyAssigned ? "Bereits aktiv zugeordnet" : "Für neue Zuordnung auswählbar";

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (IsAlreadyAssigned)
                    value = false;

                if (_isSelected == value)
                    return;

                _isSelected = value;
                OnPropertyChanged();
                _selectionChanged();
            }
        }

        public bool CanSelect
        {
            get => _canSelect;
            set
            {
                if (_canSelect == value)
                    return;

                _canSelect = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
