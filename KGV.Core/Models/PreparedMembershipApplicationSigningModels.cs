namespace KGV.Core.Models;

public interface IPreparedSignatureCapture
{
    Task<DigitalSignatureCapture?> CaptureAsync(SignatureStep step, CancellationToken cancellationToken = default);
}

public enum PreparedMembershipApplicationSigningState
{
    Cancelled,
    SignaturesCaptured,
    FinalizeFailedRetryPossible,
    FinalDocumentReviewPersistenceFailed,
    FinalDocumentReadyForReview
}

public sealed record PreparedMembershipApplicationSigningResult(
    PreparedMembershipApplicationSigningState State,
    bool DiscardedCapturedSignatures,
    ContractFinalizeResult? FinalizeResult = null,
    KommunikationsvorgangDto? Contract = null,
    Exception? Error = null);
