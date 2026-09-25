using ZXing.Net.Maui;
using ZXing.Net.Maui.Controls;
using Microsoft.Maui;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace KGV.Maui.Pages;

public sealed class VereinsQrScannerPage : ContentPage
{
    private readonly Action<string> _onCodeScanned;
    private bool _completed;

    public VereinsQrScannerPage(Action<string> onCodeScanned)
    {
        _onCodeScanned = onCodeScanned;
        Title = "Vereins-QR-Code scannen";

        var scanner = new CameraBarcodeReaderView
        {
            Options = new BarcodeReaderOptions
            {
                Formats = BarcodeFormats.TwoDimensional,
                AutoRotate = true,
                Multiple = false
            },
            HorizontalOptions = LayoutOptions.Fill,
            VerticalOptions = LayoutOptions.Fill
        };
        scanner.BarcodesDetected += OnBarcodesDetected;

        Content = new Grid
        {
            Children =
            {
                scanner,
                new VerticalStackLayout
                {
                    Padding = 24,
                    VerticalOptions = LayoutOptions.End,
                    Children =
                    {
                        new Label { Text = "QR-Code des Vereins in den Rahmen halten.", TextColor = Colors.White, BackgroundColor = Colors.Black },
                        new Button { Text = "Abbrechen", Command = new Command(async () => await Navigation.PopModalAsync()) }
                    }
                }
            }
        };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        var status = await Permissions.RequestAsync<Permissions.Camera>();
        if (status != PermissionStatus.Granted)
        {
            await DisplayAlertAsync("Kamera", "Für das Scannen des Vereins-QR-Codes wird der Kamerazugriff benötigt.", "OK");
            await Navigation.PopModalAsync();
        }
    }

    private void OnBarcodesDetected(object? sender, BarcodeDetectionEventArgs e)
    {
        var code = e.Results?.FirstOrDefault()?.Value?.Trim();
        if (_completed || string.IsNullOrWhiteSpace(code))
            return;

        _completed = true;
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            _onCodeScanned(code);
            await Navigation.PopModalAsync();
        });
    }
}
