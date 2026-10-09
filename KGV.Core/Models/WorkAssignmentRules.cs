using KGV.Core.Utilities;

namespace KGV.Core.Models;

public static class WorkAssignmentRules
{
    public static (DateTime VisibleUntil, DateTime SignUpDeadline) CreateDateDefaults(DateTime assignmentDate)
    {
        var date = assignmentDate.Date;
        return (date.AddDays(14).AddHours(23).AddMinutes(59), date.AddDays(-2));
    }

    public static (DateTime VisibleFrom, DateTime VisibleUntil, DateTime SignUpDeadline) CreateDefaults(DateTime assignmentDate)
    {
        var dateDefaults = CreateDateDefaults(assignmentDate);
        return (Vereinszeit.NowToMinute(), dateDefaults.VisibleUntil, dateDefaults.SignUpDeadline);
    }

    public static (DateTime? VisibleUntil, DateTime? SignUpDeadline) RefreshNewEntryDateDefaults(
        DateTime previousAssignmentDate,
        DateTime newAssignmentDate,
        DateTime? visibleUntil,
        DateTime? signUpDeadline)
    {
        var previousDefaults = CreateDateDefaults(previousAssignmentDate);
        var nextDefaults = CreateDateDefaults(newAssignmentDate);
        return (
            visibleUntil == previousDefaults.VisibleUntil ? nextDefaults.VisibleUntil : visibleUntil,
            signUpDeadline == previousDefaults.SignUpDeadline ? nextDefaults.SignUpDeadline : signUpDeadline);
    }

    public static string? Validate(ArbeitseinsatzRecord record)
    {
        if (string.IsNullOrWhiteSpace(record.Titel)) return "Titel ist ein Pflichtfeld.";
        if (record.EndUhrzeit.HasValue && record.StartUhrzeit.HasValue && record.EndUhrzeit < record.StartUhrzeit) return "Die Endzeit darf nicht vor der Startzeit liegen.";
        if (record.StundenWert < 0 || record.MaxTeilnehmer is <= 0) return "Stundenwert oder Teilnehmerbegrenzung sind ungültig.";
        if (record.SichtbarAb.HasValue && record.SichtbarBis.HasValue && record.SichtbarBis < record.SichtbarAb) return "Sichtbar bis darf nicht vor Sichtbar ab liegen.";
        var latestDeadline = record.Datum.Date.Add(record.StartUhrzeit ?? new TimeSpan(23, 59, 0));
        if (record.AnmeldungBis.HasValue && record.AnmeldungBis > latestDeadline) return "Der Anmeldeschluss darf nicht nach Einsatzbeginn liegen.";
        return null;
    }
}
