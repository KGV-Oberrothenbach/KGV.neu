using KGV.Core.Models;

namespace KGV.ViewModels
{
    public sealed class ArbeitsstundenNavigationContext
    {
        public required MemberDTO Member { get; init; }
        public bool ReviewMode { get; init; }
        public bool ShowOnlyPending { get; init; }
        public bool IncludeNebenmitglied { get; init; } = true;
    }
}
