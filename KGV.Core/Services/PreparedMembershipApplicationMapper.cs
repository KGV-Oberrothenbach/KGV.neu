using System.Globalization;
using KGV.Core.Models;

namespace KGV.Core.Services;

public static class PreparedMembershipApplicationMapper
{
    public static PreparedMembershipApplicationDraft ToPreparedDraft(MitgliedsantragDokumentRequest request, bool isMinor)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (isMinor && request.NebenmitgliedEinbeziehen)
            throw new PreparedContractValidationException("Ein minderjähriges Mitglied kann kein Nebenmitglied im Mitgliedsantrag haben.");
        LegalRepresentativeDraft? representative = null;
        if (isMinor)
        {
            if (request.GesetzlicherVertreterAusBestehendemMitglied)
            {
                if (request.GesetzlicherVertreterMitgliedId is not > 0) throw new PreparedContractValidationException("Für den vorhandenen gesetzlichen Vertreter fehlt die Mitglieds-ID.");
                representative = new LegalRepresentativeDraft { Mode = "existing", MemberId = request.GesetzlicherVertreterMitgliedId };
            }
            else
            {
                var snapshot = request.GesetzlicherVertreterSnapshot ?? throw new PreparedContractValidationException("Für den gesetzlichen Vertreter fehlen die Daten.");
                if (string.IsNullOrWhiteSpace(snapshot.Vorname) || string.IsNullOrWhiteSpace(snapshot.Nachname)) throw new PreparedContractValidationException("Vorname und Nachname des gesetzlichen Vertreters sind erforderlich.");
                if (request.GesetzlicherVertreterAdresseAbweichend && (string.IsNullOrWhiteSpace(snapshot.Adresse) || string.IsNullOrWhiteSpace(snapshot.Plz) || string.IsNullOrWhiteSpace(snapshot.Ort))) throw new PreparedContractValidationException("Die abweichende Anschrift des gesetzlichen Vertreters ist unvollständig.");
                representative = new LegalRepresentativeDraft { Mode = "manual", MemberId = null, Vorname = snapshot.Vorname, Nachname = snapshot.Nachname, AdresseAbweichend = request.GesetzlicherVertreterAdresseAbweichend, Adresse = snapshot.Adresse, Plz = snapshot.Plz, Ort = snapshot.Ort };
            }
        }
        return new PreparedMembershipApplicationDraft { MemberId = request.MitgliedId, StartDate = request.BeginnDatum.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), MemberFee = request.Mitgliedsbeitrag, Representative = representative, IncludeSecondaryMember = request.NebenmitgliedEinbeziehen };
    }

    public static MitgliedsantragDokumentRequest ToDocumentRequest(PreparedMembershipApplicationDraft draft, bool isMinor)
    {
        ArgumentNullException.ThrowIfNull(draft);
        if (isMinor && draft.IncludeSecondaryMember)
            throw new PreparedContractValidationException("Ein minderjähriges Mitglied kann kein Nebenmitglied im Mitgliedsantrag haben.");
        if (!DateTime.TryParseExact(draft.StartDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)) throw new PreparedContractValidationException("Das Startdatum des Mitgliedsantrags ist ungültig.");
        var request = new MitgliedsantragDokumentRequest { MitgliedId = draft.MemberId, BeginnDatum = date.Date, Mitgliedsbeitrag = draft.MemberFee, NebenmitgliedEinbeziehen = draft.IncludeSecondaryMember, IstMinderjaehrig = isMinor };
        if (!isMinor || draft.Representative is null) return request;
        if (draft.Representative.Mode == "existing") { if (draft.Representative.MemberId is not > 0) throw new PreparedContractValidationException("Für den vorhandenen gesetzlichen Vertreter fehlt die Mitglieds-ID."); request.GesetzlicherVertreterAusBestehendemMitglied = true; request.GesetzlicherVertreterMitgliedId = draft.Representative.MemberId; return request; }
        if (draft.Representative.Mode != "manual") throw new PreparedContractValidationException("Der Vertretermodus wird nicht unterstützt.");
        request.GesetzlicherVertreterSnapshot = new MitgliedsantragVertreterSnapshot { Vorname = draft.Representative.Vorname, Nachname = draft.Representative.Nachname, Adresse = draft.Representative.Adresse, Plz = draft.Representative.Plz, Ort = draft.Representative.Ort };
        request.GesetzlicherVertreterAdresseAbweichend = draft.Representative.AdresseAbweichend;
        return request;
    }
}
