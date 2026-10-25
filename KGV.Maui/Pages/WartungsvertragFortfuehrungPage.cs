using KGV.Core.Interfaces;
using KGV.Core.Models;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace KGV.Maui.Pages;

/// <summary>Server-driven continuation UI. It never writes an assignment directly.</summary>
public sealed class WartungsvertragFortfuehrungPage : ContentPage, IQueryAttributable
{
    private readonly ISupabaseService _service;
    private readonly VerticalStackLayout _content = new() { Padding = 24, Spacing = 10 };
    private readonly Label _status = new() { TextColor = Colors.DarkSlateBlue, LineBreakMode = LineBreakMode.WordWrap };
    private long _assignmentId;
    private WartungsvertragFortfuehrungDetail? _detail;
    private bool _busy;

    public WartungsvertragFortfuehrungPage(ISupabaseService service)
    {
        _service = service;
        Title = "Vertragsfortführung";
        Content = new ScrollView { Content = _content };
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("zuordnungId", out var raw) && long.TryParse(Uri.UnescapeDataString(raw?.ToString() ?? ""), out var id))
        { _assignmentId = id; _ = LoadAsync(); }
    }

    protected override async void OnAppearing() { base.OnAppearing(); await LoadAsync(); }

    private async Task LoadAsync()
    {
        if (_busy || _assignmentId <= 0) return;
        _busy = true;
        try { _detail = await _service.GetWartungsvertragFortfuehrungDetailAsync(_assignmentId); Render(); }
        catch (Exception ex) { _status.Text = FriendlyError(ex); Render(); }
        finally { _busy = false; }
    }

    private void Render()
    {
        _content.Children.Clear();
        _content.Children.Add(new Label { Text = "Wartungsvertrag fortführen", FontSize = 24, FontAttributes = FontAttributes.Bold });
        if (_detail == null) { _content.Children.Add(new Label { Text = "Die Folgeentscheidung konnte nicht geladen werden." }); return; }
        var d = _detail;
        _content.Children.Add(Text($"Bisherige Laufzeit: {d.GueltigAb:dd.MM.yyyy} – {(d.GueltigBis.HasValue ? d.GueltigBis.Value.ToString("dd.MM.yyyy") : "offen")}"));
        _content.Children.Add(Text(DecisionText(d)));
        if (d.FolgezuordnungId is > 0) _content.Children.Add(Text($"Folgezuordnung: {d.FolgeWartungsvertragTitel ?? "Wartungsvertrag"} · {d.FolgeGueltigAb:dd.MM.yyyy} – {d.FolgeGueltigBis:dd.MM.yyyy}"));
        if (!string.IsNullOrWhiteSpace(d.WorkflowStatus)) _content.Children.Add(Text($"Verlängerungsmitteilung: {WorkflowText(d.WorkflowStatus)}"));
        if (d.ZurueckgenommenAm.HasValue) _content.Children.Add(Text($"Zurückgenommen am {d.ZurueckgenommenAm.Value:dd.MM.yyyy}{(string.IsNullOrWhiteSpace(d.RuecknahmeBegruendung) ? "" : $" · {d.RuecknahmeBegruendung}")}"));
        if (!string.IsNullOrWhiteSpace(d.Begruendung)) _content.Children.Add(Text($"Begründung: {d.Begruendung}"));
        _content.Children.Add(_status);
        if (d.CanDecide) { _content.Children.Add(Button("Verlängern", ExtendAsync)); _content.Children.Add(Button("Nicht verlängern", NonExtendAsync)); }
        if (d.CanWithdrawNichtverlaengerung) _content.Children.Add(Button("Nichtverlängerung zurücknehmen", WithdrawAsync));
        if (d.Entscheidung == "verlaengern" && d.DokumentId is not > 0 && d.KommunikationsvorgangId is > 0) _content.Children.Add(Button("Dokumenterzeugung erneut versuchen", RetryDocumentAsync));
        if (d.DokumentId is > 0) _content.Children.Add(Button("Verlängerungsmitteilung öffnen", OpenDocumentAsync));
        _content.Children.Add(Button("Aktualisieren", LoadAsync));
    }

    private async Task ExtendAsync()
    {
        if (_detail?.GueltigBis is not { } end) return;
        var newEnd = await DisplayPromptAsync("Verlängern", $"Der bestehende Wartungsvertrag wird unverändert fortgeführt.\nNeuer Beginn (serverseitig bestimmt): {end.AddDays(1):dd.MM.yyyy}\nNeues Vertragsende (TT.MM.JJJJ)", initialValue: "");
        var board = await DisplayPromptAsync("Verlängern", "Vorstandsbeschlussdatum (TT.MM.JJJJ)", initialValue: "");
        if (!DateTime.TryParse(newEnd, out var validUntil) || !DateTime.TryParse(board, out var boardDate)) { _status.Text = "Neues Vertragsende und Vorstandsbeschlussdatum sind erforderlich."; Render(); return; }
        var note = await DisplayPromptAsync("Verlängern", "Bemerkung (optional)", initialValue: "");
        await RunAsync(async () =>
        {
            var result = await _service.VerlaengereWartungsvertragAsync(_assignmentId, validUntil, boardDate, note);
            // The continuation RPC is idempotent. Only its confirmed communication
            // process may be finalized; never derive it from the stale predecessor detail.
            await _service.DokumenterzeugungWartungsvertragVerlaengerungWiederholenAsync(result.VertragsparteiMitgliedId, result.KommunikationsvorgangId);
        });
    }

    private async Task NonExtendAsync()
    {
        var initiator = await DisplayActionSheetAsync("Initiator", "Abbrechen", null, "Verein", "Mitglied", "Beidseitig");
        if (initiator is null or "Abbrechen") return;
        var decision = await DisplayPromptAsync("Nicht verlängern", initiator == "Mitglied" ? "Eingangs-/Entscheidungsdatum (TT.MM.JJJJ)" : "Entscheidungsdatum (TT.MM.JJJJ)", initialValue: DateTime.Today.ToString("dd.MM.yyyy"));
        if (!DateTime.TryParse(decision, out var decisionDate)) { _status.Text = "Ein Entscheidungsdatum ist erforderlich."; Render(); return; }
        DateTime? board = null;
        if (initiator != "Mitglied") { var text = await DisplayPromptAsync("Nicht verlängern", "Vorstandsbeschlussdatum (TT.MM.JJJJ)"); if (!DateTime.TryParse(text, out var value)) { _status.Text = "Das Vorstandsbeschlussdatum ist erforderlich."; Render(); return; } board = value; }
        var reason = await DisplayPromptAsync("Nicht verlängern", "Begründung (optional)");
        await RunAsync(() => _service.NichtverlaengerungEntscheidenAsync(_assignmentId, initiator.ToLowerInvariant() switch { "verein" => "verein", "mitglied" => "mitglied", _ => "beidseitig" }, decisionDate, board, reason));
    }

    private async Task WithdrawAsync()
    {
        if (_detail?.FolgeentscheidungId is not > 0 || !await DisplayAlertAsync("Rücknahme", "Nichtverlängerung wirklich zurücknehmen?", "Zurücknehmen", "Abbrechen")) return;
        var reason = await DisplayPromptAsync("Rücknahme", "Begründung (optional)");
        await RunAsync(() => _service.NichtverlaengerungZuruecknehmenAsync(_detail.FolgeentscheidungId.Value, reason));
    }

    private Task RetryDocumentAsync() => RunAsync(RetryDocumentIfPossibleAsync);
    private async Task RetryDocumentIfPossibleAsync() { if (_detail?.KommunikationsvorgangId is > 0) await _service.DokumenterzeugungWartungsvertragVerlaengerungWiederholenAsync(_detail.HauptmitgliedId, _detail.KommunikationsvorgangId.Value); }
    private async Task OpenDocumentAsync() { if (_detail?.DokumentId is not > 0) return; var doc = await _service.GetDokumentByIdAsync(_detail.DokumentId.Value); if (doc == null) throw new InvalidOperationException("Das finale Dokument konnte nicht geladen werden."); var content = await _service.DownloadDokumentContentAsync(doc); if (content is not { Length: > 0 }) throw new InvalidOperationException("Das finale Dokument konnte nicht geladen werden."); await Navigation.PushAsync(new PdfViewerPage(doc.Title, content)); }
    private async Task RunAsync(Func<Task> action) { if (_busy) return; _busy = true; try { _status.Text = "Wird gespeichert."; await action(); _status.Text = "Gespeichert."; } catch (Exception ex) { _status.Text = FriendlyError(ex); } finally { _busy = false; await LoadAsync(); } }
    private static Label Text(string value) => new() { Text = value, LineBreakMode = LineBreakMode.WordWrap };
    private static Button Button(string text, Func<Task> action) { var button = new Button { Text = text, HorizontalOptions = LayoutOptions.Start }; button.Clicked += async (_, _) => await action(); return button; }
    private static string DecisionText(WartungsvertragFortfuehrungDetail d) => d.Entscheidung switch { "verlaengern" => "Verlängert.", "nicht_verlaengern" when d.ZurueckgenommenAm.HasValue => "Nichtverlängerung zurückgenommen.", "nicht_verlaengern" => $"Nichtverlängerung beschlossen. Vertrag läuft bis {d.GueltigBis:dd.MM.yyyy}.", _ => "Noch keine Folgeentscheidung." };
    private static string WorkflowText(string status) => status switch { "dokument_ausstehend" => "Verlängerungsmitteilung wird erstellt", "abgeschlossen" => "Verlängerungsmitteilung erstellt", "fehlgeschlagen" => "Dokumenterzeugung fehlgeschlagen", _ => status };
    private static string FriendlyError(Exception ex) { var m = ex.Message; if (m.Contains("42501") || m.Contains("Berechtigung", StringComparison.OrdinalIgnoreCase)) return "Keine Berechtigung für diese Wartungsvertragsentscheidung."; if (m.Contains("bereits", StringComparison.OrdinalIgnoreCase)) return "Für diese Laufzeit liegt bereits eine wirksame Entscheidung vor."; if (m.Contains("Kapazit", StringComparison.OrdinalIgnoreCase) || m.Contains("Überschneid", StringComparison.OrdinalIgnoreCase)) return "Die Verlängerung ist wegen Kapazität oder Überschneidung nicht zulässig."; return "Die Aktion konnte nicht durchgeführt werden. " + m; }
}
