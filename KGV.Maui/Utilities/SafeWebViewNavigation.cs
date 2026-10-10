using System;

namespace KGV.Maui.Utilities;

public static class SafeWebViewNavigation
{
    public static bool IsInternal(string? url) => string.IsNullOrWhiteSpace(url)
        || url.Equals("about:blank", StringComparison.OrdinalIgnoreCase)
        || url.StartsWith("about:blank#", StringComparison.OrdinalIgnoreCase)
        || url.StartsWith('#');

    public static bool TryGetAllowedExternal(string? url, out Uri? uri)
    {
        uri = null;
        if (!Uri.TryCreate(url, UriKind.Absolute, out var candidate)) return false;
        if (!IsAllowedExternal(candidate)) return false;
        uri = candidate;
        return true;
    }

    private static bool IsAllowedExternal(Uri uri) => uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
        || uri.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
        || uri.Scheme.Equals("mailto", StringComparison.OrdinalIgnoreCase)
        || uri.Scheme.Equals("tel", StringComparison.OrdinalIgnoreCase);
}
