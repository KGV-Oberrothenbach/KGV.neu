using System.Text.Json;
using System.Security.Cryptography;
using System.Text;
using KGV.Maui.Settings;
using KGV.Core.Interfaces;
using KGV.Core.Models;
using Microsoft.Maui;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;

namespace KGV.Maui.Pages;

/// <summary>Online-only mobile client for the atomic G6a/G6b2 historical-contract RPC.</summary>
public sealed class HistoricalContractRegistrationPage : ContentPage
{
    private readonly ISupabaseService _supabase;
    private readonly IAuthService _auth;
    private PendingContract? _pending;
    private Guid _operationId = Guid.NewGuid();
    private readonly Button _submit = new() { Text = "7. Altvertrag übernehmen" };
    private readonly Button _newContract = new() { Text = "Neuen Altvertrag beginnen", IsVisible = false };
    private sealed record PendingContract(DokumentUploadRequest Upload, HistoricalContractRegistrationRequest Registration, bool Completed = false);
    private string PendingPath
    {
        get
        {
            var context = AppSettings.Vereinskontext ?? throw new InvalidOperationException("Vereinskontext fehlt.");
            var user = _auth.CurrentUserId ?? throw new InvalidOperationException("Benutzerkontext fehlt.");
            var scope = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(context.SupabaseUrl + ":" + context.VereinId + ":" + user)));
            return Path.Combine(FileSystem.AppDataDirectory, "historical-contract-" + scope + ".json");
        }
    }
    private async Task SavePendingAsync()
    {
        var path = PendingPath;
        await File.WriteAllTextAsync(path + ".tmp", JsonSerializer.Serialize(_pending));
        File.Move(path + ".tmp", path, true);
    }
    private async Task RestorePendingAsync()
    {
        if (_pending is not null) return;
        var path = PendingPath;
        if (!File.Exists(path)) return;
        _pending = JsonSerializer.Deserialize<PendingContract>(await File.ReadAllTextAsync(path))
            ?? throw new InvalidOperationException("Gespeicherter Vorgang ist nicht lesbar.");
        _operationId = _pending.Registration.OperationId ?? throw new InvalidOperationException("Gespeicherte Vorgangs-ID fehlt.");
        _status.Text = _pending.Completed ? "Altvertrag bereits abgeschlossen. Für einen weiteren Vertrag bitte einen neuen Vorgang beginnen." : "Offener Altvertrag vorhanden. Erneutes Übernehmen setzt ihn mit den ursprünglichen Angaben fort.";
        _file.Text = "3. Gespeicherte Datei: " + _pending.Upload.FileName;
        _submit.Text = "Offenen Vorgang fortsetzen";
        _submit.IsEnabled = !_pending.Completed;
        _newContract.IsVisible = _pending.Completed;
        if (_pending.Completed) { _resultDocumentId = _pending.Registration.DokumentId; _openResult.IsVisible = true; }
    }
    private readonly Picker _kind = new() { Title = "1. Vertragsart" };
    private readonly Picker _primary = new() { Title = "2. Hauptpächter" };
    private readonly Picker _secondary = new() { Title = "Optionaler zweiter Pächter" };
    private readonly VerticalStackLayout _leaseChoices = new() { Spacing = 4 };
    private readonly VerticalStackLayout _existingDocuments = new() { Spacing = 3 };
    private readonly VerticalStackLayout _history = new() { Spacing = 3 };
    private readonly VerticalStackLayout _removedPartyChoices = new() { Spacing = 3 };
    private readonly DatePicker _date = new() { Date = DateTime.Today };
    private readonly Switch _signed = new() { IsToggled = true };
    private readonly Switch _correctSince = new();
    private readonly DatePicker _memberSince = new() { Date = DateTime.Today };
    private readonly DatePicker _membershipEnd = new() { Date = DateTime.Today };
    private readonly Picker _activity = new() { Title = "Bestätigte Aktiv/Passiv-Änderung" };
    private readonly Label _file = new() { Text = "3. Keine Datei ausgewählt." };
    private readonly Label _status = new() { LineBreakMode = LineBreakMode.WordWrap };
    private readonly List<HistoricalContractLookupMember> _members = [];
    private readonly List<HistorischePachtbeziehung> _leases = [];
    private readonly Dictionary<long, CheckBox> _leaseChecks = [];
    private readonly Dictionary<long, Switch> _removedPartyMembershipEnds = [];
    private readonly List<HistoricalContractHistoryEntry> _contractHistory = [];
    private long? _resultDocumentId;
    private readonly Button _openResult = new() { Text = "Vertragsdokument öffnen", IsVisible = false };
    private byte[]? _content; private string _fileName = string.Empty; private string _mimeType = "application/octet-stream"; private bool _busy;

    public HistoricalContractRegistrationPage(ISupabaseService supabase, IAuthService auth)
    {
        _supabase = supabase; _auth = auth; Title = "Verträge nacherfassen";
        _kind.ItemsSource = new[] { "MA + PV – Erstvertrag", "PV – Folgevertrag" }; _kind.SelectedIndex = 0;
        _activity.ItemsSource = new[] { "Keine Statusänderung", "aktiv setzen", "passiv setzen" }; _activity.SelectedIndex = 0;
        _primary.SelectedIndexChanged += async (_, _) => await LoadLeasesAsync();
        _date.DateSelected += async (_, _) => await RefreshHistoryAsync();
        var choose = new Button { Text = "Datei auswählen" }; choose.Clicked += async (_, _) => await SelectFileAsync();
        _openResult.Clicked += async (_, _) => await OpenResultAsync();
        _submit.Clicked += async (_, _) => await SubmitAsync();
        _newContract.Clicked += (_, _) => {
            if (_busy || _pending?.Completed != true) return;
            File.Delete(PendingPath); _pending = null; _operationId = Guid.NewGuid(); _content = null; _file.Text = "3. Keine Datei ausgewählt.";
            _resultDocumentId = null; _openResult.IsVisible = false; _newContract.IsVisible = false;
            _submit.IsEnabled = true; _submit.Text = "7. Altvertrag übernehmen"; _status.Text = "Neuer Altvertrag.";
        };
        Content = new ScrollView { Content = new VerticalStackLayout { Padding = 20, Spacing = 12, Children =
        {
            new Label { Text = "Historischen Vertrag nacherfassen", FontSize = 22, FontAttributes = FontAttributes.Bold },
            new Label { Text = "Online-Abschluss: Parteien, Mitgliedschaft und Vertragsstände werden gemeinsam atomar gespeichert." },
            _kind, _primary, _secondary, new Label { Text = "2. Betroffene Pachtbeziehungen / Parzellen (Mehrfachauswahl)" }, _leaseChoices,
            choose, _file, new Label { Text = "4. Vertragsdaten / Historie" }, _date, new HorizontalStackLayout { Children = { new Label { Text = "Unterzeichnet" }, _signed } },
            new Label { Text = "5. Änderungen und Hinweise" }, new HorizontalStackLayout { Children = { new Label { Text = "Mitglied seit bewusst korrigieren" }, _correctSince } }, _memberSince,
            new Label { Text = "Vorhandene Vertragsstände der ausgewählten Parzellen" }, _history, _existingDocuments,
            new Label { Text = "Herausfallende Parteien (Mitgliedschaft nur bewusst beenden)" }, _removedPartyChoices, _membershipEnd, _activity,
            new Label { Text = "6. Zusammenfassung: Die 80xxx-Vertragsnummer und der PDF-Stempel werden beim Übernehmen automatisch erzeugt." }, _submit, _status, _openResult, _newContract
        } } };
        Appearing += async (_, _) => { try { await RestorePendingAsync(); await LoadMembersAsync(); } catch (Exception ex) { _submit.IsEnabled = false; _status.Text = ex.Message; } };
    }

    private async Task LoadMembersAsync()
    {
        try
        {
            _members.Clear(); _members.AddRange(await _supabase.GetHistoricalContractMembersAsync());
            var labels = _members.Select(MemberLabel).ToList();
            _primary.ItemsSource = labels; _secondary.ItemsSource = new[] { "Kein zweiter Pächter" }.Concat(labels).ToList(); _secondary.SelectedIndex = 0;
        }
        catch (Exception ex) { _status.Text = $"Mitglieder konnten nicht geladen werden: {ex.Message}"; }
    }

    private async Task LoadLeasesAsync()
    {
        _existingDocuments.Children.Clear();
        _leaseChoices.Children.Clear(); _leaseChecks.Clear(); _leases.Clear();
        if (_primary.SelectedIndex < 0) return;
        var member = _members[_primary.SelectedIndex];
        try
        {
            _leases.AddRange(await _supabase.GetHistoricalContractLeaseRelationshipsAsync(member.Id));
            foreach (var lease in _leases)
            {
                var check = new CheckBox(); check.CheckedChanged += async (_, _) => await RefreshHistoryAsync(); _leaseChecks[lease.PachtverhaeltnisId] = check;
                _leaseChoices.Children.Add(new HorizontalStackLayout { Children = { check, new Label { Text = $"Garten {lease.Parzellennummer} · {lease.Anlage} · {lease.Rolle}" } } });
            }
            foreach (var document in await _supabase.GetHistoricalContractDocumentsAsync(member.Id))
            {
                var open = new Button { Text = $"{document.Title ?? "Historischer Vertrag"} · {document.Date:dd.MM.yyyy} · Vertrags-Nr. {document.Number?.ToString() ?? "–"}" };
                open.Clicked += async (_, _) => await OpenDocumentAsync(document.DocumentId);
                _existingDocuments.Children.Add(open);
            }
            _memberSince.Date = member.MitgliedSeit ?? DateTime.Today;
        }
        catch (Exception ex) { _status.Text = $"Pachtbeziehungen konnten nicht geladen werden: {ex.Message}"; }
    }

    private async Task RefreshHistoryAsync()
    {
        _history.Children.Clear(); _removedPartyChoices.Children.Clear(); _removedPartyMembershipEnds.Clear(); _contractHistory.Clear();
        var relationIds = _leaseChecks.Where(item => item.Value.IsChecked).Select(item => item.Key).ToList();
        if (relationIds.Count == 0) return;
        try
        {
            _contractHistory.AddRange(await _supabase.GetHistoricalContractHistoryAsync(relationIds, DateOnly.FromDateTime(_date.Date ?? DateTime.Today)));
            foreach (var entry in _contractHistory)
                _history.Children.Add(new Label { Text = $"{entry.WirksamAb:dd.MM.yyyy}–{entry.GueltigBis?.ToString("dd.MM.yyyy") ?? "offen"}, Nr. {entry.HistorischeVertragsnummer?.ToString() ?? "–"}, Parteien {string.Join(" / ", entry.VertragsparteiMitgliedIds)}{(entry.IstVorgaenger ? " (Vorgänger)" : entry.IstNachfolger ? " (wird danach eingeordnet)" : string.Empty)}" });
            var parties = new HashSet<long> { _primary.SelectedIndex >= 0 ? _members[_primary.SelectedIndex].Id : 0 };
            if (_secondary.SelectedIndex > 0) parties.Add(_members[_secondary.SelectedIndex - 1].Id);
            foreach (var memberId in _contractHistory.Where(item => item.IstVorgaenger).SelectMany(item => item.VertragsparteiMitgliedIds).Distinct().Where(memberId => !parties.Contains(memberId)))
            {
                var toggle = new Switch(); _removedPartyMembershipEnds[memberId] = toggle;
                var member = _members.FirstOrDefault(item => item.Id == memberId);
                _removedPartyChoices.Children.Add(new HorizontalStackLayout { Children = { toggle, new Label { Text = $"{(member is null ? $"Mitglied {memberId}" : MemberLabel(member))} ebenfalls als Mitglied beenden" } } });
            }
        }
        catch (Exception ex) { _status.Text = $"Vertragshistorie konnte nicht geladen werden: {ex.Message}"; }
    }

    private async Task SelectFileAsync()
    {
        var selected = await FilePicker.Default.PickAsync(new PickOptions { PickerTitle = "Historischen Vertrag auswählen" });
        if (selected == null) return;
        await using var input = await selected.OpenReadAsync(); using var output = new MemoryStream(); await input.CopyToAsync(output);
        _content = output.ToArray(); _fileName = selected.FileName; _mimeType = string.IsNullOrWhiteSpace(selected.ContentType) ? "application/octet-stream" : selected.ContentType; _file.Text = $"3. Datei: {_fileName}";
    }

    private async Task SubmitAsync()
    {
        if (_busy || _pending?.Completed == true) return;
        if (_pending is null && (_primary.SelectedIndex < 0 || _content is not { Length: > 0 })) { _status.Text = "Bitte Hauptpächter, mindestens eine Parzelle und eine Datei wählen."; return; }
        var relations = _leaseChecks.Where(x => x.Value.IsChecked).Select(x => x.Key).ToList(); if (_pending is null && relations.Count == 0) { _status.Text = "Bitte mindestens eine Pachtbeziehung auswählen."; return; }
        _busy = true; _submit.IsEnabled = false;
        try
        {
            if (_pending is null)
            {
                var operationId = _operationId;
                var primary = _members[_primary.SelectedIndex]; var second = _secondary.SelectedIndex > 0 ? _members[_secondary.SelectedIndex - 1] : null;
                var code = _kind.SelectedIndex == 0 ? HistoricalContractDocumentTypes.MembershipAndLeaseInitialContract : HistoricalContractDocumentTypes.LeaseFollowUpContract;
                var partyIds = second == null ? new List<long> { primary.Id } : new List<long> { primary.Id, second.Id };
                var contractDate = _date.Date ?? DateTime.Today;
                var ends = _removedPartyMembershipEnds.Where(item => item.Value.IsToggled).Select(item => new HistoricalMembershipEnd(item.Key, _membershipEnd.Date ?? contractDate)).ToArray();
                var status = _activity.SelectedIndex switch { 1 => new[] { new HistoricalActivityStatusChange(primary.Id, true) }, 2 => new[] { new HistoricalActivityStatusChange(primary.Id, false) }, _ => [] };
                var previousEnds = relations.ToDictionary(id => id, id => _contractHistory.FirstOrDefault(item => item.PachtverhaeltnisId == id && item.IstVorgaenger)?.VorgeschlagenesVorgaengerende is DateOnly end ? end.ToDateTime(TimeOnly.MinValue) : contractDate.AddDays(-1));
                var request = new HistoricalContractRegistrationRequest(0, code, contractDate, _signed.IsToggled, relations, previousEnds, _correctSince.IsToggled ? primary.Id : null, _correctSince.IsToggled ? _memberSince.Date ?? contractDate : null, partyIds, ends, status, operationId);
                _pending = new PendingContract(new DokumentUploadRequest { HistoricalOperationId = operationId, HistoricalContractType = code, MitgliedId = primary.Id, Titel = $"Historischer Vertrag – {contractDate:yyyy-MM-dd}", FileName = _fileName, MimeType = _mimeType, FileContent = _content! }, request);
            }
            // Durable snapshot before networking; this is explicitly resumed online,
            // never enqueued in HistoricalDocumentRegistrationQueue.
            await SavePendingAsync();
            var state = await _supabase.GetHistoricalContractOperationAsync(_pending.Registration.OperationId!.Value);
            var documentId = state?.DocumentId ?? _pending.Registration.DokumentId;
            if (documentId <= 0)
            {
                var upload = await _supabase.CreateDokumentAsync(_pending.Upload);
                if (!upload.Success || upload.Document?.Id is not > 0) throw new InvalidOperationException(upload.Message);
                documentId = upload.Document.Id;
            }
            _pending = _pending with { Registration = _pending.Registration with { DokumentId = documentId } };
            await SavePendingAsync();
            var result = await _supabase.RegisterHistoricalContractAsync(_pending.Registration);
            if (result.Count == 0) throw new InvalidOperationException("Der Server hat keine Vertragsfassung bestätigt.");
            var number = await _supabase.ReserveHistorischeVertragsnummerAsync(documentId);
            _pending = _pending with { Completed = true, Upload = new DokumentUploadRequest() };
            await SavePendingAsync();
            _newContract.IsVisible = true;
            _resultDocumentId = documentId; _openResult.IsVisible = true;
            _status.Text = $"Altvertrag übernommen. Vertrags-Nr. {result.First().HistorischeVertragsnummer}{(number.PdfGestempelt ? " wurde im PDF gestempelt." : " wurde vergeben.")}";
        }
        catch (Exception ex) { if (_pending?.Completed == true) _pending = _pending with { Completed = false }; _status.Text = $"Vorgang noch nicht vollständig bestätigt. Bitte denselben Vorgang erneut versuchen: {ex.Message}"; }
        finally { _busy = false; _submit.IsEnabled = _pending?.Completed != true; }
    }

    private async Task OpenResultAsync()
    {
        if (_resultDocumentId is not > 0) return;
        await OpenDocumentAsync(_resultDocumentId.Value);
    }

    private async Task OpenDocumentAsync(long documentId)
    {
        try
        {
            var loaded = await _supabase.LoadHistoricalContractDocumentAsync(documentId);
            if (loaded.Document.MimeType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase))
                await Navigation.PushAsync(new PdfViewerPage(loaded.Document.Title, loaded.Content));
            else
            {
                var extension = loaded.Document.MimeType.ToLowerInvariant() switch { "image/png" => ".png", "image/webp" => ".webp", _ => ".jpg" };
                var path = Path.Combine(FileSystem.CacheDirectory, Guid.NewGuid().ToString("N") + extension);
                await File.WriteAllBytesAsync(path, loaded.Content);
                await Launcher.Default.OpenAsync(new OpenFileRequest { File = new ReadOnlyFile(path) });
            }
        }
        catch (Exception ex) { _status.Text = $"Vertragsdokument konnte nicht geöffnet werden: {ex.Message}"; }
    }

    private static string MemberLabel(HistoricalContractLookupMember member) => $"{member.Mitgliedsnummer} – {member.Vorname} {member.Name}";
}
