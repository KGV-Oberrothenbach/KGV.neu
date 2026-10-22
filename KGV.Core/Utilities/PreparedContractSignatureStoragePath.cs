using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace KGV.Core.Utilities;

public static partial class PreparedContractSignatureStoragePath
{
    public const string Bucket = "prepared-contract-signatures";

    public static string Create(long kommunikationsvorgangId, int signaturRevision, string signaturSchluessel, Guid? objectId = null)
    {
        if (kommunikationsvorgangId <= 0) throw new ArgumentOutOfRangeException(nameof(kommunikationsvorgangId));
        if (signaturRevision < 1) throw new ArgumentOutOfRangeException(nameof(signaturRevision));
        if (string.IsNullOrWhiteSpace(signaturSchluessel) || !SignatureKey().IsMatch(signaturSchluessel)) throw new ArgumentException("Der Signaturschlüssel ist kein sicherer Storage-Pfadbestandteil.", nameof(signaturSchluessel));
        return $"{kommunikationsvorgangId}/{signaturRevision}/{signaturSchluessel}/{objectId ?? Guid.NewGuid():D}.png";
    }

    public static string ComputeSha256(ReadOnlySpan<byte> bytes) => Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();

    [GeneratedRegex("^[A-Za-z0-9._-]+$")]
    private static partial Regex SignatureKey();
}
