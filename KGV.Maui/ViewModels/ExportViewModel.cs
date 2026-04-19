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

            var filters = await _supabaseService.GetExportFilterDefinitionsAsync(def.Id);
            foreach (var f in filters)
                Filters.Add(f);

            var cols = await _supabaseService.GetExportColumnDefinitionsAsync(def.Id);
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

            // build parameters from FilterValues
            var parameters = new Dictionary<string, object?>();
            foreach (var kv in FilterValues)
                parameters[kv.Key] = kv.Value;

            var rpcName = SelectedDefinition.QuelleName ?? string.Empty;
            var rows = await _supabaseService.RunExportRpcAsync(rpcName, parameters);
            foreach (var r in rows)
                Results.Add(r);

            // Prepare visible columns in order
            var visible = Columns.Where(c => c.Visible).OrderBy(c => c.SortOrder).ToList();

            // determine sort column from filter definitions and filter values
            string? sortKey = null;
            // 1) explicit filter keys
            foreach (var f in Filters)
            {
                if (f == null)
                    continue;
                var name = (f.Name ?? string.Empty).ToLowerInvariant();
                if (name.Contains("sort") || string.Equals(f.Type, "sort", StringComparison.OrdinalIgnoreCase))
                {
                    if (FilterValues.TryGetValue(f.Name ?? string.Empty, out var val) && val is string sval && !string.IsNullOrWhiteSpace(sval))
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
                var sortCol = visible.FirstOrDefault(c => string.Equals(c.Name, sortKey, StringComparison.OrdinalIgnoreCase));
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

            var fileName = $"{DateTime.Now:yyyy-MM-dd_HH-mm-ss}_{(SelectedDefinition?.Name ?? "export")}.csv";
            var filePath = System.IO.Path.Combine(Microsoft.Maui.Storage.FileSystem.CacheDirectory, fileName);
            var content = sb.ToString();
            // write with UTF8 BOM
            await System.IO.File.WriteAllTextAsync(filePath, content, new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
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
