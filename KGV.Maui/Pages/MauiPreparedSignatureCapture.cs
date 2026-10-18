using KGV.Core.Models;
using Microsoft.Maui.Controls;

namespace KGV.Maui.Pages;

public sealed record PreparedMembershipSignatureUiContext(
    string? MemberDisplayName = null,
    string? SecondaryDisplayName = null,
    string? RepresentativeDisplayName = null);

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

    public async Task<DigitalSignatureCapture?> CaptureAsync(PreparedSignatureRole role, CancellationToken cancellationToken = default)
    {
        var signer = SignerFor(role);
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
                TitleFor(role),
                isLastSignature: role == PreparedSignatureRole.Board,
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

    private string TitleFor(PreparedSignatureRole role) => role switch
    {
        PreparedSignatureRole.MemberApplication => WithName(_context.MemberDisplayName, "Mitglied – Mitgliedsantrag"),
        PreparedSignatureRole.MemberPrivacy => WithName(_context.MemberDisplayName, "Mitglied – Datenschutz"),
        PreparedSignatureRole.SecondaryApplication => WithName(_context.SecondaryDisplayName, "Nebenmitglied – Mitgliedsantrag"),
        PreparedSignatureRole.SecondaryPrivacy => WithName(_context.SecondaryDisplayName, "Nebenmitglied – Datenschutz"),
        PreparedSignatureRole.RepresentativeApplication => WithName(_context.RepresentativeDisplayName, "Gesetzlicher Vertreter – Mitgliedsantrag"),
        PreparedSignatureRole.RepresentativePrivacy => WithName(_context.RepresentativeDisplayName, "Gesetzlicher Vertreter – Datenschutz"),
        PreparedSignatureRole.Board => "Vorstand",
        _ => throw new ArgumentOutOfRangeException(nameof(role))
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
    private static SignerGroup SignerFor(PreparedSignatureRole role) => role switch
    {
        PreparedSignatureRole.MemberApplication or PreparedSignatureRole.MemberPrivacy => SignerGroup.Member,
        PreparedSignatureRole.SecondaryApplication or PreparedSignatureRole.SecondaryPrivacy => SignerGroup.Secondary,
        PreparedSignatureRole.RepresentativeApplication or PreparedSignatureRole.RepresentativePrivacy => SignerGroup.Representative,
        PreparedSignatureRole.Board => SignerGroup.Board,
        _ => throw new ArgumentOutOfRangeException(nameof(role))
    };

    private enum SignerGroup { Member, Secondary, Representative, Board }
}
