using System;

namespace KGV.Core.Models;

public sealed class PachtvertragDokumentRequest
{
    public int MitgliedId { get; set; }
    public int ParzelleId { get; set; }
    public DateTime Vertragsbeginn { get; set; }
    public string? Status { get; set; }
    public bool IstMinderjaehrig { get; set; }
    public VereinskonfigurationSnapshot? VereinskonfigurationSnapshot { get; set; }

    [Obsolete("Use VereinskonfigurationSnapshot.")]
    public MitgliedsantragBankverbindungSnapshot? BankverbindungSnapshot
    {
        get => VereinskonfigurationSnapshot as MitgliedsantragBankverbindungSnapshot;
        set => VereinskonfigurationSnapshot = value;
    }
    public DateTime? AltvertragDatum { get; set; }
        // If set, controls whether an existing Nebenmitglied should be included as second tenant (Pächter2).
        // null = keep existing default behavior on server (no explicit preference)
        public bool? IncludeSecondaryMember { get; set; }
}
