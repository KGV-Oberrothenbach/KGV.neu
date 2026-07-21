using System;
using System.IO;
using System.Threading.Tasks;
using KGV.Core.Models;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Storage;

namespace KGV.Maui.Pages;

internal enum MitgliedsantragPreviewDecision
{
    Cancel,
    BackToEditor,
    ContinueToSignature
}

public sealed class MitgliedsantragPreviewPage : ContentPage
{
    private readonly TaskCompletionSource<MitgliedsantragPreviewDecision> _resultSource = new();
    private readonly DokumentUploadRequest _previewUploadRequest;
        private readonly string _tempFilePath;
        private readonly string _persistentFilePath;
    private bool _previewOpenedOnce;

    public MitgliedsantragPreviewPage(DokumentUploadRequest previewUploadRequest)
    {
        _previewUploadRequest = previewUploadRequest ?? throw new ArgumentNullException(nameof(previewUploadRequest));
        if ((_previewUploadRequest.FileContent?.Length ?? 0) <= 0)
            throw new InvalidOperationException("Für die Vorschau liegt kein PDF-Inhalt vor.");

        Title = "Mitgliedsantrag prüfen";
        BackgroundColor = Colors.White;
        _tempFilePath = Path.Combine(FileSystem.CacheDirectory, _previewUploadRequest.FileName);
        _persistentFilePath = KGV.Maui.Services.Documents.DocumentStorage.GetPersistentFilePath(_previewUploadRequest.FileName);

        var openPreviewButton = new Button { Text = "Dokumentvorschau öffnen" };
        openPreviewButton.Clicked += async (_, _) => await OpenPreviewAsync();

        var backButton = new Button { Text = "Zurück" };
        backButton.Clicked += async (_, _) => await CloseAsync(MitgliedsantragPreviewDecision.BackToEditor);

        var cancelButton = new Button { Text = "Abbrechen" };
        cancelButton.Clicked += async (_, _) => await CloseAsync(MitgliedsantragPreviewDecision.Cancel);

        var continueButton = new Button { Text = "Weiter zur Unterschrift" };
        continueButton.Clicked += async (_, _) => await CloseAsync(MitgliedsantragPreviewDecision.ContinueToSignature);

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = 24,
                Spacing = 14,
                Children =
                {
                    new Label
                    {
                        Text = "Mitgliedsantrag prüfen",
                        FontSize = 24,
                        FontAttributes = FontAttributes.Bold
                    },
                    new Label
                    {
                        Text = "Vor dem finalen Speichern wird der vollständige Mitgliedsantrag zuerst nur temporär als PDF-Vorschau geöffnet. Bitte das vollständige Dokument prüfen und danach zur Unterschrift zurückkehren.",
                        TextColor = Colors.Gray,
                        LineBreakMode = Microsoft.Maui.LineBreakMode.WordWrap
                    },
                    new Border
                    {
                        Stroke = Colors.LightGray,
                        Padding = 16,
                        Content = new VerticalStackLayout
                        {
                            Spacing = 8,
                            Children =
                            {
                                new Label { Text = _previewUploadRequest.Titel, FontAttributes = FontAttributes.Bold, FontSize = 18 },
                                new Label { Text = $"Datei: {_previewUploadRequest.FileName}", TextColor = Colors.Gray, LineBreakMode = Microsoft.Maui.LineBreakMode.WordWrap },
                                new Label { Text = "Die vollständige PDF-Vorschau wird über den temporären lokalen Preview-Pfad geöffnet; der offizielle Mitgliedsdokumentpfad wird erst nach erfolgreicher Unterschrift verwendet.", TextColor = Colors.Gray, LineBreakMode = Microsoft.Maui.LineBreakMode.WordWrap },
                                openPreviewButton
                            }
                        }
                    },
                    new HorizontalStackLayout
                    {
                        Spacing = 12,
                        HorizontalOptions = LayoutOptions.End,
                        Children =
                        {
                            backButton,
                            cancelButton,
                            continueButton
                        }
                    }
                }
            }
        };
    }

    internal Task<MitgliedsantragPreviewDecision> WaitForResultAsync() => _resultSource.Task;

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (_previewOpenedOnce)
            return;

        _previewOpenedOnce = true;
        try { System.Diagnostics.Debug.WriteLine("[MitgliedsantragPreview] OnAppearing: opening preview"); } catch { }
        await OpenPreviewAsync();
    }

    protected override bool OnBackButtonPressed()
    {
        _resultSource.TrySetResult(MitgliedsantragPreviewDecision.Cancel);
        return base.OnBackButtonPressed();
    }

    private async Task OpenPreviewAsync()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_tempFilePath)!);
        await File.WriteAllBytesAsync(_tempFilePath, _previewUploadRequest.FileContent);

        // Also write a persistent copy so the file remains available for signing later
        try
        {
            var persistentDir = Path.GetDirectoryName(_persistentFilePath)!;
            Directory.CreateDirectory(persistentDir);
            await File.WriteAllBytesAsync(_persistentFilePath, _previewUploadRequest.FileContent);
        }
        catch
        {
            // Ignore persistent write failures; preview still works
        }

        await Launcher.Default.OpenAsync(new OpenFileRequest("Mitgliedsantrag Vorschau", new ReadOnlyFile(_tempFilePath)));
    }

    private async Task CloseAsync(MitgliedsantragPreviewDecision decision)
    {
        TryDeleteTempFile();
        // Do not delete persistent file; it is kept for signing.
        await Navigation.PopModalAsync();
        _resultSource.TrySetResult(decision);
    }

    private void TryDeleteTempFile()
    {
        try
        {
            if (File.Exists(_tempFilePath))
                File.Delete(_tempFilePath);
        }
        catch
        {
        }
    }
}
