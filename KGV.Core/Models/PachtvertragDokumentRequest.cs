using System;

namespace KGV.Core.Models;

public sealed class PachtvertragDokumentRequest
{
    public int MitgliedId { get; set; }
    public int ParzelleId { get; set; }
    public DateTime Vertragsbeginn { get; set; }
    public string? Status { get; set; }
    public bool IstMinderjaehrig { get; set; }
    public bool GesetzlicherVertreterAusBestehendemMitglied { get; set; }
    public bool GesetzlicherVertreterAdresseAbweichend { get; set; }
    public int? GesetzlicherVertreterMitgliedId { get; set; }
    public MitgliedsantragVertreterSnapshot? GesetzlicherVertreterSnapshot { get; set; }
    public MitgliedsantragBankverbindungSnapshot? BankverbindungSnapshot { get; set; }
    public DateTime? AltvertragDatum { get; set; }
        // If set, controls whether an existing Nebenmitglied should be included as second tenant (Pächter2).
        // null = keep existing default behavior on server (no explicit preference)
        public bool? IncludeSecondaryMember { get; set; }
}