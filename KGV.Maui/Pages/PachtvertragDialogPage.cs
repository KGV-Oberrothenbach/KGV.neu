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

public sealed class PachtvertragDialogPage : ContentPage
{
    private static readonly CultureInfo DeCulture = CultureInfo.GetCultureInfo("de-DE");
    private readonly TaskCompletionSource<PachtvertragDokumentRequest?> _resultSource = new();
    private readonly MitgliedRecord _member;
    private readonly ParzelleRecord _parzelle;
    private readonly DateTime _vertragsbeginn;
    private readonly bool _istMinderjaehrig;
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
    private bool? _includeSecondaryForPreview;

    public PachtvertragDialogPage(
        MitgliedRecord member,
        ParzelleRecord parzelle,
        DateTime vertragsbeginn,
        GesetzlicherVertreterAufloesung? gesetzlicherVertreterAufloesung,
        IReadOnlyCollection<MitgliedRecord>? vertreterMitglieder,
        PachtvertragDokumentRequest? initialRequest = null)
    {
        _member = member ?? throw new ArgumentNullException(nameof(member));
        _parzelle = parzelle ?? throw new ArgumentNullException(nameof(parzelle));
        _vertragsbeginn = vertragsbeginn.Date;
        _istMinderjaehrig = gesetzlicherVertreterAufloesung?.IstMinderjaehrig ?? GesetzlicherVertreterResolver.IsMinderjaehrig(member, _vertragsbeginn);
        _mitgliedOptionen = (vertreterMitglieder ?? Array.Empty<MitgliedRecord>())
            .Where(x => x != null && x.Id > 0 && x.Id != member.Id)
            .Select(x => new MitgliedOption(x))
            .OrderBy(x => x.DisplayName, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        var activeVertreter = gesetzlicherVertreterAufloesung?.VertreterMitglied;
        if (activeVertreter != null && activeVertreter.Id > 0 && _mitgliedOptionen.All(x => x.MitgliedId != activeVertreter.Id))
            _mitgliedOptionen.Insert(0, new MitgliedOption(activeVertreter));

        Title = "Pachtvertrag";
        BackgroundColor = Colors.White;

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
                    Text = "Das Mitglied ist am Vertragsbeginn minderjährig. Deshalb werden Vertreterdaten und eine zusätzliche Vertreter-Unterschrift benötigt.",
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

        var previewButton = new Button { Text = "Vorschau" };
        previewButton.Clicked += async (_, _) => await PreviewOrAcceptAsync();

        // Altvertrag-Abfrage: ask before accepting

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
                    CreateField("Vertragsbeginn", new Label { Text = _vertragsbeginn.ToString("dd.MM.yyyy", DeCulture) }),
                    _vertreterRootSection,
                    new HorizontalStackLayout
                    {
                        Spacing = 12,
                        HorizontalOptions = LayoutOptions.End,
                        Children = { cancelButton, previewButton }
                    }
                }
            }
        };

        ApplyInitialVertreterState(initialRequest, gesetzlicherVertreterAufloesung);
        UpdateAdresseSection();
        UpdateVertreterMode();
    }

    public Task<PachtvertragDokumentRequest?> WaitForResultAsync() => _resultSource.Task;

    protected override bool OnBackButtonPressed()
    {
        _resultSource.TrySetResult(null);
        return base.OnBackButtonPressed();
    }

    private void ApplyInitialVertreterState(PachtvertragDokumentRequest? initialRequest, GesetzlicherVertreterAufloesung? gesetzlicherVertreterAufloesung)
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
        var request = new PachtvertragDokumentRequest
        {
            MitgliedId = _member.Id,
            ParzelleId = _parzelle.Id,
            Vertragsbeginn = _vertragsbeginn,
            Status = FormularDokumentStatus.Unsigniert,
            IstMinderjaehrig = _istMinderjaehrig
        };

        // Ask whether an existing previous contract (Altvertrag) exists
        var altvertragAnswer = await DisplayAlert("Altvertrag", "Liegt ein Altvertrag vor?", "Ja", "Nein");
        if (altvertragAnswer)
        {
            // prompt for date
            var datePicker = new DatePicker { Date = DateTime.Today };
            var ok = false;
            var tcs = new TaskCompletionSource<bool?>();

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
                                new Button { Text = "Abbrechen", Command = new Command(async () => { tcs.TrySetResult(null); await Navigation.PopModalAsync(); }) },
                                new Button { Text = "OK", Command = new Command(async () => { tcs.TrySetResult(true); await Navigation.PopModalAsync(); }) }
                            }
                        }
                    }
                }
            };

            await Navigation.PushModalAsync(new NavigationPage(promptPage));
            var res = await tcs.Task;
            if (res != true)
            {
                // cancelled
                return;
            }

            request.AltvertragDatum = datePicker.Date.Date;
        }

        if (_istMinderjaehrig)
        {
            if (_vertreterModusPicker.SelectedIndex != 1)
            {
                if (_bestehendesMitgliedPicker.SelectedItem is not MitgliedOption option)
                {
                    await DisplayAlert("Pachtvertrag", "Bitte ein vorhandenes Mitglied als gesetzlichen Vertreter auswählen.", "OK");
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
                    await DisplayAlert("Pachtvertrag", "Bitte Vorname und Nachname des gesetzlichen Vertreters eingeben.", "OK");
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
                        await DisplayAlert("Pachtvertrag", "Bitte die abweichende Anschrift des gesetzlichen Vertreters vollständig eingeben.", "OK");
                        _vertreterAdresseEntry.Focus();
                        return;
                    }
                }
            }
        }

        // propagate the temporary include-secondary choice (null = server default)
        request.IncludeSecondaryMember = _includeSecondaryForPreview;
        _includeSecondaryForPreview = null;
        _resultSource.TrySetResult(request);
        await Navigation.PopModalAsync();
    }

    private async Task PreviewOrAcceptAsync()
    {
        // Before calling AcceptAsync, check whether a secondary member exists and offer to include them as Pächter2
        try
        {
            // Try to resolve ISupabaseService via the MAUI application context if available.
            var services = App.Current?.Handler?.MauiContext?.Services;
            var supabase = services is null ? null : (KGV.Core.Interfaces.ISupabaseService?)services.GetService(typeof(KGV.Core.Interfaces.ISupabaseService));
            var secondary = supabase is null ? null : await supabase.GetNebenmitgliedByHauptmitgliedIdAsync(_member.Id);
            if (secondary != null)
            {
                var include = await DisplayAlert("Nebenmitglied", $"Für dieses Mitglied existiert ein Nebenmitglied ({secondary.Vorname} {secondary.Name}). Soll dieses als Pächter 2 in den Pachtvertrag aufgenommen werden?", "Ja", "Nein");
                if (include)
                {
                    // set request preference via temporary state on page and then call AcceptAsync
                    // We'll create a small wrapper: temporarily set a field and call AcceptAsync
                    _includeSecondaryForPreview = true;
                }
                else
                {
                    _includeSecondaryForPreview = false;
                }
            }
        }
        catch
        {
            // ignore lookup errors and proceed
            _includeSecondaryForPreview = null;
        }

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