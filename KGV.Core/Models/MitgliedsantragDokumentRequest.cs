using System;

namespace KGV.Core.Models
{
    public sealed class MitgliedsantragDokumentRequest
    {
        public int MitgliedId { get; set; }
        public DateTime BeginnDatum { get; set; }
        public decimal Mitgliedsbeitrag { get; set; }
        public decimal? Aufnahmegebuehr { get; set; }
        public string? Status { get; set; }
        public bool IstMinderjaehrig { get; set; }
        public bool GesetzlicherVertreterAusBestehendemMitglied { get; set; }
        public bool GesetzlicherVertreterAdresseAbweichend { get; set; }
        public int? GesetzlicherVertreterMitgliedId { get; set; }
        public MitgliedsantragVertreterSnapshot? GesetzlicherVertreterSnapshot { get; set; }
        public MitgliedsantragBankverbindungSnapshot? BankverbindungSnapshot { get; set; }
    }
}
