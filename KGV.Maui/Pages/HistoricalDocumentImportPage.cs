using KGV.Core.Interfaces;
using KGV.Core.Models;
using KGV.Maui.Services.Documents;
using KGV.Maui.Models;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Media;
using Microsoft.Maui.Storage;

namespace KGV.Maui.Pages;

public sealed class HistoricalDocumentImportPage : ContentPage
{
    private readonly ISupabaseService _supabase;
    private readonly HistoricalDocumentRegistrationQueue _queue;
    private readonly int _memberId;
    private readonly Picker _type = new() { Title = "Dokumenttyp" };
    private readonly DatePicker _date = new() { Date = DateTime.Today };
    private readonly Switch _signed = new() { IsToggled = true };
    private readonly Picker _lease = new() { Title = "Pachtbeziehung auswählen", IsVisible = false };
    private readonly Picker _assignment = new() { Title = "Wartungsvertragszuordnung auswählen", IsVisible = false };
    private readonly Label _file = new() { Text = "Keine Datei ausgewählt." };
    private readonly Label _status = new() { LineBreakMode = LineBreakMode.WordWrap };
    private readonly Button _assignNumber = new() { Text = "Nummer zuweisen", IsVisible = false };
    private readonly List<DokumenttypItem> _types = [];
    private readonly List<HistorischePachtbeziehung> _leases = [];
    private readonly List<HistorischeWartungsvertragZuordnung> _assignments = [];
    private byte[]? _content; private string _name = string.Empty; private string _mime = "application/octet-stream"; private bool _busy;
    public event Func<Task>? RegistrationCompleted;

    public HistoricalDocumentImportPage(ISupabaseService supabase, HistoricalDocumentRegistrationQueue queue, int memberId)
    {
        _supabase = supabase; _queue = queue; _memberId = memberId; Title = "Neues Dokument / Scan";
        _type.SelectedIndexChanged += (_, _) => UpdateRequirements();
        var pick = new Button { Text = "Datei auswählen" }; pick.Clicked += async (_, _) => await SelectFileAsync(false);
        var camera = new Button { Text = "Foto aufnehmen", IsVisible = MediaPicker.Default.IsCaptureSupported }; camera.Clicked += async (_, _) => await SelectFileAsync(true);
        var save = new Button { Text = "Hochladen und zuordnen" }; save.Clicked += async (_, _) => await UploadAndRegisterAsync();
        var retry = new Button { Text = "Offene Zuordnung erneut versuchen" }; retry.Clicked += async (_, _) => await RetryAsync();
        _assignNumber.Clicked += async (_, _) => await AssignNumberAsync();
        Content = new ScrollView { Content = new VerticalStackLayout { Padding = 24, Spacing = 12, Children = { new Label { Text = "Historisches Mitgliedsdokument", FontSize = 22, FontAttributes = FontAttributes.Bold }, _file, pick, camera, _type, _date, new HorizontalStackLayout { Children = { new Label { Text = "Unterzeichnet" }, _signed } }, _lease, _assignment, save, retry, _assignNumber, _status } } };
        Appearing += async (_, _) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        try
        {
            _types.Clear(); _types.AddRange(await _supabase.GetAktiveDokumenttypenAsync()); _type.ItemsSource = _types.Select(x => x.Bezeichnung).ToList();
            _leases.Clear(); _leases.AddRange(await _supabase.GetPachtbeziehungenForMitgliedAsync(_memberId)); _lease.ItemsSource = _leases.Select(x => $"Parzelle {x.Parzellennummer} · {x.Rolle} · {x.GueltigAb:dd.MM.yyyy}–{x.GueltigBis?.ToString("dd.MM.yyyy") ?? "offen"}").ToList();
            _assignments.Clear(); _assignments.AddRange(await _supabase.GetHistorischeWartungsvertragZuordnungenForMitgliedAsync(_memberId)); _assignment.ItemsSource = _assignments.Select(x => $"{x.Bezeichnung} · {x.GueltigAb:dd.MM.yyyy}–{x.GueltigBis?.ToString("dd.MM.yyyy") ?? "offen"}").ToList();
            var pending = _queue.GetForMember(_memberId); if (pending.Count > 0) _status.Text = "Eine gespeicherte Datei wartet noch auf ihre fachliche Zuordnung.";
        }
        catch (Exception ex) { _status.Text = $"Auswahldaten konnten nicht geladen werden: {ex.Message}"; }
    }

    private void UpdateRequirements()
    {
        var selected = _type.SelectedIndex >= 0 ? _types[_type.SelectedIndex] : null;
        var needsLease = selected?.Wirkungen.Contains(DokumenttypWirkung.PachtbezugErforderlich) == true || selected?.Wirkungen.Contains(DokumenttypWirkung.PachtvertragNachweis) == true;
        var needsAssignment = selected?.Wirkungen.Contains(DokumenttypWirkung.WartungsvertragsbezugErforderlich) == true || selected?.Wirkungen.Contains(DokumenttypWirkung.WartungsvertragNachweis) == true;
        _lease.IsVisible = needsLease; _assignment.IsVisible = needsAssignment;
        if (needsAssignment && _assignments.Count == 0)
            _status.Text = "Für dieses Mitglied ist keine Wartungsvertragszuordnung verfügbar. Wartungsnachweise können nur beim Hauptmitglied registriert werden.";
    }

    private async Task SelectFileAsync(bool capture)
    {
        try
        {
            FileResult? file = capture ? await MediaPicker.Default.CapturePhotoAsync() : await FilePicker.Default.PickAsync(new PickOptions { PickerTitle = "Dokument auswählen" });
            if (file == null) return;
            await using var input = await file.OpenReadAsync(); using var output = new MemoryStream(); await input.CopyToAsync(output);
            _content = output.ToArray(); _name = file.FileName; _mime = string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType; _file.Text = $"Ausgewählt: {_name}";
        }
        catch (Exception ex) { _status.Text = $"Datei konnte nicht ausgewählt werden: {ex.Message}"; }
    }

    private async Task UploadAndRegisterAsync()
    {
        if (_busy) return;
        if (_type.SelectedIndex < 0 || _content is not { Length: > 0 }) { _status.Text = "Bitte Datei und Dokumenttyp auswählen."; return; }
        if (_date.Date is not DateTime documentDate) { _status.Text = "Bitte ein Dokumentdatum auswählen."; return; }
        var type = _types[_type.SelectedIndex]; var lease = _lease.IsVisible && _lease.SelectedIndex >= 0 ? _leases[_lease.SelectedIndex].PachtverhaeltnisId : (long?)null; var assignment = _assignment.IsVisible && _assignment.SelectedIndex >= 0 ? _assignments[_assignment.SelectedIndex].ZuordnungId : (long?)null;
        if ((_lease.IsVisible && lease is null) || (_assignment.IsVisible && assignment is null)) { _status.Text = "Bitte die erforderliche fachliche Zuordnung auswählen."; return; }
        _busy = true;
        try
        {
            var upload = await _supabase.CreateDokumentAsync(new DokumentUploadRequest { MitgliedId = _memberId, Titel = $"{type.Bezeichnung} – {documentDate:yyyy-MM-dd}", FileName = _name, MimeType = _mime, FileContent = _content });
            if (!upload.Success || upload.Document?.Id is not > 0) { _status.Text = upload.Success ? "Die Datei wurde nicht mit einer sicheren Dokument-ID bestätigt." : upload.Message; return; }
            var pending = _queue.Create(upload.Document.Id, _memberId, type.Id, documentDate, _signed.IsToggled, lease, assignment); _queue.Save(pending); await RegisterAsync(pending);
        }
        finally { _busy = false; }
    }

    private async Task RetryAsync()
    {
        var pending = _queue.GetForMember(_memberId).FirstOrDefault();
        if (pending == null) { _status.Text = "Keine offene Zuordnung vorhanden."; return; }
        await RegisterAsync(pending);
    }

    private async Task RegisterAsync(HistoricalDocumentRegistrationPending pending)
    {
        try
        {
            await _supabase.RegisterHistorischesDokumentAsync(new HistorischesDokumentRegistrierungRequest(pending.DokumentId, pending.DokumenttypId, pending.Dokumentdatum, pending.Unterzeichnet, pending.PachtverhaeltnisId, pending.WartungsvertragZuordnungId));
            var type = _types.FirstOrDefault(x => x.Id == pending.DokumenttypId);
            var isHistoricalCombinedContract = type?.Wirkungen.Contains(DokumenttypWirkung.MitgliedsantragNachweis) == true
                && type.Wirkungen.Contains(DokumenttypWirkung.PachtvertragNachweis);
            if (isHistoricalCombinedContract)
            {
                _assignNumber.IsVisible = true;
                _assignNumber.CommandParameter = pending;
                _status.Text = "Dokument fachlich zugeordnet. Prüfen Sie die Angaben und weisen Sie anschließend ausdrücklich die Vertragsnummer zu.";
                return;
            }
            _queue.Remove(pending.Id);
            _status.Text = "Dokument fachlich zugeordnet.";
            if (RegistrationCompleted != null)
                await RegistrationCompleted.Invoke();
            await Navigation.PopAsync();
        }
        catch { _status.Text = "Die Datei wurde gespeichert, die fachliche Zuordnung konnte jedoch noch nicht abgeschlossen werden. Sie können die Zuordnung erneut versuchen, ohne die Datei erneut hochzuladen."; }
    }

    private async Task AssignNumberAsync()
    {
        if (_busy || _assignNumber.CommandParameter is not HistoricalDocumentRegistrationPending pending) return;
        _busy = true;
        try
        {
            var result = await _supabase.ReserveHistorischeVertragsnummerAsync(pending.DokumentId);
            _queue.Remove(pending.Id);
            _assignNumber.IsVisible = false;
            _status.Text = result.PdfGestempelt
                ? $"Vertrags-Nr. {result.Nummer} wurde zugewiesen und im PDF gespeichert."
                : $"Vertrags-Nr. {result.Nummer} wurde zugewiesen. Der Bildscan bleibt unverändert; Titel und Metadaten führen die Nummer.";
            if (RegistrationCompleted != null)
                await RegistrationCompleted.Invoke();
        }
        catch (Exception ex) { _status.Text = $"Die Vertragsnummer konnte nicht zugewiesen werden: {ex.Message}"; }
        finally { _busy = false; }
    }
}
