using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using KGV.Core.Interfaces;
using KGV.Core.Security;
using Newtonsoft.Json;
using Supabase;

namespace KGV.Maui.Services;

// AP-02.3A: nur lesende Schattenrechte; keine Client-Autorisierung.
public sealed class FachrechtV2Record
{
    [JsonProperty("code")]
    public string? Code { get; set; }

    [JsonProperty("gruppe")]
    public string? Gruppe { get; set; }

    [JsonProperty("titel")]
    public string? Titel { get; set; }

    [JsonProperty("erlaubt")]
    public bool Erlaubt { get; set; }

    [JsonProperty("individuell_entzogen")]
    public bool IndividuellEntzogen { get; set; }

    [JsonProperty("quellen")]
    public string[]? Quellen { get; set; }
}

public sealed class FachrechteV2SnapshotService
{
    private static readonly HashSet<string> KnownCodes = typeof(FachrechteV2)
        .GetFields(BindingFlags.Public | BindingFlags.Static)
        .Where(field => field.IsLiteral && field.FieldType == typeof(string))
        .Select(field => (string)field.GetRawConstantValue()!)
        .ToHashSet(StringComparer.Ordinal);

    private readonly ISupabaseClientFactory _clientFactory;

    public FachrechteV2SnapshotService(ISupabaseClientFactory clientFactory)
        => _clientFactory = clientFactory ?? throw new ArgumentNullException(nameof(clientFactory));

    public async Task<IReadOnlyList<FachrechtV2Record>> LoadAsync()
    {
        Client client = await _clientFactory.CreateAsync();
        var rows = await client.Rpc<FachrechtV2Record[]>("ap02_preview_effective_rights", new { });
        if (rows == null || rows.Length != KnownCodes.Count)
            throw new InvalidOperationException("Fachrechte 2.0 konnten nicht vollstaendig geladen werden.");

        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var row in rows)
        {
            if (row == null || string.IsNullOrWhiteSpace(row.Code)
                || !KnownCodes.Contains(row.Code) || !seen.Add(row.Code)
                || row.Gruppe == null || row.Titel == null || row.Quellen == null)
                throw new InvalidOperationException("Fachrechte-Antwort entspricht nicht dem Katalog.");
        }
        return Array.AsReadOnly(rows);
    }
}
