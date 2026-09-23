namespace KGV.Core.Models;

public sealed record Vereinskontext(
    Guid VereinId,
    string VereinsCode,
    string Vereinsname,
    string? Kurzname,
    string SupabaseUrl,
    string SupabasePublishableKey);
