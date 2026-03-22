using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Messaging;
using KGV.Core.Interfaces;
using KGV.Core.Models;
using KGV.Helpers;
using KGV.Messages;

namespace KGV.ViewModels
{
    public sealed class ArbeitsstundenErfassungViewModel : BaseViewModel, INavigationAware
    {
        public const string FocusDatum = "Datum";
        public const string FocusStunden = "Stunden";
        public const string FocusArtDerArbeit = "ArtDerArbeit";

        private readonly ISupabaseService _supabaseService;
        private readonly MainWindowViewModel _mainWindowViewModel;

        private int? _mitgliedId;
        private int? _saisonId;

        public ArbeitsstundenErfassungViewModel(ISupabaseService supabaseService, MainWindowViewModel mainWindowViewModel)
        {
            _supabaseService = supabaseService ?? throw new ArgumentNullException(nameof(supabaseService));
            _mainWindowViewModel = mainWindowViewModel ?? throw new ArgumentNullException(nameof(mainWindowViewModel));

            SpeichernCommand = new RelayCommand<object?>(_ => _ = SaveAsync(), _ => IsReadyForInput);
            AbbrechenCommand = new RelayCommand<object?>(_ => ResetForm(), _ => IsReadyForInput);
        }

        public string Title => "Arbeitsstunden erfassen";
        public string Description => "Erfasse eine neue Arbeitsstunde für deinen eigenen Mitgliedskontext. Neue Einträge werden in diesem Flow immer zunächst als nicht freigegeben gespeichert.";
        public string CurrentMemberText => !string.IsNullOrWhiteSpace(_currentMemberDisplayName)
            ? _currentMemberDisplayName
            : "Aktuell konnte kein eigener Mitgliedskontext geladen werden.";
        public bool ShowValidationMessage => !string.IsNullOrWhiteSpace(ValidationMessage);
        public bool ShowStatusMessage => !string.IsNullOrWhiteSpace(StatusMessage);
        public bool IsReadyForInput => _mitgliedId.HasValue && _saisonId.HasValue;

        private string _currentMemberDisplayName = string.Empty;
        private string CurrentMemberDisplayName
        {
            get => _currentMemberDisplayName;
            set
            {
                if (SetProperty(ref _currentMemberDisplayName, value))
                    OnPropertyChanged(nameof(CurrentMemberText));
            }
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

        private string _stundenText = string.Empty;
        public string StundenText
        {
            get => _stundenText;
            set
            {
                if (SetProperty(ref _stundenText, value))
                    ClearFieldValidation(nameof(IsStundenInvalid));
            }
        }

        private string _artDerArbeit = string.Empty;
        public string ArtDerArbeit
        {
            get => _artDerArbeit;
            set
            {
                if (SetProperty(ref _artDerArbeit, value))
                    ClearFieldValidation(nameof(IsArtDerArbeitInvalid));
            }
        }

        private bool _isDatumInvalid;
        public bool IsDatumInvalid
        {
            get => _isDatumInvalid;
            private set => SetProperty(ref _isDatumInvalid, value);
        }

        private bool _isStundenInvalid;
        public bool IsStundenInvalid
        {
            get => _isStundenInvalid;
            private set => SetProperty(ref _isStundenInvalid, value);
        }

        private bool _isArtDerArbeitInvalid;
        public bool IsArtDerArbeitInvalid
        {
            get => _isArtDerArbeitInvalid;
            private set => SetProperty(ref _isArtDerArbeitInvalid, value);
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

        private string _statusMessage = string.Empty;
        public string StatusMessage
        {
            get => _statusMessage;
            private set
            {
                if (SetProperty(ref _statusMessage, value))
                    OnPropertyChanged(nameof(ShowStatusMessage));
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

        public RelayCommand<object?> SpeichernCommand { get; }
        public RelayCommand<object?> AbbrechenCommand { get; }

        public async Task OnNavigatedToAsync()
        {
            await LoadContextAsync();
        }

        public Task OnNavigatedFromAsync() => Task.CompletedTask;

        private async Task LoadContextAsync()
        {
            StatusMessage = string.Empty;
            ValidationMessage = string.Empty;

            var member = await _mainWindowViewModel.EnsureCurrentMemberSelectedAsync();
            _mitgliedId = member?.Id;
            CurrentMemberDisplayName = member?.DisplayName ?? string.Empty;

            var saisonen = await _supabaseService.GetSaisonRecordsAsync();
            var todayYear = DateTime.Today.Year;
            _saisonId = saisonen.FirstOrDefault(x => x.Jahr == todayYear)?.Id
                ?? saisonen.OrderByDescending(x => x.Jahr).FirstOrDefault()?.Id;

            Datum = DateTime.Today;
            StundenText = string.Empty;
            ArtDerArbeit = string.Empty;

            if (!_mitgliedId.HasValue)
                StatusMessage = "Der eigene Mitgliedskontext konnte aktuell nicht geladen werden.";
            else if (!_saisonId.HasValue)
                StatusMessage = "Die aktuelle Saison konnte aktuell nicht geladen werden.";

            OnPropertyChanged(nameof(IsReadyForInput));
            SpeichernCommand.RaiseCanExecuteChanged();
            AbbrechenCommand.RaiseCanExecuteChanged();
            RequestFocus(FocusDatum);
        }

        private async Task SaveAsync()
        {
            if (!TryBuildRecord(out var record))
                return;

            var ok = await _supabaseService.AddArbeitsstundeAsync(record);
            if (!ok)
            {
                ValidationMessage = "Die Arbeitsstunde konnte nicht gespeichert werden. Details stehen im Debug-/Anwendungslog.";
                return;
            }

            WeakReferenceMessenger.Default.Send(new ArbeitsstundenChangedMessage());
            StatusMessage = "Arbeitsstunde wurde zur späteren Freigabe gespeichert.";
            ValidationMessage = string.Empty;
            Datum = DateTime.Today;
            StundenText = string.Empty;
            ArtDerArbeit = string.Empty;
            RequestFocus(FocusDatum);
        }

        private bool TryBuildRecord(out ArbeitsstundeRecord record)
        {
            record = new ArbeitsstundeRecord();
            ClearValidation();
            StatusMessage = string.Empty;

            string? firstFocus = null;
            var errors = new ObservableCollection<string>();

            if (!_mitgliedId.HasValue || !_saisonId.HasValue)
            {
                ValidationMessage = "Arbeitsstunden können aktuell nicht erfasst werden, weil Mitglied oder Saison fehlen.";
                return false;
            }

            if (!Datum.HasValue)
            {
                IsDatumInvalid = true;
                firstFocus ??= FocusDatum;
                errors.Add("Datum ist ein Pflichtfeld.");
            }

            if (!TryParseHours(StundenText, out var stunden) || stunden <= 0)
            {
                IsStundenInvalid = true;
                firstFocus ??= FocusStunden;
                errors.Add("Stunden ist ein Pflichtfeld und muss größer als 0 sein.");
            }
            else
            {
                StundenText = stunden.ToString("0.##", CultureInfo.CurrentCulture);
            }

            if (string.IsNullOrWhiteSpace(ArtDerArbeit))
            {
                IsArtDerArbeitInvalid = true;
                firstFocus ??= FocusArtDerArbeit;
                errors.Add("Art der Arbeit ist ein Pflichtfeld.");
            }

            if (errors.Count > 0)
            {
                ValidationMessage = string.Join(Environment.NewLine, errors);
                RequestFocus(firstFocus ?? FocusDatum);
                return false;
            }

            record = new ArbeitsstundeRecord
            {
                MitgliedId = _mitgliedId.Value,
                SaisonId = _saisonId.Value,
                Datum = Datum!.Value.Date,
                Stunden = stunden,
                ArtDerArbeit = ArtDerArbeit.Trim(),
                Freigegeben = false,
                Status = "offen",
                GenehmigtAm = null,
                GenehmigtVon = null
            };

            return true;
        }

        private static bool TryParseHours(string? input, out decimal value)
        {
            value = 0;
            if (string.IsNullOrWhiteSpace(input))
                return false;

            if (decimal.TryParse(input, NumberStyles.Number, CultureInfo.CurrentCulture, out value))
                return true;

            if (decimal.TryParse(input, NumberStyles.Number, CultureInfo.InvariantCulture, out value))
                return true;

            var normalized = input.Replace(',', '.');
            return decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out value);
        }

        private void ResetForm()
        {
            Datum = DateTime.Today;
            StundenText = string.Empty;
            ArtDerArbeit = string.Empty;
            ClearValidation();
            StatusMessage = string.Empty;
            RequestFocus(FocusDatum);
        }

        private void RequestFocus(string target)
        {
            FocusTarget = target;
            FocusRequestToken++;
        }

        private void ClearValidation()
        {
            IsDatumInvalid = false;
            IsStundenInvalid = false;
            IsArtDerArbeitInvalid = false;
            ValidationMessage = string.Empty;
        }

        private void ClearFieldValidation(string propertyName)
        {
            switch (propertyName)
            {
                case nameof(IsDatumInvalid):
                    IsDatumInvalid = false;
                    break;
                case nameof(IsStundenInvalid):
                    IsStundenInvalid = false;
                    break;
                case nameof(IsArtDerArbeitInvalid):
                    IsArtDerArbeitInvalid = false;
                    break;
            }

            ValidationMessage = string.Empty;
        }
    }
}
