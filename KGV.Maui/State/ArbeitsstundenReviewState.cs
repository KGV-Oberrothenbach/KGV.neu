using KGV.Core.Models;

namespace KGV.Maui.State;

public sealed class ArbeitsstundenReviewState
{
    private readonly List<ArbeitsstundeDTO> _entries = new();

    public IReadOnlyList<ArbeitsstundeDTO> Entries => _entries;
    public int CurrentIndex { get; private set; } = -1;
    public int TotalCount => _entries.Count;
    public ArbeitsstundeDTO? CurrentEntry => CurrentIndex >= 0 && CurrentIndex < _entries.Count ? _entries[CurrentIndex] : null;
    public bool CanMovePrevious => CurrentIndex > 0;
    public bool CanMoveNext => CurrentIndex >= 0 && CurrentIndex < _entries.Count - 1;

    public void SetEntries(IEnumerable<ArbeitsstundeDTO> entries, int? selectedEntryId = null)
    {
        var previousIndex = CurrentIndex;
        var normalized = entries
            .Where(IsOpenReviewCase)
            .OrderBy(x => x.Datum)
            .ThenBy(x => x.Id)
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

    public static bool IsOpenReviewCase(ArbeitsstundeDTO? entry)
    {
        if (entry == null || entry.Freigegeben)
            return false;

        var status = NormalizeStatus(entry.Status);
        return !status.StartsWith("abgelehnt", StringComparison.OrdinalIgnoreCase)
            && !status.StartsWith("genehmigt", StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeStatus(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }
}
