using KGV.Core.Interfaces;
using KGV.Core.Models;
using KGV.Helpers;
using System;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows;

namespace KGV.ViewModels
{
    public sealed class ZaehlerwechselEinbauViewModel : BaseViewModel, INavigationAware
    {
        private readonly ISupabaseService _supabaseService;
        private readonly MainWindowViewModel _mainVm;
        private readonly ParzelleDetailDTO _detail;
        private readonly string _medium;
        private string _zaehlernummer = string.Empty;
        private string _anfangsstandText = string.Empty;
        private string _statusMessage = string.Empty;
        private bool _isBusy;
        private DateTime _einbauDatum = DateTime.Today;
        private DateTime _eichdatum = DateTime.Today;

        public ZaehlerwechselEinbauViewModel(ISupabaseService supabaseService, MainWindowViewModel mainVm, ParzelleDetailDTO detail, string medium)
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

        public string Title => $"Zählerwechsel – {MediumDisplay}einbau";
        public string Description => "Korrekturpfad für eine gezielt ausgewählte Parzelle ohne aktiven Zähler. Es wird ein neuer Zähler angelegt und direkt mit Anfangsstand gespeichert.";
        public string ParzelleDisplayName => _detail.DisplayName;
        public string MediumDisplay => string.Equals(_medium, "wasser", StringComparison.OrdinalIgnoreCase) ? "Wasser" : "Strom";
        public bool HasStatusMessage => !string.IsNullOrWhiteSpace(StatusMessage);

        public DateTime EinbauDatum
        {
            get => _einbauDatum;
            set => SetProperty(ref _einbauDatum, value);
        }

        public DateTime Eichdatum
        {
            get => _eichdatum;
            set => SetProperty(ref _eichdatum, value);
        }

        public string Zaehlernummer
        {
            get => _zaehlernummer;
            set => SetProperty(ref _zaehlernummer, value);
        }

        public string AnfangsstandText
        {
            get => _anfangsstandText;
            set => SetProperty(ref _anfangsstandText, value);
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

        public bool CanSave => !IsBusy;

        public Task OnNavigatedToAsync() => Task.CompletedTask;
        public Task OnNavigatedFromAsync() => Task.CompletedTask;

        private async Task SaveAsync()
        {
            if (string.IsNullOrWhiteSpace(Zaehlernummer))
            {
                StatusMessage = "Bitte eine Zählernummer eingeben.";
                return;
            }

            if (!TryParseDecimal(AnfangsstandText, out var anfangsstand) || anfangsstand < 0)
            {
                StatusMessage = "Bitte einen gültigen Anfangsstand eingeben.";
                return;
            }

            IsBusy = true;
            try
            {
                var meterCreated = string.Equals(_medium, "wasser", StringComparison.OrdinalIgnoreCase)
                    ? await _supabaseService.AddWasserzaehlerAsync(new WasserzaehlerInsertRecord
                    {
                        ParzelleId = _detail.ParzelleId,
                        Zaehlernummer = Zaehlernummer.Trim(),
                        Eichdatum = Eichdatum.Date,
                        EingebautAm = EinbauDatum.Date
                    })
                    : await _supabaseService.AddStromzaehlerAsync(new StromzaehlerInsertRecord
                    {
                        ParzelleId = _detail.ParzelleId,
                        Zaehlernummer = Zaehlernummer.Trim(),
                        Eichdatum = Eichdatum.Date,
                        EingebautAm = EinbauDatum.Date
                    });

                if (!meterCreated)
                {
                    StatusMessage = "Der neue Zähler konnte nicht angelegt werden.";
                    return;
                }

                var activeMeterId = await ResolveNewMeterIdAsync();
                if (activeMeterId <= 0)
                {
                    StatusMessage = "Der neu angelegte Zähler konnte für die Anfangsablesung nicht aufgelöst werden.";
                    return;
                }

                var readingSaved = await _supabaseService.AddAblesungAsync(new AblesungInsertRecord
                {
                    ZaehlerId = activeMeterId,
                    ZaehlerTyp = GetZaehlerTyp(),
                    Ablesedatum = EinbauDatum.Date,
                    Stand = anfangsstand,
                    FotoPfad = null,
                    Freigegeben = true
                });

                if (!readingSaved)
                {
                    StatusMessage = "Die Anfangsablesung konnte nicht gespeichert werden.";
                    return;
                }

                MessageBox.Show("Zählereinbau erfolgreich gespeichert.", "Zählerwechsel", MessageBoxButton.OK, MessageBoxImage.Information);
                await NavigateBackAsync();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Einbau konnte nicht gespeichert werden: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task<long> ResolveNewMeterIdAsync()
        {
            if (string.Equals(_medium, "wasser", StringComparison.OrdinalIgnoreCase))
            {
                var meter = await _supabaseService.GetActiveWasserzaehlerAsync(_detail.ParzelleId, EinbauDatum.Date);
                if (meter?.Id > 0 && string.Equals(meter.Zaehlernummer?.Trim(), Zaehlernummer.Trim(), StringComparison.OrdinalIgnoreCase))
                    return meter.Id;

                return meter?.Id ?? 0;
            }

            var stromMeter = await _supabaseService.GetActiveStromzaehlerAsync(_detail.ParzelleId, EinbauDatum.Date);
            if (stromMeter?.Id > 0 && string.Equals(stromMeter.Zaehlernummer?.Trim(), Zaehlernummer.Trim(), StringComparison.OrdinalIgnoreCase))
                return stromMeter.Id;

            return stromMeter?.Id ?? 0;
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
