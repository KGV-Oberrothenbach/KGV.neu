using System;
using KGV.Core.Models;

namespace KGV.Core.Utilities;

public static class GesetzlicherVertreterResolver
{
    public static GesetzlicherVertreterAufloesung Resolve(MitgliedRecord? mitglied, MitgliedGesetzlicherVertreterRecord? aktiveVertretung, MitgliedRecord? vertreterMitglied, DateTime? stichtag = null)
    {
        var istMinderjaehrig = IsMinderjaehrig(mitglied, stichtag);
        var vorbelegung = BuildVorbelegung(vertreterMitglied);

        return new GesetzlicherVertreterAufloesung
        {
            MitgliedId = mitglied?.Id ?? 0,
            IstMinderjaehrig = istMinderjaehrig,
            AktiveVertretung = aktiveVertretung,
            VertreterMitglied = vertreterMitglied,
            Vorbelegung = vorbelegung
        };
    }

    public static bool IsMinderjaehrig(MitgliedRecord? mitglied, DateTime? stichtag = null)
    {
        if (mitglied?.Geburtsdatum == null)
            return false;

        var referenceDate = (stichtag ?? DateTime.Today).Date;
        var birthDate = mitglied.Geburtsdatum.Value.Date;
        var age = referenceDate.Year - birthDate.Year;
        if (birthDate > referenceDate.AddYears(-age))
            age--;

        return age < 18;
    }

    private static GesetzlicherVertreterVorbelegung? BuildVorbelegung(MitgliedRecord? vertreterMitglied)
    {
        if (vertreterMitglied == null || vertreterMitglied.Id <= 0)
            return null;

        return new GesetzlicherVertreterVorbelegung
        {
            VertreterMitgliedId = vertreterMitglied.Id,
            Vorname = Clean(vertreterMitglied.Vorname),
            Nachname = Clean(vertreterMitglied.Name),
            Adresse = Clean(vertreterMitglied.Adresse),
            Plz = Clean(vertreterMitglied.Plz),
            Ort = Clean(vertreterMitglied.Ort),
            Telefon = Clean(vertreterMitglied.Telefon),
            Handy = Clean(vertreterMitglied.Handy),
            Email = Clean(vertreterMitglied.Email)
        };
    }

    private static string Clean(string? value)
        => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
}