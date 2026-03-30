using KGV.Core.Interfaces;
using KGV.Core.Models;
using KGV.Helpers;
using System;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows;

namespace KGV.ViewModels
{
    public sealed class ZaehlerwechselAusbauViewModel : BaseViewModel, INavigationAware
    {
        private readonly ISupabaseService _supabaseService;
        private readonly MainWindowViewModel _mainVm;
        private readonly ParzelleDetailDTO _detail;
        private readonly string _medium;
        private string _schlussstandText = string.Empty;
        private string _statusMessage = string.Empty;
        private bool _isBusy;
        private DateTime _ausbauDatum = DateTime.Today;

        public ZaehlerwechselAusbauViewModel(ISupabaseService supabaseService, MainWindowViewModel mainVm, ParzelleDetailDTO detail, string medium)
        {
            _supabaseService = supabaseService ?? throw new ArgumentNullException(nameof(supabaseService));
            _mainVm = mainVm ?? throw new ArgumentNullException(nameof(mainVm));
            _detail = detail ?? throw new ArgumentNullException(nameof(detail));
            _medium = string.Equals(medium, "wasser", StringComparison.OrdinalIgnoreCase) ? "wasser" : "strom";

            SaveCommand = new RelayCommand<object?>(_ => _ = SaveAsync(), _ => CanSave);
            BackCommand = new RelayCommand<object?>(_ => _ = NavigateBackAsync(), _ => !IsBusy);
        }

        public RelayCommand<object?> SaveCommand { get; }
        public RelayCommand<object?> BackCommand { get; }

        public string Title => $"Zählerwechsel – {MediumDisplay}ausbau";
        public string Description => "Korrekturpfad für eine gezielt ausgewählte Parzelle. Zuerst wird die Schlussablesung gespeichert, danach wird der aktive Zähler beendet.";
        public string ParzelleDisplayName => _detail.DisplayName;
        public string MediumDisplay => string.Equals(_medium, "wasser", StringComparison.OrdinalIgnoreCase) ? "Wasser" : "Strom";
        public string AktiverZaehlerDisplay => ActiveMeterNumber is null ? "Kein aktiver Zähler" : $"{ActiveMeterNumber} seit {ActiveMeterInstalledOn:dd.MM.yyyy}";
        public bool HasStatusMessage => !string.IsNullOrWhiteSpace(StatusMessage);

        public DateTime AusbauDatum
        {
            get => _ausbauDatum;
            set => SetProperty(ref _ausbauDatum, value);
        }

        public string SchlussstandText
        {
            get => _schlussstandText;
            set => SetProperty(ref _schlussstandText, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            private set
            {
                if (!SetProperty(ref _statusMessage, value))
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

                SaveCommand.RaiseCanExecuteChanged();
                BackCommand.RaiseCanExecuteChanged();
            }
        }

        public bool CanSave => !IsBusy && ActiveMeterId > 0;

        private long ActiveMeterId => string.Equals(_medium, "wasser", StringComparison.OrdinalIgnoreCase)
            ? _detail.AktiverWasserzaehler?.Id ?? 0
            : _detail.AktiverStromzaehler?.Id ?? 0;

        private string? ActiveMeterNumber => string.Equals(_medium, "wasser", StringComparison.OrdinalIgnoreCase)
            ? _detail.AktiverWasserzaehler?.Zaehlernummer
            : _detail.AktiverStromzaehler?.Zaehlernummer;

        private DateTime ActiveMeterInstalledOn => string.Equals(_medium, "wasser", StringComparison.OrdinalIgnoreCase)
            ? _detail.AktiverWasserzaehler?.EingebautAm ?? DateTime.Today
            : _detail.AktiverStromzaehler?.EingebautAm ?? DateTime.Today;

        public Task OnNavigatedToAsync() => Task.CompletedTask;
        public Task OnNavigatedFromAsync() => Task.CompletedTask;

        private async Task SaveAsync()
        {
            if (ActiveMeterId <= 0)
            {
                StatusMessage = "Für diese Parzelle ist kein aktiver Zähler mehr vorhanden.";
                return;
            }

            if (!TryParseDecimal(SchlussstandText, out var schlussstand) || schlussstand < 0)
            {
                StatusMessage = "Bitte einen gültigen Schlussstand eingeben.";
                return;
            }

            IsBusy = true;
            try
            {
                var readingSaved = await _supabaseService.AddAblesungAsync(new AblesungInsertRecord
                {
                    ZaehlerId = ActiveMeterId,
                    ZaehlerTyp = GetZaehlerTyp(),
                    Ablesedatum = AusbauDatum.Date,
                    Stand = schlussstand,
                    FotoPfad = null,
                    Freigegeben = true
                });

                if (!readingSaved)
                {
                    StatusMessage = "Die Schlussablesung konnte nicht gespeichert werden.";
                    return;
                }

                var meterStopped = string.Equals(_medium, "wasser", StringComparison.OrdinalIgnoreCase)
                    ? await _supabaseService.SetWasserzaehlerAusgebautAmAsync(ActiveMeterId, AusbauDatum.Date)
                    : await _supabaseService.SetStromzaehlerAusgebautAmAsync(ActiveMeterId, AusbauDatum.Date);

                if (!meterStopped)
                {
                    StatusMessage = "Der aktive Zähler konnte nicht beendet werden.";
                    return;
                }

                MessageBox.Show("Zählerausbau erfolgreich gespeichert.", "Zählerwechsel", MessageBoxButton.OK, MessageBoxImage.Information);
                await NavigateBackAsync();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ausbau konnte nicht gespeichert werden: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task NavigateBackAsync()
        {
            await _mainVm.NavigateToAsync(new ZaehlerwechselScanViewModel(_supabaseService, _mainVm));
        }

        private short GetZaehlerTyp()
            => string.Equals(_medium, "wasser", StringComparison.OrdinalIgnoreCase) ? (short)2 : (short)1;

        private static bool TryParseDecimal(string? value, out decimal result)
        {
            var normalized = (value ?? string.Empty).Trim();
            if (decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.CurrentCulture, out result))
                return true;

            return decimal.TryParse(normalized.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out result);
        }
    }
}
