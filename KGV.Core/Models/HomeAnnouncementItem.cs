namespace KGV.Core.Models;

public sealed class HomeAnnouncementItem
{
    public int Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Subtitle { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public string HtmlContent { get; init; } = string.Empty;
    public string DetailInfo { get; init; } = string.Empty;
}