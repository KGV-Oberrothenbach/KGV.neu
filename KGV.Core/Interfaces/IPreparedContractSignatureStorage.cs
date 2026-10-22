namespace KGV.Core.Interfaces;

public interface IPreparedContractSignatureStorage
{
    Task<PreparedContractSignatureStorageUploadResult> UploadAsync(PreparedContractSignatureStorageUploadRequest request, CancellationToken cancellationToken = default);
}

public sealed record PreparedContractSignatureStorageUploadRequest(
    long KommunikationsvorgangId,
    int SignaturRevision,
    string SignaturSchluessel,
    ReadOnlyMemory<byte> PngBytes);

public sealed record PreparedContractSignatureStorageUploadResult(string StoragePath, string Sha256);
