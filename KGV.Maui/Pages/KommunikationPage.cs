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
    private readonly ILeaseContractCompletionClient _leaseCompletionClient;
    private readonly IPreparedContractSignatureService _preparedSignatures;
    private readonly UserContextState _userContext;
    private readonly ObservableCollection<SignatureListItem> _items = [];
    private readonly CollectionView _list;
    private readonly Label _emptyLabel;
    private readonly Label _statusLabel;
    private readonly RefreshView _refreshView;
    private readonly Button? _historyRangeButton;
    private SignatureFilter _filter = SignatureFilter.Mine;
    private bool _showAllHistory;
    private bool _isLoading;

    public CommunicationArea FixedArea { get; }

    public KommunikationPage(
        CommunicationArea fixedArea,
        IPreparedContractRepository repository,
        IContractGenerationClient contractClient,
        IAuthService authService,
        ISupabaseService supabaseService,
        ILeaseContractCompletionClient leaseCompletionClient,
        IPreparedContractSignatureService preparedSignatures,
        UserContextState userContext)
    {
        if (!Enum.IsDefined(fixedArea))
            throw new ArgumentOutOfRangeException(nameof(fixedArea), fixedArea, "Unknown communication area.");

        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _contractClient = contractClient ?? throw new ArgumentNullException(nameof(contractClient));
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _supabaseService = supabaseService ?? throw new ArgumentNullException(nameof(supabaseService));
        _leaseCompletionClient = leaseCompletionClient ?? throw new ArgumentNullException(nameof(leaseCompletionClient));
        _preparedSignatures = preparedSignatures ?? throw new ArgumentNullException(nameof(preparedSignatures));
        _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
        FixedArea = fixedArea;
        Title = AreaTitle(FixedArea);

        var mine = FilterButton("Meine", SignatureFilter.Mine);
        var unassigned = FilterButton("Unzugeordnet", SignatureFilter.Unassigned);
        var all = FilterButton("Alle", SignatureFilter.All);
        _statusLabel = new Label { TextColor = Colors.Gray, IsVisible = false, LineBreakMode = LineBreakMode.WordWrap };
        _emptyLabel = new Label { Text = "Keine Vorgänge vorhanden.", TextColor = Colors.Gray, IsVisible = false };
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
        if (FixedArea == CommunicationArea.History)
        {
            _historyRangeButton = new Button { Text = "Alle anzeigen", HorizontalOptions = LayoutOptions.Start };
            _historyRangeButton.Clicked += async (_, _) =>
            {
                _showAllHistory = !_showAllHistory;
                _historyRangeButton.Text = _showAllHistory ? "Letzte 90 Tage" : "Alle anzeigen";
                await LoadAsync();
            };
        }

        Content = new VerticalStackLayout
        {
            Padding = 20,
            Spacing = 12,
            Children =
            {
                new Label { Text = AreaTitle(FixedArea), FontSize = 24, FontAttributes = FontAttributes.Bold },
                new Label { Text = AreaHint(FixedArea), TextColor = Colors.Gray },
                new FlexLayout { Direction = FlexDirection.Row, Wrap = FlexWrap.Wrap, Children = { mine, unassigned, all } },
                (View?)_historyRangeButton ?? new Label { IsVisible = false },
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
                throw new InvalidOperationException("Vorgänge sind nur für Admin und Vorstand verfügbar.");
            var currentUserId = CurrentUserId();

            var contracts = await _repository.ListForAreaAsync(FixedArea, _showAllHistory);
            var members = (await _supabaseService.GetMitgliederAsync()).ToDictionary(member => member.Id);
            var parcels = (await _supabaseService.GetAllParzellenAsync()).ToDictionary(parcel => parcel.Id);
            var filtered = contracts.Where(contract => MatchesFilter(contract, currentUserId)).OrderByDescending(contract => contract.BearbeitetAm).ToList();
            _items.Clear();
            foreach (var contract in filtered)
                _items.Add(new SignatureListItem(contract, MemberName(contract.MitgliedId, members), GardenNumber(contract.ParzelleId, parcels), currentUserId, FixedArea));
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
        if (FixedArea != CommunicationArea.Signatures)
            return;
        if (!item.IsMine)
        {
            await DisplayAlertAsync("Vorgänge", item.IsUnassigned
                ? "Bitte übernehmen Sie den unzugeordneten Vorgang zuerst."
                : "Dieser Vorgang ist einem anderen Bearbeiter zugewiesen.", "OK");
            return;
        }
        if (item.Contract.Status == PreparedContractConstants.FinalReview)
        {
            var review = new PreparedFinalDocumentReviewFlow(this, _repository, _supabaseService, _leaseCompletionClient);
            await review.OpenAsync(item.Contract);
            await LoadAsync();
            return;
        }
        if (item.Contract.Dokumenttyp == PreparedContractConstants.MaintenanceContract)
        {
            await RunMaintenanceSigningAsync(item.Contract);
            await LoadAsync();
            return;
        }
        if (item.Contract.Dokumenttyp == PreparedContractConstants.LeaseContract)
        {
            await RunLeaseSigningAsync(item.Contract);
            await LoadAsync();
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
                    var review = new PreparedFinalDocumentReviewFlow(this, _repository, _supabaseService, _leaseCompletionClient);
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
        var signing = new PreparedMembershipApplicationSigningService(
            _repository,
            _contractClient,
            new MitgliedsantragVereinsunterlagenService(_supabaseService),
            persistentSignatures: _preparedSignatures);
        PreparedMembershipApplicationSignatureState state;
        try { state = await signing.StartOrResumeAsync(contract); }
        catch (Exception ex) { await DisplayAlertAsync("Mitgliedsantrag", ex.Message, "OK"); return; }

        while (true)
        {
            var choices = state.Positions.OrderBy(position => position.SortOrder).Select(MembershipPositionText).ToArray();
            var finalize = state.Progress.AllRequiredSigned ? "Mitgliedsantrag finalisieren" : null;
            var actions = finalize is null ? choices : choices.Append(finalize).ToArray();
            var action = await DisplayActionSheetAsync($"Mitgliedsantrag\n{state.Progress.RequiredSignedCount} von {state.Progress.RequiredCount} Pflichtunterschriften", "Schließen", null, actions);
            if (string.IsNullOrWhiteSpace(action) || action == "Schließen") return;
            if (action == finalize)
            {
                var result = await signing.FinalizeAsync(state.Contract);
                if (result.State == PreparedMembershipApplicationSigningState.FinalDocumentReadyForReview && result.Contract is not null)
                {
                    var review = new PreparedFinalDocumentReviewFlow(this, _repository, _supabaseService, _leaseCompletionClient);
                    await review.OpenAsync(result.Contract); return;
                }
                await DisplayAlertAsync("Mitgliedsantrag", result.Error?.Message ?? "Die Finalisierung ist fehlgeschlagen. Die gespeicherten Signaturen bleiben erhalten.", "OK");
                state = await signing.GetSignatureStateAsync(state.Contract); continue;
            }
            var position = state.Positions.First(position => MembershipPositionText(position) == action);
            if (position.Status == PreparedContractSignatureStatuses.Signed) { await DisplayAlertAsync("Unterschrift", "Diese Position ist bereits unterschrieben und kann nicht überschrieben werden.", "OK"); continue; }
            var signatory = await SelectClubSignatoryAsync(state, position);
            if (position.UnterzeichnerTyp == "vereinsunterzeichner" && signatory is null) continue;
            var capture = new MauiPreparedSignatureCapture(this, new DocumentInfo { Title = "Mitgliedsantrag", Dateiname = "Mitgliedsantrag.pdf" });
            var capturePosition = new MauiPreparedSignaturePosition(
                state.Contract.Id,
                state.Contract.SignaturRevision!.Value,
                position.SignaturSchluessel,
                MembershipPositionLabel(position),
                signatory);
            var captured = await capture.CaptureSingleAsync(capturePosition);
            if (captured is null) continue;
            try
            {
                var png = Convert.FromBase64String(DigitalSignatureEncoder.EncodePngDataUrl(captured).Split(',', 2)[1]);
                var representativeName = position.UnterzeichnerTyp == SignaturePersonTypes.LegalRepresentative && state.Draft.Representative?.Mode == "manual"
                    ? $"{state.Draft.Representative.Vorname} {state.Draft.Representative.Nachname}".Trim()
                    : null;
                var request = new PreparedContractSignatureSaveRequest(
                    state.Contract.Id,
                    state.Contract.SignaturRevision!.Value,
                    position.SignaturSchluessel,
                    png,
                    signatory?.MitgliedId ?? position.MitgliedId,
                    UnterzeichnerNameSnapshot: representativeName,
                    VereinsfunktionBesetzungId: signatory?.VereinsfunktionBesetzungId,
                    VertretungId: signatory?.VertretungId,
                    VertreteneBesetzungId: signatory?.VertreteneBesetzungId);
                await signing.SaveSignatureAsync(state.Contract, request);
                state = await signing.GetSignatureStateAsync(state.Contract);
            }
            catch (Exception ex) { await DisplayAlertAsync("Unterschrift", $"Die Signatur konnte nicht gespeichert werden. {ex.Message}", "OK"); }
        }
    }

    private async Task<PreparedContractAuthorizedSignatoryDto?> SelectClubSignatoryAsync(PreparedMembershipApplicationSignatureState state, PreparedContractSignatureDto position)
    {
        if (position.UnterzeichnerTyp != "vereinsunterzeichner") return null;
        var excluded = state.Positions.FirstOrDefault(other => other.SignaturSchluessel != position.SignaturSchluessel && (other.SignaturSchluessel is "vorstand_1" or "vorstand_2") && other.Status == PreparedContractSignatureStatuses.Signed)?.MitgliedId;
        var options = (await _preparedSignatures.GetAuthorizedClubSignatoriesAsync(state.Contract.Id, state.Contract.SignaturRevision!.Value)).Where(option => option.MitgliedId != excluded).ToArray();
        if (options.Length == 0) { await DisplayAlertAsync("Vereinsunterzeichner", "Es ist kein berechtigter Vereinsunterzeichner verfügbar.", "OK"); return null; }
        var labels = options.Select(ClubSignatoryText).ToArray(); var selected = await DisplayActionSheetAsync("Vereinsunterzeichner auswählen", "Abbrechen", null, labels);
        return options.FirstOrDefault(option => ClubSignatoryText(option) == selected);
    }

    private static string MembershipPositionLabel(PreparedContractSignatureDto position) => position.SignaturSchluessel switch { "member.application" => "Antragsteller – Mitgliedsantrag", "member.privacy" => "Antragsteller – Datenschutz", "gesetzliche_vertretung.antrag" => "Gesetzliche Vertretung – Mitgliedsantrag", "gesetzliche_vertretung.datenschutz" => "Gesetzliche Vertretung – Datenschutz", "nebenmitglied.antrag" => "Nebenmitglied – Mitgliedsantrag", "nebenmitglied.datenschutz" => "Nebenmitglied – Datenschutz", "vorstand_1" => "Vereinsunterzeichner 1", "vorstand_2" => "Vereinsunterzeichner 2", _ => position.SignaturSchluessel };
    private static string MembershipPositionText(PreparedContractSignatureDto position) => $"{(position.Status == PreparedContractSignatureStatuses.Signed ? "✓" : "○")} {MembershipPositionLabel(position)}{(string.IsNullOrWhiteSpace(position.UnterzeichnerNameSnapshot) ? "" : $" – {position.UnterzeichnerNameSnapshot}")}{(string.IsNullOrWhiteSpace(position.VereinsfunktionBezeichnungSnapshot) ? "" : $" · {position.VereinsfunktionBezeichnungSnapshot}")}{(position.VertretungId is null ? "" : $" · Vertretung für {position.VertreteneFunktionBezeichnungSnapshot}")}{(position.UnterzeichnetAm is null ? "" : $" · {position.UnterzeichnetAm.Value.LocalDateTime:g}")}";
    private static string ClubSignatoryText(PreparedContractAuthorizedSignatoryDto option) => option.VertretungId is null ? $"{option.Anzeigename} – {option.VereinsfunktionBezeichnung}" : $"{option.Anzeigename} – {option.VereinsfunktionBezeichnung}\nVertretung für {option.VertreteneFunktionBezeichnung}";

    private async Task RunMaintenanceSigningAsync(KommunikationsvorgangDto contract)
    {
        var signing = new PreparedMaintenanceContractSigningService(_repository, _contractClient, _preparedSignatures);
        PreparedMaintenanceContractSignatureState state;
        try
        {
            var existingNumber = contract.WartungsvertragId is > 0
                ? await _supabaseService.GetWartungsvertragFachnummerAsync(contract.WartungsvertragId.Value)
                : null;
            var preview = await _contractClient.PreviewMaintenanceContractAsync(contract.Id, contract.MitgliedId);
            var previewTitle = existingNumber is > 0 ? $"Wartungsvertrag · Vertrags-Nr. {existingNumber}" : "Wartungsvertragsvorschau";
            await Navigation.PushAsync(new PdfViewerPage(previewTitle, preview));
            state = await signing.StartOrResumeAsync(contract);
            var contractNumber = state.Contract.WartungsvertragId is > 0
                ? await _supabaseService.GetWartungsvertragFachnummerAsync(state.Contract.WartungsvertragId.Value)
                : null;
            if (contractNumber is not > 0)
                throw new InvalidOperationException("Für den signaturbereiten Wartungsvertrag fehlt die reservierte 90xxx.");
            if (contract.Status == PreparedContractConstants.Prepared)
            {
                var numberedPreview = await _contractClient.PreviewMaintenanceContractAsync(state.Contract.Id, state.Contract.MitgliedId);
                await Navigation.PushAsync(new PdfViewerPage($"Wartungsvertrag · Vertrags-Nr. {contractNumber}", numberedPreview));
            }
        }
        catch (Exception ex) { await DisplayAlertAsync("Wartungsvertrag", ex.Message, "OK"); return; }
        while (true)
        {
            var choices = state.Positions.OrderBy(position => position.SortOrder).Select(MaintenancePositionText).ToArray();
            var finalize = state.Progress.AllRequiredSigned ? "Wartungsvertrag finalisieren" : null;
            var action = await DisplayActionSheetAsync($"Wartungsvertrag\n{state.Progress.RequiredSignedCount} von {state.Progress.RequiredCount} Pflichtunterschriften", "Schließen", null, finalize is null ? choices : choices.Append(finalize).ToArray());
            if (string.IsNullOrWhiteSpace(action) || action == "Schließen") return;
            if (action == finalize)
            {
                var result = await signing.FinalizeAsync(state.Contract);
                if (result.Completed) { await DisplayAlertAsync("Wartungsvertrag", result.FinalizeResult?.Message ?? "Wartungsvertrag abgeschlossen.", "OK"); return; }
                await DisplayAlertAsync("Wartungsvertrag", result.Error?.Message ?? "Finalisierung fehlgeschlagen. Die Signaturen bleiben erhalten.", "OK"); state = await signing.GetSignatureStateAsync(state.Contract); continue;
            }
            var position = state.Positions.First(item => MaintenancePositionText(item) == action);
            if (position.Status == PreparedContractSignatureStatuses.Signed) continue;
            PreparedContractAuthorizedSignatoryDto? signatory = null;
            if (position.UnterzeichnerTyp == "vereinsunterzeichner") signatory = await SelectMaintenanceClubSignatoryAsync(state, position);
            if (position.UnterzeichnerTyp == "vereinsunterzeichner" && signatory is null) continue;
            var capture = new MauiPreparedSignatureCapture(this, new DocumentInfo { Title = "Wartungsvertrag", Dateiname = "Wartungsvertrag.pdf" });
            var captured = await capture.CaptureSingleAsync(new MauiPreparedSignaturePosition(state.Contract.Id, state.Contract.SignaturRevision!.Value, position.SignaturSchluessel, MaintenancePositionLabel(position), signatory));
            if (captured is null) continue;
            try { var png = Convert.FromBase64String(DigitalSignatureEncoder.EncodePngDataUrl(captured).Split(',', 2)[1]); await signing.SaveSignatureAsync(state.Contract, new PreparedContractSignatureSaveRequest(state.Contract.Id, state.Contract.SignaturRevision!.Value, position.SignaturSchluessel, png, signatory?.MitgliedId ?? position.MitgliedId, VereinsfunktionBesetzungId: signatory?.VereinsfunktionBesetzungId, VertretungId: signatory?.VertretungId, VertreteneBesetzungId: signatory?.VertreteneBesetzungId)); state = await signing.GetSignatureStateAsync(state.Contract); }
            catch (Exception ex) { await DisplayAlertAsync("Unterschrift", $"Die Signatur konnte nicht gespeichert werden. {ex.Message}", "OK"); }
        }
    }
    private async Task<PreparedContractAuthorizedSignatoryDto?> SelectMaintenanceClubSignatoryAsync(PreparedMaintenanceContractSignatureState state, PreparedContractSignatureDto position)
    { var excluded = state.Positions.FirstOrDefault(other => other.SignaturSchluessel != position.SignaturSchluessel && (other.SignaturSchluessel is "vorstand_1" or "vorstand_2") && other.Status == PreparedContractSignatureStatuses.Signed)?.MitgliedId; var options = (await _preparedSignatures.GetAuthorizedClubSignatoriesAsync(state.Contract.Id, state.Contract.SignaturRevision!.Value)).Where(option => option.MitgliedId != excluded).ToArray(); if (options.Length == 0) { await DisplayAlertAsync("Vereinsunterzeichner", "Es ist kein berechtigter Vereinsunterzeichner verfügbar.", "OK"); return null; } var selected = await DisplayActionSheetAsync("Vereinsunterzeichner auswählen", "Abbrechen", null, options.Select(ClubSignatoryText).ToArray()); return options.FirstOrDefault(option => ClubSignatoryText(option) == selected); }
    private static string MaintenancePositionLabel(PreparedContractSignatureDto position) => position.SignaturSchluessel switch { "mitglied" => "Mitglied", "vorstand_1" => "Vereinsunterzeichner 1", "vorstand_2" => "Vereinsunterzeichner 2", _ => position.SignaturSchluessel };
    private static string MaintenancePositionText(PreparedContractSignatureDto position) => $"{(position.Status == PreparedContractSignatureStatuses.Signed ? "✓" : "○")} {MaintenancePositionLabel(position)}{(string.IsNullOrWhiteSpace(position.UnterzeichnerNameSnapshot) ? "" : $" – {position.UnterzeichnerNameSnapshot}")}{(string.IsNullOrWhiteSpace(position.VereinsfunktionBezeichnungSnapshot) ? "" : $" · {position.VereinsfunktionBezeichnungSnapshot}")}{(position.VertretungId is null ? "" : $" · Vertretung für {position.VertreteneFunktionBezeichnungSnapshot}")}{(position.UnterzeichnetAm is null ? "" : $" · {position.UnterzeichnetAm.Value.LocalDateTime:g}")}";

    private async Task RunLeaseSigningAsync(KommunikationsvorgangDto contract)
    {
        var signing = new PreparedLeaseContractSigningService(_repository, _contractClient, _supabaseService, new PreparedLeaseContractVereinsunterlagenService(_repository, new MitgliedsantragVereinsunterlagenService(_supabaseService)), persistentSignatures: _preparedSignatures);
        PreparedLeaseContractSignatureState state;
        try { state = await signing.StartOrResumeAsync(contract); }
        catch (Exception ex) { await DisplayAlertAsync("Pachtvertrag", ex.Message, "OK"); return; }

        while (true)
        {
            var choices = state.Positions.OrderBy(position => position.SortOrder).Select(LeasePositionText).ToArray();
            var finalize = state.Progress.AllRequiredSigned ? "Pachtvertrag finalisieren" : null;
            var action = await DisplayActionSheetAsync($"Pachtvertrag\n{state.Progress.RequiredSignedCount} von {state.Progress.RequiredCount} Pflichtunterschriften", "Schließen", null, finalize is null ? choices : choices.Append(finalize).ToArray());
            if (string.IsNullOrWhiteSpace(action) || action == "Schließen") return;
            if (action == finalize)
            {
                var result = await signing.FinalizeAsync(state.Contract);
                if (result.ReadyForReview)
                {
                    var review = new PreparedFinalDocumentReviewFlow(this, _repository, _supabaseService, _leaseCompletionClient);
                    await review.OpenAsync(result.Contract); return;
                }
                await DisplayAlertAsync("Pachtvertrag", result.Error?.Message ?? "Die Finalisierung ist fehlgeschlagen. Die gespeicherten Signaturen bleiben erhalten.", "OK");
                state = await signing.GetSignatureStateAsync(state.Contract); continue;
            }
            var position = state.Positions.First(item => LeasePositionText(item) == action);
            if (position.Status == PreparedContractSignatureStatuses.Signed) { await DisplayAlertAsync("Unterschrift", "Diese Position ist bereits unterschrieben und kann nicht überschrieben werden.", "OK"); continue; }
            var signatory = await SelectLeaseClubSignatoryAsync(state, position);
            if (position.UnterzeichnerTyp == "vereinsunterzeichner" && signatory is null) continue;
            var capture = new MauiPreparedSignatureCapture(this, new DocumentInfo { Title = "Pachtvertrag", Dateiname = "Pachtvertrag.pdf" });
            var captured = await capture.CaptureSingleAsync(new MauiPreparedSignaturePosition(state.Contract.Id, state.Contract.SignaturRevision!.Value, position.SignaturSchluessel, LeasePositionLabel(position), signatory));
            if (captured is null) continue;
            try
            {
                var png = Convert.FromBase64String(DigitalSignatureEncoder.EncodePngDataUrl(captured).Split(',', 2)[1]);
                await signing.SaveSignatureAsync(state.Contract, new PreparedContractSignatureSaveRequest(state.Contract.Id, state.Contract.SignaturRevision!.Value, position.SignaturSchluessel, png, signatory?.MitgliedId ?? position.MitgliedId, VereinsfunktionBesetzungId: signatory?.VereinsfunktionBesetzungId, VertretungId: signatory?.VertretungId, VertreteneBesetzungId: signatory?.VertreteneBesetzungId));
                state = await signing.GetSignatureStateAsync(state.Contract);
            }
            catch (Exception ex) { await DisplayAlertAsync("Unterschrift", $"Die Signatur konnte nicht gespeichert werden. {ex.Message}", "OK"); }
        }
    }

    private async Task<PreparedContractAuthorizedSignatoryDto?> SelectLeaseClubSignatoryAsync(PreparedLeaseContractSignatureState state, PreparedContractSignatureDto position)
    {
        if (position.UnterzeichnerTyp != "vereinsunterzeichner") return null;
        var excluded = state.Positions.FirstOrDefault(other => other.SignaturSchluessel != position.SignaturSchluessel && (other.SignaturSchluessel is "vorstand_1" or "vorstand_2") && other.Status == PreparedContractSignatureStatuses.Signed)?.MitgliedId;
        var options = (await _preparedSignatures.GetAuthorizedClubSignatoriesAsync(state.Contract.Id, state.Contract.SignaturRevision!.Value)).Where(option => option.MitgliedId != excluded).ToArray();
        if (options.Length == 0) { await DisplayAlertAsync("Vereinsunterzeichner", "Es ist kein berechtigter Vereinsunterzeichner verfügbar.", "OK"); return null; }
        var labels = options.Select(ClubSignatoryText).ToArray(); var selected = await DisplayActionSheetAsync("Vereinsunterzeichner auswählen", "Abbrechen", null, labels);
        return options.FirstOrDefault(option => ClubSignatoryText(option) == selected);
    }

    private static string LeasePositionLabel(PreparedContractSignatureDto position) => position.SignaturSchluessel switch { "lease.member" => "Pächter/in 1", "lease.second_party" => "Pächter/in 2", "vorstand_1" => "Vereinsunterzeichner 1", "vorstand_2" => "Vereinsunterzeichner 2", _ => position.SignaturSchluessel };
    private static string LeasePositionText(PreparedContractSignatureDto position) => $"{(position.Status == PreparedContractSignatureStatuses.Signed ? "✓" : "○")} {LeasePositionLabel(position)}{(string.IsNullOrWhiteSpace(position.UnterzeichnerNameSnapshot) ? "" : $" – {position.UnterzeichnerNameSnapshot}")}{(string.IsNullOrWhiteSpace(position.VereinsfunktionBezeichnungSnapshot) ? "" : $" · {position.VereinsfunktionBezeichnungSnapshot}")}{(position.VertretungId is null ? "" : $" · Vertretung für {position.VertreteneFunktionBezeichnungSnapshot}")}{(position.UnterzeichnetAm is null ? "" : $" · {position.UnterzeichnetAm.Value.LocalDateTime:g}")}";

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

    private async Task CompletePrintAsync(SignatureListItem item)
    {
        if (FixedArea != CommunicationArea.Print || !item.CanCompletePrint)
            return;
        try { await _repository.CompletePrintAsync(item.Contract.Id); await LoadAsync(); }
        catch (Exception ex) { await DisplayAlertAsync("Druckausgabe", ex.Message, "OK"); await LoadAsync(); }
    }

    private async Task DiscardAsync(SignatureListItem item)
    {
        if (!item.IsMine || item.Contract.Status is not (PreparedContractConstants.Prepared or PreparedContractConstants.ReadyForSignature or PreparedContractConstants.FinalReview)) return;
        var confirmed = await DisplayAlertAsync("Vorgang verwerfen", "Vorgang wirklich verwerfen? Alle bisherigen Eingaben und Unterschriften dieses Vorgangs gehen verloren. Bereits versendete Vereinsunterlagen bleiben erhalten.", "Verwerfen", "Abbrechen");
        if (!confirmed) return;
        try { await _repository.DiscardAsync(item.Contract.Id, item.Contract.Revision); await LoadAsync(); }
        catch (Exception ex) { await DisplayAlertAsync("Vorgang verwerfen", ex.Message, "OK"); await LoadAsync(); }
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
        action.SetBinding(IsVisibleProperty, nameof(SignatureListItem.CanOpen));
        action.Clicked += async (sender, _) => { if (sender is Button { BindingContext: SignatureListItem item }) await OpenAsync(item); };
        var completePrint = new Button { Text = "Druck erledigt", HorizontalOptions = LayoutOptions.Start };
        completePrint.SetBinding(IsVisibleProperty, nameof(SignatureListItem.CanCompletePrint));
        completePrint.Clicked += async (sender, _) => { if (sender is Button { BindingContext: SignatureListItem item }) await CompletePrintAsync(item); };
        var discard = new Button { Text = "Verwerfen", HorizontalOptions = LayoutOptions.Start, TextColor = Colors.DarkRed };
        discard.SetBinding(IsVisibleProperty, nameof(SignatureListItem.CanDiscard));
        discard.Clicked += async (sender, _) => { if (sender is Button { BindingContext: SignatureListItem item }) await DiscardAsync(item); };
        return new Border { Stroke = Colors.LightGray, Padding = 12, Content = new VerticalStackLayout { Spacing = 4, Children = { title, details, status, assignment, claim, action, completePrint, discard } } };
    }

    private bool MatchesFilter(KommunikationsvorgangDto contract, Guid currentUserId) => _filter switch
    {
        SignatureFilter.Mine => contract.ZugewiesenAn == currentUserId,
        SignatureFilter.Unassigned => contract.ZugewiesenAn is null,
        _ => true
    };
    private static string MemberName(int id, IReadOnlyDictionary<int, MitgliedRecord> members)
    {
        if (!members.TryGetValue(id, out var member))
            return "Mitglied unbekannt";

        var name = $"{member.Vorname} {member.Name}".Trim();
        if (member.Mitgliedsnummer <= 0)
            return "Mitgliedsnummer nicht verfügbar";

        return string.IsNullOrWhiteSpace(name)
            ? $"Mgl.-Nr. {member.Mitgliedsnummer}"
            : $"{name} · Mgl.-Nr. {member.Mitgliedsnummer}";
    }
    private static string? GardenNumber(int? id, IReadOnlyDictionary<int, ParzelleRecord> parcels) => id is { } parcelId && parcels.TryGetValue(parcelId, out var parcel) ? parcel.GartenNr : null;
    private Guid CurrentUserId() => Guid.TryParse(_authService.CurrentUserId, out var userId) && userId != Guid.Empty
        ? userId
        : throw new InvalidOperationException("Es ist kein angemeldeter Benutzer vorhanden.");
    private static string AreaTitle(CommunicationArea area) => area switch
    {
        CommunicationArea.Signatures => "Signaturen",
        CommunicationArea.Documents => "Dokumente",
        CommunicationArea.MailOutbox => "Mailausgang",
        CommunicationArea.Sent => "Versendet",
        CommunicationArea.Print => "Druckausgabe",
        CommunicationArea.History => "Verlauf",
        _ => "Vorgänge"
    };
    private static string AreaHint(CommunicationArea area) => area switch
    {
        CommunicationArea.History => "Die letzten 90 Tage werden angezeigt.",
        CommunicationArea.Signatures => "Vorbereitete Vertrags- und Prüfvorgänge",
        _ => "Kommunikationsvorgänge"
    };
    private enum SignatureFilter { Unassigned, Mine, All }

    private sealed class SignatureListItem
    {
        public SignatureListItem(KommunikationsvorgangDto contract, string memberName, string? gardenNumber, Guid currentUserId, CommunicationArea area)
        { Contract = contract; MemberName = memberName; GardenNumber = gardenNumber; IsMine = contract.ZugewiesenAn == currentUserId; Area = area; IsUnassigned = contract.ZugewiesenAn is null && area is not CommunicationArea.Sent and not CommunicationArea.History && contract.Status is (PreparedContractConstants.Prepared or PreparedContractConstants.ReadyForSignature); }
        public KommunikationsvorgangDto Contract { get; } public string MemberName { get; } public string? GardenNumber { get; } public bool IsMine { get; } public bool IsUnassigned { get; } public CommunicationArea Area { get; }
        public string DocumentType => Contract.Dokumenttyp == PreparedContractConstants.MembershipApplication ? "Mitgliedsantrag" : Contract.Dokumenttyp == PreparedContractConstants.MaintenanceContract ? "Wartungsvertrag" : string.IsNullOrWhiteSpace(Contract.Dokumenttyp) ? "Kommunikationsvorgang" : Contract.Dokumenttyp;
        public string Details => GardenNumber is null ? MemberName : $"{MemberName} · Garten {GardenNumber}";
        public string Status => Contract.Status switch { PreparedContractConstants.Prepared => "Vorbereitet", PreparedContractConstants.ReadyForSignature => "Zur Signatur", PreparedContractConstants.FinalReview => "Letzte Prüfung", "mail_bereit" => "Versandbereit", "wird_versendet" => "Wird versendet", "druck_bereit" => "Druckbereit", PreparedContractConstants.Completed => "Erledigt", "fehler" => "Fehler", "abgebrochen" => "Abgebrochen", PreparedContractConstants.Discarded => "Verworfen", _ => Contract.Status };
        public string PrimaryAction => Contract.Status == PreparedContractConstants.FinalReview ? "Letzte Prüfung" : "Zur Signatur";
        public bool CanOpen => Area == CommunicationArea.Signatures && IsMine;
        public bool CanCompletePrint => Area == CommunicationArea.Print && IsMine && Contract.Status == "druck_bereit";
        public bool CanDiscard => IsMine && Contract.Status is PreparedContractConstants.Prepared or PreparedContractConstants.ReadyForSignature or PreparedContractConstants.FinalReview;
        public string Assignment => IsMine ? "Mir zugewiesen" : IsUnassigned ? "Unzugeordnet" : "Anderem Bearbeiter zugewiesen";
    }
}
