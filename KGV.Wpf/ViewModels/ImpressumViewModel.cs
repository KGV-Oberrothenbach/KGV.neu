using KGV.Core.Interfaces;
using KGV.Core.Models;
using KGV.Helpers;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace KGV.ViewModels
{
    public sealed class ImpressumViewModel : BaseViewModel, INavigationAware
    {
        private readonly ISupabaseService _supabaseService;
        private bool _isBusy;
        private string _statusMessage = string.Empty;

        public ImpressumViewModel(ISupabaseService supabaseService)
        {
            _supabaseService = supabaseService ?? throw new ArgumentNullException(nameof(supabaseService));
            OpenDatenschutzCommand = new RelayCommand<object?>(_ => OpenDatenschutz());
        }

        public string Title => "Impressum";
        public string Description => "Fester Vereinskopf mit Verantwortlichkeit sowie – falls vorhanden – weitere Vorstands- und Bauausschusskontakte aus dem bestehenden Datenpfad.";
        public string ClubName => ImpressumInfo.VereinsName;
        public string ClubRegistry => ImpressumInfo.VereinsRegister;
        public string ResponsibleName => ImpressumInfo.VerantwortlichName;
        public string ResponsibleStreet => ImpressumInfo.VerantwortlichStrasse;
        public string ResponsibleCity => ImpressumInfo.VerantwortlichOrt;
        public string ClubEmail => ImpressumInfo.VereinsEmail;
        public string DatenschutzHinweis => ImpressumInfo.DatenschutzHinweis;
        public string DatenschutzUrl => ImpressumInfo.DatenschutzUrl;

        public ObservableCollection<ImpressumKontaktItem> WeitereVorstandsmitglieder { get; } = new();
        public ObservableCollection<ImpressumKontaktItem> Bauausschussmitglieder { get; } = new();
        public ICommand OpenDatenschutzCommand { get; }

        public bool IsBusy
        {
            get => _isBusy;
            private set => SetProperty(ref _isBusy, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            private set => SetProperty(ref _statusMessage, value);
        }

        public Visibility StatusVisibility => string.IsNullOrWhiteSpace(StatusMessage)
            ? Visibility.Collapsed
            : Visibility.Visible;

        public Visibility VorstandFallbackVisibility => WeitereVorstandsmitglieder.Count == 0
            ? Visibility.Visible
            : Visibility.Collapsed;

        public Visibility BauausschussFallbackVisibility => Bauausschussmitglieder.Count == 0
            ? Visibility.Visible
            : Visibility.Collapsed;

        public Task OnNavigatedFromAsync() => Task.CompletedTask;

        public async Task OnNavigatedToAsync()
        {
            await LoadAsync();
        }

        private async Task LoadAsync()
        {
            IsBusy = true;
            try
            {
                var info = await _supabaseService.GetImpressumInfoAsync() ?? new ImpressumInfo();
                ApplyItems(WeitereVorstandsmitglieder, info.WeitereVorstandsmitglieder);
                ApplyItems(Bauausschussmitglieder, info.WeitereBauausschussmitglieder);
                StatusMessage = string.Empty;
            }
            catch (Exception ex)
            {
                ApplyItems(WeitereVorstandsmitglieder, Array.Empty<ImpressumKontaktItem>());
                ApplyItems(Bauausschussmitglieder, Array.Empty<ImpressumKontaktItem>());
                StatusMessage = "Weitere Impressumskontakte konnten aktuell nicht geladen werden.";
                Debug.WriteLine($"[KGV.Wpf] Impressum.LoadAsync failed: {ex}");
            }
            finally
            {
                IsBusy = false;
                RaiseSectionStateChanged();
            }
        }

        private void OpenDatenschutz()
        {
            try
            {
                Process.Start(new ProcessStartInfo(DatenschutzUrl) { UseShellExecute = true });
                StatusMessage = string.Empty;
            }
            catch (Exception ex)
            {
                StatusMessage = "Datenschutzerklärung konnte nicht geöffnet werden.";
                Debug.WriteLine($"[KGV.Wpf] Impressum.OpenDatenschutz failed: {ex}");
                RaiseSectionStateChanged();
            }
        }

        private static void ApplyItems(ObservableCollection<ImpressumKontaktItem> target, System.Collections.Generic.IEnumerable<ImpressumKontaktItem> items)
        {
            target.Clear();
            foreach (var item in items)
                target.Add(item);
        }

        private void RaiseSectionStateChanged()
        {
            OnPropertyChanged(nameof(StatusVisibility));
            OnPropertyChanged(nameof(VorstandFallbackVisibility));
            OnPropertyChanged(nameof(BauausschussFallbackVisibility));
        }
    }
}
