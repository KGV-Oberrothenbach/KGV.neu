using System.Text.Json;
using KGV.Core.Interfaces;
using KGV.Core.Models;
using KGV.Core.Services;

namespace KGV.Tests;

public sealed class PreparedMembershipApplicationSigningServiceTests
{
    [Fact] public async Task Prepared_adult_without_secondary_captures_3_signatures() => Assert.Equal(3, (await Run()).Capture.Calls.Count);
    [Fact] public async Task Prepared_adult_with_secondary_captures_5_signatures() => Assert.Equal(5, (await Run(secondary: true)).Capture.Calls.Count);
    [Fact] public async Task Prepared_minor_captures_5_signatures() => Assert.Equal(5, (await Run(minor: true)).Capture.Calls.Count);
    [Fact] public async Task Uses_exact_capture_order_for_adult() => Assert.Equal(new[] { MembershipApplicationSignatureKeys.MemberApplication, MembershipApplicationSignatureKeys.MemberPrivacy, MembershipApplicationSignatureKeys.BoardApproval }, (await Run()).Capture.Calls);
    [Fact] public async Task Uses_exact_capture_order_for_secondary() => Assert.Equal(new[] { MembershipApplicationSignatureKeys.MemberApplication, MembershipApplicationSignatureKeys.MemberPrivacy, MembershipApplicationSignatureKeys.SecondaryApplication, MembershipApplicationSignatureKeys.SecondaryPrivacy, MembershipApplicationSignatureKeys.BoardApproval }, (await Run(secondary: true)).Capture.Calls);
    [Fact] public async Task Uses_exact_capture_order_for_minor() => Assert.Equal(new[] { MembershipApplicationSignatureKeys.MemberApplication, MembershipApplicationSignatureKeys.MemberPrivacy, MembershipApplicationSignatureKeys.RepresentativeApplication, MembershipApplicationSignatureKeys.RepresentativePrivacy, MembershipApplicationSignatureKeys.BoardApproval }, (await Run(minor: true)).Capture.Calls);
    [Fact] public async Task Application_and_privacy_are_separate_capture_calls() { var run = await Run(); Assert.Contains(MembershipApplicationSignatureKeys.MemberApplication, run.Capture.Calls); Assert.Contains(MembershipApplicationSignatureKeys.MemberPrivacy, run.Capture.Calls); }
    [Fact] public async Task Uses_stored_member_fee() { var run = await Run(); Assert.Equal(42.5m, run.Client.Request!.AdmissionFee); }
    [Fact] public async Task Prepared_contract_calls_start_once() { var run = await Run(); Assert.Equal(1, run.Repository.StartCalls); }
    [Fact] public async Task ReadyForSignature_contract_does_not_start_again() { var run = await Run(ready: true); Assert.Equal(0, run.Repository.StartCalls); }
    [Fact] public async Task Validates_existing_frozen_revision() { var run = Create(ready: true); run.Contract = run.Contract with { SignaturRevision = 1 }; await Assert.ThrowsAsync<PreparedContractValidationException>(() => run.Service.RunAsync(run.Contract, run.Capture)); }
    [Fact] public async Task Encodes_every_capture_as_png_data_url() { var run = await Run(secondary: true); Assert.All(Signatures(run.Client.Request!), value => Assert.StartsWith("data:image/png;base64,", value)); }
    [Fact] public async Task Finalize_receives_correct_member_fields() { var run = await Run(); Assert.NotNull(run.Client.Request!.ApplicationMemberSignature); Assert.NotNull(run.Client.Request.PrivacyMemberSignature); Assert.Null(run.Client.Request.ApplicationRepresentativeSignature); }
    [Fact] public async Task Finalize_receives_correct_secondary_fields() { var run = await Run(secondary: true); Assert.NotNull(run.Client.Request!.ApplicationSecondarySignature); Assert.NotNull(run.Client.Request.PrivacySecondarySignature); }
    [Fact] public async Task Finalize_receives_correct_representative_fields() { var run = await Run(minor: true); Assert.NotNull(run.Client.Request!.ApplicationRepresentativeSignature); Assert.NotNull(run.Client.Request.PrivacyRepresentativeSignature); Assert.Null(run.Client.Request.ApplicationSecondarySignature); }
    [Fact] public async Task Finalize_receives_board_signature() { var run = await Run(); Assert.NotNull(run.Client.Request!.BoardSignature); }
    [Fact] public async Task Finalize_failure_keeps_signatures_for_retry() { var run = Create(); run.Client.FailFinalize = true; var result = await run.Service.RunAsync(run.Contract, run.Capture); Assert.Equal(PreparedMembershipApplicationSigningState.FinalizeFailedRetryPossible, result.State); Assert.Equal(0, run.Repository.MarkCalls); Assert.Equal(0, run.Repository.CompleteCalls); run.Client.FailFinalize = false; Assert.Equal(PreparedMembershipApplicationSigningState.FinalDocumentReadyForReview, (await run.Service.RetryAsync()).State); Assert.Equal(1, run.Repository.MarkCalls); }
    [Fact] public async Task Retry_after_finalize_failure_does_not_capture_again() { var run = Create(); run.Client.FailFinalize = true; await run.Service.RunAsync(run.Contract, run.Capture); var captures = run.Capture.Calls.Count; run.Client.FailFinalize = false; await run.Service.RetryAsync(); Assert.Equal(captures, run.Capture.Calls.Count); }
    [Fact] public async Task Finalize_marks_final_document_ready_for_review_without_complete() { var run = Create(); var result = await run.Service.RunAsync(run.Contract, run.Capture); Assert.Equal(PreparedMembershipApplicationSigningState.FinalDocumentReadyForReview, result.State); Assert.Equal(1, run.Client.FinalizeCalls); Assert.Equal(1, run.Repository.MarkCalls); Assert.Equal(0, run.Repository.CompleteCalls); Assert.Equal(5, result.FinalizeResult!.DocumentId); Assert.Equal(5, result.Contract!.FinalDokumentId); Assert.Equal(PreparedContractConstants.FinalReview, result.Contract.Status); }
    [Fact] public async Task Mark_failure_keeps_document_and_signatures_for_retry() { var run = Create(); run.Repository.FailMark = true; var result = await run.Service.RunAsync(run.Contract, run.Capture); Assert.Equal(PreparedMembershipApplicationSigningState.FinalDocumentReviewPersistenceFailed, result.State); Assert.Equal(5, result.FinalizeResult!.DocumentId); Assert.Equal(1, run.Client.FinalizeCalls); Assert.Equal(0, run.Repository.CompleteCalls); run.Repository.FailMark = false; Assert.Equal(PreparedMembershipApplicationSigningState.FinalDocumentReadyForReview, (await run.Service.RetryAsync()).State); Assert.Equal(1, run.Client.FinalizeCalls); Assert.Equal(2, run.Repository.MarkCalls); }
    [Fact] public async Task Invalid_finalize_document_id_is_not_accepted() { var run = Create(); run.Client.DocumentId = null; var result = await run.Service.RunAsync(run.Contract, run.Capture); Assert.Equal(PreparedMembershipApplicationSigningState.FinalDocumentReviewPersistenceFailed, result.State); Assert.Equal(0, run.Repository.MarkCalls); Assert.Equal(0, run.Repository.CompleteCalls); }
    [Fact] public async Task Cancel_before_first_signature_does_not_finalize() { var run = Create(); run.Capture.CancelAt = 0; var result = await run.Service.RunAsync(run.Contract, run.Capture); Assert.Equal(PreparedMembershipApplicationSigningState.Cancelled, result.State); Assert.Equal(0, run.Client.FinalizeCalls); }
    [Fact] public async Task Cancel_after_signature_discards_in_memory_signatures() { var run = Create(); run.Capture.CancelAt = 1; var result = await run.Service.RunAsync(run.Contract, run.Capture); Assert.True(result.DiscardedCapturedSignatures); await Assert.ThrowsAsync<InvalidOperationException>(() => run.Service.RetryAsync()); }
    [Fact] public async Task Cancel_does_not_change_contract_status_back() { var run = Create(); run.Capture.CancelAt = 0; await run.Service.RunAsync(run.Contract, run.Capture); Assert.Equal(PreparedContractConstants.ReadyForSignature, run.Repository.Started!.Status); }
    [Fact] public async Task No_signature_persistence() { var run = await Run(); Assert.Equal(0, run.Repository.UpdateDraftCalls); }

    private static async Task<TestContext> Run(bool secondary = false, bool minor = false, bool ready = false) { var run = Create(secondary, minor, ready); Assert.Equal(PreparedMembershipApplicationSigningState.FinalDocumentReadyForReview, (await run.Service.RunAsync(run.Contract, run.Capture)).State); return run; }
    private static TestContext Create(bool secondary = false, bool minor = false, bool ready = false)
    {
        var draft = new PreparedMembershipApplicationDraft { MemberId = 7, StartDate = "2026-11-01", MemberFee = 42.5m, IncludeSecondaryMember = secondary, Representative = minor ? new LegalRepresentativeDraft { Mode = "existing", MemberId = 8 } : null };
        var contract = new KommunikationsvorgangDto { Id = 11, Dokumenttyp = PreparedContractConstants.MembershipApplication, Status = ready ? PreparedContractConstants.ReadyForSignature : PreparedContractConstants.Prepared, MitgliedId = 7, Revision = 2, SignaturRevision = ready ? 2 : null, SignaturGestartetAm = ready ? DateTimeOffset.UtcNow : null, EntwurfDaten = PreparedContractDraftParser.Serialize(draft) };
        var repository = new Repository { ToStart = contract }; var client = new Client(); var capture = new Capture();
        return new TestContext(contract, repository, client, capture, new PreparedMembershipApplicationSigningService(repository, client));
    }
    private static IEnumerable<string?> Signatures(MembershipApplicationFinalizeRequest request) => new string?[] { request.ApplicationMemberSignature, request.PrivacyMemberSignature, request.ApplicationSecondarySignature, request.PrivacySecondarySignature, request.ApplicationRepresentativeSignature, request.PrivacyRepresentativeSignature, request.BoardSignature }.Where(value => value is not null);
    private sealed class TestContext
    {
        public TestContext(KommunikationsvorgangDto contract, Repository repository, Client client, Capture capture, PreparedMembershipApplicationSigningService service) { Contract = contract; Repository = repository; Client = client; Capture = capture; Service = service; }
        public KommunikationsvorgangDto Contract { get; set; } public Repository Repository { get; } public Client Client { get; } public Capture Capture { get; } public PreparedMembershipApplicationSigningService Service { get; }
    }

    private sealed class Capture : IPreparedSignatureCapture
    {
        public List<string> Calls { get; } = []; public int? CancelAt { get; set; }
        public Task<DigitalSignatureCapture?> CaptureAsync(SignatureStep step, CancellationToken cancellationToken = default) { if (CancelAt == Calls.Count) return Task.FromResult<DigitalSignatureCapture?>(null); Calls.Add(step.Key); return Task.FromResult<DigitalSignatureCapture?>(new DigitalSignatureCapture { CanvasWidth = 20, CanvasHeight = 10, Strokes = [new() { Points = [new() { X = 1, Y = 1 }, new() { X = 10, Y = 5 }] }] }); }
    }
    private sealed class Client : IContractGenerationClient
    {
        public MembershipApplicationFinalizeRequest? Request { get; private set; } public int FinalizeCalls { get; private set; } public bool FailFinalize { get; set; } public int? DocumentId { get; set; } = 5;
        public Task<byte[]> PreviewMembershipApplicationAsync(MitgliedsantragDokumentRequest request, decimal admissionFee, CancellationToken cancellationToken = default) => Task.FromResult(Array.Empty<byte>());
        public Task<byte[]> PreviewLeaseContractAsync(PachtvertragDokumentRequest request, CancellationToken cancellationToken = default) => Task.FromResult(Array.Empty<byte>());
        public Task<ContractFinalizeResult> FinalizeMembershipApplicationAsync(MembershipApplicationFinalizeRequest request, CancellationToken cancellationToken = default) { FinalizeCalls++; Request = request; if (FailFinalize) throw new InvalidOperationException("finalize"); return Task.FromResult(new ContractFinalizeResult(DocumentId, "ok")); }
        public Task<ContractFinalizeResult> FinalizeLeaseContractAsync(LeaseContractFinalizeRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
    private sealed class Repository : IPreparedContractRepository
    {
        public int StartCalls { get; private set; } public int CompleteCalls { get; private set; } public int MarkCalls { get; private set; } public int UpdateDraftCalls { get; private set; } public bool FailMark { get; set; } public KommunikationsvorgangDto? Started { get; private set; } public KommunikationsvorgangDto? ToStart { get; set; }
        public Task<KommunikationsvorgangDto> StartSignatureAsync(long id, int expectedRevision, CancellationToken cancellationToken = default) { StartCalls++; Started = ToStart! with { Id = id, Status = PreparedContractConstants.ReadyForSignature, Revision = expectedRevision, SignaturRevision = expectedRevision, SignaturGestartetAm = DateTimeOffset.UtcNow }; return Task.FromResult(Started); }
        public Task<KommunikationsvorgangDto> MarkFinalDocumentReadyForReviewAsync(long id, int expectedRevision, long finalDocumentId, CancellationToken cancellationToken = default) { MarkCalls++; if (FailMark) throw new InvalidOperationException("mark"); return Task.FromResult(Started! with { Status = PreparedContractConstants.FinalReview, FinalDokumentId = finalDocumentId }); }
        public Task<KommunikationsvorgangDto> CompleteAsync(long id, CancellationToken cancellationToken = default) { CompleteCalls++; return Task.FromResult(Started! with { Status = PreparedContractConstants.Completed }); }
        public Task<KommunikationsvorgangDto> LoadAsync(long id, CancellationToken cancellationToken = default) => throw new NotSupportedException(); public Task<KommunikationsvorgangDto?> FindActiveAsync(string documentType, int memberId, int? parcelId, CancellationToken cancellationToken = default) => throw new NotSupportedException(); public Task<KommunikationsvorgangDto> CreateAsync(string documentType, int memberId, int? parcelId, JsonElement draft, CancellationToken cancellationToken = default) => throw new NotSupportedException(); public Task<KommunikationsvorgangDto> ClaimAsync(long id, CancellationToken cancellationToken = default) => throw new NotSupportedException(); public Task ReleaseAsync(long id, CancellationToken cancellationToken = default) => throw new NotSupportedException(); public Task<KommunikationsvorgangDto> UpdateDraftAsync(long id, int expectedRevision, JsonElement draft, CancellationToken cancellationToken = default) { UpdateDraftCalls++; throw new InvalidOperationException(); }
    }
}
