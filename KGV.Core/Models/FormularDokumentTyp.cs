namespace KGV.Core.Models
{
    public static class FormularDokumentTyp
    {
        public const string Mitgliedsantrag = "mitgliedsantrag";
        public const string Mitgliedsvertrag = "mitgliedsvertrag";
        public const string Pachtvertrag = "pachtvertrag";

        public static string Normalize(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            return value.Trim().ToLowerInvariant() switch
            {
                Mitgliedsantrag => Mitgliedsantrag,
                Mitgliedsvertrag => Mitgliedsvertrag,
                Pachtvertrag => Pachtvertrag,
                _ => string.Empty
            };
        }

        public static string ToDisplayName(string? value)
        {
            return Normalize(value) switch
            {
                Mitgliedsantrag => "Mitgliedsantrag",
                Mitgliedsvertrag => "Mitgliedsvertrag",
                Pachtvertrag => "Pachtvertrag",
                _ => "-"
            };
        }
    }
}
