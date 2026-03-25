using KGV.Core.Models;
using System.Globalization;

namespace KGV.Maui.State;

public sealed class ArbeitseinsaetzeManagementState
{
    private readonly List<ArbeitseinsatzRecord> _entries = new();

    public IReadOnlyList<ArbeitseinsatzRecord> Entries => _entries;
    public int CurrentIndex { get; private set; } = -1;
    public int TotalCount => _entries.Count;
    public ArbeitseinsatzRecord? CurrentEntry => CurrentIndex >= 0 && CurrentIndex < _entries.Count ? _entries[CurrentIndex] : null;
    public bool CanMovePrevious => CurrentIndex > 0;
    public bool CanMoveNext => CurrentIndex >= 0 && CurrentIndex < _entries.Count - 1;

    public void SetEntries(IEnumerable<ArbeitseinsatzRecord> entries, long? selectedEntryId = null)
    {
        var previousIndex = CurrentIndex;
        var normalized = entries
            .OrderBy(x => x.Datum)
            .ThenBy(x => x.StartUhrzeit ?? TimeSpan.MaxValue)
            .ThenBy(x => x.EndUhrzeit ?? TimeSpan.MaxValue)
            .ThenBy(x => x.Titel ?? string.Empty, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

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

    public bool SetCurrentById(long entryId)
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
