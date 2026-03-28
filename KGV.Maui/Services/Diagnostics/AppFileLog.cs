using Android.Util;
using System;
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

            WriteToAndroidLog(level, $"{category}: {sanitizedMessage}", exception);

            lock (SyncRoot)
            {
                File.AppendAllText(logFilePath, line + Environment.NewLine);
            }
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

    private static void WriteToAndroidLog(string level, string message, Exception? exception)
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
