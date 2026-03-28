using KGV.Core.Interfaces;
using KGV.Core.Models;
using KGV.Core.Utilities;
using KGV.Helpers;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace KGV.ViewModels
{
    public sealed class BekanntmachungenVerwaltungViewModel : BaseViewModel, INavigationAware
    {
        public const string FocusTitel = "Titel";
        public const string FocusInhaltHtml = "InhaltHtml";
        public const string FocusSichtbarAb = "SichtbarAb";
        public const string FocusSichtbarBis = "SichtbarBis";
        public const string FocusSortOrder = "SortOrder";

        private readonly ISupabaseService _supabaseService;
        private readonly MainWindowViewModel _mainWindowViewModel;
        private long? _editingBekanntmachungId;
        private EditorStateSnapshot? _initialEditorState;

        public BekanntmachungenVerwaltungViewModel(ISupabaseService supabaseService, MainWindowViewModel mainWindowViewModel)
        {
            _supabaseService = supabaseService ?? throw new ArgumentNullException(nameof(supabaseService));
            _mainWindowViewModel = mainWindowViewModel ?? throw new ArgumentNullException(nameof(mainWindowViewModel));
            AktualisierenCommand = new RelayCommand<object?>(_ => _ = LoadAsync());
            NeuCommand = new RelayCommand<object?>(_ => OpenNewEditor());
            OeffnenCommand = new RelayCommand<object?>(_ => OpenSelectedEditor(), _ => SelectedEntry != null);
            AbbrechenCommand = new RelayCommand<object?>(_ => CancelEdit(), _ => IsEditorOpen);
            SpeichernCommand = new RelayCommand<object?>(_ => _ = SaveAsync(), _ => IsEditorOpen);
            ZurueckCommand = new RelayCommand<object?>(_ => _ = NavigateBackAsync());
        }

        public string Title => "Bekanntmachungen bearbeiten";
        public string EmptyText => "Aktuell wurden in der Basistabelle `bekanntmachung` keine Bekanntmachungen gefunden.";
        public string ReadPathText => "Lesepfad: bekanntmachung";
        public string WritePathText => "Schreibpfad: bekanntmachung";
        public string ValidationHintText => "Pflichtfelder: Titel und HTML-Inhalt. `Sichtbar bis` darf nicht vor `Sichtbar ab` liegen. `Sortierreihenfolge` ist optional und muss eine ganze Zahl sein.";
        public bool HasEntries => Entries.Count > 0;
        public bool ShowEmptyState => !HasEntries;
        public bool ShowValidationMessage => !string.IsNullOrWhiteSpace(ValidationMessage);

        public ObservableCollection<BekanntmachungRecord> Entries { get; } = new();

        private BekanntmachungRecord? _selectedEntry;
        public BekanntmachungRecord? SelectedEntry
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
            private set
            {
                if (SetProperty(ref _isEditorOpen, value))
                {
                    AbbrechenCommand.RaiseCanExecuteChanged();
                    SpeichernCommand.RaiseCanExecuteChanged();
                }
            }
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

        private string _titel = string.Empty;
        public string Titel
        {
            get => _titel;
            set
            {
                if (SetProperty(ref _titel, value))
                    ClearFieldValidation(nameof(IsTitelInvalid));
            }
        }

        private string _inhaltHtml = string.Empty;
        public string InhaltHtml
        {
            get => _inhaltHtml;
            set
            {
                if (SetProperty(ref _inhaltHtml, value))
                    ClearFieldValidation(nameof(IsInhaltHtmlInvalid));
            }
        }

        private DateTime? _sichtbarAbDatum;
        public DateTime? SichtbarAbDatum
        {
            get => _sichtbarAbDatum;
            set
            {
                if (SetProperty(ref _sichtbarAbDatum, value))
                    ClearFieldValidation(nameof(IsSichtbarAbInvalid));
            }
        }

        private string _sichtbarAbZeitText = string.Empty;
        public string SichtbarAbZeitText
        {
            get => _sichtbarAbZeitText;
            set
            {
                if (SetProperty(ref _sichtbarAbZeitText, value))
                    ClearFieldValidation(nameof(IsSichtbarAbInvalid));
            }
        }

        private DateTime? _sichtbarBisDatum;
        public DateTime? SichtbarBisDatum
        {
            get => _sichtbarBisDatum;
            set
            {
                if (SetProperty(ref _sichtbarBisDatum, value))
                    ClearFieldValidation(nameof(IsSichtbarBisInvalid));
            }
        }

        private string _sichtbarBisZeitText = string.Empty;
        public string SichtbarBisZeitText
        {
            get => _sichtbarBisZeitText;
            set
            {
                if (SetProperty(ref _sichtbarBisZeitText, value))
                    ClearFieldValidation(nameof(IsSichtbarBisInvalid));
            }
        }

        private string _sortOrderText = string.Empty;
        public string SortOrderText
        {
            get => _sortOrderText;
            set
            {
                if (SetProperty(ref _sortOrderText, value))
                    ClearFieldValidation(nameof(IsSortOrderInvalid));
            }
        }

        private bool _aktiv = true;
        public bool Aktiv
        {
            get => _aktiv;
            set => SetProperty(ref _aktiv, value);
        }

        private bool _isTitelInvalid;
        public bool IsTitelInvalid
        {
            get => _isTitelInvalid;
            private set => SetProperty(ref _isTitelInvalid, value);
        }

        private bool _isInhaltHtmlInvalid;
        public bool IsInhaltHtmlInvalid
        {
            get => _isInhaltHtmlInvalid;
            private set => SetProperty(ref _isInhaltHtmlInvalid, value);
        }

        private bool _isSichtbarAbInvalid;
        public bool IsSichtbarAbInvalid
        {
            get => _isSichtbarAbInvalid;
            private set => SetProperty(ref _isSichtbarAbInvalid, value);
        }

        private bool _isSichtbarBisInvalid;
        public bool IsSichtbarBisInvalid
        {
            get => _isSichtbarBisInvalid;
            private set => SetProperty(ref _isSichtbarBisInvalid, value);
        }

        private bool _isSortOrderInvalid;
        public bool IsSortOrderInvalid
        {
            get => _isSortOrderInvalid;
            private set => SetProperty(ref _isSortOrderInvalid, value);
        }

        private string _validationMessage = string.Empty;
        public string ValidationMessage
        {
            get => _validationMessage;
            private set
            {
                if (SetProperty(ref _validationMessage, value))
                    OnPropertyChanged(nameof(ShowValidationMessage));
            }
        }

        private string _focusTarget = string.Empty;
        public string FocusTarget
        {
            get => _focusTarget;
            private set => SetProperty(ref _focusTarget, value);
        }

        private int _focusRequestToken;
        public int FocusRequestToken
        {
            get => _focusRequestToken;
            private set => SetProperty(ref _focusRequestToken, value);
        }

        public RelayCommand<object?> AktualisierenCommand { get; }
        public RelayCommand<object?> NeuCommand { get; }
        public RelayCommand<object?> OeffnenCommand { get; }
        public RelayCommand<object?> AbbrechenCommand { get; }
        public RelayCommand<object?> SpeichernCommand { get; }
        public RelayCommand<object?> ZurueckCommand { get; }

        public async Task OnNavigatedToAsync()
        {
            await LoadAsync();
        }

        public Task OnNavigatedFromAsync() => Task.CompletedTask;

        private async Task LoadAsync(long? selectId = null)
        {
            var items = await _supabaseService.GetBekanntmachungenVerwaltungAsync();
            Entries.Clear();
            foreach (var item in items)
                Entries.Add(item);

            SelectedEntry = selectId.HasValue
                ? Entries.FirstOrDefault(x => x.Id == selectId.Value)
                : Entries.FirstOrDefault();

            OnPropertyChanged(nameof(HasEntries));
            OnPropertyChanged(nameof(ShowEmptyState));

            if (selectId.HasValue && SelectedEntry != null)
                OpenEditor(SelectedEntry, false);
            else
                ResetEditor();
        }

        private void OpenSelectedEditor()
        {
            if (IsEditorOpen && !CanCloseEditor())
                return;

            if (SelectedEntry == null)
                return;

            OpenEditor(SelectedEntry, false);
        }

        private void OpenNewEditor()
        {
            if (IsEditorOpen && !CanCloseEditor())
                return;

            _editingBekanntmachungId = null;
            IsEditorOpen = true;
            IsNewMode = true;
            EditorCaption = "Neue Bekanntmachung";
            Titel = string.Empty;
            InhaltHtml = "<p></p>";
            var sichtbarAb = CreateCurrentTimestampDefault();
            SichtbarAbDatum = sichtbarAb.Date;
            SichtbarAbZeitText = sichtbarAb.ToString("HH:mm");
            var sichtbarBis = sichtbarAb.AddMonths(1);
            SichtbarBisDatum = sichtbarBis.Date;
            SichtbarBisZeitText = sichtbarBis.ToString("HH:mm");
            SortOrderText = string.Empty;
            Aktiv = true;
            ClearValidation();
            _initialEditorState = CaptureEditorState();
        }

        private void OpenEditor(BekanntmachungRecord record, bool isNew)
        {
            _editingBekanntmachungId = record.Id;
            IsEditorOpen = true;
            IsNewMode = isNew;
            EditorCaption = isNew ? "Neue Bekanntmachung" : "Bekanntmachung bearbeiten";
            Titel = record.Titel ?? string.Empty;
            InhaltHtml = record.InhaltHtml ?? string.Empty;
            var sichtbarAb = record.SichtbarAb ?? CreateCurrentTimestampDefault();
            SichtbarAbDatum = sichtbarAb.Date;
            SichtbarAbZeitText = sichtbarAb.ToString("HH:mm");
            var sichtbarBis = record.SichtbarBis ?? sichtbarAb.AddMonths(1);
            SichtbarBisDatum = sichtbarBis.Date;
            SichtbarBisZeitText = sichtbarBis.ToString("HH:mm");
            SortOrderText = record.SortOrder?.ToString() ?? string.Empty;
            Aktiv = record.Aktiv;
            ClearValidation();
            _initialEditorState = CaptureEditorState();
        }

        private void CancelEdit()
        {
            if (!CanCloseEditor())
                return;

            ResetEditor();
        }

        private async Task SaveAsync()
        {
            if (!TryBuildRecord(out var record))
                return;

            if (IsNewMode)
            {
                var created = await _supabaseService.CreateBekanntmachungAsync(record.ToInsertRecord());
                if (created == null)
                {
                    ValidationMessage = "Die Bekanntmachung konnte nicht gespeichert werden. Details stehen im Debug-/Anwendungslog.";
                    return;
                }

                _initialEditorState = CaptureEditorState();
                await NavigateHomeAsync();
                return;
            }

            var success = await _supabaseService.UpdateBekanntmachungAsync(record);
            if (!success)
            {
                ValidationMessage = "Die Bekanntmachung konnte nicht gespeichert werden. Details stehen im Debug-/Anwendungslog.";
                return;
            }

            _initialEditorState = CaptureEditorState();
            await NavigateHomeAsync();
        }

        private bool TryBuildRecord(out BekanntmachungRecord record)
        {
            record = new BekanntmachungRecord();
            ClearValidation();

            string? firstFocus = null;
            var errors = new ObservableCollection<string>();

            if (string.IsNullOrWhiteSpace(Titel))
            {
                IsTitelInvalid = true;
                firstFocus ??= FocusTitel;
                errors.Add("Titel ist ein Pflichtfeld.");
            }

            if (string.IsNullOrWhiteSpace(InhaltHtml))
            {
                IsInhaltHtmlInvalid = true;
                firstFocus ??= FocusInhaltHtml;
                errors.Add("HTML-Inhalt ist ein Pflichtfeld.");
            }

            var sichtbarAbOk = TryBuildOptionalTimestamp(SichtbarAbDatum, SichtbarAbZeitText, FocusSichtbarAb, "Sichtbar ab", out var sichtbarAb, out var normalizedSichtbarAbZeit, ref firstFocus, errors);
            SichtbarAbZeitText = normalizedSichtbarAbZeit;
            IsSichtbarAbInvalid = !sichtbarAbOk;

            var sichtbarBisOk = TryBuildOptionalTimestamp(SichtbarBisDatum, SichtbarBisZeitText, FocusSichtbarBis, "Sichtbar bis", out var sichtbarBis, out var normalizedSichtbarBisZeit, ref firstFocus, errors);
            SichtbarBisZeitText = normalizedSichtbarBisZeit;
            IsSichtbarBisInvalid = !sichtbarBisOk;

            if (sichtbarAbOk && sichtbarBisOk && sichtbarAb.HasValue && sichtbarBis.HasValue && sichtbarBis.Value < sichtbarAb.Value)
            {
                IsSichtbarBisInvalid = true;
                firstFocus ??= FocusSichtbarBis;
                errors.Add("Sichtbar bis darf nicht vor Sichtbar ab liegen.");
            }

            int? sortOrder = null;
            if (!string.IsNullOrWhiteSpace(SortOrderText))
            {
                if (!int.TryParse(SortOrderText.Trim(), out var parsedSortOrder))
                {
                    IsSortOrderInvalid = true;
                    firstFocus ??= FocusSortOrder;
                    errors.Add("Sortierreihenfolge muss eine ganze Zahl sein.");
                }
                else
                {
                    sortOrder = parsedSortOrder;
                    SortOrderText = parsedSortOrder.ToString();
                }
            }

            if (errors.Count > 0)
            {
                ValidationMessage = string.Join(Environment.NewLine, errors);
                RequestFocus(firstFocus ?? FocusTitel);
                return false;
            }

            record = new BekanntmachungRecord
            {
                Id = _editingBekanntmachungId.GetValueOrDefault(),
                Titel = Titel.Trim(),
                InhaltHtml = InhaltHtml.Trim(),
                SichtbarAb = sichtbarAb,
                SichtbarBis = sichtbarBis,
                SortOrder = sortOrder,
                Aktiv = Aktiv
            };

            ValidationMessage = string.Empty;
            return true;
        }

        private bool TryBuildOptionalTimestamp(
            DateTime? date,
            string timeText,
            string focusKey,
            string fieldName,
            out DateTime? value,
            out string normalizedTimeText,
            ref string? firstFocus,
            ObservableCollection<string> errors)
        {
            value = null;
            normalizedTimeText = string.Empty;

            var hasDate = date.HasValue;
            var hasTime = !string.IsNullOrWhiteSpace(timeText);
            if (!hasDate && !hasTime)
                return true;

            if (!hasDate || !hasTime)
            {
                firstFocus ??= focusKey;
                errors.Add($"{fieldName} benötigt Datum und Uhrzeit.");
                normalizedTimeText = timeText?.Trim() ?? string.Empty;
                return false;
            }

            if (!TemporalInputParser.TryNormalizeTimeText(timeText, out normalizedTimeText, out var time))
            {
                normalizedTimeText = timeText?.Trim() ?? string.Empty;
                firstFocus ??= focusKey;
                errors.Add($"{fieldName} ist ungültig.");
                return false;
            }

            value = date!.Value.Date.Add(time!.Value);
            return true;
        }

        private void RequestFocus(string target)
        {
            FocusTarget = target;
            FocusRequestToken++;
        }

        private static DateTime CreateCurrentTimestampDefault()
        {
            var now = DateTime.Now;
            return new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, 0);
        }

        private bool CanCloseEditor()
        {
            if (!IsEditorOpen)
                return true;

            if (!HasUnsavedChanges())
                return true;

            return MessageBox.Show(
                "Es liegen ungespeicherte Änderungen vor. Änderungen verwerfen?",
                "Ungespeicherte Änderungen",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning) == MessageBoxResult.Yes;
        }

        private bool HasUnsavedChanges()
        {
            return IsEditorOpen && _initialEditorState != null && _initialEditorState != CaptureEditorState();
        }

        private async Task NavigateBackAsync()
        {
            if (!CanCloseEditor())
                return;

            await NavigateHomeAsync();
        }

        private async Task NavigateHomeAsync()
        {
            var home = _mainWindowViewModel.NavigateToHomeViewModel();
            if (home != null)
                await _mainWindowViewModel.NavigateToAsync(home);
        }

        private EditorStateSnapshot CaptureEditorState()
        {
            return new EditorStateSnapshot(
                Titel?.Trim() ?? string.Empty,
                InhaltHtml?.Trim() ?? string.Empty,
                SichtbarAbDatum?.Date,
                SichtbarAbZeitText?.Trim() ?? string.Empty,
                SichtbarBisDatum?.Date,
                SichtbarBisZeitText?.Trim() ?? string.Empty,
                SortOrderText?.Trim() ?? string.Empty,
                Aktiv,
                IsNewMode);
        }

        private void ResetEditor()
        {
            _editingBekanntmachungId = null;
            IsEditorOpen = false;
            IsNewMode = false;
            EditorCaption = string.Empty;
            Titel = string.Empty;
            InhaltHtml = string.Empty;
            SichtbarAbDatum = null;
            SichtbarAbZeitText = string.Empty;
            SichtbarBisDatum = null;
            SichtbarBisZeitText = string.Empty;
            SortOrderText = string.Empty;
            Aktiv = true;
            ClearValidation();
            _initialEditorState = null;
        }

        private void ClearValidation()
        {
            IsTitelInvalid = false;
            IsInhaltHtmlInvalid = false;
            IsSichtbarAbInvalid = false;
            IsSichtbarBisInvalid = false;
            IsSortOrderInvalid = false;
            ValidationMessage = string.Empty;
        }

        private void ClearFieldValidation(string propertyName)
        {
            switch (propertyName)
            {
                case nameof(IsTitelInvalid):
                    IsTitelInvalid = false;
                    break;
                case nameof(IsInhaltHtmlInvalid):
                    IsInhaltHtmlInvalid = false;
                    break;
                case nameof(IsSichtbarAbInvalid):
                    IsSichtbarAbInvalid = false;
                    break;
                case nameof(IsSichtbarBisInvalid):
                    IsSichtbarBisInvalid = false;
                    break;
                case nameof(IsSortOrderInvalid):
                    IsSortOrderInvalid = false;
                    break;
            }

            ValidationMessage = string.Empty;
        }

        private sealed record EditorStateSnapshot(
            string Titel,
            string InhaltHtml,
            DateTime? SichtbarAbDatum,
            string SichtbarAbZeitText,
            DateTime? SichtbarBisDatum,
            string SichtbarBisZeitText,
            string SortOrderText,
            bool Aktiv,
            bool IsNewMode);
    }
}
