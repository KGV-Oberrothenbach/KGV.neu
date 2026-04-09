using System;
using System.Collections.Generic;
using System.Linq;

namespace KGV.Core.Models
{
    public sealed class VereinsdokumentAbschnitt
    {
        public VereinsdokumentAbschnitt(string ueberschrift, IEnumerable<string> zeilen)
        {
            Ueberschrift = ueberschrift ?? string.Empty;
            Zeilen = zeilen?
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .ToList()
                ?? new List<string>();
        }

        public string Ueberschrift { get; }
        public IReadOnlyList<string> Zeilen { get; }
        public bool HasContent => Zeilen.Count > 0;
    }
}
