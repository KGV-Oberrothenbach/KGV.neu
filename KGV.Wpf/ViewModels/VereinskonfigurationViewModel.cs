using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using KGV.Core.Interfaces;
using KGV.Core.Models;
using KGV.Core.Security;
using KGV.Helpers;

namespace KGV.ViewModels
{
    public sealed class VereinskonfigurationViewModel : BaseViewModel, INavigationAware
    {
        private readonly ISupabaseService _supabaseService;
        private readonly MainWindowViewModel _mainWindowViewModel;
        private VereinskonfigurationRecord _currentRecord = new() { Aktiv = true };
        private string _vereinsname = string.Empty;
        private string _kurzname = string.Empty;
        private string _registerangabe = string.Empty;
        private string _strasse = string.Empty;
        private string _plz = string.Empty;
        private string _ort = string.Empty;
        private string _standardEmail = string.Empty;
        private string _standardTelefon = string.Empty;
        private string _website = string.Empty;
        private string _kontoinhaber = string.Empty;
        private string _bankname = string.Empty;
        private string _iban = string.Empty;
        private string _bic = string.Empty;
        private string _verwendungszweckMitgliedsantrag = string.Empty;
        private string _verwendungszweckPachtvertrag = string.Empty;
        private string _dokumentOrt = string.Empty;
        private string _standardHinweistext = string.Empty;
        private string _datenschutzText = string.Empty;
        private string _datenschutzVersion = string.Empty;
        private string _datenschutzStand = string.Empty;
        private string _statusMessage = string.Empty;
        private bool _isBusy;

        public VereinskonfigurationViewModel(ISupabaseService supabaseService, MainWindowViewModel mainWindowViewModel)
        {
            _supabaseService = supabaseService ?? throw new ArgumentNullException(nameof(supabaseService));
            _mainWindowViewModel = mainWindowViewModel ?? throw new ArgumentNullException(nameof(mainWindowViewModel));

            RefreshCommand = new RelayCommand<object?>(_ => _ = LoadAsync(), _ => !IsBusy);
            SaveCommand = new RelayCommand<object?>(_ => _ = SaveAsync(), _ => CanSave);
        }

        public RelayCommand<object?> RefreshCommand { get; }
        public RelayCommand<object?> SaveCommand { get; }

        public string Titel => "Verwaltung";
        public string Untertitel => "Vereinskonfiguration";
        public string Beschreibung => "Die aktive Vereinskonfiguration wird zentral für Vereinsdaten, Standardtexte und Dokumentmetadaten gepflegt.";
        public bool IsAdmin => _mainWindowViewModel.UserContext.Role == UserRole.Admin;
        public bool HasStatusMessage => !string.IsNullOrWhiteSpace(StatusMessage);
        public bool IsEditable => IsAdmin && !IsBusy;
        public bool CanSave => IsEditable;

        public string Vereinsname
        {
            get => _vereinsname;
            set => SetEditorValue(ref _vereinsname, value);
        }

        public string Kurzname
        {
            get => _kurzname;
            set => SetEditorValue(ref _kurzname, value);
        }

        public string Registerangabe
        {
            get => _registerangabe;
            set => SetEditorValue(ref _registerangabe, value);
        }

        public string Strasse
        {
            get => _strasse;
            set => SetEditorValue(ref _strasse, value);
        }

        public string Plz
        {
            get => _plz;
            set => SetEditorValue(ref _plz, value);
        }

        public string Ort
        {
            get => _ort;
            set => SetEditorValue(ref _ort, value);
        }

        public string StandardEmail
        {
            get => _standardEmail;
            set => SetEditorValue(ref _standardEmail, value);
        }

        public string StandardTelefon
        {
            get => _standardTelefon;
            set => SetEditorValue(ref _standardTelefon, value);
        }

        public string Website
        {
            get => _website;
            set => SetEditorValue(ref _website, value);
        }

        public string Kontoinhaber
        {
            get => _kontoinhaber;
            set => SetEditorValue(ref _kontoinhaber, value);
        }

        public string Bankname
        {
            get => _bankname;
            set => SetEditorValue(ref _bankname, value);
        }

        public string Iban
        {
            get => _iban;
            set => SetEditorValue(ref _iban, value);
        }

        public string Bic
        {
            get => _bic;
            set => SetEditorValue(ref _bic, value);
        }

        public string VerwendungszweckMitgliedsantrag
        {
            get => _verwendungszweckMitgliedsantrag;
            set => SetEditorValue(ref _verwendungszweckMitgliedsantrag, value);
        }

        public string VerwendungszweckPachtvertrag
        {
            get => _verwendungszweckPachtvertrag;
            set => SetEditorValue(ref _verwendungszweckPachtvertrag, value);
        }

        public string DokumentOrt
        {
            get => _dokumentOrt;
            set => SetEditorValue(ref _dokumentOrt, value);
        }

        public string StandardHinweistext
        {
            get => _standardHinweistext;
            set => SetEditorValue(ref _standardHinweistext, value);
        }

        public string DatenschutzText
        {
            get => _datenschutzText;
            set => SetEditorValue(ref _datenschutzText, value);
        }

        public string DatenschutzVersion
        {
            get => _datenschutzVersion;
            set => SetEditorValue(ref _datenschutzVersion, value);
        }

        public string DatenschutzStand
        {
            get => _datenschutzStand;
            set => SetEditorValue(ref _datenschutzStand, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            private set
            {
                if (!SetProperty(ref _statusMessage, value ?? string.Empty))
                    return;

                OnPropertyChanged(nameof(HasStatusMessage));
            }
        }

        public bool IsBusy
        {
            get => _isBusy;
            private set
            {
                if (!SetProperty(ref _isBusy, value))
                    return;

                OnPropertyChanged(nameof(IsEditable));
                OnPropertyChanged(nameof(CanSave));
                RefreshCommand.RaiseCanExecuteChanged();
                SaveCommand.RaiseCanExecuteChanged();
            }
        }

        public Task OnNavigatedToAsync() => LoadAsync();
        public Task OnNavigatedFromAsync() => Task.CompletedTask;

        private async Task LoadAsync()
        {
            try
            {
                IsBusy = true;
                StatusMessage = string.Empty;

                var record = await _supabaseService.GetAktiveVereinskonfigurationAsync();
                _currentRecord = record ?? new VereinskonfigurationRecord { Aktiv = true };
                ApplyEditor(_currentRecord);

                if (record == null)
                    StatusMessage = "Es ist noch keine aktive Vereinskonfiguration hinterlegt. Mit dem ersten Speichern wird sie angelegt.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Vereinskonfiguration konnte nicht geladen werden: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task SaveAsync()
        {
            if (!TryBuildRecord(out var record, out var validationMessage))
            {
                StatusMessage = validationMessage;
                return;
            }

            try
            {
                IsBusy = true;
                var saved = await _supabaseService.SaveAktiveVereinskonfigurationAsync(record);
                if (saved == null)
                {
                    StatusMessage = "Vereinskonfiguration konnte nicht gespeichert werden.";
                    return;
                }

                _currentRecord = saved;
                ApplyEditor(saved);
                StatusMessage = "Vereinskonfiguration gespeichert.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Vereinskonfiguration konnte nicht gespeichert werden: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void ApplyEditor(VereinskonfigurationRecord record)
        {
            Vereinsname = record.Vereinsname ?? string.Empty;
            Kurzname = record.Kurzname ?? string.Empty;
            Registerangabe = record.Registerangabe ?? string.Empty;
            Strasse = record.Strasse ?? string.Empty;
            Plz = record.Plz ?? string.Empty;
            Ort = record.Ort ?? string.Empty;
            StandardEmail = record.StandardEmail ?? string.Empty;
            StandardTelefon = record.StandardTelefon ?? string.Empty;
            Website = record.Website ?? string.Empty;
            Kontoinhaber = record.Kontoinhaber ?? string.Empty;
            Bankname = record.Bankname ?? string.Empty;
            Iban = record.Iban ?? string.Empty;
            Bic = record.Bic ?? string.Empty;
            VerwendungszweckMitgliedsantrag = record.VerwendungszweckMitgliedsantrag ?? string.Empty;
            VerwendungszweckPachtvertrag = record.VerwendungszweckPachtvertrag ?? string.Empty;
            DokumentOrt = record.DokumentOrt ?? string.Empty;
            StandardHinweistext = record.StandardHinweistext ?? string.Empty;
            DatenschutzText = record.DatenschutzText ?? string.Empty;
            DatenschutzVersion = record.DatenschutzVersion ?? string.Empty;
            DatenschutzStand = record.DatenschutzStand?.ToString("dd.MM.yyyy", CultureInfo.CurrentCulture) ?? string.Empty;
        }

        private bool TryBuildRecord(out VereinskonfigurationRecord record, out string validationMessage)
        {
            validationMessage = string.Empty;
            record = new VereinskonfigurationRecord
            {
                Id = _currentRecord.Id,
                Aktiv = true,
                CreatedAt = _currentRecord.CreatedAt,
                UpdatedAt = _currentRecord.UpdatedAt,
                Vereinsname = Vereinsname,
                Kurzname = Kurzname,
                Registerangabe = Registerangabe,
                Strasse = Strasse,
                Plz = Plz,
                Ort = Ort,
                StandardEmail = StandardEmail,
                StandardTelefon = StandardTelefon,
                Website = Website,
                Kontoinhaber = Kontoinhaber,
                Bankname = Bankname,
                Iban = Iban,
                Bic = Bic,
                VerwendungszweckMitgliedsantrag = VerwendungszweckMitgliedsantrag,
                VerwendungszweckPachtvertrag = VerwendungszweckPachtvertrag,
                DokumentOrt = DokumentOrt,
                StandardHinweistext = StandardHinweistext,
                DatenschutzText = DatenschutzText,
                DatenschutzVersion = DatenschutzVersion
            };

            if (!TryParseOptionalDate(DatenschutzStand, out var datenschutzStand))
            {
                validationMessage = "Bitte ein gültiges Datum für Datenschutz Stand eingeben.";
                return false;
            }

            record.DatenschutzStand = datenschutzStand;
            return true;
        }

        private void SetEditorValue(ref string field, string? value, [CallerMemberName] string? propertyName = null)
        {
            if (!SetProperty(ref field, value ?? string.Empty, propertyName))
                return;

            SaveCommand.RaiseCanExecuteChanged();
        }

        private static bool TryParseOptionalDate(string raw, out DateTime? value)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                value = null;
                return true;
            }

            if (DateTime.TryParse(raw, CultureInfo.CurrentCulture, DateTimeStyles.AssumeLocal, out var parsed)
                || DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out parsed))
            {
                value = parsed.Date;
                return true;
            }

            value = null;
            return false;
        }
    }
}
