using System;
using System.Collections.Generic;
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
    private readonly TaskCompletionSource<MitgliedsantragDokumentRequest?> _resultSource = new();
    private readonly MitgliedRecord _member;
    private readonly MitgliedsantragBeitragVorschlag _vorschlag;
    private readonly bool _istMinderjaehrig;
    private readonly Entry _mitgliedsbeitragEntry;
    private readonly Picker _vertreterModusPicker;
    private readonly Picker _bestehendesMitgliedPicker;
    private readonly VerticalStackLayout _vertreterRootSection;
    private readonly VerticalStackLayout _bestehendesMitgliedSection;
    private readonly VerticalStackLayout _manuelleVertreterSection;
    private readonly Entry _vertreterVornameEntry;
    private readonly Entry _vertreterNachnameEntry;
    private readonly CheckBox _vertreterAdresseAbweichendCheckBox;
    private readonly VerticalStackLayout _abweichendeAdresseSection;
    private readonly Entry _vertreterAdresseEntry;
    private readonly Entry _vertreterPlzEntry;
    private readonly Entry _vertreterOrtEntry;
    private readonly List<MitgliedOption> _mitgliedOptionen;

    public MitgliedsantragDialogPage(
        MitgliedRecord member,
        MitgliedsantragBeitragVorschlag vorschlag,
        GesetzlicherVertreterAufloesung? gesetzlicherVertreterAufloesung,
        IReadOnlyCollection<MitgliedRecord>? vertreterMitglieder,
        MitgliedsantragDokumentRequest? initialRequest = null)
    {
        _member = member ?? throw new ArgumentNullException(nameof(member));
        _vorschlag = vorschlag ?? throw new ArgumentNullException(nameof(vorschlag));
        _istMinderjaehrig = gesetzlicherVertreterAufloesung?.IstMinderjaehrig ?? GesetzlicherVertreterResolver.IsMinderjaehrig(member, vorschlag.BeginnDatum);
        _mitgliedOptionen = (vertreterMitglieder ?? Array.Empty<MitgliedRecord>())
            .Where(x => x != null && x.Id > 0 && x.Id != member.Id)
            .Select(x => new MitgliedOption(x))
            .OrderBy(x => x.DisplayName, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        var activeVertreter = gesetzlicherVertreterAufloesung?.VertreterMitglied;
        if (activeVertreter != null && activeVertreter.Id > 0 && _mitgliedOptionen.All(x => x.MitgliedId != activeVertreter.Id))
            _mitgliedOptionen.Insert(0, new MitgliedOption(activeVertreter));

        var displayName = string.Join(' ', new[] { member.Vorname, member.Name }
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!.Trim()));
        if (string.IsNullOrWhiteSpace(displayName))
            displayName = $"Mitglied #{member.Id}";

        Title = "Mitgliedsantrag";
        BackgroundColor = Colors.White;

        _mitgliedsbeitragEntry = new Entry
        {
            Text = MitgliedsantragBeitragHelper.NormalizeBeitrag(initialRequest?.Mitgliedsbeitrag ?? vorschlag.VorgeschlagenerBeitrag).ToString("0.00", DeCulture),
            Keyboard = Microsoft.Maui.Keyboard.Numeric,
            Placeholder = "Mitgliedsbeitrag"
        };

        _vertreterModusPicker = new Picker { Title = "Vertreterdaten" };
        _vertreterModusPicker.ItemsSource = new List<string>
        {
            "Vorhandenes Mitglied auswählen",
            "Vertreter manuell erfassen"
        };
        _vertreterModusPicker.SelectedIndexChanged += (_, _) => UpdateVertreterMode();

        _bestehendesMitgliedPicker = new Picker { Title = "Vorhandenes Mitglied" };
        _bestehendesMitgliedPicker.ItemsSource = _mitgliedOptionen;
        _bestehendesMitgliedPicker.ItemDisplayBinding = new Binding(nameof(MitgliedOption.DisplayName));

        _bestehendesMitgliedSection = new VerticalStackLayout
        {
            Spacing = 8,
            Children =
            {
                new Label
                {
                    Text = "Vorhandenes Mitglied als gesetzlichen Vertreter verwenden",
                    TextColor = Colors.Gray,
                    LineBreakMode = LineBreakMode.WordWrap
                },
                _bestehendesMitgliedPicker
            }
        };

        _vertreterVornameEntry = new Entry { Placeholder = "Vorname Vertreter/in" };
        _vertreterNachnameEntry = new Entry { Placeholder = "Nachname Vertreter/in" };
        _vertreterAdresseAbweichendCheckBox = new CheckBox();
        _vertreterAdresseAbweichendCheckBox.CheckedChanged += (_, _) => UpdateAdresseSection();
        _vertreterAdresseEntry = new Entry { Placeholder = "Adresse" };
        _vertreterPlzEntry = new Entry { Placeholder = "PLZ", Keyboard = Keyboard.Numeric };
        _vertreterOrtEntry = new Entry { Placeholder = "Ort" };
        _abweichendeAdresseSection = new VerticalStackLayout
        {
            Spacing = 8,
            Children =
            {
                _vertreterAdresseEntry,
                _vertreterPlzEntry,
                _vertreterOrtEntry
            }
        };

        _manuelleVertreterSection = new VerticalStackLayout
        {
            Spacing = 8,
            Children =
            {
                new Label
                {
                    Text = "Wenn diese Person noch kein Mitglied ist, wird sie beim finalen Speichern als Nebenmitglied angelegt und anschließend als gesetzlicher Vertreter verknüpft.",
                    TextColor = Colors.Gray,
                    LineBreakMode = LineBreakMode.WordWrap
                },
                _vertreterVornameEntry,
                _vertreterNachnameEntry,
                new HorizontalStackLayout
                {
                    Spacing = 8,
                    Children =
                    {
                        _vertreterAdresseAbweichendCheckBox,
                        new Label { Text = "Adresse weicht von der Anschrift des Mitglieds ab", VerticalTextAlignment = TextAlignment.Center }
                    }
                },
                _abweichendeAdresseSection
            }
        };

        _vertreterRootSection = new VerticalStackLayout
        {
            Spacing = 10,
            IsVisible = _istMinderjaehrig,
            Children =
            {
                new Label
                {
                    Text = "Gesetzlicher Vertreter",
                    FontAttributes = FontAttributes.Bold,
                    FontSize = 18
                },
                new Label
                {
                    Text = "Das aufzunehmende Mitglied ist am Antragsbeginn minderjährig. Deshalb werden Vertreterdaten und eine zusätzliche Vertreter-Unterschrift benötigt.",
                    TextColor = Colors.Gray,
                    LineBreakMode = LineBreakMode.WordWrap
                },
                _vertreterModusPicker,
                _bestehendesMitgliedSection,
                _manuelleVertreterSection
            }
        };

        var cancelButton = new Button { Text = "Abbrechen" };
        cancelButton.Clicked += async (_, _) => await CancelAsync();

        var createButton = new Button { Text = "Vorschau" };
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
                    _vertreterRootSection,
                    new HorizontalStackLayout
                    {
                        Spacing = 12,
                        HorizontalOptions = LayoutOptions.End,
                        Children = { cancelButton, createButton }
                    }
                }
            }
        };

        ApplyInitialVertreterState(initialRequest, gesetzlicherVertreterAufloesung);
        UpdateAdresseSection();
        UpdateVertreterMode();
    }

    public Task<MitgliedsantragDokumentRequest?> WaitForResultAsync() => _resultSource.Task;

    protected override bool OnBackButtonPressed()
    {
        _resultSource.TrySetResult(null);
        return base.OnBackButtonPressed();
    }

    private void ApplyInitialVertreterState(MitgliedsantragDokumentRequest? initialRequest, GesetzlicherVertreterAufloesung? gesetzlicherVertreterAufloesung)
    {
        if (!_istMinderjaehrig)
            return;

        if (initialRequest?.GesetzlicherVertreterAusBestehendemMitglied == true && initialRequest.GesetzlicherVertreterMitgliedId is > 0)
        {
            _vertreterModusPicker.SelectedIndex = 0;
            _bestehendesMitgliedPicker.SelectedItem = _mitgliedOptionen.FirstOrDefault(x => x.MitgliedId == initialRequest.GesetzlicherVertreterMitgliedId.Value);
            return;
        }

        if (initialRequest?.GesetzlicherVertreterSnapshot != null)
        {
            _vertreterModusPicker.SelectedIndex = initialRequest.GesetzlicherVertreterAusBestehendemMitglied ? 0 : 1;
            if (initialRequest.GesetzlicherVertreterAusBestehendemMitglied && initialRequest.GesetzlicherVertreterMitgliedId is > 0)
            {
                _bestehendesMitgliedPicker.SelectedItem = _mitgliedOptionen.FirstOrDefault(x => x.MitgliedId == initialRequest.GesetzlicherVertreterMitgliedId.Value);
                return;
            }

            ApplyManualSnapshot(initialRequest.GesetzlicherVertreterSnapshot, initialRequest.GesetzlicherVertreterAdresseAbweichend);
            return;
        }

        if (gesetzlicherVertreterAufloesung?.HatAktivenGesetzlichenVertreter == true && gesetzlicherVertreterAufloesung.VertreterMitglied != null)
        {
            _vertreterModusPicker.SelectedIndex = 0;
            _bestehendesMitgliedPicker.SelectedItem = _mitgliedOptionen.FirstOrDefault(x => x.MitgliedId == gesetzlicherVertreterAufloesung.VertreterMitglied.Id);
            return;
        }

        _vertreterModusPicker.SelectedIndex = _mitgliedOptionen.Count > 0 ? 0 : 1;
    }

    private void ApplyManualSnapshot(MitgliedsantragVertreterSnapshot snapshot, bool adresseAbweichend)
    {
        _vertreterVornameEntry.Text = snapshot.Vorname;
        _vertreterNachnameEntry.Text = snapshot.Nachname;
        _vertreterAdresseAbweichendCheckBox.IsChecked = adresseAbweichend;
        _vertreterAdresseEntry.Text = snapshot.Adresse;
        _vertreterPlzEntry.Text = snapshot.Plz;
        _vertreterOrtEntry.Text = snapshot.Ort;
    }

    private void UpdateVertreterMode()
    {
        var existingMode = _vertreterModusPicker.SelectedIndex != 1;
        _bestehendesMitgliedSection.IsVisible = _istMinderjaehrig && existingMode;
        _manuelleVertreterSection.IsVisible = _istMinderjaehrig && !existingMode;
    }

    private void UpdateAdresseSection()
        => _abweichendeAdresseSection.IsVisible = _vertreterAdresseAbweichendCheckBox.IsChecked;

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

        var request = new MitgliedsantragDokumentRequest
        {
            MitgliedId = _member.Id,
            BeginnDatum = _vorschlag.BeginnDatum,
            Mitgliedsbeitrag = MitgliedsantragBeitragHelper.NormalizeBeitrag(beitrag),
            Status = FormularDokumentStatus.Unsigniert,
            IstMinderjaehrig = _istMinderjaehrig
        };

        if (_istMinderjaehrig)
        {
            if (_vertreterModusPicker.SelectedIndex != 1)
            {
                if (_bestehendesMitgliedPicker.SelectedItem is not MitgliedOption option)
                {
                    await DisplayAlert("Mitgliedsantrag", "Bitte ein vorhandenes Mitglied als gesetzlichen Vertreter auswählen.", "OK");
                    _bestehendesMitgliedPicker.Focus();
                    return;
                }

                request.GesetzlicherVertreterAusBestehendemMitglied = true;
                request.GesetzlicherVertreterMitgliedId = option.MitgliedId;
                request.GesetzlicherVertreterSnapshot = option.ToSnapshot();
            }
            else
            {
                var vorname = (_vertreterVornameEntry.Text ?? string.Empty).Trim();
                var nachname = (_vertreterNachnameEntry.Text ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(vorname) || string.IsNullOrWhiteSpace(nachname))
                {
                    await DisplayAlert("Mitgliedsantrag", "Bitte Vorname und Nachname des gesetzlichen Vertreters eingeben.", "OK");
                    _vertreterVornameEntry.Focus();
                    return;
                }

                request.GesetzlicherVertreterAusBestehendemMitglied = false;
                request.GesetzlicherVertreterAdresseAbweichend = _vertreterAdresseAbweichendCheckBox.IsChecked;
                request.GesetzlicherVertreterSnapshot = new MitgliedsantragVertreterSnapshot
                {
                    Vorname = vorname,
                    Nachname = nachname,
                    Adresse = (_vertreterAdresseEntry.Text ?? string.Empty).Trim(),
                    Plz = (_vertreterPlzEntry.Text ?? string.Empty).Trim(),
                    Ort = (_vertreterOrtEntry.Text ?? string.Empty).Trim()
                };

                if (request.GesetzlicherVertreterAdresseAbweichend)
                {
                    if (string.IsNullOrWhiteSpace(request.GesetzlicherVertreterSnapshot.Adresse)
                        || string.IsNullOrWhiteSpace(request.GesetzlicherVertreterSnapshot.Plz)
                        || string.IsNullOrWhiteSpace(request.GesetzlicherVertreterSnapshot.Ort))
                    {
                        await DisplayAlert("Mitgliedsantrag", "Bitte die abweichende Anschrift des gesetzlichen Vertreters vollständig eingeben.", "OK");
                        _vertreterAdresseEntry.Focus();
                        return;
                    }
                }
            }
        }

        _resultSource.TrySetResult(request);
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

    private sealed class MitgliedOption
    {
        private readonly MitgliedRecord _member;

        public MitgliedOption(MitgliedRecord member)
        {
            _member = member;
            var name = string.Join(" ", new[] { member.Vorname, member.Name }
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x!.Trim()));
            DisplayName = string.IsNullOrWhiteSpace(name) ? $"Mitglied #{member.Id}" : $"{name} · #{member.Id}";
        }

        public int MitgliedId => _member.Id;
        public string DisplayName { get; }

        public MitgliedsantragVertreterSnapshot ToSnapshot()
        {
            return new MitgliedsantragVertreterSnapshot
            {
                VertreterMitgliedId = _member.Id,
                Vorname = _member.Vorname?.Trim() ?? string.Empty,
                Nachname = _member.Name?.Trim() ?? string.Empty,
                Adresse = _member.Adresse?.Trim() ?? string.Empty,
                Plz = _member.Plz?.Trim() ?? string.Empty,
                Ort = _member.Ort?.Trim() ?? string.Empty,
                Telefon = _member.Telefon?.Trim() ?? string.Empty,
                Handy = _member.Handy?.Trim() ?? string.Empty,
                Email = _member.Email?.Trim() ?? string.Empty
            };
        }
    }
}
