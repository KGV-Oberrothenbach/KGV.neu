using System;

namespace KGV.Core.Models;

public sealed class OtpFailureDiagnosticInfo
{
    public string Code { get; init; } = string.Empty;
    public string UserMessage { get; init; } = string.Empty;
    public DateTimeOffset Timestamp { get; init; }
}
