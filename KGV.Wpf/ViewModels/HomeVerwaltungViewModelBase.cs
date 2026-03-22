using KGV.Core.Interfaces;
using KGV.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace KGV.ViewModels
{
    public abstract class HomeVerwaltungViewModelBase : BaseViewModel, INavigationAware
    {
        protected readonly ISupabaseService SupabaseService;

        protected HomeVerwaltungViewModelBase(ISupabaseService supabaseService)
        {
            SupabaseService = supabaseService ?? throw new ArgumentNullException(nameof(supabaseService));
            AktualisierenCommand = new RelayCommand<object?>(_ => _ = LoadAsync());
            NeuCommand = new RelayCommand<object?>(_ => OpenNewEditor());
            OeffnenCommand = new RelayCommand<object?>(_ => OpenSelectedEditor(), _ => SelectedEntry != null);
        }

        public ObservableCollection<HomeVerwaltungListItem> Entries { get; } = new();

        private HomeVerwaltungListItem? _selectedEntry;
        public HomeVerwaltungListItem? SelectedEntry
        {
            get => _selectedEntry;
            set
            {
                if (SetProperty(ref _selectedEntry, value))
                    OeffnenCommand.RaiseCanExecuteChanged();
            }
        }

        private bool _isEditorOpen;
        public bool IsEditorOpen
        {
            get => _isEditorOpen;
            private set => SetProperty(ref _isEditorOpen, value);
        }

        private bool _isNewMode;
        public bool IsNewMode
        {
            get => _isNewMode;
            private set => SetProperty(ref _isNewMode, value);
        }

        private string _editorCaption = string.Empty;
        public string EditorCaption
        {
            get => _editorCaption;
            private set => SetProperty(ref _editorCaption, value);
        }

        private string _editorTitle = string.Empty;
        public string EditorTitle
        {
            get => _editorTitle;
            private set => SetProperty(ref _editorTitle, value);
        }

        private string _editorSubtitle = string.Empty;
        public string EditorSubtitle
        {
            get => _editorSubtitle;
            private set => SetProperty(ref _editorSubtitle, value);
        }

        private string _editorContent = string.Empty;
        public string EditorContent
        {
            get => _editorContent;
            private set => SetProperty(ref _editorContent, value);
        }

        private string _editorAdditionalInfo = string.Empty;
        public string EditorAdditionalInfo
        {
            get => _editorAdditionalInfo;
            private set => SetProperty(ref _editorAdditionalInfo, value);
        }

        public bool HasEntries => Entries.Count > 0;
        public bool ShowEmptyState => !HasEntries;
        public bool HasEditorTitle => !string.IsNullOrWhiteSpace(EditorTitle);
        public bool HasEditorSubtitle => !string.IsNullOrWhiteSpace(EditorSubtitle);
        public bool HasEditorContent => !string.IsNullOrWhiteSpace(EditorContent);
        public bool HasEditorAdditionalInfo => !string.IsNullOrWhiteSpace(EditorAdditionalInfo);

        public RelayCommand<object?> AktualisierenCommand { get; }
        public RelayCommand<object?> NeuCommand { get; }
        public RelayCommand<object?> OeffnenCommand { get; }

        public abstract string Title { get; }
        public abstract string EmptyText { get; }
        public abstract string ReadPathText { get; }
        public abstract string WritePathText { get; }
        public abstract string NewCaption { get; }

        protected abstract Task<IReadOnlyList<HomeVerwaltungListItem>> LoadEntriesCoreAsync();

        public async Task OnNavigatedToAsync()
        {
            await LoadAsync();
        }

        public Task OnNavigatedFromAsync() => Task.CompletedTask;

        private async Task LoadAsync()
        {
            var items = await LoadEntriesCoreAsync();
            Entries.Clear();
            foreach (var item in items)
                Entries.Add(item);

            SelectedEntry = Entries.Count > 0 ? Entries[0] : null;
            CloseEditor();
            OnPropertyChanged(nameof(HasEntries));
            OnPropertyChanged(nameof(ShowEmptyState));
        }

        private void OpenSelectedEditor()
        {
            if (SelectedEntry == null)
                return;

            IsEditorOpen = true;
            IsNewMode = false;
            EditorCaption = $"Bearbeiten – {Title}";
            EditorTitle = SelectedEntry.Title;
            EditorSubtitle = SelectedEntry.Subtitle;
            EditorContent = SelectedEntry.Content;
            EditorAdditionalInfo = SelectedEntry.AdditionalInfo;
            RaiseEditorStateChanged();
        }

        private void OpenNewEditor()
        {
            IsEditorOpen = true;
            IsNewMode = true;
            EditorCaption = NewCaption;
            EditorTitle = string.Empty;
            EditorSubtitle = string.Empty;
            EditorContent = string.Empty;
            EditorAdditionalInfo = string.Empty;
            RaiseEditorStateChanged();
        }

        private void CloseEditor()
        {
            IsEditorOpen = false;
            IsNewMode = false;
            EditorCaption = string.Empty;
            EditorTitle = string.Empty;
            EditorSubtitle = string.Empty;
            EditorContent = string.Empty;
            EditorAdditionalInfo = string.Empty;
            RaiseEditorStateChanged();
        }

        private void RaiseEditorStateChanged()
        {
            OnPropertyChanged(nameof(HasEditorTitle));
            OnPropertyChanged(nameof(HasEditorSubtitle));
            OnPropertyChanged(nameof(HasEditorContent));
            OnPropertyChanged(nameof(HasEditorAdditionalInfo));
        }
    }
}
