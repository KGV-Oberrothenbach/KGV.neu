using KGV.Core.Models;
namespace KGV.Maui.State;
public sealed class BekanntmachungenUserState
{
    private readonly List<HomeAnnouncementItem> _entries = new();
    public IReadOnlyList<HomeAnnouncementItem> Entries => _entries;
    public int CurrentIndex { get; private set; } = -1;
    public int TotalCount => _entries.Count;
    public HomeAnnouncementItem? CurrentEntry => CurrentIndex >= 0 && CurrentIndex < _entries.Count ? _entries[CurrentIndex] : null;
    public bool CanMovePrevious => CurrentIndex > 0;
    public bool CanMoveNext => CurrentIndex >= 0 && CurrentIndex < _entries.Count - 1;
    public void SetEntries(IEnumerable<HomeAnnouncementItem> entries, int? selectedEntryId = null) { var prior = CurrentIndex; _entries.Clear(); _entries.AddRange(entries); if (_entries.Count == 0) { CurrentIndex = -1; return; } var index = selectedEntryId.HasValue ? _entries.FindIndex(x => x.Id == selectedEntryId) : -1; CurrentIndex = index >= 0 ? index : prior >= 0 ? Math.Clamp(prior, 0, _entries.Count - 1) : 0; }
    public bool SetCurrentById(int id) { var index = _entries.FindIndex(x => x.Id == id); if (index < 0) return false; CurrentIndex = index; return true; }
    public bool MovePrevious() { if (!CanMovePrevious) return false; CurrentIndex--; return true; }
    public bool MoveNext() { if (!CanMoveNext) return false; CurrentIndex++; return true; }
    public void Clear() { _entries.Clear(); CurrentIndex = -1; }
}
