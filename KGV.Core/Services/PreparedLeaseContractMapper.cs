using System.Globalization;
using KGV.Core.Models;

namespace KGV.Core.Services;

public static class PreparedLeaseContractMapper
{
    public static PreparedLeaseContractDraft ToPreparedDraft(PachtvertragDokumentRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.MitgliedId <= 0 || request.ParzelleId <= 0) throw new PreparedContractValidationException("Mitglied und Parzelle sind erforderlich.");
        if (request.HasPreviousContract is null) throw new PreparedContractValidationException("Bitte entscheiden Sie, ob ein Altvertrag vorhanden ist.");
        if (request.HasPreviousContract == true && request.AltvertragDatum is null) throw new PreparedContractValidationException("Für den Altvertrag fehlt das Datum.");
        var includeSecondaryMember = !request.IstMinderjaehrig && request.IncludeSecondaryMember == true;
        if (includeSecondaryMember && request.SecondaryMemberId is not > 0) throw new PreparedContractValidationException("Für Pächter 2 fehlt das Nebenmitglied.");
        var start = Date(request.Vertragsbeginn, "Vertragsbeginn");
        return new PreparedLeaseContractDraft
        {
            MemberId = request.MitgliedId,
            VereinsunterlagenNachweis = request.VereinsunterlagenSnapshot,
            Draft = new LeaseContractDraft
            {
                StartDate = start, ParcelId = request.ParzelleId, HasPreviousContract = request.HasPreviousContract,
                PreviousContractDate = request.HasPreviousContract == true ? Date(request.AltvertragDatum!.Value, "Altvertragsdatum") : null,
                IncludeSecondaryMember = includeSecondaryMember,
                SecondaryMemberId = includeSecondaryMember ? request.SecondaryMemberId : null,
                EffectiveDate = request.IstMinderjaehrig || request.EffectiveDate is null ? null : Date(request.EffectiveDate.Value, "Wirksamkeitsdatum"),
                HandoverDate = Date(request.UebergabeDatum ?? request.Vertragsbeginn, "Übergabedatum"),
                FixedUntil = OptionalDate(request.BefristetBis), GardenRulesVersion = request.KleingartenordnungFassung ?? "",
                GardenRulesHandedOverAt = OptionalDate(request.KleingartenordnungAusgehaendigtAm), AdditionalAgreements = request.WeitereVereinbarungen ?? "",
                Attachment1 = request.Anlage1, Attachment2 = request.Anlage2, Attachment3 = request.Anlage3, Attachment4 = request.Anlage4,
                ClubSignatory = request.VereinsUnterzeichner ?? ""
            }
        };
    }

    public static PachtvertragDokumentRequest ToDocumentRequest(PreparedLeaseContractDraft draft, bool isMinor)
    {
        ArgumentNullException.ThrowIfNull(draft);
        var d = draft.Draft ?? throw new PreparedContractValidationException("Der Pachtvertragsentwurf fehlt.");
        if (draft.MemberId <= 0 || d.ParcelId is not > 0 || d.HasPreviousContract is null) throw new PreparedContractValidationException("Der Pachtvertragsentwurf ist unvollständig.");
        var request = new PachtvertragDokumentRequest { MitgliedId = draft.MemberId, ParzelleId = d.ParcelId.Value, Vertragsbeginn = Parse(d.StartDate, "Vertragsbeginn"), IstMinderjaehrig = isMinor, HasPreviousContract = d.HasPreviousContract, AltvertragDatum = d.HasPreviousContract == true ? Parse(d.PreviousContractDate, "Altvertragsdatum") : null, UebergabeDatum = Parse(d.HandoverDate, "Übergabedatum"), BefristetBis = OptionalParse(d.FixedUntil), KleingartenordnungFassung = d.GardenRulesVersion, KleingartenordnungAusgehaendigtAm = OptionalParse(d.GardenRulesHandedOverAt), WeitereVereinbarungen = d.AdditionalAgreements, Anlage1 = d.Attachment1, Anlage2 = d.Attachment2, Anlage3 = d.Attachment3, Anlage4 = d.Attachment4, VereinsUnterzeichner = d.ClubSignatory, IncludeSecondaryMember = !isMinor && d.IncludeSecondaryMember, SecondaryMemberId = !isMinor && d.IncludeSecondaryMember ? d.SecondaryMemberId : null, EffectiveDate = isMinor ? null : OptionalParse(d.EffectiveDate), VereinsunterlagenSnapshot = draft.VereinsunterlagenNachweis, VereinsunterlagenNachweis = draft.VereinsunterlagenNachweis is null ? null : VereinsunterlagenNachweisHelper.FormatForPdf(draft.VereinsunterlagenNachweis, PreparedContractConstants.LeaseContract) };
        return request;
    }

    private static string Date(DateTime value, string name) => value == default ? throw new PreparedContractValidationException($"{name} ist ungültig.") : value.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    private static string OptionalDate(DateTime? value) => value is null ? "" : Date(value.Value, "Datum");
    private static DateTime Parse(string? value, string name) => DateTime.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) ? date.Date : throw new PreparedContractValidationException($"{name} ist ungültig.");
    private static DateTime? OptionalParse(string? value) => string.IsNullOrWhiteSpace(value) ? null : Parse(value, "Datum");
}
