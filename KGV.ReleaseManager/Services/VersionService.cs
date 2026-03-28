using System;
using System.Linq;

namespace KGV.ReleaseManager.Services;

public sealed class VersionService
{
    public string IncrementPatch(string version)
    {
        if (string.IsNullOrWhiteSpace(version))
        {
            return "0.1.0";
        }

        var parts = version.Split('.');
        if (parts.Length != 3 || parts.Any(p => !int.TryParse(p, out _)))
        {
            throw new InvalidOperationException($"Ungültige Versionsnummer: {version}");
        }

        var major = int.Parse(parts[0]);
        var minor = int.Parse(parts[1]);
        var patch = int.Parse(parts[2]) + 1;

        return $"{major}.{minor}.{patch}";
    }
}
