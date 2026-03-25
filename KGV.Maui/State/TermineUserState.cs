using KGV.Core.Models;

namespace KGV.Maui.State;

public sealed class TermineUserState
{
    private readonly List<HomeAppointmentItem> _entries = new();

    public IReadOnlyList<HomeAppointmentItem> Entries => _entries;
    public int CurrentIndex { get; private set; } = -1;
    public int TotalCount => _entries.Count;
    public HomeAppointmentItem? CurrentEntry => CurrentIndex >= 0 && CurrentIndex < _entries.Count ? _entries[CurrentIndex] : null;
    public bool CanMovePrevious => CurrentIndex > 0;
    public bool CanMoveNext => CurrentIndex >= 0 && CurrentIndex < _entries.Count - 1;

    public void SetEntries(IEnumerable<HomeAppointmentItem> entries, int? selectedEntryId = null)
    {
        var previousIndex = CurrentIndex;
        var normalized = entries.ToList();

        _entries.Clear();
        _entries.AddRange(normalized);

        if (_entries.Count == 0)
        {
            CurrentIndex = -1;
            return;
        }

        if (selectedEntryId.HasValue)
        {
            var selectedIndex = _entries.FindIndex(x => x.Id == selectedEntryId.Value);
            if (selectedIndex >= 0)
            {
                CurrentIndex = selectedIndex;
                return;
            }
        }

        CurrentIndex = previousIndex >= 0
            ? Math.Clamp(previousIndex, 0, _entries.Count - 1)
            : 0;
    }

    public bool SetCurrentById(int entryId)
    {
        var index = _entries.FindIndex(x => x.Id == entryId);
        if (index < 0)
            return false;

        CurrentIndex = index;
        return true;
    }

    public bool MovePrevious()
    {
        if (!CanMovePrevious)
            return false;

        CurrentIndex--;
        return true;
    }

    public bool MoveNext()
    {
        if (!CanMoveNext)
            return false;

        CurrentIndex++;
        return true;
    }

    public void Clear()
    {
        _entries.Clear();
        CurrentIndex = -1;
    }
}
