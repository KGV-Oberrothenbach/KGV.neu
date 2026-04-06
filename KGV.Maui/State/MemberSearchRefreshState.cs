namespace KGV.Maui.State;

public sealed class MemberSearchRefreshState
{
    private bool _reloadRequested;

    public void RequestReload()
    {
        _reloadRequested = true;
    }

    public bool ConsumeReloadRequest()
    {
        var requested = _reloadRequested;
        _reloadRequested = false;
        return requested;
    }
}
