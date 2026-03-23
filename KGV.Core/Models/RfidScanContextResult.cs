namespace KGV.Core.Models
{
    public sealed class RfidScanContextResult
    {
        public string NormalizedUid { get; set; } = string.Empty;
        public RfidScanContextState State { get; set; }
        public RfidScanContextRecord? Context { get; set; }
        public string Message { get; set; } = string.Empty;

        public bool IsKnown => State != RfidScanContextState.Unknown;
        public bool HasActiveMeter => State == RfidScanContextState.KnownWithActiveMeter;
        public string StateDisplay => State switch
        {
            RfidScanContextState.KnownWithActiveMeter => "Bekannter RFID-Tag mit aktivem Zähler",
            RfidScanContextState.KnownWithoutActiveMeter => "Bekannter RFID-Tag ohne aktiven Zähler",
            _ => "Unbekannter RFID-Tag"
        };
    }
}
