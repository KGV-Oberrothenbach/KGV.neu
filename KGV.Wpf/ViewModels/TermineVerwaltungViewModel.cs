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
    public sealed class TermineVerwaltungViewModel : BaseViewModel, INavigationAware
    {
        public const string FocusTitel = "Titel";
        public const string FocusDatum = "Datum";
        public const string FocusStartUhrzeit = "StartUhrzeit";
        public const string FocusEndUhrzeit = "EndUhrzeit";
        public const string FocusSichtbarAb = "SichtbarAb";
        public const string FocusSichtbarBis = "SichtbarBis";

        private readonly ISupabaseService _supabaseService;
        private readonly MainWindowViewModel _mainWindowViewModel;
        private long? _editingTerminId;
        private EditorStateSnapshot? _initialEditorState;

        public TermineVerwaltungViewModel(ISupabaseService supabaseService, MainWindowViewModel mainWindowViewModel)
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

        public string Title => "Termine bearbeiten";
        public string EmptyText => "Aktuell wurden in der Basistabelle `termin` keine Termine gefunden.";
        public string ReadPathText => "Lesepfad: termin";
        public string WritePathText => "Schreibpfad: termin";
        public string ValidationHintText => "Pflichtfelder: Titel und Datum. `Enduhrzeit` darf nicht vor `Startuhrzeit` liegen. `Sichtbar bis` darf nicht vor `Sichtbar ab` liegen.";
        public bool HasEntries => Entries.Count > 0;
        public bool ShowEmptyState => !HasEntries;
        public bool IsExistingEntry => IsEditorOpen && !IsNewMode;
        public bool ShowValidationMessage => !string.IsNullOrWhiteSpace(ValidationMessage);

        public ObservableCollection<TerminRecord> Entries { get; } = new();

        private TerminRecord? _selectedEntry;
        public TerminRecord? SelectedEntry
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
                    OnPropertyChanged(nameof(IsExistingEntry));
                    AbbrechenCommand.RaiseCanExecuteChanged();
                    SpeichernCommand.RaiseCanExecuteChanged();
                }
            }
        }

        private bool _isNewMode;
        public bool IsNewMode
        {
            get => _isNewMode;
            private set
            {
                if (SetProperty(ref _isNewMode, value))
                    OnPropertyChanged(nameof(IsExistingEntry));
            }
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

        private string _beschreibung = string.Empty;
        public string Beschreibung
        {
            get => _beschreibung;
            set => SetProperty(ref _beschreibung, value);
        }

        private DateTime? _datum;
        public DateTime? Datum
        {
            get => _datum;
            set
            {
                if (SetProperty(ref _datum, value))
                    ClearFieldValidation(nameof(IsDatumInvalid));
            }
        }

        private string _startUhrzeitText = string.Empty;
        public string StartUhrzeitText
        {
            get => _startUhrzeitText;
            set
            {
                if (SetProperty(ref _startUhrzeitText, value))
                    ClearFieldValidation(nameof(IsStartUhrzeitInvalid));
            }
        }

        private string _endUhrzeitText = string.Empty;
        public string EndUhrzeitText
        {
            get => _endUhrzeitText;
            set
            {
                if (SetProperty(ref _endUhrzeitText, value))
                    ClearFieldValidation(nameof(IsEndUhrzeitInvalid));
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

        private bool _isDatumInvalid;
        public bool IsDatumInvalid
        {
            get => _isDatumInvalid;
            private set => SetProperty(ref _isDatumInvalid, value);
        }

        private bool _isStartUhrzeitInvalid;
        public bool IsStartUhrzeitInvalid
        {
            get => _isStartUhrzeitInvalid;
            private set => SetProperty(ref _isStartUhrzeitInvalid, value);
        }

        private bool _isEndUhrzeitInvalid;
        public bool IsEndUhrzeitInvalid
        {
            get => _isEndUhrzeitInvalid;
            private set => SetProperty(ref _isEndUhrzeitInvalid, value);
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
            var items = await _supabaseService.GetTermineVerwaltungAsync();
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

            _editingTerminId = null;
            IsEditorOpen = true;
            IsNewMode = true;
            EditorCaption = "Neuer Termin";
            Titel = string.Empty;
            Beschreibung = string.Empty;
            Datum = DateTime.Today;
            StartUhrzeitText = string.Empty;
            EndUhrzeitText = string.Empty;
            SichtbarAbDatum = null;
            SichtbarAbZeitText = string.Empty;
            SichtbarBisDatum = null;
            SichtbarBisZeitText = string.Empty;
            Aktiv = true;
            ClearValidation();
            _initialEditorState = CaptureEditorState();
        }

        private void OpenEditor(TerminRecord record, bool isNew)
        {
            _editingTerminId = record.Id;
            IsEditorOpen = true;
            IsNewMode = isNew;
            EditorCaption = isNew ? "Neuer Termin" : "Termin bearbeiten";
            Titel = record.Titel ?? string.Empty;
            Beschreibung = record.Beschreibung ?? string.Empty;
            Datum = record.Datum.Date;
            StartUhrzeitText = record.StartUhrzeit.HasValue ? record.StartUhrzeit.Value.ToString(@"hh\:mm") : string.Empty;
            EndUhrzeitText = record.EndUhrzeit.HasValue ? record.EndUhrzeit.Value.ToString(@"hh\:mm") : string.Empty;
            SichtbarAbDatum = record.SichtbarAb?.Date;
            SichtbarAbZeitText = record.SichtbarAb.HasValue ? record.SichtbarAb.Value.ToString("HH:mm") : string.Empty;
            SichtbarBisDatum = record.SichtbarBis?.Date;
            SichtbarBisZeitText = record.SichtbarBis.HasValue ? record.SichtbarBis.Value.ToString("HH:mm") : string.Empty;
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
                var created = await _supabaseService.CreateTerminAsync(InsertRecordMappingExtensions.ToInsertRecord(record));
                if (created == null)
                {
                    ValidationMessage = "Der Termin konnte nicht gespeichert werden. Details stehen im Debug-/Anwendungslog.";
                    return;
                }

                _initialEditorState = CaptureEditorState();
                await NavigateHomeAsync();
                return;
            }

            var success = await _supabaseService.UpdateTerminAsync(record);
            if (!success)
            {
                ValidationMessage = "Der Termin konnte nicht gespeichert werden. Details stehen im Debug-/Anwendungslog.";
                return;
            }

            _initialEditorState = CaptureEditorState();
            await NavigateHomeAsync();
        }

        private bool TryBuildRecord(out TerminRecord record)
        {
            record = new TerminRecord();
            ClearValidation();

            string? firstFocus = null;
            var errors = new ObservableCollection<string>();

            if (string.IsNullOrWhiteSpace(Titel))
            {
                IsTitelInvalid = true;
                firstFocus ??= FocusTitel;
                errors.Add("Titel ist ein Pflichtfeld.");
            }

            if (!Datum.HasValue)
            {
                IsDatumInvalid = true;
                firstFocus ??= FocusDatum;
                errors.Add("Datum ist ein Pflichtfeld.");
            }

            var originalStartText = StartUhrzeitText;
            var startOk = TemporalInputParser.TryNormalizeTimeText(StartUhrzeitText, out var normalizedStartText, out var startUhrzeit);
            StartUhrzeitText = startOk ? normalizedStartText : originalStartText?.Trim() ?? string.Empty;
            if (!startOk)
            {
                IsStartUhrzeitInvalid = true;
                firstFocus ??= FocusStartUhrzeit;
                errors.Add("Startuhrzeit ist ungültig.");
            }

            var originalEndText = EndUhrzeitText;
            var endOk = TemporalInputParser.TryNormalizeTimeText(EndUhrzeitText, out var normalizedEndText, out var endUhrzeit);
            EndUhrzeitText = endOk ? normalizedEndText : originalEndText?.Trim() ?? string.Empty;
            if (!endOk)
            {
                IsEndUhrzeitInvalid = true;
                firstFocus ??= FocusEndUhrzeit;
                errors.Add("Enduhrzeit ist ungültig.");
            }

            if (startOk && endOk && startUhrzeit.HasValue && endUhrzeit.HasValue && endUhrzeit.Value < startUhrzeit.Value)
            {
                IsEndUhrzeitInvalid = true;
                firstFocus ??= FocusEndUhrzeit;
                errors.Add("Enduhrzeit darf nicht vor Startuhrzeit liegen.");
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

            if (errors.Count > 0)
            {
                ValidationMessage = string.Join(Environment.NewLine, errors);
                RequestFocus(firstFocus ?? FocusTitel);
                return false;
            }

            record = new TerminRecord
            {
                Id = _editingTerminId.GetValueOrDefault(),
                Titel = Titel.Trim(),
                Beschreibung = string.IsNullOrWhiteSpace(Beschreibung) ? null : Beschreibung.Trim(),
                Datum = Datum!.Value.Date,
                StartUhrzeit = startUhrzeit,
                EndUhrzeit = endUhrzeit,
                SichtbarAb = sichtbarAb,
                SichtbarBis = sichtbarBis,
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
                Beschreibung?.Trim() ?? string.Empty,
                Datum?.Date,
                StartUhrzeitText?.Trim() ?? string.Empty,
                EndUhrzeitText?.Trim() ?? string.Empty,
                SichtbarAbDatum?.Date,
                SichtbarAbZeitText?.Trim() ?? string.Empty,
                SichtbarBisDatum?.Date,
                SichtbarBisZeitText?.Trim() ?? string.Empty,
                Aktiv,
                IsNewMode);
        }

        private void ResetEditor()
        {
            _editingTerminId = null;
            IsEditorOpen = false;
            IsNewMode = false;
            EditorCaption = string.Empty;
            Titel = string.Empty;
            Beschreibung = string.Empty;
            Datum = null;
            StartUhrzeitText = string.Empty;
            EndUhrzeitText = string.Empty;
            SichtbarAbDatum = null;
            SichtbarAbZeitText = string.Empty;
            SichtbarBisDatum = null;
            SichtbarBisZeitText = string.Empty;
            Aktiv = true;
            ClearValidation();
            _initialEditorState = null;
        }

        private void ClearValidation()
        {
            IsTitelInvalid = false;
            IsDatumInvalid = false;
            IsStartUhrzeitInvalid = false;
            IsEndUhrzeitInvalid = false;
            IsSichtbarAbInvalid = false;
            IsSichtbarBisInvalid = false;
            ValidationMessage = string.Empty;
        }

        private void ClearFieldValidation(string propertyName)
        {
            switch (propertyName)
            {
                case nameof(IsTitelInvalid):
                    IsTitelInvalid = false;
                    break;
                case nameof(IsDatumInvalid):
                    IsDatumInvalid = false;
                    break;
                case nameof(IsStartUhrzeitInvalid):
                    IsStartUhrzeitInvalid = false;
                    break;
                case nameof(IsEndUhrzeitInvalid):
                    IsEndUhrzeitInvalid = false;
                    break;
                case nameof(IsSichtbarAbInvalid):
                    IsSichtbarAbInvalid = false;
                    break;
                case nameof(IsSichtbarBisInvalid):
                    IsSichtbarBisInvalid = false;
                    break;
            }

            ValidationMessage = string.Empty;
        }

        private sealed record EditorStateSnapshot(
            string Titel,
            string Beschreibung,
            DateTime? Datum,
            string StartUhrzeitText,
            string EndUhrzeitText,
            DateTime? SichtbarAbDatum,
            string SichtbarAbZeitText,
            DateTime? SichtbarBisDatum,
            string SichtbarBisZeitText,
            bool Aktiv,
            bool IsNewMode);
    }
}
