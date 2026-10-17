using System;
using System.IO;
using System.Threading.Tasks;
using KGV.Core.Models;
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
    private readonly string? _persistentFilePath;
    private readonly bool _persistForSignature;
    private bool _previewOpenedOnce;

    public MitgliedsantragPreviewPage(DokumentUploadRequest previewUploadRequest, bool persistForSignature = true)
    {
        _previewUploadRequest = previewUploadRequest ?? throw new ArgumentNullException(nameof(previewUploadRequest));
        if ((_previewUploadRequest.FileContent?.Length ?? 0) <= 0)
            throw new InvalidOperationException("Für die Vorschau liegt kein PDF-Inhalt vor.");

        Title = "Mitgliedsantrag prüfen";
        BackgroundColor = Colors.White;
        _persistForSignature = persistForSignature;
        _tempFilePath = Path.Combine(FileSystem.CacheDirectory, _previewUploadRequest.FileName);
        _persistentFilePath = persistForSignature ? KGV.Maui.Services.Documents.DocumentStorage.GetPersistentFilePath(_previewUploadRequest.FileName) : null;

        var openPreviewButton = new Button { Text = "Dokumentvorschau öffnen" };
        openPreviewButton.Clicked += async (_, _) => await OpenPreviewAsync();

        var backButton = new Button { Text = "Zurück" };
        backButton.Clicked += async (_, _) => await CloseAsync(MitgliedsantragPreviewDecision.BackToEditor);

        var cancelButton = new Button { Text = "Abbrechen" };
        cancelButton.Clicked += async (_, _) => await CloseAsync(MitgliedsantragPreviewDecision.Cancel);

        var continueButton = new Button { Text = "Weiter zur Unterschrift", IsVisible = persistForSignature };
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
                        Text = persistForSignature ? "Vor dem finalen Speichern wird der vollständige Mitgliedsantrag zuerst nur temporär als PDF-Vorschau geöffnet. Bitte das vollständige Dokument prüfen und danach zur Unterschrift zurückkehren." : "Diese Vorschau ist flüchtig. Änderungen am vorbereiteten Antrag sind noch nicht gespeichert.",
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
                                new Label { Text = persistForSignature ? "Die vollständige PDF-Vorschau wird direkt in der App angezeigt. Der offizielle Mitgliedsdokumentpfad wird erst nach erfolgreicher Unterschrift verwendet." : "Die PDF wird nur im Arbeitsspeicher angezeigt und nicht gespeichert.", TextColor = Colors.Gray, LineBreakMode = Microsoft.Maui.LineBreakMode.WordWrap },
                                openPreviewButton
                            }
                        }
                    },
                    new FlexLayout
                    {
                        Direction = FlexDirection.Row,
                        Wrap = FlexWrap.Wrap,
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
        // Die Kopie bleibt für den nachfolgenden Signaturvorgang erhalten.
        if (_persistForSignature && _persistentFilePath is { } persistentFilePath)
        {
            try
            {
                var persistentDir = Path.GetDirectoryName(persistentFilePath)!;
                Directory.CreateDirectory(persistentDir);
                await File.WriteAllBytesAsync(persistentFilePath, _previewUploadRequest.FileContent);
            }
            catch { }
        }

        await Navigation.PushAsync(new PdfViewerPage(_previewUploadRequest.Titel, _previewUploadRequest.FileContent));
    }

    private async Task CloseAsync(MitgliedsantragPreviewDecision decision)
    {
        if (!_persistForSignature && decision == MitgliedsantragPreviewDecision.ContinueToSignature)
            decision = MitgliedsantragPreviewDecision.Cancel;
        TryDeleteTempFile();
        // Do not delete the legacy persistent copy; it is kept for signing.
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
