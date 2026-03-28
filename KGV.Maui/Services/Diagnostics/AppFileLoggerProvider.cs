using Microsoft.Extensions.Logging;

namespace KGV.Maui.Services.Diagnostics;

internal sealed class AppFileLoggerProvider : ILoggerProvider
{
    public ILogger CreateLogger(string categoryName) => new AppFileLogger(categoryName);

    public void Dispose()
    {
    }
}
