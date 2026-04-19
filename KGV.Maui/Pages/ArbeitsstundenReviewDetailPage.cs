using KGV.Core.Interfaces;
using KGV.Core.Models;
using KGV.Maui.State;
using Microsoft.Maui;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace KGV.Maui.Pages;

public sealed class ArbeitsstundenReviewDetailPage : ContentPage
{
    private const int LockTimeoutMinutes = 10;
    private static readonly TimeSpan LockHeartbeatInterval = TimeSpan.FromMinutes(3);

    private readonly ISupabaseService _supabaseService;
    private readonly UserContextState _userContextState;
    private readonly ArbeitsstundenReviewState _reviewState;

    private readonly Label _headlineLabel;
    private readonly Label _statusLabel;
    private readonly Label _lockLabel;
    private readonly Label _memberLabel;
    private readonly Label _dateLabel;
    private readonly Label _hoursLabel;
    private readonly Label _workTypeLabel;
    private readonly Label _approvalInfoLabel;
    private readonly Editor _commentEditor;
    private readonly DatePicker _correctionDatePicker;
    private readonly Entry _correctionHoursEntry;
    private readonly Editor _correctionWorkTypeEditor;
    private readonly Button _approveButton;
    private readonly Button _rejectButton;
    private readonly Button _correctButton;
    private readonly Button _deleteButton;
    private readonly Button _previousButton;
    private readonly Button _nextButton;
    private readonly Label _positionLabel;
    private readonly ObservableCollection<ArbeitsstundenPruefverlaufItem> _historyItems = new();
    private readonly CollectionView _historyList;
    private readonly Label _historyEmptyLabel;
    private readonly Label _historyLoadingLabel;
    private CancellationTokenSource? _lockHeartbeatCts;
    private bool _lockAcquired;
    private string? _currentUserId;

    private bool _isBusy;
    private bool _isApplyingEntry;
    private bool _isLoadingHistory;

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
        _lockLabel = new Label { TextColor = Colors.DarkRed, LineBreakMode = LineBreakMode.WordWrap, IsVisible = false };
        _memberLabel = CreateValueLabel();
        _dateLabel = CreateValueLabel();
        _hoursLabel = CreateValueLabel();
        _workTypeLabel = CreateValueLabel();
        _approvalInfoLabel = CreateValueLabel();

        _commentEditor = new Editor
        {
            AutoSize = EditorAutoSizeOption.TextChanges,
            HeightRequest = 100,
            Placeholder = "Prüfkommentar (Pflichtfeld)"
        };
        _commentEditor.TextChanged += (_, _) =>
        {
            if (_isApplyingEntry)
                return;

            UpdateActionState();
        };

        _correctionDatePicker = new DatePicker();
        _correctionDatePicker.DateSelected += (_, _) => UpdateActionState();

        _correctionHoursEntry = new Entry
        {
            Keyboard = Keyboard.Numeric,
            Placeholder = "Stunden"
        };
        _correctionHoursEntry.TextChanged += (_, _) => UpdateActionState();

        _correctionWorkTypeEditor = new Editor
        {
            AutoSize = EditorAutoSizeOption.TextChanges,
            HeightRequest = 90,
            Placeholder = "Art der Arbeit"
        };
        _correctionWorkTypeEditor.TextChanged += (_, _) => UpdateActionState();

        _approveButton = new Button { Text = "Freigeben", BackgroundColor = Colors.LightGreen };
        _approveButton.Clicked += async (_, _) => await FreigebenAsync();

        _rejectButton = new Button { Text = "Ablehnen", BackgroundColor = Colors.LightPink };
        _rejectButton.Clicked += async (_, _) => await AblehnenAsync();

        _correctButton = new Button { Text = "Korrigieren", BackgroundColor = Colors.LightGoldenrodYellow };
        _correctButton.Clicked += async (_, _) => await KorrigierenAsync();

        _deleteButton = new Button { Text = "Löschen", BackgroundColor = Colors.MistyRose };
        _deleteButton.Clicked += async (_, _) => await LoeschenAsync();

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

        _historyLoadingLabel = new Label { Text = "Verlauf wird geladen...", TextColor = Colors.Gray, IsVisible = false };
        _historyEmptyLabel = new Label { Text = "Zu diesem Prüffall liegt noch kein Verlauf vor.", TextColor = Colors.Gray, IsVisible = false };
        _historyList = new CollectionView
        {
            ItemsSource = _historyItems,
            SelectionMode = SelectionMode.None,
            ItemTemplate = new DataTemplate(() =>
            {
                var action = new Label { FontAttributes = FontAttributes.Bold };
                action.SetBinding(Label.TextProperty, nameof(ArbeitsstundenPruefverlaufItem.AktionDisplay));

                var meta = new Label { FontSize = 12, TextColor = Colors.Gray };
                meta.SetBinding(Label.TextProperty, new Binding(nameof(ArbeitsstundenPruefverlaufItem.GeprueftAm), stringFormat: "{0:dd.MM.yyyy HH:mm}"));

                var reviewer = new Label();
                reviewer.SetBinding(Label.TextProperty, nameof(ArbeitsstundenPruefverlaufItem.GeprueftVonName));

                var commentHeadline = new Label { Text = "Kommentar", FontAttributes = FontAttributes.Bold };
                var comment = new Label { LineBreakMode = LineBreakMode.WordWrap };
                comment.SetBinding(Label.TextProperty, nameof(ArbeitsstundenPruefverlaufItem.Kommentar));

                var beforeHeadline = new Label { Text = "Vorher", FontAttributes = FontAttributes.Bold };
                var before = new Label { LineBreakMode = LineBreakMode.WordWrap };
                before.SetBinding(Label.TextProperty, nameof(ArbeitsstundenPruefverlaufItem.VorherSummary));

                var afterHeadline = new Label { Text = "Nachher", FontAttributes = FontAttributes.Bold };
                var after = new Label { LineBreakMode = LineBreakMode.WordWrap };
                after.SetBinding(Label.TextProperty, nameof(ArbeitsstundenPruefverlaufItem.NachherSummary));

                return new Border
                {
                    Stroke = Colors.LightGray,
                    Padding = 12,
                    Margin = new Thickness(0, 0, 0, 10),
                    Content = new VerticalStackLayout
                    {
                        Spacing = 4,
                        Children = { action, meta, reviewer, commentHeadline, comment, beforeHeadline, before, afterHeadline, after }
                    }
                };
            })
        };

        var actionGrid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Star)
            },
            RowDefinitions = new RowDefinitionCollection
            {
                new RowDefinition(GridLength.Auto),
                new RowDefinition(GridLength.Auto)
            },
            ColumnSpacing = 8,
            RowSpacing = 8,
            Children =
            {
                _approveButton,
                _rejectButton,
                _correctButton,
                _deleteButton
            }
        };

        Grid.SetColumn(_approveButton, 0);
        Grid.SetColumn(_rejectButton, 1);
        Grid.SetRow(_approveButton, 0);
        Grid.SetRow(_rejectButton, 0);
        Grid.SetColumn(_correctButton, 0);
        Grid.SetColumn(_deleteButton, 1);
        Grid.SetRow(_correctButton, 1);
        Grid.SetRow(_deleteButton, 1);

        var navigationGrid = new Grid
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
        };
        Grid.SetColumn(_previousButton, 0);
        Grid.SetColumn(_positionLabel, 1);
        Grid.SetColumn(_nextButton, 2);

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
                        Text = "Ein Prüffall pro Seite. Alle vier Aktionen laufen über denselben Prüfservice; der Prüfkommentar ist immer verpflichtend.",
                        TextColor = Colors.Gray,
                        LineBreakMode = LineBreakMode.WordWrap
                    },
                    _lockLabel,
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
                        new Label { Text = "Prüfkommentar *", FontAttributes = FontAttributes.Bold },
                        _commentEditor,
                        new Label
                        {
                            Text = "Korrekturwerte werden nur für die Aktion Korrigieren verwendet. Freigeben, Ablehnen und Löschen verlangen ebenfalls denselben Pflichtkommentar.",
                            TextColor = Colors.Gray,
                            LineBreakMode = LineBreakMode.WordWrap
                        },
                        new Label { Text = "Korrekturdatum", FontAttributes = FontAttributes.Bold },
                        _correctionDatePicker,
                        new Label { Text = "Korrigierte Stunden", FontAttributes = FontAttributes.Bold },
                        _correctionHoursEntry,
                        new Label { Text = "Korrigierte Art der Arbeit", FontAttributes = FontAttributes.Bold },
                        _correctionWorkTypeEditor,
                        actionGrid),
                    navigationGrid,
                    CreateSection(
                        "Verlauf",
                        _historyLoadingLabel,
                        _historyEmptyLabel,
                        _historyList)
                }
            }
        };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        _currentUserId = ResolveCurrentUserId();
        var lockResult = await EnsureReviewLockAsync();
        if (!lockResult.Acquired)
        {
            ShowLockedState();
            return;
        }

        await LoadCurrentEntryAsync(refreshEntries: _reviewState.TotalCount == 0);
    }

    protected override async void OnDisappearing()
    {
        base.OnDisappearing();
        await ReleaseReviewLockAsync();
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
            _commentEditor.Text = string.Empty;
            _correctionHoursEntry.Text = string.Empty;
            _correctionWorkTypeEditor.Text = string.Empty;
            _historyItems.Clear();
            UpdateHistoryState();
            UpdateActionState();
            return;
        }

        _isApplyingEntry = true;
        try
        {
            _headlineLabel.Text = $"Prüffall: {BuildMemberDisplay(entry)}";
            _memberLabel.Text = BuildMemberDisplay(entry);
            _dateLabel.Text = entry.Datum.ToString("dd.MM.yyyy");
            _hoursLabel.Text = entry.Stunden.ToString("0.##", CultureInfo.CurrentCulture);
            _workTypeLabel.Text = string.IsNullOrWhiteSpace(entry.Beschreibung) ? "-" : entry.Beschreibung.Trim();
            _approvalInfoLabel.Text = entry.Freigegeben
                ? $"Freigegeben am {entry.FreigegebenAm:dd.MM.yyyy HH:mm}"
                : "Offener Prüffall";
            _commentEditor.Text = string.Empty;
            _correctionDatePicker.Date = entry.Datum.Date;
            _correctionHoursEntry.Text = entry.Stunden.ToString("0.##", CultureInfo.CurrentCulture);
            _correctionWorkTypeEditor.Text = entry.Beschreibung ?? string.Empty;
            _positionLabel.Text = $"{_reviewState.CurrentIndex + 1}/{_reviewState.TotalCount}";
        }
        finally
        {
            _isApplyingEntry = false;
        }

        await LoadHistoryAsync(entry.Id);
        UpdateActionState();
    }

    private async Task RefreshEntriesAsync(int? selectedEntryId)
    {
        var entries = await _supabaseService.GetOffeneArbeitsstundenZurFreigabeAsync();
        _reviewState.SetEntries(entries, selectedEntryId);

        if (Shell.Current is AdminShell shell)
            await shell.RefreshWorkhoursReviewMenuAsync();
    }

    private async Task LoadHistoryAsync(int arbeitsstundeId)
    {
        _isLoadingHistory = true;
        UpdateHistoryState();

        try
        {
            var items = await _supabaseService.GetArbeitsstundenPruefverlaufAsync(arbeitsstundeId);
            _historyItems.Clear();
            foreach (var item in items)
                _historyItems.Add(item);
        }
        finally
        {
            _isLoadingHistory = false;
            UpdateHistoryState();
        }
    }

    private async Task FreigebenAsync()
    {
        if (!TryGetReviewKommentar(out var kommentar) || !TryResolveApproverId(out var approverId))
            return;

        await ExecuteReviewActionAsync(
            async () => await _supabaseService.ApproveArbeitsstundeImPruefprozessAsync(_reviewState.CurrentEntry!.Id, kommentar, approverId),
            "Prüffall wurde freigegeben.");
    }

    private async Task AblehnenAsync()
    {
        if (!TryGetReviewKommentar(out var kommentar) || !TryResolveApproverId(out var approverId))
            return;

        await ExecuteReviewActionAsync(
            async () => await _supabaseService.RejectArbeitsstundeImPruefprozessAsync(_reviewState.CurrentEntry!.Id, kommentar, approverId),
            "Prüffall wurde abgelehnt und aus der offenen Liste entfernt.");
    }

    private async Task KorrigierenAsync()
    {
        if (!TryGetReviewKommentar(out var kommentar) || !TryResolveApproverId(out var approverId))
            return;

        var entry = _reviewState.CurrentEntry;
        if (entry == null)
            return;

        if (!TryParseHours(_correctionHoursEntry.Text, out var stunden) || stunden <= 0)
        {
            _statusLabel.Text = "Für die Korrektur müssen Stunden größer als 0 angegeben werden.";
            return;
        }

        if (string.IsNullOrWhiteSpace(_correctionWorkTypeEditor.Text))
        {
            _statusLabel.Text = "Für die Korrektur ist die Art der Arbeit erforderlich.";
            return;
        }

        var request = new ArbeitsstundenPruefkorrekturRequest
        {
            ArbeitsstundeId = entry.Id,
            Datum = _correctionDatePicker.Date,
            Stunden = stunden,
            ArtDerArbeit = _correctionWorkTypeEditor.Text.Trim(),
            Begruendung = kommentar,
            GeprueftVon = approverId
        };

        await ExecuteReviewActionAsync(
            async () => await _supabaseService.CorrectArbeitsstundeImPruefprozessAsync(request),
            "Prüffall wurde korrigiert, freigegeben und im Verlauf dokumentiert.");
    }

    private async Task LoeschenAsync()
    {
        if (!TryGetReviewKommentar(out var kommentar) || !TryResolveApproverId(out var approverId))
            return;

        var entry = _reviewState.CurrentEntry;
        if (entry == null)
            return;

        var confirm = await DisplayAlert(
            "Arbeitsstunde löschen",
            $"Soll die Arbeitsstunde von {BuildMemberDisplay(entry)} wirklich im Prüfprozess gelöscht werden?",
            "Ja",
            "Nein");

        if (!confirm)
            return;

        await ExecuteReviewActionAsync(
            async () => await _supabaseService.DeleteArbeitsstundeImPruefprozessAsync(entry.Id, kommentar, approverId),
            "Prüffall wurde gelöscht. Der Verlauf bleibt nachvollziehbar erhalten.");
    }

    private async Task ExecuteReviewActionAsync(Func<Task<bool>> action, string successMessage)
    {
        var entry = _reviewState.CurrentEntry;
        if (!_lockAcquired || entry == null || _isBusy)
            return;

        _isBusy = true;
        UpdateActionState();

        try
        {
            // Debug: log start of review action
            System.Diagnostics.Debug.WriteLine($"KGV: ArbeitsstundenReviewDetailPage: ExecuteReviewActionAsync START. Message='{successMessage}', CurrentId={entry.Id}, CurrentIndex={_reviewState.CurrentIndex}, TotalCount={_reviewState.TotalCount}");
            Console.WriteLine($"KGV: ArbeitsstundenReviewDetailPage: ExecuteReviewActionAsync START. Message='{successMessage}', CurrentId={entry.Id}, CurrentIndex={_reviewState.CurrentIndex}, TotalCount={_reviewState.TotalCount}");

            var success = await action();
            if (!success)
            {
                _statusLabel.Text = "Die Prüfaktion konnte nicht ausgeführt werden. Details stehen im Anwendungslog oder der Datensatz ist nicht mehr offen.";
                System.Diagnostics.Debug.WriteLine($"KGV: ArbeitsstundenReviewDetailPage: ExecuteReviewActionAsync ACTION FAILED for Id={entry.Id}");
                Console.WriteLine($"KGV: ArbeitsstundenReviewDetailPage: ExecuteReviewActionAsync ACTION FAILED for Id={entry.Id}");
                return;
            }

            _statusLabel.Text = successMessage;
            var currentId = entry.Id;
            System.Diagnostics.Debug.WriteLine($"KGV: ArbeitsstundenReviewDetailPage: ExecuteReviewActionAsync ACTION SUCCESS for Id={currentId}");
            Console.WriteLine($"KGV: ArbeitsstundenReviewDetailPage: ExecuteReviewActionAsync ACTION SUCCESS for Id={currentId}");

            // reload entries and log state after reload
            await RefreshEntriesAsync(currentId);
            var afterCount = _reviewState.TotalCount;
            var afterIndex = _reviewState.CurrentIndex;
            var afterCurrentId = _reviewState.CurrentEntry?.Id;
            var currentEntryNull = _reviewState.CurrentEntry == null;
            System.Diagnostics.Debug.WriteLine($"KGV: ArbeitsstundenReviewDetailPage: After RefreshEntriesAsync: Count={afterCount}, CurrentIndex={afterIndex}, CurrentId={afterCurrentId}, CurrentEntryNull={currentEntryNull}");
            Console.WriteLine($"KGV: ArbeitsstundenReviewDetailPage: After RefreshEntriesAsync: Count={afterCount}, CurrentIndex={afterIndex}, CurrentId={afterCurrentId}, CurrentEntryNull={currentEntryNull}");

            // Decide navigation or loading next
            if (_reviewState.CurrentEntry == null)
            {
                System.Diagnostics.Debug.WriteLine($"KGV: ArbeitsstundenReviewDetailPage: No current entry after refresh -> navigating back. ProcessedId={currentId}");
                Console.WriteLine($"KGV: ArbeitsstundenReviewDetailPage: No current entry after refresh -> navigating back. ProcessedId={currentId}");
                await Shell.Current.GoToAsync("..");
                return;
            }

            System.Diagnostics.Debug.WriteLine($"KGV: ArbeitsstundenReviewDetailPage: Loading current entry after refresh. ProcessedId={currentId}, NewCurrentId={_reviewState.CurrentEntry.Id}");
            Console.WriteLine($"KGV: ArbeitsstundenReviewDetailPage: Loading current entry after refresh. ProcessedId={currentId}, NewCurrentId={_reviewState.CurrentEntry.Id}");

            await LoadCurrentEntryAsync(refreshEntries: false);

            System.Diagnostics.Debug.WriteLine($"KGV: ArbeitsstundenReviewDetailPage: After LoadCurrentEntryAsync final displayed Id={_reviewState.CurrentEntry?.Id}");
            Console.WriteLine($"KGV: ArbeitsstundenReviewDetailPage: After LoadCurrentEntryAsync final displayed Id={_reviewState.CurrentEntry?.Id}");
        }
        catch (Exception ex)
        {
            _statusLabel.Text = ex.Message;
            System.Diagnostics.Debug.WriteLine($"KGV: ArbeitsstundenReviewDetailPage: ExecuteReviewActionAsync EXCEPTION: {ex.GetType().Name}: {ex.Message}");
            Console.WriteLine($"KGV: ArbeitsstundenReviewDetailPage: ExecuteReviewActionAsync EXCEPTION: {ex.GetType().Name}: {ex.Message}");
        }
        finally
        {
            _isBusy = false;
            UpdateActionState();
        }
    }

    private async Task NavigateRelativeAsync(int offset)
    {
        if (_reviewState.CurrentEntry == null || _isBusy)
            return;

        var canMove = offset < 0 ? _reviewState.CanMovePrevious : _reviewState.CanMoveNext;
        if (!canMove)
            return;

        if (offset < 0)
            _reviewState.MovePrevious();
        else
            _reviewState.MoveNext();

        await LoadCurrentEntryAsync(refreshEntries: false);
    }

    private void UpdateActionState()
    {
        var hasEntry = _reviewState.CurrentEntry != null;
        var hasComment = ArbeitsstundenPruefprozess.HasRequiredKommentar(_commentEditor.Text);
        _approveButton.IsEnabled = _lockAcquired && hasEntry && !_isBusy && hasComment;
        _rejectButton.IsEnabled = _lockAcquired && hasEntry && !_isBusy && hasComment;
        _correctButton.IsEnabled = _lockAcquired && hasEntry && !_isBusy && hasComment;
        _deleteButton.IsEnabled = _lockAcquired && hasEntry && !_isBusy && hasComment;
        _previousButton.IsEnabled = _lockAcquired && hasEntry && !_isBusy && _reviewState.CanMovePrevious;
        _nextButton.IsEnabled = _lockAcquired && hasEntry && !_isBusy && _reviewState.CanMoveNext;
        _commentEditor.IsEnabled = _lockAcquired && hasEntry && !_isBusy;
        _correctionDatePicker.IsEnabled = _lockAcquired && hasEntry && !_isBusy;
        _correctionHoursEntry.IsEnabled = _lockAcquired && hasEntry && !_isBusy;
        _correctionWorkTypeEditor.IsEnabled = _lockAcquired && hasEntry && !_isBusy;
    }

    private async Task<ArbeitsstundenReviewLockResult> EnsureReviewLockAsync()
    {
        if (string.IsNullOrWhiteSpace(_currentUserId))
        {
            _lockAcquired = false;
            SetLockMessage("Die Prüfsperre konnte nicht gesetzt werden, weil keine aktuelle Benutzer-ID verfügbar ist.");
            UpdateActionState();
            return new ArbeitsstundenReviewLockResult();
        }

        var result = await _supabaseService.TryAcquireArbeitsstundenReviewLockAsync(_currentUserId, LockTimeoutMinutes);
        _lockAcquired = result.Acquired;
        if (_lockAcquired)
        {
            SetLockMessage("Prüfsitzung aktiv. Offene Arbeitsstunden sind während dieser mobilen Sitzung global für andere Prüfer gesperrt.");
            StartLockHeartbeat();
        }
        else
        {
            StopLockHeartbeat();
            SetLockMessage(BuildForeignLockMessage(result));
        }

        UpdateActionState();
        return result;
    }

    private async Task ReleaseReviewLockAsync()
    {
        StopLockHeartbeat();
        if (!_lockAcquired || string.IsNullOrWhiteSpace(_currentUserId))
            return;

        try
        {
            await _supabaseService.ReleaseArbeitsstundenReviewLockAsync(_currentUserId);
        }
        catch
        {
        }
        finally
        {
            _lockAcquired = false;
            UpdateActionState();
        }
    }

    private void StartLockHeartbeat()
    {
        StopLockHeartbeat();
        var cts = new CancellationTokenSource();
        _lockHeartbeatCts = cts;
        _ = Task.Run(() => RunLockHeartbeatAsync(cts.Token));
    }

    private void StopLockHeartbeat()
    {
        var cts = _lockHeartbeatCts;
        _lockHeartbeatCts = null;
        if (cts == null)
            return;

        try
        {
            cts.Cancel();
            cts.Dispose();
        }
        catch
        {
        }
    }

    private async Task RunLockHeartbeatAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var timer = new PeriodicTimer(LockHeartbeatInterval);
            while (await timer.WaitForNextTickAsync(cancellationToken))
            {
                if (!_lockAcquired || string.IsNullOrWhiteSpace(_currentUserId))
                    return;

                var ok = await _supabaseService.RefreshArbeitsstundenReviewLockAsync(_currentUserId, LockTimeoutMinutes);
                if (ok)
                    continue;

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    StopLockHeartbeat();
                    _lockAcquired = false;
                    SetLockMessage("Die globale Prüfsperre konnte nicht verlängert werden. Bitte die Seite neu öffnen, bevor weitere Aktionen ausgeführt werden.");
                    _statusLabel.Text = string.Empty;
                    UpdateActionState();
                });
                return;
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    private string? ResolveCurrentUserId()
    {
        var userId = _userContextState.CurrentUserContext?.UserId;
        return userId.HasValue && userId.Value != Guid.Empty ? userId.Value.ToString() : null;
    }

    private void SetLockMessage(string? message)
    {
        _lockLabel.Text = message ?? string.Empty;
        _lockLabel.IsVisible = !string.IsNullOrWhiteSpace(_lockLabel.Text);
    }

    private void ShowLockedState()
    {
        _headlineLabel.Text = "Prüfsitzung nicht verfügbar";
        _memberLabel.Text = "-";
        _dateLabel.Text = "-";
        _hoursLabel.Text = "-";
        _workTypeLabel.Text = "-";
        _approvalInfoLabel.Text = "Die mobile Arbeitsstundenprüfung ist aktuell global gesperrt.";
        _positionLabel.Text = "0/0";
        _commentEditor.Text = string.Empty;
        _correctionHoursEntry.Text = string.Empty;
        _correctionWorkTypeEditor.Text = string.Empty;
        _historyItems.Clear();
        UpdateHistoryState();
        UpdateActionState();
    }

    private void UpdateHistoryState()
    {
        _historyLoadingLabel.IsVisible = _isLoadingHistory;
        _historyList.IsVisible = !_isLoadingHistory && _historyItems.Count > 0;
        _historyEmptyLabel.IsVisible = !_isLoadingHistory && _historyItems.Count == 0 && _reviewState.CurrentEntry != null;
    }

    private bool TryGetReviewKommentar(out string kommentar)
    {
        kommentar = ArbeitsstundenPruefprozess.NormalizeKommentar(_commentEditor.Text);
        if (ArbeitsstundenPruefprozess.HasRequiredKommentar(kommentar))
            return true;

        _statusLabel.Text = "Für Freigeben, Ablehnen, Korrigieren und Löschen ist ein Prüfkommentar verpflichtend.";
        return false;
    }

    private bool TryResolveApproverId(out int approverId)
    {
        approverId = 0;
        var mitgliedId = _userContextState.CurrentMitgliedId;
        if (mitgliedId.HasValue && mitgliedId.Value > 0 && mitgliedId.Value <= int.MaxValue)
        {
            approverId = (int)mitgliedId.Value;
            return true;
        }

        _statusLabel.Text = "Genehmiger-MitgliedId fehlt.";
        return false;
    }

    private static bool TryParseHours(string? value, out decimal stunden)
    {
        var normalized = string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Trim().Replace(',', '.');

        return decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out stunden)
               || decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.CurrentCulture, out stunden);
    }

    private static string BuildMemberDisplay(ArbeitsstundeDTO entry)
    {
        var display = $"{entry.Nachname} {entry.Vorname}".Trim();
        return string.IsNullOrWhiteSpace(display) ? $"Mitglied {entry.MitgliedId}" : display;
    }

    private static string BuildForeignLockMessage(ArbeitsstundenReviewLockResult result)
    {
        var lockedBy = string.IsNullOrWhiteSpace(result.LockedByDisplayName)
            ? (!string.IsNullOrWhiteSpace(result.LockedByUserId) ? result.LockedByUserId : "einen anderen Prüfer")
            : result.LockedByDisplayName;

        if (result.LockedAt.HasValue)
            return $"Die Freigabeansicht ist aktuell global durch {lockedBy} gesperrt (seit {result.LockedAt.Value.ToLocalTime().ToString("dd.MM.yyyy HH:mm", CultureInfo.CurrentCulture)}). Bitte warte auf die Freigabe oder auf das Timeout einer hängenden Sitzung.";

        return $"Die Freigabeansicht ist aktuell global durch {lockedBy} gesperrt. Bitte warte auf die Freigabe oder auf das Timeout einer hängenden Sitzung.";
    }

    private static Label CreateValueLabel()
    {
        return new Label { LineBreakMode = LineBreakMode.WordWrap };
    }

    private static Border CreateReadonlyField(string title, View valueView)
    {
        return new Border
        {
            Stroke = Colors.LightGray,
            Padding = 10,
            Content = new VerticalStackLayout
            {
                Spacing = 4,
                Children =
                {
                    new Label { Text = title, FontAttributes = FontAttributes.Bold },
                    valueView
                }
            }
        };
    }

    private static Border CreateSection(string title, params View[] content)
    {
        var stack = new VerticalStackLayout { Spacing = 10 };
        stack.Children.Add(new Label { Text = title, FontSize = 18, FontAttributes = FontAttributes.Bold });
        foreach (var item in content)
            stack.Children.Add(item);

        return new Border
        {
            Stroke = Colors.LightGray,
            Padding = 16,
            Content = stack
        };
    }
}
