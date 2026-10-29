namespace KGV.Infrastructure.Supabase;

/// <summary>Normalizes credentials before use in HTTP headers without exposing them.</summary>
internal static class SupabaseHeaderValue
{
    public static string NormalizeRequired(string? value, string valueName)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
            throw new InvalidOperationException($"{valueName} fehlt.");
        if (normalized.IndexOfAny(['\r', '\n', '\0']) >= 0)
            throw new InvalidOperationException($"{valueName} enthält unzulässige Steuerzeichen.");
        return normalized;
    }

    public static string? NormalizeOptional(string? value, string valueName)
        => string.IsNullOrWhiteSpace(value) ? null : NormalizeRequired(value, valueName);
}
