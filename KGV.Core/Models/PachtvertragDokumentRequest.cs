using System;

namespace KGV.Core.Models;

public sealed class PachtvertragDokumentRequest
{
    public int MitgliedId { get; set; }
    public int ParzelleId { get; set; }
    /// <summary>Optionaler bestehender Signaturvorgang; dient bei der Vorschau ausschließlich zum Lesen einer bereits reservierten Fachnummer.</summary>
    public long KommunikationsvorgangId { get; set; }
    /// <summary>Bereits reservierte 20xxx; null lässt eine Vorschau bewusst ohne Ersatznummer.</summary>
    public int? Vertragsnummer { get; set; }
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
    public bool? HasPreviousContract { get; set; }
    public DateTime? UebergabeDatum { get; set; }
    /// <summary>Snapshot des bei der Vertragserstellung ausgehändigten Vereinsunterlagen-Nachweises. Bis zur zentralen Historie optional.</summary>
    public string? VereinsunterlagenNachweis { get; set; }
    public IReadOnlyList<VereinsunterlagenNachweisSnapshotItem>? VereinsunterlagenSnapshot { get; set; }
    public DateTime? BefristetBis { get; set; }
    public string? KleingartenordnungFassung { get; set; }
    public DateTime? KleingartenordnungAusgehaendigtAm { get; set; }
    public string? WeitereVereinbarungen { get; set; }
    public bool Anlage1 { get; set; }
    public bool Anlage2 { get; set; }
    public bool Anlage3 { get; set; }
    public bool Anlage4 { get; set; }
    public string? VereinsUnterzeichner { get; set; }
    // If set, controls whether an existing Nebenmitglied should be included as second tenant (Pächter2).
    // null = keep existing default behavior on server (no explicit preference)
    public bool? IncludeSecondaryMember { get; set; }
    public int? SecondaryMemberId { get; set; }
    public DateTime? EffectiveDate { get; set; }
}
