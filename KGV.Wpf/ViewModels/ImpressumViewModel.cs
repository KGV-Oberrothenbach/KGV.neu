using KGV.Core.Interfaces;
using KGV.Core.Models;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace KGV.ViewModels
{
    public sealed class ImpressumViewModel : BaseViewModel, INavigationAware
    {
        private readonly ISupabaseService _supabaseService;
        private bool _isBusy;

        public ImpressumViewModel(ISupabaseService supabaseService)
        {
            _supabaseService = supabaseService ?? throw new ArgumentNullException(nameof(supabaseService));
        }

        public string Title => "Impressum";
        public string Description => "Reiner Informationsbereich mit den statischen Vereinsangaben sowie den aktuell in Supabase hinterlegten Funktionen für Vorstand und Bauausschuss.";

        public ObservableCollection<ImpressumKontaktItem> Vorstand { get; } = new();
        public ObservableCollection<ImpressumKontaktItem> Bauausschuss { get; } = new();

        public bool IsBusy
        {
            get => _isBusy;
            private set => SetProperty(ref _isBusy, value);
        }

        public bool ShowVorstandFallback => Vorstand.Count == 0;
        public bool ShowBauausschussFallback => Bauausschuss.Count == 0;

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
                ApplyItems(Vorstand, info.Vorstand);
                ApplyItems(Bauausschuss, info.Bauausschuss);
            }
            finally
            {
                IsBusy = false;
                OnPropertyChanged(nameof(ShowVorstandFallback));
                OnPropertyChanged(nameof(ShowBauausschussFallback));
            }
        }

        private static void ApplyItems(ObservableCollection<ImpressumKontaktItem> target, System.Collections.Generic.IEnumerable<ImpressumKontaktItem> items)
        {
            target.Clear();
            foreach (var item in items)
                target.Add(item);
        }
    }
}
