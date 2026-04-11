using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using KGV.Core.Models;
using KGV.Core.Utilities;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace KGV.Maui.Pages;

public sealed class MitgliedsantragDialogPage : ContentPage
{
    private static readonly CultureInfo DeCulture = CultureInfo.GetCultureInfo("de-DE");
    private readonly TaskCompletionSource<decimal?> _resultSource = new();
    private readonly Entry _mitgliedsbeitragEntry;

    public MitgliedsantragDialogPage(MitgliedRecord member, MitgliedsantragBeitragVorschlag vorschlag)
    {
        var displayName = string.Join(' ', new[] { member.Vorname, member.Name }
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!.Trim()));
        if (string.IsNullOrWhiteSpace(displayName))
            displayName = $"Mitglied #{member.Id}";

        Title = "Mitgliedsantrag";
        BackgroundColor = Colors.White;

        _mitgliedsbeitragEntry = new Entry
        {
            Text = vorschlag.VorgeschlagenerBeitrag.ToString("0.00", DeCulture),
            Keyboard = Microsoft.Maui.Keyboard.Numeric,
            Placeholder = "Mitgliedsbeitrag"
        };

        var cancelButton = new Button { Text = "Abbrechen" };
        cancelButton.Clicked += async (_, _) => await CancelAsync();

        var createButton = new Button { Text = "Erzeugen" };
        createButton.Clicked += async (_, _) => await AcceptAsync();

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
                        Text = "Mitgliedsantrag erstellen",
                        FontSize = 24,
                        FontAttributes = FontAttributes.Bold
                    },
                    new Label
                    {
                        Text = $"Der Mitgliedsantrag wird für {displayName} als rein mitgliedsbezogenes Dokument erzeugt.",
                        TextColor = Colors.Gray
                    },
                    CreateField("Beginn", new Label { Text = vorschlag.BeginnDatum.ToString("dd.MM.yyyy", DeCulture) }),
                    CreateField("Jahresbeitrag", new Label { Text = FormatCurrency(vorschlag.Jahresbeitrag) }),
                    new Label
                    {
                        Text = vorschlag.IstHalberBeitrag
                            ? $"Beginn ab 01.07.{vorschlag.SaisonJahr}: Es wird automatisch der halbe Jahresbeitrag vorgeschlagen. Der Wert kann vor dem Erzeugen angepasst werden."
                            : $"Beginn vor 01.07.{vorschlag.SaisonJahr}: Es wird automatisch der volle Jahresbeitrag vorgeschlagen. Der Wert kann vor dem Erzeugen angepasst werden.",
                        TextColor = Colors.Gray
                    },
                    CreateField("Mitgliedsbeitrag", _mitgliedsbeitragEntry),
                    new HorizontalStackLayout
                    {
                        Spacing = 12,
                        HorizontalOptions = LayoutOptions.End,
                        Children = { cancelButton, createButton }
                    }
                }
            }
        };
    }

    public Task<decimal?> WaitForResultAsync() => _resultSource.Task;

    protected override bool OnBackButtonPressed()
    {
        _resultSource.TrySetResult(null);
        return base.OnBackButtonPressed();
    }

    private async Task AcceptAsync()
    {
        if (!TryParseBeitrag(_mitgliedsbeitragEntry.Text, out var beitrag))
        {
            await DisplayAlert("Mitgliedsantrag", "Bitte einen gültigen Mitgliedsbeitrag eingeben.", "OK");
            _mitgliedsbeitragEntry.Focus();
            return;
        }

        if (beitrag < 0m)
        {
            await DisplayAlert("Mitgliedsantrag", "Der Mitgliedsbeitrag darf nicht negativ sein.", "OK");
            _mitgliedsbeitragEntry.Focus();
            return;
        }

        _resultSource.TrySetResult(MitgliedsantragBeitragHelper.NormalizeBeitrag(beitrag));
        await Navigation.PopModalAsync();
    }

    private async Task CancelAsync()
    {
        _resultSource.TrySetResult(null);
        await Navigation.PopModalAsync();
    }

    private static View CreateField(string title, View content)
    {
        return new VerticalStackLayout
        {
            Spacing = 6,
            Children =
            {
                new Label { Text = title, FontAttributes = FontAttributes.Bold },
                content
            }
        };
    }

    private static bool TryParseBeitrag(string? text, out decimal value)
    {
        return decimal.TryParse(text, NumberStyles.Number, DeCulture, out value)
               || decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out value);
    }

    private static string FormatCurrency(decimal value)
        => MitgliedsantragBeitragHelper.NormalizeBeitrag(value).ToString("0.00 €", DeCulture);
}
