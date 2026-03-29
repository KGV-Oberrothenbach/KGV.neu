using System.Diagnostics;
using System.IO;
using System.Linq;

namespace KGV.ReleaseManager.Services;

public sealed class GitCommandService
{
    private static readonly string GitExecutablePath = ResolveGitExecutablePath();

    public ProcessStartInfo CreateVersionCommand()
    {
        return new ProcessStartInfo
        {
            FileName = GitExecutablePath,
            Arguments = "--version",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
    }

    public ProcessStartInfo CreateStatusCommand(string repositoryPath)
    {
        return new ProcessStartInfo
        {
            FileName = GitExecutablePath,
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

    public ProcessStartInfo CreateRevParseWorkTreeCommand(string repositoryPath)
    {
        return CreateGitCommand(repositoryPath, "rev-parse --is-inside-work-tree");
    }

    public ProcessStartInfo CreateRemoteOriginCommand(string repositoryPath)
    {
        return CreateGitCommand(repositoryPath, "remote get-url origin");
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
            FileName = GitExecutablePath,
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

    private static string ResolveGitExecutablePath()
    {
        var candidates = new[]
        {
            @"C:\Program Files\Microsoft Visual Studio\18\Community\Common7\IDE\CommonExtensions\Microsoft\TeamFoundation\Team Explorer\Git\cmd\git.exe",
            @"C:\Program Files\Microsoft Visual Studio\2022\Community\Common7\IDE\CommonExtensions\Microsoft\TeamFoundation\Team Explorer\Git\cmd\git.exe",
            @"C:\Program Files\Git\cmd\git.exe"
        };

        return candidates.FirstOrDefault(File.Exists) ?? "git";
    }
}
