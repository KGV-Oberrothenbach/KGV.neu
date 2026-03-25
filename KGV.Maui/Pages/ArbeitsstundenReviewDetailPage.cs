using KGV.Core.Interfaces;
using KGV.Core.Models;
using KGV.Maui.State;

namespace KGV.Maui.Pages;

public sealed class ArbeitsstundenReviewDetailPage : ContentPage
{
    private readonly ISupabaseService _supabaseService;
    private readonly UserContextState _userContextState;
    private readonly ArbeitsstundenReviewState _reviewState;

    private readonly Label _headlineLabel;
    private readonly Label _statusLabel;
    private readonly Label _memberLabel;
    private readonly Label _dateLabel;
    private readonly Label _hoursLabel;
    private readonly Label _workTypeLabel;
    private readonly Label _approvalInfoLabel;
    private readonly Editor _statusEditor;
    private readonly Button _saveButton;
    private readonly Button _approveButton;
    private readonly Button _rejectButton;
    private readonly Button _previousButton;
    private readonly Button _nextButton;
    private readonly Label _positionLabel;

    private bool _isBusy;
    private bool _isApplyingEntry;
    private string _originalStatusText = string.Empty;

    public ArbeitsstundenReviewDetailPage(
        ISupabaseService supabaseService,
        UserContextState userContextState,
        ArbeitsstundenReviewState reviewState)
    {
        _supabaseService = supabaseService;
        _userContextState = userContextState;
        _reviewState = reviewState;

        Title = "Arbeitsstunden prüfen";

        _headlineLabel = new Label { FontSize = 24, FontAttributes = FontAttributes.Bold, LineBreakMode = LineBreakMode.WordWrap };
        _statusLabel = new Label { TextColor = Colors.DarkSlateBlue, LineBreakMode = LineBreakMode.WordWrap };
        _memberLabel = CreateValueLabel();
        _dateLabel = CreateValueLabel();
        _hoursLabel = CreateValueLabel();
        _workTypeLabel = CreateValueLabel();
        _approvalInfoLabel = CreateValueLabel();

        _statusEditor = new Editor
        {
            AutoSize = EditorAutoSizeOption.TextChanges,
            HeightRequest = 110,
            Placeholder = "Status / Anmerkung"
        };
        _statusEditor.TextChanged += (_, _) =>
        {
            if (_isApplyingEntry)
                return;

            UpdateActionState();
        };

        _saveButton = new Button { Text = "Speichern" };
        _saveButton.Clicked += async (_, _) => await SaveAsync();

        _approveButton = new Button { Text = "Freigeben", BackgroundColor = Colors.LightGreen };
        _approveButton.Clicked += async (_, _) => await ApplyDecisionAsync(true);

        _rejectButton = new Button { Text = "Ablehnen", BackgroundColor = Colors.LightPink };
        _rejectButton.Clicked += async (_, _) => await ApplyDecisionAsync(false);

        _previousButton = new Button { Text = "←", WidthRequest = 56 };
        _previousButton.Clicked += async (_, _) => await NavigateRelativeAsync(-1);

        _nextButton = new Button { Text = "→", WidthRequest = 56 };
        _nextButton.Clicked += async (_, _) => await NavigateRelativeAsync(1);

        _positionLabel = new Label
        {
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center,
            FontAttributes = FontAttributes.Bold
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
                        Text = "Ein offener Prüffall pro Seite. Daten oben, Entscheidung unten; bei offenen Änderungen wird vor dem Blättern zuerst gespeichert.",
                        TextColor = Colors.Gray,
                        LineBreakMode = LineBreakMode.WordWrap
                    },
                    _statusLabel,
                    CreateSection(
                        "Prüffall",
                        CreateReadonlyField("Mitglied", _memberLabel),
                        CreateReadonlyField("Datum", _dateLabel),
                        CreateReadonlyField("Stunden", _hoursLabel),
                        CreateReadonlyField("Art der Arbeit", _workTypeLabel),
                        CreateReadonlyField("Freigabe", _approvalInfoLabel)),
                    CreateSection(
                        "Prüfung / Entscheidung",
                        new Label { Text = "Status / Anmerkung", FontAttributes = FontAttributes.Bold },
                        _statusEditor,
                        _saveButton,
                        new HorizontalStackLayout
                        {
                            Spacing = 8,
                            Children = { _approveButton, _rejectButton }
                        }),
                    new Grid
                    {
                        ColumnDefinitions = new ColumnDefinitionCollection
                        {
                            new ColumnDefinition(GridLength.Auto),
                            new ColumnDefinition(GridLength.Star),
                            new ColumnDefinition(GridLength.Auto)
                        },
                        Children =
                        {
                            _previousButton,
                            _positionLabel,
                            _nextButton
                        }
                    }
                }
            }
        };

        Grid.SetColumn(_previousButton, 0);
        Grid.SetColumn(_positionLabel, 1);
        Grid.SetColumn(_nextButton, 2);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadCurrentEntryAsync(refreshEntries: _reviewState.TotalCount == 0);
    }

    private async Task LoadCurrentEntryAsync(bool refreshEntries)
    {
        _statusLabel.Text = string.Empty;

        if (refreshEntries)
            await RefreshEntriesAsync(_reviewState.CurrentEntry?.Id);

        var entry = _reviewState.CurrentEntry;
        if (entry == null)
        {
            _headlineLabel.Text = "Kein offener Prüffall";
            _memberLabel.Text = "-";
            _dateLabel.Text = "-";
            _hoursLabel.Text = "-";
            _workTypeLabel.Text = "-";
            _approvalInfoLabel.Text = "Aktuell liegen keine offenen Prüffälle vor.";
            _positionLabel.Text = "0/0";
            UpdateActionState();
            return;
        }

        _isApplyingEntry = true;
        try
        {
            _headlineLabel.Text = $"Prüffall: {BuildMemberDisplay(entry)}";
            _memberLabel.Text = BuildMemberDisplay(entry);
            _dateLabel.Text = entry.Datum.ToString("dd.MM.yyyy");
            _hoursLabel.Text = entry.Stunden.ToString("0.##", System.Globalization.CultureInfo.CurrentCulture);
            _workTypeLabel.Text = string.IsNullOrWhiteSpace(entry.Beschreibung) ? "-" : entry.Beschreibung.Trim();
            _approvalInfoLabel.Text = entry.Freigegeben
                ? $"Freigegeben am {entry.FreigegebenAm:dd.MM.yyyy HH:mm}"
                : "Offener Prüffall";
            _statusEditor.Text = entry.Status ?? string.Empty;
            _originalStatusText = NormalizeStatus(entry.Status);
            _positionLabel.Text = $"{_reviewState.CurrentIndex + 1}/{_reviewState.TotalCount}";
        }
        finally
        {
            _isApplyingEntry = false;
        }

        UpdateActionState();
    }

    private async Task RefreshEntriesAsync(int? selectedEntryId)
    {
        var entries = await _supabaseService.GetOffeneArbeitsstundenZurFreigabeAsync();
        _reviewState.SetEntries(entries, selectedEntryId);

        if (Shell.Current is AdminShell shell)
            await shell.RefreshWorkhoursReviewMenuAsync();
    }

    private async Task SaveAsync()
    {
        await SaveCurrentAsync(saveAsDecision: null);
    }

    private async Task ApplyDecisionAsync(bool approve)
    {
        var saved = await SaveCurrentAsync(approve);
        if (!saved)
            return;

        if (_reviewState.CurrentEntry == null)
        {
            await Shell.Current.GoToAsync("..");
            return;
        }

        await LoadCurrentEntryAsync(refreshEntries: false);
    }

    private async Task NavigateRelativeAsync(int offset)
    {
        if (_reviewState.CurrentEntry == null || _isBusy)
            return;

        var canMove = offset < 0 ? _reviewState.CanMovePrevious : _reviewState.CanMoveNext;
        if (!canMove)
            return;

        if (HasPendingChanges())
        {
            var saved = await SaveCurrentAsync(saveAsDecision: null);
            if (!saved)
                return;
        }

        if (offset < 0)
            _reviewState.MovePrevious();
        else
            _reviewState.MoveNext();

        await LoadCurrentEntryAsync(refreshEntries: false);
    }

    private async Task<bool> SaveCurrentAsync(bool? saveAsDecision)
    {
        var entry = _reviewState.CurrentEntry;
        if (entry == null || _isBusy)
            return false;

        _isBusy = true;
        UpdateActionState();

        try
        {
            var normalizedStatus = BuildPersistedStatus(saveAsDecision, _statusEditor.Text);
            var approverId = ResolveApproverId();
            if (saveAsDecision.HasValue && !approverId.HasValue)
            {
                _statusLabel.Text = "Genehmiger-MitgliedId fehlt.";
                return false;
            }

            var record = new ArbeitsstundeRecord
            {
                Id = entry.Id,
                MitgliedId = entry.MitgliedId,
                SaisonId = entry.SaisonId,
                Datum = entry.Datum.Date,
                Stunden = entry.Stunden,
                ArtDerArbeit = entry.Beschreibung,
                Status = string.IsNullOrWhiteSpace(normalizedStatus) ? null : normalizedStatus,
                Freigegeben = saveAsDecision == true,
                GenehmigtAm = saveAsDecision == true ? DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified) : null,
                GenehmigtVon = saveAsDecision == true ? approverId : null
            };

            var success = await _supabaseService.UpdateArbeitsstundeAsync(record);
            if (!success)
            {
                _statusLabel.Text = "Arbeitsstunde konnte nicht gespeichert werden.";
                return false;
            }

            var currentId = entry.Id;
            await RefreshEntriesAsync(currentId);

            if (saveAsDecision == true)
                _statusLabel.Text = "Prüffall wurde freigegeben.";
            else if (saveAsDecision == false)
                _statusLabel.Text = "Prüffall wurde abgelehnt und aus der offenen Liste entfernt.";
            else
                _statusLabel.Text = "Änderungen gespeichert.";

            if (_reviewState.CurrentEntry == null)
                return true;

            await LoadCurrentEntryAsync(refreshEntries: false);
            return true;
        }
        catch (Exception ex)
        {
            _statusLabel.Text = ex.Message;
            return false;
        }
        finally
        {
            _isBusy = false;
            UpdateActionState();
        }
    }

    private void UpdateActionState()
    {
        var hasEntry = _reviewState.CurrentEntry != null;
        _saveButton.IsEnabled = hasEntry && !_isBusy && HasPendingChanges();
        _approveButton.IsEnabled = hasEntry && !_isBusy;
        _rejectButton.IsEnabled = hasEntry && !_isBusy;
        _previousButton.IsEnabled = hasEntry && !_isBusy && _reviewState.CanMovePrevious;
        _nextButton.IsEnabled = hasEntry && !_isBusy && _reviewState.CanMoveNext;
        _statusEditor.IsEnabled = hasEntry && !_isBusy;
    }

    private bool HasPendingChanges()
    {
        return !string.Equals(_originalStatusText, NormalizeStatus(_statusEditor.Text), StringComparison.Ordinal);
    }

    private int? ResolveApproverId()
    {
        return _userContextState.CurrentMitgliedId is > 0 and <= int.MaxValue
            ? (int)_userContextState.CurrentMitgliedId.Value
            : null;
    }

    private static string NormalizeStatus(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    private static string BuildPersistedStatus(bool? saveAsDecision, string? editorText)
    {
        var normalized = NormalizeStatus(editorText);

        return saveAsDecision switch
        {
            true => normalized,
            false when string.IsNullOrWhiteSpace(normalized) => "abgelehnt",
            false when normalized.StartsWith("abgelehnt", StringComparison.OrdinalIgnoreCase) => normalized,
            false => $"abgelehnt: {normalized}",
            _ => normalized
        };
    }

    private static string BuildMemberDisplay(ArbeitsstundeDTO entry)
    {
        var member = $"{entry.Nachname}, {entry.Vorname}".Trim(' ', ',');
        return string.IsNullOrWhiteSpace(member) ? "Unbekanntes Mitglied" : member;
    }

    private static Label CreateValueLabel()
    {
        return new Label { LineBreakMode = LineBreakMode.WordWrap };
    }

    private static View CreateReadonlyField(string title, View valueView)
    {
        return new VerticalStackLayout
        {
            Spacing = 2,
            Children =
            {
                new Label { Text = title, FontAttributes = FontAttributes.Bold, FontSize = 12, TextColor = Colors.Gray },
                valueView
            }
        };
    }

    private static View CreateSection(string title, params View[] children)
    {
        var stack = new VerticalStackLayout { Spacing = 8 };
        stack.Children.Add(new Label { Text = title, FontAttributes = FontAttributes.Bold, FontSize = 18 });
        foreach (var child in children)
            stack.Children.Add(child);

        return new Border
        {
            Stroke = Colors.LightGray,
            Padding = 14,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = new CornerRadius(14) },
            Content = stack
        };
    }
}
