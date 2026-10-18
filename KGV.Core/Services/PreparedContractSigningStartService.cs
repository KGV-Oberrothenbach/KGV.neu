using KGV.Core.Interfaces;
using KGV.Core.Models;

namespace KGV.Core.Services;

public sealed class PreparedContractSigningStartService
{
    private readonly IPreparedContractRepository _repository;

    public PreparedContractSigningStartService(IPreparedContractRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public async Task<KommunikationsvorgangDto> StartAsync(KommunikationsvorgangDto contract, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(contract);
        ValidateStartable(contract);

        var started = await _repository.StartSignatureAsync(contract.Id, contract.Revision, cancellationToken);
        ValidateStarted(contract, started);
        return started;
    }

    private static void ValidateStartable(KommunikationsvorgangDto contract)
    {
        if (contract.Id <= 0) throw new PreparedContractValidationException("Der Vertragsvorgang hat keine gültige ID.");
        if (contract.Revision < 1) throw new PreparedContractValidationException("Der Vertragsvorgang hat keine gültige Revision.");
        if (contract.Status != PreparedContractConstants.Prepared) throw new PreparedContractValidationException("Der Vertragsvorgang ist nicht mehr vorbereitbar für die Signatur.");
        if (contract.SignaturRevision is not null) throw new PreparedContractValidationException("Der Vertragsvorgang hat bereits eine eingefrorene Signaturrevision.");
        if (contract.SignaturGestartetAm is not null) throw new PreparedContractValidationException("Die Signatur wurde bereits gestartet.");
        if (contract.Dokumenttyp is not PreparedContractConstants.MembershipApplication and not PreparedContractConstants.LeaseContract)
            throw new PreparedContractValidationException("Der Dokumenttyp wird für den Signaturstart nicht unterstützt.");
    }

    private static void ValidateStarted(KommunikationsvorgangDto expected, KommunikationsvorgangDto started)
    {
        if (started is null) throw new PreparedContractValidationException("Der Server hat keinen gestarteten Vertragsvorgang zurückgegeben.");
        if (started.Id != expected.Id) throw new PreparedContractValidationException("Der gestartete Vertragsvorgang hat eine andere ID.");
        if (started.Dokumenttyp != expected.Dokumenttyp) throw new PreparedContractValidationException("Der gestartete Vertragsvorgang hat einen anderen Dokumenttyp.");
        if (started.MitgliedId != expected.MitgliedId) throw new PreparedContractValidationException("Der gestartete Vertragsvorgang gehört zu einem anderen Mitglied.");
        if (started.ParzelleId != expected.ParzelleId) throw new PreparedContractValidationException("Der gestartete Vertragsvorgang gehört zu einer anderen Parzelle.");
        if (started.Revision != expected.Revision) throw new PreparedContractValidationException("Der Server hat die eingefrorene Vertragsrevision geändert.");
        if (started.Status != PreparedContractConstants.ReadyForSignature) throw new PreparedContractValidationException("Der Server hat den Signaturstart nicht bestätigt.");
        if (started.SignaturRevision is null || started.SignaturRevision != expected.Revision || started.SignaturRevision != started.Revision)
            throw new PreparedContractValidationException("Der Server hat die Signaturrevision nicht korrekt eingefroren.");
        if (started.SignaturGestartetAm is null) throw new PreparedContractValidationException("Der Server hat den Zeitpunkt des Signaturstarts nicht bestätigt.");
        if (expected.ZugewiesenAn is not null && started.ZugewiesenAn != expected.ZugewiesenAn)
            throw new PreparedContractValidationException("Die Zuweisung des Vertragsvorgangs ging beim Signaturstart verloren.");
    }
}
