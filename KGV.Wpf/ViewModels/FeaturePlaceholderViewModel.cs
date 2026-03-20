namespace KGV.ViewModels
{
    public class FeaturePlaceholderViewModel : BaseViewModel
    {
        public string Title { get; }
        public string Description { get; }
        public string RecoveryHint { get; }

        protected FeaturePlaceholderViewModel(string title, string description, string recoveryHint)
        {
            Title = title;
            Description = description;
            RecoveryHint = recoveryHint;
        }
    }
}
