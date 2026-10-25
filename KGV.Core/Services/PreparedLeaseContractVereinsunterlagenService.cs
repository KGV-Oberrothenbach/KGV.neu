using KGV.Core.Interfaces;
using KGV.Core.Models;

namespace KGV.Core.Services;

/// <summary>Revalidiert den gespeicherten Unterlagen-Snapshot unmittelbar vor einem künftigen Pachtvertrags-Signaturstart.</summary>
public sealed class PreparedLeaseContractVereinsunterlagenService
{
    private readonly IPreparedContractRepository _repository;
    private readonly MitgliedsantragVereinsunterlagenService _vereinsunterlagenService;

    public PreparedLeaseContractVereinsunterlagenService(
        IPreparedContractRepository repository,
        MitgliedsantragVereinsunterlagenService vereinsunterlagenService)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _vereinsunterlagenService = vereinsunterlagenService ?? throw new ArgumentNullException(nameof(vereinsunterlagenService));
    }

    public async Task RevalidateBeforeStartAsync(KommunikationsvorgangDto contract, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(contract);
        if (contract.Dokumenttyp != PreparedContractConstants.LeaseContract || contract.Status != PreparedContractConstants.Prepared)
            throw new PreparedContractValidationException("Der Vertragsvorgang ist kein bearbeitbarer Pachtvertragsentwurf.");

        if (PreparedContractDraftParser.Parse(contract.EntwurfDaten) is not PreparedLeaseContractDraft draft || draft.MemberId != contract.MitgliedId)
            throw new PreparedContractValidationException("Der gespeicherte Pachtvertragsentwurf passt nicht zum Vertragsvorgang.");
        if (!VereinsunterlagenNachweisHelper.IsComplete(draft.VereinsunterlagenNachweis, PreparedContractConstants.LeaseContract))
            throw new PreparedContractValidationException("Der vorbereitete Pachtvertrag enthält noch keinen Vereinsunterlagen-Snapshot. Bitte erstellen Sie zuerst eine neue Vorschau.");

        var currentSnapshot = await _vereinsunterlagenService.CreateCurrentSnapshotAsync(contract.MitgliedId, PreparedContractConstants.LeaseContract, cancellationToken);
        if (VereinsunterlagenNachweisHelper.Equivalent(draft.VereinsunterlagenNachweis, currentSnapshot, PreparedContractConstants.LeaseContract))
            return;

        await _repository.UpdateDraftAsync(
            contract.Id,
            contract.Revision,
            PreparedContractDraftParser.Serialize(draft with { VereinsunterlagenNachweis = currentSnapshot }),
            cancellationToken);
        throw new PreparedContractValidationException("Die Pflichtunterlagen haben sich seit der Vorschau geändert. Der aktualisierte Entwurf wurde gespeichert; bitte erstellen Sie eine neue Vorschau.");
    }
}
