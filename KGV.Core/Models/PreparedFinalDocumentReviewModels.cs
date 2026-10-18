namespace KGV.Core.Models;

public enum PreparedFinalDocumentReviewState
{
    FinalDocumentLoaded,
    FinalDocumentLoadFailed,
    CompleteFailed,
    Completed
}

public sealed record PreparedFinalDocumentReviewResult(
    PreparedFinalDocumentReviewState State,
    KommunikationsvorgangDto Contract,
    LoadedDocument? Document = null,
    Exception? Error = null);
