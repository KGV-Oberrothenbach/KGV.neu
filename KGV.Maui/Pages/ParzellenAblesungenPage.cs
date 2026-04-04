using KGV.Core.Interfaces;
using KGV.Core.Models;
using KGV.Core.Security;
using KGV.Maui.State;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace KGV.Maui.Pages;

public sealed class ParzellenAblesungenPage : ContentPage, IQueryAttributable
{
    private readonly ISupabaseService _supabaseService;
    private readonly ParzellenContextState _parzellenContextState;
    private readonly UserContextState _userContextState;
    private readonly Button _submitReadingButton;
    private readonly Label _submissionHintLabel;
    private int? _parzelleId;
    private string _medium = "strom";
    private bool _pendingLoad;

    public ParzellenAblesungenPage(ISupabaseService supabaseService, ParzellenContextState parzellenContextState, UserContextState userContextState)
    {
        _supabaseService = supabaseService ?? throw new ArgumentNullException(nameof(supabaseService));
        _parzellenContextState = parzellenContextState ?? throw new ArgumentNullException(nameof(parzellenContextState));
        _userContextState = userContextState ?? throw new ArgumentNullException(nameof(userContextState));

        BindingContext = this;
        Title = "Ablesungen";

        var titleLabel = new Label { FontSize = 24, FontAttributes = FontAttributes.Bold };
        titleLabel.SetBinding(Label.TextProperty, nameof(PageTitle));

        var parzelleLabel = new Label { FontAttributes = FontAttributes.Bold, FontSize = 16 };
        parzelleLabel.SetBinding(Label.TextProperty, nameof(ParzelleDisplayText));

        var hintLabel = new Label { TextColor = Colors.Gray, LineBreakMode = Microsoft.Maui.LineBreakMode.WordWrap };
        hintLabel.SetBinding(Label.TextProperty, nameof(HintText));

        var loadingIndicator = new ActivityIndicator { Color = Colors.DarkSlateBlue };
        loadingIndicator.SetBinding(ActivityIndicator.IsRunningProperty, nameof(IsBusy));
        loadingIndicator.SetBinding(IsVisibleProperty, nameof(IsBusy));

        var emptyLabel = new Label
        {
            TextColor = Colors.Gray,
            HorizontalTextAlignment = Microsoft.Maui.TextAlignment.Center,
            VerticalTextAlignment = Microsoft.Maui.TextAlignment.Center,
            Margin = new Microsoft.Maui.Thickness(0, 24, 0, 0)
        };
        emptyLabel.SetBinding(Label.TextProperty, nameof(EmptyText));

        var list = new CollectionView
        {
            SelectionMode = SelectionMode.None,
            EmptyView = emptyLabel,
            ItemTemplate = new DataTemplate(() =>
            {
                var datumLabel = new Label { FontSize = 16, FontAttributes = FontAttributes.Bold };
                datumLabel.SetBinding(Label.TextProperty, nameof(ParzellenAblesungHistorieItem.AblesedatumText));

                var standLabel = CreateValueLabel("Zählerstand", nameof(ParzellenAblesungHistorieItem.StandText));
                var zaehlernummerLabel = CreateValueLabel("Zählernummer", nameof(ParzellenAblesungHistorieItem.ZaehlernummerText));
                zaehlernummerLabel.SetBinding(IsVisibleProperty, nameof(ParzellenAblesungHistorieItem.HasZaehlernummer));

                var eichdatumLabel = CreateValueLabel("Eichdatum", nameof(ParzellenAblesungHistorieItem.EichdatumText));
                eichdatumLabel.SetBinding(IsVisibleProperty, nameof(ParzellenAblesungHistorieItem.HasEichdatum));

                var fotoButton = new Button { Text = "Foto öffnen" };
                fotoButton.SetBinding(IsVisibleProperty, nameof(ParzellenAblesungHistorieItem.HasFoto));
                fotoButton.SetBinding(IsEnabledProperty, nameof(ParzellenAblesungHistorieItem.CanOpenFoto));
                fotoButton.Clicked += async (_, _) =>
                {
                    if (fotoButton.BindingContext is ParzellenAblesungHistorieItem item)
                        await OpenFotoAsync(item);
                };

                return new Border
                {
                    Stroke = Colors.LightGray,
                    Padding = 12,
                    Margin = new Microsoft.Maui.Thickness(0, 0, 0, 8),
                    Content = new VerticalStackLayout
                    {
                        Spacing = 8,
                        Children =
                        {
                            datumLabel,
                            standLabel,
                            zaehlernummerLabel,
                            eichdatumLabel,
                            fotoButton
                        }
                    }
                };
            })
        };
        list.SetBinding(ItemsView.ItemsSourceProperty, nameof(Items));

        var statusLabel = new Label { TextColor = Colors.DarkSlateBlue, LineBreakMode = Microsoft.Maui.LineBreakMode.WordWrap };
        statusLabel.SetBinding(Label.TextProperty, nameof(StatusMessage));
        statusLabel.SetBinding(IsVisibleProperty, nameof(HasStatusMessage));

        _submissionHintLabel = new Label
        {
            TextColor = Colors.Gray,
            LineBreakMode = Microsoft.Maui.LineBreakMode.WordWrap,
            IsVisible = false
        };

        _submitReadingButton = new Button
        {
            Text = "Ablesung einreichen",
            IsVisible = false
        };
        _submitReadingButton.Clicked += async (_, _) => await OpenSubmissionAsync();

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = 24,
                Spacing = 12,
                Children =
                {
                    titleLabel,
                    parzelleLabel,
                    hintLabel,
                    loadingIndicator,
                    list,
                    statusLabel,
                    _submissionHintLabel,
                    _submitReadingButton
                }
            }
        };
    }

    public ObservableCollection<ParzellenAblesungHistorieItem> Items { get; } = new();

    public string PageTitle { get; private set; } = "Ablesungen";
    public string ParzelleDisplayText { get; private set; } = "";
    public string HintText { get; private set; } = "Lesende Historie der ausgewählten Parzelle.";
    public string EmptyText { get; private set; } = "Keine Ablesungen vorhanden.";
    public string StatusMessage { get; private set; } = string.Empty;
    public bool HasStatusMessage => !string.IsNullOrWhiteSpace(StatusMessage);
    public bool IsLoading { get; private set; }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (_pendingLoad)
        {
            _pendingLoad = false;
            await LoadAsync();
        }
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        _parzelleId = TryReadPositiveInt(query, "parzelleId");
        _medium = NormalizeMedium(ReadQueryValue(query, "medium"));
        UpdateStaticTexts();
        _pendingLoad = true;
    }

    private async Task LoadAsync()
    {
        if (_parzelleId is not > 0)
        {
            SetStatus("Parzellenkontext konnte nicht geladen werden.");
            return;
        }

        try
        {
            IsLoading = true;
            OnPropertyChanged(nameof(IsLoading));
            SetStatus(string.Empty);
            Items.Clear();

            _parzellenContextState.SetSelectedParzelle(_parzelleId);

            var detail = await _supabaseService.GetParzelleDetailAsync(_parzelleId.Value);
            ParzelleDisplayText = detail?.DisplayName ?? $"Parzelle {_parzelleId.Value}";
            OnPropertyChanged(nameof(ParzelleDisplayText));

            if (detail == null)
            {
                SetStatus("Parzelle konnte nicht geladen werden.");
                EmptyText = BuildEmptyText();
                OnPropertyChanged(nameof(EmptyText));
                UpdateSubmissionUi(false, string.Empty);
                return;
            }

            var hasMedium = IsWasserMedium
                ? detail.HatWasser
                : detail.HatStrom;
            if (!hasMedium)
            {
                EmptyText = IsWasserMedium
                    ? "Für diese Parzelle ist kein Wasseranschluss hinterlegt."
                    : "Für diese Parzelle ist kein Stromanschluss hinterlegt.";
                OnPropertyChanged(nameof(EmptyText));
                UpdateSubmissionUi(false, string.Empty);
                return;
            }

            var ablesungen = IsWasserMedium
                ? await _supabaseService.GetWasserAblesungenAsync(_parzelleId.Value)
                : await _supabaseService.GetStromAblesungenAsync(_parzelleId.Value);

            foreach (var item in ablesungen
                         .OrderByDescending(x => x.Ablesedatum)
                         .ThenByDescending(x => x.AblesungId)
                         .Select(x => ParzellenAblesungHistorieItem.FromDto(x)))
            {
                Items.Add(item);
            }

            EmptyText = BuildEmptyText();
            OnPropertyChanged(nameof(EmptyText));

            await UpdateSubmissionUiAsync();
        }
        catch (Exception ex)
        {
            SetStatus($"Historie konnte nicht geladen werden: {ex.Message}");
            UpdateSubmissionUi(false, string.Empty);
        }
        finally
        {
            IsLoading = false;
            OnPropertyChanged(nameof(IsLoading));
        }
    }

    private async Task OpenFotoAsync(ParzellenAblesungHistorieItem item)
    {
        if (!item.HasFoto)
            return;

        try
        {
            var openUrl = await _supabaseService.ResolveAblesungFotoOpenUrlAsync(item.FotoPfad, item.FotoDriveFileId);
            if (string.IsNullOrWhiteSpace(openUrl))
            {
                SetStatus("Foto konnte nicht geöffnet werden. Für den gespeicherten Fotopfad konnte kein öffnungsfähiger Link erzeugt werden.");
                return;
            }

            await Launcher.Default.OpenAsync(openUrl);
        }
        catch
        {
            SetStatus("Foto konnte nicht geöffnet werden.");
        }
    }

    private void UpdateStaticTexts()
    {
        PageTitle = IsWasserMedium ? "Wasser-Historie" : "Strom-Historie";
        HintText = IsWasserMedium
            ? "Lesende Wasser-Ablesungen der aktuell ausgewählten Parzelle."
            : "Lesende Strom-Ablesungen der aktuell ausgewählten Parzelle.";
        EmptyText = BuildEmptyText();
        Title = PageTitle;
        OnPropertyChanged(nameof(PageTitle));
        OnPropertyChanged(nameof(HintText));
        OnPropertyChanged(nameof(EmptyText));
    }

    private void SetStatus(string message)
    {
        StatusMessage = message;
        OnPropertyChanged(nameof(StatusMessage));
        OnPropertyChanged(nameof(HasStatusMessage));
    }

    private string BuildEmptyText()
        => IsWasserMedium
            ? "Keine Wasser-Ablesungen für diese Parzelle vorhanden."
            : "Keine Strom-Ablesungen für diese Parzelle vorhanden.";

    private bool IsWasserMedium => string.Equals(_medium, "wasser", StringComparison.OrdinalIgnoreCase);

    private static string? ReadQueryValue(IDictionary<string, object> query, string key)
    {
        if (!query.TryGetValue(key, out var raw) || raw == null)
            return null;

        return Uri.UnescapeDataString(raw.ToString() ?? string.Empty);
    }

    private static int? TryReadPositiveInt(IDictionary<string, object> query, string key)
    {
        if (!query.TryGetValue(key, out var raw) || raw == null)
            return null;

        if (raw is int intValue && intValue > 0)
            return intValue;

        if (int.TryParse(Uri.UnescapeDataString(raw.ToString() ?? string.Empty), out var parsed) && parsed > 0)
            return parsed;

        return null;
    }

    private static string NormalizeMedium(string? medium)
        => string.Equals(medium, "wasser", StringComparison.OrdinalIgnoreCase)
            ? "wasser"
            : "strom";

    private async Task UpdateSubmissionUiAsync()
    {
        if (!PermissionChecks.CanSubmitOwnMeterReadings(_userContextState.CurrentUserContext))
        {
            UpdateSubmissionUi(false, string.Empty);
            return;
        }

        if (!_parzellenContextState.IsFromMemberContext
            || _parzellenContextState.ContextMitgliedId is not > 0
            || _userContextState.CurrentMitgliedId is not > 0
            || _parzellenContextState.ContextMitgliedId != (int)_userContextState.CurrentMitgliedId.Value)
        {
            UpdateSubmissionUi(false, string.Empty);
            return;
        }

        var allowSubmissions = false;
        try
        {
            allowSubmissions = await _supabaseService.GetAllowUserMeterReadingSubmissionsAsync();
        }
        catch
        {
            allowSubmissions = false;
        }

        UpdateSubmissionUi(
            allowSubmissions,
            allowSubmissions
                ? "Eigene Zählerablesungen werden hier als Einreichung gespeichert und später geprüft."
                : "Eigene Zählerablesungen sind aktuell nicht freigeschaltet.");
    }

    private void UpdateSubmissionUi(bool canSubmit, string hint)
    {
        _submitReadingButton.IsVisible = canSubmit;
        _submissionHintLabel.IsVisible = !string.IsNullOrWhiteSpace(hint);
        _submissionHintLabel.Text = hint;
    }

    private async Task OpenSubmissionAsync()
    {
        if (_parzelleId is not > 0)
            return;

        await Shell.Current.GoToAsync($"{nameof(AblesungErfassenPage)}?parzelleId={_parzelleId.Value}&medium={Uri.EscapeDataString(_medium)}");
    }

    private static View CreateValueLabel(string title, string path)
    {
        var valueLabel = new Label { LineBreakMode = Microsoft.Maui.LineBreakMode.WordWrap };
        valueLabel.SetBinding(Label.TextProperty, path);

        return new VerticalStackLayout
        {
            Spacing = 2,
            Children =
            {
                new Label
                {
                    Text = title,
                    FontAttributes = FontAttributes.Bold,
                    FontSize = 12,
                    TextColor = Colors.Gray
                },
                valueLabel
            }
        };
    }

    public sealed class ParzellenAblesungHistorieItem
    {
        public long AblesungId { get; private set; }
        public DateTime Ablesedatum { get; private set; }
        public string AblesedatumText { get; private set; } = string.Empty;
        public string StandText { get; private set; } = string.Empty;
        public string ZaehlernummerText { get; private set; } = string.Empty;
        public bool HasZaehlernummer { get; private set; }
        public string EichdatumText { get; private set; } = string.Empty;
        public bool HasEichdatum { get; private set; }
        public string? FotoPfad { get; private set; }
        public string? FotoDriveFileId { get; private set; }
        public bool HasFoto { get; private set; }
        public bool CanOpenFoto { get; private set; }

        public static ParzellenAblesungHistorieItem FromDto(ZaehlerAblesungDTO dto)
        {
            var fotoPfad = string.IsNullOrWhiteSpace(dto.FotoPfad) ? null : dto.FotoPfad.Trim();
            var fotoDriveFileId = string.IsNullOrWhiteSpace(dto.FotoDriveFileId) ? null : dto.FotoDriveFileId.Trim();

            return new ParzellenAblesungHistorieItem
            {
                AblesungId = dto.AblesungId,
                Ablesedatum = dto.Ablesedatum,
                AblesedatumText = dto.Ablesedatum == default ? "Datum unbekannt" : dto.Ablesedatum.ToString("dd.MM.yyyy"),
                StandText = dto.Stand.ToString("0.##"),
                ZaehlernummerText = string.IsNullOrWhiteSpace(dto.Zaehlernummer) ? string.Empty : dto.Zaehlernummer.Trim(),
                HasZaehlernummer = !string.IsNullOrWhiteSpace(dto.Zaehlernummer),
                EichdatumText = dto.Eichdatum == default ? string.Empty : dto.Eichdatum.Year.ToString(CultureInfo.InvariantCulture),
                HasEichdatum = dto.Eichdatum != default,
                FotoPfad = fotoPfad,
                FotoDriveFileId = fotoDriveFileId,
                HasFoto = !string.IsNullOrWhiteSpace(fotoPfad) || !string.IsNullOrWhiteSpace(fotoDriveFileId),
                CanOpenFoto = CanResolveFotoReference(fotoPfad, fotoDriveFileId)
            };
        }

        private static bool CanResolveFotoReference(string? fotoPfad, string? fotoDriveFileId)
        {
            if (!string.IsNullOrWhiteSpace(fotoDriveFileId))
                return true;

            if (string.IsNullOrWhiteSpace(fotoPfad))
                return false;

            if (Uri.TryCreate(fotoPfad, UriKind.Absolute, out var uri))
                return string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase);

            return fotoPfad.Contains(':');
        }
    }
}
