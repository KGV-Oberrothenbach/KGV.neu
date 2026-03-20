using CommunityToolkit.Mvvm.Input;
using KGV.Core.Interfaces;
using KGV.Core.Models;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace KGV.ViewModels
{
    public sealed class ParzellenVerwaltungViewModel : BaseViewModel, INavigationAware
    {
        private readonly ISupabaseService _supabaseService;
        private readonly MainWindowViewModel _mainVm;
        private ParzelleVerwaltungItem? _selectedItem;
        private string _statusMessage = string.Empty;
        private bool _isBusy;

        public string Title => "Parzellenverwaltung";
        public string Description => "Zeigt die aktuell belastbar ableitbaren Parzellen mit ihren aktuellen Mitgliedsbezügen und vorhandenen Dokumentpfaden.";

        public ObservableCollection<ParzelleVerwaltungItem> Items { get; } = new();

        public ParzelleVerwaltungItem? SelectedItem
        {
            get => _selectedItem;
            set
            {
                if (!SetProperty(ref _selectedItem, value))
                    return;

                OpenMemberCommand.NotifyCanExecuteChanged();
                OpenDokumenteCommand.NotifyCanExecuteChanged();
            }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                if (!SetProperty(ref _isBusy, value))
                    return;

                RefreshCommand.NotifyCanExecuteChanged();
                OpenMemberCommand.NotifyCanExecuteChanged();
                OpenDokumenteCommand.NotifyCanExecuteChanged();
            }
        }

        public IAsyncRelayCommand RefreshCommand { get; }
        public IAsyncRelayCommand OpenMemberCommand { get; }
        public IAsyncRelayCommand OpenDokumenteCommand { get; }

        public ParzellenVerwaltungViewModel(ISupabaseService supabaseService, MainWindowViewModel mainVm)
        {
            _supabaseService = supabaseService ?? throw new ArgumentNullException(nameof(supabaseService));
            _mainVm = mainVm ?? throw new ArgumentNullException(nameof(mainVm));

            RefreshCommand = new AsyncRelayCommand(LoadAsync, () => !IsBusy);
            OpenMemberCommand = new AsyncRelayCommand(OpenMemberAsync, () => !IsBusy && SelectedItem?.MitgliedId is > 0);
            OpenDokumenteCommand = new AsyncRelayCommand(OpenDokumenteAsync, () => !IsBusy && SelectedItem != null);
        }

        public async Task OnNavigatedToAsync()
        {
            await LoadAsync();
        }

        public Task OnNavigatedFromAsync() => Task.CompletedTask;

        private async Task LoadAsync()
        {
            IsBusy = true;
            StatusMessage = string.Empty;

            try
            {
                var parzellen = await _supabaseService.GetAllParzellenAsync();
                var belegungen = await _supabaseService.GetAllParzellenBelegungenAsync();
                var mitglieder = await _supabaseService.GetMitgliederAsync();

                var mitgliederById = mitglieder.ToDictionary(x => x.Id, x => x);
                var today = DateTime.Today;
                var currentByParzelle = belegungen
                    .GroupBy(x => x.ParzelleId)
                    .Select(g => g.Where(x => IsActiveOn(x, today))
                        .OrderByDescending(x => x.VonDatum ?? DateTime.MinValue)
                        .FirstOrDefault())
                    .Where(x => x != null)
                    .ToDictionary(x => x!.ParzelleId, x => x!);

                Items.Clear();

                foreach (var parzelle in parzellen
                             .OrderBy(x => GetGartenNrSortKey(x.GartenNr))
                             .ThenBy(x => x.GartenNr, StringComparer.CurrentCultureIgnoreCase))
                {
                    currentByParzelle.TryGetValue(parzelle.Id, out var belegung);
                    mitgliederById.TryGetValue(belegung?.MitgliedId ?? 0, out var mitglied);

                    Items.Add(new ParzelleVerwaltungItem
                    {
                        ParzelleId = parzelle.Id,
                        GartenNr = parzelle.GartenNr,
                        Anlage = parzelle.Anlage,
                        MitgliedId = belegung?.MitgliedId,
                        MitgliedName = FormatMemberName(mitglied),
                        IstVergeben = belegung != null,
                        StatusText = belegung != null ? "vergeben" : "frei"
                    });
                }

                StatusMessage = Items.Count == 0
                    ? "Keine Parzellen geladen."
                    : $"{Items.Count} Parzellen geladen.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Parzellenverwaltung konnte nicht geladen werden: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task OpenMemberAsync()
        {
            if (SelectedItem?.MitgliedId is not > 0)
                return;

            var memberRecord = await _supabaseService.GetMitgliedByIdAsync(SelectedItem.MitgliedId.Value);
            if (memberRecord == null)
            {
                StatusMessage = "Zugehöriges Mitglied konnte nicht geladen werden.";
                return;
            }

            var member = ToMemberDto(memberRecord);
            _mainVm.SelectedMember = member;
            await _mainVm.NavigateToAsync(new MemberDetailViewModel(_supabaseService, _mainVm.AuthService, member));
        }

        private async Task OpenDokumenteAsync()
        {
            if (SelectedItem == null)
                return;

            var belegung = new ParzellenBelegungDTO
            {
                ParzelleId = SelectedItem.ParzelleId,
                MitgliedId = SelectedItem.MitgliedId ?? 0,
                GartenNr = SelectedItem.GartenNr,
                Anlage = SelectedItem.Anlage
            };

            await _mainVm.NavigateToAsync(new GartenDokumenteViewModel(_supabaseService, belegung));
        }

        private static MemberDTO ToMemberDto(MitgliedRecord record)
        {
            return new MemberDTO
            {
                Id = record.Id,
                Vorname = record.Vorname ?? string.Empty,
                Nachname = record.Name ?? string.Empty,
                Email = record.Email ?? string.Empty,
                Role = record.Role ?? string.Empty
            };
        }

        private static string FormatMemberName(MitgliedRecord? member)
        {
            if (member == null)
                return string.Empty;

            var name = $"{member.Vorname} {member.Name}".Trim();
            return string.IsNullOrWhiteSpace(name) ? (member.Email ?? string.Empty) : name;
        }

        private static bool IsActiveOn(ParzellenBelegungRecord belegung, DateTime date)
        {
            var onDate = date.Date;
            var von = (belegung.VonDatum ?? DateTime.MinValue).Date;
            var bis = belegung.BisDatum?.Date;
            return von <= onDate && (bis == null || bis.Value >= onDate);
        }

        private static int GetGartenNrSortKey(string? gartenNr)
        {
            if (string.IsNullOrWhiteSpace(gartenNr))
                return int.MaxValue;

            var digits = new string(gartenNr.TakeWhile(char.IsDigit).ToArray());
            return int.TryParse(digits, out var value) ? value : int.MaxValue;
        }
    }
}
