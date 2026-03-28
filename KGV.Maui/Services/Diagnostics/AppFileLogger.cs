using System;
using Microsoft.Extensions.Logging;

namespace KGV.Maui.Services.Diagnostics;

internal sealed class AppFileLogger : ILogger
{
    private readonly string _categoryName;

    public AppFileLogger(string categoryName)
    {
        _categoryName = categoryName;
    }

    public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

    public bool IsEnabled(LogLevel logLevel)
    {
        if (logLevel < LogLevel.Information)
        {
            return false;
        }

        return _categoryName.StartsWith("KGV.", StringComparison.Ordinal);
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
        {
            return;
        }

        var message = formatter(state, exception);
        if (string.IsNullOrWhiteSpace(message) && exception is null)
        {
            return;
        }

        AppFileLog.Write(logLevel.ToString().ToUpperInvariant(), _categoryName, message, exception);
    }

    private sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();

        public void Dispose()
        {
        }
    }
}
