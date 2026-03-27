using KGV.Core.Interfaces;
using KGV.Core.Models;
using KGV.Core.Security;
using KGV.Maui.State;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace KGV.Maui.Pages;

public sealed class MemberWartungsvertraegePage : ContentPage
{
    private readonly ISupabaseService _supabaseService;
    private readonly MemberContextState _memberContextState;
    private readonly UserContextState _userContextState;
    private readonly ObservableCollection<MemberContractListItem> _items = new();
    private readonly ObservableCollection<SelectableWartungsvertragItem> _assignableItems = new();
    private readonly Label _headlineLabel;
    private readonly Label _countLabel;
    private readonly Label _statusLabel;
    private readonly CollectionView _itemsView;
    private readonly Button _refreshButton;
    private readonly Button _assignButton;
    private readonly Border _assignSection;
    private readonly DatePicker _assignDatePicker;
    private readonly CollectionView _assignView;
    private readonly Label _assignEmptyLabel;
    private readonly Button _saveAssignButton;
    private readonly Button _cancelAssignButton;
    private bool _isBusy;
    private bool _isAssignMode;
    private bool _canManage;

    public MemberWartungsvertraegePage(ISupabaseService supabaseService, MemberContextState memberContextState, UserContextState userContextState)
    {
        _supabaseService = supabaseService;
        _memberContextState = memberContextState;
        _userContextState = userContextState;
        Title = "Wartungsverträge";

        _headlineLabel = new Label { FontSize = 24, FontAttributes = FontAttributes.Bold };
        _countLabel = new Label { FontSize = 12, TextColor = Colors.Gray };
        _statusLabel = new Label { TextColor = Colors.DarkSlateBlue, LineBreakMode = LineBreakMode.WordWrap };

        _refreshButton = new Button { Text = "Aktualisieren" };
        _refreshButton.Clicked += async (_, _) => await LoadAsync();
        _assignButton = new Button { Text = "Wartungsvertrag zuweisen", IsVisible = false };
        _assignButton.Clicked += async (_, _) => await BeginAssignAsync();

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
                title.SetBinding(Label.TextProperty, nameof(MemberContractListItem.Titel));

                var description = new Label { FontSize = 12, TextColor = Colors.Gray, LineBreakMode = LineBreakMode.WordWrap };
                description.SetBinding(Label.TextProperty, nameof(MemberContractListItem.Kurzbeschreibung));

                var validity = new Label { FontSize = 12, TextColor = Colors.Gray, LineBreakMode = LineBreakMode.WordWrap };
                validity.SetBinding(Label.TextProperty, nameof(MemberContractListItem.GueltigkeitText));

                var usage = new Label { FontSize = 12, TextColor = Colors.DarkSlateBlue, LineBreakMode = LineBreakMode.WordWrap };
                usage.SetBinding(Label.TextProperty, nameof(MemberContractListItem.BelegungText));

                var status = new Label { FontSize = 12, TextColor = Colors.DarkSlateBlue, LineBreakMode = LineBreakMode.WordWrap };
                status.SetBinding(Label.TextProperty, nameof(MemberContractListItem.StatusText));

                var endButton = new Button { Text = "Beenden", FontSize = 12, HorizontalOptions = LayoutOptions.End };
                endButton.SetBinding(IsVisibleProperty, nameof(MemberContractListItem.CanEnd));
                endButton.Clicked += async (sender, _) =>
                {
                    if (sender is Button button && button.BindingContext is MemberContractListItem item)
                        await EndAssignmentAsync(item);
                };

                return new Border
                {
                    Stroke = Colors.LightGray,
                    Padding = 12,
                    Margin = new Thickness(0, 0, 0, 8),
                    Content = new VerticalStackLayout
                    {
                        Spacing = 4,
                        Children = { title, description, validity, usage, status, endButton }
                    }
                };
            })
        };

        _itemsView.SelectionChanged += async (_, e) =>
        {
            var selected = e.CurrentSelection?.Count > 0 ? e.CurrentSelection[0] as MemberContractListItem : null;
            _itemsView.SelectedItem = null;
            if (selected == null)
                return;

            await Shell.Current.GoToAsync($"{nameof(WartungsvertragDetailPage)}?wartungsvertragId={selected.ContractId}");
        };

        _assignDatePicker = new DatePicker { Date = DateTime.Today };
        _assignView = new CollectionView
        {
            SelectionMode = SelectionMode.None,
            ItemsSource = _assignableItems,
            ItemTemplate = new DataTemplate(() =>
            {
                var checkbox = new CheckBox();
                checkbox.SetBinding(CheckBox.IsCheckedProperty, nameof(SelectableWartungsvertragItem.IsSelected), mode: BindingMode.TwoWay);

                var title = new Label { FontAttributes = FontAttributes.Bold, LineBreakMode = LineBreakMode.WordWrap };
                title.SetBinding(Label.TextProperty, nameof(SelectableWartungsvertragItem.Titel));

                var description = new Label { FontSize = 12, TextColor = Colors.Gray, LineBreakMode = LineBreakMode.WordWrap };
                description.SetBinding(Label.TextProperty, nameof(SelectableWartungsvertragItem.Kurzbeschreibung));

                var usage = new Label { FontSize = 12, TextColor = Colors.DarkSlateBlue, LineBreakMode = LineBreakMode.WordWrap };
                usage.SetBinding(Label.TextProperty, nameof(SelectableWartungsvertragItem.BelegungText));

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
                            checkbox,
                            new VerticalStackLayout
                            {
                                Spacing = 4,
                                HorizontalOptions = LayoutOptions.Fill,
                                Children = { title, description, usage }
                            }
                        }
                    }
                };
            })
        };

        _assignEmptyLabel = new Label
        {
            Text = "Für dieses Mitglied sind aktuell keine freien zusätzlichen Wartungsverträge verfügbar.",
            TextColor = Colors.Gray,
            IsVisible = false
        };

        _saveAssignButton = new Button { Text = "Zuordnung speichern" };
        _saveAssignButton.Clicked += async (_, _) => await SaveAssignmentsAsync();
        _cancelAssignButton = new Button { Text = "Abbrechen" };
        _cancelAssignButton.Clicked += (_, _) => HideAssignMode();

        _assignSection = new Border
        {
            Stroke = Colors.LightGray,
            Padding = 12,
            IsVisible = false,
            Content = new VerticalStackLayout
            {
                Spacing = 12,
                Children =
                {
                    new Label { Text = "Wartungsvertrag zuweisen", FontAttributes = FontAttributes.Bold, FontSize = 18 },
                    new Label
                    {
                        Text = "Es werden nur freie Wartungsverträge angezeigt, die diesem Mitglied aktuell nicht aktiv zugeordnet sind.",
                        TextColor = Colors.Gray,
                        LineBreakMode = LineBreakMode.WordWrap
                    },
                    CreateField("Gültig ab", _assignDatePicker),
                    _assignEmptyLabel,
                    _assignView,
                    new HorizontalStackLayout
                    {
                        Spacing = 8,
                        Children = { _cancelAssignButton, _saveAssignButton }
                    }
                }
            }
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
                        Text = "Mitgliedsbezogene Übersicht der aktiven Wartungsverträge. Antippen öffnet dieselbe Detailansicht wie im globalen Bereich.",
                        TextColor = Colors.Gray,
                        LineBreakMode = LineBreakMode.WordWrap
                    },
                    new HorizontalStackLayout
                    {
                        Spacing = 8,
                        Children = { _refreshButton, _assignButton }
                    },
                    _countLabel,
                    _statusLabel,
                    _itemsView,
                    _assignSection
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
            _assignButton.IsVisible = _canManage;
            SetBusyState(true);
            _statusLabel.Text = "Wartungsverträge werden geladen.";
            _items.Clear();

            var selectedMember = _memberContextState.SelectedMember;
            if (selectedMember?.Id is not > 0)
            {
                _headlineLabel.Text = "Wartungsverträge";
                _countLabel.Text = string.Empty;
                _statusLabel.Text = "Bitte zuerst ein Mitglied auswählen.";
                HideAssignMode();
                return;
            }

            var member = await _supabaseService.GetMitgliedByIdAsync(selectedMember.Id);
            var displayName = member == null
                ? $"Mitglied #{selectedMember.Id}"
                : BuildDisplayName(member.Vorname, member.Name, selectedMember.Id);
            _headlineLabel.Text = $"Wartungsverträge von {displayName}";

            var items = await _supabaseService.GetWartungsvertraegeForMitgliedAsync(selectedMember.Id);
            foreach (var item in items)
                _items.Add(new MemberContractListItem(item, _canManage));

            _countLabel.Text = items.Count > 0
                ? $"{items.Count} aktive Zuordnung(en)"
                : "Keine aktiven Wartungsverträge.";
            _statusLabel.Text = string.Empty;

            if (_isAssignMode)
                await LoadAssignableContractsAsync(selectedMember.Id);
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

    private async Task BeginAssignAsync()
    {
        if (!_canManage || _isBusy)
            return;

        var selectedMember = _memberContextState.SelectedMember;
        if (selectedMember?.Id is not > 0)
            return;

        _isBusy = true;
        SetBusyState(true);
        try
        {
            _isAssignMode = true;
            await LoadAssignableContractsAsync(selectedMember.Id);
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

    private async Task LoadAssignableContractsAsync(int mitgliedId)
    {
        _statusLabel.Text = "Wartungsverträge werden geladen.";
        _assignableItems.Clear();

        var assignableContracts = await _supabaseService.GetAssignableWartungsvertraegeForMitgliedAsync(mitgliedId);
        foreach (var item in assignableContracts)
            _assignableItems.Add(new SelectableWartungsvertragItem(item, UpdateAssignSelectionState));

        _assignEmptyLabel.IsVisible = _assignableItems.Count == 0;
        _assignView.IsVisible = _assignableItems.Count > 0;
        _assignSection.IsVisible = _isAssignMode && _canManage;
        UpdateAssignSelectionState();
        _statusLabel.Text = _assignableItems.Count == 0
            ? "Für dieses Mitglied sind aktuell keine freien zusätzlichen Wartungsverträge verfügbar."
            : string.Empty;
    }

    private async Task SaveAssignmentsAsync()
    {
        if (!_canManage || _isBusy)
            return;

        var selectedMember = _memberContextState.SelectedMember;
        if (selectedMember?.Id is not > 0)
            return;

        var selectedIds = _assignableItems.Where(x => x.IsSelected).Select(x => x.Id).ToList();
        if (selectedIds.Count == 0)
        {
            _statusLabel.Text = "Bitte mindestens einen Wartungsvertrag auswählen.";
            return;
        }

        _isBusy = true;
        SetBusyState(true);
        _statusLabel.Text = "Zuordnung wird gespeichert.";

        try
        {
            var result = await _supabaseService.AssignWartungsvertraegeToMitgliedAsync(selectedMember.Id, _assignDatePicker.Date, selectedIds);
            if (!result.Success)
            {
                _statusLabel.Text = result.Message;
                return;
            }

            HideAssignMode();
            _isBusy = false;
            await LoadAsync();
            _statusLabel.Text = result.Message;
            return;
        }
        catch (Exception ex)
        {
            _statusLabel.Text = ex.Message;
        }
        finally
        {
            _isBusy = false;
            SetBusyState(false);
            UpdateAssignSelectionState();
        }
    }

    private async Task EndAssignmentAsync(MemberContractListItem item)
    {
        if (!_canManage || _isBusy || item.ZuordnungId <= 0)
            return;

        var confirmed = await DisplayAlert("Wartungsvertrag beenden", $"Die aktive Zuordnung von '{item.Titel}' wird beendet. Fortfahren?", "Ja", "Nein");
        if (!confirmed)
            return;

        _isBusy = true;
        SetBusyState(true);
        _statusLabel.Text = "Zuordnung wird beendet.";

        try
        {
            var success = await _supabaseService.EndWartungsvertragZuordnungAsync(item.ZuordnungId, DateTime.Today);
            if (!success)
            {
                _statusLabel.Text = "Die aktive Zuordnung konnte nicht beendet werden.";
                return;
            }

            _isBusy = false;
            await LoadAsync();
            _statusLabel.Text = "Zuordnung wurde beendet.";
            return;
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

    private void HideAssignMode()
    {
        _isAssignMode = false;
        _assignableItems.Clear();
        _assignSection.IsVisible = false;
        _assignEmptyLabel.IsVisible = false;
        _assignView.IsVisible = false;
        UpdateAssignSelectionState();
    }

    private void UpdateAssignSelectionState()
    {
        _saveAssignButton.IsEnabled = _canManage && !_isBusy && _assignableItems.Any(x => x.IsSelected);
        _cancelAssignButton.IsEnabled = !_isBusy;
    }

    private void SetBusyState(bool isBusy)
    {
        _refreshButton.IsEnabled = !isBusy;
        _assignButton.IsEnabled = _canManage && !isBusy;
        _itemsView.IsEnabled = !isBusy;
        _assignDatePicker.IsEnabled = !isBusy;
        _assignView.IsEnabled = !isBusy;
        UpdateAssignSelectionState();
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

    private static string BuildDisplayName(string? vorname, string? nachname, int fallbackId)
    {
        var displayName = $"{vorname} {nachname}".Trim();
        return string.IsNullOrWhiteSpace(displayName) ? $"Mitglied #{fallbackId}" : displayName;
    }

    private sealed class MemberContractListItem
    {
        public MemberContractListItem(MemberWartungsvertragItem source, bool canEnd)
        {
            ContractId = source.Id;
            ZuordnungId = source.ZuordnungId;
            Titel = source.Titel;
            Kurzbeschreibung = source.Kurzbeschreibung;
            GueltigkeitText = source.GueltigkeitText;
            BelegungText = source.BelegungText;
            StatusText = source.StatusText;
            CanEnd = canEnd && source.ZuordnungId > 0;
        }

        public long ContractId { get; }
        public long ZuordnungId { get; }
        public string Titel { get; }
        public string Kurzbeschreibung { get; }
        public string GueltigkeitText { get; }
        public string BelegungText { get; }
        public string StatusText { get; }
        public bool CanEnd { get; }
    }

    private sealed class SelectableWartungsvertragItem : INotifyPropertyChanged
    {
        private readonly Action _selectionChanged;
        private bool _isSelected;

        public SelectableWartungsvertragItem(WartungsvertragOverviewItem source, Action selectionChanged)
        {
            Id = source.Id;
            Titel = source.Titel;
            Kurzbeschreibung = source.Kurzbeschreibung;
            BelegungText = source.BelegungText;
            _selectionChanged = selectionChanged;
        }

        public long Id { get; }
        public string Titel { get; }
        public string Kurzbeschreibung { get; }
        public string BelegungText { get; }

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected == value)
                    return;

                _isSelected = value;
                OnPropertyChanged();
                _selectionChanged();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
