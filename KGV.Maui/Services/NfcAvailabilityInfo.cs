namespace KGV.Maui.Services;

public sealed class NfcAvailabilityInfo
{
    public NfcAvailabilityInfo(NfcAvailabilityState state, string message)
    {
        State = state;
        Message = message;
    }

    public NfcAvailabilityState State { get; }
    public string Message { get; }
}
