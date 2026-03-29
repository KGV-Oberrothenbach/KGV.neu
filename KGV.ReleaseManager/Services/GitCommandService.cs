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

    public ProcessStartInfo CreatePorcelainStatusCommand(string repositoryPath)
    {
        return CreateGitCommand(repositoryPath, "status --porcelain");
    }

    public ProcessStartInfo CreateAddAllCommand(string repositoryPath)
    {
        return CreateGitCommand(repositoryPath, "add -A");
    }

    public ProcessStartInfo CreateCommitCommand(string repositoryPath, string commitMessage)
    {
        return CreateGitCommand(repositoryPath, $"commit -m \"{EscapeArgument(commitMessage)}\"");
    }

    public ProcessStartInfo CreatePushCommand(string repositoryPath)
    {
        return CreateGitCommand(repositoryPath, "push");
    }

    public string CreateReleaseCommitMessage(string version, string scope)
    {
        var normalizedScope = string.IsNullOrWhiteSpace(scope) ? "Release" : scope.Trim();
        return $"Release {version}: {normalizedScope}";
    }

    private static ProcessStartInfo CreateGitCommand(string repositoryPath, string arguments)
    {
        return new ProcessStartInfo
        {
            FileName = "git",
            Arguments = $"-C \"{repositoryPath}\" {arguments}",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
    }

    private static string EscapeArgument(string value)
        => (value ?? string.Empty)
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal);
}
