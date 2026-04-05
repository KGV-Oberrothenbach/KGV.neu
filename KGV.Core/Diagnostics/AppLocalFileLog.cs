using System;
using System.IO;
using System.Text;

namespace KGV.Core.Diagnostics
{
    public static class AppLocalFileLog
    {
        private static readonly object SyncRoot = new();
        private static string? _logFilePath;

        public static string LogFilePath => EnsureInitialized();

        public static void Initialize(string? preferredDirectory = null)
        {
            lock (SyncRoot)
            {
                if (!string.IsNullOrWhiteSpace(_logFilePath))
                    return;

                _logFilePath = CreateLogFilePath(preferredDirectory);
            }
        }

        public static void Info(string category, string message)
            => Write("INFO", category, message);

        public static void Warning(string category, string message)
            => Write("WARN", category, message);

        public static void Error(string category, string message, Exception? exception = null)
            => Write("ERROR", category, message, exception);

        private static string EnsureInitialized()
        {
            Initialize();
            return _logFilePath!;
        }

        private static void Write(string level, string category, string message, Exception? exception = null)
        {
            try
            {
                var path = EnsureInitialized();
                var directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrWhiteSpace(directory))
                    Directory.CreateDirectory(directory);

                var builder = new StringBuilder();
                builder.Append(DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm:ss.fff zzz"));
                builder.Append(" [");
                builder.Append(level);
                builder.Append("] ");
                builder.Append(category);
                builder.Append(": ");
                builder.AppendLine(message ?? string.Empty);

                if (exception != null)
                    AppendException(builder, exception, 0);

                lock (SyncRoot)
                {
                    File.AppendAllText(path, builder.ToString());
                }
            }
            catch
            {
            }
        }

        private static void AppendException(StringBuilder builder, Exception exception, int depth)
        {
            builder.Append("ExceptionDepth=");
            builder.Append(depth);
            builder.Append(" Type=");
            builder.Append(exception.GetType().FullName);
            builder.AppendLine();
            builder.AppendLine("Message=" + (exception.Message ?? string.Empty));
            builder.AppendLine("StackTrace=");
            builder.AppendLine(exception.StackTrace ?? string.Empty);

            if (exception.InnerException != null)
                AppendException(builder, exception.InnerException, depth + 1);
        }

        private static string CreateLogFilePath(string? preferredDirectory)
        {
            foreach (var directory in GetCandidateDirectories(preferredDirectory))
            {
                try
                {
                    Directory.CreateDirectory(directory);
                    return Path.Combine(directory, $"wpf-permissiondiag-{DateTime.Now:yyyyMMdd-HHmmssfff}.log");
                }
                catch
                {
                }
            }

            var fallback = Path.Combine(Path.GetTempPath(), "KGV", "_logs");
            Directory.CreateDirectory(fallback);
            return Path.Combine(fallback, $"wpf-permissiondiag-{DateTime.Now:yyyyMMdd-HHmmssfff}.log");
        }

        private static string[] GetCandidateDirectories(string? preferredDirectory)
        {
            var candidates = new System.Collections.Generic.List<string>();

            if (!string.IsNullOrWhiteSpace(preferredDirectory))
                candidates.Add(preferredDirectory);

            var workspaceRoot = FindWorkspaceRoot();
            if (!string.IsNullOrWhiteSpace(workspaceRoot))
                candidates.Add(Path.Combine(workspaceRoot, "_logs"));

            candidates.Add(Path.Combine(Directory.GetCurrentDirectory(), "_logs"));
            candidates.Add(Path.Combine(AppContext.BaseDirectory, "_logs"));
            candidates.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "KGV", "_logs"));

            return candidates.ToArray();
        }

        private static string? FindWorkspaceRoot()
        {
            foreach (var startDirectory in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
            {
                if (string.IsNullOrWhiteSpace(startDirectory))
                    continue;

                var current = new DirectoryInfo(startDirectory);
                while (current != null)
                {
                    if (Directory.Exists(Path.Combine(current.FullName, ".git")) || File.Exists(Path.Combine(current.FullName, "KGV.slnx")))
                        return current.FullName;

                    current = current.Parent;
                }
            }

            return null;
        }
    }
}
