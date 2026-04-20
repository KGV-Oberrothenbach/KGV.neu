using KGV.Core.Interfaces;
using KGV.Core.Models;
using KGV.Maui.State;
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

        public ObservableCollection<AppExportDefinitionRecord> Definitions { get; } = new ObservableCollection<AppExportDefinitionRecord>();
        public ObservableCollection<AppExportFilterDefinitionRecord> Filters { get; } = new ObservableCollection<AppExportFilterDefinitionRecord>();
        public ObservableCollection<AppExportColumnDefinitionRecord> Columns { get; } = new ObservableCollection<AppExportColumnDefinitionRecord>();
        public ObservableCollection<JsonElement> Results { get; } = new ObservableCollection<JsonElement>();
        public ObservableCollection<Dictionary<string, string>> ProcessedResults { get; } = new ObservableCollection<Dictionary<string, string>>();

        // Visible columns in display order (respecting sort column moved to front if applicable)
        public List<AppExportColumnDefinitionRecord> ColumnsVisibleOrdered { get; private set; } = new List<AppExportColumnDefinitionRecord>();

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
        }

        public async Task SelectDefinitionAsync(AppExportDefinitionRecord def)
        {
            SelectedDefinition = def;
            Filters.Clear();
            Columns.Clear();
            Results.Clear();
            ProcessedResults.Clear();
            ColumnsVisibleOrdered = new List<AppExportColumnDefinitionRecord>();
            CurrentIndex = -1;

            var exportKey = def.ExportKey ?? def.DisplayText ?? string.Empty;
            var filters = await _supabaseService.GetExportFilterDefinitionsAsync(exportKey);
            foreach (var f in filters)
                Filters.Add(f);

            var cols = await _supabaseService.GetExportColumnDefinitionsAsync(exportKey);
            foreach (var c in cols)
                Columns.Add(c);
        }

        public async Task ExecuteAsync()
        {
            Results.Clear();
            ProcessedResults.Clear();
            ColumnsVisibleOrdered = new List<AppExportColumnDefinitionRecord>();
            CurrentIndex = -1;
            if (SelectedDefinition == null)
                return;

            // build parameters from FilterValues with mapping to RPC parameter names and type conversion
            var mapped = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            foreach (var kv in FilterValues)
            {
                if (kv.Key == null)
                    continue;
                var key = kv.Key;
                object? val = kv.Value;

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

                // map UI filter keys to RPC parameter names
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
                        // generic fallback: prefix with p_ if ends with _filter
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
                rows = await _supabaseService.RunExportRpcAsync(rpcName ?? string.Empty, mapped);
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

            // Prepare visible columns in order (use new model helpers)
            var visible = Columns.Where(c => c.Visible).OrderBy(c => c.SortOrder).ToList();
            // If no columns marked visible in the DB, fall back to all columns in defined sort order
            if (visible.Count == 0)
            {
                visible = Columns.OrderBy(c => c.SortOrder).ToList();
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

            // map rows to dictionaries
            foreach (var row in Results)
            {
                var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                if (row.ValueKind == JsonValueKind.Object)
                {
                    foreach (var col in ColumnsVisibleOrdered)
                    {
                        var key = col.Name ?? string.Empty;
                        string val = string.Empty;
                        try
                        {
                            if (!string.IsNullOrWhiteSpace(key) && row.TryGetProperty(key, out var prop) && prop.ValueKind != JsonValueKind.Null)
                            {
                                if (prop.ValueKind == JsonValueKind.True)
                                    val = "Ja";
                                else if (prop.ValueKind == JsonValueKind.False)
                                    val = "Nein";
                                else if (prop.ValueKind == JsonValueKind.String)
                                    val = prop.GetString() ?? string.Empty;
                                else if (prop.ValueKind == JsonValueKind.Number)
                                    val = prop.ToString();
                                else if (prop.ValueKind == JsonValueKind.Array || prop.ValueKind == JsonValueKind.Object)
                                    val = prop.ToString();
                                else
                                    val = prop.ToString();
                            }
                        }
                        catch
                        {
                            val = string.Empty;
                        }

                        dict[key] = val ?? string.Empty;
                    }
                }
                else
                {
                    dict["value"] = row.ToString();
                }

                ProcessedResults.Add(dict);
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
                foreach (var col in ColumnsVisibleOrdered)
                {
                    var key = col.Name ?? string.Empty;
                    row.TryGetValue(key, out var val);
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
            // use ExportPdfBuilder from Core.Utilities
            var pdf = KGV.Core.Utilities.ExportPdfBuilder.BuildExportPdf(exportKey, ColumnsVisibleOrdered, ProcessedResults.ToList());

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
    }
}
