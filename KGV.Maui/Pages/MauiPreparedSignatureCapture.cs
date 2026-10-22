using KGV.Core.Models;
using Microsoft.Maui.Controls;

namespace KGV.Maui.Pages;

public sealed record PreparedMembershipSignatureUiContext(
    string? MemberDisplayName = null,
    string? SecondaryDisplayName = null,
    string? RepresentativeDisplayName = null);

/// <summary>Immutable identity of the one persistent position currently being captured.</summary>
public sealed record MauiPreparedSignaturePosition(
    long KommunikationsvorgangId,
    int SignaturRevision,
    string SignaturSchluessel,
    string Anzeige,
    PreparedContractAuthorizedSignatoryDto? Vereinsunterzeichner = null);

public sealed class MauiPreparedSignatureCapture : IPreparedSignatureCapture
{
    private readonly Page _hostPage;
    private readonly DocumentInfo _sourceDocument;
    private readonly PreparedMembershipSignatureUiContext _context;
    private SignerGroup? _lastSuccessfulSigner;
    private int _successfulCaptures;

    public MauiPreparedSignatureCapture(
        Page hostPage,
        DocumentInfo sourceDocument,
        PreparedMembershipSignatureUiContext? context = null)
    {
        _hostPage = hostPage ?? throw new ArgumentNullException(nameof(hostPage));
        _sourceDocument = sourceDocument ?? throw new ArgumentNullException(nameof(sourceDocument));
        _context = context ?? new PreparedMembershipSignatureUiContext();
    }

    public async Task<DigitalSignatureCapture?> CaptureAsync(SignatureStep step, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(step);
        var signer = SignerFor(step.PersonType);
        var handoffShown = false;
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!handoffShown && _lastSuccessfulSigner is not null && _lastSuccessfulSigner != signer)
            {
                await _hostPage.DisplayAlertAsync("Unterschrift", HandoffText(signer), "OK");
                handoffShown = true;
            }

            var capture = await SignatureFlowHelper.CaptureSignatureAsync(
                _hostPage.Navigation,
                _sourceDocument,
                TitleFor(step),
                isLastSignature: step.PersonType == SignaturePersonTypes.Board,
                forceLandscape: false);

            if (capture is not null)
            {
                _successfulCaptures++;
                _lastSuccessfulSigner = signer;
                return capture;
            }

            if (_successfulCaptures == 0)
                return null;

            var discard = await _hostPage.DisplayAlertAsync(
                "Unterschriften verwerfen?",
                "Bereits erfasste Unterschriften werden verworfen. Der Vorgang bleibt zur Signatur gespeichert.",
                "Unterschriften verwerfen",
                "Weiter unterschreiben");
            if (discard)
                return null;
        }
    }

    /// <summary>Captures exactly one persistent contract-signature position. The caller owns saving and navigation.</summary>
    public Task<DigitalSignatureCapture?> CaptureSingleAsync(MauiPreparedSignaturePosition position, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(position);
        if (position.KommunikationsvorgangId <= 0 || position.SignaturRevision <= 0 || string.IsNullOrWhiteSpace(position.SignaturSchluessel) || string.IsNullOrWhiteSpace(position.Anzeige))
            throw new ArgumentException("Die persistente Signaturposition ist unvollständig.", nameof(position));
        if (position.SignaturSchluessel is PreparedContractBoardSignatureKeys.Board1 or PreparedContractBoardSignatureKeys.Board2 && position.Vereinsunterzeichner is null)
            throw new ArgumentException("Für eine Vereinsposition fehlt der berechtigte Vereinsunterzeichner.", nameof(position));
        cancellationToken.ThrowIfCancellationRequested();
        return SignatureFlowHelper.CaptureSignatureAsync(_hostPage.Navigation, _sourceDocument, position.Anzeige, isLastSignature: true, forceLandscape: false);
    }

    private string TitleFor(SignatureStep step) => step.PersonType switch
    {
        SignaturePersonTypes.Member => WithName(_context.MemberDisplayName, step.DisplayName),
        SignaturePersonTypes.SecondaryMember => WithName(_context.SecondaryDisplayName, step.DisplayName),
        SignaturePersonTypes.LegalRepresentative => WithName(_context.RepresentativeDisplayName, step.DisplayName),
        _ => step.DisplayName
    };

    private string HandoffText(SignerGroup signer) => signer switch
    {
        SignerGroup.Secondary => AppendName("Bitte das Gerät jetzt an das Nebenmitglied zur Unterschrift übergeben.", _context.SecondaryDisplayName),
        SignerGroup.Representative => AppendName("Bitte das Gerät jetzt an den gesetzlichen Vertreter zur Unterschrift übergeben.", _context.RepresentativeDisplayName),
        SignerGroup.Board => "Bitte das Gerät jetzt an den Vorstand zur Unterschrift übergeben.",
        _ => string.Empty
    };

    private static string WithName(string? name, string title) => string.IsNullOrWhiteSpace(name) ? title : $"{name.Trim()} – {title}";
    private static string AppendName(string text, string? name) => string.IsNullOrWhiteSpace(name) ? text : $"{text}\n{name.Trim()}";
    private static SignerGroup SignerFor(string personType) => personType switch
    {
        SignaturePersonTypes.Member => SignerGroup.Member,
        SignaturePersonTypes.SecondaryMember => SignerGroup.Secondary,
        SignaturePersonTypes.LegalRepresentative => SignerGroup.Representative,
        SignaturePersonTypes.Board => SignerGroup.Board,
        _ => throw new ArgumentOutOfRangeException(nameof(personType))
    };

    private enum SignerGroup { Member, Secondary, Representative, Board }
}
