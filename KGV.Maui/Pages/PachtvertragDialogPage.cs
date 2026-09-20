using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using KGV.Core.Models;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace KGV.Maui.Pages;

public sealed class PachtvertragDialogPage : ContentPage
{
    private static readonly CultureInfo DeCulture = CultureInfo.GetCultureInfo("de-DE");
    private readonly TaskCompletionSource<PachtvertragDokumentRequest?> _resultSource = new();
    private readonly MitgliedRecord _member;
    private readonly ParzelleRecord _parzelle;
    private readonly DateTime _vertragsbeginn;
    private readonly bool _istMinderjaehrig;
    private readonly MitgliedRecord? _gesetzlicherVertreter;
    private readonly MitgliedRecord? _nebenmitglied;
    private bool? _includeSecondaryMember;
    private DateTime? _altvertragDatum;
    private bool _altvertragEntscheidungErfasst;

    public PachtvertragDialogPage(
        MitgliedRecord member,
        ParzelleRecord parzelle,
        DateTime vertragsbeginn,
        GesetzlicherVertreterAufloesung? gesetzlicherVertreterAufloesung,
        MitgliedRecord? nebenmitglied,
        PachtvertragDokumentRequest? initialRequest = null)
    {
        _member = member ?? throw new ArgumentNullException(nameof(member));
        _parzelle = parzelle ?? throw new ArgumentNullException(nameof(parzelle));
        _vertragsbeginn = vertragsbeginn.Date;
        _nebenmitglied = nebenmitglied;
        _altvertragDatum = initialRequest?.AltvertragDatum;
        _altvertragEntscheidungErfasst = initialRequest != null;
        _istMinderjaehrig = gesetzlicherVertreterAufloesung?.IstMinderjaehrig ?? false;
        _gesetzlicherVertreter = gesetzlicherVertreterAufloesung?.VertreterMitglied;
        _includeSecondaryMember = !_istMinderjaehrig && _nebenmitglied != null
            ? initialRequest?.IncludeSecondaryMember ?? true
            : false;

        if (_istMinderjaehrig && (_gesetzlicherVertreter == null || _gesetzlicherVertreter.Id <= 0))
        {
            throw new InvalidOperationException(
                "Für dieses minderjährige Mitglied ist im signierten Mitgliedsantrag kein gesetzlicher Vertreter hinterlegt.");
        }

        Title = "Pachtvertrag";
        BackgroundColor = Colors.White;

        var cancelButton = new Button { Text = "Abbrechen" };
        cancelButton.Clicked += async (_, _) => await CancelAsync();

        var previewButton = new Button { Text = "Vorschau" };
        previewButton.Clicked += async (_, _) => await PreviewOrAcceptAsync();

        var content = new VerticalStackLayout
        {
            Padding = 24,
            Spacing = 14,
            Children =
            {
                new Label
                {
                    Text = "Pachtvertrag erstellen",
                    FontSize = 24,
                    FontAttributes = FontAttributes.Bold
                },
                new Label
                {
                    Text = "Der Pachtvertrag wird über den bestehenden parzellenbezogenen Template-Pfad erzeugt.",
                    TextColor = Colors.Gray,
                    LineBreakMode = LineBreakMode.WordWrap
                },
                CreateField("Mitglied", new Label { Text = BuildMemberDisplayName(member) }),
                CreateField("Parzelle", new Label { Text = BuildParzelleDisplayName(parzelle) }),
                CreateField("Vertragsbeginn", new Label { Text = _vertragsbeginn.ToString("dd.MM.yyyy", DeCulture) })
            }
        };

        if (_istMinderjaehrig)
        {
            content.Children.Add(CreateField(
                "Gesetzlicher Vertreter",
                new VerticalStackLayout
                {
                    Spacing = 4,
                    Children =
                    {
                        new Label { Text = BuildMemberDisplayName(_gesetzlicherVertreter!) },
                        new Label
                        {
                            Text = "Aus dem signierten Mitgliedsantrag übernommen. Änderungen erfolgen in den Mitgliedsdaten, nicht im Pachtvertrag.",
                            TextColor = Colors.Gray,
                            LineBreakMode = LineBreakMode.WordWrap
                        }
                    }
                }));
        }

        if (!_istMinderjaehrig && _nebenmitglied != null)
        {
            var includeSecondaryMemberCheckBox = new CheckBox
            {
                IsChecked = _includeSecondaryMember == true,
                VerticalOptions = LayoutOptions.Center
            };
            includeSecondaryMemberCheckBox.CheckedChanged += (_, args) => _includeSecondaryMember = args.Value;

            content.Children.Add(CreateField(
                "Pächter/in 2",
                new HorizontalStackLayout
                {
                    Spacing = 10,
                    Children =
                    {
                        includeSecondaryMemberCheckBox,
                        new Label
                        {
                            Text = $"{BuildMemberDisplayName(_nebenmitglied)} als Pächter/in 2 aufnehmen und unterschreiben lassen.",
                            VerticalOptions = LayoutOptions.Center,
                            LineBreakMode = LineBreakMode.WordWrap
                        }
                    }
                }));
        }

        content.Children.Add(new HorizontalStackLayout
        {
            Spacing = 12,
            HorizontalOptions = LayoutOptions.End,
            Children = { cancelButton, previewButton }
        });

        Content = new ScrollView { Content = content };
    }

    public Task<PachtvertragDokumentRequest?> WaitForResultAsync() => _resultSource.Task;

    protected override bool OnBackButtonPressed()
    {
        _resultSource.TrySetResult(null);
        return base.OnBackButtonPressed();
    }

    private async Task AcceptAsync()
    {
        var request = new PachtvertragDokumentRequest
        {
            MitgliedId = _member.Id,
            ParzelleId = _parzelle.Id,
            Vertragsbeginn = _vertragsbeginn,
            Status = FormularDokumentStatus.Unsigniert,
            IstMinderjaehrig = _istMinderjaehrig,
            AltvertragDatum = _altvertragDatum
        };

        if (!_altvertragEntscheidungErfasst)
        {
            var altvertragVorhanden = await DisplayAlert("Altvertrag", "Liegt ein Altvertrag vor?", "Ja", "Nein");
            if (altvertragVorhanden)
            {
                var datePicker = new DatePicker { Date = DateTime.Today };
                var resultSource = new TaskCompletionSource<bool?>();

                var promptPage = new ContentPage
                {
                    Title = "Altvertrag-Datum",
                    Content = new VerticalStackLayout
                    {
                        Padding = 24,
                        Spacing = 12,
                        Children =
                        {
                            new Label { Text = "Bitte Datum des Altvertrags eingeben.", LineBreakMode = LineBreakMode.WordWrap },
                            datePicker,
                            new HorizontalStackLayout
                            {
                                Spacing = 12,
                                HorizontalOptions = LayoutOptions.End,
                                Children =
                                {
                                    new Button { Text = "Abbrechen", Command = new Command(async () => { resultSource.TrySetResult(null); await Navigation.PopModalAsync(); }) },
                                    new Button { Text = "OK", Command = new Command(async () => { resultSource.TrySetResult(true); await Navigation.PopModalAsync(); }) }
                                }
                            }
                        }
                    }
                };

                await Navigation.PushModalAsync(new NavigationPage(promptPage));
                if (await resultSource.Task != true)
                    return;

                _altvertragDatum = datePicker.Date!.Value.Date;
            }

            _altvertragEntscheidungErfasst = true;
            request.AltvertragDatum = _altvertragDatum;
        }

        request.IncludeSecondaryMember = _includeSecondaryMember;
        _resultSource.TrySetResult(request);
        await Navigation.PopModalAsync();
    }

    private async Task PreviewOrAcceptAsync()
    {
        await AcceptAsync();
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

    private static string BuildMemberDisplayName(MitgliedRecord member)
    {
        var name = string.Join(" ", new[] { member.Vorname, member.Name }
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!.Trim()));
        return string.IsNullOrWhiteSpace(name) ? $"Mitglied #{member.Id}" : $"{name} · #{member.Id}";
    }

    private static string BuildParzelleDisplayName(ParzelleRecord parzelle)
    {
        var gartenNr = string.IsNullOrWhiteSpace(parzelle.GartenNr) ? $"#{parzelle.Id}" : parzelle.GartenNr.Trim();
        var anlage = string.IsNullOrWhiteSpace(parzelle.Anlage) ? null : parzelle.Anlage.Trim();
        return string.IsNullOrWhiteSpace(anlage) ? $"Garten {gartenNr}" : $"Garten {gartenNr} ({anlage})";
    }
}
