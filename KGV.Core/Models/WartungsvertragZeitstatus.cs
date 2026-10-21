using System;

namespace KGV.Core.Models;

public enum WartungsvertragZeitstatus
{
    Aktuell,
    Zukuenftig,
    Historisch
}

public static class WartungsvertragZeitmodell
{
    public static WartungsvertragZeitstatus Klassifiziere(DateTime gueltigAb, DateTime? gueltigBis, DateTime stichtag)
    {
        var today = stichtag.Date;
        if (gueltigAb.Date > today)
            return WartungsvertragZeitstatus.Zukuenftig;
        return gueltigBis.HasValue && gueltigBis.Value.Date < today
            ? WartungsvertragZeitstatus.Historisch
            : WartungsvertragZeitstatus.Aktuell;
    }

    public static string Bezeichnung(WartungsvertragZeitstatus status) => status switch
    {
        WartungsvertragZeitstatus.Aktuell => "Aktuell",
        WartungsvertragZeitstatus.Zukuenftig => "Zukünftig",
        _ => "Historisch"
    };
}
