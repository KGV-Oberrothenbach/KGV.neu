using System.Diagnostics;
using System.IO;
using System.Text;

namespace KGV.ReleaseManager.Services;

public sealed class BuildCommandService
{
    public ProcessStartInfo CreateDotnetBuildCommand(string projectPath, string configuration)
    {
        return CreateDotnetCommand(projectPath, $"build \"{projectPath}\" -c {configuration}");
    }

    public ProcessStartInfo CreateDotnetPublishCommand(string projectPath, string configuration)
    {
        return CreateDotnetCommand(projectPath, $"publish \"{projectPath}\" -c {configuration}");
    }

    public ProcessStartInfo CreateAndroidPublishCommand(
        string projectPath,
        string packageFormat,
        string keystorePath,
        string keyAlias,
        string storePassword,
        string keyPassword)
    {
        var arguments = new StringBuilder();
        arguments.Append($"publish \"{projectPath}\" -c Release -f net9.0-android");
        arguments.Append($" /p:AndroidPackageFormat={packageFormat}");
        arguments.Append(" /p:AndroidKeyStore=true");
        arguments.Append($" /p:AndroidSigningKeyStore=\"{keystorePath}\"");
        arguments.Append($" /p:AndroidSigningStorePass=\"{storePassword}\"");
        arguments.Append($" /p:AndroidSigningKeyAlias=\"{keyAlias}\"");
        arguments.Append($" /p:AndroidSigningKeyPass=\"{keyPassword}\"");

        return CreateDotnetCommand(projectPath, arguments.ToString());
    }

    public ProcessStartInfo CreateInnoCompileCommand(string compilerPath, string scriptPath, string outputDirectory, string outputBaseFileName)
    {
        return new ProcessStartInfo
        {
            FileName = compilerPath,
            Arguments = $"\"{scriptPath}\" /O\"{outputDirectory}\" /F\"{outputBaseFileName}\"",
            WorkingDirectory = Path.GetDirectoryName(scriptPath) ?? string.Empty,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
    }

    private static ProcessStartInfo CreateDotnetCommand(string projectPath, string arguments)
    {
        return new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = arguments,
            WorkingDirectory = Path.GetDirectoryName(projectPath) ?? string.Empty,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
    }
}
