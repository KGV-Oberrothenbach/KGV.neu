using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Messaging;
using KGV.Core.Interfaces;
using KGV.Core.Models;
using KGV.Core.Security;
using KGV.Messages;

namespace KGV.ViewModels
{
    public class MemberSearchViewModel : BaseViewModel
    {
        private readonly ISupabaseService _supabaseService;
        private readonly MainWindowViewModel _mainVm;
        private readonly bool _isSelectionMode;

        public ObservableCollection<object> Results { get; } = new();
        public ObservableCollection<string> DebugMessages { get; } = new();
        public string ViewTitle => _isSelectionMode ? "Mitglied suchen" : "Mitgliedersuche";
        public MemberDTO? SelectionResult { get; private set; }
        public event EventHandler? CloseRequested;

        private string _searchText = string.Empty;
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (_searchText == value) return;
                _searchText = value;
                OnPropertyChanged(nameof(SearchText));
                UpdateFilter();
            }
        }

        private bool _searchByParzelle;
        public bool SearchByParzelle
        {
            get => _searchByParzelle;
            set
            {
                if (_searchByParzelle == value) return;
                _searchByParzelle = value;
                OnPropertyChanged(nameof(SearchByParzelle));
                _ = EnsureDataLoadedAsync();
                UpdateFilter();
            }
        }

        public ICommand SearchCommand { get; }
        public ICommand SelectCommand { get; }
        public ICommand NewMemberCommand { get; }
        public bool CanCreateMember => PermissionChecks.CanEditAllMembers(_mainVm.UserContext);

        private object? _selectedResult;
        public object? SelectedResult
        {
            get => _selectedResult;
            set
            {
                if (_selectedResult == value) return;
                _selectedResult = value;
                OnPropertyChanged(nameof(SelectedResult));
            }
        }

        private readonly System.Collections.Generic.List<MemberDTO> _allMembers;
        private readonly System.Collections.Generic.List<ParzelleRecord> _allParzellen;
        private readonly Dictionary<int, List<GartenDTO>> _gartenLookup;

        public MemberSearchViewModel(ISupabaseService supabaseService, MainWindowViewModel mainVm, bool isSelectionMode = false)
        {
            _supabaseService = supabaseService ?? throw new ArgumentNullException(nameof(supabaseService));
            _mainVm = mainVm ?? throw new ArgumentNullException(nameof(mainVm));
            _isSelectionMode = isSelectionMode;

            SearchCommand = new KGV.Helpers.RelayCommand<object?>(_ => UpdateFilter());
            SelectCommand = new KGV.Helpers.RelayCommand<object?>(_ => _ = SelectResultAsync(SelectedResult));
            NewMemberCommand = new KGV.Helpers.RelayCommand<object?>(_ => _ = OpenNewMemberAsync(), _ => CanCreateMember);

            _allMembers = new System.Collections.Generic.List<MemberDTO>();
            _allParzellen = new System.Collections.Generic.List<ParzelleRecord>();
            _gartenLookup = new Dictionary<int, List<GartenDTO>>();

            WeakReferenceMessenger.Default.Register<MemberSearchViewModel, MemberSavedMessage>(
                this,
                (r, m) => _ = r.HandleMemberSavedAsync(m));
        }

        public async Task InitializeAsync()
        {
            await EnsureDataLoadedAsync();
            UpdateFilter();
        }

        private async Task HandleMemberSavedAsync(MemberSavedMessage message)
        {
            if (message?.Member == null) return;

            if (Application.Current?.Dispatcher != null && !Application.Current.Dispatcher.CheckAccess())
            {
                await Application.Current.Dispatcher.InvokeAsync(() => ApplySavedMember(message.Member));
                return;
            }

            ApplySavedMember(message.Member);
        }

        private void ApplySavedMember(MemberDTO saved)
        {
            if (SearchByParzelle) return;

            var existing = _allMembers.FirstOrDefault(m => m.Id == saved.Id);
            if (existing == null)
            {
                existing = saved.Clone();
                _allMembers.Add(existing);
            }
            else
            {
                existing.CopyFrom(saved); // ✅ jetzt INotifyPropertyChanged -> Liste aktualisiert ohne Rebuild
            }

            // Falls gefiltert: Ergebnisliste anpassen (ohne kompletter Rebuild)
            var matches = MatchesMemberSearch(existing, SearchText);
            var inResults = Results.OfType<MemberDTO>().FirstOrDefault(m => m.Id == existing.Id);

            if (matches)
            {
                // sortiert einfügen / ggf. Position aktualisieren
                if (inResults != null) Results.Remove(inResults);
                InsertSortedMemberResult(existing);
            }
            else
            {
                if (inResults != null) Results.Remove(inResults);
            }
        }

        private void InsertSortedMemberResult(MemberDTO member)
        {
            var members = Results.OfType<MemberDTO>().ToList();

            var insertIndex = members.FindIndex(m => CompareMembers(member, m) < 0);
            if (insertIndex < 0)
            {
                Results.Add(member);
                return;
            }

            Results.Insert(insertIndex, member);
        }

        private static int CompareMembers(MemberDTO a, MemberDTO b)
        {
            var cmp = StringComparer.CurrentCultureIgnoreCase;

            var c = cmp.Compare(a.Nachname ?? string.Empty, b.Nachname ?? string.Empty);
            if (c != 0) return c;

            c = cmp.Compare(a.Vorname ?? string.Empty, b.Vorname ?? string.Empty);
            if (c != 0) return c;

            return cmp.Compare(a.Email ?? string.Empty, b.Email ?? string.Empty);
        }

        private static bool MatchesMemberSearch(MemberDTO m, string? text)
        {
            var t = (text ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(t)) return true;

            return
                (!string.IsNullOrEmpty(m.Vorname) && m.Vorname.Contains(t, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrEmpty(m.Nachname) && m.Nachname.Contains(t, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrEmpty(m.Email) && m.Email.Contains(t, StringComparison.OrdinalIgnoreCase));
        }

        private async Task EnsureDataLoadedAsync()
        {
            DebugMessages.Clear();
            DebugMessages.Add("⚡ Lade Mitglieder von Supabase...");

            try
            {
                if (_allMembers.Count == 0)
                {
                    var members = await _supabaseService.GetMitgliederAsync();
                    if (members == null)
                    {
                        DebugMessages.Add("❌ SupabaseService.GetMitgliederAsync() gab null zurück!");
                        return;
                    }

                    var parzellen = await _supabaseService.GetAllParzellenAsync();
                    var belegungen = await _supabaseService.GetAllParzellenBelegungenAsync();
                    if (parzellen != null && _allParzellen.Count == 0)
                        _allParzellen.AddRange(parzellen);

                    _gartenLookup.Clear();
                    foreach (var pair in BuildGartenLookup(parzellen ?? new List<ParzelleRecord>(), belegungen ?? new List<ParzellenBelegungRecord>()))
                        _gartenLookup[pair.Key] = pair.Value;

                    foreach (var m in members)
                    {
                        var dto = MapToDTO(m, _gartenLookup);
                        _allMembers.Add(dto);
                        DebugMessages.Add($"✅ Mitglied geladen: {dto.DisplayName}");
                    }
                }
            }
            catch (Exception ex)
            {
                DebugMessages.Add($"❌ Fehler beim Laden der Mitglieder: {ex.Message}");
            }

            if (SearchByParzelle && _allParzellen.Count == 0)
            {
                try
                {
                    var pars = await _supabaseService.GetAllParzellenAsync();
                    if (pars != null)
                    {
                        _allParzellen.AddRange(pars);
                        DebugMessages.Add($"⚡ {_allParzellen.Count} Parzellen geladen.");
                    }
                }
                catch (Exception ex)
                {
                    DebugMessages.Add($"❌ Fehler beim Laden der Parzellen: {ex.Message}");
                }
            }
        }

        private void UpdateFilter()
        {
            Results.Clear();
            var text = (SearchText ?? string.Empty).Trim();

            if (SearchByParzelle)
            {
                var matches = string.IsNullOrEmpty(text)
                    ? _allParzellen
                    : _allParzellen.Where(p =>
                        !string.IsNullOrEmpty(p.GartenNr) &&
                        p.GartenNr.Contains(text, StringComparison.OrdinalIgnoreCase));

                foreach (var p in matches
                             .OrderBy(p => GetGartenNrSortKey(p.GartenNr))
                             .ThenBy(p => p.GartenNr, StringComparer.CurrentCultureIgnoreCase))
                    Results.Add(p);
                return;
            }

            var memberMatches = string.IsNullOrEmpty(text)
                ? _allMembers
                : _allMembers.Where(m => MatchesMemberSearch(m, text));

            foreach (var m in memberMatches
                         .OrderBy(m => m.Nachname, StringComparer.CurrentCultureIgnoreCase)
                         .ThenBy(m => m.Vorname, StringComparer.CurrentCultureIgnoreCase)
                         .ThenBy(m => m.Email, StringComparer.CurrentCultureIgnoreCase))
                Results.Add(m);
        }

        private static Dictionary<int, List<GartenDTO>> BuildGartenLookup(IReadOnlyCollection<ParzelleRecord> parzellen, IReadOnlyCollection<ParzellenBelegungRecord> belegungen)
        {
            var now = DateTime.Today;
            var parzellenById = parzellen
                .Where(p => p.Id > 0)
                .ToDictionary(p => p.Id);

            return belegungen
                .Where(b => b.MitgliedId > 0 && b.ParzelleId > 0 && IsActiveBelegungOn(b, now))
                .GroupBy(b => b.MitgliedId)
                .ToDictionary(
                    g => g.Key,
                    g => g
                        .Select(b => parzellenById.TryGetValue(b.ParzelleId, out var parzelle)
                            ? new GartenDTO
                            {
                                Id = parzelle.Id,
                                Name = $"Garten {parzelle.GartenNr}",
                                Nummer = parzelle.GartenNr ?? string.Empty
                            }
                            : null)
                        .Where(x => x != null)
                        .Cast<GartenDTO>()
                        .OrderBy(x => GetGartenNrSortKey(x.Nummer))
                        .ThenBy(x => x.Nummer, StringComparer.CurrentCultureIgnoreCase)
                        .ToList());
        }

        private static bool IsActiveBelegungOn(ParzellenBelegungRecord belegung, DateTime date)
        {
            var target = date.Date;
            var start = belegung.VonDatum?.Date;
            var end = belegung.BisDatum?.Date;

            return (!start.HasValue || start.Value <= target)
                && (!end.HasValue || end.Value >= target);
        }

        private static int GetGartenNrSortKey(string? gartenNr)
        {
            if (string.IsNullOrWhiteSpace(gartenNr))
                return int.MaxValue;

            var digits = new string(gartenNr.TakeWhile(char.IsDigit).ToArray());
            return int.TryParse(digits, out var n) ? n : int.MaxValue;
        }

        // bewusst "light" – Detail lädt komplett per GetMitgliedByIdAsync
        private static MemberDTO MapToDTO(MitgliedRecord m, IReadOnlyDictionary<int, List<GartenDTO>> gartenLookup)
        {
            return new MemberDTO
            {
                Id = m.Id,
                Vorname = m.Vorname ?? string.Empty,
                Nachname = m.Name ?? string.Empty,
                Email = m.Email ?? string.Empty,
                Role = m.Role ?? string.Empty,
                IstHauptmitglied = !m.HauptmitgliedId.HasValue || m.HauptmitgliedId.Value <= 0,
                Gärten = gartenLookup.TryGetValue(m.Id, out var gaerten)
                    ? gaerten.Select(g => new GartenDTO { Id = g.Id, Name = g.Name, Nummer = g.Nummer }).ToList()
                    : new List<GartenDTO>()
            };
        }

        private async Task SelectResultAsync(object? result)
        {
            if (result == null) return;

            MemberDTO? selected = null;

            if (result is MemberDTO md)
            {
                selected = md;
            }
            else if (result is ParzelleRecord pr)
            {
                var beleg = await _supabaseService.GetCurrentBelegungForParzelleAsync(pr.Id);
                if (beleg != null)
                {
                    var members = await _supabaseService.GetMitgliederAsync();
                    var mit = members.FirstOrDefault(m => m.Id == beleg.MitgliedId);
                    if (mit != null)
                        selected = MapToDTO(mit, _gartenLookup);
                }
            }

            if (selected == null) return;

            if (_isSelectionMode)
            {
                SelectionResult = selected.Clone();
                CloseRequested?.Invoke(this, EventArgs.Empty);
                return;
            }

            // Clone, damit Liste nicht "unsaved" live mitläuft – Refresh kommt per MemberSavedMessage
            var memberForDetail = selected.Clone();

            _mainVm.SelectedMember = memberForDetail;

            var detailVm = new MemberDetailViewModel(_supabaseService, _mainVm.AuthService, _mainVm.UserContext, memberForDetail);
            await _mainVm.NavigateToAsync(detailVm);
        }

        private async Task OpenNewMemberAsync()
        {
            if (!CanCreateMember)
                return;

            var newMember = new MemberDTO
            {
                Vorname = string.Empty,
                Nachname = string.Empty,
                Email = string.Empty,
                Strasse = string.Empty,
                PLZ = string.Empty,
                Ort = string.Empty,
                Telefon = string.Empty,
                Mobilnummer = string.Empty,
                Bemerkungen = string.Empty,
                Aktiv = true,
                IstHauptmitglied = true,
                MitgliedSeit = DateTime.Today
            };

            _mainVm.SelectedMember = newMember;
            var detailVm = new MemberDetailViewModel(_supabaseService, _mainVm.AuthService, _mainVm.UserContext, newMember, isNewMode: true);
            await _mainVm.NavigateToAsync(detailVm);
        }
    }
}