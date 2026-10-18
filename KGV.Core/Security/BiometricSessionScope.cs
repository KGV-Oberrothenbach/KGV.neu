using System.Security.Cryptography;
using System.Text;

namespace KGV.Core.Security;

/// <summary>Stable, non-secret key that binds a locally protected session to one club and Supabase environment.</summary>
public static class BiometricSessionScope
{
    public static string? Create(Guid clubId, string? supabaseUrl)
    {
        if (clubId == Guid.Empty || string.IsNullOrWhiteSpace(supabaseUrl)) return null;
        var normalizedUrl = supabaseUrl.Trim().TrimEnd('/').ToLowerInvariant();
        var environment = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(normalizedUrl)));
        return $"{clubId}:{environment}";
    }
}
