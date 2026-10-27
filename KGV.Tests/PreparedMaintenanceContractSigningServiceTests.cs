using System.Text.Json;
using KGV.Core.Interfaces;
using KGV.Core.Models;
using KGV.Core.Services;

namespace KGV.Tests;

public sealed class PreparedMaintenanceContractSigningServiceTests
{
    [Fact]
    public async Task Start_or_resume_ensures_exactly_the_three_fixed_positions_and_fixed_member()
    {
        var context = Create(); var state = await context.Service.StartOrResumeAsync(context.Contract);
        Assert.Equal(new[] { "mitglied", "vorstand_1", "vorstand_2" }, context.Signatures.LastSlots.Select(slot => slot.Key));
        Assert.Equal(7, context.Signatures.LastSlots[0].MemberId); Assert.Equal(3, state.Progress.RequiredCount);
    }

    [Fact]
    public async Task Save_forwards_exactly_one_slot_and_the_structured_representative_assignment()
    {
        var context = Create();
        await context.Service.SaveSignatureAsync(context.Contract, new(context.Contract.Id, context.Contract.Revision, "vorstand_2", [1], 19, VereinsfunktionBesetzungId: 20, VertretungId: 21, VertreteneBesetzungId: 22));
        Assert.Equal(1, context.Signatures.SaveCalls); Assert.Equal("vorstand_2", context.Signatures.LastSave!.SignaturSchluessel);
        Assert.Equal(20L, context.Signatures.LastSave.VereinsfunktionBesetzungId); Assert.Equal(21L, context.Signatures.LastSave.VertretungId); Assert.Equal(22L, context.Signatures.LastSave.VertreteneBesetzungId);
    }

    [Fact]
    public async Task Progress_is_derived_from_persisted_positions_and_board_slots_remain_separate()
    {
        var context = Create(); var state = await context.Service.GetSignatureStateAsync(context.Contract);
        Assert.Equal(1, state.Progress.RequiredSignedCount); Assert.Equal(new[] { "vorstand_1", "vorstand_2" }, state.Positions.Where(position => position.UnterzeichnerTyp == "vereinsunterzeichner").Select(position => position.SignaturSchluessel));
    }

    [Fact]
    public async Task Finalize_requires_complete_positions_and_sends_only_the_process_identity()
    {
        var context = Create();
        await Assert.ThrowsAsync<PreparedContractValidationException>(() => context.Service.FinalizeAsync(context.Contract));
        context.Signatures.Complete = true; var result = await context.Service.FinalizeAsync(context.Contract);
        Assert.True(result.Completed); Assert.Equal(context.Contract.Id, context.Client.Request!.KommunikationsvorgangId); Assert.Equal(1, context.Client.FinalizeCalls);
    }

    [Fact]
    public async Task Failed_finalization_keeps_positions_and_retry_uses_the_same_process()
    {
        var context = Create(); context.Signatures.Complete = true; context.Client.Failure = new InvalidOperationException("temporär");
        var failed = await context.Service.FinalizeAsync(context.Contract);
        Assert.False(failed.Completed); Assert.Equal(0, context.Signatures.DeleteCalls);
        context.Client.Failure = null; var retried = await context.Service.FinalizeAsync(context.Contract);
        Assert.True(retried.Completed); Assert.Equal(new[] { context.Contract.Id, context.Contract.Id }, context.Client.RequestedIds);
    }

    [Fact]
    public async Task Completed_contract_never_starts_a_new_signing_flow()
    {
        var context = Create(PreparedContractConstants.Completed);
        await Assert.ThrowsAsync<PreparedContractValidationException>(() => context.Service.StartOrResumeAsync(context.Contract));
        Assert.Equal(0, context.Repository.StartCalls);
    }

    private static Context Create(string status = PreparedContractConstants.ReadyForSignature)
    {
        var draft = new PreparedMaintenanceContractDraft { MemberId = 7, WartungsvertragZuordnungId = 8, WartungsvertragId = 9, GueltigAb = "2026-01-01" };
        var contract = new KommunikationsvorgangDto { Id = 4, Vorgangstyp = PreparedContractConstants.ContractSignatureProcess, Dokumenttyp = PreparedContractConstants.MaintenanceContract, Status = status, MitgliedId = 7, Revision = 2, SignaturRevision = 2, SignaturGestartetAm = DateTimeOffset.UtcNow, EntwurfDaten = PreparedContractDraftParser.Serialize(draft) };
        var repository = new Repository(contract); var signatures = new Signatures(); var client = new Client();
        return new(contract, repository, signatures, client, new PreparedMaintenanceContractSigningService(repository, client, signatures));
    }
    private sealed record Context(KommunikationsvorgangDto Contract, Repository Repository, Signatures Signatures, Client Client, PreparedMaintenanceContractSigningService Service);
    private sealed class Signatures : IPreparedContractSignatureService
    {
        public bool Complete; public int SaveCalls; public int DeleteCalls; public PreparedContractSignatureSaveRequest? LastSave; public IReadOnlyList<PreparedContractSignatureSlot> LastSlots = [];
        private readonly IReadOnlyList<PreparedContractSignatureDto> _positions = [new() { Id = 1, SignaturSchluessel = "mitglied", Pflicht = true, Status = PreparedContractSignatureStatuses.Signed, MitgliedId = 7, UnterzeichnerTyp = SignaturePersonTypes.Member }, new() { Id = 2, SignaturSchluessel = "vorstand_1", Pflicht = true, Status = PreparedContractSignatureStatuses.Open, UnterzeichnerTyp = "vereinsunterzeichner" }, new() { Id = 3, SignaturSchluessel = "vorstand_2", Pflicht = true, Status = PreparedContractSignatureStatuses.Open, UnterzeichnerTyp = "vereinsunterzeichner" }];
        public Task<IReadOnlyList<PreparedContractSignatureDto>> EnsurePositionsAsync(KommunikationsvorgangDto contract, IReadOnlyList<PreparedContractSignatureSlot> slots, CancellationToken cancellationToken = default) { LastSlots = slots; return Task.FromResult(_positions); }
        public Task<IReadOnlyList<PreparedContractSignatureDto>> GetSignaturesAsync(KommunikationsvorgangDto contract, CancellationToken cancellationToken = default) => Task.FromResult(_positions);
        public Task<IReadOnlyList<PreparedContractAuthorizedSignatoryDto>> GetAuthorizedClubSignatoriesAsync(long kommunikationsvorgangId, int signaturRevision, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<PreparedContractAuthorizedSignatoryDto>>([]);
        public Task<PreparedContractSignatureDto> SaveSignatureAsync(PreparedContractSignatureSaveRequest request, CancellationToken cancellationToken = default) { SaveCalls++; LastSave = request; return Task.FromResult(new PreparedContractSignatureDto { SignaturSchluessel = request.SignaturSchluessel, Status = PreparedContractSignatureStatuses.Signed }); }
        public Task<bool> AreRequiredSignaturesCompleteAsync(KommunikationsvorgangDto contract, CancellationToken cancellationToken = default) => Task.FromResult(Complete);
    }
    private sealed class Client : IContractGenerationClient
    {
        public MaintenanceContractFinalizeRequest? Request; public int FinalizeCalls; public Exception? Failure; public List<long> RequestedIds { get; } = [];
        public Task<byte[]> PreviewMembershipApplicationAsync(MitgliedsantragDokumentRequest request, decimal admissionFee, CancellationToken cancellationToken = default) => throw new NotSupportedException(); public Task<byte[]> PreviewLeaseContractAsync(PachtvertragDokumentRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException(); public Task<byte[]> PreviewMaintenanceContractAsync(long kommunikationsvorgangId, int memberId, CancellationToken cancellationToken = default) => throw new NotSupportedException(); public Task<ContractFinalizeResult> FinalizeMembershipApplicationAsync(MembershipApplicationFinalizeRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException(); public Task<ContractFinalizeResult> FinalizeLeaseContractAsync(LeaseContractFinalizeRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<ContractFinalizeResult> FinalizeMaintenanceContractAsync(MaintenanceContractFinalizeRequest request, CancellationToken cancellationToken = default) { FinalizeCalls++; Request = request; RequestedIds.Add(request.KommunikationsvorgangId); if (Failure is not null) throw Failure; return Task.FromResult(new ContractFinalizeResult(9, "ok")); }
    }
    private sealed class Repository(KommunikationsvorgangDto contract) : IPreparedContractRepository
    {
        public int StartCalls; public Task<KommunikationsvorgangDto> LoadAsync(long id, CancellationToken cancellationToken = default) => Task.FromResult(contract); public Task<KommunikationsvorgangDto> StartSignatureAsync(long id, int revision, CancellationToken cancellationToken = default) { StartCalls++; return Task.FromResult(contract with { Status = PreparedContractConstants.ReadyForSignature }); }
        public Task<IReadOnlyList<KommunikationsvorgangDto>> ListActiveAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException(); public Task<KommunikationsvorgangDto?> FindActiveAsync(string documentType, int memberId, int? parcelId, CancellationToken cancellationToken = default) => throw new NotSupportedException(); public Task<KommunikationsvorgangDto> CreateAsync(string documentType, int memberId, int? parcelId, JsonElement draft, CancellationToken cancellationToken = default) => throw new NotSupportedException(); public Task<KommunikationsvorgangDto> ClaimAsync(long id, CancellationToken cancellationToken = default) => throw new NotSupportedException(); public Task ReleaseAsync(long id, CancellationToken cancellationToken = default) => throw new NotSupportedException(); public Task<KommunikationsvorgangDto> UpdateDraftAsync(long id, int revision, JsonElement draft, CancellationToken cancellationToken = default) => throw new NotSupportedException(); public Task<KommunikationsvorgangDto> MarkFinalDocumentReadyForReviewAsync(long id, int revision, long documentId, CancellationToken cancellationToken = default) => throw new NotSupportedException(); public Task<KommunikationsvorgangDto> CompleteAsync(long id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
