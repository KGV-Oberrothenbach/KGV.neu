using KGV.Core.Interfaces;
using KGV.Core.Models;
using KGV.Core.Security;
using KGV.Core.Utilities;
using KGV.Helpers;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KGV.ViewModels
{
    public sealed class ExportViewModel : BaseViewModel
    {
        private readonly ISupabaseService _supabaseService;
        private readonly UserContext _userContext;

        public RelayCommand<object?> ExportMitgliederCsvCommand { get; }

        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            private set
            {
                if (SetProperty(ref _isBusy, value))
                {
                    ExportMitgliederCsvCommand.RaiseCanExecuteChanged();
                }
            }
        }

        private string _statusMessage = string.Empty;
        public string StatusMessage
        {
            get => _statusMessage;
            private set => SetProperty(ref _statusMessage, value);
        }

        public ExportViewModel(ISupabaseService supabaseService, UserContext userContext)
        {
            _supabaseService = supabaseService ?? throw new ArgumentNullException(nameof(supabaseService));
            _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));

            ExportMitgliederCsvCommand = new RelayCommand<object?>(
                _ => _ = ExportMitgliederCsvAsync(),
                _ => !IsBusy);
        }

        private async Task ExportMitgliederCsvAsync()
        {
            try
            {
                IsBusy = true;
                StatusMessage = "Lade Mitglieder...";

                var members = await _supabaseService.GetMitgliederAsync();

                if (members == null || members.Count == 0)
                {
                    StatusMessage = "Keine Mitglieder gefunden.";
                    return;
                }

                var dlg = new SaveFileDialog
                {
                    Title = "Mitglieder exportieren (CSV)",
                    Filter = "CSV (*.csv)|*.csv|Alle Dateien (*.*)|*.*",
                    FileName = $"kgv_mitglieder_{DateTime.Today:yyyy-MM-dd}.csv",
                    AddExtension = true,
                    DefaultExt = ".csv",
                    OverwritePrompt = true
                };

                if (dlg.ShowDialog() != true)
                {
                    StatusMessage = "Export abgebrochen.";
                    return;
                }

                var csv = BuildMitgliederCsv(members);

                // UTF-8 BOM hilft Excel beim Erkennen von Umlauten
                File.WriteAllText(dlg.FileName, csv, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));

                var scope = _userContext.Has(PermissionFlags.CanSeeOwnDataOnly) ? "(nur eigene Daten)" : "";
                StatusMessage = $"Exportiert: {members.Count} Datensätze {scope}\n{dlg.FileName}";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Export fehlgeschlagen: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        internal static string BuildMitgliederCsv(IReadOnlyList<MitgliedRecord> members)
        {
            return MitgliederCsvExportBuilder.Build(members, operationalOnly: true);
        }
    }
}
