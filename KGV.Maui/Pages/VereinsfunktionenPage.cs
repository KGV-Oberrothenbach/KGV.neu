using System.Collections.ObjectModel;
using System.Globalization;
using KGV.Core.Interfaces;
using KGV.Core.Models;
using KGV.Core.Security;
using KGV.Maui.State;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace KGV.Maui.Pages;

public sealed class VereinsfunktionenPage : ContentPage
{
    private readonly ISupabaseService _service;
    private readonly UserContextState _user;
    private readonly ObservableCollection<VereinsfunktionRecord> _functions = new();
    private readonly List<VereinsfunktionBesetzungRecord> _assignments = new();
    private readonly List<VereinsfunktionVertretungRecord> _representations = new();
    private readonly List<VereinsfunktionVerwendungRecord> _uses = new();
    private readonly Dictionary<long, MitgliedRecord> _members = new();
    private readonly VerticalStackLayout _details = new() { Spacing = 8 };
    private readonly Label _status = new() { TextColor = Colors.DarkSlateBlue, LineBreakMode = LineBreakMode.WordWrap };
    private VereinsfunktionRecord? _selected;
    private bool _busy;

    public VereinsfunktionenPage(ISupabaseService service, UserContextState user)
    {
        _service = service; _user = user; Title = "Vereinsfunktionen";
        var list = new CollectionView { ItemsSource = _functions, SelectionMode = SelectionMode.Single, EmptyView = new Label { Text = "Keine Vereinsfunktionen vorhanden." }, ItemTemplate = new DataTemplate(() =>
        {
            var title = new Label { FontAttributes = FontAttributes.Bold }; title.SetBinding(Label.TextProperty, nameof(VereinsfunktionRecord.Bezeichnung));
            var state = new Label { FontSize = 12, TextColor = Colors.Gray }; state.SetBinding(Label.TextProperty, new Binding(nameof(VereinsfunktionRecord.Aktiv), converter: new BoolTextConverter()));
            return new Border { Stroke = Colors.LightGray, Padding = 12, Margin = new Thickness(0, 0, 0, 6), Content = new VerticalStackLayout { Children = { title, state } } };
        })};
        list.SelectionChanged += (_, e) => { _selected = e.CurrentSelection.FirstOrDefault() as VereinsfunktionRecord; RenderDetails(); };
        Content = new ScrollView { Content = new VerticalStackLayout { Padding = 24, Spacing = 12, Children =
        {
            new Label { Text = "Vereinsfunktionen", FontSize = 24, FontAttributes = FontAttributes.Bold },
            new Label { Text = "Funktionen, Besetzungen, Vertretungen und Impressumsdaten sind Fachdaten. App-Rollen werden hier nicht verändert.", TextColor = Colors.Gray, LineBreakMode = LineBreakMode.WordWrap },
            Buttons(("Aktualisieren", LoadAsync), ("Neue Funktion", NewFunctionAsync)), _status, list, _details
        }}};
        Appearing += async (_, _) => await LoadAsync();
    }

    private bool Authorized => _user.CurrentUserContext?.Role is UserRole.Admin or UserRole.Vorstand;
    private async Task LoadAsync()
    {
        if (_busy) return;
        if (!Authorized) { _functions.Clear(); _details.Children.Clear(); _status.Text = "Für die Verwaltung von Vereinsfunktionen fehlt die Berechtigung."; return; }
        _busy = true; _status.Text = "Daten werden geladen.";
        try
        {
            var f = await _service.GetVereinsfunktionenAsync(); var b = await _service.GetVereinsfunktionBesetzungenAsync(); var r = await _service.GetVereinsfunktionVertretungenAsync(); var u = await _service.GetVereinsfunktionVerwendungenAsync(); var m = await _service.GetMitgliederAsync();
            _functions.Clear(); foreach (var x in f) _functions.Add(x); _assignments.Clear(); _assignments.AddRange(b); _representations.Clear(); _representations.AddRange(r); _uses.Clear(); _uses.AddRange(u); _members.Clear(); foreach (var x in m) _members[x.Id] = x;
            if (_selected != null) _selected = _functions.FirstOrDefault(x => x.Id == _selected.Id); RenderDetails(); _status.Text = string.Empty;
        }
        catch (Exception ex) { _status.Text = ErrorText(ex); } finally { _busy = false; }
    }

    private void RenderDetails()
    {
        _details.Children.Clear(); if (_selected == null) return;
        _details.Children.Add(new Label { Text = _selected.Bezeichnung, FontSize = 20, FontAttributes = FontAttributes.Bold });
        _details.Children.Add(new Label { Text = (_selected.Aktiv ? "Aktiv" : "Inaktiv") + " · " + (_selected.ErlaubtMehrfachbesetzung ? "Mehrfachbesetzung erlaubt" : "Einzelbesetzung"), TextColor = Colors.DarkSlateBlue });
        _details.Children.Add(Buttons(("Funktion bearbeiten", EditFunctionAsync), ("Besetzung hinzufügen", NewAssignmentAsync), ("Verwendung hinzufügen", NewUseAsync)));
        _details.Children.Add(new Label { Text = "Besetzungen", FontAttributes = FontAttributes.Bold });
        foreach (var x in _assignments.Where(x => x.VereinsfunktionId == _selected.Id).OrderByDescending(x => x.GueltigAb))
            _details.Children.Add(Row(MemberName(x.MitgliedId) + " · " + Date(x.GueltigAb) + " – " + Date(x.GueltigBis) + " · " + AssignmentState(x), "Bearbeiten / beenden", () => EditAssignmentAsync(x)));
        _details.Children.Add(new Label { Text = "Verwendungszwecke", FontAttributes = FontAttributes.Bold });
        foreach (var x in _uses.Where(x => x.VereinsfunktionId == _selected.Id))
            _details.Children.Add(UseRow(x));
        _details.Children.Add(new Label { Text = "Vertretungen", FontAttributes = FontAttributes.Bold });
        _details.Children.Add(Buttons(("Neue Vertretung", NewRepresentationAsync)));
        foreach (var x in _representations.Where(IsRepresentationForSelected))
            _details.Children.Add(Row(AssignmentName(x.VertreteneBesetzungId) + " → " + AssignmentName(x.VertreterBesetzungId) + " · " + Date(x.GueltigAb) + " – " + Date(x.GueltigBis), "Bearbeiten", () => EditRepresentationAsync(x)));
    }

    private async Task NewFunctionAsync() => await EditFunctionAsync(new VereinsfunktionRecord { Aktiv = true, ErlaubtMehrfachbesetzung = true }, true);
    private Task EditFunctionAsync() => EditFunctionAsync(_selected!, false);
    private async Task EditFunctionAsync(VereinsfunktionRecord record, bool isNew)
    {
        var name = new Entry { Text = record.Bezeichnung, Placeholder = "Bezeichnung" }; var active = new Switch { IsToggled = record.Aktiv }; var multi = new Switch { IsToggled = record.ErlaubtMehrfachbesetzung };
        await EditPage("Funktion", new[] { Field("Bezeichnung", name), Field("Aktiv", active), Field("Mehrfachbesetzung erlaubt", multi) }, async () =>
        {
            if (string.IsNullOrWhiteSpace(name.Text)) throw new InvalidOperationException("Bitte eine Bezeichnung eingeben.");
            record.Bezeichnung = name.Text.Trim(); record.Aktiv = active.IsToggled; record.ErlaubtMehrfachbesetzung = multi.IsToggled; if (isNew) record.Schluessel = Key(record.Bezeichnung); await _service.SaveVereinsfunktionAsync(record);
        });
    }

    private async Task NewAssignmentAsync() { if (_selected != null) await EditAssignmentAsync(new VereinsfunktionBesetzungRecord { VereinsfunktionId = _selected.Id, MitgliedId = _members.Keys.FirstOrDefault(), GueltigAb = DateTime.Today, SortOrder = 10 }); }
    private async Task EditAssignmentAsync(VereinsfunktionBesetzungRecord record)
    {
        var people = _members.Values.OrderBy(x => x.Name).ThenBy(x => x.Vorname).Select(x => new Choice(x.Id, MemberName(x.Id))).ToList(); var member = new Picker { ItemsSource = people, ItemDisplayBinding = new Binding(nameof(Choice.Label)) }; member.SelectedItem = people.FirstOrDefault(x => x.Id == record.MitgliedId);
        var from = new DatePicker { Date = record.GueltigAb == default ? DateTime.Today : record.GueltigAb }; var until = new DatePicker { Date = record.GueltigBis ?? DateTime.Today }; var useUntil = new Switch { IsToggled = record.GueltigBis.HasValue }; var order = new Entry { Text = record.SortOrder.ToString(), Keyboard = Keyboard.Numeric };
        member.IsEnabled = record.Id <= 0; from.IsEnabled = record.Id <= 0;
        await EditPage("Besetzung", new[] { Field("Mitglied", member), Field("Gültig ab", from), Field("Gültig bis verwenden", useUntil), Field("Gültig bis", until), Field("Sortierung", order) }, async () =>
        {
            if (member.SelectedItem is not Choice selected || !short.TryParse(order.Text, out var sort)) throw new InvalidOperationException("Bitte Mitglied und Sortierung prüfen.");
            record.MitgliedId = selected.Id; record.GueltigAb = from.Date!.Value.Date; record.GueltigBis = useUntil.IsToggled ? until.Date!.Value.Date : null; record.SortOrder = sort; await _service.SaveVereinsfunktionBesetzungAsync(record);
        });
    }

    private async Task NewRepresentationAsync() => await EditRepresentationAsync(new VereinsfunktionVertretungRecord { VertreteneBesetzungId = _assignments.FirstOrDefault()?.Id ?? 0, VertreterBesetzungId = _assignments.Skip(1).FirstOrDefault()?.Id ?? 0, GueltigAb = DateTime.Today });
    private async Task EditRepresentationAsync(VereinsfunktionVertretungRecord record)
    {
        var represented = AssignmentPicker(record.VertreteneBesetzungId); var representative = AssignmentPicker(record.VertreterBesetzungId); var from = new DatePicker { Date = record.GueltigAb == default ? DateTime.Today : record.GueltigAb }; var until = new DatePicker { Date = record.GueltigBis ?? DateTime.Today }; var useUntil = new Switch { IsToggled = record.GueltigBis.HasValue };
        represented.IsEnabled = record.Id <= 0; representative.IsEnabled = record.Id <= 0; from.IsEnabled = record.Id <= 0;
        await EditPage("Vertretung", new[] { Field("Vertretene Besetzung", represented), Field("Vertretende Besetzung", representative), Field("Gültig ab", from), Field("Gültig bis verwenden", useUntil), Field("Gültig bis", until) }, async () =>
        {
            if (represented.SelectedItem is not Choice one || representative.SelectedItem is not Choice two) throw new InvalidOperationException("Bitte beide Besetzungen auswählen.");
            record.VertreteneBesetzungId = one.Id; record.VertreterBesetzungId = two.Id; record.GueltigAb = from.Date!.Value.Date; record.GueltigBis = useUntil.IsToggled ? until.Date!.Value.Date : null; await _service.SaveVereinsfunktionVertretungAsync(record);
        });
    }

    private async Task NewUseAsync() { if (_selected != null) await EditUseAsync(new VereinsfunktionVerwendungRecord { VereinsfunktionId = _selected.Id }); }
    private async Task DeleteUseAsync(VereinsfunktionVerwendungRecord record)
    {
        var confirmed = await DisplayAlertAsync("Verwendungszweck entfernen", $"{record.Verwendungszweck} wirklich entfernen?", "Entfernen", "Abbrechen");
        if (!confirmed) return;
        try
        {
            await _service.DeleteVereinsfunktionVerwendungAsync(record.Id);
            await LoadAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Entfernen nicht möglich", ErrorText(ex), "OK");
        }
    }
    private async Task EditUseAsync(VereinsfunktionVerwendungRecord record)
    {
        var purpose = new Picker { ItemsSource = new List<string> { "impressum", "vertragsunterzeichnung", "mitgliedsaufnahme_abstimmung", "mitgliedsaufnahme_entscheidung", "wartungsvertrag_entscheidung" }, SelectedItem = record.Verwendungszweck }; var group = new Entry { Text = record.ImpressumGruppe }; var label = new Entry { Text = record.ImpressumGruppeBezeichnung }; var groupOrder = new Entry { Text = record.ImpressumGruppenSortOrder.ToString(), Keyboard = Keyboard.Numeric }; var order = new Entry { Text = record.SortOrder.ToString(), Keyboard = Keyboard.Numeric }; var primary = new Switch { IsToggled = record.ImpressumIstHauptkontakt }; var address = new Switch { IsToggled = record.ImpressumZeigeLadungsfaehigeAnschrift };
        await EditPage("Verwendungszweck", new[] { Field("Verwendungszweck", purpose), Field("Funktionssortierung", order), Field("Gruppenschlüssel (Impressum)", group), Field("Gruppenbezeichnung (Impressum)", label), Field("Gruppensortierung", groupOrder), Field("Hauptkontakt", primary), Field("Ladungsfähige Anschrift", address) }, async () =>
        {
            if (purpose.SelectedItem is not string value || !short.TryParse(order.Text, out var sort) || !short.TryParse(groupOrder.Text, out var groupSort)) throw new InvalidOperationException("Bitte Verwendungszweck und Sortierungen prüfen.");
            record.Verwendungszweck = value; record.SortOrder = sort; record.ImpressumGruppe = group.Text?.Trim() ?? "weitere"; record.ImpressumGruppeBezeichnung = label.Text?.Trim() ?? "Weitere Funktionen"; record.ImpressumGruppenSortOrder = groupSort; record.ImpressumIstHauptkontakt = primary.IsToggled; record.ImpressumZeigeLadungsfaehigeAnschrift = address.IsToggled; await _service.SaveVereinsfunktionVerwendungAsync(record);
        });
    }

    private async Task EditPage(string title, IEnumerable<View> fields, Func<Task> save)
    {
        var host = new VerticalStackLayout { Padding = 24, Spacing = 10 }; foreach (var field in fields) host.Children.Add(field); var page = new ContentPage { Title = title, Content = new ScrollView { Content = host } }; var saveButton = new Button { Text = "Speichern" }; var cancel = new Button { Text = "Abbrechen" };
        saveButton.Clicked += async (_, _) => { try { await save(); await Navigation.PopAsync(); await LoadAsync(); } catch (Exception ex) { await page.DisplayAlertAsync("Speichern nicht möglich", ErrorText(ex), "OK"); } }; cancel.Clicked += async (_, _) => await Navigation.PopAsync(); host.Children.Add(saveButton); host.Children.Add(cancel); await Navigation.PushAsync(page);
    }

    private HorizontalStackLayout Buttons(params (string text, Func<Task> action)[] values) { var row = new HorizontalStackLayout { Spacing = 8 }; foreach (var value in values) { var b = new Button { Text = value.text }; b.Clicked += async (_, _) => await value.action(); row.Children.Add(b); } return row; }
    private View Row(string text, string action, Func<Task> callback) { var button = new Button { Text = action }; button.Clicked += async (_, _) => await callback(); return new HorizontalStackLayout { Spacing = 8, Children = { new Label { Text = text, HorizontalOptions = LayoutOptions.Fill, LineBreakMode = LineBreakMode.WordWrap }, button } }; }
    private View UseRow(VereinsfunktionVerwendungRecord record)
    {
        var edit = new Button { Text = "Bearbeiten" }; edit.Clicked += async (_, _) => await EditUseAsync(record);
        var remove = new Button { Text = "Entfernen" }; remove.Clicked += async (_, _) => await DeleteUseAsync(record);
        var text = record.Verwendungszweck == "impressum" ? "Impressum · " + record.ImpressumGruppeBezeichnung : record.Verwendungszweck;
        return new HorizontalStackLayout { Spacing = 8, Children = { new Label { Text = text, HorizontalOptions = LayoutOptions.Fill, LineBreakMode = LineBreakMode.WordWrap }, edit, remove } };
    }
    private static View Field(string label, View input) => new VerticalStackLayout { Spacing = 2, Children = { new Label { Text = label }, input } };
    private Picker AssignmentPicker(long id) { var values = _assignments.Select(x => new Choice(x.Id, AssignmentName(x.Id))).ToList(); var picker = new Picker { ItemsSource = values, ItemDisplayBinding = new Binding(nameof(Choice.Label)) }; picker.SelectedItem = values.FirstOrDefault(x => x.Id == id); return picker; }
    private string MemberName(long id)
    {
        if (!_members.TryGetValue(id, out var member))
            return "Unbekanntes Mitglied";

        var name = $"{member.Vorname} {member.Name}".Trim();
        if (member.Mitgliedsnummer <= 0)
            return "Mitgliedsnummer nicht verfügbar";

        return string.IsNullOrWhiteSpace(name)
            ? $"Mgl.-Nr. {member.Mitgliedsnummer}"
            : $"{name} · Mgl.-Nr. {member.Mitgliedsnummer}";
    }
    private string AssignmentName(long id) { var item = _assignments.FirstOrDefault(x => x.Id == id); return item == null ? "Unbekannte Besetzung" : (_functions.FirstOrDefault(x => x.Id == item.VereinsfunktionId)?.Bezeichnung ?? "Funktion") + ": " + MemberName(item.MitgliedId); }
    private bool IsRepresentationForSelected(VereinsfunktionVertretungRecord representation) => _selected != null && _assignments.Any(x => x.VereinsfunktionId == _selected.Id && (x.Id == representation.VertreteneBesetzungId || x.Id == representation.VertreterBesetzungId));
    private static string Date(DateTime? value) => value?.ToString("dd.MM.yyyy") ?? "offen";
    private static string AssignmentState(VereinsfunktionBesetzungRecord item) => item.GueltigAb.Date > DateTime.Today ? "zukünftig" : item.GueltigBis is { } until && until.Date < DateTime.Today ? "historisch" : "aktuell";
    private static string Key(string value) => string.Concat(value.ToLowerInvariant().Select(c => char.IsLetterOrDigit(c) ? c : '_')).Trim('_');
    private static string ErrorText(Exception error) { var text = error.Message; if (text.Contains("eigene Besetzung", StringComparison.OrdinalIgnoreCase)) return "Eine Person kann nicht ihre eigene Besetzung vertreten."; if (text.Contains("nur eine Vertretung", StringComparison.OrdinalIgnoreCase) || text.Contains("überlappende Vertretung", StringComparison.OrdinalIgnoreCase)) return "Für eine Besetzung darf zu einem Zeitpunkt nur eine Vertretung gelten."; if (text.Contains("Vertretungszeitraum", StringComparison.OrdinalIgnoreCase)) return "Der Vertretungszeitraum muss innerhalb beider Besetzungen liegen."; if (text.Contains("Einzelbesetzung", StringComparison.OrdinalIgnoreCase)) return "Bei Einzelbesetzung dürfen sich Zeiträume nicht überschneiden."; if (text.Contains("historisch", StringComparison.OrdinalIgnoreCase) || text.Contains("Besetzung beenden und eine neue anlegen", StringComparison.OrdinalIgnoreCase)) return "Funktion, Mitglied und Beginn einer Besetzung oder Vertretung bleiben unveränderlich. Bitte den Eintrag beenden und neu anlegen."; if (text.Contains("Schlüssel", StringComparison.OrdinalIgnoreCase) && text.Contains("unveränderlich", StringComparison.OrdinalIgnoreCase)) return "Der technische Schlüssel bleibt nach der Neuanlage unveränderlich."; if (text.Contains("duplicate", StringComparison.OrdinalIgnoreCase) || text.Contains("unique", StringComparison.OrdinalIgnoreCase)) return "Diese Verwendung ist für die Funktion bereits hinterlegt."; if (text.Contains("Vertretung", StringComparison.OrdinalIgnoreCase)) return "Diese Änderung würde eine bestehende Vertretung ungültig machen."; return "Die Änderung konnte nicht gespeichert werden. " + text; }
    private sealed record Choice(long Id, string Label);
    private sealed class BoolTextConverter : IValueConverter { public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value is true ? "Aktiv" : "Inaktiv"; public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => false; }
}
