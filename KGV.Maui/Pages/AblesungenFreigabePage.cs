using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using KGV.Core.Interfaces;
using KGV.Core.Models;
using KGV.Core.Security;
using KGV.Maui.State;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace KGV.Maui.Pages;

public sealed class AblesungenFreigabePage : ContentPage
{
    private readonly ISupabaseService _supabaseService;
    private readonly UserContextState _userContextState;
    private readonly ObservableCollection<AblesungReviewItem> _items = new();
    private readonly CollectionView _list;
    private readonly Label _statusLabel;
    private readonly Label _countLabel;
    private readonly Label _detailHeaderLabel;
    private readonly Label _detailInfoLabel;
    private readonly Label _emptyLabel;
    private readonly VerticalStackLayout _detailLayout;
    private readonly DatePicker _ablesedatumPicker;
    private readonly Entry _standEntry;
    private readonly Editor _commentEditor;
    private readonly Button _refreshButton;
    private readonly Button _approveButton;
    private readonly Button _rejectButton;
    private readonly Button _correctButton;
    private readonly Button _removeButton;

    private AblesungReviewItem? _selectedItem;
    private bool _isBusy;

    public AblesungenFreigabePage(ISupabaseService supabaseService, UserContextState userContextState)
    {
        _supabaseService = supabaseService ?? throw new ArgumentNullException(nameof(supabaseService));
        _userContextState = userContextState ?? throw new ArgumentNullException(nameof(userContextState));

        Title = "Ablesungen freigeben";

        _statusLabel = new Label
        {
            TextColor = Colors.DarkSlateBlue,
            LineBreakMode = LineBreakMode.WordWrap,
            IsVisible = false
        };

        _countLabel = new Label
        {
            TextColor = Colors.Gray,
            FontSize = 12,
            IsVisible = false
        };

        _emptyLabel = new Label
        {
            Text = "Aktuell liegen keine eingereichten Ablesungen zur Prüfung vor.",
            TextColor = Colors.Gray,
            LineBreakMode = LineBreakMode.WordWrap,
            IsVisible = false
        };

        _detailHeaderLabel = new Label
        {
            FontSize = 18,
            FontAttributes = FontAttributes.Bold,
            LineBreakMode = LineBreakMode.WordWrap
        };

        _detailInfoLabel = new Label
        {
            TextColor = Colors.Gray,
            LineBreakMode = LineBreakMode.WordWrap
        };

        _ablesedatumPicker = new DatePicker { Date = DateTime.Today };
        _standEntry = new Entry { Keyboard = Keyboard.Numeric, Placeholder = "Zählerstand" };
        _commentEditor = new Editor
        {
            AutoSize = EditorAutoSizeOption.TextChanges,
            HeightRequest = 120,
            Placeholder = "Pflichtkommentar für Freigeben, Ablehnen, Korrigieren oder Entfernen"
        };

        _refreshButton = new Button { Text = "Aktualisieren" };
        _refreshButton.Clicked += async (_, _) => await LoadAsync();

        _approveButton = new Button { Text = "Freigeben" };
        _approveButton.Clicked += async (_, _) => await EntscheidenAsync(AblesungPruefstatus.Freigegeben);

        _rejectButton = new Button { Text = "Ablehnen" };
        _rejectButton.Clicked += async (_, _) => await EntscheidenAsync(AblesungPruefstatus.Abgelehnt);

        _correctButton = new Button { Text = "Korrigieren" };
        _correctButton.Clicked += async (_, _) => await KorrigierenAsync();

        _removeButton = new Button { Text = "Entfernen" };
        _removeButton.Clicked += async (_, _) => await EntfernenAsync();

        _list = new CollectionView
        {
            SelectionMode = SelectionMode.Single,
            ItemsSource = _items,
            EmptyView = new Label
            {
                Text = "Aktuell liegen keine eingereichten Ablesungen zur Prüfung vor.",
                TextColor = Colors.Gray
            },
            ItemTemplate = new DataTemplate(() =>
            {
                var title = new Label { FontAttributes = FontAttributes.Bold, LineBreakMode = LineBreakMode.WordWrap };
                title.SetBinding(Label.TextProperty, nameof(AblesungReviewItem.ParzelleDisplayName));

                var info = new Label { FontSize = 12, TextColor = Colors.Gray, LineBreakMode = LineBreakMode.WordWrap };
                info.SetBinding(Label.TextProperty, nameof(AblesungReviewItem.MitgliedDisplay));

                var meter = new Label { FontSize = 12, TextColor = Colors.Gray };
                meter.SetBinding(Label.TextProperty, nameof(AblesungReviewItem.Zaehlernummer));

                var date = new Label { FontSize = 12, TextColor = Colors.DarkSlateBlue };
                date.SetBinding(Label.TextProperty, new Binding(nameof(AblesungReviewItem.Ablesedatum), stringFormat: "{0:dd.MM.yyyy}"));

                var stand = new Label { FontSize = 12, TextColor = Colors.DarkSlateBlue };
                stand.SetBinding(Label.TextProperty, new Binding(nameof(AblesungReviewItem.Stand), stringFormat: "Stand: {0:0.##}"));

                return new Border
                {
                    Stroke = Colors.LightGray,
                    StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = new CornerRadius(16) },
                    Padding = 12,
                    Margin = new Thickness(0, 0, 0, 8),
                    Content = new VerticalStackLayout
                    {
                        Spacing = 4,
                        Children = { title, info, meter, date, stand }
                    }
                };
            })
        };
        _list.SelectionChanged += OnSelectionChanged;

        _detailLayout = new VerticalStackLayout
        {
            Spacing = 12,
            IsVisible = false,
            Children =
            {
                _detailHeaderLabel,
                _detailInfoLabel,
                new Label
                {
                    Text = "Kommentar ist für alle vier Aktionen Pflicht. Korrigieren speichert Datum und Zählerstand direkt als freigegeben; Entfernen markiert die Einreichung mit Begründung als abgelehnt und nimmt sie damit aus dem offenen Prüfprozess heraus.",
                    TextColor = Colors.Gray,
                    LineBreakMode = LineBreakMode.WordWrap
                },
                CreateField("Korrekturdatum", _ablesedatumPicker),
                CreateField("Korrigierter Stand", _standEntry),
                CreateField("Prüfkommentar", _commentEditor),
                new HorizontalStackLayout
                {
                    Spacing = 8,
                    Children = { _approveButton, _rejectButton }
                },
                new HorizontalStackLayout
                {
                    Spacing = 8,
                    Children = { _correctButton, _removeButton }
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
                    new Label
                    {
                        Text = "Offene Einreichungen werden zentral über den Shared-Service geladen. Für Freigeben, Ablehnen, Korrigieren und Entfernen ist immer ein Kommentar erforderlich.",
                        TextColor = Colors.Gray,
                        LineBreakMode = LineBreakMode.WordWrap
                    },
                    _refreshButton,
                    _countLabel,
                    _statusLabel,
                    _emptyLabel,
                    _list,
                    _detailLayout
                }
            }
        };

        UpdateUiState();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        if (_isBusy)
            return;

        if (!PermissionChecks.CanApproveMeterReadings(_userContextState.CurrentUserContext))
        {
            _items.Clear();
            _selectedItem = null;
            SetStatus("Mit den aktuellen Rechten ist keine Ablesungsfreigabe möglich.");
            ApplySelection();
            UpdateUiState();
            return;
        }

        _isBusy = true;
        var selectedId = _selectedItem?.AblesungId;
        SetStatus(string.Empty);
        UpdateUiState();

        try
        {
            var items = await _supabaseService.GetOffeneAblesungenZurFreigabeAsync();
            _items.Clear();
            foreach (var item in items)
                _items.Add(item);

            _selectedItem = selectedId.HasValue
                ? _items.FirstOrDefault(x => x.AblesungId == selectedId.Value) ?? _items.FirstOrDefault()
                : _items.FirstOrDefault();

            ApplySelection();

            if (_items.Count == 0)
                SetStatus("Aktuell liegen keine eingereichten Ablesungen zur Prüfung vor.");
        }
        catch (Exception ex)
        {
            SetStatus(ex.Message);
        }
        finally
        {
            _isBusy = false;
            UpdateUiState();
        }
    }

    private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        _selectedItem = e.CurrentSelection?.OfType<AblesungReviewItem>().FirstOrDefault();
        ApplySelection();
        UpdateUiState();
    }

    private void ApplySelection()
    {
        if (_selectedItem == null)
        {
            _detailHeaderLabel.Text = string.Empty;
            _detailInfoLabel.Text = string.Empty;
            _ablesedatumPicker.Date = DateTime.Today;
            _standEntry.Text = string.Empty;
            _commentEditor.Text = string.Empty;
            _detailLayout.IsVisible = false;
            return;
        }

        _detailHeaderLabel.Text = _selectedItem.ParzelleDisplayName;
        _detailInfoLabel.Text = $"{_selectedItem.MediumDisplay} · Zähler {_selectedItem.Zaehlernummer} · {_selectedItem.Ablesedatum:dd.MM.yyyy} · Stand {_selectedItem.Stand:0.##} · {_selectedItem.MitgliedDisplay}";
        _ablesedatumPicker.Date = _selectedItem.Ablesedatum.Date;
        _standEntry.Text = _selectedItem.Stand.ToString("0.##", CultureInfo.CurrentCulture);
        _commentEditor.Text = _selectedItem.Pruefkommentar ?? string.Empty;
        _detailLayout.IsVisible = true;
    }

    private async Task EntscheidenAsync(string pruefstatus)
    {
        var selected = _selectedItem;
        if (selected == null || _isBusy)
            return;

        var approverId = ResolveApproverMitgliedId();
        if (!approverId.HasValue)
        {
            SetStatus("Freigabe oder Ablehnung ist nur mit gültigem Mitgliedskontext möglich.");
            return;
        }

        var kommentar = NormalizeComment();
        if (kommentar == null)
        {
            SetStatus("Bitte einen Prüfkommentar eingeben, bevor die Entscheidung gespeichert wird.");
            return;
        }

        _isBusy = true;
        SetStatus(string.Empty);
        UpdateUiState();

        try
        {
            var ok = await _supabaseService.UpdateAblesungPruefstatusAsync(selected.AblesungId, pruefstatus, kommentar, approverId, DateTime.UtcNow);
            SetStatus(ok ? (pruefstatus == AblesungPruefstatus.Freigegeben ? "Ablesung wurde freigegeben." : "Ablesung wurde abgelehnt.") : "Die Entscheidung konnte nicht gespeichert werden.");
            await LoadAsync();
        }
        catch (Exception ex)
        {
            SetStatus(ex.Message);
        }
        finally
        {
            _isBusy = false;
            UpdateUiState();
        }
    }

    private async Task KorrigierenAsync()
    {
        var selected = _selectedItem;
        if (selected == null || _isBusy)
            return;

        var approverId = ResolveApproverMitgliedId();
        if (!approverId.HasValue)
        {
            SetStatus("Korrektur ist nur mit gültigem Mitgliedskontext möglich.");
            return;
        }

        var kommentar = NormalizeComment();
        if (kommentar == null)
        {
            SetStatus("Bitte einen Korrekturkommentar eingeben, bevor die Ablesung korrigiert wird.");
            return;
        }

        if (!TryParseStand(_standEntry.Text, out var stand))
        {
            SetStatus("Bitte einen gültigen Zählerstand für die Korrektur eingeben.");
            return;
        }

        var confirmed = await DisplayAlert("Ablesung korrigieren", "Die eingereichte Ablesung wird mit den geänderten Werten korrigiert und direkt freigegeben. Fortfahren?", "Ja", "Nein");
        if (!confirmed)
            return;

        _isBusy = true;
        SetStatus(string.Empty);
        UpdateUiState();

        try
        {
            var ok = await _supabaseService.CorrectAblesungImPruefprozessAsync(selected.AblesungId, _ablesedatumPicker.Date.Date, stand, kommentar, approverId.Value, DateTime.UtcNow);
            SetStatus(ok ? "Ablesung wurde korrigiert und direkt freigegeben." : "Die Korrektur konnte nicht gespeichert werden.");
            await LoadAsync();
        }
        catch (Exception ex)
        {
            SetStatus(ex.Message);
        }
        finally
        {
            _isBusy = false;
            UpdateUiState();
        }
    }

    private async Task EntfernenAsync()
    {
        var selected = _selectedItem;
        if (selected == null || _isBusy)
            return;

        var approverId = ResolveApproverMitgliedId();
        if (!approverId.HasValue)
        {
            SetStatus("Entfernen ist nur mit gültigem Mitgliedskontext möglich.");
            return;
        }

        var kommentar = NormalizeComment();
        if (kommentar == null)
        {
            SetStatus("Bitte eine Löschbegründung eingeben, bevor die Ablesung entfernt wird.");
            return;
        }

        var confirmed = await DisplayAlert("Ablesung entfernen", "Die Ablesung wird mit Begründung aus dem aktiven Prüfprozess entfernt. Fortfahren?", "Ja", "Nein");
        if (!confirmed)
            return;

        _isBusy = true;
        SetStatus(string.Empty);
        UpdateUiState();

        try
        {
            var ok = await _supabaseService.RemoveAblesungImPruefprozessAsync(selected.AblesungId, kommentar, approverId.Value, DateTime.UtcNow);
            SetStatus(ok ? "Ablesung wurde aus dem aktiven Prüfprozess entfernt." : "Die Ablesung konnte nicht aus dem Prüfprozess entfernt werden.");
            await LoadAsync();
        }
        catch (Exception ex)
        {
            SetStatus(ex.Message);
        }
        finally
        {
            _isBusy = false;
            UpdateUiState();
        }
    }

    private void UpdateUiState()
    {
        var canApprove = PermissionChecks.CanApproveMeterReadings(_userContextState.CurrentUserContext);
        var hasSelection = _selectedItem != null;

        _refreshButton.IsEnabled = !_isBusy && canApprove;
        _list.IsEnabled = !_isBusy && canApprove;
        _ablesedatumPicker.IsEnabled = !_isBusy && canApprove && hasSelection;
        _standEntry.IsEnabled = !_isBusy && canApprove && hasSelection;
        _commentEditor.IsEnabled = !_isBusy && canApprove && hasSelection;
        _approveButton.IsEnabled = !_isBusy && canApprove && hasSelection;
        _rejectButton.IsEnabled = !_isBusy && canApprove && hasSelection;
        _correctButton.IsEnabled = !_isBusy && canApprove && hasSelection;
        _removeButton.IsEnabled = !_isBusy && canApprove && hasSelection;
        _countLabel.Text = _items.Count > 0 ? $"{_items.Count} offene Einreichung(en)" : string.Empty;
        _countLabel.IsVisible = !string.IsNullOrWhiteSpace(_countLabel.Text);
        _emptyLabel.IsVisible = !_isBusy && _items.Count == 0;
    }

    private void SetStatus(string message)
    {
        _statusLabel.Text = message;
        _statusLabel.IsVisible = !string.IsNullOrWhiteSpace(message);
    }

    private int? ResolveApproverMitgliedId()
    {
        var mitgliedId = _userContextState.CurrentMitgliedId;
        if (!mitgliedId.HasValue || mitgliedId.Value <= 0 || mitgliedId.Value > int.MaxValue)
            return null;

        return (int)mitgliedId.Value;
    }

    private string? NormalizeComment()
        => string.IsNullOrWhiteSpace(_commentEditor.Text) ? null : _commentEditor.Text.Trim();

    private static bool TryParseStand(string? input, out decimal stand)
    {
        var normalized = input?.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            stand = 0;
            return false;
        }

        var styles = NumberStyles.Number;
        if (decimal.TryParse(normalized, styles, CultureInfo.CurrentCulture, out stand))
            return stand >= 0;

        if (decimal.TryParse(normalized, styles, CultureInfo.InvariantCulture, out stand))
            return stand >= 0;

        stand = 0;
        return false;
    }

    private static View CreateField(string title, View view)
    {
        return new VerticalStackLayout
        {
            Spacing = 4,
            Children =
            {
                new Label { Text = title, FontAttributes = FontAttributes.Bold },
                view
            }
        };
    }
}
