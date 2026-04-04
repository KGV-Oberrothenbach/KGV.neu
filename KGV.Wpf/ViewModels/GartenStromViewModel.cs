// File: ViewModels/GartenStromViewModel.cs
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using KGV.Core.Interfaces;
using KGV.Core.Models;
using KGV.Core.Security;
using KGV.Helpers;
using KGV.Views;
using System;
using System.Windows;
using System.Diagnostics;
using System.Linq;

namespace KGV.ViewModels
{
    public sealed class GartenStromViewModel : BaseViewModel, INavigationAware
    {
        private const short ZaehlerTypStrom = 1;
        private readonly ISupabaseService _supabaseService;
        private readonly MainWindowViewModel _mainVm;
        private bool _allowUserMeterReadingSubmissions;

        public ParzellenBelegungDTO Belegung { get; }

        public string GartenNr => Belegung.GartenNr;

        public ObservableCollection<ZaehlerAblesungDTO> Ablesungen { get; } = new();

        public RelayCommand<object?> ZaehlerTauschCommand { get; }
        public RelayCommand<object?> NeueAblesungCommand { get; }
        public RelayCommand<object?> AblesungBearbeitenCommand { get; }
        public RelayCommand<object?> OpenFotoCommand { get; }

        private ZaehlerAblesungDTO? _selectedAblesung;
        public ZaehlerAblesungDTO? SelectedAblesung
        {
            get => _selectedAblesung;
            set
            {
                if (_selectedAblesung == value) return;
                _selectedAblesung = value;
                OnPropertyChanged();
                AblesungBearbeitenCommand.RaiseCanExecuteChanged();
            }
        }

        public GartenStromViewModel(ISupabaseService supabaseService, ParzellenBelegungDTO belegung, MainWindowViewModel mainVm)
        {
            _supabaseService = supabaseService;
            Belegung = belegung;
            _mainVm = mainVm ?? throw new ArgumentNullException(nameof(mainVm));

            ZaehlerTauschCommand = new RelayCommand<object?>(_ => _ = ZaehlerTauschAsync(), _ => PermissionChecks.CanManageMeterChanges(_mainVm.UserContext));
            NeueAblesungCommand = new RelayCommand<object?>(_ => _ = NeueAblesungAsync(), _ => CanCreateReadings);
            AblesungBearbeitenCommand = new RelayCommand<object?>(_ => _ = AblesungBearbeitenAsync(), _ => SelectedAblesung != null && CanEditReadings);
            OpenFotoCommand = new RelayCommand<object?>(p => OpenFoto(p));
        }

        public async Task OnNavigatedToAsync()
        {
            _allowUserMeterReadingSubmissions = await _supabaseService.GetAllowUserMeterReadingSubmissionsAsync();
            RaiseCommandStates();
            await LoadAsync();
        }

        private bool CanCreateReadings
            => PermissionChecks.CanApproveMeterReadings(_mainVm.UserContext)
               || (PermissionChecks.CanSubmitOwnMeterReadings(_mainVm.UserContext) && _allowUserMeterReadingSubmissions);

        private bool CanEditReadings => PermissionChecks.CanApproveMeterReadings(_mainVm.UserContext);

        private bool SavesAsSubmission
            => PermissionChecks.CanSubmitOwnMeterReadings(_mainVm.UserContext)
               && !PermissionChecks.CanApproveMeterReadings(_mainVm.UserContext);

        private void RaiseCommandStates()
        {
            ZaehlerTauschCommand.RaiseCanExecuteChanged();
            NeueAblesungCommand.RaiseCanExecuteChanged();
            AblesungBearbeitenCommand.RaiseCanExecuteChanged();
        }

        private async Task LoadAsync()
        {
            var items = await _supabaseService.GetStromAblesungenAsync(Belegung.ParzelleId);
            Ablesungen.Clear();
            foreach (var i in items)
                Ablesungen.Add(i);
        }

        private void OpenFoto(object? parameter)
        {
            var url = parameter as string;
            if (string.IsNullOrWhiteSpace(url)) return;

            try
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch
            {
            }
        }

        private async Task NeueAblesungAsync()
        {
            if (!CanCreateReadings)
            {
                MessageBox.Show(
                    PermissionChecks.CanSubmitOwnMeterReadings(_mainVm.UserContext)
                        ? "Eigene Zählerablesungen sind aktuell nicht freigeschaltet."
                        : "Mit den aktuellen Rechten können hier keine Ablesungen erfasst werden.",
                    "Hinweis",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            var dlg = new AblesungDialog
            {
                Owner = Application.Current?.MainWindow,
                Title = $"Neue Ablesung (Strom) - Garten Nr. {GartenNr}"
            };

            if (dlg.ShowDialog() != true)
                return;

            if (!dlg.Ablesedatum.HasValue || !dlg.Stand.HasValue)
            {
                MessageBox.Show("Bitte Ablesedatum und Zählerstand angeben.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var meter = await _supabaseService.GetActiveStromzaehlerAsync(Belegung.ParzelleId, dlg.Ablesedatum.Value);
            if (meter == null)
            {
                MessageBox.Show("Kein aktiver Stromzähler für dieses Datum gefunden.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var savesAsSubmission = SavesAsSubmission;
            var ok = await _supabaseService.AddAblesungAsync(new AblesungInsertRecord
            {
                ZaehlerId = meter.Id,
                Ablesedatum = dlg.Ablesedatum.Value,
                Stand = dlg.Stand.Value,
                FotoPfad = string.IsNullOrWhiteSpace(dlg.FotoPfad) ? null : dlg.FotoPfad.Trim(),
                Freigegeben = !savesAsSubmission,
                Pruefstatus = savesAsSubmission ? AblesungPruefstatus.Eingereicht : AblesungPruefstatus.Freigegeben
            });
            if (!ok)
            {
                MessageBox.Show("Ablesung konnte nicht gespeichert werden.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (savesAsSubmission)
            {
                MessageBox.Show("Ablesung eingereicht. Sie ist noch nicht direkt freigegeben.", "Ablesung", MessageBoxButton.OK, MessageBoxImage.Information);
            }

            await LoadAsync();
        }

        private async Task AblesungBearbeitenAsync()
        {
            if (SelectedAblesung == null)
                return;

            var dlg = new AblesungDialog
            {
                Owner = Application.Current?.MainWindow,
                Title = $"Ablesung bearbeiten (Strom) - Garten Nr. {GartenNr}"
            };
            dlg.SetInitialValues(SelectedAblesung.Ablesedatum, SelectedAblesung.Stand, SelectedAblesung.FotoPfad);

            if (dlg.ShowDialog() != true)
                return;

            if (!dlg.Ablesedatum.HasValue || !dlg.Stand.HasValue)
            {
                MessageBox.Show("Bitte Ablesedatum und Zählerstand angeben.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var ok = await _supabaseService.UpdateAblesungAsync(SelectedAblesung.AblesungId, dlg.Ablesedatum.Value, dlg.Stand.Value, string.IsNullOrWhiteSpace(dlg.FotoPfad) ? null : dlg.FotoPfad.Trim());
            if (!ok)
            {
                MessageBox.Show("Ablesung konnte nicht gespeichert werden.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            await LoadAsync();
        }

        private async Task ZaehlerTauschAsync()
        {
            try
            {
                var dlg = new ZaehlerTauschDialog
                {
                    Owner = Application.Current?.MainWindow,
                    Title = $"Stromzähler tauschen (Garten Nr. {GartenNr})"
                };

                if (dlg.ShowDialog() != true)
                    return;

                if (string.IsNullOrWhiteSpace(dlg.Zaehlernummer) || !dlg.Eichdatum.HasValue || !dlg.EingebautAm.HasValue)
                {
                    MessageBox.Show("Bitte Zählernummer, Eichdatum und Einbaudatum angeben.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Strom: Ausbau- und Einbaudatum identisch
                var changeDate = dlg.EingebautAm.Value.Date;
                var current = await _supabaseService.GetActiveStromzaehlerAsync(Belegung.ParzelleId, changeDate);
                if (current != null)
                {
                    var ended = await _supabaseService.SetStromzaehlerAusgebautAmAsync(current.Id, changeDate);
                    if (!ended)
                    {
                        MessageBox.Show("Alter Zähler konnte nicht ausgebaut werden.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                }

                var ok = await _supabaseService.AddStromzaehlerAsync(new StromzaehlerInsertRecord
                {
                    ParzelleId = Belegung.ParzelleId,
                    Zaehlernummer = dlg.Zaehlernummer.Trim(),
                    Eichdatum = dlg.Eichdatum.Value,
                    EingebautAm = changeDate
                });
                if (!ok)
                {
                    MessageBox.Show("Zählertausch konnte nicht gespeichert werden.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                await LoadAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Zählertausch: {ex.Message}", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public Task OnNavigatedFromAsync() => Task.CompletedTask;
    }
}
