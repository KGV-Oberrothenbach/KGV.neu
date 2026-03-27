using System;
using System.Threading.Tasks;
using KGV.Core.Interfaces;
using KGV.Core.Models;
using KGV.Helpers;

namespace KGV.ViewModels
{
    public sealed class WartungsvertragEditorViewModel : BaseViewModel, INavigationAware
    {
        private readonly ISupabaseService _supabaseService;
        private readonly MainWindowViewModel _mainVm;
        private readonly long? _wartungsvertragId;
        private readonly BaseViewModel? _backTarget;
        private string _titel = string.Empty;
        private string _beschreibung = string.Empty;
        private string _maxKontingentText = "1";
        private bool _aktiv = true;
        private string _statusMessage = string.Empty;
        private bool _isBusy;

        public WartungsvertragEditorViewModel(ISupabaseService supabaseService, MainWindowViewModel mainVm, long? wartungsvertragId = null, BaseViewModel? backTarget = null)
        {
            _supabaseService = supabaseService ?? throw new ArgumentNullException(nameof(supabaseService));
            _mainVm = mainVm ?? throw new ArgumentNullException(nameof(mainVm));
            _wartungsvertragId = wartungsvertragId;
            _backTarget = backTarget;

            SaveCommand = new RelayCommand<object?>(_ => _ = SaveAsync(), _ => !IsBusy);
            CancelCommand = new RelayCommand<object?>(_ => _ = CancelAsync(), _ => !IsBusy);
        }

        public string PageTitle => _wartungsvertragId.HasValue ? "Wartungsvertrag bearbeiten" : "Wartungsvertrag neu";
        public string Description => _wartungsvertragId.HasValue
            ? "Produktiver Editorpfad für Titel, Beschreibung, Kontingent und Aktivstatus des ausgewählten Wartungsvertrags."
            : "Produktiver Editorpfad für einen neuen Wartungsvertrag mit Titel, Beschreibung, Kontingent und Aktivstatus.";

        public string Titel
        {
            get => _titel;
            set => SetProperty(ref _titel, value);
        }

        public string Beschreibung
        {
            get => _beschreibung;
            set => SetProperty(ref _beschreibung, value);
        }

        public string MaxKontingentText
        {
            get => _maxKontingentText;
            set => SetProperty(ref _maxKontingentText, value);
        }

        public bool Aktiv
        {
            get => _aktiv;
            set => SetProperty(ref _aktiv, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            private set => SetProperty(ref _statusMessage, value);
        }

        public bool IsBusy
        {
            get => _isBusy;
            private set
            {
                if (SetProperty(ref _isBusy, value))
                {
                    SaveCommand.RaiseCanExecuteChanged();
                    CancelCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public RelayCommand<object?> SaveCommand { get; }
        public RelayCommand<object?> CancelCommand { get; }

        public Task OnNavigatedToAsync() => LoadAsync();
        public Task OnNavigatedFromAsync() => Task.CompletedTask;

        private async Task LoadAsync()
        {
            if (IsBusy)
                return;

            IsBusy = true;
            try
            {
                StatusMessage = "Wartungsvertrag wird geladen.";
                if (!_wartungsvertragId.HasValue)
                {
                    Titel = string.Empty;
                    Beschreibung = string.Empty;
                    MaxKontingentText = "1";
                    Aktiv = true;
                    StatusMessage = string.Empty;
                    return;
                }

                var contract = await _supabaseService.GetWartungsvertragByIdAsync(_wartungsvertragId.Value);
                if (contract == null)
                {
                    StatusMessage = "Der ausgewählte Wartungsvertrag konnte nicht geladen werden.";
                    return;
                }

                Titel = contract.Titel?.Trim() ?? string.Empty;
                Beschreibung = contract.Beschreibung?.Trim() ?? string.Empty;
                MaxKontingentText = Math.Max(1, contract.MaxAktiveZuordnungen).ToString();
                Aktiv = contract.Aktiv;
                StatusMessage = string.Empty;
            }
            catch (Exception ex)
            {
                StatusMessage = $"Wartungsvertrag konnte nicht geladen werden: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task SaveAsync()
        {
            if (IsBusy)
                return;

            if (!TryBuildRecord(out var record))
                return;

            IsBusy = true;
            try
            {
                StatusMessage = "Wartungsvertrag wird gespeichert.";

                if (_wartungsvertragId.HasValue)
                {
                    record.Id = _wartungsvertragId.Value;
                    var success = await _supabaseService.UpdateWartungsvertragAsync(record);
                    if (!success)
                    {
                        StatusMessage = "Der Wartungsvertrag konnte nicht gespeichert werden.";
                        return;
                    }

                    await NavigateToDetailAsync(record.Id);
                    return;
                }

                var created = await _supabaseService.CreateWartungsvertragAsync(record.ToInsertRecord());
                if (created == null)
                {
                    StatusMessage = "Der Wartungsvertrag konnte nicht erstellt werden.";
                    return;
                }

                await NavigateToDetailAsync(created.Id);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Wartungsvertrag konnte nicht gespeichert werden: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private bool TryBuildRecord(out WartungsvertragRecord record)
        {
            record = new WartungsvertragRecord();

            if (string.IsNullOrWhiteSpace(Titel))
            {
                StatusMessage = "Titel ist ein Pflichtfeld.";
                return false;
            }

            if (!int.TryParse(MaxKontingentText?.Trim(), out var maxKontingent) || maxKontingent <= 0)
            {
                StatusMessage = "Max. Kontingent muss eine ganze Zahl größer als 0 sein.";
                return false;
            }

            record = new WartungsvertragRecord
            {
                Titel = Titel.Trim(),
                Beschreibung = string.IsNullOrWhiteSpace(Beschreibung) ? null : Beschreibung.Trim(),
                MaxAktiveZuordnungen = maxKontingent,
                Aktiv = Aktiv
            };

            return true;
        }

        private async Task CancelAsync()
        {
            if (_wartungsvertragId.HasValue)
            {
                await NavigateToDetailAsync(_wartungsvertragId.Value);
                return;
            }

            await _mainVm.NavigateToAsync(ResolveOverviewTarget());
        }

        private async Task NavigateToDetailAsync(long wartungsvertragId)
        {
            await _mainVm.NavigateToAsync(new WartungsvertragDetailViewModel(_supabaseService, _mainVm, wartungsvertragId, ResolveOverviewTarget(), true));
        }

        private BaseViewModel ResolveOverviewTarget()
            => _backTarget ?? new WartungsvertraegeVerwaltungViewModel(_supabaseService, _mainVm);
    }
}
