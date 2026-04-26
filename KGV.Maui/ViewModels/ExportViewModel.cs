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

        private const bool EXPORT_DIAG = true;

        // Controlled alias rules for known RPC key deviations (technicalKey -> possible raw keys)
        private static readonly Dictionary<string, string[]> TECH_ALIASES = new(StringComparer.OrdinalIgnoreCase)
        {
            { "nr", new[] { "nummer", "id" } },
            { "nummer", new[] { "nr", "id" } },
            { "telefon", new[] { "handy", "mobil", "telefonnummer" } },
            { "handy", new[] { "telefon", "mobil" } },
            { "mobil", new[] { "handy", "telefon" } },
            { "adresse", new[] { "strasse", "street", "adresse1" } },
            { "plz", new[] { "postcode", "postalcode" } },
            { "ort", new[] { "stadt", "city" } },
            { "aktiv", new[] { "is_active", "active" } },
            { "status", new[] { "zustand" } }
        };

        public ObservableCollection<AppExportDefinitionRecord> Definitions { get; } = new();
        public ObservableCollection<AppExportFilterDefinitionRecord> Filters { get; } = new();
        public ObservableCollection<AppExportColumnDefinitionRecord> Columns { get; } = new();
        public ObservableCollection<System.Collections.Generic.Dictionary<string, string>> Results { get; } = new();
        public ObservableCollection<Dictionary<string, string>> ProcessedResults { get; } = new();

        // Visible columns in display order (respecting sort column moved to front if applicable)
        public List<AppExportColumnDefinitionRecord> ColumnsVisibleOrdered { get; private set; } = new();

        // Visible columns with their canonical lookup key used for ProcessedResults and all views/exports
        public List<(AppExportColumnDefinitionRecord Column, string CanonicalKey)> VisibleColumnsMapped { get; private set; } = new();

        public int CurrentIndex { get; private set; } = -1;
        public Dictionary<string, string>? CurrentRecord =>
            CurrentIndex >= 0 && CurrentIndex < ProcessedResults.Count
                ? ProcessedResults[CurrentIndex]
                : null;

        public AppExportDefinitionRecord? SelectedDefinition { get; set; }
        public Dictionary<string, object?> FilterValues { get; } = new();

        // Diagnostics
        public string? LastRpcName { get; private set; }
        public string? LastRpcParameterSummary { get; private set; }
        public int LastRpcRowCount { get; private set; }
        public string? LastRpcError { get; private set; }

        public ExportViewModel(ISupabaseService supabaseService)
        {
            _supabaseService = supabaseService;
        }

        // Returns value and match type for diagnostics
        private static (string value, string match) ResolveMaterializedValueWithMatch(
            Dictionary<string, string> propMap,
            Dictionary<string, string> normalizedMap,
            string technicalKey)
        {
            if (string.IsNullOrWhiteSpace(technicalKey))
                return (string.Empty, "none");

            if (propMap.TryGetValue(technicalKey, out var exact) && !string.IsNullOrWhiteSpace(exact))
                return (exact ?? string.Empty, "exact");

            var lower = technicalKey.ToLowerInvariant();
            if (propMap.TryGetValue(lower, out var lowerVal) && !string.IsNullOrWhiteSpace(lowerVal))
                return (lowerVal ?? string.Empty, "lower");

            var norm = NormalizeKey(technicalKey);
            if (!string.IsNullOrWhiteSpace(norm) &&
                normalizedMap.TryGetValue(norm, out var originalKey) &&
                propMap.TryGetValue(originalKey, out var normVal) && !string.IsNullOrWhiteSpace(normVal))
            {
                return (normVal ?? string.Empty, "normalized");
            }

            if (TECH_ALIASES.TryGetValue(technicalKey, out var aliases))
            {
                foreach (var alias in aliases)
                {
                    if (propMap.TryGetValue(alias, out var aliasVal) && !string.IsNullOrWhiteSpace(aliasVal))
                        return (aliasVal ?? string.Empty, "alias");

                    var aliasLower = alias.ToLowerInvariant();
                    if (propMap.TryGetValue(aliasLower, out var aliasLowerVal) && !string.IsNullOrWhiteSpace(aliasLowerVal))
                        return (aliasLowerVal ?? string.Empty, "alias_lower");

                    var aliasNorm = NormalizeKey(alias);
                    if (!string.IsNullOrWhiteSpace(aliasNorm) &&
                        normalizedMap.TryGetValue(aliasNorm, out var aliasOriginal) &&
                        propMap.TryGetValue(aliasOriginal, out var aliasNormVal) && !string.IsNullOrWhiteSpace(aliasNormVal))
                    {
                        return (aliasNormVal ?? string.Empty, "alias_normalized");
                    }
                }
            }

            return (string.Empty, "none");
        }

        private static string DetectUsedKeyForTech(Dictionary<string, string> propMap, Dictionary<string, string> normalizedMap, string technicalKey)
        {
            if (string.IsNullOrWhiteSpace(technicalKey))
                return string.Empty;

            if (propMap.ContainsKey(technicalKey))
                return technicalKey;

            var lower = technicalKey.ToLowerInvariant();
            if (propMap.ContainsKey(lower))
                return lower;

            var norm = NormalizeKey(technicalKey);
            if (!string.IsNullOrWhiteSpace(norm) && normalizedMap.TryGetValue(norm, out var originalKey) && propMap.ContainsKey(originalKey))
                return originalKey;

            if (TECH_ALIASES.TryGetValue(technicalKey, out var aliases))
            {
                foreach (var alias in aliases)
                {
                    if (propMap.ContainsKey(alias))
                        return alias;
                    var aliasLower = alias.ToLowerInvariant();
                    if (propMap.ContainsKey(aliasLower))
                        return aliasLower;
                    var aliasNorm = NormalizeKey(alias);
                    if (!string.IsNullOrWhiteSpace(aliasNorm) && normalizedMap.TryGetValue(aliasNorm, out var aliasOriginal) && propMap.ContainsKey(aliasOriginal))
                        return aliasOriginal;
                }
            }

            return string.Empty;
        }

        public async Task<List<System.Collections.Generic.Dictionary<string, string>>> ExecuteOptionsRpcAsync(string rpcName)
        {
            if (string.IsNullOrWhiteSpace(rpcName))
                return new List<System.Collections.Generic.Dictionary<string, string>>();

            return await _supabaseService.RunExportRpcAsync(rpcName, null);
        }

        public async Task LoadDefinitionsAsync()
        {
            Definitions.Clear();
            var defs = await _supabaseService.GetExportDefinitionsAsync();
            foreach (var d in defs)
                Definitions.Add(d);

            try
            {
                Console.WriteLine($"EXPORTDBG: LoadDefinitionsAsync loaded {Definitions.Count} definitions");
                System.Diagnostics.Debug.WriteLine($"EXPORTDBG: LoadDefinitionsAsync loaded {Definitions.Count} definitions");
            }
            catch
            {
            }
        }

        public async Task SelectDefinitionAsync(AppExportDefinitionRecord def)
        {
            SelectedDefinition = def;

            Filters.Clear();
            Columns.Clear();
            Results.Clear();
            ProcessedResults.Clear();
            FilterValues.Clear();

            ColumnsVisibleOrdered = new List<AppExportColumnDefinitionRecord>();
            VisibleColumnsMapped = new List<(AppExportColumnDefinitionRecord, string)>();
            CurrentIndex = -1;

            var exportKey = def.ExportKey ?? def.DisplayText ?? string.Empty;

            try
            {
                Console.WriteLine($"EXPORTDBG: SelectDefinitionAsync start exportKey={exportKey}, Titel={def.Titel ?? ""}, QuelleName={def.QuelleName ?? ""}");
                System.Diagnostics.Debug.WriteLine($"EXPORTDBG: SelectDefinitionAsync start exportKey={exportKey}, Titel={def.Titel ?? ""}, QuelleName={def.QuelleName ?? ""}");
            }
            catch
            {
            }

            var filters = await _supabaseService.GetExportFilterDefinitionsAsync(exportKey);
            foreach (var f in filters)
                Filters.Add(f);

            try
            {
                Console.WriteLine($"EXPORTDBG: SelectDefinitionAsync loaded filters count={Filters.Count}");
                System.Diagnostics.Debug.WriteLine($"EXPORTDBG: SelectDefinitionAsync loaded filters count={Filters.Count}");
            }
            catch
            {
            }

            var cols = await _supabaseService.GetExportColumnDefinitionsAsync(exportKey);
            foreach (var c in cols)
                Columns.Add(c);

            try
            {
                Console.WriteLine($"EXPORTDBG: SelectDefinitionAsync loaded columns count={Columns.Count}");
                System.Diagnostics.Debug.WriteLine($"EXPORTDBG: SelectDefinitionAsync loaded columns count={Columns.Count}");
            }
            catch
            {
            }
        }

        public async Task ExecuteAsync()
        {
            Results.Clear();
            ProcessedResults.Clear();
            ColumnsVisibleOrdered = new List<AppExportColumnDefinitionRecord>();
            VisibleColumnsMapped = new List<(AppExportColumnDefinitionRecord, string)>();
            CurrentIndex = -1;
            LastRpcError = null;
            LastRpcRowCount = 0;

            if (SelectedDefinition == null)
                return;

            var mapped = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

            foreach (var f in Filters)
            {
                if (f == null)
                    continue;

                var key = (f.FilterKey ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(key))
                    continue;

                FilterValues.TryGetValue(key, out var rawVal);
                object? val = rawVal;

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

                string paramName = key.ToLowerInvariant() switch
                {
                    "sortierung" => "p_sortierung",
                    "aktiv_filter" => "p_aktiv_filter",
                    "anlage_filter" => "p_anlage_filter",
                    "status_filter" => "p_status_filter",
                    _ => key.EndsWith("_filter", StringComparison.OrdinalIgnoreCase) ? "p_" + key : key
                };

                mapped[paramName] = val;
            }

            LastRpcParameterSummary = string.Join(", ", mapped.Select(kv => kv.Key + "=" + (kv.Value == null ? "null" : kv.Value.ToString())));

            try
            {
                Console.WriteLine($"EXPORTDBG: ExecuteAsync export_key={(SelectedDefinition?.ExportKey ?? "?")}, final params={LastRpcParameterSummary}");
                System.Diagnostics.Debug.WriteLine($"EXPORTDBG: ExecuteAsync export_key={(SelectedDefinition?.ExportKey ?? "?")}, final params={LastRpcParameterSummary}");
            }
            catch
            {
            }

            var rpcName = SelectedDefinition.QuelleName;
            if (string.IsNullOrWhiteSpace(rpcName))
                rpcName = SelectedDefinition.StandardAusgabe;
            if (string.IsNullOrWhiteSpace(rpcName))
                rpcName = SelectedDefinition.ExportKey;

            LastRpcName = rpcName;

            List<System.Collections.Generic.Dictionary<string, string>> rows;
            try
            {
                try
                {
                    Console.WriteLine($"EXPORTDBG: ExecuteAsync calling RPC={rpcName} with params={LastRpcParameterSummary}");
                    System.Diagnostics.Debug.WriteLine($"EXPORTDBG: ExecuteAsync calling RPC={rpcName} with params={LastRpcParameterSummary}");
                }
                catch
                {
                }

                rows = await _supabaseService.RunExportRpcAsync(rpcName ?? string.Empty, mapped);

                try
                {
                    Console.WriteLine($"EXPORTDBG: ExecuteAsync raw rows returned={rows.Count}");
                    System.Diagnostics.Debug.WriteLine($"EXPORTDBG: ExecuteAsync raw rows returned={rows.Count}");
                }
                catch
                {
                }

                if (EXPORT_DIAG)
                {
            try
            {
                Console.WriteLine("EXPORTDBG: ----- EXPORTDBG RAW -----");
                Console.WriteLine($"EXPORTDBG: RAW count={rows.Count}");
                if (rows.Count > 0)
                {
                    var firstKeys = rows[0].Count > 0 ? string.Join(',', rows[0].Keys) : "(none)";
                    Console.WriteLine($"EXPORTDBG: RAW_ROW[0] materializedKeys={SafeDebugValue(firstKeys,400)}");
                    Console.WriteLine($"EXPORTDBG: RAW sample values={FormatDebugDictionarySample(rows[0])}");
                }
                Console.WriteLine("EXPORTDBG: ----- END RAW -----");
            }
                    catch
                    {
                    }
                }
            }
            catch (Exception ex)
            {
                LastRpcError = ex.Message;
                LastRpcRowCount = 0;
                return;
            }

            foreach (var r in rows)
                Results.Add(r);

            try
            {
                Console.WriteLine($"EXPORTDBG: ExecuteAsync Results count (raw)={Results.Count}");
                System.Diagnostics.Debug.WriteLine($"EXPORTDBG: ExecuteAsync Results count (raw)={Results.Count}");
            }
            catch
            {
            }

            var visible = Columns
                .Where(c => c.Visible)
                .OrderBy(c => c.SortOrder)
                .ToList();

            if (visible.Count == 0)
            {
                visible = Columns
                    .OrderBy(c => c.SortOrder)
                    .ToList();

                try
                {
                    Console.WriteLine($"EXPORTDBG: Visible-column fallback used (no standard_sichtbar); visibleColumns={visible.Count}");
                    System.Diagnostics.Debug.WriteLine($"EXPORTDBG: Visible-column fallback used (no standard_sichtbar); visibleColumns={visible.Count}");
                }
                catch
                {
                }
            }
            else
            {
                try
                {
                    Console.WriteLine($"EXPORTDBG: Visible columns determined from standard_sichtbar; visibleColumns={visible.Count}");
                    System.Diagnostics.Debug.WriteLine($"EXPORTDBG: Visible columns determined from standard_sichtbar; visibleColumns={visible.Count}");
                }
                catch
                {
                }
            }

            string? sortKey = null;

            foreach (var f in Filters)
            {
                if (f == null)
                    continue;

                var fk = (f.FilterKey ?? string.Empty).ToLowerInvariant();
                var t = (f.Typ ?? string.Empty).ToLowerInvariant();

                if (fk.Contains("sort") || t.Contains("sort"))
                {
                    if (FilterValues.TryGetValue(f.FilterKey ?? string.Empty, out var val) &&
                        val is string sval &&
                        !string.IsNullOrWhiteSpace(sval))
                    {
                        sortKey = sval;
                        break;
                    }
                }
            }

            if (string.IsNullOrWhiteSpace(sortKey))
            {
                if (FilterValues.TryGetValue("sort", out var s) && s is string ss && !string.IsNullOrWhiteSpace(ss))
                    sortKey = ss;
                else if (FilterValues.TryGetValue("sort_by", out var s2) && s2 is string ss2 && !string.IsNullOrWhiteSpace(ss2))
                    sortKey = ss2;
            }

            if (!string.IsNullOrWhiteSpace(sortKey))
            {
                var sortCol = visible.FirstOrDefault(c =>
                    string.Equals(c.Name, sortKey, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(c.ColumnKey, sortKey, StringComparison.OrdinalIgnoreCase));

                if (sortCol != null)
                {
                    visible.Remove(sortCol);
                    visible.Insert(0, sortCol);
                }
            }

            ColumnsVisibleOrdered = visible;

            VisibleColumnsMapped = new List<(AppExportColumnDefinitionRecord, string)>();
            foreach (var col in ColumnsVisibleOrdered)
            {
                var tech = col.ColumnKey ?? col.Name;
                if (string.IsNullOrWhiteSpace(tech))
                {
                    try
                    {
                        Console.WriteLine($"EXPORTDBG: ExecuteAsync skipping column without technical key label={col.LabelLang ?? col.LabelKurz ?? "?"}");
                    }
                    catch
                    {
                    }
                    continue;
                }

                VisibleColumnsMapped.Add((col, tech));
            }

            // Results already materialized as dictionaries by the service; MapRpcRowsToCanonical overloads accept dictionaries now
            // Offload CPU-heavy mapping to background thread to avoid UI-thread ANR
            List<System.Collections.Generic.Dictionary<string, string>> mappedRows;
            try
            {
                var rowsCopy = Results.ToList();
                mappedRows = await System.Threading.Tasks.Task.Run(() => MapRpcRowsToCanonical(rowsCopy, VisibleColumnsMapped));
            }
            catch (Exception exMap)
            {
                try { Console.WriteLine($"EXPORTDBG: mapping failed: {exMap.Message}"); } catch { }
                mappedRows = new List<System.Collections.Generic.Dictionary<string, string>>();
            }

            // Apply mapped rows to ProcessedResults on UI thread; small count (e.g. 86) expected — keep simple adds
            ProcessedResults.Clear();
            foreach (var mr in mappedRows)
                ProcessedResults.Add(mr);

            if (EXPORT_DIAG)
            {
                try
                {
                    Console.WriteLine("EXPORTDBG: ----- EXPORTDBG MAPPINGS -----");
                    Console.WriteLine($"EXPORTDBG: Visible columns count={VisibleColumnsMapped.Count}");

                    for (int i = 0; i < Math.Min(2, Results.Count); i++)
                    {
                        var rawProps = Results[i] ?? new Dictionary<string, string>();
                        var keys = rawProps.Count > 0
                            ? string.Join(",", rawProps.Keys)
                            : "(none)";
                        Console.WriteLine($"EXPORTDBG: RAW_ROW[{i}] materializedKeys={SafeDebugValue(keys, 400)}");
                        // also dump value-wrapper sample
                        if (rawProps.TryGetValue("value", out var v))
                        {
                            Console.WriteLine($"EXPORTDBG: RAW_ROW[{i}] valueWrapperSample={SafeDebugValue(v,800)}");
                        }
                    }

                    if (mappedRows.Count > 0)
                        Console.WriteLine($"EXPORTDBG: FIRST_MAPPED_ROW sample={FormatDebugDictionarySample(mappedRows[0])}");

                    // Per-column matching diagnostics for first raw row
                    if (Results.Count > 0 && VisibleColumnsMapped.Count > 0)
                    {
                        try
                        {
                            var first = Results[0] ?? new Dictionary<string, string>();
                            var normalizedMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                            foreach (var key in first.Keys)
                            {
                                var norm = NormalizeKey(key);
                                if (!string.IsNullOrWhiteSpace(norm) && !normalizedMap.ContainsKey(norm))
                                    normalizedMap[norm] = key;
                            }

                            var usedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                            var colsToShow = Math.Min(8, VisibleColumnsMapped.Count);
                            for (int ci = 0; ci < colsToShow; ci++)
                            {
                                var tech = VisibleColumnsMapped[ci].CanonicalKey ?? VisibleColumnsMapped[ci].Column.ColumnKey ?? VisibleColumnsMapped[ci].Column.Name ?? string.Empty;
                                var (val, match) = ResolveMaterializedValueWithMatch(first, normalizedMap, tech);
                                if (!string.IsNullOrWhiteSpace(val))
                                {
                                    // try to detect which original key was used
                                    var detectedKey = DetectUsedKeyForTech(first, normalizedMap, tech);
                                    if (!string.IsNullOrWhiteSpace(detectedKey)) usedKeys.Add(detectedKey);
                                }
                                Console.WriteLine($"EXPORTDBG: COL_MATCH col={tech} match={match} sample={SafeDebugValue(val)}");
                            }

                            var unmatched = first.Keys.Where(k => !usedKeys.Contains(k)).ToList();
                            Console.WriteLine($"EXPORTDBG: RAW_ROW[0] unmatchedKeys={SafeDebugValue(string.Join(',', unmatched),400)}");
                        }
                        catch { }
                    }

                    Console.WriteLine("EXPORTDBG: ----- END MAPPINGS -----");
                }
                catch
                {
                }
            }

            try
            {
                Console.WriteLine($"EXPORTDBG: ExecuteAsync ProcessedResults count={ProcessedResults.Count}");
                System.Diagnostics.Debug.WriteLine($"EXPORTDBG: ExecuteAsync ProcessedResults count={ProcessedResults.Count}");
            }
            catch
            {
            }

            if (EXPORT_DIAG)
            {
                try
                {
                    Console.WriteLine("EXPORTDBG: ----- EXPORTDBG PROCESSED -----");
                    Console.WriteLine($"EXPORTDBG: PROCESSED count={ProcessedResults.Count}");
                    if (ProcessedResults.Count > 0)
                        Console.WriteLine($"EXPORTDBG: PROCESSED sample keys/values={FormatDebugDictionarySample(ProcessedResults[0])}");
                    Console.WriteLine("EXPORTDBG: ----- END PROCESSED -----");
                }
                catch
                {
                }
            }

            if (Results.Count > 0 && ProcessedResults.Count == 0)
            {
                try
                {
                    Console.WriteLine($"EXPORTDBG: ExecuteAsync WARNING: raw rows present={Results.Count} but no processed rows were added");
                    System.Diagnostics.Debug.WriteLine($"EXPORTDBG: ExecuteAsync WARNING: raw rows present={Results.Count} but no processed rows were added");
                }
                catch
                {
                }
            }

            CurrentIndex = ProcessedResults.Count > 0 ? 0 : -1;
            LastRpcRowCount = Results.Count;
        }

        public async Task<string> ExportToCsvAsync()
        {
            if (ProcessedResults.Count == 0 || ColumnsVisibleOrdered.Count == 0)
                throw new InvalidOperationException("Keine Daten zum Exportieren.");

            var headers = ColumnsVisibleOrdered
                .Select(c => c.Label ?? c.Name ?? string.Empty)
                .ToList();

            var sb = new System.Text.StringBuilder();
            sb.AppendLine(string.Join(";", headers.Select(EscapeCsv)));

            foreach (var row in ProcessedResults)
            {
                var values = new List<string>();
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

            await System.IO.File.WriteAllTextAsync(
                filePath,
                content,
                new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: true));

            return filePath;
        }

        public async Task<string> ExportToPdfAsync()
        {
            if (ProcessedResults.Count == 0 || ColumnsVisibleOrdered.Count == 0)
                throw new InvalidOperationException("Keine Daten zum Exportieren.");

            var exportKey = SelectedDefinition?.ExportKey ?? "export";

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

            try
            {
                Console.WriteLine($"EXPORTDBG: ExportToPdfAsync remapped rows for PDF count={remappedRows.Count}");
                System.Diagnostics.Debug.WriteLine($"EXPORTDBG: ExportToPdfAsync remapped rows for PDF count={remappedRows.Count}");
            }
            catch
            {
            }

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
                catch
                {
                }
            }

            var pdf = KGV.Core.Utilities.ExportPdfBuilder.BuildExportPdf(exportKey, ColumnsVisibleOrdered, remappedRows);

            var fileName = $"{DateTime.Now:yyyy-MM-dd_HH-mm-ss}_{exportKey}.pdf";
            var filePath = System.IO.Path.Combine(Microsoft.Maui.Storage.FileSystem.CacheDirectory, fileName);
            await System.IO.File.WriteAllBytesAsync(filePath, pdf);
            return filePath;
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
            if (row == null || col == null)
                return string.Empty;

            var vmcol = VisibleColumnsMapped.FirstOrDefault(x =>
                ReferenceEquals(x.Column, col) ||
                string.Equals(x.Column.ColumnKey, col.ColumnKey, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(x.Column.Name, col.Name, StringComparison.OrdinalIgnoreCase));

            var canonical = !string.IsNullOrWhiteSpace(vmcol.CanonicalKey)
                ? vmcol.CanonicalKey
                : (col.ColumnKey ?? col.Name ?? string.Empty);

            if (!string.IsNullOrWhiteSpace(canonical) &&
                row.TryGetValue(canonical, out var v) &&
                !string.IsNullOrWhiteSpace(v))
            {
                return v;
            }

            var tryKeys = new[]
            {
                col.ColumnKey,
                col.Name,
                NormalizeKey(col.ColumnKey ?? string.Empty),
                NormalizeKey(col.Name ?? string.Empty)
            };

            foreach (var k in tryKeys.Where(x => !string.IsNullOrWhiteSpace(x)))
            {
                if (row.TryGetValue(k!, out var v2) && !string.IsNullOrWhiteSpace(v2))
                    return v2;
            }

            var targetNorm = NormalizeKey(col.ColumnKey ?? col.Name ?? col.LabelLang ?? col.LabelKurz ?? string.Empty);
            if (!string.IsNullOrEmpty(targetNorm))
            {
                foreach (var kv in row)
                {
                    if (NormalizeKey(kv.Key) == targetNorm && !string.IsNullOrWhiteSpace(kv.Value))
                        return kv.Value;
                }
            }

            return string.Empty;
        }

        // Central mapper: converts RPC JsonElement rows into canonical export rows keyed by technical column keys
        private static List<Dictionary<string, string>> MapRpcRowsToCanonical(
            List<Dictionary<string, string>> rows,
            List<(AppExportColumnDefinitionRecord Column, string CanonicalKey)> visibleColumns)
        {
            var result = new List<Dictionary<string, string>>();
            if (rows == null || visibleColumns == null)
                return result;

            foreach (var row in rows)
            {
                // row is already a materialized dictionary from the service
                var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                var propMap = row ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                if (propMap.Count == 0)
                {
                    dict["value"] = string.Empty;
                    result.Add(dict);
                    continue;
                }

                var normalizedMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var key in propMap.Keys)
                {
                    var norm = NormalizeKey(key);
                    if (!string.IsNullOrWhiteSpace(norm) && !normalizedMap.ContainsKey(norm))
                        normalizedMap[norm] = key;
                }

                foreach (var (_, tech) in visibleColumns)
                {
                    var value = ResolveMaterializedValue(propMap, normalizedMap, tech);
                    dict[tech] = value ?? string.Empty;
                }

                result.Add(dict);
            }

            return result;
        }

        private static string ResolveMaterializedValue(
            Dictionary<string, string> propMap,
            Dictionary<string, string> normalizedMap,
            string technicalKey)
        {
            if (string.IsNullOrWhiteSpace(technicalKey))
                return string.Empty;

            if (propMap.TryGetValue(technicalKey, out var exact))
                return exact ?? string.Empty;

            var lower = technicalKey.ToLowerInvariant();
            if (propMap.TryGetValue(lower, out var lowerVal))
                return lowerVal ?? string.Empty;

            var norm = NormalizeKey(technicalKey);
            if (!string.IsNullOrWhiteSpace(norm) &&
                normalizedMap.TryGetValue(norm, out var originalKey) &&
                propMap.TryGetValue(originalKey, out var normVal))
            {
                return normVal ?? string.Empty;
            }

            if (TECH_ALIASES.TryGetValue(technicalKey, out var aliases))
            {
                foreach (var alias in aliases)
                {
                    if (propMap.TryGetValue(alias, out var aliasVal))
                        return aliasVal ?? string.Empty;

                    var aliasLower = alias.ToLowerInvariant();
                    if (propMap.TryGetValue(aliasLower, out var aliasLowerVal))
                        return aliasLowerVal ?? string.Empty;

                    var aliasNorm = NormalizeKey(alias);
                    if (!string.IsNullOrWhiteSpace(aliasNorm) &&
                        normalizedMap.TryGetValue(aliasNorm, out var aliasOriginal) &&
                        propMap.TryGetValue(aliasOriginal, out var aliasNormVal))
                    {
                        return aliasNormVal ?? string.Empty;
                    }
                }
            }

            return string.Empty;
        }

        private static Dictionary<string, string> TryMaterializeRowProperties(JsonElement row)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            TryMaterializeRowPropertiesInternal(row, result, depth: 0);
            return result;
        }

        private static void TryMaterializeRowPropertiesInternal(JsonElement row, Dictionary<string, string> target, int depth)
        {
            if (depth > 4)
                return;

            switch (row.ValueKind)
            {
                case JsonValueKind.Object:
                    {
                        var props = row.EnumerateObject().ToList();

                        // Special-case wrapper with only one property like { "value": "{...}" } or { "value": {...} }
                        if (props.Count == 1)
                        {
                            var only = props[0];
                            if (string.Equals(only.Name, "value", StringComparison.OrdinalIgnoreCase) ||
                                string.Equals(only.Name, "data", StringComparison.OrdinalIgnoreCase) ||
                                string.Equals(only.Name, "row", StringComparison.OrdinalIgnoreCase))
                            {
                                var nested = TryExtractNestedElement(only.Value);
                                if (nested.HasValue)
                                {
                                    TryMaterializeRowPropertiesInternal(nested.Value, target, depth + 1);
                                    if (target.Count > 0)
                                        return;
                                }
                            }
                        }

                        foreach (var prop in props)
                        {
                            if (prop.Value.ValueKind == JsonValueKind.Object || prop.Value.ValueKind == JsonValueKind.Array)
                            {
                                // nested structures only unwrap for typical wrappers
                                if (string.Equals(prop.Name, "value", StringComparison.OrdinalIgnoreCase) ||
                                    string.Equals(prop.Name, "data", StringComparison.OrdinalIgnoreCase) ||
                                    string.Equals(prop.Name, "row", StringComparison.OrdinalIgnoreCase))
                                {
                                    var nested = TryExtractNestedElement(prop.Value);
                                    if (nested.HasValue)
                                    {
                                        TryMaterializeRowPropertiesInternal(nested.Value, target, depth + 1);
                                        if (target.Count > 0)
                                            continue;
                                    }
                                }

                                target[prop.Name] = prop.Value.ToString();
                                continue;
                            }

                            target[prop.Name] = ExtractSimpleValue(prop.Value);
                        }

                        break;
                    }

                case JsonValueKind.String:
                    {
                        var raw = row.GetString();
                        if (TryParseJson(raw, out var parsed))
                        {
                            TryMaterializeRowPropertiesInternal(parsed, target, depth + 1);
                        }
                        break;
                    }

                case JsonValueKind.Array:
                    {
                        // If the row itself is an array and contains a single object/stringified object, unwrap the first useful item.
                        foreach (var item in row.EnumerateArray())
                        {
                            var nested = TryExtractNestedElement(item);
                            if (nested.HasValue)
                            {
                                TryMaterializeRowPropertiesInternal(nested.Value, target, depth + 1);
                                if (target.Count > 0)
                                    return;
                            }
                        }
                        break;
                    }
            }
        }

        private static JsonElement? TryExtractNestedElement(JsonElement element)
        {
            if (element.ValueKind == JsonValueKind.Object || element.ValueKind == JsonValueKind.Array)
                return element;

            if (element.ValueKind == JsonValueKind.String)
            {
                var raw = element.GetString();
                if (TryParseJson(raw, out var parsed))
                    return parsed;
            }

            return null;
        }

        private static bool TryParseJson(string? raw, out JsonElement parsed)
        {
            parsed = default;

            if (string.IsNullOrWhiteSpace(raw))
                return false;

            var trimmed = raw.Trim();
            if (!trimmed.StartsWith("{", StringComparison.Ordinal) &&
                !trimmed.StartsWith("[", StringComparison.Ordinal))
            {
                return false;
            }

            try
            {
                using var doc = JsonDocument.Parse(trimmed);
                parsed = doc.RootElement.Clone();
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static string ExtractSimpleValue(JsonElement value)
        {
            try
            {
                return value.ValueKind switch
                {
                    JsonValueKind.String => value.GetString() ?? string.Empty,
                    JsonValueKind.Number => value.ToString(),
                    JsonValueKind.True => "Ja",
                    JsonValueKind.False => "Nein",
                    JsonValueKind.Null => string.Empty,
                    JsonValueKind.Undefined => string.Empty,
                    _ => value.ToString()
                };
            }
            catch
            {
                return value.ToString();
            }
        }

        private static string FormatDebugDictionarySample(IDictionary<string, string> dict)
        {
            if (dict == null)
                return "{}";

            try
            {
                var parts = dict.Take(12).Select(kv => kv.Key + "=" + SafeDebugValue(kv.Value));
                return "{" + string.Join(", ", parts) + (dict.Count > 12 ? ", ..." : string.Empty) + "}";
            }
            catch
            {
                return "{}";
            }
        }

        private static string FormatDebugJsonElementSample(JsonElement el)
        {
            try
            {
                var props = TryMaterializeRowProperties(el);
                if (props.Count > 0)
                    return FormatDebugDictionarySample(props);

                if (el.ValueKind == JsonValueKind.Object)
                {
                    var parts = new List<string>();
                    foreach (var p in el.EnumerateObject().Take(12))
                        parts.Add(p.Name + "=" + SafeDebugValue(p.Value.ToString()));
                    return "{" + string.Join(", ", parts) + (el.EnumerateObject().Count() > 12 ? ", ..." : string.Empty) + "}";
                }

                return SafeDebugValue(el.ToString());
            }
            catch
            {
                return SafeDebugValue(el.ToString());
            }
        }

        private static string SafeDebugValue(string? s, int max = 120)
        {
            if (s == null)
                return "null";

            var single = s.Replace('\n', ' ').Replace('\r', ' ');
            return single.Length <= max ? single : single[..max] + "...";
        }

        private static string NormalizeKey(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            var lowered = input.Trim().ToLowerInvariant();
            var sb = new System.Text.StringBuilder();

            foreach (var ch in lowered)
            {
                if (char.IsLetterOrDigit(ch) || ch == '_')
                    sb.Append(ch);
            }

            return sb.ToString();
        }

        private static string EscapeCsv(string input)
        {
            if (input == null)
                return string.Empty;

            var needsQuotes = input.Contains(';') || input.Contains('"') || input.Contains('\n') || input.Contains('\r');
            var escaped = input.Replace("\"", "\"\"");
            return needsQuotes ? "\"" + escaped + "\"" : escaped;
        }
    }
}