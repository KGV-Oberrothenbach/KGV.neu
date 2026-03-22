using KGV.Core.Interfaces;
using KGV.Core.Models;
using KGV.Helpers;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace KGV.ViewModels
{
    public sealed class ArbeitsstundenPruefungViewModel : BaseViewModel, INavigationAware
    {
        private readonly ISupabaseService _supabaseService;
        private readonly MainWindowViewModel _mainWindowViewModel;

        public ObservableCollection<PruefungseintragItem> OffenePruefungen { get; } = new();

        private PruefungseintragItem? _selectedEintrag;
        public PruefungseintragItem? SelectedEintrag
        {
            get => _selectedEintrag;
            set
            {
                if (SetProperty(ref _selectedEintrag, value))
                    OeffnenCommand.RaiseCanExecuteChanged();
            }
        }

        public string Title => "Arbeitsstunden prüfen";
        public string EmptyText => "Aktuell liegen keine Arbeitsstunden zur Prüfung vor.";
        public bool HasEntries => OffenePruefungen.Count > 0;
        public bool ShowEmptyState => !HasEntries;

        public RelayCommand<object?> AktualisierenCommand { get; }
        public RelayCommand<object?> OeffnenCommand { get; }

        public ArbeitsstundenPruefungViewModel(ISupabaseService supabaseService, MainWindowViewModel mainWindowViewModel)
        {
            _supabaseService = supabaseService ?? throw new ArgumentNullException(nameof(supabaseService));
            _mainWindowViewModel = mainWindowViewModel ?? throw new ArgumentNullException(nameof(mainWindowViewModel));

            AktualisierenCommand = new RelayCommand<object?>(_ => _ = LoadAsync());
            OeffnenCommand = new RelayCommand<object?>(_ => _ = OeffnenAsync(), _ => SelectedEintrag != null);
        }

        public async Task OnNavigatedToAsync()
        {
            await LoadAsync();
        }

        public Task OnNavigatedFromAsync() => Task.CompletedTask;

        private async Task LoadAsync()
        {
            var entries = await _supabaseService.GetUnapprovedArbeitsstundenByMitgliedAsync();

            OffenePruefungen.Clear();
            foreach (var entry in entries)
            {
                OffenePruefungen.Add(new PruefungseintragItem
                {
                    MitgliedId = entry.MitgliedId,
                    DisplayName = $"{entry.Nachname}, {entry.Vorname}".Trim(' ', ','),
                    OffeneAnzahl = entry.Count
                });
            }

            SelectedEintrag = OffenePruefungen.FirstOrDefault();
            OnPropertyChanged(nameof(HasEntries));
            OnPropertyChanged(nameof(ShowEmptyState));
        }

        private async Task OeffnenAsync()
        {
            if (SelectedEintrag == null)
                return;

            var member = await _supabaseService.GetMitgliedByIdAsync(SelectedEintrag.MitgliedId);
            if (member == null)
                return;

            var dto = new MemberDTO
            {
                Id = member.Id,
                Vorname = member.Vorname ?? string.Empty,
                Nachname = member.Name ?? string.Empty,
                Email = member.Email ?? string.Empty,
                Role = member.Role ?? string.Empty
            };

            _mainWindowViewModel.SelectedMember = dto;
            var created = _mainWindowViewModel.NavigateToArbeitsstundenViewModel(dto);
            if (created != null)
                await _mainWindowViewModel.NavigateToAsync(created);
        }

        public sealed class PruefungseintragItem
        {
            public int MitgliedId { get; init; }
            public string DisplayName { get; init; } = string.Empty;
            public int OffeneAnzahl { get; init; }
            public string CounterText => $"{OffeneAnzahl} offen";
        }
    }
}
