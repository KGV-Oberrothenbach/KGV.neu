using System.Text.Json;
using KGV.Core.Interfaces;
using KGV.Core.Models;
using KGV.Core.Services;

namespace KGV.Tests;

public sealed class PreparedMembershipApplicationPersistentSigningTests
{
    [Fact] public async Task Start_or_resume_ensures_persistent_positions_and_uses_their_progress()
    {
        var context = Create(); var state = await context.Service.StartOrResumeAsync(context.Contract);
        Assert.Equal(1, context.Signatures.EnsureCalls); Assert.Equal(new[] { "member.application", "member.privacy", "vorstand_1", "vorstand_2" }, context.Signatures.LastSlots.Select(x => x.Key));
        Assert.Equal(4, state.Progress.RequiredCount); Assert.Equal(1, state.Progress.RequiredSignedCount);
    }

    [Fact] public async Task Save_forwards_exactly_one_position_to_common_service()
    {
        var context = Create(); await context.Service.SaveSignatureAsync(context.Contract, new(context.Contract.Id, context.Contract.Revision, "member.application", [1, 2], context.Contract.MitgliedId));
        Assert.Equal(1, context.Signatures.SaveCalls); Assert.Equal("member.application", context.Signatures.LastSave!.SignaturSchluessel);
    }

    [Fact] public async Task Reload_keeps_the_persisted_position_state_and_progress()
    {
        var context = Create();
        var state = await context.Service.GetSignatureStateAsync(context.Contract);
        var memberApplication = Assert.Single(state.Positions.Where(position => position.SignaturSchluessel == "member.application"));
        Assert.Equal(PreparedContractSignatureStatuses.Signed, memberApplication.Status);
        Assert.Equal(1, state.Progress.RequiredSignedCount);
    }

    [Fact] public async Task Any_open_planned_position_can_be_saved_with_its_structured_board_assignment()
    {
        var context = Create();
        var request = new PreparedContractSignatureSaveRequest(context.Contract.Id, context.Contract.Revision, "vorstand_2", [3, 4], 18, VereinsfunktionBesetzungId: 19, VertretungId: 20, VertreteneBesetzungId: 21);
        await context.Service.SaveSignatureAsync(context.Contract, request);
        Assert.Equal("vorstand_2", context.Signatures.LastSave!.SignaturSchluessel);
        Assert.Equal(19L, context.Signatures.LastSave.VereinsfunktionBesetzungId);
        Assert.Equal(20L, context.Signatures.LastSave.VertretungId);
        Assert.Equal(21L, context.Signatures.LastSave.VertreteneBesetzungId);
    }

    [Fact] public async Task Finalize_uses_common_completeness_and_sends_no_signature_images()
    {
        var context = Create(); context.Signatures.Complete = true; var result = await context.Service.FinalizeAsync(context.Contract);
        Assert.Equal(PreparedMembershipApplicationSigningState.FinalDocumentReadyForReview, result.State); Assert.Equal(1, context.Signatures.CompleteCalls);
        Assert.Null(context.Client.Request!.ApplicationMemberSignature); Assert.Null(context.Client.Request.BoardSignature); Assert.Equal(context.Contract.Id, context.Client.Request.KommunikationsvorgangId);
    }

    private static Context Create()
    {
        var draft = new PreparedMembershipApplicationDraft { MemberId = 7, StartDate = "2026-11-01", MemberFee = 12m };
        var contract = new KommunikationsvorgangDto { Id = 4, Vorgangstyp = PreparedContractConstants.ContractSignatureProcess, Dokumenttyp = PreparedContractConstants.MembershipApplication, Status = PreparedContractConstants.ReadyForSignature, MitgliedId = 7, Revision = 2, SignaturRevision = 2, SignaturGestartetAm = DateTimeOffset.UtcNow, EntwurfDaten = PreparedContractDraftParser.Serialize(draft) };
        var repository = new Repository(contract); var signatures = new Signatures(); var client = new Client();
        return new(contract, signatures, client, new PreparedMembershipApplicationSigningService(repository, client, persistentSignatures: signatures));
    }
    private sealed record Context(KommunikationsvorgangDto Contract, Signatures Signatures, Client Client, PreparedMembershipApplicationSigningService Service);
    private sealed class Signatures : IPreparedContractSignatureService
    {
        public int EnsureCalls; public int SaveCalls; public int CompleteCalls; public bool Complete; public IReadOnlyList<PreparedContractSignatureSlot> LastSlots = []; public PreparedContractSignatureSaveRequest? LastSave;
        private readonly List<PreparedContractSignatureDto> _positions = [new() { Id = 1, SignaturSchluessel = "member.application", Pflicht = true, Status = PreparedContractSignatureStatuses.Signed }, new() { Id = 2, SignaturSchluessel = "member.privacy", Pflicht = true, Status = PreparedContractSignatureStatuses.Open }, new() { Id = 3, SignaturSchluessel = "vorstand_1", Pflicht = true, Status = PreparedContractSignatureStatuses.Open }, new() { Id = 4, SignaturSchluessel = "vorstand_2", Pflicht = true, Status = PreparedContractSignatureStatuses.Open }];
        public Task<IReadOnlyList<PreparedContractSignatureDto>> EnsurePositionsAsync(KommunikationsvorgangDto contract, IReadOnlyList<PreparedContractSignatureSlot> slots, CancellationToken cancellationToken = default) { EnsureCalls++; LastSlots = slots; return Task.FromResult<IReadOnlyList<PreparedContractSignatureDto>>(_positions); }
        public Task<IReadOnlyList<PreparedContractSignatureDto>> GetSignaturesAsync(KommunikationsvorgangDto contract, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<PreparedContractSignatureDto>>(_positions);
        public Task<IReadOnlyList<PreparedContractAuthorizedSignatoryDto>> GetAuthorizedClubSignatoriesAsync(long kommunikationsvorgangId, int signaturRevision, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<PreparedContractAuthorizedSignatoryDto>>([]);
        public Task<PreparedContractSignatureDto> SaveSignatureAsync(PreparedContractSignatureSaveRequest request, CancellationToken cancellationToken = default) { SaveCalls++; LastSave = request; return Task.FromResult(new PreparedContractSignatureDto { SignaturSchluessel = request.SignaturSchluessel, Status = PreparedContractSignatureStatuses.Signed }); }
        public Task<bool> AreRequiredSignaturesCompleteAsync(KommunikationsvorgangDto contract, CancellationToken cancellationToken = default) { CompleteCalls++; return Task.FromResult(Complete); }
    }
    private sealed class Client : IContractGenerationClient
    {
        public MembershipApplicationFinalizeRequest? Request; public Task<byte[]> PreviewMembershipApplicationAsync(MitgliedsantragDokumentRequest request, decimal admissionFee, CancellationToken cancellationToken = default) => throw new NotSupportedException(); public Task<byte[]> PreviewLeaseContractAsync(PachtvertragDokumentRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException(); public Task<byte[]> PreviewMaintenanceContractAsync(long kommunikationsvorgangId, int memberId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<ContractFinalizeResult> FinalizeMembershipApplicationAsync(MembershipApplicationFinalizeRequest request, CancellationToken cancellationToken = default) { Request = request; return Task.FromResult(new ContractFinalizeResult(9, "ok")); } public Task<ContractFinalizeResult> FinalizeLeaseContractAsync(LeaseContractFinalizeRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException(); public Task<ContractFinalizeResult> FinalizeMaintenanceContractAsync(MaintenanceContractFinalizeRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
    private sealed class Repository(KommunikationsvorgangDto contract) : IPreparedContractRepository
    {
        public Task<KommunikationsvorgangDto> LoadAsync(long id, CancellationToken cancellationToken = default) => Task.FromResult(contract); public Task<KommunikationsvorgangDto> MarkFinalDocumentReadyForReviewAsync(long id, int revision, long documentId, CancellationToken cancellationToken = default) => Task.FromResult(contract with { Status = PreparedContractConstants.FinalReview, FinalDokumentId = documentId });
        public Task<IReadOnlyList<KommunikationsvorgangDto>> ListActiveAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException(); public Task<KommunikationsvorgangDto?> FindActiveAsync(string documentType, int memberId, int? parcelId, CancellationToken cancellationToken = default) => throw new NotSupportedException(); public Task<KommunikationsvorgangDto> CreateAsync(string documentType, int memberId, int? parcelId, JsonElement draft, CancellationToken cancellationToken = default) => throw new NotSupportedException(); public Task<KommunikationsvorgangDto> ClaimAsync(long id, CancellationToken cancellationToken = default) => throw new NotSupportedException(); public Task ReleaseAsync(long id, CancellationToken cancellationToken = default) => throw new NotSupportedException(); public Task<KommunikationsvorgangDto> UpdateDraftAsync(long id, int revision, JsonElement draft, CancellationToken cancellationToken = default) => throw new NotSupportedException(); public Task<KommunikationsvorgangDto> StartSignatureAsync(long id, int revision, CancellationToken cancellationToken = default) => throw new NotSupportedException(); public Task<KommunikationsvorgangDto> CompleteAsync(long id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
