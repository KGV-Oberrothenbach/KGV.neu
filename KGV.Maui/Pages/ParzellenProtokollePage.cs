using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Extensions.DependencyInjection;
using KGV.Core.Interfaces;
using KGV.Core.Models;
using KGV.Maui.State;
using Microsoft.Maui.Media;
using System.Collections.ObjectModel;

namespace KGV.Maui.Pages;

/// <summary>Gemeinsamer Einstieg für Mitglieds-Protokolle mit Parzellenbezug.</summary>
public sealed class ParzellenProtokollePage : ContentPage
{
    private readonly ISupabaseService _supabase;
    private readonly UserContextState _userContext;
    private readonly Picker _typPicker = new() { Title = "Protokollart" };
    private readonly Picker _mitgliedPicker = new() { Title = "Mitglied auswählen" };
    private readonly Picker _parzellePicker = new() { Title = "Parzelle auswählen" };
    private readonly Picker _vorstand2Picker = new() { Title = "Zweiten Vorstand auswählen" };
    private readonly Switch _begleitpersonSwitch = new();
    private readonly Picker _begleitmitgliedPicker = new() { Title = "Nebenmitglied auswählen" };
    private readonly Entry _begleitpersonName = new() { Placeholder = "Name der Begleitperson" };
    private readonly Label _vorstand1Label = new() { TextColor = Colors.DimGray };
    private readonly Picker _wasserQuellePicker = new() { Title = "Wasserstand auswählen" };
    private readonly Picker _stromQuellePicker = new() { Title = "Stromstand auswählen" };
    private readonly DatePicker _protokollDatumPicker = new() { Date = DateTime.Today };
    private readonly Entry _anlassEntry = new() { Placeholder = "z. B. Pächterwechsel, Rückgabe oder turnusmäßige Begehung" };
    private readonly Editor _zustandEditor = new() { Placeholder = "Zustand, Hinweise und Auffälligkeiten", AutoSize = EditorAutoSizeOption.TextChanges, MinimumHeightRequest = 90 };
    private readonly Editor _vereinbarungEditor = new() { Placeholder = "Vereinbarungen, Fristen und nächste Schritte", AutoSize = EditorAutoSizeOption.TextChanges, MinimumHeightRequest = 80 };
    private readonly Label _wasserStandLabel = new() { TextColor = Colors.DimGray, LineBreakMode = LineBreakMode.WordWrap };
    private readonly Label _stromStandLabel = new() { TextColor = Colors.DimGray, LineBreakMode = LineBreakMode.WordWrap };
    private readonly ObservableCollection<ProtocolPhotoItem> _photos = new();
    private readonly VerticalStackLayout _photoList = new() { Spacing = 5 };
    private readonly Label _photoHint = new() { TextColor = Colors.DimGray };
    private readonly Label _signatureHint = new() { TextColor = Colors.DimGray, LineBreakMode = LineBreakMode.WordWrap };
    private DigitalSignatureCapture? _paechterSignature;
    private DigitalSignatureCapture? _begleitpersonSignature;
    private DigitalSignatureCapture? _vorstand1Signature;
    private DigitalSignatureCapture? _vorstand2Signature;
    private readonly VerticalStackLayout _form = new() { Spacing = 10, IsVisible = false };
    private readonly VerticalStackLayout _readingSection = new() { Spacing = 10 };
    private ZaehlerAblesungDTO? _lastWasserReading;
    private ZaehlerAblesungDTO? _lastStromReading;
    private bool _loaded;

    public ParzellenProtokollePage()
    {
        var services = Application.Current?.Handler?.MauiContext?.Services
            ?? throw new InvalidOperationException("MAUI-Services sind aktuell nicht verfügbar.");
        _supabase = services.GetRequiredService<ISupabaseService>();
        _userContext = services.GetRequiredService<UserContextState>();
        Title = "Protokolle";
        BackgroundColor = Colors.White;

        _typPicker.ItemsSource = new[] { "Übernahmeprotokoll", "Rückgabeprotokoll", "Begehungsprotokoll" };
        _typPicker.SelectedIndexChanged += (_, _) =>
        {
            _form.IsVisible = _typPicker.SelectedIndex >= 0;
            _readingSection.IsVisible = _typPicker.SelectedIndex is 0 or 1;
        };
        _begleitpersonSwitch.Toggled += (_, e) =>
        {
            _begleitmitgliedPicker.IsVisible = e.Value;
            _begleitpersonName.IsVisible = e.Value;
        };
        _parzellePicker.SelectedIndexChanged += async (_, _) => await LoadLastReadingsAsync();
        _wasserQuellePicker.ItemsSource = new[] { "Letzte Ablesung übernehmen", "Neu ablesen" };
        _stromQuellePicker.ItemsSource = new[] { "Letzte Ablesung übernehmen", "Neu ablesen" };
        _wasserQuellePicker.SelectedIndexChanged += async (_, _) => await HandleReadingChoiceAsync("wasser", _wasserQuellePicker);
        _stromQuellePicker.SelectedIndexChanged += async (_, _) => await HandleReadingChoiceAsync("strom", _stromQuellePicker);

        _form.Children.Add(CreateField("Mitglied", _mitgliedPicker));
        _form.Children.Add(CreateField("Parzelle", _parzellePicker));
        _form.Children.Add(CreateField("Vorstand 1", _vorstand1Label));
        _form.Children.Add(CreateField("Vorstand 2", _vorstand2Picker));
        _form.Children.Add(CreateField("Pächter bringt Begleitperson mit", _begleitpersonSwitch));
        _begleitmitgliedPicker.IsVisible = false;
        _begleitpersonName.IsVisible = false;
        _form.Children.Add(_begleitmitgliedPicker);
        _form.Children.Add(_begleitpersonName);
        _readingSection.Children.Add(CreateField("Wasser", new VerticalStackLayout { Spacing = 4, Children = { _wasserQuellePicker, _wasserStandLabel } }));
        _readingSection.Children.Add(CreateField("Strom", new VerticalStackLayout { Spacing = 4, Children = { _stromQuellePicker, _stromStandLabel } }));
        _form.Children.Add(_readingSection);
        _form.Children.Add(CreateField("Protokolldatum", _protokollDatumPicker));
        _form.Children.Add(CreateField("Anlass", _anlassEntry));
        _form.Children.Add(CreateField("Zustand / Feststellungen", _zustandEditor));
        _form.Children.Add(CreateField("Vereinbarungen", _vereinbarungEditor));
        var capturePhotoButton = new Button { Text = "Foto aufnehmen" };
        capturePhotoButton.Clicked += async (_, _) => await AddPhotoAsync(true);
        var pickPhotoButton = new Button { Text = "Foto auswählen" };
        pickPhotoButton.Clicked += async (_, _) => await AddPhotoAsync(false);
        _photoHint.Text = "Noch keine Fotos ausgewählt (maximal 10).";
        _form.Children.Add(CreateField("Fotoanlagen", new VerticalStackLayout
        {
            Spacing = 6,
            Children =
            {
                new FlexLayout { Direction = FlexDirection.Row, Wrap = FlexWrap.Wrap, Children = { capturePhotoButton, pickPhotoButton } },
                _photoHint,
                _photoList
            }
        }));
        var signaturesButton = new Button { Text = "Unterschriften erfassen" };
        signaturesButton.Clicked += async (_, _) => await CaptureSignaturesAsync();
        _signatureHint.Text = "Unterschriften noch nicht erfasst.";
        _form.Children.Add(CreateField("Unterschriften", new VerticalStackLayout { Spacing = 5, Children = { signaturesButton, _signatureHint } }));
        var saveDraftButton = new Button { Text = "Entwurf speichern" };
        saveDraftButton.Clicked += async (_, _) => await SaveDraftAsync();
        _form.Children.Add(saveDraftButton);

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = 20,
                Spacing = 14,
                Children =
                {
                    new Label { Text = "Parzellen-Protokolle", FontSize = 24, FontAttributes = FontAttributes.Bold },
                    new Label
                    {
                        Text = "Protokolle werden dem Mitglied zugeordnet. Die Parzelle, Ablesungen, Fotos und Unterschriften bilden den nachvollziehbaren fachlichen Bezug.",
                        LineBreakMode = LineBreakMode.WordWrap,
                        TextColor = Colors.DimGray
                    },
                    CreateField("Protokollart", _typPicker),
                    _form
                }
            }
        };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_loaded)
        {
            await LoadLastReadingsAsync();
            return;
        }
        _loaded = true;
        try
        {
            var members = (await _supabase.GetMitgliederAsync()).Where(m => m.Aktiv).OrderBy(m => m.Name).ToList();
            _mitgliedPicker.ItemsSource = members;
            _mitgliedPicker.ItemDisplayBinding = new Binding("Name");
            _begleitmitgliedPicker.ItemsSource = members;
            _begleitmitgliedPicker.ItemDisplayBinding = new Binding("Name");
            _parzellePicker.ItemsSource = (await _supabase.GetAllParzellenAsync()).Where(p => p.Aktiv).OrderBy(p => p.GartenNrSortKey).ToList();
            _parzellePicker.ItemDisplayBinding = new Binding("DisplayName");
            _vorstand2Picker.ItemsSource = members.Where(m => string.Equals(m.Role, "admin", StringComparison.OrdinalIgnoreCase) || string.Equals(m.Role, "vorstand", StringComparison.OrdinalIgnoreCase)).ToList();
            _vorstand2Picker.ItemDisplayBinding = new Binding("Name");
            _vorstand1Label.Text = _userContext.CurrentMitgliedId is long id
                ? (members.FirstOrDefault(m => m.Id == id) is { } current ? $"{current.Vorname} {current.Name}" : "Angemeldetes Vorstandsmitglied")
                : "Angemeldetes Vorstandsmitglied";
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Protokolle", $"Stammdaten konnten nicht geladen werden: {ex.Message}", "OK");
        }
    }

    private async Task SaveDraftAsync()
    {
        if (_mitgliedPicker.SelectedItem is not MitgliedRecord || _parzellePicker.SelectedItem is not ParzelleRecord || _vorstand2Picker.SelectedItem is not MitgliedRecord)
        {
            await DisplayAlertAsync("Validierung", "Bitte Mitglied, Parzelle und das zweite Vorstandsmitglied auswählen.", "OK");
            return;
        }
        if (_begleitpersonSwitch.IsToggled && _begleitmitgliedPicker.SelectedItem is not MitgliedRecord && string.IsNullOrWhiteSpace(_begleitpersonName.Text))
        {
            await DisplayAlertAsync("Validierung", "Bitte ein Nebenmitglied auswählen oder den Namen der Begleitperson eintragen.", "OK");
            return;
        }
        if (_mitgliedPicker.SelectedItem is not MitgliedRecord member || _parzellePicker.SelectedItem is not ParzelleRecord parcel || _vorstand2Picker.SelectedItem is not MitgliedRecord board2 || _userContext.CurrentMitgliedId is not long board1Id)
            return;

        var protocolType = _typPicker.SelectedIndex switch { 0 => "uebernahme", 1 => "rueckgabe", _ => "begehung" };
        var request = new ParzellenProtokollCreateRequest
        {
            Protokoll = new ParzellenProtokollInsertRecord
            {
                ParzelleId = parcel.Id,
                MitgliedId = member.Id,
                ProtokollTyp = protocolType,
                ProtokollDatum = _protokollDatumPicker.Date ?? DateTime.Today,
                VorstandMitgliedId = board1Id,
                Vorstand2MitgliedId = board2.Id,
                BegleitpersonMitgliedId = (_begleitpersonSwitch.IsToggled ? (_begleitmitgliedPicker.SelectedItem as MitgliedRecord)?.Id : null),
                BegleitpersonName = _begleitpersonSwitch.IsToggled ? _begleitpersonName.Text?.Trim() : null,
                Anlass = _anlassEntry.Text?.Trim(),
                ZustandBemerkung = _zustandEditor.Text?.Trim(),
                Vereinbarung = _vereinbarungEditor.Text?.Trim(),
                PaechterSigniertAm = _paechterSignature?.HasContent == true ? DateTime.UtcNow : null,
                BegleitpersonSigniertAm = _begleitpersonSignature?.HasContent == true ? DateTime.UtcNow : null,
                Vorstand1SigniertAm = _vorstand1Signature?.HasContent == true ? DateTime.UtcNow : null,
                Vorstand2SigniertAm = _vorstand2Signature?.HasContent == true ? DateTime.UtcNow : null
            },
            Ablesungen = BuildReadingSnapshots()
        };

        var saved = await _supabase.CreateParzellenProtokollAsync(request);
        await DisplayAlertAsync("Protokolle", saved
            ? "Der Protokoll-Entwurf wurde gespeichert. Die verbindliche PDF wird im nächsten Schritt erzeugt und dem Mitglied zugeordnet."
            : "Der Protokoll-Entwurf konnte nicht gespeichert werden.", "OK");
    }

    private async Task LoadLastReadingsAsync()
    {
        if (_parzellePicker.SelectedItem is not ParzelleRecord parzelle) return;
        try
        {
            _lastWasserReading = (await _supabase.GetWasserAblesungenAsync(parzelle.Id)).OrderByDescending(x => x.Ablesedatum).FirstOrDefault();
            _lastStromReading = (await _supabase.GetStromAblesungenAsync(parzelle.Id)).OrderByDescending(x => x.Ablesedatum).FirstOrDefault();
            _wasserStandLabel.Text = FormatLastReading(_lastWasserReading, "Wasser");
            _stromStandLabel.Text = FormatLastReading(_lastStromReading, "Strom");
        }
        catch
        {
            _wasserStandLabel.Text = "Letzte Wasserablesung konnte nicht geladen werden.";
            _stromStandLabel.Text = "Letzte Stromablesung konnte nicht geladen werden.";
        }
    }

    private async Task HandleReadingChoiceAsync(string medium, Picker picker)
    {
        if (picker.SelectedIndex != 1 || _parzellePicker.SelectedItem is not ParzelleRecord parzelle) return;
        var art = _typPicker.SelectedIndex == 0 ? AblesungArt.PachtAnfang : AblesungArt.PachtEnde;
        await Shell.Current.GoToAsync($"{nameof(AblesungErfassenPage)}?parzelleId={parzelle.Id}&medium={medium}&art={art}");
        picker.SelectedIndex = -1;
    }

    private static string FormatLastReading(ZaehlerAblesungDTO? value, string medium) => value == null
        ? $"Keine frühere {medium}ablesung vorhanden."
        : $"Letzte Ablesung: {value.Stand:0.##} ({value.Zaehlernummer}), {value.Ablesedatum:dd.MM.yyyy}";

    private IReadOnlyList<ParzellenProtokollAblesungInsertRecord> BuildReadingSnapshots()
    {
        if (_typPicker.SelectedIndex is not (0 or 1)) return Array.Empty<ParzellenProtokollAblesungInsertRecord>();
        return new[]
        {
            CreateReadingSnapshot("wasser", _wasserQuellePicker, _lastWasserReading),
            CreateReadingSnapshot("strom", _stromQuellePicker, _lastStromReading)
        }.Where(x => x != null).Cast<ParzellenProtokollAblesungInsertRecord>().ToList();
    }

    private static ParzellenProtokollAblesungInsertRecord? CreateReadingSnapshot(string medium, Picker sourcePicker, ZaehlerAblesungDTO? reading)
    {
        if (sourcePicker.SelectedIndex < 0 || reading == null) return null;
        return new ParzellenProtokollAblesungInsertRecord
        {
            Medium = medium,
            ZaehlerId = reading.ZaehlerId,
            AblesungId = reading.AblesungId,
            Quelle = sourcePicker.SelectedIndex == 1 ? "neu_abgelesen" : "letzte_uebernommen",
            Ablesedatum = reading.Ablesedatum,
            Zaehlernummer = reading.Zaehlernummer,
            Stand = reading.Stand,
            FotoPfad = reading.FotoPfad,
            FotoDateiname = reading.FotoDateiname,
            FotoDriveFileId = reading.FotoDriveFileId
        };
    }

    private async Task AddPhotoAsync(bool capture)
    {
        if (_photos.Count >= 10)
        {
            await DisplayAlertAsync("Fotos", "Es können maximal 10 Fotos zum Protokoll hinzugefügt werden.", "OK");
            return;
        }
        try
        {
            var result = capture
                ? await MediaPicker.Default.CapturePhotoAsync()
                : (await MediaPicker.Default.PickPhotosAsync()).FirstOrDefault();
            if (result == null) return;
            await using var source = await result.OpenReadAsync();
            using var memory = new MemoryStream();
            await source.CopyToAsync(memory);
            _photos.Add(new ProtocolPhotoItem(result.FileName ?? $"protokollfoto-{_photos.Count + 1}.jpg", memory.ToArray()));
            RefreshPhotoList();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Fotos", $"Foto konnte nicht hinzugefügt werden: {ex.Message}", "OK");
        }
    }

    private void RefreshPhotoList()
    {
        _photoList.Children.Clear();
        foreach (var photo in _photos)
        {
            var remove = new Button { Text = "Entfernen", FontSize = 12 };
            remove.Clicked += (_, _) => { _photos.Remove(photo); RefreshPhotoList(); };
            _photoList.Children.Add(new HorizontalStackLayout { Spacing = 8, Children = { new Label { Text = $"Foto {_photos.IndexOf(photo) + 1}: {photo.FileName}", VerticalOptions = LayoutOptions.Center }, remove } });
        }
        _photoHint.Text = _photos.Count == 0 ? "Noch keine Fotos ausgewählt (maximal 10)." : $"{_photos.Count} von 10 Fotos ausgewählt.";
    }

    private sealed record ProtocolPhotoItem(string FileName, byte[] Content);

    private async Task CaptureSignaturesAsync()
    {
        if (_mitgliedPicker.SelectedItem is not MitgliedRecord member || _vorstand2Picker.SelectedItem is not MitgliedRecord board2)
        {
            await DisplayAlertAsync("Unterschriften", "Bitte zuerst Mitglied und zweiten Vorstand auswählen.", "OK");
            return;
        }

        var document = new DocumentInfo { Title = _typPicker.SelectedItem as string ?? "Parzellenprotokoll", Name = "Parzellenprotokoll" };
        _paechterSignature = await SignatureFlowHelper.CaptureSignatureAsync(Navigation, document,
            _typPicker.SelectedIndex == 2 ? "Kenntnisnahme Pächter/in" : "Unterschrift Pächter/in", false);
        if (_paechterSignature == null) { UpdateSignatureHint(member, board2); return; }

        if (_begleitpersonSwitch.IsToggled)
        {
            _begleitpersonSignature = await SignatureFlowHelper.CaptureSignatureAsync(Navigation, document, "Unterschrift Begleitperson", false);
            if (_begleitpersonSignature == null) { UpdateSignatureHint(member, board2); return; }
        }

        _vorstand1Signature = await SignatureFlowHelper.CaptureSignatureAsync(Navigation, document, "Unterschrift Vorstand 1", false);
        if (_vorstand1Signature == null) { UpdateSignatureHint(member, board2); return; }
        _vorstand2Signature = await SignatureFlowHelper.CaptureSignatureAsync(Navigation, document, "Unterschrift Vorstand 2", true);
        UpdateSignatureHint(member, board2);
    }

    private void UpdateSignatureHint(MitgliedRecord member, MitgliedRecord board2)
    {
        var lines = new List<string>
        {
            $"Pächter {member.Vorname} {member.Name}: {(_paechterSignature?.HasContent == true ? "erfasst" : "offen")}",
            $"Vorstand 1: {(_vorstand1Signature?.HasContent == true ? "erfasst" : "offen")}",
            $"Vorstand 2 {board2.Vorname} {board2.Name}: {(_vorstand2Signature?.HasContent == true ? "erfasst" : "offen")}" 
        };
        if (_begleitpersonSwitch.IsToggled)
            lines.Insert(1, $"Begleitperson: {(_begleitpersonSignature?.HasContent == true ? "erfasst" : "offen")}");
        _signatureHint.Text = string.Join(Environment.NewLine, lines);
    }

    private static View CreateField(string label, View field) => new VerticalStackLayout { Spacing = 3, Children = { new Label { Text = label, FontAttributes = FontAttributes.Bold }, field } };

}
