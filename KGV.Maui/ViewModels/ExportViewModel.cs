using KGV.Core.Interfaces;
using KGV.Core.Models;
using KGV.Maui.State;
// using System.Diagnostics removed to avoid Switch type ambiguity in MAUI pages; use fully qualified calls where needed
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace KGV.Maui.ViewModels
{
    public class ExportViewModel
    {
        private readonly ISupabaseService _supabaseService;
        // Enable extended export diagnostics
        private const bool EXPORT_DIAG = true;

        public ObservableCollection<AppExportDefinitionRecord> Definitions { get; } = new ObservableCollection<AppExportDefinitionRecord>();
        public ObservableCollection<AppExportFilterDefinitionRecord> Filters { get; } = new ObservableCollection<AppExportFilterDefinitionRecord>();
        public ObservableCollection<AppExportColumnDefinitionRecord> Columns { get; } = new ObservableCollection<AppExportColumnDefinitionRecord>();
        public ObservableCollection<JsonElement> Results { get; } = new ObservableCollection<JsonElement>();
        public ObservableCollection<Dictionary<string, string>> ProcessedResults { get; } = new ObservableCollection<Dictionary<string, string>>();

        // Visible columns in display order (respecting sort column moved to front if applicable)
        public List<AppExportColumnDefinitionRecord> ColumnsVisibleOrdered { get; private set; } = new List<AppExportColumnDefinitionRecord>();
        // Visible columns with their canonical lookup key used for ProcessedResults and all views/exports
        public List<(AppExportColumnDefinitionRecord Column, string CanonicalKey)> VisibleColumnsMapped { get; private set; } = new List<(AppExportColumnDefinitionRecord, string)>();

        public int CurrentIndex { get; private set; } = -1;
        public Dictionary<string, string>? CurrentRecord => CurrentIndex >= 0 && CurrentIndex < ProcessedResults.Count ? ProcessedResults[CurrentIndex] : null;

        public AppExportDefinitionRecord? SelectedDefinition { get; set; }
        public Dictionary<string, object?> FilterValues { get; } = new Dictionary<string, object?>();
        // Diagnostics
        public string? LastRpcName { get; private set; }
        public string? LastRpcParameterSummary { get; private set; }
        public int LastRpcRowCount { get; private set; }
        public string? LastRpcError { get; private set; }

        public ExportViewModel(ISupabaseService supabaseService)
        {
            _supabaseService = supabaseService;
        }

        // Central mapper: converts RPC JsonElement rows into canonical export rows keyed by technical column keys
        private static List<Dictionary<string, string>> MapRpcRowsToCanonical(List<System.Text.Json.JsonElement> rows, List<(AppExportColumnDefinitionRecord Column, string CanonicalKey)> visibleColumns)
        {
            var result = new List<Dictionary<string, string>>();
            if (rows == null || visibleColumns == null) return result;

            foreach (var row in rows)
            {
                var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                if (row.ValueKind == JsonValueKind.Object)
                {
                    // build direct property map
                    var propMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var prop in row.EnumerateObject())
                    {
                        try
                        {
                            if (prop.Value.ValueKind == JsonValueKind.String)
                                propMap[prop.Name] = prop.Value.GetString() ?? string.Empty;
                            else if (prop.Value.ValueKind == JsonValueKind.Number)
                                propMap[prop.Name] = prop.Value.ToString();
                            else if (prop.Value.ValueKind == JsonValueKind.True)
                                propMap[prop.Name] = "Ja";
                            else if (prop.Value.ValueKind == JsonValueKind.False)
                                propMap[prop.Name] = "Nein";
                            else
                                propMap[prop.Name] = prop.Value.ToString();
                        }
                        catch
                        {
                            propMap[prop.Name] = prop.Value.ToString();
                        }
                    }

                    // for each visible column, pick value by technical key only (strict)
                    foreach (var (col, tech) in visibleColumns)
                    {
                        var val = string.Empty;
                        if (!string.IsNullOrWhiteSpace(tech))
                        {
                            if (propMap.TryGetValue(tech, out var v)) val = v ?? string.Empty;
                            else if (propMap.TryGetValue(tech.ToLowerInvariant(), out var v2)) val = v2 ?? string.Empty;
                        }
                        dict[tech] = val ?? string.Empty;
                    }
                }
                else
                {
                    // non-object row, store as single value under a generic key
                    dict["value"] = row.ToString();
                }

                result.Add(dict);
            }

            return result;
        }

        // Helper debug formatters
        private static string FormatDebugDictionary(IDictionary<string, object?> dict)
        {
            if (dict == null) return "{}";
            try
            {
                var parts = dict.Select(kv => kv.Key + "=" + SafeDebugValue(kv.Value?.ToString()));
                return "{" + string.Join(", ", parts) + "}";
            }
            catch { return "{}"; }
        }

        private static string FormatDebugDictionarySample(IDictionary<string, string> dict)
        {
            if (dict == null) return "{}";
            try
            {
                var parts = dict.Take(12).Select(kv => kv.Key + "=" + SafeDebugValue(kv.Value));
                return "{" + string.Join(", ", parts) + (dict.Count > 12 ? ", ..." : string.Empty) + "}";
            }
            catch { return "{}"; }
        }

        private static string FormatDebugJsonElementSample(System.Text.Json.JsonElement el)
        {
            try
            {
                if (el.ValueKind != System.Text.Json.JsonValueKind.Object)
                    return SafeDebugValue(el.ToString());
                var parts = new List<string>();
                foreach (var p in el.EnumerateObject().Take(12))
                {
                    parts.Add(p.Name + "=" + SafeDebugValue(p.Value.ToString()));
                }
                return "{" + string.Join(", ", parts) + (el.EnumerateObject().Count() > 12 ? ", ..." : string.Empty) + "}";
            }
            catch { return SafeDebugValue(el.ToString()); }
        }

        private static string SafeDebugValue(string? s, int max = 120)
        {
            if (s == null) return "null";
            var single = s.Replace('\n', ' ').Replace('\r', ' ');
            if (single.Length <= max) return single;
            return single.Substring(0, max) + "...";
        }

        // Compute a canonical key for a column to be used across views/exports
        private static string ComputeCanonicalKey(AppExportColumnDefinitionRecord col)
        {
            // prefer Name (ColumnKey), then LabelLang, then LabelKurz, fallback to a normalized ColumnKey
            var candidates = new[] { col.Name, col.ColumnKey, col.LabelLang, col.LabelKurz };
            foreach (var c in candidates)
            {
                if (!string.IsNullOrWhiteSpace(c))
                    return NormalizeKey(c!);
            }
            return NormalizeKey(col.ColumnKey ?? col.LabelLang ?? col.LabelKurz ?? "col");
        }

        private static string NormalizeKey(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;
            var lowered = input.Trim().ToLowerInvariant();
            var sb = new System.Text.StringBuilder();
            foreach (var ch in lowered)
            {
                if (char.IsLetterOrDigit(ch) || ch == '_') sb.Append(ch);
            }
            return sb.ToString();
        }

        public async Task<List<System.Text.Json.JsonElement>> ExecuteOptionsRpcAsync(string rpcName)
        {
            if (string.IsNullOrWhiteSpace(rpcName))
                return new List<System.Text.Json.JsonElement>();

            return await _supabaseService.RunExportRpcAsync(rpcName, null);
        }

        public async Task LoadDefinitionsAsync()
        {
            Definitions.Clear();
            var defs = await _supabaseService.GetExportDefinitionsAsync();
            foreach (var d in defs)
                Definitions.Add(d);
            try { Console.WriteLine($"EXPORTDBG: LoadDefinitionsAsync loaded {Definitions.Count} definitions"); System.Diagnostics.Debug.WriteLine($"EXPORTDBG: LoadDefinitionsAsync loaded {Definitions.Count} definitions"); } catch {}
        }

        public async Task SelectDefinitionAsync(AppExportDefinitionRecord def)
        {
            SelectedDefinition = def;
            Filters.Clear();
            Columns.Clear();
            Results.Clear();
            ProcessedResults.Clear();
            // clear previous filter values to avoid sending export-foreign parameters
            FilterValues.Clear();
            ColumnsVisibleOrdered = new List<AppExportColumnDefinitionRecord>();
            CurrentIndex = -1;

            var exportKey = def.ExportKey ?? def.DisplayText ?? string.Empty;
            try { Console.WriteLine($"EXPORTDBG: SelectDefinitionAsync start exportKey={exportKey}, Titel={def.Titel ?? ""}, QuelleName={def.QuelleName ?? ""}"); System.Diagnostics.Debug.WriteLine($"EXPORTDBG: SelectDefinitionAsync start exportKey={exportKey}, Titel={def.Titel ?? ""}, QuelleName={def.QuelleName ?? ""}"); } catch {}

            var filters = await _supabaseService.GetExportFilterDefinitionsAsync(exportKey);
            foreach (var f in filters)
                Filters.Add(f);
            try { Console.WriteLine($"EXPORTDBG: SelectDefinitionAsync loaded filters count={Filters.Count}"); System.Diagnostics.Debug.WriteLine($"EXPORTDBG: SelectDefinitionAsync loaded filters count={Filters.Count}"); } catch {}

            var cols = await _supabaseService.GetExportColumnDefinitionsAsync(exportKey);
            foreach (var c in cols)
                Columns.Add(c);
            try { Console.WriteLine($"EXPORTDBG: SelectDefinitionAsync loaded columns count={Columns.Count}"); System.Diagnostics.Debug.WriteLine($"EXPORTDBG: SelectDefinitionAsync loaded columns count={Columns.Count}"); } catch {}
        }

        public async Task ExecuteAsync()
        {
            Results.Clear();
            ProcessedResults.Clear();
            ColumnsVisibleOrdered = new List<AppExportColumnDefinitionRecord>();
            CurrentIndex = -1;
            if (SelectedDefinition == null)
                return;

            // build parameters only from Filters belonging to the selected definition
            var mapped = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            foreach (var f in Filters)
            {
                if (f == null) continue;
                var key = (f.FilterKey ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(key)) continue;

                // obtain raw value from FilterValues if present
                FilterValues.TryGetValue(key, out var rawVal);
                object? val = rawVal;

                // normalize string inputs
                if (val is string s)
                {
                    var ts = s.Trim();
                    if (string.Equals(ts, "null", StringComparison.OrdinalIgnoreCase) || ts == string.Empty)
                        val = null;
                    else if (string.Equals(ts, "true", StringComparison.OrdinalIgnoreCase))
                        val = true;
                    else if (string.Equals(ts, "false", StringComparison.OrdinalIgnoreCase))
                        val = false;
                    else
                        val = ts;
                }

                // map UI filter keys to RPC parameter names (specific mapping)
                string paramName;
                switch (key.ToLowerInvariant())
                {
                    case "sortierung":
                        paramName = "p_sortierung";
                        break;
                    case "aktiv_filter":
                        paramName = "p_aktiv_filter";
                        break;
                    case "anlage_filter":
                        paramName = "p_anlage_filter";
                        break;
                    case "status_filter":
                        paramName = "p_status_filter";
                        break;
                    default:
                        if (key.EndsWith("_filter", StringComparison.OrdinalIgnoreCase))
                            paramName = "p_" + key;
                        else
                            paramName = key;
                        break;
                }

                mapped[paramName] = val;
            }

            // summary for diagnostics
            LastRpcParameterSummary = string.Join(", ", mapped.Select(kv => kv.Key + "=" + (kv.Value == null ? "null" : kv.Value.ToString())));
            try { Console.WriteLine($"EXPORTDBG: ExecuteAsync export_key={(SelectedDefinition?.ExportKey ?? "?")}, final params={LastRpcParameterSummary}"); System.Diagnostics.Debug.WriteLine($"EXPORTDBG: ExecuteAsync export_key={(SelectedDefinition?.ExportKey ?? "?")}, final params={LastRpcParameterSummary}"); } catch {}
            LastRpcError = null;
            // Determine RPC name: prefer explicit quelle_name, fallback to standard_ausgabe or export_key
            var rpcName = SelectedDefinition.QuelleName;
            if (string.IsNullOrWhiteSpace(rpcName))
            {
                rpcName = SelectedDefinition.StandardAusgabe;
            }
            if (string.IsNullOrWhiteSpace(rpcName))
            {
                rpcName = SelectedDefinition.ExportKey;
            }

            LastRpcName = rpcName;
            List<System.Text.Json.JsonElement> rows;
            try
            {
            try { Console.WriteLine($"EXPORTDBG: ExecuteAsync calling RPC={rpcName} with params={LastRpcParameterSummary}"); System.Diagnostics.Debug.WriteLine($"EXPORTDBG: ExecuteAsync calling RPC={rpcName} with params={LastRpcParameterSummary}"); } catch {}
                rows = await _supabaseService.RunExportRpcAsync(rpcName ?? string.Empty, mapped);
            try { Console.WriteLine($"EXPORTDBG: ExecuteAsync raw rows returned={rows.Count}"); System.Diagnostics.Debug.WriteLine($"EXPORTDBG: ExecuteAsync raw rows returned={rows.Count}"); } catch {}

            if (EXPORT_DIAG)
            {
                try
                {
                    Console.WriteLine("EXPORTDBG: ----- EXPORTDBG RAW -----");
                    Console.WriteLine($"EXPORTDBG: RAW count={rows.Count}");
                    if (rows.Count > 0)
                    {
                        Console.WriteLine($"EXPORTDBG: RAW sample keys/values={FormatDebugJsonElementSample(rows[0])}");
                    }
                    Console.WriteLine("EXPORTDBG: ----- END RAW -----");
                }
                catch { }
            }
            }
            catch (Exception ex)
            {
                // capture error for UI
                LastRpcError = ex.Message;
                LastRpcRowCount = 0;
                return;
            }
            foreach (var r in rows)
                Results.Add(r);

            try { Console.WriteLine($"EXPORTDBG: ExecuteAsync Results count (raw)={Results.Count}"); System.Diagnostics.Debug.WriteLine($"EXPORTDBG: ExecuteAsync Results count (raw)={Results.Count}"); } catch {}

            // Prepare visible columns in order (use new model helpers)
            var visible = Columns.Where(c => c.Visible).OrderBy(c => c.SortOrder).ToList();
            // If no columns marked visible in the DB, fall back to all columns in defined sort order
            var usedVisibleFallback = false;
            if (visible.Count == 0)
            {
                visible = Columns.OrderBy(c => c.SortOrder).ToList();
                usedVisibleFallback = true;
                try { Console.WriteLine($"EXPORTDBG: Visible-column fallback used (no standard_sichtbar); visibleColumns={visible.Count}"); System.Diagnostics.Debug.WriteLine($"EXPORTDBG: Visible-column fallback used (no standard_sichtbar); visibleColumns={visible.Count}"); } catch {}
            }
            else
            {
                try { Console.WriteLine($"EXPORTDBG: Visible columns determined from standard_sichtbar; visibleColumns={visible.Count}"); System.Diagnostics.Debug.WriteLine($"EXPORTDBG: Visible columns determined from standard_sichtbar; visibleColumns={visible.Count}"); } catch {}
            }

            // determine sort column from filter definitions and filter values
            string? sortKey = null;
            // 1) explicit filter keys - new model uses FilterKey and Typ
            foreach (var f in Filters)
            {
                if (f == null)
                    continue;
                var fk = (f.FilterKey ?? string.Empty).ToLowerInvariant();
                var t = (f.Typ ?? string.Empty).ToLowerInvariant();
                if (fk.Contains("sort") || t.Contains("sort"))
                {
                    if (FilterValues.TryGetValue(f.FilterKey ?? string.Empty, out var val) && val is string sval && !string.IsNullOrWhiteSpace(sval))
                    {
                        sortKey = sval;
                        break;
                    }
                }
            }

            // 2) fallback keys
            if (string.IsNullOrWhiteSpace(sortKey))
            {
                if (FilterValues.TryGetValue("sort", out var s) && s is string ss && !string.IsNullOrWhiteSpace(ss))
                    sortKey = ss;
                else if (FilterValues.TryGetValue("sort_by", out var s2) && s2 is string ss2 && !string.IsNullOrWhiteSpace(ss2))
                    sortKey = ss2;
            }

            if (!string.IsNullOrWhiteSpace(sortKey))
            {
                var sortCol = visible.FirstOrDefault(c => string.Equals(c.Name, sortKey, StringComparison.OrdinalIgnoreCase) || string.Equals(c.ColumnKey, sortKey, StringComparison.OrdinalIgnoreCase));
                if (sortCol != null)
                {
                    visible.Remove(sortCol);
                    visible.Insert(0, sortCol);
                }
            }

            ColumnsVisibleOrdered = visible;

            // compute technical keys for visible columns (use ColumnKey/Name as technical key)
            VisibleColumnsMapped = new List<(AppExportColumnDefinitionRecord, string)>();
            foreach (var col in ColumnsVisibleOrdered)
            {
                var tech = col.ColumnKey ?? col.Name;
                if (string.IsNullOrWhiteSpace(tech))
                {
                    // skip columns without a technical key
                    try { Console.WriteLine($"EXPORTDBG: ExecuteAsync skipping column without technical key label={col.LabelLang ?? col.LabelKurz ?? "?"}"); } catch { }
                    continue;
                }
                VisibleColumnsMapped.Add((col, tech));
            }

            // Map RPC raw rows into canonical export rows using technical column keys (ColumnKey)
            var mappedRows = MapRpcRowsToCanonical(Results.ToList(), VisibleColumnsMapped);
            foreach (var mr in mappedRows)
                ProcessedResults.Add(mr);

            if (EXPORT_DIAG)
            {
                try
                {
                    Console.WriteLine("EXPORTDBG: ----- EXPORTDBG MAPPINGS -----");
                    Console.WriteLine($"EXPORTDBG: Visible columns count={VisibleColumnsMapped.Count}");
                    // log raw keys for first 2 rows
                    for (int i = 0; i < Math.Min(2, Results.Count); i++)
                    {
                        var r = Results[i];
                        if (r.ValueKind == JsonValueKind.Object)
                        {
                            var keys = string.Join(",", r.EnumerateObject().Select(p => p.Name));
                            Console.WriteLine($"EXPORTDBG: RAW_ROW[{i}] keys={SafeDebugValue(keys, 400)}");
                        }
                    }
                    // log mapping sample: columnKey -> matched raw key for first mapped row
                    if (mappedRows.Count > 0)
                    {
                        var first = mappedRows[0];
                        Console.WriteLine($"EXPORTDBG: FIRST_MAPPED_ROW sample={FormatDebugDictionarySample(first)}");
                    }
                    Console.WriteLine("EXPORTDBG: ----- END MAPPINGS -----");
                }
                catch { }
            }
            try { Console.WriteLine($"EXPORTDBG: ExecuteAsync ProcessedResults count={ProcessedResults.Count}"); System.Diagnostics.Debug.WriteLine($"EXPORTDBG: ExecuteAsync ProcessedResults count={ProcessedResults.Count}"); } catch {}

            if (EXPORT_DIAG)
            {
                try
                {
                    Console.WriteLine("EXPORTDBG: ----- EXPORTDBG PROCESSED -----");
                    Console.WriteLine($"EXPORTDBG: PROCESSED count={ProcessedResults.Count}");
                    if (ProcessedResults.Count > 0)
                    {
                        Console.WriteLine($"EXPORTDBG: PROCESSED sample keys/values={FormatDebugDictionarySample(ProcessedResults[0])}");
                    }
                    Console.WriteLine("EXPORTDBG: ----- END PROCESSED -----");
                }
                catch { }
            }

            if (Results.Count > 0 && ProcessedResults.Count == 0)
            {
            try { Console.WriteLine($"EXPORTDBG: ExecuteAsync WARNING: raw rows present={Results.Count} but no processed rows were added"); System.Diagnostics.Debug.WriteLine($"EXPORTDBG: ExecuteAsync WARNING: raw rows present={Results.Count} but no processed rows were added"); } catch {}
            }

            CurrentIndex = ProcessedResults.Count > 0 ? 0 : -1;
            LastRpcRowCount = Results.Count;
        }

        public async Task<string> ExportToCsvAsync()
        {
            if (ProcessedResults.Count == 0 || ColumnsVisibleOrdered.Count == 0)
                throw new InvalidOperationException("Keine Daten zum Exportieren.");

            // build header
            var headers = ColumnsVisibleOrdered.Select(c => c.Label ?? c.Name ?? string.Empty).ToList();

            var sb = new System.Text.StringBuilder();
            // header
            sb.AppendLine(string.Join(";", headers.Select(h => EscapeCsv(h))));

            // rows
            foreach (var row in ProcessedResults)
            {
                var values = new List<string>();
                // use canonical mapping to fetch values
                foreach (var (col, canonical) in VisibleColumnsMapped)
                {
                    row.TryGetValue(canonical, out var val);
                    values.Add(EscapeCsv(val ?? string.Empty));
                }

                sb.AppendLine(string.Join(";", values));
            }

            var fileName = $"{DateTime.Now:yyyy-MM-dd_HH-mm-ss}_{(SelectedDefinition?.ExportKey ?? "export")}.csv";
            var filePath = System.IO.Path.Combine(Microsoft.Maui.Storage.FileSystem.CacheDirectory, fileName);
            var content = sb.ToString();
            // write with UTF8 BOM
            await System.IO.File.WriteAllTextAsync(filePath, content, new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
            return filePath;
        }

        public async Task<string> ExportToPdfAsync()
        {
            if (ProcessedResults.Count == 0 || ColumnsVisibleOrdered.Count == 0)
                throw new InvalidOperationException("Keine Daten zum Exportieren.");

            var exportKey = SelectedDefinition?.ExportKey ?? "export"; 
            // ExportPdfBuilder expects rows keyed by ColumnKey (or Name). Our ProcessedResults are keyed by canonical keys.
            // Remap rows to use ColumnKey (fallback to canonical) so PDF builder keeps working unchanged.
            var remappedRows = new List<Dictionary<string, string>>();
            foreach (var row in ProcessedResults)
            {
                var rem = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var (col, canonical) in VisibleColumnsMapped)
                {
                    row.TryGetValue(canonical, out var val);
                    var targetKey = col.ColumnKey ?? col.Name ?? canonical;
                    rem[targetKey] = val ?? string.Empty;
                }
                remappedRows.Add(rem);
            }
            try { Console.WriteLine($"EXPORTDBG: ExportToPdfAsync remapped rows for PDF count={remappedRows.Count}"); System.Diagnostics.Debug.WriteLine($"EXPORTDBG: ExportToPdfAsync remapped rows for PDF count={remappedRows.Count}"); } catch {}

            if (EXPORT_DIAG)
            {
                try
                {
                    Console.WriteLine("EXPORTDBG: ----- EXPORTDBG PDF -----");
                    Console.WriteLine($"EXPORTDBG: PDF rows passed={remappedRows.Count}");
                    if (remappedRows.Count > 0)
                        Console.WriteLine($"EXPORTDBG: PDF sample keys/values={FormatDebugDictionarySample(remappedRows[0])}");
                    Console.WriteLine("EXPORTDBG: ----- END PDF -----");
                }
                catch { }
            }

            // use ExportPdfBuilder from Core.Utilities
            var pdf = KGV.Core.Utilities.ExportPdfBuilder.BuildExportPdf(exportKey, ColumnsVisibleOrdered, remappedRows);

            var fileName = $"{DateTime.Now:yyyy-MM-dd_HH-mm-ss}_{exportKey}.pdf";
            var filePath = System.IO.Path.Combine(Microsoft.Maui.Storage.FileSystem.CacheDirectory, fileName);
            await System.IO.File.WriteAllBytesAsync(filePath, pdf);
            return filePath;
        }

        private static string EscapeCsv(string input)
        {
            if (input == null)
                return string.Empty;
            var needsQuotes = input.Contains(';') || input.Contains('"') || input.Contains('\n') || input.Contains('\r');
            var escaped = input.Replace("\"", "\"\"");
            return needsQuotes ? "\"" + escaped + "\"" : escaped;
        }

        public bool MoveNext()
        {
            if (CurrentIndex < 0 || CurrentIndex >= ProcessedResults.Count - 1)
                return false;
            CurrentIndex++;
            return true;
        }

        public bool MovePrevious()
        {
            if (CurrentIndex <= 0)
                return false;
            CurrentIndex--;
            return true;
        }

        // Resolve a value for a visible column from a processed row using the canonical mapping.
        // Returns empty string when no value found. Does NOT fall back to label text.
        public string ResolveColumnValue(Dictionary<string, string> row, AppExportColumnDefinitionRecord col)
        {
            if (row == null || col == null) return string.Empty;

            // find canonical for this column
            var vmcol = VisibleColumnsMapped.FirstOrDefault(x => ReferenceEquals(x.Column, col) || string.Equals(x.Column.ColumnKey, col.ColumnKey, StringComparison.OrdinalIgnoreCase));
            var canonical = vmcol.CanonicalKey ?? ComputeCanonicalKey(col);

            // direct canonical lookup
            if (row.TryGetValue(canonical, out var v) && !string.IsNullOrWhiteSpace(v))
                return v;

            // try common alternatives: ColumnKey, Name
            var tryKeys = new[] { col.ColumnKey, col.Name };
            foreach (var k in tryKeys)
            {
                if (string.IsNullOrWhiteSpace(k)) continue;
                if (row.TryGetValue(k, out var v2) && !string.IsNullOrWhiteSpace(v2)) return v2;
                var lower = k.ToLowerInvariant();
                if (row.TryGetValue(lower, out var v3) && !string.IsNullOrWhiteSpace(v3)) return v3;
                var norm = NormalizeKey(k);
                if (row.TryGetValue(norm, out var v4) && !string.IsNullOrWhiteSpace(v4)) return v4;
            }

            // last resort: try fuzzy normalized keys in the row matching the normalized column key
            var targetNorm = NormalizeKey(col.ColumnKey ?? col.Name ?? col.LabelLang ?? col.LabelKurz ?? string.Empty);
            if (!string.IsNullOrEmpty(targetNorm))
            {
                foreach (var kv in row)
                {
                    var normKey = NormalizeKey(kv.Key);
                    if (!string.IsNullOrEmpty(normKey) && normKey == targetNorm && !string.IsNullOrWhiteSpace(kv.Value))
                        return kv.Value;
                }
            }

            return string.Empty;
        }
    }
}
