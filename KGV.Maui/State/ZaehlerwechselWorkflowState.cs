using KGV.Core.Models;

namespace KGV.Maui.State;

public sealed class ZaehlerwechselWorkflowState
{
    public RfidScanContextResult? CurrentContext { get; private set; }
    public PendingAblesungFlowContext? PendingAblesungFlow { get; private set; }

    public void SetContext(RfidScanContextResult? context)
    {
        CurrentContext = context;
    }

    public void SetPendingAblesungFlow(RfidScanContextResult context, string art, DateTime defaultDate, string hint)
    {
        PendingAblesungFlow = new PendingAblesungFlowContext
        {
            Context = context,
            Art = AblesungArt.Normalize(art),
            DefaultDate = defaultDate.Date,
            Hint = hint
        };
    }

    public PendingAblesungFlowContext? ConsumePendingAblesungFlow()
    {
        var flow = PendingAblesungFlow;
        PendingAblesungFlow = null;
        return flow;
    }

    public void Clear()
    {
        CurrentContext = null;
        PendingAblesungFlow = null;
    }
}

public sealed class PendingAblesungFlowContext
{
    public RfidScanContextResult Context { get; set; } = new();
    public string Art { get; set; } = AblesungArt.Normal;
    public DateTime DefaultDate { get; set; } = DateTime.Today;
    public string Hint { get; set; } = string.Empty;
    public byte[]? PendingPhotoContent { get; set; }
    public string PendingPhotoFileName { get; set; } = string.Empty;
    public string PendingPhotoContentType { get; set; } = "application/octet-stream";
}
