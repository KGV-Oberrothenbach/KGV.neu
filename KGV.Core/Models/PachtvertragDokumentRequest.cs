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
}