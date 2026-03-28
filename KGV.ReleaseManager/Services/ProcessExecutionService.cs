using System.Diagnostics;
using System.Text;
using KGV.ReleaseManager.Models;

namespace KGV.ReleaseManager.Services;

public sealed class ProcessExecutionService
{
    public async Task<ProcessExecutionResult> RunAsync(ProcessStartInfo startInfo, string stepName, CancellationToken cancellationToken = default)
    {
        try
        {
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;
            startInfo.CreateNoWindow = true;

            using var process = new Process { StartInfo = startInfo };
            var standardOutput = new StringBuilder();
            var standardError = new StringBuilder();

            process.OutputDataReceived += (_, args) =>
            {
                if (args.Data is not null)
                {
                    standardOutput.AppendLine(args.Data);
                }
            };

            process.ErrorDataReceived += (_, args) =>
            {
                if (args.Data is not null)
                {
                    standardError.AppendLine(args.Data);
                }
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            await process.WaitForExitAsync(cancellationToken);

            return new ProcessExecutionResult
            {
                StepName = stepName,
                ExitCode = process.ExitCode,
                StandardOutput = standardOutput.ToString().Trim(),
                StandardError = standardError.ToString().Trim()
            };
        }
        catch (Exception ex)
        {
            return new ProcessExecutionResult
            {
                StepName = stepName,
                ExitCode = -1,
                ExceptionMessage = ex.Message
            };
        }
    }
}
