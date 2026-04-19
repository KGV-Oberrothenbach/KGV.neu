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
        }
    }
}
