using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using KGV.Core.Interfaces;
using KGV.Core.Models;
using KGV.Maui.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace KGV.Maui.Pages;

public sealed class VereinsauswahlPage : ContentPage
{
    private readonly IConfiguration _configuration;
    private readonly IVereinskontext _vereinskontext;
    private readonly Entry _codeEntry = new() { Placeholder = "Vereins-ID, z. B. KGV-DEMO" };
    private readonly Label _status = new() { TextColor = Colors.Red, LineBreakMode = LineBreakMode.WordWrap };

    public VereinsauswahlPage(IConfiguration configuration, IVereinskontext vereinskontext)
    {
        _configuration = configuration;
        _vereinskontext = vereinskontext;
        Title = "Verein auswählen";

        var continueButton = new Button { Text = "Verein bestätigen", FontAttributes = FontAttributes.Bold, Padding = new Thickness(16, 12) };
        continueButton.Clicked += async (_, _) => await ResolveAndContinueAsync(continueButton);
        var scanButton = new Button { Text = "QR-Code scannen" };
        scanButton.Clicked += async (_, _) => await ScanQrCodeAsync(continueButton);

        Content = new VerticalStackLayout
        {
            Padding = 24,
            Spacing = 14,
            VerticalOptions = LayoutOptions.Center,
            Children =
            {
                new Image
                {
                    Source = "kgv_neutral_logo.png",
                    HeightRequest = 132,
                    Aspect = Aspect.AspectFit,
                    HorizontalOptions = LayoutOptions.Center
                },
                new Label { Text = "Verein auswählen", FontSize = 24, FontAttributes = FontAttributes.Bold },
                new Label { Text = "Gib zuerst die Vereins-ID ein. Erst danach ist eine Anmeldung möglich." },
                _codeEntry,
                continueButton,
                scanButton,
                _status
            }
        };
    }

    private async Task ScanQrCodeAsync(Button continueButton)
    {
        await Navigation.PushModalAsync(new VereinsQrScannerPage(async code =>
        {
            _codeEntry.Text = code;
            await ResolveAndContinueAsync(continueButton);
        }));
    }

    private async Task ResolveAndContinueAsync(Button continueButton)
    {
        var code = (_codeEntry.Text ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(code))
        {
            _status.Text = "Bitte eine Vereins-ID eingeben.";
            return;
        }

        var url = _configuration["Vereinsregister:Url"]?.TrimEnd('/');
        var key = _configuration["Vereinsregister:PublishableKey"]?.Trim();
        if (string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(key))
        {
            _status.Text = "Das Vereinsregister ist in dieser App-Version nicht konfiguriert.";
            return;
        }

        continueButton.IsEnabled = false;
        _status.Text = "Vereins-ID wird geprüft …";
        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
            using var request = new HttpRequestMessage(HttpMethod.Post, $"{url}/rest/v1/rpc/resolve_vereinscode")
            {
                Content = JsonContent.Create(new { p_code = code })
            };
            request.Headers.Add("apikey", key);
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", key);

            using var response = await client.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                _status.Text = "Die Vereins-ID konnte nicht geprüft werden. Bitte Verbindung und Eingabe kontrollieren.";
                return;
            }

            var entries = await response.Content.ReadFromJsonAsync<List<RegistryEntry>>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            var entry = entries?.SingleOrDefault();
            if (entry is null || !Guid.TryParse(entry.VereinId, out var vereinId)
                || string.IsNullOrWhiteSpace(entry.SupabaseUrl)
                || string.IsNullOrWhiteSpace(entry.SupabasePublishableKey))
            {
                _status.Text = "Die Vereins-ID ist ungültig, inaktiv oder unvollständig eingerichtet.";
                return;
            }

            var kontext = new Vereinskontext(vereinId, entry.VereinsCode!, entry.Vereinsname!, entry.Kurzname,
                entry.SupabaseUrl, entry.SupabasePublishableKey);
            _vereinskontext.Setzen(kontext);
            AppSettings.Vereinskontext = kontext;
            AppSettings.Save();

            if (Application.Current is App app)
                await app.SwitchToCurrentRootAsync();
        }
        catch (Exception)
        {
            _status.Text = "Das Vereinsregister ist momentan nicht erreichbar.";
        }
        finally
        {
            continueButton.IsEnabled = true;
        }
    }

    private sealed class RegistryEntry
    {
        [JsonPropertyName("verein_id")]
        public string? VereinId { get; init; }
        [JsonPropertyName("vereins_code")]
        public string? VereinsCode { get; init; }
        [JsonPropertyName("vereinsname")]
        public string? Vereinsname { get; init; }
        [JsonPropertyName("kurzname")]
        public string? Kurzname { get; init; }
        [JsonPropertyName("supabase_url")]
        public string? SupabaseUrl { get; init; }
        [JsonPropertyName("supabase_publishable_key")]
        public string? SupabasePublishableKey { get; init; }
    }
}
