using KGV.Core.Interfaces;
using KGV.Core.Models;

namespace KGV.Core.Services;

/// <summary>Loads the current mandatory-document status and turns it into the immutable application snapshot.</summary>
public sealed class MitgliedsantragVereinsunterlagenService
{
    private readonly ISupabaseService _supabaseService;

    public MitgliedsantragVereinsunterlagenService(ISupabaseService supabaseService)
        => _supabaseService = supabaseService ?? throw new ArgumentNullException(nameof(supabaseService));

    public async Task<IReadOnlyList<VereinsunterlagenNachweisSnapshotItem>> CreateCurrentSnapshotAsync(long mitgliedId, CancellationToken cancellationToken = default)
    {
        if (mitgliedId <= 0) throw new PreparedContractValidationException("Für den Mitgliedsantrag fehlt ein gültiges Mitglied.");
        cancellationToken.ThrowIfCancellationRequested();
        var status = await _supabaseService.GetMitgliedVereinsdokumentStatusAsync(mitgliedId, DateOnly.FromDateTime(DateTime.Today));
        return VereinsunterlagenNachweisHelper.CreateSnapshot(status);
    }
}
