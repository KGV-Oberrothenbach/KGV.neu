using Microsoft.Maui;
using KGV.Core.Interfaces;
using KGV.Core.Models;
using KGV.Core.Services;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace KGV.Maui.Pages;

public sealed class PreparedFinalDocumentReviewFlow
{
    private readonly Page _hostPage;
    private readonly IPreparedContractRepository _repository;
    private readonly IFinalDocumentLoader _documentLoader;
    private readonly ILeaseContractCompletionClient _leaseCompletionClient;

    public PreparedFinalDocumentReviewFlow(
        Page hostPage,
        IPreparedContractRepository repository,
        IFinalDocumentLoader documentLoader, ILeaseContractCompletionClient leaseCompletionClient)
    {
        _hostPage = hostPage ?? throw new ArgumentNullException(nameof(hostPage));
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _documentLoader = documentLoader ?? throw new ArgumentNullException(nameof(documentLoader));
        _leaseCompletionClient = leaseCompletionClient ?? throw new ArgumentNullException(nameof(leaseCompletionClient));
    }

    public async Task OpenAsync(KommunikationsvorgangDto contract, CancellationToken cancellationToken = default)
    {
        var page = new PreparedFinalDocumentReviewPage(
            new PreparedFinalDocumentReviewService(_repository, _documentLoader, _leaseCompletionClient),
            contract,
            cancellationToken);
        await _hostPage.Navigation.PushModalAsync(new NavigationPage(page));
        await page.WaitForCompletionAsync();
    }
}

internal sealed class PreparedFinalDocumentReviewPage : ContentPage
{
    private readonly PreparedFinalDocumentReviewService _reviewService;
    private readonly KommunikationsvorgangDto _contract;
    private readonly CancellationToken _cancellationToken;
    private readonly TaskCompletionSource _completion = new();
    private readonly Label _statusLabel;
    private readonly Button _reloadButton;
    private readonly Button _completeButton;
    private bool _opened;
    private bool _closed;

    public PreparedFinalDocumentReviewPage(
        PreparedFinalDocumentReviewService reviewService,
        KommunikationsvorgangDto contract,
        CancellationToken cancellationToken)
    {
        _reviewService = reviewService ?? throw new ArgumentNullException(nameof(reviewService));
        _contract = contract ?? throw new ArgumentNullException(nameof(contract));
        _cancellationToken = cancellationToken;
        Title = "Letzte Prüfung";

        _statusLabel = new Label { TextColor = Colors.Gray, LineBreakMode = LineBreakMode.WordWrap };
        _reloadButton = new Button { Text = "Erneut laden" };
        _completeButton = new Button { Text = "Abschließen", IsEnabled = false };
        _reloadButton.Clicked += async (_, _) => await LoadAndShowAsync(reload: true);
        _completeButton.Clicked += async (_, _) => await CompleteAsync();

        Content = new VerticalStackLayout
        {
            Padding = 24,
            Spacing = 14,
            Children =
            {
                new Label { Text = "Letzte Prüfung", FontSize = 24, FontAttributes = FontAttributes.Bold },
                new Label { Text = "Das bereits erzeugte Finaldokument wird direkt vom Server geladen. Erst danach kann der Vorgang abgeschlossen werden.", TextColor = Colors.Gray, LineBreakMode = LineBreakMode.WordWrap },
                _statusLabel,
                new FlexLayout { Direction = FlexDirection.Row, Wrap = FlexWrap.Wrap, Children = { _reloadButton, _completeButton } }
            }
        };
    }

    public Task WaitForCompletionAsync() => _completion.Task;

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_opened)
            return;
        _opened = true;
        await LoadAndShowAsync(reload: false);
    }

    protected override bool OnBackButtonPressed()
    {
        _completion.TrySetResult();
        return base.OnBackButtonPressed();
    }

    private async Task LoadAndShowAsync(bool reload)
    {
        _reloadButton.IsEnabled = false;
        _completeButton.IsEnabled = false;
        _statusLabel.Text = "Finaldokument wird geladen...";
        var result = reload
            ? await _reviewService.ReloadAsync(_cancellationToken)
            : await _reviewService.OpenAsync(_contract, _cancellationToken);

        if (result.State != PreparedFinalDocumentReviewState.FinalDocumentLoaded || result.Document is null)
        {
            _statusLabel.Text = $"Finaldokument konnte nicht geladen werden: {result.Error?.Message}";
            _reloadButton.IsEnabled = true;
            return;
        }

        _statusLabel.Text = $"Finaldokument #{result.Contract.FinalDokumentId} wurde geladen.";
        _reloadButton.IsEnabled = true;
        _completeButton.IsEnabled = _reviewService.CanComplete;
        await Navigation.PushAsync(new PdfViewerPage(result.Document.Document.Title, result.Document.Content));
    }

    private async Task CompleteAsync()
    {
        if (!_reviewService.CanComplete)
            return;

        var confirm = await DisplayAlertAsync("Vorgang abschließen", "Das bereits geprüfte Finaldokument bleibt unverändert bestehen. Den Vorgang jetzt abschließen?", "Abschließen", "Abbrechen");
        if (!confirm)
            return;

        _reloadButton.IsEnabled = false;
        _completeButton.IsEnabled = false;
        _statusLabel.Text = "Vorgang wird abgeschlossen...";
        var result = await _reviewService.CompleteAsync(_cancellationToken);
        if (result.State == PreparedFinalDocumentReviewState.Completed)
        {
            _closed = true;
            _completion.TrySetResult();
            await Navigation.PopModalAsync();
            return;
        }

        _statusLabel.Text = $"Vorgang konnte nicht abgeschlossen werden: {result.Error?.Message}";
        _reloadButton.IsEnabled = true;
        _completeButton.IsEnabled = _reviewService.CanComplete;
    }

    protected override void OnDisappearing()
    {
        if (_closed)
            _completion.TrySetResult();
        base.OnDisappearing();
    }
}

