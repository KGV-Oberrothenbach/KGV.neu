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
    private static bool _androidLogUnavailableReported;

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
            var directory = Path.GetDirectoryName(logFilePath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            TrimLogFileIfNeeded(logFilePath);

            var sanitizedMessage = Sanitize(message);
            var line = $"{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss.fff zzz} [{level}] {category}: {sanitizedMessage}";
            if (exception is not null)
            {
                line += $" | {exception.GetType().Name}: {Sanitize(exception.Message)}";
            }

            WriteLineToPersistentOutputs(logFilePath, line);
            TryWriteToAndroidLog(level, $"{category}: {sanitizedMessage}", exception, logFilePath);
        }
        catch
        {
        }
    }

    private static void WriteLineToPersistentOutputs(string logFilePath, string line)
    {
        lock (SyncRoot)
        {
            File.AppendAllText(logFilePath, line + Environment.NewLine);
        }

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
            var begin = $"{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss.fff zzz} [ERROR] {category}.Exception.Full: BEGIN";
            var end = $"{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss.fff zzz} [ERROR] {category}.Exception.Full: END";

            WriteLineToPersistentOutputs(logFilePath, begin);

            lock (SyncRoot)
            {
                File.AppendAllText(logFilePath, exception + Environment.NewLine);
            }

            try
            {
                Debug.WriteLine(exception.ToString());
            }
            catch
            {
            }

            try
            {
                Console.Error.WriteLine(exception.ToString());
            }
            catch
            {
            }

            WriteLineToPersistentOutputs(logFilePath, end);
        }
        catch
        {
        }
    }

    private static void TrimLogFileIfNeeded(string logFilePath)
    {
        lock (SyncRoot)
        {
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

    private static void TryWriteToAndroidLog(string level, string message, Exception? exception, string logFilePath)
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
            WriteLineToPersistentOutputs(logFilePath, fallbackLine);
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
