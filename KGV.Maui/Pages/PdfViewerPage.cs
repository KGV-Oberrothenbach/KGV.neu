using System;
using System.IO;
using System.Threading.Tasks;
using KGV.Core.Models;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace KGV.Maui.Pages
{
    // Simple POC in-app PDF viewer that embeds the PDF as a base64 data URI in an HTML page shown via WebView.
    // This is intentionally minimal and meant as a proof-of-concept before integrating PDF.js or a native viewer.
    public sealed class PdfViewerPage : ContentPage
    {
        private readonly string _filePath;
        private readonly WebView _webView;

        public PdfViewerPage(string filePath)
        {
            _filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
            Title = Path.GetFileName(filePath);
            BackgroundColor = Colors.White;

            _webView = new WebView
            {
                VerticalOptions = LayoutOptions.FillAndExpand,
                HorizontalOptions = LayoutOptions.FillAndExpand
            };

            var signButton = new Button { Text = "Unterschreiben" };
            signButton.Clicked += async (_, _) => await OnSignClicked();

            var uploadButton = new Button { Text = "Speichern & Hochladen" };
            uploadButton.Clicked += async (_, _) => await OnUploadClicked();

            Content = new VerticalStackLayout
            {
                Padding = 0,
                Spacing = 8,
                Children =
                {
                    new HorizontalStackLayout
                    {
                        Padding = 8,
                        Spacing = 8,
                        Children = { signButton, uploadButton }
                    },
                    new Border
                    {
                        Stroke = Colors.LightGray,
                        Content = _webView,
                        VerticalOptions = LayoutOptions.FillAndExpand,
                        HorizontalOptions = LayoutOptions.FillAndExpand
                    }
                }
            };
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            _ = LoadPdfIntoWebViewAsync();
        }

        private async Task LoadPdfIntoWebViewAsync()
        {
            try
            {
                if (!File.Exists(_filePath))
                {
                    await DisplayAlert("Fehler", "Die Datei wurde nicht gefunden.", "OK");
                    return;
                }

                var bytes = await File.ReadAllBytesAsync(_filePath);
                var base64 = Convert.ToBase64String(bytes);

                var html = $"<!doctype html><html><head><meta charset=\"utf-8\"></head><body style=\"margin:0; padding:0; height:100vh\">" +
                           $"<embed width=\"100%\" height=\"100%\" src=\"data:application/pdf;base64,{base64}\" type=\"application/pdf\"></embed>" +
                           "</body></html>";

                _webView.Source = new HtmlWebViewSource { Html = html };
            }
            catch (Exception ex)
            {
                await DisplayAlert("Fehler beim Laden", ex.Message, "OK");
            }
        }

        private async Task OnSignClicked()
        {
            try
            {
                var document = new DocumentInfo
                {
                    Title = Title ?? string.Empty,
                    Dateiname = Path.GetFileName(_filePath),
                    Name = Path.GetFileName(_filePath),
                    MimeType = "application/pdf",
                    StoragePath = _filePath
                };

                var signPage = new VertragsSignaturPage(document, "Unterschrift (POC)");
                await Navigation.PushModalAsync(new NavigationPage(signPage));
                var capture = await signPage.WaitForResultAsync();
                if (capture == null)
                {
                    await DisplayAlert("Signatur", "Signatur abgebrochen.", "OK");
                    return;
                }

                // POC: wir erfassen die Signatur, aber integrieren sie noch nicht in das PDF.
                await DisplayAlert("Signatur", "Signatur erfasst (POC). Integration in PDF folgt in späteren Schritten.", "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Fehler", ex.Message, "OK");
            }
        }

        private async Task OnUploadClicked()
        {
            await DisplayAlert("Upload", "Upload-Flow ist noch nicht implementiert. Dies ist ein POC-Button.", "OK");
        }
    }
}
