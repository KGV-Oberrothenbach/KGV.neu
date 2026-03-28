using System.Diagnostics;
using System.IO;
using System.Text;

namespace KGV.ReleaseManager.Services;

public sealed class BuildCommandService
{
    private const string AndroidSigningStorePasswordProperty = "KGV_ANDROID_SIGNING_STORE_PASS";
    private const string AndroidSigningKeyPasswordProperty = "KGV_ANDROID_SIGNING_KEY_PASS";

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
        string packageName,
        string keystorePath,
        string keyAlias,
        string storePassword,
        string keyPassword)
    {
        var arguments = new StringBuilder();
        arguments.Append($"publish \"{projectPath}\" -c Release -f net9.0-android");
        arguments.Append($" /p:AndroidPackageFormat={packageFormat}");
        arguments.Append(" /p:AndroidCreatePackagePerAbi=false");
        arguments.Append(" /p:AndroidKeyStore=true");
        if (!string.IsNullOrWhiteSpace(packageName))
        {
            arguments.Append($" /p:ApplicationId=\"{packageName}\"");
        }

        arguments.Append($" /p:AndroidSigningKeyStore=\"{keystorePath}\"");
        arguments.Append($" /p:AndroidSigningStorePass=$({AndroidSigningStorePasswordProperty})");
        arguments.Append($" /p:AndroidSigningKeyAlias=\"{keyAlias}\"");
        arguments.Append($" /p:AndroidSigningKeyPass=$({AndroidSigningKeyPasswordProperty})");

        var startInfo = CreateDotnetCommand(projectPath, arguments.ToString());
        startInfo.Environment[AndroidSigningStorePasswordProperty] = storePassword;
        startInfo.Environment[AndroidSigningKeyPasswordProperty] = keyPassword;
        return startInfo;
    }

    public ProcessStartInfo CreateInnoCompileCommand(
        string compilerPath,
        string scriptPath,
        string outputDirectory,
        string outputBaseFileName,
        string appVersion)
    {
        var versionDefine = string.IsNullOrWhiteSpace(appVersion)
            ? string.Empty
            : $" /DAppVersion=\"{appVersion}\"";

        return new ProcessStartInfo
        {
            FileName = compilerPath,
            Arguments = $"\"{scriptPath}\" /O\"{outputDirectory}\" /F\"{outputBaseFileName}\"{versionDefine}",
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
