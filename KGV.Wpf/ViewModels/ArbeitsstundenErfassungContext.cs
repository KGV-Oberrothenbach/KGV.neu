using KGV.Core.Models;

namespace KGV.ViewModels
{
    public sealed class ArbeitsstundenErfassungContext
    {
        public ArbeitsstundeDTO? ExistingEntry { get; init; }
        public bool IsAdminEditMode { get; init; }
        public bool OpenAsDialog { get; init; }
    }
}
