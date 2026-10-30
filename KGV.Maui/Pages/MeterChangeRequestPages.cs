using KGV.Core.Interfaces;
using KGV.Core.Models;
using KGV.Core.Security;
using KGV.Maui.State;
using Microsoft.Maui.Controls;
using System.Collections.ObjectModel;
using System.Globalization;

namespace KGV.Maui.Pages;

public sealed class MeterChangeRequestPage : ContentPage
{
    readonly ISupabaseService _db;
    readonly UserContextState _user;
    readonly Picker _parcel = new() { Title = "Eigene Parzelle" };
    readonly Picker _medium = new() { Title = "Medium" };
    readonly Label _meter = new() { Text = "Bitte zuerst Parzelle und Medium wählen." };
    readonly DatePicker _out = new() { Date = DateTime.Today };
    readonly Entry _end = new() { Placeholder = "Endstand" };
    readonly Entry _number = new() { Placeholder = "Neue Zählernummer" };
    readonly DatePicker _cal = new() { Date = DateTime.Today };
    readonly DatePicker _install = new() { Date = DateTime.Today };
    readonly Entry _initial = new() { Placeholder = "Anfangsstand" };
    readonly Label _status = new();
    long? _oldMeterId;
    int _contextVersion;
    bool _refreshingMedia;

    public MeterChangeRequestPage(ISupabaseService db, UserContextState user)
    {
        _db = db; _user = user; Title = "Zählerwechsel melden";
        var save = new Button { Text = "Zählerwechsel einreichen" };
        save.Clicked += async (_, _) => await Save();
        _parcel.SelectedIndexChanged += async (_, _) => await RefreshMedia();
        _medium.SelectedIndexChanged += async (_, _) => await ResolveMeter();
        _out.DateSelected += async (_, _) => await RefreshMedia();
        Content = new ScrollView { Content = new VerticalStackLayout { Padding = 24, Spacing = 10, Children =
        {
            new Label { Text = "Zählerwechsel melden", FontSize = 24 },
            new Label { Text = "Die Einreichung verändert keinen Zählerbestand." },
            _parcel, _medium, new Label { Text = "Aktiver Zähler" }, _meter,
            new Label { Text = "Ausbaudatum" }, _out, _end, _number, _cal, _install, _initial, _status, save
        } } };
        Appearing += async (_, _) => await Load();
    }

    async Task Load()
    {
        if (!PermissionChecks.CanSubmitOwnMeterReadings(_user.CurrentUserContext)) { _status.Text = "Keine Berechtigung für eigene Zählerwechsel."; return; }
        var today = DateTime.Today;
        var parcels = (await _db.GetPachtbeziehungenForMitgliedAsync((int)(_user.CurrentMitgliedId ?? 0)))
            .Where(x => x.GueltigAb.Date <= today && (!x.GueltigBis.HasValue || x.GueltigBis.Value.Date >= today))
            .Select(x => x.ParzelleId).Distinct().Cast<object>().ToList();
        _parcel.ItemsSource = parcels;
        _medium.ItemsSource = Array.Empty<string>();
        if (parcels.Count == 0) _status.Text = "Für Ihr Mitgliedskonto ist keine aktuell wirksame Parzelle hinterlegt.";
    }

    async Task RefreshMedia()
    {
        var contextVersion = ++_contextVersion;
        _oldMeterId = null;
        _refreshingMedia = true;
        _medium.SelectedIndex = -1;
        _medium.ItemsSource = Array.Empty<string>();
        if (_parcel.SelectedItem is not long parcel) { _meter.Text = "Bitte zuerst eine Parzelle wählen."; _refreshingMedia = false; return; }
        _meter.Text = "Relevante Medien werden geladen …";
        var media = new List<string>();
        foreach (var medium in new[] { "strom", "wasser" })
            if ((await _db.GetActiveMeterCandidatesAsync((int)parcel, medium, _out.Date!.Value)).Count > 0)
                media.Add(medium);
        if (contextVersion != _contextVersion) { _refreshingMedia = false; return; }
        _medium.ItemsSource = media;
        _meter.Text = media.Count == 0
            ? "Für diese Parzelle wurde zum Ausbaudatum kein aktiver Zähler gefunden. Einreichen ist nicht möglich."
            : "Bitte ein Medium wählen.";
        _refreshingMedia = false;
    }

    async Task ResolveMeter()
    {
        if (_refreshingMedia) return;
        var contextVersion = ++_contextVersion;
        // Jede Kontextänderung verwirft bewusst die zuvor nur intern gehaltene ID.
        _oldMeterId = null;
        _meter.Text = "Aktive Zähler werden geladen …";
        if (_parcel.SelectedItem is not long parcel || _medium.SelectedItem is not string medium) { _meter.Text = "Bitte Parzelle und Medium wählen."; return; }
        var matches = await _db.GetActiveMeterCandidatesAsync((int)parcel, medium, _out.Date!.Value);
        if (contextVersion != _contextVersion) return;
        if (matches.Count == 0) { _meter.Text = "Für diese Parzelle und dieses Medium wurde zum Ausbaudatum kein aktiver Zähler gefunden. Einreichen ist nicht möglich."; return; }
        if (matches.Count > 1) { _meter.Text = "Für diese Parzelle und dieses Medium sind mehrere aktive Zähler hinterlegt. Bitte den Verein kontaktieren; Einreichen ist nicht möglich."; return; }
        var result = matches[0];
        _oldMeterId = result.Id;
        _meter.Text = $"{medium}: Zähler {result.Zaehlernummer} · eingebaut {result.EingebautAm:dd.MM.yyyy}";
    }

    async Task Save()
    {
        if (_parcel.SelectedItem is not long parcel || _medium.SelectedItem is not string medium || _oldMeterId is not long old ||
            !decimal.TryParse(_end.Text, NumberStyles.Number, CultureInfo.CurrentCulture, out var end) ||
            !decimal.TryParse(_initial.Text, NumberStyles.Number, CultureInfo.CurrentCulture, out var initial) || string.IsNullOrWhiteSpace(_number.Text))
        { _status.Text = "Bitte gültigen Zählerkontext und alle Wechselangaben angeben."; return; }
        var ok = await _db.SubmitOwnMeterChangeRequestAsync(new(old, parcel, medium, _out.Date!.Value, end, null, _number.Text, _cal.Date!.Value, _install.Date!.Value, initial, null));
        _status.Text = ok ? "Zählerwechselantrag wurde zur Prüfung eingereicht." : "Einreichung fehlgeschlagen.";
    }
}

public sealed class MeterChangeRequestReviewPage : ContentPage
{
    readonly ISupabaseService _db; readonly UserContextState _user; readonly CollectionView _list = new(); readonly ObservableCollection<MeterChangeRequestRecord> _items = []; readonly Label _status = new();
    public MeterChangeRequestReviewPage(ISupabaseService db, UserContextState user)
    {
        _db = db; _user = user; Title = "Zählerwechsel prüfen"; _list.ItemsSource = _items;
        _list.ItemTemplate = new DataTemplate(() => { var title = new Label { FontAttributes = FontAttributes.Bold }; title.SetBinding(Label.TextProperty, "NeueZaehlernummer"); var button = new Button { Text = "Prüfen" }; button.Clicked += async (s, _) => { if ((s as Button)?.BindingContext is MeterChangeRequestRecord request) await Open(request); }; button.SetBinding(Button.BindingContextProperty, "."); return new VerticalStackLayout { Children = { title, button } }; });
        Content = new VerticalStackLayout { Padding = 24, Children = { new Label { Text = "Offene Zählerwechselanträge", FontSize = 24 }, _status, _list } };
        Appearing += async (_, _) => await Load();
    }
    async Task Load()
    {
        if (!PermissionChecks.CanManageMeterChanges(_user.CurrentUserContext)) { _status.Text = "Keine Berechtigung."; return; }
        _items.Clear(); foreach (var request in await _db.GetOpenMeterChangeRequestsAsync()) _items.Add(request);
    }
    async Task Open(MeterChangeRequestRecord request)
    {
        await DisplayAlertAsync("Zählerwechselantrag", $"Antragsteller: Mitglied {request.AntragstellerMitgliedId}\nParzelle: {request.ParzelleId}\nMedium: {request.Medium}\nAusbau: {request.Ausbaudatum:dd.MM.yyyy}, Endstand {request.Endstand}\nNeuer Zähler: {request.NeueZaehlernummer}\nEichdatum: {request.Eichdatum:dd.MM.yyyy}\nEinbau: {request.Einbaudatum:dd.MM.yyyy}, Anfangsstand {request.Anfangsstand}\nAusbaufoto: {request.AusbauFotoPfad ?? "–"}\nEinbaufoto: {request.EinbauFotoPfad ?? "–"}", "Weiter");
        var action = await DisplayActionSheet("Antrag bearbeiten", "Abbrechen", null, "Korrigieren", "Ablehnen", "Zählerwechsel ausführen");
        if (action == "Korrigieren") await Correct(request);
        else if (action == "Ablehnen") await _db.ReviewMeterChangeRequestAsync(request.Id, "ablehnen", await DisplayPromptAsync("Ablehnen", "Prüfkommentar"));
        else if (action == "Zählerwechsel ausführen" && await DisplayAlertAsync("Ausführen", "Technischen Wechsel jetzt atomar ausführen?", "Ausführen", "Abbrechen")) await _db.ReviewMeterChangeRequestAsync(request.Id, "ausfuehren", await DisplayPromptAsync("Ausführen", "Prüfkommentar"));
        await Load();
    }
    async Task Correct(MeterChangeRequestRecord request)
    {
        var editor = new MeterChangeRequestCorrectionPage(_db, request);
        await Navigation.PushModalAsync(new NavigationPage(editor));
        if (await editor.Completion)
            await Load();
    }
}

public sealed class MeterChangeRequestCorrectionPage : ContentPage
{
    readonly ISupabaseService _db;
    readonly MeterChangeRequestRecord _request;
    readonly DatePicker _out;
    readonly Entry _end;
    readonly Entry _number;
    readonly DatePicker _calibration;
    readonly DatePicker _installation;
    readonly Entry _initial;
    readonly Editor _comment;
    readonly Label _status = new();
    readonly TaskCompletionSource<bool> _completion = new();
    public Task<bool> Completion => _completion.Task;

    public MeterChangeRequestCorrectionPage(ISupabaseService db, MeterChangeRequestRecord request)
    {
        _db = db;
        _request = request;
        Title = "Zählerwechsel korrigieren";
        _out = new DatePicker { Date = request.Ausbaudatum };
        _end = new Entry { Text = request.Endstand.ToString(CultureInfo.CurrentCulture), Keyboard = Keyboard.Numeric };
        _number = new Entry { Text = request.NeueZaehlernummer };
        _calibration = new DatePicker { Date = request.Eichdatum };
        _installation = new DatePicker { Date = request.Einbaudatum };
        _initial = new Entry { Text = request.Anfangsstand.ToString(CultureInfo.CurrentCulture), Keyboard = Keyboard.Numeric };
        _comment = new Editor { Text = request.Pruefkommentar, AutoSize = EditorAutoSizeOption.TextChanges };
        var save = new Button { Text = "Korrektur speichern" };
        var cancel = new Button { Text = "Abbrechen" };
        save.Clicked += async (_, _) => await Save();
        cancel.Clicked += async (_, _) => await Close(false);
        Content = new ScrollView { Content = new VerticalStackLayout { Padding = 24, Spacing = 10, Children =
        {
            new Label { Text = "Zählerwechsel korrigieren", FontSize = 24 },
            new Label { Text = $"Parzelle {_request.ParzelleId} · {_request.Medium}" },
            new Label { Text = "Ausbaudatum" }, _out,
            new Label { Text = "Endstand" }, _end,
            new Label { Text = "Neue Zählernummer" }, _number,
            new Label { Text = "Eichdatum" }, _calibration,
            new Label { Text = "Einbaudatum" }, _installation,
            new Label { Text = "Anfangsstand" }, _initial,
            new Label { Text = "Prüfkommentar" }, _comment, _status, save, cancel
        } } };
    }

    async Task Save()
    {
        if (string.IsNullOrWhiteSpace(_number.Text) ||
            !decimal.TryParse(_end.Text, NumberStyles.Number, CultureInfo.CurrentCulture, out var end) ||
            !decimal.TryParse(_initial.Text, NumberStyles.Number, CultureInfo.CurrentCulture, out var initial))
        {
            _status.Text = "Bitte alle fachlichen Angaben gültig eingeben.";
            return;
        }
        if (_installation.Date!.Value.Date < _out.Date!.Value.Date)
        {
            _status.Text = "Das Einbaudatum darf nicht vor dem Ausbaudatum liegen.";
            return;
        }
        _request.Ausbaudatum = _out.Date!.Value;
        _request.Endstand = end;
        _request.NeueZaehlernummer = _number.Text.Trim();
        _request.Eichdatum = _calibration.Date!.Value;
        _request.Einbaudatum = _installation.Date!.Value;
        _request.Anfangsstand = initial;
        var saved = await _db.CorrectMeterChangeRequestAsync(_request, _comment.Text);
        if (!saved) { _status.Text = "Korrektur konnte nicht gespeichert werden."; return; }
        await Close(true);
    }

    async Task Close(bool saved)
    {
        _completion.TrySetResult(saved);
        await Navigation.PopModalAsync();
    }

    protected override void OnDisappearing()
    {
        _completion.TrySetResult(false);
        base.OnDisappearing();
    }
}
