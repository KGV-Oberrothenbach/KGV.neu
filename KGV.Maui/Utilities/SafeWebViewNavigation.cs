using System;

namespace KGV.Maui.Utilities;

public static class SafeWebViewNavigation
{
    public static bool IsInternal(Uri? uri) => uri is null || uri.Scheme.Equals("about", StringComparison.OrdinalIgnoreCase);
    public static bool IsAllowedExternal(Uri uri) => uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
        || uri.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
        || uri.Scheme.Equals("mailto", StringComparison.OrdinalIgnoreCase)
        || uri.Scheme.Equals("tel", StringComparison.OrdinalIgnoreCase);
}
