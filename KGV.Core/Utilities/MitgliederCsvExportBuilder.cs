using KGV.Core.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace KGV.Core.Utilities;

public static class MitgliederCsvExportBuilder
{
    public static string Build(IReadOnlyList<MitgliedRecord> members, bool operationalOnly = true)
    {
        const char sep = ';';

        static string Csv(string? value)
        {
            var s = (value ?? string.Empty).Trim();
            s = s.Replace("\r\n", " ").Replace("\n", " ").Replace("\r", " ");

            var needsQuotes = s.Contains('"') || s.Contains(sep);
            if (s.Contains('"'))
                s = s.Replace("\"", "\"\"");

            return needsQuotes ? $"\"{s}\"" : s;
        }

        static string CsvDate(DateTime? dt)
        {
            if (!dt.HasValue)
                return string.Empty;

            return dt.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        }

        var source = operationalOnly
            ? (members ?? Array.Empty<MitgliedRecord>()).Where(OperationalDataFilter.IsOperationalMember)
            : (members ?? Array.Empty<MitgliedRecord>()).AsEnumerable();

        var sb = new StringBuilder(1024);
        sb.AppendJoin(sep, new[]
        {
            "Id",
            "Vorname",
            "Nachname",
            "E-Mail",
            "Telefon",
            "Handy",
            "Adresse",
            "PLZ",
            "Ort",
            "Aktiv",
            "Role",
            "MitgliedSeit",
            "MitgliedEnde"
        });
        sb.AppendLine();

        foreach (var m in source.OrderBy(x => x.Name).ThenBy(x => x.Vorname).ThenBy(x => x.Id))
        {
            sb.Append(Csv(m.Id.ToString(CultureInfo.InvariantCulture))).Append(sep)
              .Append(Csv(m.Vorname)).Append(sep)
              .Append(Csv(m.Name)).Append(sep)
              .Append(Csv(m.Email)).Append(sep)
              .Append(Csv(m.Telefon)).Append(sep)
              .Append(Csv(m.Handy)).Append(sep)
              .Append(Csv(m.Adresse)).Append(sep)
              .Append(Csv(m.Plz)).Append(sep)
              .Append(Csv(m.Ort)).Append(sep)
              .Append(Csv(m.Aktiv ? "1" : "0")).Append(sep)
              .Append(Csv(m.Role)).Append(sep)
              .Append(Csv(CsvDate(m.MitgliedSeit))).Append(sep)
              .Append(Csv(CsvDate(m.MitgliedEnde)));

            sb.AppendLine();
        }

        return sb.ToString();
    }
}
