using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using KGV.Core.Models;
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
    private readonly Func<PachtvertragDokumentRequest?, bool>? _hasPreparedUnsavedChanges;
    private readonly DatePicker _altvertragDatePicker;
    private readonly VerticalStackLayout _altvertragDateSection;
    private readonly DatePicker _uebergabeDatePicker;
    private readonly CheckBox _befristetCheckBox;
    private readonly DatePicker _befristetDatePicker;
    private readonly VerticalStackLayout _befristetDateSection;
    private readonly Entry _kleingartenordnungEntry;
    private readonly CheckBox _kleingartenordnungAusgehaendigtCheckBox;
    private readonly DatePicker _kleingartenordnungAusgehaendigtDatePicker;
    private readonly VerticalStackLayout _kleingartenordnungAusgehaendigtDateSection;
    private readonly Editor _weitereVereinbarungenEditor;
    private readonly CheckBox _anlage1CheckBox;
    private readonly CheckBox _anlage2CheckBox;
    private readonly CheckBox _anlage3CheckBox;
    private readonly CheckBox _anlage4CheckBox;
    private readonly Entry _vereinsunterzeichnerEntry;
    private bool? _hasPreviousContract;
    private bool _includeSecondaryMember;
    private bool _cancelInProgress;

    public PachtvertragDialogPage(MitgliedRecord member, ParzelleRecord parzelle, DateTime vertragsbeginn,
        GesetzlicherVertreterAufloesung? gesetzlicherVertreterAufloesung, MitgliedRecord? nebenmitglied,
        PachtvertragDokumentRequest? initialRequest = null,
        Func<PachtvertragDokumentRequest?, bool>? hasPreparedUnsavedChanges = null)
    {
        _member = member ?? throw new ArgumentNullException(nameof(member));
        _parzelle = parzelle ?? throw new ArgumentNullException(nameof(parzelle));
        _vertragsbeginn = (initialRequest?.Vertragsbeginn ?? vertragsbeginn).Date;
        _nebenmitglied = nebenmitglied;
        _hasPreparedUnsavedChanges = hasPreparedUnsavedChanges;
        _istMinderjaehrig = gesetzlicherVertreterAufloesung?.IstMinderjaehrig ?? false;
        _gesetzlicherVertreter = gesetzlicherVertreterAufloesung?.VertreterMitglied;
        _hasPreviousContract = initialRequest?.HasPreviousContract;
        _includeSecondaryMember = !_istMinderjaehrig && _nebenmitglied != null && (initialRequest?.IncludeSecondaryMember ?? true);
        if (_istMinderjaehrig && (_gesetzlicherVertreter == null || _gesetzlicherVertreter.Id <= 0)) throw new InvalidOperationException("Für dieses minderjährige Mitglied ist im signierten Mitgliedsantrag kein gesetzlicher Vertreter hinterlegt.");

        Title = "Pachtvertrag";
        BackgroundColor = Colors.White;
        _altvertragDatePicker = new DatePicker { Date = (initialRequest?.AltvertragDatum ?? _vertragsbeginn).Date };
        _altvertragDateSection = CreateField("Datum des Altvertrags", _altvertragDatePicker);
        _altvertragDateSection.IsVisible = _hasPreviousContract == true;
        _uebergabeDatePicker = new DatePicker { Date = (initialRequest?.UebergabeDatum ?? initialRequest?.Vertragsbeginn ?? vertragsbeginn).Date };
        _befristetCheckBox = new CheckBox { IsChecked = initialRequest?.BefristetBis != null };
        _befristetDatePicker = new DatePicker { Date = (initialRequest?.BefristetBis ?? _vertragsbeginn).Date };
        _befristetDateSection = CreateField("Befristet bis", _befristetDatePicker);
        _befristetDateSection.IsVisible = _befristetCheckBox.IsChecked;
        _befristetCheckBox.CheckedChanged += (_, e) => _befristetDateSection.IsVisible = e.Value;
        _kleingartenordnungEntry = new Entry { Text = initialRequest?.KleingartenordnungFassung ?? "", Placeholder = "Kleingartenordnung / Fassung" };
        _kleingartenordnungAusgehaendigtCheckBox = new CheckBox { IsChecked = initialRequest?.KleingartenordnungAusgehaendigtAm != null };
        _kleingartenordnungAusgehaendigtDatePicker = new DatePicker { Date = (initialRequest?.KleingartenordnungAusgehaendigtAm ?? _vertragsbeginn).Date };
        _kleingartenordnungAusgehaendigtDateSection = CreateField("Ausgehändigt am", _kleingartenordnungAusgehaendigtDatePicker);
        _kleingartenordnungAusgehaendigtDateSection.IsVisible = _kleingartenordnungAusgehaendigtCheckBox.IsChecked;
        _kleingartenordnungAusgehaendigtCheckBox.CheckedChanged += (_, e) => _kleingartenordnungAusgehaendigtDateSection.IsVisible = e.Value;
        _weitereVereinbarungenEditor = new Editor { Text = initialRequest?.WeitereVereinbarungen ?? "", AutoSize = EditorAutoSizeOption.TextChanges, Placeholder = "Weitere Vereinbarungen" };
        _anlage1CheckBox = new CheckBox { IsChecked = initialRequest?.Anlage1 ?? false };
        _anlage2CheckBox = new CheckBox { IsChecked = initialRequest?.Anlage2 ?? false };
        _anlage3CheckBox = new CheckBox { IsChecked = initialRequest?.Anlage3 ?? false };
        _anlage4CheckBox = new CheckBox { IsChecked = initialRequest?.Anlage4 ?? false };
        _vereinsunterzeichnerEntry = new Entry { Text = initialRequest?.VereinsUnterzeichner ?? "", Placeholder = "Vereinsunterzeichner" };

        var content = new VerticalStackLayout { Padding = 24, Spacing = 14, Children =
        {
            new Label { Text = "Pachtvertrag erstellen", FontSize = 24, FontAttributes = FontAttributes.Bold },
            CreateField("Mitglied", new Label { Text = BuildMemberDisplayName(member) }),
            CreateField("Parzelle", new Label { Text = BuildParzelleDisplayName(parzelle) }),
            CreateField("Vertragsbeginn", new Label { Text = _vertragsbeginn.ToString("dd.MM.yyyy", DeCulture) }),
            CreateField("Altvertrag", CreatePreviousContractSelector()), _altvertragDateSection,
            CreateField("Übergabedatum", _uebergabeDatePicker),
            CreateField("Befristung", CreateCheckBoxRow(_befristetCheckBox, "Pachtvertrag befristet")), _befristetDateSection
        }};
        AddRepresentativeOrSecondaryMember(content);
        content.Children.Add(CreateField("Kleingartenordnung", _kleingartenordnungEntry));
        content.Children.Add(CreateField("Kleingartenordnung ausgehändigt", CreateCheckBoxRow(_kleingartenordnungAusgehaendigtCheckBox, "Kleingartenordnung ausgehändigt")));
        content.Children.Add(_kleingartenordnungAusgehaendigtDateSection);
        content.Children.Add(CreateField("Weitere Vereinbarungen", _weitereVereinbarungenEditor));
        content.Children.Add(CreateField("Anlagen", new VerticalStackLayout { Spacing = 4, Children = { CreateCheckBoxRow(_anlage1CheckBox, "Anlage 1"), CreateCheckBoxRow(_anlage2CheckBox, "Anlage 2"), CreateCheckBoxRow(_anlage3CheckBox, "Anlage 3"), CreateCheckBoxRow(_anlage4CheckBox, "Anlage 4") } }));
        content.Children.Add(CreateField("Vereinsunterzeichner", _vereinsunterzeichnerEntry));
        var cancelButton = new Button { Text = "Abbrechen" }; cancelButton.Clicked += async (_, _) => await CancelAsync();
        var previewButton = new Button { Text = "Vorschau" }; previewButton.Clicked += async (_, _) => await PreviewOrAcceptAsync();
        content.Children.Add(new HorizontalStackLayout { Spacing = 12, HorizontalOptions = LayoutOptions.End, Children = { cancelButton, previewButton } });
        Content = new ScrollView { Content = content };
    }

    public Task<PachtvertragDokumentRequest?> WaitForResultAsync() => _resultSource.Task;
    protected override bool OnBackButtonPressed() { _ = CancelAsync(); return true; }

    private View CreatePreviousContractSelector()
    {
        var present = new RadioButton { Content = "Altvertrag vorhanden", IsChecked = _hasPreviousContract == true };
        var absent = new RadioButton { Content = "Kein Altvertrag", IsChecked = _hasPreviousContract == false };
        present.CheckedChanged += (_, e) => { if (e.Value) SetPreviousContract(true); };
        absent.CheckedChanged += (_, e) => { if (e.Value) SetPreviousContract(false); };
        return new VerticalStackLayout { Spacing = 4, Children = { present, absent } };
    }

    private void SetPreviousContract(bool value) { _hasPreviousContract = value; _altvertragDateSection.IsVisible = value; }

    private void AddRepresentativeOrSecondaryMember(VerticalStackLayout content)
    {
        if (_istMinderjaehrig)
        {
            content.Children.Add(CreateField("Gesetzlicher Vertreter", new VerticalStackLayout { Spacing = 4, Children = { new Label { Text = BuildMemberDisplayName(_gesetzlicherVertreter!) }, new Label { Text = "Aus dem signierten Mitgliedsantrag übernommen.", TextColor = Colors.Gray } } }));
            return;
        }
        if (_nebenmitglied == null) return;
        var checkBox = new CheckBox { IsChecked = _includeSecondaryMember, VerticalOptions = LayoutOptions.Center };
        checkBox.CheckedChanged += (_, e) => _includeSecondaryMember = e.Value;
        content.Children.Add(CreateField("Pächter/in 2", CreateCheckBoxRow(checkBox, $"{BuildMemberDisplayName(_nebenmitglied)} als Pächter/in 2 aufnehmen und unterschreiben lassen.")));
    }

    private async Task PreviewOrAcceptAsync()
    {
        if (!TryCreateCurrentRequest(out var request, out var validationMessage)) { await DisplayAlertAsync("Pachtvertrag", validationMessage!, "OK"); return; }
        _resultSource.TrySetResult(request);
        await Navigation.PopModalAsync();
    }

    private async Task CancelAsync()
    {
        if (_cancelInProgress) return;
        _cancelInProgress = true;
        try
        {
            var hasChanges = _hasPreparedUnsavedChanges != null && (!TryCreateCurrentRequest(out var request, out _) || _hasPreparedUnsavedChanges(request));
            if (hasChanges && !await DisplayAlertAsync("Ungespeicherte Änderungen", "Die Änderungen wurden noch nicht gespeichert. Änderungen verwerfen?", "Verwerfen", "Weiter bearbeiten")) return;
            _resultSource.TrySetResult(null);
            await Navigation.PopModalAsync();
        }
        finally { _cancelInProgress = false; }
    }

    private bool TryCreateCurrentRequest(out PachtvertragDokumentRequest? request, out string? validationMessage)
    {
        request = null; validationMessage = null;
        if (_hasPreviousContract is null) { validationMessage = "Bitte angeben, ob ein Altvertrag vorhanden ist."; return false; }
        if (_hasPreviousContract == true && _altvertragDatePicker.Date == default) { validationMessage = "Bitte das Datum des Altvertrags angeben."; return false; }
        request = new PachtvertragDokumentRequest
        {
            MitgliedId = _member.Id, ParzelleId = _parzelle.Id, Vertragsbeginn = _vertragsbeginn,
            HasPreviousContract = _hasPreviousContract, AltvertragDatum = _hasPreviousContract == true ? _altvertragDatePicker.Date?.Date : null,
            UebergabeDatum = _uebergabeDatePicker.Date?.Date, BefristetBis = _befristetCheckBox.IsChecked ? _befristetDatePicker.Date?.Date : null,
            KleingartenordnungFassung = (_kleingartenordnungEntry.Text ?? "").Trim(),
            KleingartenordnungAusgehaendigtAm = _kleingartenordnungAusgehaendigtCheckBox.IsChecked ? _kleingartenordnungAusgehaendigtDatePicker.Date?.Date : null,
            WeitereVereinbarungen = (_weitereVereinbarungenEditor.Text ?? "").Trim(),
            Anlage1 = _anlage1CheckBox.IsChecked, Anlage2 = _anlage2CheckBox.IsChecked, Anlage3 = _anlage3CheckBox.IsChecked, Anlage4 = _anlage4CheckBox.IsChecked,
            VereinsUnterzeichner = (_vereinsunterzeichnerEntry.Text ?? "").Trim(),
            IncludeSecondaryMember = !_istMinderjaehrig && _nebenmitglied != null && _includeSecondaryMember,
            IstMinderjaehrig = _istMinderjaehrig, Status = FormularDokumentStatus.Unsigniert
        };
        return true;
    }

    private static VerticalStackLayout CreateField(string title, View content) => new() { Spacing = 6, Children = { new Label { Text = title, FontAttributes = FontAttributes.Bold }, content } };
    private static HorizontalStackLayout CreateCheckBoxRow(CheckBox checkBox, string text) => new() { Spacing = 10, Children = { checkBox, new Label { Text = text, VerticalOptions = LayoutOptions.Center, LineBreakMode = LineBreakMode.WordWrap } } };
    private static string BuildMemberDisplayName(MitgliedRecord member) { var name = string.Join(" ", new[] { member.Vorname, member.Name }.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x!.Trim())); return string.IsNullOrWhiteSpace(name) ? $"Mitglied #{member.Id}" : $"{name} · #{member.Id}"; }
    private static string BuildParzelleDisplayName(ParzelleRecord parzelle) { var gartenNr = string.IsNullOrWhiteSpace(parzelle.GartenNr) ? $"#{parzelle.Id}" : parzelle.GartenNr.Trim(); var anlage = string.IsNullOrWhiteSpace(parzelle.Anlage) ? null : parzelle.Anlage.Trim(); return string.IsNullOrWhiteSpace(anlage) ? $"Garten {gartenNr}" : $"Garten {gartenNr} ({anlage})"; }
}
