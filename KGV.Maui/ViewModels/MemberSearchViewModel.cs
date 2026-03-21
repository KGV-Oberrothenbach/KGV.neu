using KGV.Core.Interfaces;
using KGV.Core.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;

namespace KGV.Maui.ViewModels;

public class MemberSearchViewModel : INotifyPropertyChanged
{
    private readonly ISupabaseService _supabaseService;
    private readonly List<MemberSearchResultItem> _allMembers = new();
    private readonly List<ParzelleRecord> _allParzellen = new();
    private readonly Dictionary<int, MitgliedRecord> _membersById = new();

    private string _searchText = string.Empty;
    private string _statusMessage = string.Empty;
    private bool _isBusy;
    private bool _searchByParzelle;

    public MemberSearchViewModel(ISupabaseService supabaseService)
    {
        _supabaseService = supabaseService ?? throw new ArgumentNullException(nameof(supabaseService));
        SearchCommand = new Command(ApplyFilter);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<MemberSearchResultItem> Results { get; } = new();
    public ObservableCollection<string> DebugMessages { get; } = new();
    public ICommand SearchCommand { get; }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (string.Equals(_searchText, value, StringComparison.Ordinal))
                return;

            _searchText = value ?? string.Empty;
            OnPropertyChanged();
            ApplyFilter();
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set
        {
            if (string.Equals(_statusMessage, value, StringComparison.Ordinal))
                return;

            _statusMessage = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasStatusMessage));
        }
    }

    public bool HasStatusMessage => !string.IsNullOrWhiteSpace(StatusMessage);

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (_isBusy == value)
                return;

            _isBusy = value;
            OnPropertyChanged();
        }
    }

    public bool SearchByParzelle
    {
        get => _searchByParzelle;
        set
        {
            if (_searchByParzelle == value)
                return;

            _searchByParzelle = value;
            OnPropertyChanged();
            ApplyFilter();
        }
    }

    public async Task InitializeAsync()
    {
        if (_allMembers.Count > 0 || _allParzellen.Count > 0)
        {
            ApplyFilter();
            return;
        }

        try
        {
            IsBusy = true;
            DebugMessages.Clear();
            StatusMessage = "Mitglieder werden geladen...";

            var members = await _supabaseService.GetMitgliederAsync();
            _allMembers.Clear();
            _membersById.Clear();

            foreach (var member in members ?? new List<MitgliedRecord>())
            {
                _membersById[member.Id] = member;
                _allMembers.Add(MapToMemberResult(member));
            }

            var parzellen = await _supabaseService.GetAllParzellenAsync();
            _allParzellen.Clear();
            _allParzellen.AddRange(parzellen ?? new List<ParzelleRecord>());

            DebugMessages.Add($"Mitglieder geladen: {_allMembers.Count}");
            DebugMessages.Add($"Parzellen geladen: {_allParzellen.Count}");

            ApplyFilter();
            StatusMessage = _allMembers.Count == 0 ? "Keine Mitglieder gefunden." : string.Empty;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Mitgliedersuche konnte nicht geladen werden: {ex.Message}";
            DebugMessages.Add(StatusMessage);
        }
        finally
        {
            IsBusy = false;
        }
    }

    public Task<MemberSearchResultItem?> SelectResultAsync(MemberSearchResultItem? result)
    {
        if (result == null)
            return Task.FromResult<MemberSearchResultItem?>(null);

        if (result.MemberId.HasValue)
            return Task.FromResult<MemberSearchResultItem?>(result);

        return SelectMemberForParzelleAsync(result);
    }

    private void ApplyFilter()
    {
        var term = (SearchText ?? string.Empty).Trim();

        IEnumerable<MemberSearchResultItem> filtered;
        if (SearchByParzelle)
        {
            filtered = string.IsNullOrWhiteSpace(term)
                ? _allParzellen.Select(MapToParzelleResult)
                : _allParzellen
                    .Where(p => !string.IsNullOrWhiteSpace(p.GartenNr) && p.GartenNr.Contains(term, StringComparison.OrdinalIgnoreCase))
                    .Select(MapToParzelleResult);
        }
        else
        {
            filtered = string.IsNullOrWhiteSpace(term)
                ? _allMembers
                : _allMembers.Where(m =>
                    (!string.IsNullOrWhiteSpace(m.DisplayName) && m.DisplayName.Contains(term, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrWhiteSpace(m.Email) && m.Email.Contains(term, StringComparison.OrdinalIgnoreCase)));
        }

        var filteredList = filtered.ToList();

        Results.Clear();
        foreach (var item in filteredList)
            Results.Add(item);

        if ((SearchByParzelle ? _allParzellen.Count : _allMembers.Count) > 0 && Results.Count == 0)
            StatusMessage = "Keine Treffer zur aktuellen Suche.";
        else if (!string.Equals(StatusMessage, "Mitglieder werden geladen...", StringComparison.Ordinal))
            StatusMessage = string.Empty;
    }

    private async Task<MemberSearchResultItem?> SelectMemberForParzelleAsync(MemberSearchResultItem parzelleResult)
    {
        if (!parzelleResult.ParzelleId.HasValue)
            return null;

        var belegung = await _supabaseService.GetCurrentBelegungForParzelleAsync(parzelleResult.ParzelleId.Value);
        if (belegung == null)
            return null;

        if (_membersById.TryGetValue(belegung.MitgliedId, out var existingMember))
            return MapToMemberResult(existingMember);

        var member = await _supabaseService.GetMitgliedByIdAsync(belegung.MitgliedId);
        if (member == null)
            return null;

        _membersById[member.Id] = member;
        return MapToMemberResult(member);
    }

    private static MemberSearchResultItem MapToMemberResult(MitgliedRecord member)
    {
        var displayName = $"{member.Vorname} {member.Name}".Trim();
        if (string.IsNullOrWhiteSpace(displayName))
            displayName = member.Email ?? $"Mitglied {member.Id}";

        return new MemberSearchResultItem(
            member.Id,
            null,
            displayName,
            member.Email ?? string.Empty,
            displayName,
            member.Email ?? string.Empty);
    }

    private static MemberSearchResultItem MapToParzelleResult(ParzelleRecord parzelle)
    {
        var gartenNr = string.IsNullOrWhiteSpace(parzelle.GartenNr) ? parzelle.Id.ToString() : parzelle.GartenNr;
        return new MemberSearchResultItem(
            null,
            parzelle.Id,
            $"Garten {gartenNr}",
            string.Empty,
            $"Garten {gartenNr}",
            "Parzelle");
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public sealed record MemberSearchResultItem(int? MemberId, int? ParzelleId, string DisplayName, string Email, string Title, string Subtitle);
