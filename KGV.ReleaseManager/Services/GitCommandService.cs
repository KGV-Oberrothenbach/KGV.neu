using System.Diagnostics;

namespace KGV.ReleaseManager.Services;

public sealed class GitCommandService
{
    public ProcessStartInfo CreateStatusCommand(string repositoryPath)
    {
        return new ProcessStartInfo
        {
            FileName = "git",
            Arguments = $"-C \"{repositoryPath}\" status -sb",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
    }

    // TODO: Commit, Push, Tag und Zielrepo-Operationen ergänzen.
}
