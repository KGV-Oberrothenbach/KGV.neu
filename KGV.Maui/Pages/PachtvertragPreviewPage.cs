using System;
using System.IO;
using System.Threading.Tasks;
using KGV.Core.Models;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Storage;

namespace KGV.Maui.Pages;

internal enum PachtvertragPreviewDecision
{
    Cancel,
    BackToEditor,
    ContinueToSignature
}

public sealed class PachtvertragPreviewPage : ContentPage
{
    private readonly TaskCompletionSource<PachtvertragPreviewDecision> _resultSource = new();
    private readonly DokumentUploadRequest _previewUploadRequest;
    private readonly string _tempFilePath;
    private bool _previewOpenedOnce;

    public PachtvertragPreviewPage(DokumentUploadRequest previewUploadRequest)
    {
        _previewUploadRequest = previewUploadRequest ?? throw new ArgumentNullException(nameof(previewUploadRequest));
        if ((_previewUploadRequest.FileContent?.Length ?? 0) <= 0)
            throw new InvalidOperationException("Für die Vorschau liegt kein PDF-Inhalt vor.");

        Title = "Pachtvertrag prüfen";
        BackgroundColor = Colors.White;
        _tempFilePath = Path.Combine(FileSystem.CacheDirectory, _previewUploadRequest.FileName);

        var openPreviewButton = new Button { Text = "Dokumentvorschau öffnen" };
        openPreviewButton.Clicked += async (_, _) => await OpenPreviewAsync();

        var backButton = new Button { Text = "Zurück" };
        backButton.Clicked += async (_, _) => await CloseAsync(PachtvertragPreviewDecision.BackToEditor);

        var cancelButton = new Button { Text = "Abbrechen" };
        cancelButton.Clicked += async (_, _) => await CloseAsync(PachtvertragPreviewDecision.Cancel);

        var continueButton = new Button { Text = "Weiter zur Unterschrift" };
        continueButton.Clicked += async (_, _) => await CloseAsync(PachtvertragPreviewDecision.ContinueToSignature);

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
                        Text = "Pachtvertrag prüfen",
                        FontSize = 24,
                        FontAttributes = FontAttributes.Bold
                    },
                    new Label
                    {
                        Text = "Vor dem finalen Speichern wird der vollständige Pachtvertrag zuerst nur temporär als PDF-Vorschau geöffnet. Bitte das vollständige Dokument prüfen und danach zur Unterschrift zurückkehren.",
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
                                new Label { Text = "Die vollständige PDF-Vorschau wird über den temporären lokalen Preview-Pfad geöffnet; der offizielle Dokumentpfad wird erst nach erfolgreicher Unterschrift verwendet.", TextColor = Colors.Gray, LineBreakMode = Microsoft.Maui.LineBreakMode.WordWrap },
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

    internal Task<PachtvertragPreviewDecision> WaitForResultAsync() => _resultSource.Task;

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (_previewOpenedOnce)
            return;

        _previewOpenedOnce = true;
        await OpenPreviewAsync();
    }

    protected override bool OnBackButtonPressed()
    {
        _resultSource.TrySetResult(PachtvertragPreviewDecision.Cancel);
        return base.OnBackButtonPressed();
    }

    private async Task OpenPreviewAsync()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_tempFilePath)!);
        await File.WriteAllBytesAsync(_tempFilePath, _previewUploadRequest.FileContent);
        await Launcher.Default.OpenAsync(new OpenFileRequest("Pachtvertrag Vorschau", new ReadOnlyFile(_tempFilePath)));
    }

    private async Task CloseAsync(PachtvertragPreviewDecision decision)
    {
        _resultSource.TrySetResult(decision);
        TryDeleteTempFile();
        await Navigation.PopModalAsync();
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
