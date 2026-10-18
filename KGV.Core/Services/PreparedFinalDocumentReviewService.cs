using KGV.Core.Interfaces;
using KGV.Core.Models;

namespace KGV.Core.Services;

/// <summary>
/// Loads and completes the last-review state without generating or replacing a document.
/// </summary>
public sealed class PreparedFinalDocumentReviewService
{
    private readonly IPreparedContractRepository _repository;
    private readonly IFinalDocumentLoader _documentLoader;
    private KommunikationsvorgangDto? _contract;
    private LoadedDocument? _document;

    public PreparedFinalDocumentReviewService(
        IPreparedContractRepository repository,
        IFinalDocumentLoader documentLoader)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _documentLoader = documentLoader ?? throw new ArgumentNullException(nameof(documentLoader));
    }

    public bool CanComplete => _contract?.Status == PreparedContractConstants.FinalReview && _document is not null;

    public Task<PreparedFinalDocumentReviewResult> OpenAsync(
        KommunikationsvorgangDto contract,
        CancellationToken cancellationToken = default)
    {
        ValidateReviewContract(contract);
        _contract = contract;
        _document = null;
        return ReloadAsync(cancellationToken);
    }

    public async Task<PreparedFinalDocumentReviewResult> ReloadAsync(CancellationToken cancellationToken = default)
    {
        var contract = RequireReviewContract();
        _document = null;
        try
        {
            var document = await _documentLoader.LoadDokumentByIdAsync(contract.FinalDokumentId!.Value);
            ValidateLoadedDocument(contract.FinalDokumentId.Value, document);
            _document = document;
            return new(PreparedFinalDocumentReviewState.FinalDocumentLoaded, contract, document);
        }
        catch (Exception exception)
        {
            return new(PreparedFinalDocumentReviewState.FinalDocumentLoadFailed, contract, null, exception);
        }
    }

    public async Task<PreparedFinalDocumentReviewResult> CompleteAsync(CancellationToken cancellationToken = default)
    {
        var contract = RequireReviewContract();
        if (_document is null)
            throw new InvalidOperationException("Der Vertragsvorgang kann erst nach dem erfolgreichen Laden des Finaldokuments abgeschlossen werden.");

        try
        {
            var completed = await _repository.CompleteAsync(contract.Id, cancellationToken);
            if (completed.Id != contract.Id ||
                completed.Status != PreparedContractConstants.Completed ||
                completed.Revision != contract.Revision ||
                completed.FinalDokumentId != contract.FinalDokumentId)
            {
                throw new PreparedContractValidationException("Der Server hat den Abschluss des Vertragsvorgangs nicht korrekt bestätigt.");
            }

            _contract = completed;
            return new(PreparedFinalDocumentReviewState.Completed, completed, _document);
        }
        catch (Exception exception)
        {
            return new(PreparedFinalDocumentReviewState.CompleteFailed, contract, _document, exception);
        }
    }

    private KommunikationsvorgangDto RequireReviewContract()
    {
        if (_contract is null)
            throw new InvalidOperationException("Es wurde kein Vertragsvorgang zur letzten Prüfung geöffnet.");
        ValidateReviewContract(_contract);
        return _contract;
    }

    private static void ValidateReviewContract(KommunikationsvorgangDto contract)
    {
        ArgumentNullException.ThrowIfNull(contract);
        if (contract.Id <= 0 || contract.Status != PreparedContractConstants.FinalReview || contract.FinalDokumentId is not > 0)
            throw new PreparedContractValidationException("Der Vertragsvorgang enthält kein gültiges Finaldokument zur letzten Prüfung.");
    }

    private static void ValidateLoadedDocument(long expectedDocumentId, LoadedDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        if (document.Document is null || document.Document.Id != expectedDocumentId || document.Content is not { Length: >= 5 } content ||
            content[0] != (byte)'%' || content[1] != (byte)'P' || content[2] != (byte)'D' || content[3] != (byte)'F' || content[4] != (byte)'-')
        {
            throw new InvalidOperationException("Das geladene Finaldokument ist keine gültige PDF-Datei mit der erwarteten Dokument-ID.");
        }
    }
}
