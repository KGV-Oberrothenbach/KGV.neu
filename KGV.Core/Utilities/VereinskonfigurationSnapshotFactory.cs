using KGV.Core.Models;

namespace KGV.Core.Utilities;

public static class VereinskonfigurationSnapshotFactory
{
    public static VereinskonfigurationSnapshot FromActiveConfiguration(VereinskonfigurationRecord configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        return new VereinskonfigurationSnapshot
        {
            VereinName = Clean(configuration.Vereinsname), Kurzname = Clean(configuration.Kurzname), VereinRegisterangabe = Clean(configuration.Registerangabe),
            Strasse = Clean(configuration.Strasse), Plz = Clean(configuration.Plz), Ort = Clean(configuration.Ort), VereinEmail = Clean(configuration.StandardEmail),
            StandardTelefon = Clean(configuration.StandardTelefon), Website = Clean(configuration.Website), Kontoinhaber = Clean(configuration.Kontoinhaber),
            Bankname = Clean(configuration.Bankname), Iban = Clean(configuration.Iban), Bic = Clean(configuration.Bic),
            VerwendungszweckMitgliedsantrag = Clean(configuration.VerwendungszweckMitgliedsantrag), VerwendungszweckPachtvertrag = Clean(configuration.VerwendungszweckPachtvertrag),
            DokumentOrt = Clean(configuration.DokumentOrt), StandardHinweistext = Clean(configuration.StandardHinweistext), DatenschutzText = Clean(configuration.DatenschutzText),
            DatenschutzVersion = Clean(configuration.DatenschutzVersion), DatenschutzStand = configuration.DatenschutzStand?.Date
        };
    }

    public static VereinskonfigurationSnapshot NormalizeForMembershipApplication(VereinskonfigurationSnapshot snapshot) => Normalize(snapshot, "Mitgliedsantrag", requiredMembershipFields);
    public static VereinskonfigurationSnapshot NormalizeForLeaseContract(VereinskonfigurationSnapshot snapshot) => Normalize(snapshot, "Pachtvertrag", requiredLeaseFields);

    private static readonly string[] requiredMembershipFields = ["VereinName", "VereinRegisterangabe", "VereinEmail", "Kontoinhaber", "Bankname", "Iban", "Bic", "VerwendungszweckMitgliedsantrag", "DokumentOrt", "StandardHinweistext", "DatenschutzText", "DatenschutzVersion"];
    private static readonly string[] requiredLeaseFields = ["VereinName", "VereinRegisterangabe", "Kontoinhaber", "Bankname", "Iban", "Bic", "DokumentOrt"];

    private static VereinskonfigurationSnapshot Normalize(VereinskonfigurationSnapshot snapshot, string documentName, IEnumerable<string> required)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        var normalized = new VereinskonfigurationSnapshot
        {
            VereinName = Clean(snapshot.VereinName), Kurzname = Clean(snapshot.Kurzname), VereinRegisterangabe = Clean(snapshot.VereinRegisterangabe),
            Strasse = Clean(snapshot.Strasse), Plz = Clean(snapshot.Plz), Ort = Clean(snapshot.Ort), VereinEmail = Clean(snapshot.VereinEmail),
            StandardTelefon = Clean(snapshot.StandardTelefon), Website = Clean(snapshot.Website), Kontoinhaber = Clean(snapshot.Kontoinhaber),
            Bankname = Clean(snapshot.Bankname), Iban = Clean(snapshot.Iban), Bic = Clean(snapshot.Bic),
            VerwendungszweckMitgliedsantrag = Clean(snapshot.VerwendungszweckMitgliedsantrag), VerwendungszweckPachtvertrag = Clean(snapshot.VerwendungszweckPachtvertrag),
            DokumentOrt = Clean(snapshot.DokumentOrt), StandardHinweistext = Clean(snapshot.StandardHinweistext), DatenschutzText = Clean(snapshot.DatenschutzText),
            DatenschutzVersion = Clean(snapshot.DatenschutzVersion), DatenschutzStand = snapshot.DatenschutzStand?.Date
        };
        foreach (var property in required)
            if (string.IsNullOrWhiteSpace(typeof(VereinskonfigurationSnapshot).GetProperty(property)?.GetValue(normalized) as string))
                throw new InvalidOperationException("Die aktive Vereinskonfiguration enthält für den " + documentName + " keine vollständigen Daten (" + property + ").");
        return normalized;
    }

    private static string Clean(string? value) => value?.Trim() ?? string.Empty;
}
