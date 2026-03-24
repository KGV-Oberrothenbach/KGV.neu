namespace KGV.Maui.State;

public sealed class ParzellenContextState
{
    public int? SelectedParzelleId { get; private set; }
    public string? ContextTitle { get; private set; }
    public bool IsFromMemberContext { get; private set; }

    public void SetMemberContext(int parzelleId, string? contextTitle)
    {
        SelectedParzelleId = parzelleId;
        ContextTitle = string.IsNullOrWhiteSpace(contextTitle) ? null : contextTitle.Trim();
        IsFromMemberContext = true;
    }

    public void Clear()
    {
        SelectedParzelleId = null;
        ContextTitle = null;
        IsFromMemberContext = false;
    }
}
