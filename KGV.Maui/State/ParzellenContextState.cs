namespace KGV.Maui.State;

public sealed class ParzellenContextState
{
    public int? ContextMitgliedId { get; private set; }
    public int? SelectedParzelleId { get; private set; }
    public string? ContextTitle { get; private set; }
    public bool IsFromMemberContext { get; private set; }

    public void SetMemberContext(int mitgliedId, int? parzelleId, string? contextTitle)
    {
        ContextMitgliedId = mitgliedId > 0 ? mitgliedId : null;
        SelectedParzelleId = parzelleId is > 0 ? parzelleId : null;
        ContextTitle = string.IsNullOrWhiteSpace(contextTitle) ? null : contextTitle.Trim();
        IsFromMemberContext = ContextMitgliedId.HasValue;
    }

    public void SetSelectedParzelle(int? parzelleId)
    {
        if (!IsFromMemberContext)
            return;

        SelectedParzelleId = parzelleId is > 0 ? parzelleId : null;
    }

    public void Clear()
    {
        ContextMitgliedId = null;
        SelectedParzelleId = null;
        ContextTitle = null;
        IsFromMemberContext = false;
    }
}
