using KGV.Core.Models;

namespace KGV.Maui.State;

public sealed class ZaehlerwechselWorkflowState
{
    public RfidScanContextResult? CurrentContext { get; private set; }

    public void SetContext(RfidScanContextResult? context)
    {
        CurrentContext = context;
    }

    public void Clear()
    {
        CurrentContext = null;
    }
}
