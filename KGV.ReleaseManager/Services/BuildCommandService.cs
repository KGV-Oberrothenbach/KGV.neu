using System.Diagnostics;

namespace KGV.ReleaseManager.Services;

public sealed class BuildCommandService
{
    public ProcessStartInfo CreateDotnetPublishCommand(string projectPath, string configuration)
    {
        return new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"publish \"{projectPath}\" -c {configuration}",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
    }

    // TODO: Inno Setup, APK, AAB und Signierung ergänzen.
}
