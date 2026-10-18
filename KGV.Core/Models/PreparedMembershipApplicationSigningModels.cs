namespace KGV.Core.Models;

public enum PreparedSignatureRole
{
    MemberApplication,
    MemberPrivacy,
    RepresentativeApplication,
    RepresentativePrivacy,
    SecondaryApplication,
    SecondaryPrivacy,
    Board
}

public interface IPreparedSignatureCapture
{
    Task<DigitalSignatureCapture?> CaptureAsync(PreparedSignatureRole role, CancellationToken cancellationToken = default);
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
