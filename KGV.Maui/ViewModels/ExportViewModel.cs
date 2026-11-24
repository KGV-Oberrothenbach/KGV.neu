using KGV.Core.Interfaces;
using KGV.Core.Models;
using KGV.Maui.State;
// using System.Diagnostics removed to avoid Switch type ambiguity in MAUI pages; use fully qualified calls where needed
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace KGV.Maui.ViewModels
{
    public class ExportViewModel
    {
        private readonly ISupabaseService _supabaseService;

        private const bool EXPORT_DIAG = false;

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

        // Try to recover common mojibake where UTF-8 bytes were interpreted as Latin1/Windows-1252
        private static string FixEncoding(string? s)
        {
            if (string.IsNullOrEmpty(s))
                return string.Empty;

            var input = s;
            // quick check for common mojibake markers
            if (!input.Contains('Ã') && !input.Contains('Â') && !input.Contains('┼') && !input.Contains('├') && !input.Contains('\uFFFD'))
                return input.Normalize(System.Text.NormalizationForm.FormC);

            try
            {
                // Try Latin1 -> UTF8
                var latin1 = System.Text.Encoding.Latin1.GetBytes(input);
                var candidateLatin1 = System.Text.Encoding.UTF8.GetString(latin1);

                // Try Windows-1252 -> UTF8 (covers many Windows mojibake cases)
                string candidate1252 = candidateLatin1;
                try
                {
                    var cp1252 = System.Text.Encoding.GetEncoding(1252);
                    var bytes1252 = cp1252.GetBytes(input);
                    candidate1252 = System.Text.Encoding.UTF8.GetString(bytes1252);
                }
                catch { }

                // Choose best candidate by letter-count heuristic
                var scoreOriginal = input.Count(c => char.IsLetter(c));
                var scoreLatin1 = candidateLatin1.Count(c => char.IsLetter(c));
                var score1252 = candidate1252.Count(c => char.IsLetter(c));

                var best = input;
                var bestScore = scoreOriginal;
                if (scoreLatin1 > bestScore) { best = candidateLatin1; bestScore = scoreLatin1; }
                if (score1252 > bestScore) { best = candidate1252; bestScore = score1252; }

                if (!string.ReferenceEquals(best, input))
                    return best.Normalize(System.Text.NormalizationForm.FormC);
            }
            catch
            {
                // ignore
            }

            return input.Normalize(System.Text.NormalizationForm.FormC);
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

            if (rpcName != "rpc_export_arbeitsstunden_jahre" && rpcName != "rpc_export_jahresablesung_jahre")
                throw new InvalidOperationException("Unzulässige Export-Optionsquelle.");
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
                ; // FR2-11: export diagnostics removed.
                ; // FR2-11: export diagnostics removed.
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
                ; // FR2-11: export diagnostics removed.
                ; // FR2-11: export diagnostics removed.
            }
            catch
            {
            }

            var filters = await _supabaseService.GetExportFilterDefinitionsAsync(exportKey);
            foreach (var f in filters)
                Filters.Add(f);

            try
            {
                ; // FR2-11: export diagnostics removed.
                ; // FR2-11: export diagnostics removed.
            }
            catch
            {
            }

            var cols = await _supabaseService.GetExportColumnDefinitionsAsync(exportKey);
            foreach (var c in cols)
                Columns.Add(c);

            try
            {
                ; // FR2-11: export diagnostics removed.
                ; // FR2-11: export diagnostics removed.
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

            var definition = SelectedDefinition;
            if (definition == null)
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

                var explicitParameterName = (f.ParameterName ?? string.Empty).Trim();
                var paramName = !string.IsNullOrWhiteSpace(explicitParameterName) ? explicitParameterName : key;
                val = CoerceFilterValue(f, val);

                mapped[paramName] = val;
            }

            LastRpcParameterSummary = string.Join(", ", mapped.Keys);

            try
            {
                ; // FR2-11: export diagnostics removed.
                ; // FR2-11: export diagnostics removed.
            }
            catch
            {
            }

            var rpcName = definition.QuelleName;
            if (string.IsNullOrWhiteSpace(rpcName))
                rpcName = definition.StandardAusgabe;
            if (string.IsNullOrWhiteSpace(rpcName))
                rpcName = definition.ExportKey;

            LastRpcName = rpcName;

            List<System.Collections.Generic.Dictionary<string, string>> rows;
            try
            {
                try
                {
                    ; // FR2-11: export diagnostics removed.
                    ; // FR2-11: export diagnostics removed.
                }
                catch
                {
                }

                // Export parameters are supplied by metadata and forwarded unchanged so every
                // client uses the same canonical, server-side source and ordering.
                var minimalParams = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                if (mapped != null)
                {
                    foreach (var kv in mapped)
                    {
                        minimalParams[kv.Key] = kv.Value;
                    }
                }
                if (string.Equals(definition.QuelleTyp, "view", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException("Direkte Tabellen-/View-Exporte sind nicht freigegeben.");
                }
                else
                {
                    rows = await _supabaseService.RunProtectedExportRpcAsync(definition.ExportKey ?? string.Empty, minimalParams.Count == 0 ? null : (object)minimalParams);

                    try
                    {
                        ; // FR2-11: export diagnostics removed.
                        ; // FR2-11: export diagnostics removed.
                    }
                    catch
                    {
                    }
                }

                if (EXPORT_DIAG)
                {
            try
            {
                ; // FR2-11: export diagnostics removed.
                ; // FR2-11: export diagnostics removed.
                if (rows.Count > 0)
                {
                    var firstKeys = rows[0].Count > 0 ? string.Join(',', rows[0].Keys) : "(none)";
                    ; // FR2-11: export diagnostics removed.
                    ; // FR2-11: export diagnostics removed.
                }
                ; // FR2-11: export diagnostics removed.
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

            // Store raw rows
            Results.Clear();
            foreach (var r in rows)
                Results.Add(r);

            try
            {
                ; // FR2-11: export diagnostics removed.
            }
            catch { }

            try
            {
                ; // FR2-11: export diagnostics removed.
                ; // FR2-11: export diagnostics removed.
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
                    ; // FR2-11: export diagnostics removed.
                    ; // FR2-11: export diagnostics removed.
                }
                catch
                {
                }
            }
            else
            {
                try
                {
                    ; // FR2-11: export diagnostics removed.
                    ; // FR2-11: export diagnostics removed.
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

            if (string.IsNullOrWhiteSpace(sortKey))
                sortKey = SelectedDefinition?.StandardSortierung;

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
                        ; // FR2-11: export diagnostics removed.
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
                /* FR2-11: diagnostic removed */
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
                    ; // FR2-11: export diagnostics removed.
                    ; // FR2-11: export diagnostics removed.

                    for (int i = 0; i < Math.Min(2, Results.Count); i++)
                    {
                        var rawProps = Results[i] ?? new Dictionary<string, string>();
                        var keys = rawProps.Count > 0
                            ? string.Join(",", rawProps.Keys)
                            : "(none)";
                        ; // FR2-11: export diagnostics removed.
                        // also dump value-wrapper sample
                        if (rawProps.TryGetValue("value", out var v))
                        {
                            ; // FR2-11: export diagnostics removed.
                        }
                    }

                    if (mappedRows.Count > 0)
                        ; // FR2-11: export diagnostics removed.

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
                                ; // FR2-11: export diagnostics removed.
                            }

                            var unmatched = first.Keys.Where(k => !usedKeys.Contains(k)).ToList();
                            ; // FR2-11: export diagnostics removed.
                        }
                        catch { }
                    }

                    ; // FR2-11: export diagnostics removed.
                }
                catch
                {
                }
            }

            try
            {
                ; // FR2-11: export diagnostics removed.
                ; // FR2-11: export diagnostics removed.
            }
            catch
            {
            }

            if (EXPORT_DIAG)
            {
                try
                {
                    ; // FR2-11: export diagnostics removed.
                    ; // FR2-11: export diagnostics removed.
                    if (ProcessedResults.Count > 0)
                        ; // FR2-11: export diagnostics removed.
                    ; // FR2-11: export diagnostics removed.
                }
                catch
                {
                }
            }

            if (Results.Count > 0 && ProcessedResults.Count == 0)
            {
                try
                {
                    ; // FR2-11: export diagnostics removed.
                    ; // FR2-11: export diagnostics removed.
                }
                catch
                {
                }
            }

            CurrentIndex = ProcessedResults.Count > 0 ? 0 : -1;
            LastRpcRowCount = Results.Count;
        }

        public object? GetDefaultFilterValue(AppExportFilterDefinitionRecord filter, int currentSeason)
        {
            var token = filter.StandardwertJson;
            if (token == null || token.Type == JTokenType.Null) return null;
            if (token.Type == JTokenType.String && string.Equals(token.ToObject<string>(), "$currentSeason", StringComparison.Ordinal)) return currentSeason;
            return token.Type switch
            {
                JTokenType.Boolean => token.ToObject<bool>(),
                JTokenType.Integer => token.ToObject<int>(),
                JTokenType.Float => token.ToObject<decimal>(),
                JTokenType.String => token.ToObject<string>(),
                _ => null
            };
        }

        private static object? CoerceFilterValue(AppExportFilterDefinitionRecord filter, object? value)
        {
            if (value == null) return null;
            var type = filter.WertTyp?.Trim().ToLowerInvariant();
            if (type is "integer" or "int" && int.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), NumberStyles.Integer, CultureInfo.InvariantCulture, out var integer)) return integer;
            if (type is "boolean" or "bool" && bool.TryParse(value.ToString(), out var boolean)) return boolean;
            if (type is "number" or "decimal" or "float")
            {
                var text = Convert.ToString(value, CultureInfo.CurrentCulture);
                if (decimal.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out var decimalValue)
                    || decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out decimalValue))
                    return decimalValue;
            }
            return value;
        }

        private static void CleanupOldExportFiles()
        {
            foreach (var path in System.IO.Directory.EnumerateFiles(Microsoft.Maui.Storage.FileSystem.CacheDirectory, "export_*.csv").Concat(System.IO.Directory.EnumerateFiles(Microsoft.Maui.Storage.FileSystem.CacheDirectory, "export_*.pdf")))
            {
                var name = System.IO.Path.GetFileNameWithoutExtension(path);
                if (!name.StartsWith("export_", StringComparison.Ordinal) || !Guid.TryParseExact(name.Substring(7), "N", out _)) continue;
                try { if (System.IO.File.GetLastWriteTimeUtc(path) < DateTime.UtcNow.AddDays(-1)) System.IO.File.Delete(path); }
                catch (System.IO.IOException) { }
                catch (UnauthorizedAccessException) { }
            }
        }

        public async Task<string> ExportToCsvAsync()
        {
            CleanupOldExportFiles();
            if (SelectedDefinition?.ErlaubtCsv != true)
                throw new InvalidOperationException("CSV-Export ist für diesen Export nicht freigegeben.");
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

            var fileName = $"export_{Guid.NewGuid():N}.csv";
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
            CleanupOldExportFiles();
            if (SelectedDefinition?.ErlaubtPdf != true)
                throw new InvalidOperationException("PDF-Export ist für diesen Export nicht freigegeben.");

            var centralPdf = await _supabaseService.GenerateExportPdfAsync(BuildCentralExportRequest());
            var centralFileName = $"export_{Guid.NewGuid():N}.pdf";
            var centralFilePath = System.IO.Path.Combine(Microsoft.Maui.Storage.FileSystem.CacheDirectory, centralFileName);
            await System.IO.File.WriteAllBytesAsync(centralFilePath, centralPdf.Content);
            return centralFilePath;
        }

        private CentralExportRequest BuildCentralExportRequest()
        {
            var parameters = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            foreach (var filter in Filters)
            {
                var filterKey = filter.FilterKey;
                var parameterName = string.IsNullOrWhiteSpace(filter.ParameterName) ? filterKey : filter.ParameterName;
                if (!string.IsNullOrWhiteSpace(filterKey) && !string.IsNullOrWhiteSpace(parameterName))
                {
                    FilterValues.TryGetValue(filterKey, out var value);
                    parameters[parameterName] = CoerceFilterValue(filter, value ?? GetDefaultFilterValue(filter, DateTime.Now.Year));
                }
            }

            return new CentralExportRequest
            {
                ExportKey = SelectedDefinition?.ExportKey ?? string.Empty,
                Format = "pdf",
                Parameters = parameters,
                Columns = ColumnsVisibleOrdered
                    .Select(column => column.ColumnKey ?? column.Name)
                    .Where(columnKey => !string.IsNullOrWhiteSpace(columnKey))
                    .Select(columnKey => columnKey!)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray(),
                Orientation = "auto"
            };
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

            var trimmed = input.TrimStart();
            if ((trimmed.Length > 0 && "=+-@".Contains(trimmed[0])) || input.StartsWith('\t') || input.StartsWith('\r') || input.StartsWith('\n'))
                input = "'" + input;
            var needsQuotes = input.Contains(';') || input.Contains('"') || input.Contains('\n') || input.Contains('\r');
            var escaped = input.Replace("\"", "\"\"");
            return needsQuotes ? "\"" + escaped + "\"" : escaped;
        }
    }
}
