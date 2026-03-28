namespace KGV.ViewModels
{
    public sealed class HomeSectionDetailContext
    {
        public int WorkAssignmentId { get; init; }
        public bool IsWorkAssignment { get; init; }
        public string SectionTitle { get; init; } = string.Empty;
        public string Title { get; init; } = string.Empty;
        public string Subtitle { get; init; } = string.Empty;
        public string StartTimeText { get; init; } = string.Empty;
        public string EndTimeText { get; init; } = string.Empty;
        public string Content { get; init; } = string.Empty;
        public string HtmlContent { get; init; } = string.Empty;
        public string AdditionalInfo { get; init; } = string.Empty;
        public string RegistrationInfo { get; init; } = string.Empty;
        public bool ShowRegisterButton { get; init; }
    }
}
