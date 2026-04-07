using Android.Util;
using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.Maui.Storage;

namespace KGV.Maui.Services.Diagnostics;

internal static partial class AppFileLog
{
    private const string AndroidLogTag = "KGV";
    private const long MaxLogFileBytes = 512 * 1024;
    private static readonly object SyncRoot = new();
    private static string? _logFilePath;
    private static string? _externalLogFilePath;
    private static bool _externalLogPathResolved;
    private static bool _androidLogUnavailableReported;
    private static bool _externalLogUnavailableReported;

    public static string LogFilePath
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(_logFilePath))
            {
                return _logFilePath;
            }

            lock (SyncRoot)
            {
                _logFilePath ??= Path.Combine(GetAppDataDirectory(), "kgv-release.log");
                return _logFilePath;
            }
        }
    }

    public static string? ExternalLogFilePath
    {
        get
        {
            if (_externalLogPathResolved)
            {
                return _externalLogFilePath;
            }

            lock (SyncRoot)
            {
                if (_externalLogPathResolved)
                {
                    return _externalLogFilePath;
                }

                _externalLogFilePath = ResolveExternalLogFilePath();
                _externalLogPathResolved = true;
                return _externalLogFilePath;
            }
        }
    }

    public static void Info(string category, string message) => Write("INFO", category, message);

    public static void Warning(string category, string message) => Write("WARN", category, message);

    public static void Error(string category, string message, Exception? exception = null) => Write("ERROR", category, message, exception);

    public static void ErrorDetailed(string category, string message, Exception exception)
    {
        Write("ERROR", category, message, exception);
        WriteExceptionDetails(category, exception);
        WriteFullExceptionToFallbackOutputs(category, exception);
    }

    public static void Marker(string marker) => Write("INFO", "MARKER", marker);

    public static void Write(string level, string category, string message, Exception? exception = null)
    {
        try
        {
            var logFilePath = LogFilePath;
            var externalLogFilePath = ExternalLogFilePath;

            TrimLogFileIfNeeded(logFilePath);
            if (!string.IsNullOrWhiteSpace(externalLogFilePath))
            {
                TrimLogFileIfNeeded(externalLogFilePath);
            }

            var sanitizedMessage = Sanitize(message);
            var line = $"{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss.fff zzz} [{level}] {category}: {sanitizedMessage}";
            if (exception is not null)
            {
                line += $" | {exception.GetType().Name}: {Sanitize(exception.Message)}";
            }

            WriteLineToPersistentOutputs(logFilePath, externalLogFilePath, line);
            TryWriteToAndroidLog(level, $"{category}: {sanitizedMessage}", exception, logFilePath, externalLogFilePath);
        }
        catch
        {
        }
    }

    private static void WriteLineToPersistentOutputs(string logFilePath, string? externalLogFilePath, string line)
    {
        string? externalLogFailureLine = null;

        lock (SyncRoot)
        {
            EnsureDirectoryExists(logFilePath);
            File.AppendAllText(logFilePath, line + Environment.NewLine);

            if (!string.IsNullOrWhiteSpace(externalLogFilePath))
            {
                try
                {
                    EnsureDirectoryExists(externalLogFilePath);
                    File.AppendAllText(externalLogFilePath, line + Environment.NewLine);
                }
                catch (Exception ex)
                {
                    if (!_externalLogUnavailableReported)
                    {
                        _externalLogUnavailableReported = true;
                        externalLogFailureLine = $"{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss.fff zzz} [WARN] APPLOG: Extern lesbares Spiegel-Log konnte nicht geschrieben werden. Internes File-/stderr-Logging bleibt aktiv. {ex.GetType().Name}: {Sanitize(ex.Message)}";
                        File.AppendAllText(logFilePath, externalLogFailureLine + Environment.NewLine);
                    }
                }
            }
        }

        WriteToDebugAndError(line);

        if (!string.IsNullOrWhiteSpace(externalLogFailureLine))
        {
            WriteToDebugAndError(externalLogFailureLine);
        }
    }

    private static void WriteExceptionDetails(string category, Exception exception)
    {
        try
        {
            var lines = exception.ToString()
                .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

            for (var index = 0; index < lines.Length; index++)
            {
                Write("ERROR", $"{category}.Exception", $"[{index + 1}] {lines[index]}");
            }
        }
        catch
        {
        }
    }

    private static void WriteFullExceptionToFallbackOutputs(string category, Exception exception)
    {
        try
        {
            var logFilePath = LogFilePath;
            var externalLogFilePath = ExternalLogFilePath;
            var begin = $"{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss.fff zzz} [ERROR] {category}.Exception.Full: BEGIN";
            var end = $"{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss.fff zzz} [ERROR] {category}.Exception.Full: END";

            WriteLineToPersistentOutputs(logFilePath, externalLogFilePath, begin);

            lock (SyncRoot)
            {
                EnsureDirectoryExists(logFilePath);
                File.AppendAllText(logFilePath, exception + Environment.NewLine);

                if (!string.IsNullOrWhiteSpace(externalLogFilePath))
                {
                    try
                    {
                        EnsureDirectoryExists(externalLogFilePath);
                        File.AppendAllText(externalLogFilePath, exception + Environment.NewLine);
                    }
                    catch
                    {
                    }
                }
            }

            WriteToDebugAndError(exception.ToString());

            WriteLineToPersistentOutputs(logFilePath, externalLogFilePath, end);
        }
        catch
        {
        }
    }

    private static void TrimLogFileIfNeeded(string logFilePath)
    {
        lock (SyncRoot)
        {
            EnsureDirectoryExists(logFilePath);

            if (!File.Exists(logFilePath))
            {
                return;
            }

            var fileInfo = new FileInfo(logFilePath);
            if (fileInfo.Length <= MaxLogFileBytes)
            {
                return;
            }

            File.WriteAllText(logFilePath, string.Empty);
        }
    }

    private static void EnsureDirectoryExists(string logFilePath)
    {
        var directory = Path.GetDirectoryName(logFilePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }

    private static string GetAppDataDirectory()
    {
        try
        {
            var appDataDirectory = FileSystem.Current.AppDataDirectory;
            if (!string.IsNullOrWhiteSpace(appDataDirectory))
            {
                return appDataDirectory;
            }
        }
        catch
        {
        }

        return Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
    }

    private static string? ResolveExternalLogFilePath()
    {
        try
        {
            var externalDirectory = Android.App.Application.Context?.GetExternalFilesDir(null)?.AbsolutePath;
            if (!string.IsNullOrWhiteSpace(externalDirectory))
            {
                return Path.Combine(externalDirectory, "diagnostics", "kgv-release.log");
            }
        }
        catch
        {
        }

        return null;
    }

    private static void TryWriteToAndroidLog(string level, string message, Exception? exception, string logFilePath, string? externalLogFilePath)
    {
        try
        {
            var logMessage = exception is null
                ? message
                : $"{message} | {exception.GetType().Name}: {Sanitize(exception.Message)}";

            switch (level)
            {
                case "ERROR":
                    Log.Error(AndroidLogTag, logMessage);
                    break;
                case "WARN":
                    Log.Warn(AndroidLogTag, logMessage);
                    break;
                default:
                    Log.Info(AndroidLogTag, logMessage);
                    break;
            }
        }
        catch (Exception ex)
        {
            if (_androidLogUnavailableReported)
            {
                return;
            }

            _androidLogUnavailableReported = true;
            var fallbackLine = $"{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss.fff zzz} [WARN] APPLOG: Android liblog bridge unavailable. File-/stderr-logging remains active. {ex.GetType().Name}: {Sanitize(ex.Message)}";
            WriteLineToPersistentOutputs(logFilePath, externalLogFilePath, fallbackLine);
        }
    }

    private static void WriteToDebugAndError(string line)
    {
        try
        {
            Debug.WriteLine(line);
        }
        catch
        {
        }

        try
        {
            Console.Error.WriteLine(line);
        }
        catch
        {
        }
    }

    private static string Sanitize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var sanitized = value.Replace("\r", " ").Replace("\n", " ").Trim();
        sanitized = PublishableKeyRegex().Replace(sanitized, "sb_publishable_***");
        return AccessTokenRegex().Replace(sanitized, "$1***");
    }

    [GeneratedRegex(@"sb_publishable_[A-Za-z0-9_\-\.]+", RegexOptions.Compiled)]
    private static partial Regex PublishableKeyRegex();

    [GeneratedRegex(@"(access_token=)[^\s&]+", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex AccessTokenRegex();
}
