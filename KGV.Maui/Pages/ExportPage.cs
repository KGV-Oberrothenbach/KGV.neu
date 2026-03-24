using KGV.Core.Interfaces;
using KGV.Core.Models;
using KGV.Core.Security;
using KGV.Core.Utilities;
using KGV.Maui.State;
using Microsoft.Maui.ApplicationModel;
using System.Text;

namespace KGV.Maui.Pages;

public sealed class ExportPage : ContentPage
{
    private readonly ISupabaseService _supabaseService;
    private readonly UserContextState _userContextState;
    private readonly Label _statusLabel;
    private readonly Button _exportButton;
    private bool _isBusy;

    public ExportPage(ISupabaseService supabaseService, UserContextState userContextState)
    {
        _supabaseService = supabaseService;
        _userContextState = userContextState;
        Title = "Export";

        var titleLabel = new Label { Text = "Export", FontSize = 24, FontAttributes = FontAttributes.Bold };
        var descriptionLabel = new Label
        {
            Text = "Mobiler Kernexport für Mitglieder als CSV auf dem bestehenden Mitgliedspfad. Demo-/Testdaten werden aus dem operativen Export ausgeschlossen.",
            LineBreakMode = LineBreakMode.WordWrap
        };
        var hintLabel = new Label
        {
            Text = "Der Export wird lokal erzeugt und anschließend direkt über den Android-Share-/Teilen-Pfad weitergegeben.",
            TextColor = Colors.Gray,
            LineBreakMode = LineBreakMode.WordWrap
        };

        _exportButton = new Button { Text = "Mitglieder (CSV) exportieren" };
        _exportButton.Clicked += async (_, _) => await ExportMitgliederCsvAsync();

        _statusLabel = new Label { TextColor = Colors.DarkSlateBlue, LineBreakMode = LineBreakMode.WordWrap };

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = 24,
                Spacing = 12,
                Children =
                {
                    titleLabel,
                    descriptionLabel,
                    hintLabel,
                    _exportButton,
                    _statusLabel
                }
            }
        };
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (_userContextState.CurrentUserContext?.Role is not (UserRole.Admin or UserRole.Vorstand))
        {
            _statusLabel.Text = "Export ist mobil nur für Admin/Vorstand verfügbar.";
            _exportButton.IsVisible = false;
            return;
        }

        _exportButton.IsVisible = true;
        _exportButton.IsEnabled = !_isBusy;
    }

    private async Task ExportMitgliederCsvAsync()
    {
        if (_isBusy)
            return;

        if (_userContextState.CurrentUserContext?.Role is not (UserRole.Admin or UserRole.Vorstand))
        {
            _statusLabel.Text = "Export ist mobil nur für Admin/Vorstand verfügbar.";
            return;
        }

        _isBusy = true;
        _exportButton.IsEnabled = false;
        try
        {
            _statusLabel.Text = "Lade Mitglieder...";
            var members = await _supabaseService.GetMitgliederAsync();
            var operationalMembers = (members ?? new List<MitgliedRecord>())
                .Where(OperationalDataFilter.IsOperationalMember)
                .ToList();

            if (operationalMembers.Count == 0)
            {
                _statusLabel.Text = "Keine operativen Mitglieder für den Export gefunden.";
                return;
            }

            var csv = MitgliederCsvExportBuilder.Build(operationalMembers, operationalOnly: true);
            var fileName = $"kgv_mitglieder_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.csv";
            var filePath = Path.Combine(FileSystem.CacheDirectory, fileName);
            await File.WriteAllTextAsync(filePath, csv, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));

            await Share.Default.RequestAsync(new ShareFileRequest
            {
                Title = "Mitglieder (CSV) exportieren",
                File = new ShareFile(filePath)
            });

            _statusLabel.Text = $"Export erzeugt: {operationalMembers.Count} operative Datensätze\n{filePath}";
        }
        catch (Exception ex)
        {
            _statusLabel.Text = $"Export fehlgeschlagen: {ex.Message}";
        }
        finally
        {
            _isBusy = false;
            _exportButton.IsEnabled = _userContextState.CurrentUserContext?.Role is UserRole.Admin or UserRole.Vorstand;
        }
    }
}
