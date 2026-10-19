using Microsoft.Maui;
using System.Collections.ObjectModel;
using KGV.Core.Interfaces;
using KGV.Core.Models;
using KGV.Core.Security;
using KGV.Core.Services;
using KGV.Maui.State;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace KGV.Maui.Pages;

public sealed class KommunikationPage : ContentPage
{
    private readonly IPreparedContractRepository _repository;
    private readonly IContractGenerationClient _contractClient;
    private readonly IAuthService _authService;
    private readonly ISupabaseService _supabaseService;
    private readonly UserContextState _userContext;
    private readonly ObservableCollection<SignatureListItem> _items = [];
    private readonly CollectionView _list;
    private readonly Label _emptyLabel;
    private readonly Label _statusLabel;
    private readonly RefreshView _refreshView;
    private SignatureFilter _filter = SignatureFilter.Mine;
    private bool _isLoading;

    public KommunikationPage(
        IPreparedContractRepository repository,
        IContractGenerationClient contractClient,
        IAuthService authService,
        ISupabaseService supabaseService,
        UserContextState userContext)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _contractClient = contractClient ?? throw new ArgumentNullException(nameof(contractClient));
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _supabaseService = supabaseService ?? throw new ArgumentNullException(nameof(supabaseService));
        _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
        Title = "Kommunikation";

        var mine = FilterButton("Meine", SignatureFilter.Mine);
        var unassigned = FilterButton("Unzugeordnet", SignatureFilter.Unassigned);
        var all = FilterButton("Alle", SignatureFilter.All);
        _statusLabel = new Label { TextColor = Colors.Gray, IsVisible = false, LineBreakMode = LineBreakMode.WordWrap };
        _emptyLabel = new Label { Text = "Keine Kommunikationsvorgänge vorhanden.", TextColor = Colors.Gray, IsVisible = false };
        _list = new CollectionView
        {
            ItemsSource = _items,
            SelectionMode = SelectionMode.Single,
            ItemsLayout = new LinearItemsLayout(ItemsLayoutOrientation.Vertical) { ItemSpacing = 8 },
            ItemTemplate = new DataTemplate(CreateItem)
        };
        _list.SelectionChanged += async (_, args) =>
        {
            if (args.CurrentSelection.FirstOrDefault() is SignatureListItem item)
                await OpenAsync(item);
            _list.SelectedItem = null;
        };
        _refreshView = new RefreshView { Content = _list };
        _refreshView.Refreshing += async (_, _) => await LoadAsync();

        Content = new VerticalStackLayout
        {
            Padding = 20,
            Spacing = 12,
            Children =
            {
                new Label { Text = "Kommunikation", FontSize = 24, FontAttributes = FontAttributes.Bold },
                new Label { Text = "Vorbereitete Vertrags- und Prüfvorgänge", TextColor = Colors.Gray },
                new FlexLayout { Direction = FlexDirection.Row, Wrap = FlexWrap.Wrap, Children = { mine, unassigned, all } },
                _statusLabel,
                _emptyLabel,
                _refreshView
            }
        };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadAsync();
    }

    private Button FilterButton(string text, SignatureFilter filter)
    {
        var button = new Button { Text = text };
        button.Clicked += async (_, _) => { _filter = filter; await LoadAsync(); };
        return button;
    }

    private async Task LoadAsync()
    {
        if (_isLoading) return;
        _isLoading = true;
        _statusLabel.IsVisible = true;
        _statusLabel.Text = "Vorgänge werden geladen...";
        try
        {
            if (_userContext.CurrentUserContext?.Role is not UserRole.Admin and not UserRole.Vorstand)
                throw new InvalidOperationException("Kommunikation ist nur für Admin und Vorstand verfügbar.");
            var currentUserId = CurrentUserId();

            var contracts = await _repository.ListActiveAsync();
            var members = (await _supabaseService.GetMitgliederAsync()).ToDictionary(member => member.Id);
            var parcels = (await _supabaseService.GetAllParzellenAsync()).ToDictionary(parcel => parcel.Id);
            var filtered = contracts.Where(contract => MatchesFilter(contract, currentUserId)).OrderByDescending(contract => contract.BearbeitetAm).ToList();
            _items.Clear();
            foreach (var contract in filtered)
                _items.Add(new SignatureListItem(contract, MemberName(contract.MitgliedId, members), GardenNumber(contract.ParzelleId, parcels), currentUserId));
            _emptyLabel.IsVisible = _items.Count == 0;
            _statusLabel.IsVisible = false;
        }
        catch (Exception ex)
        {
            _statusLabel.Text = $"Vorgänge konnten nicht geladen werden: {ex.Message}";
        }
        finally
        {
            _isLoading = false;
            _refreshView.IsRefreshing = false;
        }
    }

    private async Task OpenAsync(SignatureListItem item)
    {
        if (!item.IsMine)
        {
            await DisplayAlertAsync("Kommunikation", item.IsUnassigned
                ? "Bitte übernehmen Sie den unzugeordneten Vorgang zuerst."
                : "Dieser Vorgang ist einem anderen Bearbeiter zugewiesen.", "OK");
            return;
        }
        if (item.Contract.Status == PreparedContractConstants.FinalReview)
        {
            var review = new PreparedFinalDocumentReviewFlow(this, _repository, _supabaseService);
            await review.OpenAsync(item.Contract);
            await LoadAsync();
            return;
        }
        if (item.Contract.Dokumenttyp == PreparedContractConstants.LeaseContract)
        {
            await DisplayAlertAsync("Pachtvertrag", "Die Signatur für Pachtverträge wird noch nicht unterstützt.", "OK");
            return;
        }

        if (item.Contract.Status == PreparedContractConstants.ReadyForSignature)
        {
            try
            {
                var recovered = await new PreparedFinalDocumentRecoveryService(_repository)
                    .RecoverAsync(item.Contract);
                if (recovered is not null)
                {
                    var review = new PreparedFinalDocumentReviewFlow(this, _repository, _supabaseService);
                    await review.OpenAsync(recovered);
                    await LoadAsync();
                    return;
                }
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync("Mitgliedsantrag", $"Der vorhandene signierte Mitgliedsantrag konnte nicht geprüft werden. Es wird keine neue Signatur gestartet. {ex.Message}", "OK");
                return;
            }
        }

        var preview = new PreparedMembershipApplicationPreviewFlow(this, _repository, _contractClient, new MitgliedsantragVereinsunterlagenService(_supabaseService));
        var result = await preview.OpenAsync(item.Contract.Id);
        if (result.Action is PreparedMembershipApplicationPreviewAction.StartSigning or PreparedMembershipApplicationPreviewAction.ContinueSigning)
            await RunMembershipSigningAsync(result.Contract);
        await LoadAsync();
    }

    private async Task RunMembershipSigningAsync(KommunikationsvorgangDto contract)
    {
        var sourceDocument = new DocumentInfo { Title = "Mitgliedsantrag", Dateiname = "Mitgliedsantrag.pdf" };
        var capture = new MauiPreparedSignatureCapture(this, sourceDocument);
        var signing = new PreparedMembershipApplicationSigningService(_repository, _contractClient, new MitgliedsantragVereinsunterlagenService(_supabaseService));
        var result = await signing.RunAsync(contract, capture);
        if (result.State == PreparedMembershipApplicationSigningState.FinalDocumentReadyForReview && result.Contract is not null)
        {
            var review = new PreparedFinalDocumentReviewFlow(this, _repository, _supabaseService);
            await review.OpenAsync(result.Contract);
            return;
        }

        if (result.State != PreparedMembershipApplicationSigningState.Cancelled)
            await DisplayAlertAsync("Mitgliedsantrag", result.Error?.Message ?? "Der Signaturvorgang konnte nicht abgeschlossen werden.", "OK");
    }

    private async Task ClaimAsync(SignatureListItem item)
    {
        if (!item.IsUnassigned) return;
        var currentUserId = CurrentUserId();
        try
        {
            var claimed = await _repository.ClaimAsync(item.Contract.Id);
            if (claimed.Id != item.Contract.Id || claimed.Dokumenttyp != item.Contract.Dokumenttyp ||
                claimed.Status is not PreparedContractConstants.Prepared and not PreparedContractConstants.ReadyForSignature ||
                claimed.ZugewiesenAn != currentUserId)
                throw new PreparedContractValidationException("Die Übernahme wurde vom Server nicht korrekt bestätigt.");
            await LoadAsync();
        }
        catch (PreparedContractConflictException)
        {
            await DisplayAlertAsync("Übernehmen", "Der Vorgang wurde zwischenzeitlich bereits übernommen.", "OK");
            await LoadAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Übernehmen", ex.Message, "OK");
            await LoadAsync();
        }
    }

    private View CreateItem()
    {
        var title = new Label { FontAttributes = FontAttributes.Bold, FontSize = 17 };
        title.SetBinding(Label.TextProperty, nameof(SignatureListItem.DocumentType));
        var details = new Label { TextColor = Colors.Gray, LineBreakMode = LineBreakMode.WordWrap };
        details.SetBinding(Label.TextProperty, nameof(SignatureListItem.Details));
        var status = new Label { FontSize = 12 };
        status.SetBinding(Label.TextProperty, nameof(SignatureListItem.Status));
        var assignment = new Label { FontSize = 12, TextColor = Colors.Gray };
        assignment.SetBinding(Label.TextProperty, nameof(SignatureListItem.Assignment));
        var claim = new Button { Text = "Übernehmen", HorizontalOptions = LayoutOptions.Start };
        claim.SetBinding(IsVisibleProperty, nameof(SignatureListItem.IsUnassigned));
        claim.Clicked += async (sender, _) => { if (sender is Button { BindingContext: SignatureListItem item }) await ClaimAsync(item); };
        var action = new Button { HorizontalOptions = LayoutOptions.Start };
        action.SetBinding(Button.TextProperty, nameof(SignatureListItem.PrimaryAction));
        action.SetBinding(IsVisibleProperty, nameof(SignatureListItem.IsMine));
        action.Clicked += async (sender, _) => { if (sender is Button { BindingContext: SignatureListItem item }) await OpenAsync(item); };
        return new Border { Stroke = Colors.LightGray, Padding = 12, Content = new VerticalStackLayout { Spacing = 4, Children = { title, details, status, assignment, claim, action } } };
    }

    private bool MatchesFilter(KommunikationsvorgangDto contract, Guid currentUserId) => _filter switch
    {
        SignatureFilter.Mine => contract.ZugewiesenAn == currentUserId,
        SignatureFilter.Unassigned => contract.ZugewiesenAn is null,
        _ => true
    };
    private static string MemberName(int id, IReadOnlyDictionary<int, MitgliedRecord> members) => members.TryGetValue(id, out var member) ? $"{member.Vorname} {member.Name}".Trim() : "Mitglied unbekannt";
    private static string? GardenNumber(int? id, IReadOnlyDictionary<int, ParzelleRecord> parcels) => id is { } parcelId && parcels.TryGetValue(parcelId, out var parcel) ? parcel.GartenNr : null;
    private Guid CurrentUserId() => Guid.TryParse(_authService.CurrentUserId, out var userId) && userId != Guid.Empty
        ? userId
        : throw new InvalidOperationException("Es ist kein angemeldeter Benutzer vorhanden.");
    private enum SignatureFilter { Unassigned, Mine, All }

    private sealed class SignatureListItem
    {
        public SignatureListItem(KommunikationsvorgangDto contract, string memberName, string? gardenNumber, Guid currentUserId)
        { Contract = contract; MemberName = memberName; GardenNumber = gardenNumber; IsMine = contract.ZugewiesenAn == currentUserId; IsUnassigned = contract.ZugewiesenAn is null && contract.Status is PreparedContractConstants.Prepared or PreparedContractConstants.ReadyForSignature; }
        public KommunikationsvorgangDto Contract { get; } public string MemberName { get; } public string? GardenNumber { get; } public bool IsMine { get; } public bool IsUnassigned { get; }
        public string DocumentType => Contract.Dokumenttyp == PreparedContractConstants.MembershipApplication ? "Mitgliedsantrag" : "Pachtvertrag";
        public string Details => GardenNumber is null ? MemberName : $"{MemberName} · Garten {GardenNumber}";
        public string Status => Contract.Status switch { PreparedContractConstants.Prepared => "Vorbereitet", PreparedContractConstants.ReadyForSignature => "Zur Signatur", PreparedContractConstants.FinalReview => "Letzte Prüfung", _ => Contract.Status };
        public string PrimaryAction => Contract.Status == PreparedContractConstants.FinalReview ? "Letzte Prüfung" : "Zur Signatur";
        public string Assignment => IsMine ? "Mir zugewiesen" : IsUnassigned ? "Unzugeordnet" : "Anderem Bearbeiter zugewiesen";
    }
}

