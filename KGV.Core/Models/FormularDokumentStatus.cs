namespace KGV.Core.Models
{
    public static class FormularDokumentStatus
    {
        public const string Unsigniert = "unsigniert";
        public const string Signiert = "signiert";

        public static string Normalize(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return Unsigniert;

            return value.Trim().ToLowerInvariant() switch
            {
                Signiert => Signiert,
                _ => Unsigniert
            };
        }

        public static string ToDisplayName(string? value)
        {
            return Normalize(value) switch
            {
                Signiert => "Signiert",
                _ => "Unsigniert"
            };
        }
    }
}
