namespace KGV.Core.Models;

/// <summary>
/// Identifies a functional area of the communication centre independently from
/// its route, navigation title, or localized display text.
/// </summary>
public enum CommunicationArea
{
    Signatures,
    Documents,
    Board,
    MailOutbox,
    Sent,
    Print,
    History
}

public static class CommunicationAreaRoutes
{
    public const string Signatures = "process_signatures";
    public const string Documents = "process_documents";
    public const string MailOutbox = "process_mail";
    public const string Sent = "process_sent";
    public const string Print = "process_print";
    public const string History = "process_history";

    public static bool TryGetArea(string route, out CommunicationArea area) => route switch
    {
        Signatures => Set(CommunicationArea.Signatures, out area),
        Documents => Set(CommunicationArea.Documents, out area),
        MailOutbox => Set(CommunicationArea.MailOutbox, out area),
        Sent => Set(CommunicationArea.Sent, out area),
        Print => Set(CommunicationArea.Print, out area),
        History => Set(CommunicationArea.History, out area),
        _ => Set(default, out area, false)
    };

    private static bool Set(CommunicationArea value, out CommunicationArea area, bool found = true)
    {
        area = value;
        return found;
    }
}
